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
$Ch14NodeId = [guid]"019FA06C-2D81-7D7E-87EE-BC3BA620B663"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06C-2D81-7D7E-87EE-BC3BA620B663' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'leviticus-incest-law-herodias' = @{ title="The Levitical law behind John's charge"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, Volume II: Commentary on Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 14:3-4. Davies and Allison identify the specific legal basis for John's public condemnation of Herod Antipas's marriage as Leviticus 18:16 and 20:21, both of which forbid a man from marrying his brother's wife while that brother is still living — a prohibition genuinely distinct from levirate marriage, which required exactly the opposite when a brother died childless. Herodias's first husband was, per Josephus, still alive when she left him for Antipas, making the marriage a real, checkable violation of Torah law rather than a vague charge of general immorality." }
'hoehner-herodian-genealogy' = @{ title="Untangling the Herod family's reused names"; body="Harold W. Hoehner, Herod Antipas, Society for New Testament Studies Monograph Series 17 (Cambridge: Cambridge University Press, 1972), chapter on Herodias and her marriages. Hoehner's standard scholarly reconstruction of the Herodian family tree lays out the confusion at the root of this story: Herod the Great had multiple sons by different wives who carried the names Herod and Philip, sometimes both, and the Gospels' single word Philip for Herodias's first husband conflates two distinct half-brothers of Herod Antipas — Philip the tetrarch of Ituraea and Trachonitis, already established elsewhere in this book, and a different, non-ruling son by Mariamne II, whom modern historians call Herod II to keep the two men straight." }
'josephus-herod-ii-first-husband' = @{ title="Josephus names Herodias's actual first husband"; body="Flavius Josephus, Jewish Antiquities 18.5.1, section 109 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press, 1965). Josephus, independently of the Gospels, identifies Herodias's first husband not as Philip the tetrarch but as a son of Herod the Great by Mariamne II, daughter of the high priest Simon Boethus — a man Josephus calls simply Herod, never Philip, and never assigns any tetrarchy or territorial title." }
'matthew-14-3-philip-manuscript-variant' = @{ title="A name some manuscripts drop entirely"; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece, ad loc. Matthew 14:3 and its Markan parallel, Mark 6:17. A number of early and significant manuscripts omit the name Philip from this verse altogether, identifying Herodias's first husband only as his brother, without supplying a personal name; the fuller reading naming Philip is well attested but not universal, a manuscript-level reminder that the identification difficulty predates modern scholarship." }
'josephus-antipas-fear-of-unrest' = @{ title="Josephus's own, different reason for John's death"; body="Flavius Josephus, Jewish Antiquities 18.5.2, sections 116-119 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press, 1965). Josephus states plainly that Antipas, alarmed by the size of the crowds John's preaching drew and fearing the great crowds that seemed ready to do anything he should advise might tip into rebellion, had John arrested and killed at Machaerus as a preemptive political measure — a stated motive of calculated public-order risk, with no mention anywhere in Josephus's account of a birthday banquet, a dance, or an oath." }
'meier-marginal-jew-john-motive' = @{ title="Two independent traditions, two different reasons"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume Two: Mentor, Message, and Miracles (New York: Doubleday, 1994), chapter on John the Baptist's death. Meier's standard historical-critical treatment weighs the Gospel banquet story against Josephus's political-threat account and concludes the two may be capturing different, non-exclusive layers of the same real event — private court intrigue and public political calculation are not mutually exclusive motives for the same execution — while noting that only Josephus's version is corroborated by an independent, non-Christian witness with no theological stake in how John died." }
'josephus-salome-named-18-136' = @{ title="Where the name Salome actually comes from"; body="Flavius Josephus, Jewish Antiquities 18.5.4, section 136 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press, 1965). Josephus names Herodias's daughter by her first marriage as Salome, recording her later marriages — first to her step-great-uncle Philip the tetrarch, and after his death to Aristobulus of Chalcis — in a passage about the wider Herodian family that never once mentions John the Baptist, Herod Antipas's banquet, or a dance." }
'gospel-silent-on-name-france' = @{ title="A name the Gospel text itself never supplies"; body="R.T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 2007), commentary ad loc. Matthew 14:6. France notes explicitly that Matthew, like Mark, identifies the dancer only as the daughter of Herodias, never by name; the identification with Josephus's Salome is a later inference built by cross-referencing two passages in Josephus that do not touch each other in his own text, not a reading available from the Gospel's own wording." }
'luz-matthew-women-children-clause' = @{ title="A headcount Matthew alone qualifies"; body="Ulrich Luz, Matthew 8-20: A Commentary, Hermeneia series, trans. James E. Crouch (Minneapolis: Fortress Press, 2001), commentary ad loc. Matthew 14:21. Luz notes that Matthew's summary — about five thousand men, besides women and children — is Matthew's own addition to the shared feeding tradition; Mark's parallel (6:44) and Luke's parallel (9:14) both simply state the number of men (andres) without qualification, making Matthew's explicit acknowledgment of a broader, mixed crowd a small but real point of variance in how the three Synoptic accounts describe the same headcount." }
'davies-allison-matthew-counting-convention' = @{ title="Counting only the men, and Matthew flagging it"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, Volume II: Commentary on Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 14:21. Davies and Allison identify the underlying Greek andres as reflecting an ordinary ancient counting convention — tallying adult men as the unit of a crowd's size — and treat Matthew's added clause naming women and children as present but uncounted as a distinctly Matthean editorial gloss on that convention, absent from Mark's and Luke's versions of the same verse." }
'davies-allison-theophany-ot-background' = @{ title="Job's God, walking on the sea"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, Volume II: Commentary on Matthew VIII-XVIII, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 14:25-27. Davies and Allison read Job 9:8's description of God alone treading on the waves of the sea as the single most important Old Testament background text for this scene, part of a wider biblical convention — echoed also in Psalm 77:19 and Isaiah 43:16 — in which mastery over the sea is treated as an act reserved for God, framing Jesus's walking on the water as a theological claim rather than simply an impressive feat." }
'ane-chaoskampf-water-mastery' = @{ title="An older mythological pattern behind the water"; body="Adela Yarbro Collins, 'Rulers, Divine Men, and Walking on the Water (Mark 6:45-52),' in Religious Propaganda and Missionary Competition in the New Testament World: Essays Honoring Dieter Georgi, ed. Lukas Bormann, Kelly Del Tredici, and Angela Standhartinger (Leiden: Brill, 1994). Collins situates the walking-on-water scene within a much older ancient Near Eastern mythological pattern — a storm or sky deity asserting mastery over a chaotic, personified sea, with Baal's defeat of the sea-god Yamm in the Ugaritic Baal Cycle as the best-known version — arguing that the Gospel scene draws on this deep well of water-mastery imagery to make an implicit claim about Jesus's own divine status, a background that applies to Matthew's own retelling of the shared tradition as much as to Mark's." }
'france-peter-sinking-matthean-addition' = @{ title="Peter's attempt: Matthew's own addition to the scene"; body="R.T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 2007), commentary ad loc. Matthew 14:28-31. France identifies Peter's request to come to Jesus on the water, his brief success, his fear-driven sinking, and Jesus catching him as material found only in Matthew's account of this episode — absent from Mark's and John's versions of the same night on the sea — and reads it as a deliberately shaped illustration of faith overwhelmed by the natural human perspective of visible danger, consistent with Matthew's broader interest across the Gospel in Peter as a representative, imperfect disciple." }
'luke-great-omission-streeter' = @{ title="Why Luke has no walking-on-water scene at all"; body="B.H. Streeter, The Four Gospels: A Study of Origins (London: Macmillan, 1924), discussion of Luke's omission of Mark 6:45-8:26. Streeter's classic source-critical study catalogs the block of Markan material — including the entire walking-on-water episode — that Luke's Gospel skips over entirely between the feeding of the five thousand and Peter's confession at Caesarea Philippi, a gap later scholarship nicknamed Luke's Great Omission. Whatever the reason (a damaged source manuscript, deliberate trimming of doublets, or other editorial judgment remain live proposals), the plain result is that Luke's version of this story has no walking-on-water scene to include a Peter subplot in at all — only Matthew supplies one." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
John's public objection to Herod Antipas's marriage was not a vague charge of general immorality — it had a specific, checkable legal basis. Leviticus twice forbids a man from marrying his brother's wife while that brother is still living (18:16, 20:21), a prohibition genuinely distinct from levirate marriage, which required exactly the opposite when a brother died without an heir [[NOTE:leviticus-incest-law-herodias]]. Matthew's own text (14:3) names the brother Herodias left as "Philip," and that single word sits on top of one of the more genuinely confusing tangles in the entire Herodian family tree — one worth untangling precisely rather than passing over.

Herod the Great had multiple sons by different wives, and the family kept reusing the names "Herod" and "Philip" across half-brothers who otherwise had nothing to do with one another [[NOTE:hoehner-herodian-genealogy]]. One of them — Philip the tetrarch of Ituraea and Trachonitis, already established earlier in this book — ruled a real, separate territory northeast of Galilee for decades, and is not in dispute. Herodias's actual first husband was a different son entirely: Josephus, independently of the Gospels, names him simply "Herod," a son of Herod the Great by Mariamne II, daughter of the high priest Simon Boethus, who never held a tetrarchy or any territorial title at all [[NOTE:josephus-herod-ii-first-husband]] — the man modern historians call Herod II purely to keep him distinct from his better-documented half-brother. Most historians read Matthew's "Philip" here as a conflation of these two half-brothers, rather than evidence the tetrarch himself was ever married to Herodias. It is a small mercy that the manuscript tradition itself preserves some memory of the difficulty: a number of early and significant manuscripts of both Matthew 14:3 and its Markan parallel omit the name "Philip" altogether, naming Herodias's first husband only as "his brother," without supplying a personal name at all [[NOTE:matthew-14-3-philip-manuscript-variant]].
'@

$beat2 = @'
Matthew's own account of what happens next — the birthday banquet, the dance, the rash oath, Herodias's prompted request — is vivid, specific, and entirely a private-court scene no outside source could ever confirm or deny in its particulars. But that John was arrested, imprisoned, and executed by Herod Antipas is independently confirmed outside the Gospels, by the first-century Jewish historian Flavius Josephus, writing decades later with no stake in the Christian movement's account of events.

Josephus places the imprisonment and execution at Machaerus, the Herodian fortress on the Dead Sea's eastern shore already established in this book's discussion of chapter eleven — a site whose Herodian royal-palace character is now confirmed on the ground by a Hungarian excavation and reconstruction project running since 2009 [199]. But Josephus gives a genuinely different reason for the killing than the Gospels do. In his telling, Antipas grew alarmed at the sheer size of the crowds John's preaching was drawing, and feared that "the great crowds that seemed ready to do anything he should advise" might tip into open rebellion against Roman-backed rule; rather than risk an uprising, Antipas struck first, having John arrested and killed as a calculated political measure — with no banquet, no dance, and no oath anywhere in Josephus's account [[NOTE:josephus-antipas-fear-of-unrest]].

This is a real, worth-treating-honestly divergence between two independent traditions about the same killing, not a contradiction this book needs to force into agreement. Both sources agree Antipas had John executed; they disagree entirely on why. Mainstream historical-critical scholarship generally reads the two motives as non-exclusive rather than as competing claims where only one can win — private grievance and public political calculation can both be true of the same decision — while noting plainly that only Josephus's version carries independent, non-Christian corroboration [[NOTE:meier-marginal-jew-john-motive]].
'@

$beat3 = @'
The girl whose dance triggers John's execution is never named by Matthew, and Mark's parallel account does not name her either — both Gospels call her only "the daughter of Herodias" (14:6). The name almost universally attached to her today, Salome, comes entirely from a separate source: Josephus, describing the wider Herodian family, records that Herodias's daughter by her first marriage was named Salome, and goes on to describe her own later marriages — first to her step-great-uncle Philip the tetrarch, and after his death to Aristobulus of Chalcis — in a passage that never once mentions John the Baptist, a banquet, or a dance [[NOTE:josephus-salome-named-18-136]].

That is worth being precise about, because it is a good small case study in how a very plausible identification can still be an inference rather than a direct statement of either source. Neither Gospel names the dancer; Josephus names a Salome but never connects her to this specific banquet story himself. The link between the two is built entirely by later readers cross-referencing two passages of Josephus that do not touch each other in his own text, then attaching that name to the unnamed Gospel figure [[NOTE:gospel-silent-on-name-france]]. The identification is very likely correct — Josephus is describing the same family in the same period, and no rival candidate for Herodias's daughter exists in any source — but "very likely correct" and "stated directly in the text" are two different things, and it is the second claim the Gospels themselves never make.
'@

$beat4 = @'
The numbers in the feeding story are specific enough to repeat exactly: five loaves, two fish, a crowd fed to satisfaction, twelve baskets of fragments left over (14:17-20). This book's discussion of the same event in John's Gospel, chapter six, already lays out in depth why the feeding of the five thousand carries unusual evidentiary weight — it is the one miracle story, apart from the resurrection appearances, independently attested in all four canonical Gospels — and that discussion is not worth repeating here in full.

What is worth adding, because it is specific to Matthew's own version of the shared tradition, is a small variance in how the crowd's size gets described. Matthew's closing headcount reads "about five thousand men, besides women and children" (14:21), and that qualifying clause is Matthew's own addition to the tradition. Mark's parallel (6:44) and Luke's parallel (9:14) both simply state the number of men (andres), the ordinary ancient convention for tallying a crowd's size, without any acknowledgment that women and children were present too [[NOTE:luz-matthew-women-children-clause]]. Matthew alone flags the convention he is using even while following it [[NOTE:davies-allison-matthew-counting-convention]]. It is a small textual variance, not a discrepancy that needs resolving — all three writers are almost certainly describing the same crowd — but it is a real, checkable difference in how carefully each Synoptic writer chose to qualify the number he reported.
'@

$beat5 = @'
Jesus walking on the sea at night (14:25) draws on a background considerably older than the Gospels themselves. Job 9:8 says God alone "trampled the waves of the Sea" — in the Hebrew scriptural imagination, mastery over the sea's chaos is not a generic display of power, it is something reserved for God [[NOTE:davies-allison-theophany-ot-background]]. That biblical line sits inside a much wider ancient Near Eastern mythological pattern: a storm or sky deity asserting control over a chaotic, personified sea, the best-known version being Baal's defeat of the sea-god Yamm in the Ugaritic Baal Cycle. Scholarship on the Gospel water-walking scenes reads this deep well of water-mastery imagery as the real literary and theological background against which a first-century audience would have heard this episode — an implicit claim about who is doing the walking, not just an impressive feat [[NOTE:ane-chaoskampf-water-mastery]].

One piece of this scene is unique to Matthew among the Synoptic accounts, and worth flagging precisely: Peter's own attempt to walk out to Jesus, his brief success, his fear-driven sinking, and Jesus catching him and asking why he doubted (14:28-31). Mark's version of this same night on the water has no Peter subplot at all — only Jesus walks — and Luke's Gospel does not include the walking-on-water episode in any form, having skipped this entire block of shared material between the feeding and Peter's confession at Caesarea Philippi, a gap source critics call Luke's "Great Omission" [[NOTE:luke-great-omission-streeter]]. Peter's attempt and rescue is Matthew's own addition to the inherited scene, read by mainstream commentary as a deliberately shaped illustration of faith overwhelmed by the visible danger right in front of it — consistent with Matthew's broader interest across the Gospel in Peter as a representative, imperfect disciple [[NOTE:france-peter-sinking-matthean-addition]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'HEROD II (HERODIAS''S FIRST HUSBAND)' = "The son of Herod the Great and Mariamne II (daughter of the high priest Simon Boethus) whom Herodias actually married first, before leaving him for his half-brother Herod Antipas — named plainly as `"Herod,`" never `"Philip,`" by the independent historian Flavius Josephus [[NOTE:josephus-herod-ii-first-husband]]. Modern historians call him Herod II purely to distinguish him from his father and his many half-brothers who also carried the name Herod; he held no tetrarchy or territorial title of his own, unlike Philip the tetrarch of Ituraea and Trachonitis (see PHILIP (HUSBAND OF HERODIAS)), a genuinely separate figure the Gospels' single word `"Philip`" conflates him with. Harold Hoehner's standard scholarly reconstruction of the Herodian family tree treats this conflation as the likely product of the family's own habit of reusing the names Herod and Philip across half-brothers by different mothers [[NOTE:hoehner-herodian-genealogy]], a confusion the manuscript tradition itself partly reflects: a number of early manuscripts of Matthew 14:3 and its Markan parallel omit the name `"Philip`" altogether [[NOTE:matthew-14-3-philip-manuscript-variant]].

Cited in: Matthew 14:3; see HERODIAS, PHILIP (HUSBAND OF HERODIAS), HEROD ANTIPAS."
'SALOME (TRADITIONAL NAME, NOT IN THE GOSPEL TEXT)' = "The traditional name attached to the unnamed girl of Matthew 14:6-11 (see DAUGHTER OF HERODIAS), supplied entirely by the independent historian Flavius Josephus rather than by either Gospel. Josephus, in a separate passage describing the wider Herodian family, names Herodias's daughter by her first marriage as Salome and records her own later marriages — first to her step-great-uncle Philip the tetrarch, then after his death to Aristobulus of Chalcis — without ever connecting that Salome to John the Baptist's execution himself [[NOTE:josephus-salome-named-18-136]]. Matthew's own text never supplies a name for the dancer, calling her only `"the daughter of Herodias`" throughout; the identification with Josephus's Salome is a later inference, built by cross-referencing two of his passages that do not touch each other in his own text, not a reading available from the Gospel's wording itself [[NOTE:gospel-silent-on-name-france]]. The identification is widely regarded as very likely correct, given that Josephus is describing the same family in the same period with no rival candidate on offer, but it remains an inference layered onto the sources rather than something either text states outright.

Cited in: Matthew 14:6-11; see DAUGHTER OF HERODIAS, HERODIAS."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum - $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats with placeholder replacement ----
$sortKey = $maxChapterSortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch14NodeId $id $sortKey
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
Seed-Entity "Herod II (Herodias's First Husband)" "herod-ii-herodias-first-husband" "character" "Son of Herod the Great by Mariamne II; Herodias's actual first husband per Josephus, Jewish Antiquities 18.109 - distinct from Philip the tetrarch, with whom the Gospels' word Philip is often conflated."

$conn.Close()
Write-Host "DONE Chapter 14 (Matthew)."
