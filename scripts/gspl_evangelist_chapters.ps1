$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
$deg = [char]176
$GSPL = [guid]"0197E9C9-0003-7000-8000-000000000003"

function Sha256Hex([string]$t) {
    $s = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($s.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-','').ToLower()
}
function Exec-NonQuery([string]$sql, [hashtable]$p) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql
    foreach ($k in $p.Keys) { $c.Parameters.AddWithValue("@$k", $p[$k]) | Out-Null }
    $c.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $c = $conn.CreateCommand(); $c.CommandText = $sql; return $c.ExecuteScalar() }

function Next-Note([string]$notes) {
    return [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats bt ON bt.Id=bn.BeatId
WHERE bn.NodeId='$notes' AND bn.IsEnabled=1
  AND CHARINDEX(' ', bt.Text) > 1 AND LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) NOT LIKE '%[^0-9]%'
"@) + 1
}
function Add-Note([string]$notes, [int]$num, [string]$title, [string]$body) {
    $sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId='$notes'")
    $bnum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $id = [guid]::NewGuid()
    $text = "$num $em $title" + "`n`n" + $body.Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $bnum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, @S, 1)" @{ N = [guid]$notes; B = $id; S = $sk }
}
function Add-Chapter([string]$bookId, [string]$slugBase, [string]$title, [double]$sortKey, [string]$body) {
    $exists = [int](Exec-Scalar "SELECT COUNT(*) FROM Nodes WHERE ParentNodeId='$bookId' AND Title='$title'")
    if ($exists -gt 0) { Write-Host "    chapter already present, skip"; return $false }
    $nodeId = [guid]::NewGuid()
    Exec-NonQuery @"
SET QUOTED_IDENTIFIER ON;
INSERT INTO Nodes (Id, Slug, Title, Kind, Status, SortKey, StartedAt, CharsNarrated, CreatedAt, UpdatedAt,
                   NarratedBeatCount, TotalBeatsToNarrate, IsCanon, Version, UniverseId, NodeType, ParentNodeId)
VALUES (@Id, @Slug, @T, 'chapter', 'draft', @SK, SYSUTCDATETIME(), 0, SYSUTCDATETIME(), SYSUTCDATETIME(),
        0, 0, 0, 0, @Uni, 'chapter', @Parent)
"@ @{ Id = $nodeId; Slug = "$slugBase-the-evangelist"; T = $title; SK = $sortKey; Uni = $GSPL; Parent = [guid]$bookId }
    $bnum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $beatId = [guid]::NewGuid()
    $text = $body.Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $beatId; Text = $text; Hash = (Sha256Hex $text); Number = $bnum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, 100.0, 1)" @{ N = $nodeId; B = $beatId }
    Write-Host "    chapter added ($($text.Length) chars)"
    return $true
}

$TITLE = "The Evangelist: What Is Known, What Is Believed"

