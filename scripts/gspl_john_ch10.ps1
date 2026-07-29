$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
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
$NotesNodeId = [guid]"019FA96D-7D48-75E0-9BD9-2190171276DC"
$GlossaryNodeId = [guid]"019FA96D-8DD0-70E4-8D98-34AC48833B7E"
$Ch10NodeId = [guid]"019FA96C-B3A1-7AB4-B943-A964D30342D8"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'ot-shepherd-background' = @{ title="The shepherd image's real background: royal failure, not pastoral idyll"; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 10:1-18. Brown traces the chapter's shepherd imagery to a specific Hebrew Bible tradition rather than a generic pastoral idyll: Ezekiel 34's oracle against Israel's own failed shepherd-kings, who feed themselves instead of the flock and let the sheep scatter and become prey; Psalm 23's portrait of the LORD as the psalmist's own shepherd; and Numbers 27:17, where Moses asks that Israel not be left 'like sheep without a shepherd' after his death. Read against that background, Jesus's claim to be the good shepherd is a claim to succeed where Israel's own royal shepherds failed, not merely a comforting rural image." }
'bultmann-two-shepherd-sources' = @{ title='One discourse or two, spliced together?'; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray et al. (Philadelphia: Westminster Press, 1971; German original 1941), commentary on John 10:1-21. Bultmann's source-critical analysis proposed that John 10 preserves two originally separate shepherd sayings-units awkwardly joined by the evangelist: a gate saying (10:1-10, where Jesus is the entrance the sheep pass through) and a shepherd saying (10:11-18, where Jesus is the one who tends and dies for the flock), each with its own internal logic, spliced into a single discourse whose controlling image shifts without warning." }
'brown-counter-rhetorical-layering' = @{ title='Or one discourse, deliberately layered'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 10:1-21. Brown argues the shift from gate to shepherd is not evidence of clumsy editorial splicing but a deliberate rhetorical technique attested elsewhere in John, such as the water and living-water and the bread and bread-of-life double images in chapters 4 and 6: the evangelist layers a second figure onto the first once the first has made its point, rather than abandoning it outright. The disagreement between this reading and a source-division proposal remains a live one in Johannine scholarship, not a settled question." }
'barrett-mixed-metaphor' = @{ title='The gate that is also the shepherd'; body="C.K. Barrett, The Gospel According to St John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 10:7-11. Barrett notes the plain grammatical oddity that within a few verses Jesus is identified first as 'the gate for the sheep' (10:7, 9) and then as 'the good shepherd' (10:11, 14) who enters by that same gate (10:2) — two roles that, taken as a single continuous picture, do not quite cohere, since the shepherd would then be entering through himself." }
'other-sheep-gentile-retrospective' = @{ title='Sheep not yet gathered'; body="D. Moody Smith, The Theology of the Gospel of John, New Testament Theology series (Cambridge: Cambridge University Press, 1995), chapter on the Good Shepherd discourse; see also Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 10:16. Jesus's line about 'other sheep that are not of this fold,' destined to be gathered into 'one flock, one shepherd,' is widely read by mainstream commentators as a reference to the Gentile mission of the early church, a mission not underway in Jesus's own lifetime. The saying is generally treated as a retrospective theological statement, composed after the fact and placed on Jesus's lips by the evangelist, rather than a claim the historical Jesus is likely to have voiced in these terms before his death." }
'ehrman-post-easter-retrojection' = @{ title="Words a movement grows into its founder's mouth"; body="Bart D. Ehrman, The New Testament: A Historical Introduction to the Early Christian Writings, 7th ed. (New York: Oxford University Press, 2019), chapter on the Gospel of John. Ehrman describes a broader pattern historical-critical scholarship applies across the Fourth Gospel: theological convictions the Johannine community reached only after Jesus's death and resurrection, including its stance on a Gentile mission the historical Jesus is not otherwise recorded as having launched, are voiced in the narrative as things Jesus said during his ministry, a retrojection technique consistent with ancient biographical writing generally." }
'maccabees-primary-sources' = @{ title="The Feast's own origin story"; body="1 Maccabees 4:36-59 and 2 Maccabees 10:1-8 (Apocrypha/Deuterocanonical books), the two earliest surviving accounts of the Temple's rededication. Both describe Judas Maccabeus and his forces cleansing and rededicating the Jerusalem Temple altar in 164 BCE, three years to the day after Antiochus IV Epiphanes had it desecrated, and instituting an eight-day annual festival of light and sacrifice in commemoration — the historical event John's 'Feast of Dedication' (10:22) names." }
'josephus-antiochus-desecration' = @{ title='Josephus on the desecration and its undoing'; body="Flavius Josephus, Jewish Antiquities, Book 12, chapters 7.6-7 (sections 316-326) (Loeb Classical Library, trans. Ralph Marcus, Cambridge, MA: Harvard University Press). Josephus, writing over two centuries after the events, narrates Antiochus IV Epiphanes's desecration of the Jerusalem Temple, including the installation of a pagan altar over the altar of burnt offering, and Judas Maccabeus's subsequent recapture and rededication of the Temple, explicitly naming the resulting festival 'Lights,' because the right of worship had unexpectedly reappeared to the Jewish people 'beyond their hopes.'" }
'talmud-oil-miracle-later-legend' = @{ title='The oil-miracle story: absent from the earliest sources'; body="Babylonian Talmud, tractate Shabbat, folio 21b (Soncino translation; standard critical editions). The now-famous legend that a single day's supply of ritually pure oil burned for eight days in the rededicated Temple appears in neither 1 Maccabees nor 2 Maccabees nor Josephus; it surfaces for the first time centuries later in this Talmudic discussion, making it a later theological elaboration on a genuinely historical rededication rather than part of the feast's earliest attested memory." }
'keener-winter-anchor' = @{ title='A season the Gospel gets right'; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 1 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 10:22-23. Keener notes that the Feast of Dedication falls in the Jewish month of Kislev, corresponding to late November-December, and that John's aside 'it was winter' is both historically accurate for that festival's fixed calendar date and consistent with the narrative detail that Jesus is walking in the portico of Solomon, a covered colonnade rather than an open court, exactly where a person would seek shelter from winter weather in Jerusalem." }
'josephus-solomons-portico-architecture' = @{ title="Solomon's Portico as an attested structure"; body="Flavius Josephus, Jewish Antiquities, Book 20, section 221 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press); compare Jewish War, Book 5, sections 184-227, on the Temple's porticoes generally. Josephus describes the Temple's eastern portico as a surviving remnant of Solomon's original construction, standing along the eastern edge of the Temple platform, an architectural feature independently attested outside the Gospels and giving John's setting detail a concrete, verifiable referent rather than an invented backdrop." }
'carson-psalm82-qal-vachomer' = @{ title='An argument from the lesser to the greater'; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans / Leicester: Inter-Varsity Press, 1991), commentary ad loc. John 10:34-36. Carson identifies Jesus's appeal to Psalm 82:6 as a recognizable rabbinic argument form, qal va-chomer, 'light and heavy,' reasoning from a lesser case to a greater one: if scripture itself can apply the word 'gods' to mere human recipients of God's word without that being blasphemy, the argument runs, then it is not blasphemy for the one whom the Father consecrated and sent into the world to call himself God's Son — a real, attested form of Jewish scriptural reasoning, not an improvised deflection." }
'psalm82-judges-reading' = @{ title="Psalm 82's gods, read as human judges"; body="Marvin E. Tate, Psalms 51-100, Word Biblical Commentary vol. 20 (Dallas: Word Books, 1990), commentary ad loc. Psalm 82. Tate surveys the majority historical-critical reading of Psalm 82, in which the 'gods' (elohim) addressed and condemned by God for judging unjustly are best understood as human judges or rulers of Israel and the nations, addressed with an honorific divine title because they exercise God-delegated judicial authority — the reading John's Jesus appeals to in arguing that scripture itself calls certain humans 'gods' without impiety." }
'heiser-divine-council-reading' = @{ title='Or read as a divine council'; body="Michael S. Heiser, 'The Divine Council in Late Canonical and Non-Canonical Second Temple Jewish Literature' (Ph.D. dissertation, University of Wisconsin-Madison, 2004), chapter on Psalm 82. Heiser argues for a minority but well-represented scholarly reading in which Psalm 82's 'gods' (elohim) are not human judges at all but literal members of Yahweh's heavenly divine council, condemned to 'die like men' (82:7) for failing to administer justice among the nations under their charge, a reading with roots in comparative ancient Near Eastern divine-council literature and in some strands of Second Temple Jewish angelology. The two readings remain a genuine, unresolved exegetical dispute; John's Jesus does not adjudicate between them, only exploits the term's flexibility for his own argument." }
'metzger-john10-29-textual-variant' = @{ title='A pronoun that splits the manuscripts'; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), commentary ad loc. John 10:29. Metzger documents a genuine, long-recognized textual crux in Jesus's claim about the Father who has given him the sheep: the earliest manuscripts split between a masculine reading, emphasizing that the Father himself is greater than all, and a neuter reading, emphasizing the magnitude of the gift itself, a variant significant enough that the committee could not resolve it with full confidence beyond a marginal preference — a reminder that even a verse read as a clean statement of divine protection rests on a disputed word in the Greek manuscript tradition." }
'von-wahlde-hoi-ioudaioi-conflict' = @{ title='A discourse shaped by a later synagogue conflict'; body="Urban C. von Wahlde, The Gospel and Letters of John, vol. 2: Commentary on the Gospel of John, Eerdmans Critical Commentary (Grand Rapids: Eerdmans, 2010), commentary on John 10:19-21 and the repeated hostile exchanges with 'the Jews' across chapters 5 through 10. Von Wahlde reads the discourse's recurring blasphemy accusations and attempted stonings as bearing the marks of the Johannine community's own later conflict with synagogue authorities near the end of the first century, projected back onto narrated exchanges between Jesus and his contemporaries decades earlier." }
'brown-i-and-father-functional-unity' = @{ title='One in what sense?'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 10:30. Brown notes that the Greek of 'I and the Father are one' uses the neuter hen, 'one thing,' not the masculine heis, 'one person,' and argues the original sense in context is a unity of will and protective power over the flock, consistent with the surrounding verses about no one snatching the sheep away, rather than a fully articulated claim of metaphysical identity of being. The later, doctrinally freighted reading the phrase acquired in fourth-century trinitarian debate is a theological development built on, rather than already stated by, the verse itself." }
'keener-bethany-inclusio-withdrawal' = @{ title='Back to where it started'; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 1 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 10:40-42. Keener reads Jesus's withdrawal across the Jordan to the place where John had been baptizing as a deliberate literary inclusio bracketing the entire public-ministry section of the Gospel, returning the narrative to the same Transjordan location named at its opening (1:28), with the local crowd's verdict there, that everything John said about Jesus was true, serving as the evangelist's own closing argument for John the Baptist's reliability as a witness." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
John gives no scene break after the healed blind man's story: Jesus moves straight into an extended figure of speech about a sheepfold. A shepherd enters through the gate, and the sheep recognize his voice and follow him; a thief or a robber climbs in some other way, and the sheep flee a stranger's voice they don't know. When the crowd doesn't follow the image, Jesus restates it from a different angle entirely: "I am the gate for the sheep" (10:7), through whom anyone who enters will be kept safe and find pasture; then, without any transition marking the shift, "I am the good shepherd" (10:11), who lays down his life for the sheep, unlike the hired hand, who isn't the sheep's owner, has no stake in them, and runs at the first sign of a wolf, leaving the flock to be scattered and seized (10:1-15).

The image's real background is worth establishing before anything else, because "shepherd" in the mouth of a first-century Jewish teacher wasn't a generic pastoral compliment. Ezekiel 34 is an oracle against Israel's own ruling shepherds, condemned by name for feeding themselves instead of the flock and letting the sheep scatter and become prey to every wild animal; Psalm 23 pictures the LORD himself as the shepherd a psalmist can trust; and Numbers 27:17 has Moses ask that Israel not be left "like sheep without a shepherd" after his own death. Read against that background, Jesus's claim to be the good shepherd is a claim about succeeding where Israel's own leadership failed, not a soft rural metaphor [[NOTE:ot-shepherd-background]].

The harder question, and a genuinely live one among commentators, is what to make of the discourse's own internal seams. Within a few verses Jesus is first the gate the sheep pass through (10:7, 9) and then the shepherd who enters by that same gate (10:2, 11), two roles that, read as one continuous picture, don't quite sit together, since the shepherd would then be entering through himself [[NOTE:barrett-mixed-metaphor]]. Rudolf Bultmann's classic source-critical solution was that John 10 actually preserves two originally separate shepherd sayings-units, a gate saying and a shepherd saying, joined by the evangelist into a single discourse whose imagery shifts without warning [[NOTE:bultmann-two-shepherd-sources]]. Raymond Brown pushed back directly: he read the shift as a deliberate rhetorical technique John uses elsewhere too, layering a second image onto a first once the first has done its work, rather than clumsy editorial splicing [[NOTE:brown-counter-rhetorical-layering]]. Neither side has settled the question; it remains an open disagreement about how this Gospel was actually composed.
'@

$beat2 = @'
Jesus continues the discourse: he knows his own sheep and his own know him, the same way the Father knows him and he knows the Father, and he lays down his life for the sheep of his own accord, since no one takes it from him, and he has power both to lay it down and to take it up again, a command he says he has received from the Father. Then he adds a further claim: he has "other sheep, that are not of this fold," whom he must also bring, so that there will be "one flock, one shepherd." The teaching splits the crowd; some call it demon-possessed raving, others point to the healing of the blind man and ask how a madman could open blind eyes (10:16-21).

That line about other sheep "not of this fold" is the discourse's most forward-leaning claim, and mainstream historical-critical scholarship treats it as exactly that, forward-leaning past the point where the story is actually standing. The historical Jesus, so far as the earlier Gospel tradition depicts him, is not otherwise recorded as having planned or launched a mission beyond Israel; that mission is a development of the church's first few decades. The "one flock, one shepherd" line is widely read as a retrospective statement, composed after the Gentile mission was already underway and placed on Jesus's lips by the evangelist rather than spoken by him in these terms before his death [[NOTE:other-sheep-gentile-retrospective]]. It fits a broader pattern historical-critical scholars trace across John generally: theological convictions the Johannine community reached only after the resurrection are voiced in the narrative as things Jesus said during his lifetime, a retrojection technique not unique to this Gospel or unusual for ancient biographical writing [[NOTE:ehrman-post-easter-retrojection]].

The crowd's split reaction is worth noting for what it doesn't resolve. Both sides argue from the same available evidence, the teaching itself and the healed man from the previous chapter, and reach opposite verdicts. Nothing in the text adjudicates between them; the division simply stands, unresolved, as the discourse's first scene ends.
'@

$beat3 = @'
The scene changes abruptly: it is now the Feast of Dedication, in Jerusalem, in winter, and Jesus is walking in the Temple, in the portico of Solomon. A crowd surrounds him and presses him directly: "How long will you keep us in suspense? If you are the Christ, tell us plainly." Jesus answers that he has told them and they don't believe, that his works testify on his behalf, that his sheep hear his voice and follow him and will never perish, and that no one can snatch them out of his hand or his Father's hand, and then states plainly, "I and the Father are one." The crowd picks up stones to kill him a second time in this Gospel, and when Jesus asks for which of his many good works from the Father they are stoning him, they answer that it isn't for a good work but for blasphemy, "because you, being a man, make yourself God" (10:22-33).

The festival itself is real, datable, and unusually well documented for a Gospel setting. Its origin lies in the Maccabean revolt: Antiochus IV Epiphanes desecrated the Jerusalem Temple, and three years later, in 164 BCE, Judas Maccabeus recaptured and rededicated it, an event both 1 and 2 Maccabees record as the origin of an eight-day annual festival of light and sacrifice [[NOTE:maccabees-primary-sources]]. Josephus, writing over two centuries later, tells the same story and explicitly names the festival's popular title, "Lights," because the right of worship was unexpectedly restored to the Jewish people [[NOTE:josephus-antiochus-desecration]]. The oil-lamp miracle now associated with the holiday in popular memory is absent from every one of these earliest accounts; it surfaces for the first time centuries afterward in the Babylonian Talmud, a clear case of later legendary elaboration growing up around a genuinely historical event [[NOTE:talmud-oil-miracle-later-legend]]. John's own aside that "it was winter" checks out against the festival's fixed Kislev date, and it is a rare instance of a verifiable, accurate calendrical detail in this Gospel [[NOTE:keener-winter-anchor]]. The location is equally concrete: Solomon's Portico was a real, covered colonnade along the Temple's eastern edge, independently described by Josephus as a surviving remnant of Solomon's own construction, a solid architectural anchor for the scene rather than an invented backdrop [[NOTE:josephus-solomons-portico-architecture]].

The declaration "I and the Father are one" carries less theological freight in its own Greek than later doctrine placed on it. The word for "one" here is neuter, "one thing," not the masculine "one person," and mainstream commentary reads the original claim as a unity of will and protective power over the flock, consistent with the surrounding verses about no one snatching the sheep away, rather than the fully worked-out claim of one shared divine being that fourth-century trinitarian debate later built on top of it [[NOTE:brown-i-and-father-functional-unity]]. That doesn't make the crowd's blasphemy charge irrational by the standards of the day: claiming any kind of identity with God was itself the flashpoint, whatever its exact metaphysical content.
'@

$beat4 = @'
Jesus responds to the blasphemy charge not by retreating from it but by arguing from scripture itself: isn't it written in "your Law," Psalm 82:6, "I said, you are gods"? If scripture calls those to whom the word of God came "gods," and scripture cannot be broken, how can it be blasphemy for the one whom the Father consecrated and sent into the world to say, "I am the Son of God"? He adds that if he isn't doing his Father's works, they need not believe him, but if he is, they should believe the works themselves, "that you may know and understand that the Father is in me and I am in the Father." The crowd tries again to arrest him, he escapes their hands, and withdraws across the Jordan to the place where John had first been baptizing, where he stays; many come to him there, and many believe, because, they say, everything John said about this man was true (10:34-42).

Jesus's citation isn't an improvised deflection; it follows a recognized form of Jewish scriptural argument, qal va-chomer, reasoning from a lesser case to a greater one: if scripture itself can apply the word "gods" to mere human recipients of God's word without that being blasphemous, the argument runs, then it cannot be blasphemy for the one the Father sent to call himself God's Son [[NOTE:carson-psalm82-qal-vachomer]]. What the argument leaves genuinely open is Psalm 82's own original meaning, a real and unresolved dispute among scholars of the Hebrew Bible. The historical-critical mainstream reads the psalm's addressed "gods" as human judges or rulers of Israel and the nations, given an honorific divine title because they exercised God-delegated judicial authority [[NOTE:psalm82-judges-reading]]; a substantial minority reading takes the same term at face value, as literal members of a divine council condemned for failing the nations under their charge [[NOTE:heiser-divine-council-reading]]. John's Jesus doesn't settle that dispute; he only needs the term's flexibility to hold for his argument to work.

Two smaller notes close out the chapter. First, a genuine textual crux: the manuscripts of 10:29 split over whether the emphasis falls on the Father himself being greater than all or on the magnitude of the gift he has given, a variant significant enough that even careful critical editions cannot resolve it with full confidence [[NOTE:metzger-john10-29-textual-variant]]. Second, the chapter's recurring hostility, the repeated stonings and arrest attempts stretching back through chapters 5, 7, and 8, is widely read as carrying the marks of the Johannine community's own later conflict with synagogue authorities near the end of the first century, projected back onto the narrated exchanges of decades earlier [[NOTE:von-wahlde-hoi-ioudaioi-conflict]]. And the withdrawal itself, back across the Jordan to John the Baptist's own former location, functions as a deliberate literary bracket around the whole preceding section of the Gospel, returning the story to where it began (1:28), with the local crowd's verdict on John's earlier testimony serving as the evangelist's own closing argument for its reliability [[NOTE:keener-bethany-inclusio-withdrawal]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'GOOD SHEPHERD (JOHN 10)' = "Jesus's self-identification in the discourse of John 10:1-18, contrasted explicitly with a thief, a robber, and a hired hand who abandons the flock when danger appears. The image draws on a specific Hebrew Bible tradition of shepherd-kings judged for failing their people (Ezekiel 34) rather than a generic pastoral idyll [[NOTE:ot-shepherd-background]], and its relationship to the chapter's separate gate image is a genuine point of scholarly disagreement over whether John 10 preserves one deliberately layered discourse or two originally distinct sayings-units spliced together [[NOTE:bultmann-two-shepherd-sources]] [[NOTE:brown-counter-rhetorical-layering]]."
'THE GATE FOR THE SHEEP' = "A second self-identification Jesus makes within the same discourse (10:7, 9), distinct from, and on a strictly literal reading in some tension with, his claim moments later to be the shepherd who enters by that gate (10:2, 11) [[NOTE:barrett-mixed-metaphor]]. Commentators read the shift either as evidence of two combined source-units [[NOTE:bultmann-two-shepherd-sources]] or as intentional rhetorical layering characteristic of John's style elsewhere in the Gospel [[NOTE:brown-counter-rhetorical-layering]]."
'HIRED HAND' = "The discourse's third figure (10:12-13), contrasted with the good shepherd: a paid caretaker who flees when a wolf attacks because the sheep are not his own and their fate is not his personal stake. The figure functions rhetorically as a foil rather than as a reference to any named individual or group in the surrounding narrative."
'OTHER SHEEP NOT OF THIS FOLD' = "Jesus's statement in John 10:16 that he has sheep beyond his present hearers, whom he must also bring in, so that there will be one flock and one shepherd. Mainstream historical-critical scholarship widely reads this as a retrospective reference to the Gentile mission of the early church, composed and placed in Jesus's mouth after that mission was already underway, rather than a plan the historical Jesus is likely to have articulated in these terms during his lifetime [[NOTE:other-sheep-gentile-retrospective]] [[NOTE:ehrman-post-easter-retrojection]]."
'FEAST OF DEDICATION (HANUKKAH)' = "The eight-day winter festival (John 10:22) commemorating Judas Maccabeus's rededication of the Jerusalem Temple in 164 BCE after its desecration three years earlier by the Seleucid king Antiochus IV Epiphanes, documented in 1 and 2 Maccabees and in Josephus [[NOTE:maccabees-primary-sources]] [[NOTE:josephus-antiochus-desecration]]. The now-famous legend of oil that miraculously burned for eight days is absent from all of these earliest sources and first appears centuries later in the Babylonian Talmud [[NOTE:talmud-oil-miracle-later-legend]]. John's incidental note that it was winter is a rare, independently checkable calendrical detail in this Gospel, and it checks out [[NOTE:keener-winter-anchor]]."
'ANTIOCHUS IV EPIPHANES' = "The Seleucid king (r. 175-164 BCE) whose desecration of the Jerusalem Temple, including the installation of a pagan altar over the altar of burnt offering, triggered the Maccabean revolt and, upon the revolt's success, the rededication commemorated by the Feast of Dedication referenced in this chapter's setting (10:22) [[NOTE:josephus-antiochus-desecration]] [[NOTE:maccabees-primary-sources]]."
'SOLOMON''S PORTICO' = "A covered colonnade along the eastern edge of the Jerusalem Temple platform, where Jesus is walking when confronted about his identity in this chapter's second scene (10:23). Josephus independently describes this portico as a surviving remnant of Solomon's original Temple construction, giving John's setting detail a concrete, externally attested architectural referent [[NOTE:josephus-solomons-portico-architecture]]."
'PSALM 82 ("I SAID, YOU ARE GODS")' = "The psalm Jesus cites in his own defense after being accused of blasphemy for making himself God (10:33-34), quoting Psalm 82:6's address to 'gods' whose own judgment is condemned later in the same psalm. Jesus's use of the citation follows a recognized rabbinic a fortiori argument form [[NOTE:carson-psalm82-qal-vachomer]]. The psalm's own original referent for 'gods' is itself a live scholarly dispute: the historical-critical mainstream reads it as human judges or rulers addressed with an honorific divine title [[NOTE:psalm82-judges-reading]], while a substantial minority reading takes it as literal members of a divine council [[NOTE:heiser-divine-council-reading]]."
'"I AND THE FATHER ARE ONE"' = "Jesus's declaration at John 10:30, immediately met with an attempt to stone him for blasphemy (10:31, 33). The underlying Greek construction uses a neuter 'one thing' rather than a masculine 'one person,' which mainstream commentary reads in its original context as a claim of unity of will and protective power over the flock rather than the fully developed claim of shared divine being it was later read as in fourth-century trinitarian debate [[NOTE:brown-i-and-father-functional-unity]]."
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

# ---- Insert chapter beats with placeholder replacement ----
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch10NodeId $id $sortKey
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
Seed-Entity "Feast of Dedication (Hanukkah)" "feast-of-dedication-hanukkah" "vocabulary" "Eight-day winter festival commemorating Judas Maccabeus's 164 BCE rededication of the Jerusalem Temple after Antiochus IV Epiphanes's desecration; setting of John 10:22-42 at Solomon's Portico."

$conn.Close()
Write-Host "DONE Chapter 10."
