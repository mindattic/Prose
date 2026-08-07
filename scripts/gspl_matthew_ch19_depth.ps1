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
$Ch19NodeId = [guid]"019FA06F-661F-7830-9AD8-BF91C0C5F560"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06F-661F-7830-9AD8-BF91C0C5F560' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'mishnah-gittin-9-10-hillel-shammai' = @{ title="A live rabbinic argument: 'any cause' versus 'a matter of indecency'"; body="Mishnah, Gittin 9:10, in Herbert Danby's standard English translation, The Mishnah (Oxford: Oxford University Press, 1933). The Mishnah itself records a real disagreement between the two great Pharisaic schools over the scope of Deuteronomy 24:1's grounds for divorce, 'because he has found some indecency in her' (ervat davar): the House of Shammai held that a man may divorce his wife only if he found in her 'a matter of indecency' (sexual misconduct), while the House of Hillel held that he may divorce her even over so trivial a matter as spoiling his food, reading the same phrase far more broadly; a third view, attributed to Rabbi Akiva, permitted divorce even if the man simply found another woman more attractive. Because the Mishnah was not compiled and redacted until roughly 200 CE, it postdates Jesus's own lifetime by well over a century, but the underlying two-school dispute over Deuteronomy 24:1's wording is independently understood by mainstream scholarship to have already been live in the first century, the same era addressed by the Pharisees' test question in Matthew 19:3." }
'matthew-porneia-shammai-alignment' = @{ title="Matthew's exception clause and the stricter Pharisaic school"; body="David Instone-Brewer, Divorce and Remarriage in the Bible: The Social and Literary Context (Grand Rapids, MI: Eerdmans, 2002), chapter on the Matthean divorce texts. Instone-Brewer reads Matthew's version of Jesus's divorce ruling - permitting divorce only 'for sexual immorality' (porneia, 19:9) - as a recognizable answer inside the live Hillel/Shammai dispute over Deuteronomy 24:1, one that lands close to the House of Shammai's narrow reading of 'a matter of indecency' as sexual misconduct specifically, rather than the House of Hillel's far broader reading. Instone-Brewer's specific thesis that Jesus is directly settling this named rabbinic dispute remains debated among specialists, and some critics argue the verbal match between Matthew's porneia and the Mishnah's Hebrew phrasing is looser than his argument requires; what is not seriously disputed is that Matthew's version places Jesus's ruling recognizably nearer the stricter of the two historically attested first-century positions." }
'mosaic-concession-hardness-of-heart' = @{ title="Concession, not the ideal: how 'hardness of heart' argues"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 19:8. Jesus's phrase 'because of your hardness of heart' (pros ten sklerokardian hymon) explicitly frames Moses's Deuteronomy 24:1 divorce provision as an accommodation to human moral failure rather than the creation-order ideal set out in Genesis 1:27 and 2:24, cited immediately before it. Davies and Allison note this concession-versus-ideal argumentative structure - treating a later Mosaic law as a regrettable but permitted departure from an earlier, more authoritative creation pattern - has recognizable analogues elsewhere in Second Temple and rabbinic legal reasoning, rather than being a rhetorical device unique to this passage." }
'qumran-cd-genesis-127-monogamy' = @{ title="An independent Jewish voice already arguing from Genesis"; body="Joseph A. Fitzmyer, 'The Matthean Divorce Texts and Some New Palestinian Evidence,' Theological Studies 37 (1976): 197-226. Fitzmyer's landmark study drew scholarly attention to a Qumran sectarian text, the Damascus Document, which argues against a man taking a second wife while his first still lives by citing the same verse Jesus cites - 'male and female he created them' (Genesis 1:27) - alongside Genesis 7:9's paired animals entering the ark and Deuteronomy 17:17's warning against a king multiplying wives (CD 4:20-5:2). The Qumran community's target is polygamy specifically, not divorce, and its practical conclusion is not identical to Jesus's - but the underlying interpretive habit, reaching past Deuteronomy to ground first-century marital law in Genesis's creation pattern, turns out to be an independently attested first-century Jewish argument, not a rhetorical device unique to the Gospels." }
'mark-luke-no-exception-clause' = @{ title="A clause found only in Matthew"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 19:9. Mark's parallel version of this same teaching ('whoever divorces his wife and marries another commits adultery against her,' Mark 10:11-12) and Luke's version ('everyone who divorces his wife and marries another commits adultery,' Luke 16:18) both state the prohibition without any stated exception at all; Matthew alone adds the qualifying phrase 'except for sexual immorality' (also at Matthew 5:32). Davies and Allison treat this as one of the clearest cases in the Synoptic tradition of a Gospel writer's own editorial addition to a shared saying, whatever view one takes of its ultimate historical warrant - a real, textually verifiable difference among the three Synoptic accounts of the identical teaching, not a harmony to be smoothed over." }
'eunuch-categories-matthew-19-12' = @{ title="Three kinds of eunuch, only the last one a saying"; body="J. David Hester, 'Eunuchs and the Postgender Jesus: Matthew 19.12 and Transgressive Sexualities,' Journal for the Study of the New Testament 28, no. 1 (2005): 13-40. Matthew's Jesus names three distinct categories in one verse: those 'born eunuchs from their mother's womb,' those 'made eunuchs by men' (a known practice under the period's royal and imperial households), and those who 'have made themselves eunuchs for the sake of the kingdom of heaven.' Hester's study documents that ancient audiences already treated eunuchs as a socially marked, ambiguous third category outside the male/female binary that structured most Greco-Roman and Jewish social expectation, which sharpens rather than softens how startling it is that Jesus applies the term approvingly, by voluntary metaphor, to some of his own followers." }
'ancient-marriage-universal-expectation' = @{ title="Marriage and children as the assumed life course"; body="Peter Brown, The Body and Society: Men, Women, and Sexual Renunciation in Early Christianity (New York: Columbia University Press, 1988), chapters on marriage and sexuality in the Roman world and in Judaism. Brown's standard historical study documents that in both Greco-Roman civic life and Second Temple Jewish practice, marriage and childbearing were treated as a near-universal, socially and religiously expected life course rather than one option among several; deliberate, lifelong voluntary celibacy of the kind Jesus commends in 19:12 ('for the sake of the kingdom of heaven') had no significant precedent as an organized ideal in either surrounding culture, which is a large part of why Brown and other historians treat the saying as a genuinely disruptive departure from the assumptions of its own world, not a restatement of an available option." }
'origen-self-castration-eusebius' = @{ title="Taken literally, by one of Christianity's greatest early scholars"; body="Eusebius of Caesarea, Ecclesiastical History, Book 6, chapter 8 (Loeb Classical Library, trans. Kirsopp Lake, Cambridge, MA: Harvard University Press, 1932). Eusebius reports that the third-century biblical scholar Origen, then a young catechetical teacher in Alexandria instructing both men and women, took Matthew 19:12 in what Eusebius calls 'too literal and extreme a sense' and had himself physically castrated, both to fulfill the saying and to remove any suspicion of impropriety toward the women he taught. Modern scholars have raised real doubts about the story's reliability - Eusebius wrote roughly a century after the events he describes, and some argue the claim may have originated as a hostile rumor about a man who spoke of his own celibacy in figurative, 'eunuch' language - but the episode, true or not, is itself a significant and very early data point in the saying's reception history: read literally by at least one ancient community closely enough to be reported this way within a few generations of Origen's own lifetime." }
'roman-patronage-land-wealth' = @{ title="What 'riches' meant: land and patronage, not a bank balance"; body="Peter Garnsey and Richard Saller, The Roman Empire: Economy, Society and Culture (Berkeley: University of California Press, 1987), chapters on the economy and on patronage. Garnsey and Saller document that across the Roman world of this period, wealth and elite status were overwhelmingly bound up in land ownership and the reciprocal obligations of patronage - networks of dependents, tenants, and clients owing service and loyalty in exchange for protection and support - rather than in liquid, disposable cash of the kind a modern reader might picture. A 'rich young man' being told to 'sell what you possess' (Matthew 19:21) faced a demand with concrete social consequences beyond the financial: liquidating landed wealth in this world typically meant dissolving an entire inherited position within a patronage network, not simply converting one form of movable asset into another." }
'matthew-commandment-list-redaction' = @{ title="Which commandments, exactly? Matthew edits Mark's list"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 19:18-19. Mark's parallel version of this scene (Mark 10:19) has Jesus list 'do not defraud' among the commandments - a phrase not found among the Ten Commandments themselves, though echoing Levitical fair-dealing law (Leviticus 19:13). Matthew's version drops 'do not defraud' entirely and instead appends 'you shall love your neighbor as yourself' (Leviticus 19:18), a positively framed summary commandment absent from Mark's list at this point. Davies and Allison read the substitution as a deliberate Matthean editorial choice, replacing an idiosyncratic negative prohibition with a scripturally weightier, positively framed commandment that recurs elsewhere in Matthew as a summarizing principle (22:39)." }
'patron-client-reward-reciprocity' = @{ title="'What then will we have?': reward language inside a real patronage economy"; body="Bruce J. Malina and Richard L. Rohrbaugh, Social-Science Commentary on the Synoptic Gospels, 2nd ed. (Minneapolis: Fortress Press, 2003), commentary ad loc. Matthew 19:27-30. Malina and Rohrbaugh read Peter's blunt question - 'we have left everything and followed you; what then will we have?' - and Jesus's answering promise of thrones, 'a hundredfold,' and eternal life against the everyday reciprocity expectations of Mediterranean patron-client relationships, in which a client's loyalty and service to a patron routinely and openly anticipated a proportional return. On that reading, Peter's question is not crass but socially unremarkable, and Jesus's answer engages the same patronage logic on its own terms before overturning its ranking outright: 'many who are first will be last, and the last first' (19:30)." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders and direct [N] cross-references to existing notes) ----
$beat1 = @'
The Pharisees frame their opening question with a precise legal phrase - "for any cause" (19:3) - and that phrase is not incidental color. It points directly at a genuine, contemporary rabbinic dispute over how broadly to read Deuteronomy 24:1's grounds for divorce, "because he has found some indecency in her." The Mishnah itself later records the argument by name: the House of Shammai held a man could divorce his wife only for an actual "matter of indecency" - sexual misconduct - while the House of Hillel held he could divorce her even for burning his dinner, reading the same phrase far more permissively; a third opinion attributed to Rabbi Akiva went further still, permitting divorce simply for finding a more attractive woman [[NOTE:mishnah-gittin-9-10-hillel-shammai]]. This is the same Hillel already met in this book's discussion of the Golden Rule [28][141], here representing the loose end of a live legal spectrum rather than a fixed moral teacher.

Matthew's Jesus does not simply refuse to answer; his ruling that divorce is permitted only "for sexual immorality" (porneia, 19:9) is a recognizable move inside that same spectrum, and it lands close to Shammai's narrow reading rather than Hillel's broad one [[NOTE:matthew-porneia-shammai-alignment]]. Before reaching that ruling, though, Jesus reframes the whole legal question: Moses's Deuteronomy provision, he says, was permitted only "because of your hardness of heart," not because it reflects God's original design, which he locates instead in the Genesis creation account just cited - "male and female he created them" (Genesis 1:27) and "the two shall become one flesh" (Genesis 2:24). That concession-versus-creation-ideal argument structure - a later, regrettable legal accommodation set against an earlier, more authoritative pattern - has real analogues elsewhere in Second Temple and rabbinic legal reasoning, not just here [[NOTE:mosaic-concession-hardness-of-heart]].
'@

$beat2 = @'
Jesus is not the only first-century Jewish voice arguing marital law from the Genesis creation account rather than from Deuteronomy alone. A sectarian text found among the Dead Sea Scrolls, the Damascus Document, makes its own case against a man taking a second wife while his first still lives by citing the same verse Jesus cites - "male and female he created them" (Genesis 1:27) - alongside Genesis 7:9's paired animals entering the ark and Deuteronomy 17:17's warning against a king multiplying wives (CD 4:20-5:2). The Qumran community's target is polygamy specifically, not divorce, and its practical conclusion is not identical to Jesus's - but the underlying interpretive habit, reaching past Deuteronomy to ground first-century marital law in Genesis's creation pattern, turns out to be an independently attested first-century Jewish argument, not a rhetorical device unique to the Gospels [[NOTE:qumran-cd-genesis-127-monogamy]].

One further point is worth stating plainly rather than harmonizing away. Mark's version of this same core teaching states the prohibition on divorce and remarriage with no exception at all - "whoever divorces his wife and marries another commits adultery against her" (Mark 10:11-12) - and Luke's version is equally unqualified ("everyone who divorces his wife and marries another commits adultery," Luke 16:18). Only Matthew adds the qualifying clause "except for sexual immorality," here and at 5:32. That is a real, textually checkable difference between Matthew and the other two Synoptic accounts of what is otherwise the identical saying, not a matter of one Gospel simply summarizing more briefly than another [[NOTE:mark-luke-no-exception-clause]].
'@

$beat3 = @'
The disciples' reaction - better not to marry at all - gets an answer stranger than a simple rebuke. Jesus names three distinct categories in a single verse: those "born eunuchs from their mother's womb," those "made eunuchs by men" (a known practice under the period's royal and imperial households), and those who "have made themselves eunuchs for the sake of the kingdom of heaven" (19:12) [[NOTE:eunuch-categories-matthew-19-12]]. That third category is the startling one. In both the Greco-Roman civic world and Second Temple Jewish practice, marriage and childbearing functioned as a near-universal, expected life course rather than one option weighed against others; deliberate, lifelong voluntary celibacy as a positive religious ideal had no organized precedent in either surrounding culture [[NOTE:ancient-marriage-universal-expectation]]. Jesus commending it, even as a saying "not everyone can accept," is a genuinely disruptive departure from the assumptions of its own world, not a restatement of an available lifestyle choice.

The saying's reception history supplies its own striking data point. The church historian Eusebius reports that the third-century biblical scholar Origen, then a young teacher instructing both men and women in Alexandria, took this verse in what Eusebius calls "too literal and extreme a sense" and had himself physically castrated - both to fulfill the saying and to remove any suspicion of impropriety [[NOTE:origen-self-castration-eusebius]]. Modern historians have real doubts about whether the story is accurate reporting or hostile rumor about a man who spoke of his own celibacy in eunuch language, but either way, the episode shows the saying being taken with total, literal seriousness within a few generations of Origen's own lifetime - long before anyone was reading it as a metaphor from a comfortable historical distance.
'@

$beat4 = @'
The rich young man's question and Jesus's answer are worth reading against the actual shape of wealth in the world both inhabit. Across the Roman world of this period, elite status and riches were bound up overwhelmingly in land ownership and the reciprocal obligations of patronage - networks of tenants, dependents, and clients owing service and loyalty in exchange for protection and support - not in liquid, disposable cash the way a modern reader instinctively pictures "riches" [[NOTE:roman-patronage-land-wealth]]. Told to "sell what you possess and give to the poor" (19:21), this young man is not being asked to write a large check from savings; he is being asked to dissolve an entire inherited position within a patronage network, the entire social architecture of who depends on him and whom he can call on in turn. That is a real, structurally specific demand, and it explains why the text says he went away sorrowful rather than simply declining a transaction.

One small redactional detail in the commandments Jesus lists is worth naming too. Mark's version of this same scene has Jesus include "do not defraud" among the commandments (Mark 10:19) - a phrase absent from the Ten Commandments themselves, though it echoes Levitical fair-dealing law. Matthew's version drops that phrase entirely and instead appends "you shall love your neighbor as yourself" (19:19, quoting Leviticus 19:18), a positively framed summary commandment not in Mark's list at this point at all. Mainstream commentary reads this as a deliberate Matthean editorial substitution, trading an idiosyncratic negative prohibition for a scripturally weightier positive commandment that recurs as a summarizing principle elsewhere in Matthew (22:39) [[NOTE:matthew-commandment-list-redaction]].
'@

$beat5 = @'
This chapter has already addressed, in its own account of the "camel through the eye of a needle" line (19:24), why the popular tourist-circuit claim of a small Jerusalem gate nicknamed "the needle's eye" does not hold up: no ancient text or excavation supports any such gate, its earliest known mention is a medieval gloss over a thousand years after the Gospels, and the saying reads far more naturally as genuine, deliberately impossible hyperbole - the same rhetorical move already seen in this book's discussion of the "log in your own eye" image back in chapter 7 [43][155]. What is worth adding here is simply how precisely traceable that later legend is: a close study of the manuscript tradition finds its documentary origin in a medieval gloss attributed to either the Byzantine commentator Theophylact or, on the closer reading, Anselm of Canterbury, later carried into wider circulation through Thomas Aquinas's thirteenth-century Catena Aurea [155] - a straight line to a specific named source, not an anonymous folk tradition of unknown age.

Peter's question that follows - "we have left everything and followed you; what then will we have?" (19:27) - reads as blunt, even mercenary, until it is set against the reciprocity expectations built into the era's own patron-client relationships, in which a client's loyalty and service to a patron routinely and openly anticipated a proportional return. On that background, Peter's question is not crass but socially unremarkable, and Jesus's answer - thrones, a hundredfold return, eternal life - engages that same patronage logic on its own terms before overturning its ranking outright: "many who are first will be last, and the last first" (19:30) [[NOTE:patron-client-reward-reciprocity]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'SHAMMAI (RABBINIC SAGE)' = "A leading Pharisaic sage, contemporary and frequent legal rival of Hillel the Elder, active in Jerusalem in the decades around the turn of the era. The Mishnah remembers his school (Beit Shammai) as consistently reading Jewish law more strictly than Hillel's school (Beit Hillel), including on the specific question of divorce grounds addressed in this chapter: where Hillel's school permitted divorce for almost any dissatisfaction, Shammai's school required an actual 'matter of indecency' [[NOTE:mishnah-gittin-9-10-hillel-shammai]]. Matthew's Jesus, ruling that divorce is permitted only 'for sexual immorality' (19:9), lands recognizably closer to Shammai's position than to Hillel's [[NOTE:matthew-porneia-shammai-alignment]]. See HILLEL THE ELDER for the general Golden Rule connection already established in this book; the two sages are best read together as opposite ends of a real, live first-century legal spectrum, not as isolated figures."
'HILLEL VS. SHAMMAI ON DIVORCE (LIVE HALAKHIC DEBATE)' = "The specific, named first-century rabbinic dispute over how broadly to read Deuteronomy 24:1's grounds for divorce - 'because he has found some indecency in her' - recorded in the Mishnah as a disagreement between the House of Hillel (broad: divorce permitted for almost any cause) and the House of Shammai (narrow: divorce permitted only for actual sexual misconduct) [[NOTE:mishnah-gittin-9-10-hillel-shammai]]. This is a distinct topic from Hillel's general ethical teaching already discussed in this book under HILLEL THE ELDER (the negative Golden Rule of Matthew 7:12); here Hillel and Shammai appear specifically as legal disputants, and Matthew's version of Jesus's divorce ruling situates him inside their live argument, closer to Shammai's stricter reading [[NOTE:matthew-porneia-shammai-alignment]]. Mark and Luke's parallel versions of the same core teaching state the prohibition with no exception clause at all, a real difference from Matthew's text worth noting alongside this debate [[NOTE:mark-luke-no-exception-clause]]."
'CAMEL THROUGH THE EYE OF A NEEDLE (HYPERBOLE, NOT A GATE)' = "The proverb at Matthew 19:24 - it being easier for a camel to pass through a needle's eye than for a rich person to enter the kingdom of heaven - is popularly explained via a supposed small Jerusalem gate nicknamed 'the needle's eye,' through which an unloaded, kneeling camel could just barely pass. That gate has no ancient attestation of any kind; the legend is traceable only to a medieval gloss, associated with either the Byzantine commentator Theophylact or Anselm of Canterbury, later carried forward through Thomas Aquinas's Catena Aurea [155]. The saying is much better read as genuine, deliberately impossible hyperbole - the largest land animal a Palestinian audience knew set against the smallest common household opening, the same rhetorical mode already seen in this book's 'log in your own eye' discussion at chapter 7 [43]. The disciples' shocked response one verse later, 'who then can be saved?' (19:25), only makes sense if the image was meant as genuinely impossible, not merely difficult."
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
$sortKey = $maxChapterSortKey + 1000.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch19NodeId $id $sortKey
    $sortKey += 100
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
Seed-Entity "Origen" "origen" "character" "Third-century Christian biblical scholar and theologian in Alexandria and Caesarea; per Eusebius, took Matthew 19:12's eunuch saying literally and had himself castrated as a young teacher."
Seed-Entity "David Instone-Brewer" "david-instone-brewer" "character" "Modern New Testament scholar; author of Divorce and Remarriage in the Bible, arguing Matthew's divorce exception clause situates Jesus within the live Hillel/Shammai halakhic dispute."
Seed-Entity "Peter Brown (Historian)" "peter-brown-historian" "character" "Modern historian of late antiquity; author of The Body and Society, documenting marriage and procreation as the near-universal expected life course in the Greco-Roman and Jewish world."
Seed-Entity "Peter Garnsey" "peter-garnsey" "character" "Modern classical historian; co-author with Richard Saller of The Roman Empire: Economy, Society and Culture, on land ownership and patronage as the primary form of ancient wealth."
Seed-Entity "Richard Saller" "richard-saller" "character" "Modern classical historian; co-author with Peter Garnsey of The Roman Empire: Economy, Society and Culture."
Seed-Entity "J. David Hester" "j-david-hester" "character" "Modern New Testament scholar; author of 'Eunuchs and the Postgender Jesus,' on the three eunuch categories named in Matthew 19:12."
Seed-Entity "Bruce Malina" "bruce-malina" "character" "Modern social-science New Testament scholar; co-author with Richard Rohrbaugh of the Social-Science Commentary on the Synoptic Gospels, reading Matthew 19:27-30 against Mediterranean patron-client reciprocity norms."
Seed-Entity "Richard Rohrbaugh" "richard-rohrbaugh" "character" "Modern social-science New Testament scholar; co-author with Bruce Malina of the Social-Science Commentary on the Synoptic Gospels."

$conn.Close()
Write-Host "DONE Matthew Chapter 19 depth pass."