# ================================================================ MATTHEW
Write-Host "MATTHEW"
$N = "019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$a = Next-Note $N; $b = $a+1; $c = $a+2; $d = $a+3
Add-Note $N $a "The Gospels do not name their authors" @"
Papias of Hierapolis, quoted in Eusebius of Caesarea, Ecclesiastical History, Book 3, ch. 39, sections 15-16; with Irenaeus of Lyons, Against Heresies, Book 3, ch. 1, section 1 (c. 180 CE). No canonical Gospel names its author in its own text: all four are, in the technical phrase, formally anonymous. The titles ("According to Matthew," and so on) are present throughout the surviving Greek manuscript tradition and no manuscript preserves a rival name, which is the principal argument that the attributions are early. The counter-argument is that the earliest surviving copies with titles postdate the composition by generations, that no Christian writer clearly cites the Gospels by these names before Irenaeus in the late second century, and that Justin Martyr (c. 156 CE) refers to them as "memoirs of the apostles" without attaching the four names. Mainstream historical-critical scholarship therefore treats the names as second-century attributions to originally unsigned works; confessional scholarship treats them as preserving genuine early knowledge of authorship. Both positions are held by serious specialists, and the manuscript evidence is genuinely compatible with either.
"@
Add-Note $N $b "Papias on a Matthew who wrote in Hebrew" @"
Papias, in Eusebius, Ecclesiastical History, Book 3, ch. 39, section 16: "Matthew compiled the sayings in the Hebrew language, and each interpreted them as best he could." This is the earliest surviving statement about a Gospel by Matthew, and it does not straightforwardly describe the book we have. Canonical Matthew is composed in Greek, shows no signs of being a translation from Hebrew or Aramaic, and $em on the dominant scholarly reconstruction $em uses Greek Mark as one of its sources. Proposed resolutions include that Papias is describing a lost earlier collection of sayings, that "in the Hebrew language" means something closer to "in a Hebrew manner or arrangement," or that Papias is simply mistaken. The passage is cited by every side of the authorship debate and settles none of it.
"@
Add-Note $N $c "The traditions of Matthew's death do not agree with each other" @"
The Roman Martyrology places the apostle Matthew's martyrdom in Ethiopia; other ancient and medieval traditions place it in Persia, in Parthia, or in the city of Hierapolis, and the reported manner of death varies across the sources between the sword, the spear, stoning, and burning. There is no first-century or second-century account of Matthew's death, and no independent documentary or archaeological evidence for any version. The disagreement is itself the finding: a martyrdom that had been remembered in detail would not have produced this many mutually exclusive geographies.
"@
Add-Note $N $d "Where the relics are said to be" @"
Salerno Cathedral (Duomo di Salerno), Salerno, Campania, Italy; the relics attributed to the evangelist Matthew are venerated in the cathedral crypt, having been brought to Salerno in the tenth century. Coordinates given for the old city of Salerno are approximately 40.683$deg N, 14.767$deg E; readers should note that this is a locality-level fix rather than a surveyed point for the crypt itself. No scientific examination of these remains comparable to the 1998 study of the Padua relics attributed to Luke has been published.
"@
Add-Chapter "019FA049-322F-75EF-AAB7-0C0DE8DBDB85" "matthew" $TITLE 60.0 @"
Before the book, the man $em and this is the first place where history and heritage come apart, because the honest answer to "who wrote this?" is that nobody knows.

Start with what the document itself says. It does not say who wrote it. No canonical Gospel does. All four are formally anonymous: no author names himself, claims an office, or signs off. The name at the top of your Bible is a title, and every surviving Greek manuscript carries one $em which is the strongest argument that the attribution is early, since no manuscript anywhere preserves a competing name. Against that: the earliest copies bearing titles are generations later than the composition, no Christian writer clearly refers to these books by these four names until Irenaeus around 180 CE, and Justin Martyr, writing a generation before him, calls them "memoirs of the apostles" without attaching a single one of the names. Mainstream critical scholarship concludes the names were attached in the second century to books that circulated unsigned. Confessional scholarship concludes the names preserve real memory. The evidence genuinely permits both, and anyone who tells you otherwise is selling something [$a].

Then there is the oldest specific claim, and it is a strange one. Papias, writing early in the second century and preserved only because Eusebius quoted him two centuries later, says that "Matthew compiled the sayings in the Hebrew language, and each interpreted them as best he could." That sentence is the foundation of the entire tradition of Matthean authorship, and it does not describe this book. This Gospel is written in competent Greek, shows no sign of being translated from a Semitic original, and $em on the standard reconstruction of how the Gospels relate $em draws on Greek Mark as a source. Something is being described here. Whether it is the book in your hands is exactly the question [$b].

Who was he supposed to be? A tax collector. This is worth pausing on, because this Gospel does something none of the others do: Mark and Luke both name the tax collector called from his booth "Levi," and this Gospel calls him "Matthew" (9:9), then goes out of its way in the roster of the Twelve to append the job $em "Matthew the tax collector" (10:3), a label no other list bothers with. A book that quietly renames a man and then insists on his disreputable profession is doing something deliberate. What it is not doing is signing itself.

