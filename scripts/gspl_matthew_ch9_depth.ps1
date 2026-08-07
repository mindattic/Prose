$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    $hash = $sha.ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()
}

function Exec-NonQuery([string]$sql, [hashtable]$params) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    foreach ($k in $params.Keys) {
        $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null
    }
    $cmd.ExecuteNonQuery() | Out-Null
}

function Exec-Scalar([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    return $cmd.ExecuteScalar()
}

function New-BeatRow([string]$text) {
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid()
    $hash = Sha256Hex $text
    $sql = "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())"
    Exec-NonQuery $sql @{ Id = $id; Text = $text; Hash = $hash; Number = $script:MaxNumber }
    return $id
}

function Add-BeatNode([guid]$nodeId, [guid]$beatId, [double]$sortKey) {
    $sql = "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)"
    Exec-NonQuery $sql @{ NodeId = $nodeId; BeatId = $beatId; SortKey = $sortKey }
}

function Seed-Entity([string]$name, [string]$slug, [string]$type, [string]$desc) {
    $exists = Exec-Scalar "SELECT COUNT(*) FROM Entities WHERE UniverseId='0197E9C9-0003-7000-8000-000000000003' AND Slug='$slug'"
    if ($exists -gt 0) { Write-Host "  entity exists, skip: $name"; return }
    $id = [guid]::NewGuid()
    $sql = "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId) VALUES (@Id, @Type, @Name, @Slug, 'canon', @Desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, '0197E9C9-0003-7000-8000-000000000003')"
    Exec-NonQuery $sql @{ Id = $id; Type = $type; Name = $name; Slug = $slug; Desc = $desc }
    Write-Host "  seeded entity: $name"
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch9NodeId = [guid]"019FA067-8522-77F9-898C-52F3ACA42AD1"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh9SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA067-8522-77F9-898C-52F3ACA42AD1' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh9SortKey=$maxCh9SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'papias-matthew-testimony' = @{ title="Papias, quoted by Eusebius: sayings in the Hebrew dialect"; body="Eusebius of Caesarea, Ecclesiastical History, Book 3, chapter 39, section 16, trans. Kirsopp Lake, Eusebius: The Ecclesiastical History, Volume I: Books 1-5, Loeb Classical Library 153 (Cambridge, MA: Harvard University Press, 1926). Eusebius preserves a fragment of the early-second-century bishop Papias of Hierapolis, whose own writings do not otherwise survive, stating that Matthew collected the sayings (logia) in the Hebrew dialect (Hebraidi dialekto), and each one interpreted them as best he could. This is the earliest surviving external testimony connecting the name Matthew to any written work at all, and the traditional starting point for attributing this Gospel to the apostle." }
'papias-hebrew-dialect-ambiguity' = @{ title="What did Papias actually mean?"; body="Raymond E. Brown, An Introduction to the New Testament, Anchor Bible Reference Library (New York: Doubleday, 1997), discussion of the Papias testimony on Matthew. Brown notes two genuine ambiguities in the fragment: Hebrew dialect could mean Hebrew proper, Aramaic (the vernacular Semitic language actually spoken in first-century Judea and Galilee), or simply a Hebraic style of Greek; and logia (sayings) could denote a bare collection of sayings or, since Papias uses closely parallel language to describe Mark's Gospel elsewhere in the same fragment, a full narrative gospel comparable to what survives. Brown argues the second possibility, a full gospel-type work, is the more likely sense of logia given that parallel, but the ambiguity itself remains genuinely unresolved in the scholarship, not a settled point either way." }
'markan-priority-synoptic-problem' = @{ title="Why the direction of copying is not in serious doubt"; body="Robert H. Stein, The Synoptic Problem: An Introduction (Grand Rapids, MI: Baker Book House, 1987); see also the revised edition, Studying the Synoptic Gospels: Origin and Interpretation (Grand Rapids, MI: Baker Academic, 2001). Stein lays out the standard case for Markan priority, the position that Mark was written first and used as a source by both Matthew and Luke, built on close verbal agreement in shared material, Matthew's and Luke's shared order following Mark's order even where they diverge from each other, and Matthew's and Luke's tendency to improve Mark's rougher Greek rather than the reverse. This is the majority position within mainstream source criticism and the basis for treating Matthew's Gospel as a document built on Mark's, rather than an independent eyewitness composition." }
'davies-allison-critical-authorship' = @{ title="The critical case: an unknown later author, writing in Matthew's name"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 1 (Matthew 1-7) (Edinburgh: T&T Clark, 1988), Introduction, section on authorship. Davies and Allison conclude that the Gospel's close, sustained verbal dependence on Mark, a source an eyewitness apostle would have had far less reason to lean on so heavily, together with the Papias tradition's poor fit to the finished Greek composition, makes authorship by the tax collector called in this chapter unlikely; the mainstream critical position instead holds the Gospel was written by an unknown, probably Jewish-Christian author, working from Mark plus additional material, in the last decades of the first century CE, within an apostolic tradition associated with Matthew's name rather than by Matthew's own hand." }
'conservative-matthean-authorship-defense' = @{ title="The traditional case, still argued"; body="D. A. Carson and Douglas J. Moo, An Introduction to the New Testament, 2nd ed. (Grand Rapids: Zondervan, 2005), chapter on the Gospel of Matthew, discussion of authorship. Carson and Moo argue the Papias fragment, however imperfectly it fits the finished Greek Gospel, remains the earliest and only surviving external testimony naming an author at all, and defend a mediating position: an authentic core of sayings originally collected by the apostle Matthew, later translated, expanded, and supplemented with Mark's narrative material by a subsequent editor, which would preserve a real apostolic connection without requiring the finished Gospel to be Matthew's own unassisted, word-for-word composition. This traditional and evangelical position remains a minority view relative to the weight of mainstream critical scholarship." }
'pseudonymous-attribution-ancient-convention' = @{ title="Naming a work after its tradition, not always its author"; body="Bruce M. Metzger, Literary Forgeries and Canonical Pseudepigrapha, Journal of Biblical Literature 91, no. 1 (1972): 3-24. Metzger's survey of ancient Mediterranean, Jewish, and Christian practice finds attitudes toward writing under a revered figure's name were genuinely mixed in antiquity, accepted in some school or discipleship-tradition contexts as an honest way of carrying forward a teacher's authority, and condemned as forgery in others, particularly when the attribution was used to claim false authority for a rival or later teaching. That range means the claim this Gospel was written in Matthew's name and tradition, rather than by Matthew himself, is not automatically an accusation of deception, but it is not automatically innocent either; which category a given case falls into has to be argued for, not assumed." }
'tax-collectors-ritual-social-status' = @{ title="A boundary marker, not just an economic grievance"; body="John R. Donahue, Tax Collectors and Sinners: An Attempt at Identification, Catholic Biblical Quarterly 33 (1971): 39-61. Donahue's study of the fixed phrase tax collectors and sinners in the Synoptic tradition argues the pairing reflects more than unpopularity over overcollection: because the job required routine handling of Gentile coinage and regular commercial dealing with Gentiles on Gentile terms, tax collectors were widely treated in the period's literature as functionally assimilated toward Gentile status, an exclusion closer to social and religious boundary-marking than to moral distaste alone." }
'tax-collectors-sanders-qualification' = @{ title="A qualifying view: sinners meant the wicked, not the merely excluded"; body="E. P. Sanders, Jesus and Judaism (Philadelphia: Fortress Press, 1985), discussion of Jesus and the wicked. Sanders offers a distinct, qualifying reading of the same phrase, arguing sinners in the Gospels denoted habitual, unrepentant violators of the law, people functioning as the wicked in a moral rather than a purely social-boundary sense, and that the real offense contemporaries saw in this scene was Jesus offering such people a place without first requiring repentance. Donahue's social-boundary reading and Sanders's moral-category reading are not mutually exclusive, but they weight the offense differently, and both remain live positions in the scholarship on this exact verse." }
'hosea-6-6-original-context' = @{ title="Hosea 6:6 in its own mouth"; body="Francis I. Andersen and David Noel Freedman, Hosea: A New Translation with Introduction and Commentary, Anchor Bible vol. 24 (Garden City, NY: Doubleday, 1980), commentary ad loc. Hosea 6:6. In its eighth-century-BCE setting, I desire mercy, and not sacrifice, and the knowledge of God more than burnt offerings is part of a larger prophetic indictment of Israel and Judah for maintaining formal cultic sacrifice while abandoning covenant faithfulness (mercy, Hebrew hesed) toward God and neighbor. Andersen and Freedman read the line as comparative rather than absolute, valuing covenant loyalty over ritual when the two are in tension, not abolishing sacrifice outright." }
'hosea-double-citation-matthew' = @{ title="The only Gospel to use this verse, and it uses it twice"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary (Edinburgh: T&T Clark, 1988-1997), commentary ad loc. Matthew 9:13 and 12:7. Matthew is the only Gospel to quote Hosea 6:6, and it does so twice, once in this chapter defending table-fellowship with tax collectors and sinners, once again in chapter 12 defending the disciples plucking grain on the Sabbath. Davies and Allison note the repetition as a recognizable feature of this author's own theological interests rather than an incidental coincidence, a lens this particular evangelist returns to for defending Jesus's disputed conduct." }
'leviticus-15-zavah-impurity' = @{ title="A twelve-year state the law itself defines"; body="Leviticus 15:25-30; commentary in Jacob Milgrom, Leviticus 1-16: A New Translation with Introduction and Commentary, Anchor Bible vol. 3 (New York: Doubleday, 1991), commentary ad loc. Leviticus 15:25-30. The Levitical purity code classifies a woman experiencing genital bleeding outside her normal menstrual period (zavah) as ritually impure for the entire duration of the discharge, not merely during a fixed monthly window, and states that anyone or anything she touches during that time contracts the same impurity. Milgrom situates this within the Priestly purity system's broader logic, in which impurity is a state requiring management rather than a moral failing, but a state nonetheless treated as genuinely transmissible by contact." }
'niddah-tractate-mishnah' = @{ title="An entire tractate, later, on exactly this kind of impurity"; body="Mishnah, tractate Niddah, in Herbert Danby, trans., The Mishnah (Oxford: Clarendon Press, 1933). The Mishnah, compiled around 200 CE, over a century after the events this chapter narrates, devotes an entire tractate to menstrual and genital-flow purity law, elaborating and debating the Levitical rules in far greater procedural detail than Leviticus itself supplies. The tractate postdates the Gospels by too long a gap to reconstruct exact first-century practice with precision, but its scale shows how seriously and elaborately this specific body of law was maintained across the wider rabbinic tradition, underscoring that the woman's condition in this chapter invoked a real, extensively regulated legal category, not a vague social stigma." }
'matthew-abbreviates-mark-hemorrhage' = @{ title="A story cut down to make room elsewhere"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary (Edinburgh: T&T Clark, 1988-1997), commentary ad loc. Matthew 9:20-22. Matthew's version of the hemorrhaging woman is markedly shorter than Mark's (Mark 5:25-34), dropping Mark's details of Jesus feeling power go out of him, turning to ask who touched him, and the woman's fearful confession before the whole crowd. Davies and Allison read this compression as consistent with Matthew's broader editorial habit across the Gospel of trimming Mark's fuller miracle narratives, freeing space elsewhere in the same section to add the doubled healings this chapter closes with." }
'matthew-doubling-redactional-pattern' = @{ title="Two where Mark had one"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary (Edinburgh: T&T Clark, 1988-1997), commentary ad loc. Matthew 9:27-31 and 9:32-34. Davies and Allison catalog Matthew's recurring compositional habit of doubling a single figure found in Mark's parallel account into two: Matthew's two blind men healed near Jericho (20:29-34) against Mark's one named blind beggar, Bartimaeus (Mark 10:46-52); Matthew's two demon-possessed men at Gadara (8:28) against Mark's one (Mark 5:1-20); and, within this chapter, two blind men (9:27-31) and a separate mute demoniac (9:32-34), where Mark's own parallel material is comparatively simpler and less doubled. Davies and Allison survey nine proposed explanations for the pattern without settling on a single one, but treat the doubling itself as a well-established, recurring redactional habit across the Gospel, not a one-off coincidence." }
'matthew-doubling-bartimaeus-named' = @{ title="A name Matthew drops"; body="Robert H. Gundry, Matthew: A Commentary on His Literary and Theological Art (Grand Rapids: Eerdmans, 1982), commentary ad loc. Matthew 20:29-34. Gundry notes that where Mark names the blind beggar healed at Jericho, Bartimaeus, son of Timaeus (Mark 10:46), Matthew's doubled, parallel version supplies two unnamed blind men instead, part of a broader pattern in which Matthew regularly drops individualizing names and details Mark provides when converting a single Marcan figure into a matched pair. The habit is a compositional fingerprint, not a claim about how many people were healed on any specific occasion." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Matthew's own calling in this chapter — "Follow me. And he got up and followed him" (9:9) — is also the natural place to take up a question this book has deferred since chapter one: is this Gospel actually his?

The earliest surviving external testimony on the question predates any Church-wide title page. Papias, an early-second-century bishop of Hierapolis whose own writings do not otherwise survive, is quoted by the fourth-century historian Eusebius of Caesarea as reporting that "Matthew collected the sayings in the Hebrew dialect, and each one interpreted them as best he could" [[NOTE:papias-matthew-testimony]]. That fragment is genuinely ambiguous even on its own terms — "Hebrew dialect" could mean Hebrew proper, the vernacular Aramaic actually spoken in Galilee, or simply a Hebraic style of Greek, and "sayings" (logia) could mean a bare collection of sayings or, by comparison with how Papias describes Mark elsewhere in the same fragment, a full narrative gospel [[NOTE:papias-hebrew-dialect-ambiguity]].

Whatever Papias meant, it describes something that does not match the Gospel that actually survives under Matthew's name. Mainstream source criticism holds that Mark was written first and that Matthew's Gospel follows Mark's own Greek narrative closely enough, in wording and in order, to be a document built on Mark as a written source — not an independent eyewitness account, and not a translation of a separate Hebrew or Aramaic sayings collection into Greek [[NOTE:markan-priority-synoptic-problem]]. That dependency, combined with the poor fit between Papias's description and the finished text, is why the weight of mainstream critical scholarship — represented by Davies and Allison's standard commentary — holds that this Gospel was almost certainly not written by the tax collector called in this very chapter, but by a later, unknown author, working from Mark plus additional material, probably in the last decades of the first century CE, within an apostolic tradition carrying Matthew's name rather than by Matthew's own hand [[NOTE:davies-allison-critical-authorship]].
'@

$beat2 = @'
That is not the only position in the field, and it is worth stating the traditional case fairly rather than dismissing it. More conservative and evangelical scholarship argues the Papias tradition, however imperfectly it fits the finished Greek Gospel, remains the earliest and only surviving external testimony naming an author at all, and defends a mediating possibility: an authentic core of sayings originally gathered by the apostle Matthew, later translated, expanded, and supplemented with Mark's narrative material by a subsequent editor — which would preserve a real apostolic connection to the man called in this chapter without requiring the finished Gospel to be his own unassisted, word-for-word composition [[NOTE:conservative-matthean-authorship-defense]]. That position remains a minority one relative to the weight of mainstream critical scholarship, but it is not a fringe position, and it engages the same evidence rather than ignoring it.

Attaching a work to the name of the tradition's founding authority, rather than to whoever actually held the pen, was also not automatically a deceptive act by ancient standards — nor was it automatically an innocent one. The ancient evidence on this exact question is mixed: some school and discipleship traditions treated writing in a teacher's name as an honest way of carrying forward that teacher's authority, while other cases were recognized even in antiquity as forgery, particularly when the attribution was being used to claim authority the actual author didn't have [[NOTE:pseudonymous-attribution-ancient-convention]]. Both readings of this Gospel's authorship agree on the same underlying facts: it is written in fluent, literary Greek, it draws extensively and demonstrably on Mark, and it never once names its own author inside the text — "Matthew" is a title supplied by later Church tradition, not a first-person claim the Gospel makes about itself. Where the two positions genuinely part ways is how much weight to put on the Papias fragment against those facts, and on that question the critical majority and the traditional minority are unlikely to be reconciled by any evidence currently on hand.
'@

$beat3 = @'
The criticism Matthew draws for eating with "tax collectors and sinners" (9:10-11) rests on more than the tax-farming economics already covered earlier in this chapter. A close study of the fixed phrase across the Synoptic tradition argues the pairing reflects a boundary drawn tighter than simple unpopularity: because the job required routine handling of Gentile coinage and regular commercial dealing with Gentiles on Gentile terms, tax collectors were widely treated in the period's literature as functionally assimilated toward Gentile status, an exclusion closer to social and religious boundary-marking than to moral distaste alone [[NOTE:tax-collectors-ritual-social-status]]. A different, qualifying reading of the same phrase argues "sinners" more precisely denoted habitual, unrepentant violators of the law — people functioning as "the wicked" in a moral sense — and that the real offense contemporaries saw in this scene was Jesus offering such people a place without first requiring repentance [[NOTE:tax-collectors-sanders-qualification]]. The two readings are not mutually exclusive, but they weight the scandal differently, and both remain live positions on this exact verse.

Jesus's own answer (9:13) quotes scripture directly: "Go and learn what this means, 'I desire mercy, and not sacrifice.'" That line is Hosea 6:6, and it is quoted to its original sense, not against it. In its own eighth-century-BCE setting, the verse sits inside a larger prophetic indictment of Israel and Judah for maintaining formal cultic sacrifice while abandoning covenant faithfulness — mercy, Hebrew hesed — toward God and neighbor; read comparatively rather than absolutely, it values covenant loyalty over ritual when the two are in tension, not sacrifice abolished outright [[NOTE:hosea-6-6-original-context]]. Applying that line to a dispute over table-fellowship rather than temple sacrifice extends its sense but doesn't distort it — and the extension is distinctly this evangelist's own habit: Matthew is the only Gospel to cite Hosea 6:6 at all, and does so twice, once here and again defending a different disputed practice in chapter 12 [[NOTE:hosea-double-citation-matthew]].
'@

$beat4 = @'
The woman healed of a twelve-year hemorrhage on the way to the ruler's house (9:20-22) gets a brief, compressed scene in Matthew's telling — noticeably shorter than Mark's fuller version, which includes Jesus feeling power leave him, turning to ask who touched him, and the woman's frightened public confession; Matthew drops all of that, consistent with this Gospel's broader habit of trimming Mark's longer miracle scenes to make room elsewhere in the same stretch of narrative for the doubled healings the chapter closes with [[NOTE:matthew-abbreviates-mark-hemorrhage]].

What the compression leaves unstated is the purity law her condition and her action would have invoked for a contemporary audience. Leviticus 15:25-30 classifies a woman with genital bleeding outside her normal monthly period as ritually impure for the entire duration of the discharge, not merely during a fixed monthly window, and states that anyone or anything she touches during that time contracts the same impurity [[NOTE:leviticus-15-zavah-impurity]]. Twelve years under that classification is not a minor inconvenience; it is over a decade of managed exclusion from ordinary contact. The later Mishnah devotes an entire tractate, Niddah, to exactly this category of impurity, elaborating the underlying Levitical rule in far more procedural detail than Leviticus itself supplies — a text too late to reconstruct first-century practice with precision, but proof of how extensively and seriously this specific body of law was maintained across the wider tradition [[NOTE:niddah-tractate-mishnah]].

Read against that law, reaching into a crowd to touch a rabbi's garment is not a small gesture. Under a strict reading of Leviticus 15, she risks passing impurity to Jesus and to everyone she brushes past on the way to him — a real, checkable social and religious risk this book can name precisely even though Matthew's own compressed telling leaves it as an unstated backdrop rather than a dramatized plot point.
'@

$beat5 = @'
Two more details in this chapter's back half are worth flagging as a matter of literary composition, not historical claim. Two blind men are healed together here (9:27-31), and a mute demoniac is healed separately (9:32-34); both scenes recur in doubled form again later in Matthew — two blind men healed near Jericho (20:29-34), and, earlier in the Gospel, two demon-possessed men at Gadara (8:28). In each case, Mark's parallel account has only one figure where Matthew has two: a single named blind beggar, Bartimaeus, at Jericho (Mark 10:46-52), and a single demoniac at Gadara/Gerasa (Mark 5:1-20).

Source-critical scholarship has long catalogued this as a recurring feature of Matthew's compositional method relative to the source he is independently shown, by verbal agreement, to be using. Davies and Allison's commentary surveys nine separate proposed explanations for the doubling without settling on one, but treats the pattern itself as a well-established, recurring redactional habit across the Gospel rather than a one-off coincidence [[NOTE:matthew-doubling-redactional-pattern]]. The habit extends to names as well as numbers: where Mark supplies Bartimaeus by name, Matthew's doubled, parallel version gives two unnamed men instead, part of the same broader tendency to drop Mark's individualizing detail when converting a single Marcan figure into a matched pair [[NOTE:matthew-doubling-bartimaeus-named]]. That observation is strictly about Matthew's method of composing relative to a source he is shown to have used — it says nothing, one way or the other, about how many people were actually present at any specific healing.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'GOSPEL AUTHORSHIP (TRADITIONAL ATTRIBUTION VS. CRITICAL SCHOLARSHIP)' = "The question of who actually wrote the Gospel bearing Matthew's name, raised directly by his calling in this chapter (9:9; see MATTHEW (LEVI)). The earliest external testimony, a fragment of Papias of Hierapolis preserved by Eusebius of Caesarea, describes Matthew compiling sayings ``in the Hebrew dialect'' [[NOTE:papias-matthew-testimony]], a description that does not match the finished Gospel's polished, Mark-dependent Greek composition [[NOTE:markan-priority-synoptic-problem]]. Mainstream critical scholarship concludes the Gospel was written by a later, unknown author working in Matthew's name and tradition, not by the apostle himself [[NOTE:davies-allison-critical-authorship]]; a smaller traditional and evangelical position argues for an authentic apostolic core beneath the finished text [[NOTE:conservative-matthean-authorship-defense]]. See PAPIAS OF HIERAPOLIS."
'PAPIAS OF HIERAPOLIS' = "An early-second-century bishop whose own writings do not survive but are quoted in fragments by the fourth-century historian Eusebius of Caesarea. His testimony that Matthew ``collected the sayings in the Hebrew dialect'' is the earliest surviving external claim connecting the name Matthew to any written work, and the starting point — and central complication — for the authorship debate over this Gospel [[NOTE:papias-matthew-testimony]]; the fragment's own key terms are themselves disputed among scholars [[NOTE:papias-hebrew-dialect-ambiguity]]. See GOSPEL AUTHORSHIP (TRADITIONAL ATTRIBUTION VS. CRITICAL SCHOLARSHIP)."
'TAX COLLECTORS (TELONAI) IN ROMAN JUDEA' = "Local toll and tax collectors, called telonai in Greek, who purchased collection contracts from Roman or, in Galilee specifically, Herodian authorities and recouped their investment, plus profit, by collecting above the contracted rate from their own communities. Beyond that economic resentment, tax collectors were also widely treated in the period's literature as functionally assimilated toward Gentile status, since the work required routine handling of Gentile coinage and regular commercial dealing with Gentiles on Gentile terms — a boundary-marking exclusion distinct from unpopularity alone [[NOTE:tax-collectors-ritual-social-status]], though some scholars weight the underlying offense differently, as a moral rather than a purely social-boundary category [[NOTE:tax-collectors-sanders-qualification]]. This is the social world invoked when Matthew draws criticism for eating with ``tax collectors and sinners'' after his calling (9:9-11; see MATTHEW (LEVI))."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum — $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats (appended after existing Ch9 beats) ----
$sortKey = [double]1000
if ($maxCh9SortKey -ge $sortKey) { $sortKey = $maxCh9SortKey }
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch9NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert glossary entries ----
foreach ($heading in $glossary.Keys) {
    $body = $glossary[$heading]
    foreach ($slug in $slugToNumber.Keys) {
        $body = $body.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $text = "$heading`n`n$body"
    $id = New-BeatRow $text
    $maxGlossarySortKey += 50
    Add-BeatNode $GlossaryNodeId $id $maxGlossarySortKey
}
Write-Host "Inserted $($glossary.Count) glossary entries"

# ---- Seed new entities ----
Seed-Entity "Robert H. Stein" "robert-h-stein" "character" "New Testament scholar known for standard treatments of the Synoptic Problem and Markan priority, the case that Mark was written first and used as a source by Matthew and Luke."
Seed-Entity "E. P. Sanders" "e-p-sanders" "character" "Historical-Jesus scholar whose Jesus and Judaism (1985) argues sinners in the Gospels denoted habitual, unrepentant violators of the law rather than the merely morally imperfect."
Seed-Entity "Herbert Danby" "herbert-danby" "character" "Translator of the standard English edition of the Mishnah (Oxford, 1933), including tractate Niddah's menstrual and genital-flow purity law."
Seed-Entity "Douglas J. Moo" "douglas-j-moo" "character" "New Testament scholar, co-author with D. A. Carson of a standard evangelical New Testament introduction defending a traditional apostolic core to Matthew's authorship."
Seed-Entity "Robert H. Gundry" "robert-h-gundry" "character" "New Testament scholar and commentator on Matthew's redactional habits, including the Gospel's tendency to drop names Mark supplies when doubling single figures into matched pairs."
Seed-Entity "Francis I. Andersen" "francis-i-andersen" "character" "Co-author, with David Noel Freedman, of the Anchor Bible commentary on Hosea."
Seed-Entity "David Noel Freedman" "david-noel-freedman" "character" "Co-author, with Francis I. Andersen, of the Anchor Bible commentary on Hosea, including its reading of Hosea 6:6 as valuing covenant faithfulness over ritual sacrifice."
Seed-Entity "John R. Donahue" "john-r-donahue" "character" "New Testament scholar whose 1971 Catholic Biblical Quarterly article examined the identity and social status implied by the Synoptic phrase tax collectors and sinners."

$conn.Close()
Write-Host "DONE Matthew Chapter 9 depth pass."
