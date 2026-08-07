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
$Ch22NodeId = [guid]"019FA071-9611-73CB-A9C2-8DD1ADEAD70C"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA071-9611-73CB-A9C2-8DD1ADEAD70C' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# NOTE: Matthew's Notes node already carries two orphaned, fully-cited, chapter-22-specific
# notes from an earlier pass that were never wired into the chapter's beats with [[NOTE:]] tags:
#   [46] "The 'render unto Caesar' denarius: 'son of the Divine Augustus' on its face" (cites
#        Meshorer + Hendin on the Tiberius tribute-penny obverse inscription; covers Matt 22:19-21)
#   [47] "Sadducees vs. Pharisees on resurrection: corroborated by Acts and Josephus independently"
#        (cites Acts 23:8, Jewish War 2.8.14, Antiquities 18.1.4; covers Matt 22:23-33)
# This script deliberately does NOT recreate those two notes. It cites them directly by their
# existing fixed numbers ([46], [47]) in the new beats below, and adds six genuinely NEW notes
# covering ground [46]/[47] do not: the coin's reverse imagery, the Herodians as a documented
# faction, the Sadducees'-canon nuance, the Shema/Leviticus-19:18 pairing precedent, and Psalm
# 110's reception history plus its own built-in Hebrew ambiguity. Notes [141] (Hillel's one-foot
# answer) and [145] (Golden Rule convergence, not borrowing) are likewise pre-existing and are
# cross-referenced by number only.

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'herodians-alliance-matthew-22' = @{ title="The Herodians reappear only here: what Matthew kept from Mark, and what he cut"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 22:16; compare Harold W. Hoehner, Herod Antipas, Society for New Testament Studies Monograph Series 17 (Cambridge: Cambridge University Press, 1972), on the identity and composition of 'the Herodians' as a loose network of dynastic loyalists rather than a formally organized sect comparable to the Pharisees or Sadducees. Mark names Pharisees and Herodians together twice: first plotting against Jesus in a Galilean synagogue (Mark 3:6), then again setting this same tax trap in Jerusalem (Mark 12:13). Matthew's parallel to the first scene — his own version of the healing of the man with the withered hand — drops the Herodians outright, reporting only that 'the Pharisees went out and conspired against him, how to destroy him' (Matthew 12:14). The group resurfaces in Matthew's text exactly once: here, at 22:16, following Mark's second occurrence closely. Matthew is not, on this evidence, independently attesting the Herodians as a recurring feature of his own tradition; he is following his Markan source at the one point where Mark still has them, having quietly dropped them earlier and, in a related editorial move, swapped in 'the Sadducees' for Herod when adapting Mark's leaven warning (compare Mark 8:15 with Matthew 16:6, 11-12). That is a real, checkable fact about Matthew's editorial method, independent of whatever the underlying first-century alliance actually was.
Cited in: Matthew (beat covering 22:15-16)." }
'tribute-penny-livia-pax-reverse' = @{ title="The coin's other face: Livia as Pax, under the title Pontifex Maximus"; body="David Hendin, Guide to Biblical Coins, 5th ed. (New York: Amphora, 2010), catalog discussion of the Tiberius 'tribute penny' denarius type, struck primarily at Lugdunum, circa 15-37 CE; see also Ya'akov Meshorer, Ancient Jewish Coinage, 2 vols. (Dix Hills, NY: Amphora Books, 1982), on Roman imperial coinage circulating in Judea in this period. Note 46 above covers the coin's obverse, Tiberius's portrait ringed by his claim to be Divi Filius. The reverse is no less loaded: a seated female figure, generally identified as Tiberius's own mother Livia personified as the goddess Pax, holding a scepter and an olive branch, ringed by a second legend, PONTIF MAXIM — Tiberius's title as Pontifex Maximus, the highest priestly office in the Roman state religion. A single coin, in other words, carried on its two faces both an emperor's minted claim to divine sonship and his mother's portrayal as a goddess of peace, under a legend naming him chief priest of Rome's official cult — the very object a group of religious authorities was asked to produce, examine, and name, in the same breath as an answer separating what belongs to Caesar from what belongs to God.
Cited in: Matthew (beat covering 22:19-21)." }
'sadducees-torah-only-canon-nuance' = @{ title="Did the Sadducees really recognize only the five books of Moses? A later, shakier claim"; body="Gunter Stemberger, Jewish Contemporaries of Jesus: Pharisees, Sadducees, Essenes, trans. Allan W. Mahnke (Minneapolis: Fortress Press, 1995), chapter on the Sadducees. Jesus's resurrection proof-text in this scene is drawn from Exodus 3:6, part of the Torah proper, rather than from a passage that states resurrection more explicitly, such as Daniel 12:2 — a choice some popular commentary explains by appeal to a supposed Sadducean restriction of scriptural authority to the five books of Moses alone. That specific canon-restriction claim, however, is first attested only in the third-century Christian writers Origen and Jerome, not in Josephus or the New Testament itself, both of which describe the Sadducees' resurrection denial without describing any narrower canon [47]. Stemberger and other specialists in the sect's history treat the Pentateuch-only claim as a later, probably mistaken inference from Josephus's own wording — Josephus contrasts written law against oral tradition, not Torah against the rest of scripture — rather than a first-century-attested fact. What is solidly attested, the resurrection denial itself, remains real; the reason often given for Jesus's specific choice of proof-text is not.
Cited in: Matthew (beat covering 22:31-32)." }
'shema-leviticus-pairing-testament-issachar' = @{ title="Pairing the Shema with Leviticus 19:18: a move already available"; body="H. C. Kee, trans., 'Testaments of the Twelve Patriarchs,' in James H. Charlesworth, ed., The Old Testament Pseudepigrapha, Volume 1: Apocalyptic Literature and Testaments (Garden City, NY: Doubleday & Company, 1983), Testament of Issachar 5:2 and 7:6. Issachar's testament instructs, 'Love the Lord and your neighbor' (5:2), and later has its speaker recall, 'The Lord I loved with all my strength; likewise, I loved every human being as I love my children' (7:6) — the same two-part pairing Jesus gives here, love of God first and love of neighbor second, appearing twice within a single document. The Testaments of the Twelve Patriarchs is itself a harder case than Hillel's securely dated one-line summary [141]: mainstream scholarship (see Marinus de Jonge, The Testaments of the Twelve Patriarchs: A Study of Their Text, Composition, and Origin, 2nd ed., Assen: Van Gorcum, 1975) generally reads the document as a Jewish work carrying later Christian interpolations, though a minority view treats the whole text as a second-century Christian composition built on older, now-unrecoverable material, so its precise date relative to Jesus's own lifetime cannot be fixed with confidence. What the passage still shows, on either dating, is that pairing these exact two commandments as a summary of 'the whole Law' was an available, recognized move within the wider Jewish ethical-summary tradition — convergent testimony of the same kind already noted for the Golden Rule [145] — not an invention unique to this scene.
Cited in: Matthew (beat covering 22:34-40)." }
'psalm-110-nt-reception-history' = @{ title="Psalm 110:1's outsized New Testament afterlife"; body="David M. Hay, Glory at the Right Hand: Psalm 110 in Early Christianity, Society of Biblical Literature Monograph Series 18 (Nashville: Abingdon Press, 1973). No Old Testament passage is quoted or clearly alluded to more often across the New Testament than Psalm 110:1: it is directly quoted at Acts 2:34-35, in Peter's own Pentecost speech, and repeatedly through the letter to the Hebrews (1:13, 5:6, 7:17, 7:21, 10:12-13), with further echoes at 1 Corinthians 15:25, Ephesians 1:20, Colossians 3:1, and 1 Peter 3:22 — well beyond its use here and in Mark and Luke's own parallel versions of this exchange (Mark 12:35-37; Luke 20:41-44). Hay's study concludes the verse's popularity among early Christian writers reflects how directly its imagery of enthronement and vindication met a real, felt need to articulate Jesus's exalted status after the resurrection, not merely a convenient proof-text reused out of habit.
Cited in: Matthew (beat covering 22:41-46)." }
'psalm-110-adoni-yhwh-ambiguity' = @{ title="The ambiguity built into the psalm's own Hebrew"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 22:41-46. Psalm 110:1 reads, in the Hebrew, 'The LORD [YHWH] said to my lord [adoni]: Sit at my right hand' — two different words for two different figures. Adoni, elsewhere across the Hebrew Bible, is never used of God himself, only of a human or angelic superior. In the psalm's own original court setting, the verse most naturally reads as a prophet or priest addressing the reigning Davidic king as 'my lord,' relaying God's promise to him; nothing in that original setting requires 'my lord' to mean a future, greater descendant rather than the king actually being addressed. Reading it that way — as a promise about someone still to come, someone who outranks David even as his own descendant — is Jesus's interpretive move laid on top of the psalm's plain original sense, and it is exactly that move the Pharisees cannot answer.
Cited in: Matthew (beat covering 22:41-46)." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders; [46], [47], [141], [145] are pre-existing note numbers) ----
$beat1 = @'
The tax-trap coin deserves a longer look than "a denarius bearing the emperor's portrait" conveys, because the specific coin scholars identify as the likeliest candidate carries more theological freight than a modern reader would guess from the text alone. The best-attested candidate is a silver denarius minted under Tiberius, ringed by TI CAESAR DIVI AVG F AVGVSTVS — "Tiberius Caesar, son of the Divine Augustus, Augustus" — a minted, literal claim to divine sonship, not later flattery [46].

The coin's reverse has drawn less attention in casual retellings but is just as loaded. A seated female figure, generally identified as Tiberius's own mother Livia personified as the goddess Pax, holds a scepter and an olive branch, ringed by a second legend, PONTIF MAXIM — Tiberius's own title as Pontifex Maximus, the highest priestly office in the Roman state religion [[NOTE:tribute-penny-livia-pax-reverse]]. Handed a coin like that and asked whose image and inscription it bears, the men answering "Caesar's" are not stating a neutral fact about currency; they are naming, out loud, an object that identifies its bearer as a god's son on one face [46] and his own mother worshipped as a goddess of peace on the other [[NOTE:tribute-penny-livia-pax-reverse]], in front of the one man in the scene being tested on exactly that kind of claim.

The trap's construction gets sharper once the two factions asking the question are taken seriously as real, distinct groups rather than a generic "religious leaders." The Pharisees were a lay-piety and legal-interpretation movement with a long, documented history of friction against Herodian rule; the Herodians existed specifically to protect that same dynasty's continued position under Rome [[NOTE:herodians-alliance-matthew-22]]. Putting the two in the same room, over the same question, is exactly the kind of pairing that shows up when a target is judged dangerous enough to be worth an alliance of convenience — not an everyday occurrence for either faction.
'@

$beat2 = @'
The alliance named in verse 16 deserves its own closer look, because it is a real, if imprecisely documented, group — and because Matthew's own use of it is a small, checkable fact about how this Gospel was composed, on top of whatever the underlying first-century politics actually were.

Josephus never uses a Greek term that cleanly maps onto "the Herodians" as a formal party name; the closest he comes is a passing reference to partisans "of Herod's own persuasion," describing loyalists of Herod the Great generations before Jesus's ministry. Historians studying Herod Antipas's reign generally read the Gospels' "Herodians" as a loose network of dynastic loyalists with a practical stake in Rome's arrangement with the Herodian house, rather than a formally organized sect comparable to the Pharisees or Sadducees [[NOTE:herodians-alliance-matthew-22]].

What is directly checkable, without needing to settle exactly who the Herodians were, is what Matthew did with the group across his own Gospel. Mark names Pharisees and Herodians together twice: first plotting against Jesus in a Galilean synagogue (Mark 3:6), then again setting this same tax trap in Jerusalem (Mark 12:13). Matthew's parallel to the first scene — his own version of the healing of the man with the withered hand — drops the Herodians entirely, reporting only that "the Pharisees went out and conspired against him, how to destroy him" (Matthew 12:14). The group resurfaces in Matthew's own text exactly once: here, at 22:16, following Mark's second occurrence closely. A few chapters earlier, adapting Mark's leaven warning, Matthew makes the same kind of substitution in reverse, swapping in "the leaven of the Sadducees" where Mark had written "of Herod" (compare Mark 8:15 with Matthew 16:6, 11-12). Matthew, on this evidence, is not independently attesting the Herodians as a recurring feature of his own source material; he is following Mark closely at the one point where Mark still names them, having already quietly written them out elsewhere. That is a real fact about Matthew's editorial method, whatever one concludes about the historical alliance itself.
'@

$beat3 = @'
The Sadducees' resurrection question is not an invented plot device dressed up as theology; both the group and their signature denial are independently attested outside the Gospels. Acts 23:8 states plainly that "the Sadducees say there is no resurrection... but the Pharisees acknowledge them all," and Josephus, with no stake whatsoever in this specific Gospel scene, confirms the same division from his own vantage point in both Jewish War 2.8.14 and Jewish Antiquities 18.1.4 [47]. Two independent sources, one inside the New Testament and one outside it, agree on the doctrinal fault line this hypothetical is built to probe.

Jesus's own counter is worth a closer look on its own terms. He answers from Exodus 3:6 — God identifying himself to Moses at the burning bush as "the God of Abraham, the God of Isaac, and the God of Jacob" — and reasons from the present tense: God does not describe himself as having been their god, but as being it, still, which Jesus reads as proof the patriarchs remain, in some sense, alive to God even after their deaths. Commentators have long noted that this proof-text is drawn from the Torah itself, Exodus being one of the five books of Moses, rather than from a passage that states resurrection more explicitly, such as Daniel 12:2. One popular explanation is that Jesus is deliberately meeting the Sadducees on their own restricted ground, since the Sadducees are widely said to have accepted only the five books of Moses as scripture [47]. That specific claim about a Sadducean Pentateuch-only canon, however, turns out to rest on shakier footing than its popularity suggests [[NOTE:sadducees-torah-only-canon-nuance]] — a genuine case where a secondary explanation gets repeated with confidence even though the primary, well-attested fact it is explaining does not actually require it. The resurrection denial itself stays exactly as solid as the Acts and Josephus corroboration already established makes it [47]; only the popular gloss on why Jesus argued from Exodus turns out to be softer ground.
'@

$beat4 = @'
Asked to name the greatest commandment, Jesus answers with two citations rather than one: the Shema's opening command to love God "with all your heart, with all your soul, and with all your mind," echoing Deuteronomy 6:5, followed immediately by "love your neighbor as yourself," lifted directly from Leviticus 19:18, with the comment that "on these two commandments hang all the Law and the Prophets" (22:37-40).

The individual pieces are already established ground in this campaign: Hillel the Elder's own one-line Torah summary, "what is hateful to you, do not do to your neighbor," is the closest and best-attested Jewish parallel to Matthew's Golden Rule back in chapter 7, and mainstream commentary reads that convergence as genuine independent ethical convergence rather than borrowing in either direction [141] [145]. Worth adding here is a different, more specific point: pairing these exact two commandments, love of God plus Leviticus 19:18's love of neighbor, as a joint summary of the whole Law, is not unique to this scene. The same two-part pairing appears twice within a single, roughly contemporary or near-contemporary Jewish text, the Testaments of the Twelve Patriarchs, in its Testament of Issachar: "Love the Lord and your neighbor" (5:2), and again, "The Lord I loved with all my strength; likewise, I loved every human being as I love my children" (7:6) [[NOTE:shema-leviticus-pairing-testament-issachar]]. Whatever this specific document's own complicated compositional history, the pairing itself shows that summarizing "the whole Law" by naming these two commandments together was an available, recognized move within the wider Jewish ethical tradition Matthew's Jesus is shown drawing on here [[NOTE:shema-leviticus-pairing-testament-issachar]], not a rhetorical invention built solely for this scene.
'@

$beat5 = @'
Jesus's own question — how can the Christ be David's son, if David himself, "in the Spirit," calls him "Lord" while quoting Psalm 110:1 — trades on a real ambiguity built into the psalm's own Hebrew, not merely a rhetorical trick constructed for this scene.

Psalm 110:1 reads, in its plain sense, "The LORD said to my lord: Sit at my right hand." The Hebrew uses two different words for the two figures, and the second of them, adoni, is never elsewhere used of God himself in the Hebrew Bible — only of a human or angelic superior. In its own original court setting, the verse most naturally reads as a prophet or priest addressing the reigning Davidic king as "my lord," relaying God's own promise to him; nothing in that setting requires "my lord" to mean a future, greater descendant rather than the king actually being addressed [[NOTE:psalm-110-adoni-yhwh-ambiguity]]. Reading it that way — as a promise about someone still to come, someone who outranks David even as his own descendant — is Jesus's interpretive move laid on top of the psalm's plain original sense, and it is exactly that move the Pharisees cannot answer.

The verse's outsized career in earliest Christianity is itself a real, checkable pattern, worth pausing on beyond this single exchange. Psalm 110:1 is quoted or clearly alluded to more often across the New Testament than any other Old Testament verse: directly quoted at Acts 2:34-35 in Peter's own Pentecost speech, and repeatedly through the letter to the Hebrews, with further echoes at 1 Corinthians 15:25, Ephesians 1:20, and 1 Peter 3:22, well beyond its use here and in Mark and Luke's parallel versions of this same exchange [[NOTE:psalm-110-nt-reception-history]]. A single verse, asked as a question its hearers cannot answer in this chapter, goes on to become one of the load-bearing proof-texts of the entire early Christian movement's claim about who Jesus was — the same built-in ambiguity between YHWH and adoni that stumps the Pharisees here powering, a generation later, some of that movement's own central arguments [[NOTE:psalm-110-adoni-yhwh-ambiguity]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'TRIBUTE PENNY (TIBERIUS DENARIUS)' = "The silver denarius Jesus requests in the render-unto-Caesar exchange (22:19-21), identified by numismatists as a coin struck under Tiberius. Its obverse carries the emperor's portrait ringed by TI CAESAR DIVI AVG F AVGVSTVS, a minted claim to divine sonship [46]; its reverse carries a seated figure generally identified as Livia personified as the goddess Pax, ringed by the legend PONTIF MAXIM, Tiberius's title as Rome's chief priest [[NOTE:tribute-penny-livia-pax-reverse]]. See also the DENARIUS (ROMAN DAY-LABORER WAGE) entry (chapter 20), which covers the same coin denomination's ordinary wage value rather than this specific type's inscriptions."
'HERODIANS (POLITICAL FACTION)' = "A real, if imprecisely documented, political faction named alongside the Pharisees in Matthew 22:16, and elsewhere in Mark (3:6, 12:13), generally understood by historians as a loose network of loyalists to Herodian rule rather than a formally organized sect on the order of the Pharisees or Sadducees. Matthew's own Gospel names the group only here, having dropped it from his parallel to Mark 3:6 (Matthew 12:14) and substituted 'the Sadducees' for it in his version of Mark's leaven warning (Matthew 16:6) — a checkable pattern in how this Gospel handled its source material [[NOTE:herodians-alliance-matthew-22]]."
'TESTAMENT OF ISSACHAR (TESTAMENTS OF THE TWELVE PATRIARCHS)' = "A section of the Testaments of the Twelve Patriarchs, a Jewish pseudepigraphal work (with debated Christian interpolations and compositional history) presenting Jacob's twelve sons delivering farewell instructions to their descendants. Issachar's testament pairs love of God with love of neighbor twice (5:2; 7:6), independently attesting the same two-commandment summary of the Law that Jesus gives at Matthew 22:37-40 [[NOTE:shema-leviticus-pairing-testament-issachar]]."
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
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $maxChapterSortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch22NodeId $id $maxChapterSortKey
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
# Herodians, Denarius (Tribute Penny), Sadducees, Psalm 110, Shema, Hillel, Tiberius Caesar,
# Livia, and Flavius Josephus all already exist in the GSPL entity catalog (checked by name
# before writing this script) — Seed-Entity's own existence check would skip them regardless,
# but they are intentionally not called again here. Only the genuinely new entity follows.
Seed-Entity "Testaments of the Twelve Patriarchs" "testaments-of-the-twelve-patriarchs" "document" "Jewish pseudepigraphal work (debated Christian interpolations) presenting the twelve sons of Jacob's farewell instructions; its Testament of Issachar (5:2, 7:6) independently pairs love of God with love of neighbor."

$conn.Close()
Write-Host "DONE Chapter 22 depth pass."