And his death? Here the record does not thin out so much as fly apart. The Roman Martyrology places his martyrdom in Ethiopia. Other traditions put it in Persia, in Parthia, at Hierapolis. The manner varies too $em sword, spear, stoning, fire, depending on which account you pick up. There is no first- or second-century narrative of Matthew's death at all, and no independent evidence for any version of it. That disagreement is itself informative: a death that had genuinely been remembered would not have generated four incompatible countries [$c].

Does he have a grave? There is a tomb, and you can visit it. Relics venerated as Matthew's lie in the crypt of the cathedral at Salerno, on the Campanian coast south of Naples, brought there in the tenth century (Salerno old city, approximately 40.683$deg N, 14.767$deg E $em a locality fix, not a survey of the crypt). The crypt is real, the veneration is nearly a thousand years old, and the identification of the bones has never been subjected to the kind of published scientific examination that the relics attributed to Luke received in Padua in 1998 [$d].

So: we do not know who wrote this Gospel, we do not know when or where he died, and we cannot demonstrate whose bones are in Salerno. That is the history. The heritage $em an apostle who left a tax booth, wrote in Hebrew, and died a martyr in Ethiopia $em is a coherent, moving, and largely unverifiable story assembled over centuries from fragments like the one sentence of Papias above.

None of which touches the book. Whoever wrote it knew his scriptures with a rabbi's fluency, built his genealogy on a number, and produced the most carefully structured of the four Gospels. That is visible on every page and needs no author's name to be true. The rest of this volume is about the world he was describing, which can be checked, rather than the man himself, who largely cannot.
"@

# ================================================================ MARK
Write-Host "MARK"
$N = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"
$a = Next-Note $N; $b = $a+1; $c = $a+2; $d = $a+3
Add-Note $N $a "The Gospels do not name their authors" @"
Papias of Hierapolis, quoted in Eusebius of Caesarea, Ecclesiastical History, Book 3, ch. 39, sections 15-16; with Irenaeus of Lyons, Against Heresies, Book 3, ch. 1, section 1 (c. 180 CE). No canonical Gospel names its author within its own text. The titles appear throughout the surviving Greek manuscript tradition with no rival names preserved, which is the main argument for their antiquity; against it, the earliest titled copies postdate composition by generations, no writer clearly cites the Gospels by these names before Irenaeus, and Justin Martyr (c. 156 CE) calls them "memoirs of the apostles" without attaching the four names. Mainstream historical-critical scholarship reads the names as second-century attributions; confessional scholarship reads them as preserved early knowledge. Serious specialists hold both.
"@
Add-Note $N $b "Papias on Mark as Peter's interpreter" @"
Papias, in Eusebius, Ecclesiastical History, Book 3, ch. 39, section 15, reporting what he had from a figure he calls the Elder: "Mark, having become the interpreter of Peter, wrote down accurately whatever he remembered, though not in order, of the things said or done by the Lord." Papias adds that Mark had not himself heard or followed Jesus. This is the single most important early statement about the origin of any Gospel, and its evidentiary chain is worth stating plainly: Eusebius, writing in the fourth century, quotes Papias, writing in the early second, reporting what an unnamed Elder told him about events of the 30s. It is a genuinely early tradition and it is third-hand.
"@
Add-Note $N $c "The martyrdom at Alexandria is late tradition" @"
The tradition that Mark founded the church at Alexandria and was killed there $em dragged through the streets by a rope around his neck, conventionally dated to the 60s CE $em is preserved in the apocryphal Acts of Mark and in Coptic tradition, with Eusebius (Ecclesiastical History, Book 2, ch. 16) supplying the earlier and much barer claim that Mark preached in Egypt and founded churches there. No first-century source records Mark's death, and no independent documentary or archaeological evidence attests it. The Coptic Orthodox Church, which regards Mark as its founder, preserves the fullest form of the tradition.
"@
Add-Note $N $d "The theft of the relics, and their partial return" @"
Saint Mark's Basilica (Basilica di San Marco), Piazza San Marco, Venice, Italy: approximately 45.4345$deg N, 12.3396$deg E. According to the Venetian tradition, in 828 CE two merchants, Buono da Malamocco and Rustico da Torcello, removed the body attributed to Mark from Alexandria and shipped it to Venice, concealing it under a cargo of pork so that Muslim inspectors would not examine it closely. A church was raised for the relics by 836; the present basilica dates from 1063. In 1968 Pope Paul VI returned a portion of the relics to the Coptic Orthodox Church in Alexandria. The removal is well documented as an event in Venetian history; the identity of the remains has never been scientifically tested.
"@
Add-Chapter "019FA966-2F28-7A30-9662-F0F6F33C4D54" "mark" $TITLE 60.0 @"
Before the book, the man $em and with Mark the gap between what is known and what is believed is unusually easy to measure, because the tradition is early, specific, and third-hand.

The document is anonymous. No canonical Gospel names its author, and this one is no exception: it opens on a wilderness and never once tells you who is writing. The title is a title. Every surviving Greek manuscript carries one and none preserves a rival name, which is the best argument that the attribution is old; against that, the earliest titled copies are generations later than the text, and nobody clearly cites these books by these four names until Irenaeus, around 180 CE [$a].

Then comes the sentence that built the tradition. Papias, early in the second century, says he was told by a figure he calls the Elder that "Mark, having become the interpreter of Peter, wrote down accurately whatever he remembered, though not in order," and that Mark himself had neither heard nor followed Jesus. That is a remarkable claim, and it is remarkable in both directions. It gives this Gospel a direct line to an eyewitness $em Peter, remembering aloud, with Mark taking it down. It also concedes, in the same breath, that the order is not reliable and that the author was not there. No later Christian writer would have invented a recommendation that undercuts its own book.

It is worth being exact about how that reaches us, though. Eusebius, in the fourth century, quotes Papias, in the early second, reporting what an unnamed Elder said about the 30s. Each link may be sound. It is still a chain of three [$b].

Who was he? Traditionally John Mark $em the man whose mother's house in Jerusalem is a meeting place in Acts 12:12, who quarrels with Paul and is abandoned mid-journey in Acts 15:37-39, and who is greeted as "my son Mark" at the end of 1 Peter. Whether the Mark of Acts, the Mark of 1 Peter, and Papias's Mark are one man is an inference, and a reasonable one, and an inference all the same. It is not a small thing to notice that the earliest Gospel is attributed not to an apostle but to an assistant with a documented history of walking out on an assignment.

His death: tradition has him founding the church in Alexandria and being killed there in the 60s, dragged through the streets with a rope around his neck. Eusebius supplies the early, bare version $em Mark preached in Egypt $em and the vivid martyrdom comes from the apocryphal Acts of Mark and Coptic tradition. No first-century source mentions it. The Coptic Orthodox Church, which counts him as its founder, holds the fullest form [$c].

His grave is the best story in this chapter, and almost all of it is documented $em just not the part people assume. In 828 two Venetian merchants took the body venerated as Mark's out of Alexandria and carried it home by ship, and the tradition preserves the method: they packed it under pork, correctly judging that Muslim customs officials would not rummage through it. Venice built a church for the relics by 836 and the present Basilica of San Marco over them by 1063, where they remain, in the Piazza San Marco (approximately 45.4345$deg N, 12.3396$deg E). You can stand on the spot this afternoon. In 1968 Pope Paul VI gave a portion back to the Coptic Church in Alexandria, so the remains have now been moved by politics twice, eleven centuries apart [$d].

What is established is the theft, the shipping, the basilica, and the 1968 restitution $em all of it ordinary, checkable history. What is not established, and has never been tested, is whether the bones under the high altar of San Marco belonged to the man who wrote this book. Venice's patron saint arrived as smuggled cargo, which is a more interesting fact than the sanitised version and is also the true one.

The book itself needs none of this. It is the shortest and roughest of the four, and it keeps details a tidier writer would have deleted. That roughness is the reason historians read it first.
"@

# ================================================================ LUKE
Write-Host "LUKE"
$N = "019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$a = Next-Note $N; $b = $a+1; $c = $a+2; $d = $a+3
Add-Note $N $a "The Gospels do not name their authors" @"
Papias of Hierapolis, quoted in Eusebius of Caesarea, Ecclesiastical History, Book 3, ch. 39, sections 15-16; with Irenaeus of Lyons, Against Heresies, Book 3, ch. 1, section 1 (c. 180 CE). No canonical Gospel names its author within its own text; all four are formally anonymous. The titles are universal in the surviving Greek manuscript tradition and no rival names are preserved, which is the principal argument for their antiquity. Against it: the earliest titled copies postdate composition by generations, no Christian writer clearly cites the Gospels by these four names before Irenaeus in the late second century, and Justin Martyr (c. 156 CE) speaks of "memoirs of the apostles" without naming them. Mainstream historical-critical scholarship treats the names as second-century attributions; confessional scholarship treats them as early and reliable.
"@
Add-Note $N $b "What the author says about himself, and does not" @"
Luke 1:1-4 with Acts 1:1; and the first-person plural passages at Acts 16:10-17, 20:5-15, 21:1-18, and 27:1-28:16; with Colossians 4:14, 2 Timothy 4:11, and Philemon 24. The author of this Gospel writes a first-person preface, addresses a named recipient, states that many others had already written accounts, and says the material was handed down to him by others $em explicitly not claiming to be an eyewitness of the events he narrates. The identification of that author with the "Luke, the beloved physician" greeted in Colossians rests on combining the Pauline greetings with the "we" passages of Acts, in which the narrator appears to travel with Paul. This is the strongest internal case for authorship among the four Gospels and it remains an inference: the text names Theophilus, and never names its writer.
"@
Add-Note $N $c "The traditions of Luke's death" @"
Traditions variously hold that Luke was martyred at Thebes in Boeotia, central Greece, and that he died of old age, with an age at death of eighty-four given in some accounts. No first-century source records his death, and the competing versions cannot be reconciled from the evidence. The Boeotian Thebes tradition is materially significant for one reason unconnected to martyrdom: it identifies a specific tomb, which is where the relics later venerated at Padua are said to have come from.
"@
Add-Note $N $d "The 1998 examination of the Padua relics" @"
Abbey of Santa Giustina, Prato della Valle, Padua, Veneto, Italy: approximately 45.3964$deg N, 11.8797$deg E. In 1992 the Orthodox Metropolitan of Thebes requested a relic of Luke for the tomb at Thebes; the Bishop of Padua, Antonio Mattiazzo, agreed on condition that the remains first be examined. The lead coffin was opened on 17 September 1998 and studied by teams drawn from the universities of Padua, Ferrara, Florence, Rome, Calabria, and Geneva. Findings as reported: radiocarbon dating of a tooth indicated a death between roughly 72 and 416 CE; mitochondrial DNA from the tooth $em inherited through the maternal line $em was found to resemble Syrian populations more closely than Greek ones; the coffin's dimensions correspond to the empty tomb at Thebes from which the relics are said to have been taken. The skeleton lacked its skull, which is consistent with the separate tradition that the skull was given to Charles IV and is held at Prague. The investigators did not claim to have identified the individual, and the results cannot do so; they are consistent with the traditional account and exclude neither it nor several alternatives.
"@
Add-Chapter "019FA969-3232-772B-998A-BB2D5158F96E" "luke" $TITLE 60.0 @"
Before the book, the man $em and of the four evangelists, Luke is the one where evidence and tradition can actually be set side by side, because someone opened the coffin.

The document is anonymous, like the other three. No canonical Gospel names its writer, and the titles, though universal in the manuscripts, are attached from outside the text [$a]. But this Gospel gets closer to a signature than any of the others. It opens with a first-person preface: many have undertaken accounts, the author has investigated, and he is setting it down in order for a named recipient, Theophilus, so that he may know the reliability of what he has been taught (1:1-4). He also says, without hedging, that the material was handed down to him by others. Whoever wrote this was not claiming to have been there.

The traditional identification $em Luke, the physician who travelled with Paul $em is built by combining two things: the greetings in Paul's letters that name a Luke, "the beloved physician," and the passages in Acts where the narration abruptly shifts to "we," as though the writer had joined the journey. Put together, they make the best internal case for authorship anywhere in the four Gospels. They are still an inference. The text names its reader and never names its author [$b].

His death is the usual fog. One tradition has him martyred at Thebes in Boeotia; another has him dying of old age, at eighty-four in some accounts. Nothing first-century survives, and the versions cannot be reconciled. The Theban tradition matters anyway, for a reason that has nothing to do with martyrdom: it points at a specific tomb [$c].

Which is where this becomes the most interesting relic story in the New Testament. Remains venerated as Luke's lie in the Abbey of Santa Giustina in Padua, facing the Prato della Valle (approximately 45.3964$deg N, 11.8797$deg E). In 1992 the Orthodox Metropolitan of Thebes asked Padua for a relic to place in the Theban tomb. The Bishop of Padua agreed $em on the condition that the bones be examined first. On 17 September 1998, four-hundred-year-old seals were cut and a lead coffin was opened in front of scientists from Padua, Ferrara, Florence, Rome, Calabria, and Geneva.

Here is what they found, and it is worth reading slowly, because this is what honest evidence actually looks like. Radiocarbon dating of a tooth put the death of its owner somewhere between roughly 72 and 416 CE $em a three-century window that includes the traditional date and a great deal else. Mitochondrial DNA from the same tooth, inherited down the maternal line, more closely resembled Syrian populations than Greek ones, which is quietly striking, because tradition makes Luke a native of Antioch, in Syria. The coffin's dimensions matched the empty tomb at Thebes it is supposed to have come from. And the skeleton had no skull $em consistent with the separate, independent tradition that the skull went to Charles IV and is in Prague [$d].

Now the discipline. None of that identifies anybody. A first-century Syrian man's bones are not Luke's bones merely because Luke was said to be Syrian; the window is wide, the sample is one tooth, and thousands of people fit the description. The investigators claimed no identification, and were right not to. What the examination did establish is narrower and more valuable: the remains are genuinely ancient, they are consistent with the tradition at every point tested, and nothing about them refutes it. That is a real result. It is not the result a pilgrimage brochure would print, because "consistent with, and not disproven" does not fit on a plaque.

That is the whole distinction this series is built on, sitting in a lead box in Padua. Heritage says: the body of the evangelist Luke. History says: a man who died between 72 and 416 CE, probably of Syrian maternal descent, buried in a coffin that fits a tomb in Thebes, missing the skull that Prague claims to have. The second sentence is less satisfying and enormously more interesting, and it is the one the evidence will actually support.
"@

# ================================================================ JOHN
Write-Host "JOHN"
$N = "019FA96D-7D48-75E0-9BD9-2190171276DC"
$a = Next-Note $N; $b = $a+1; $c = $a+2; $d = $a+3
Add-Note $N $a "The Gospels do not name their authors" @"
Papias of Hierapolis, quoted in Eusebius of Caesarea, Ecclesiastical History, Book 3, ch. 39, sections 15-16; with Irenaeus of Lyons, Against Heresies, Book 3, ch. 1, section 1 (c. 180 CE). No canonical Gospel names its author within its own text. The titles are universal in the surviving Greek manuscript tradition with no rival names preserved, the principal argument for their antiquity; against it, the earliest titled copies postdate composition by generations, no writer clearly cites the Gospels by these four names before Irenaeus, and Justin Martyr (c. 156 CE) refers to "memoirs of the apostles" without attaching them. Mainstream historical-critical scholarship treats the names as second-century attributions; confessional scholarship treats them as early and reliable.
"@
Add-Note $N $b "The Gospel points at a witness it never names" @"
John 13:23, 19:26, 20:2, 21:7, and 21:20-24. This Gospel repeatedly identifies a figure only as "the disciple whom Jesus loved," and closes by asserting that this disciple is the one "bearing witness about these things, and who has written these things" (21:24) $em while never supplying his name. The identification of that disciple with John the son of Zebedee is external to the text and appears in the late second century, most influentially in Irenaeus. The Gospel names many minor figures precisely (Nicodemus, Lazarus, Malchus, Nathanael) and withholds only this one.
"@
Add-Note $N $c "Papias appears to know two Johns" @"
Papias, in Eusebius, Ecclesiastical History, Book 3, ch. 39, sections 4-7. In listing his sources Papias mentions John among the disciples of the Lord and then, separately, a figure he calls "the Elder John," in a way that led Eusebius to conclude that two different men named John were remembered at Ephesus, and to propose that the second wrote Revelation. Whether Papias intends one man or two has been argued since antiquity. The significance for authorship is direct: the ancient sources closest to the tradition were already uncertain which John was meant.
"@
Add-Note $N $d "An empty tomb at Ephesus" @"
Basilica of St John, Ayasuluk Hill, Selcuk (ancient Ephesus), Izmir Province, Turkey; Selcuk town is at approximately 37.950$deg N, 27.368$deg E, with the ancient city site nearby at approximately 37.941$deg N, 27.342$deg E. Tradition holds that John died at Ephesus of old age rather than by martyrdom $em uniquely among the apostles $em and was buried on the hill; the emperor Justinian raised a large basilica over the traditional grave in the sixth century, the ruins of which are the visible site today. The tomb was reported empty when opened in antiquity. A medieval pilgrim tradition held that a fine dust called manna rose from the grave on his feast day and was collected for healing. The ruins, the basilica's sixth-century date, and the tradition are all documented; there are no remains at the site to examine.
"@
Add-Chapter "019FA96B-CAD8-7769-BF17-363E3641048E" "john" $TITLE 60.0 @"
Before the book, the man $em and John is the hardest of the four, because this Gospel goes out of its way to tell you that a specific eyewitness stands behind it and then refuses, across twenty-one chapters, to say who he was.

The anonymity first. Like the other three, this Gospel does not name its author, and the title comes from outside the text [$a]. But it does something the others do not: it repeatedly gestures at a figure called only "the disciple whom Jesus loved" $em reclining beside Jesus at the last meal, standing at the cross, outrunning Peter to the tomb $em and then ends by asserting that this is the disciple "bearing witness about these things, and who has written these things" (21:24). That is a claim of eyewitness authorship, stated plainly, about a man left deliberately unnamed.

The withholding is conspicuous, because this Gospel is otherwise the most generous of the four with names. It gives us Nicodemus, Lazarus, Nathanael, and even the name of the man whose ear was cut off in the garden $em Malchus, a servant nobody else bothers to identify. A writer that precise about minor figures is not forgetting to name his own source [$b].

The identification with John the son of Zebedee is second-century, and Irenaeus is its most influential voice. It may well be right. But the ancient evidence is muddier than the tradition suggests, and the muddle starts early: Papias, our earliest witness, seems to distinguish John the disciple from a separate figure he calls "the Elder John," and Eusebius read him as describing two men of that name remembered at Ephesus $em then used the distinction to assign Revelation to the second. Whether Papias meant one John or two has been argued for eighteen centuries. The people closest to the tradition were already unsure which John they were talking about [$c].

His death is the exception that proves the rule about these traditions. Every other apostle is given a martyrdom; John, almost uniquely, is not. The tradition has him growing old at Ephesus and dying there in the ordinary way $em which is a strange thing for a hagiography to concede, and therefore mildly interesting as evidence, since the pressure of the genre runs the other way.

And his grave? He has one, on Ayasuluk Hill above Selcuk in western Turkey, beside the ruins of Ephesus (Selcuk at approximately 37.950$deg N, 27.368$deg E; the ancient city nearby at approximately 37.941$deg N, 27.342$deg E). In the sixth century the emperor Justinian built an enormous basilica over the traditional burial place, and its ruins are what you walk through today. The tomb is there. It is also, and has been since antiquity when it was opened, empty. Medieval pilgrims believed a fine dust called manna rose from it on his feast day and carried it off across Christendom to heal the sick [$d].

So the final tally for the Gospel most insistent on eyewitness testimony: an author who tells you he was there and will not tell you his name; an early tradition that cannot decide whether he was one man or two; the only apostle allowed to die of old age; and a marked, monumental, empty grave.

There is no need to force irony onto that. A book that ends on a tomb found open and unoccupied is commemorated by a tomb found open and unoccupied, and every part of that sentence is a matter of record.
"@

$conn.Close()
Write-Host "DONE"
