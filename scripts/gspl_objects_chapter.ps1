$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
$deg = [char]176
$cu  = [char]179   # superscript 3
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
function Add-ObjectsChapter([string]$bookId, [string]$slugBase, [double]$sortKey, [string]$body) {
    $t = "The Objects: What Survives"
    $exists = [int](Exec-Scalar "SELECT COUNT(*) FROM Nodes WHERE ParentNodeId='$bookId' AND Title='$t'")
    if ($exists -gt 0) { Write-Host "    already present, skip"; return }
    $nodeId = [guid]::NewGuid()
    Exec-NonQuery @"
SET QUOTED_IDENTIFIER ON;
INSERT INTO Nodes (Id, Slug, Title, Kind, Status, SortKey, StartedAt, CharsNarrated, CreatedAt, UpdatedAt,
                   NarratedBeatCount, TotalBeatsToNarrate, IsCanon, Version, UniverseId, NodeType, ParentNodeId)
VALUES (@Id, @Slug, @T, 'chapter', 'draft', @SK, SYSUTCDATETIME(), 0, SYSUTCDATETIME(), SYSUTCDATETIME(),
        0, 0, 0, 0, @Uni, 'chapter', @Parent)
"@ @{ Id = $nodeId; Slug = "$slugBase-the-objects"; T = $t; SK = $sortKey; Uni = $GSPL; Parent = [guid]$bookId }
    $bnum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $beatId = [guid]::NewGuid()
    $text = [regex]::Replace($body.Trim(), "(?<!`n)`n(?!`n)", ("`n" + "`n"))
    $text = [regex]::Replace($text, "`n{3,}", ("`n" + "`n")).Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $beatId; Text = $text; Hash = (Sha256Hex $text); Number = $bnum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, 100.0, 1)" @{ N = $nodeId; B = $beatId }
    Write-Host "    chapter added ($($text.Length) chars)"
}

# ---- note bodies, shared across the four books ----
$nbLonginus = @"
The Gospel of Nicodemus (also called the Acts of Pilate), an apocryphal work of roughly the fourth to fifth century; and the Rabbula Gospels, an illuminated Syriac manuscript completed in 586 CE and held at the Biblioteca Medicea Laurenziana, Florence. No canonical Gospel names the soldier who pierces Jesus's side; John 19:34 has only "one of the soldiers." The name Longinus appears in the Gospel of Nicodemus and, in Greek characters (LOGINOS), above the head of the lance-bearing soldier in a miniature of the Rabbula Gospels $em though specialists note the inscription may be a later addition to the manuscript. The name is widely held to derive from the Greek lonche, meaning lance or spear, so that the figure may have been named from the weapon rather than remembered. Later tradition supplies a first name, Cassius, and merges him with the centurion who confesses at the cross (Mark 15:39), a different figure in the text. There is no Roman documentary record of him.
"@
$nbLances = @"
Three relics are commonly treated as candidate Holy Lances. The Vatican lance is kept beneath the dome of St Peter's Basilica, Rome, having been sent to Pope Innocent VIII in 1492 by the Ottoman Sultan Bayezid II; the Roman Catholic Church makes no claim as to its authenticity. The Vienna lance is displayed in the Weltliche Schatzkammer (Imperial Treasury) of the Hofburg, Vienna; it is a winged lance of a form characteristic of the Carolingian period, more elaborate and ceremonial than a soldier's weapon. The Echmiadzin lance is held at Vagharshapat, Armenia; it is flat and diamond-shaped rather than pointed, with a conical iron base, and its history connects it to the lance "discovered" at Antioch in 1098 during the First Crusade by Peter Bartholomew, who reported a vision of Saint Andrew disclosing its location. Relics claimed as the lance were also recorded historically at Paris and Nuremberg. None has been demonstrated authentic.
"@
$nbVienna = @"
Robert Feather, metallurgist, examining the Vienna lance for a television documentary in January 2003 with access permitting removal of the gold and silver binding bands, and using X-ray diffraction and fluorescence among other non-invasive methods. He dated the main body of the spearhead to the seventh century CE at the earliest $em close to, and slightly earlier than, the Kunsthistorisches Museum's own estimate. Feather also reported that an iron pin set into the blade and framed by small brass crosses, long claimed to be a nail from the crucifixion, is "consistent" in length and shape with a first-century Roman nail. Consistency of that kind is not identification: it establishes that the pin could be a Roman nail, not that it is the one.
"@
$nbTrueCross = @"
John Calvin, Traite des reliques (Treatise on Relics), 1543, in which he argues that the fragments of the True Cross in circulation would together fill a ship; against Charles Rohault de Fleury, Memoire sur les instruments de la Passion de N.-S. Jesus-Christ (Paris, 1870). Rohault de Fleury, an architect and archaeologist, catalogued and measured the fragments he could locate. He estimated the volume of a complete cross of appropriate dimensions at roughly 36,000 cubic centimetres, and the total volume of all the alleged fragments he was able to verify at under 4,000 cubic centimetres $em under a tenth of one cross. His survey has not been superseded. The point cuts against the sceptical claim rather than the devotional one: the famous "shipload" is not supported by measurement.
"@
$nbTitulus = @"
F. Bella and C. Azzi, "14C Dating of the 'Titulus Crucis'," Radiocarbon, vol. 44, no. 3 (2002), pp. 685-689, reporting work carried out in the radiocarbon laboratory of the "E. Amaldi" Physics Department of Roma Tre University using liquid scintillation spectrometry. The Titulus Crucis, a wooden board venerated as the placard bearing the charge fixed above the cross, is held at the Basilica of Santa Croce in Gerusalemme, Rome. The radiocarbon result dates the wood to between 980 and 1146 CE.
"@
$nbCrown = @"
Notre-Dame de Paris and the associated documentary record of the acquisition. Tradition traces the relic to Jerusalem from the late fourth century and to Constantinople by the tenth. In 1238 Louis IX of France acquired it from Baldwin II, the Latin Emperor of Constantinople, for a sum recorded as approximately 135,000 livres $em on the order of half the annual revenue of the French crown $em and it entered Paris on 19 August 1239, the king walking barefoot in a linen tunic to carry it. The Sainte-Chapelle was built to house it. The object itself is a circlet of rushes bound with gold thread and contains no thorns; the thorns were detached and distributed as separate relics over the centuries. It was carried out of Notre-Dame during the fire of 15 April 2019 and returned after the cathedral's restoration.
"@
$nbShroud = @"
On the radiocarbon dating: the 1988 analysis, carried out by laboratories at Oxford, Arizona, and Zurich, returned a date range of approximately 1260-1390 CE for the linen. On DNA: Gianni Barcaccia, Alessandro Achilli and colleagues, "Uncovering the sources of DNA found on the Turin Shroud," Scientific Reports 5 (2015), article 14484 (with a published Author Correction, 2021), applying metagenomic sequencing to dust from a 1978 sampling. The study recovered human mitochondrial lineages distributed across many populations $em on the order of 55.6 per cent corresponding to the Near East, 38.7 per cent to India, and under 5.6 per cent to Europe $em together with plant DNA from species native to Europe, the Mediterranean, North America, and East Asia. The finding is a record of contamination by the many people and environments that handled the cloth over centuries; it does not identify an individual and does not date the linen. A further study published in 2026 claiming to establish Middle Eastern presence has been disputed by other specialists, who hold that the DNA cannot establish the relic's origins.
"@
$nbSudarium = @"
Camara Santa, Cathedral of San Salvador, Oviedo, Asturias, Spain. The Sudarium is a small cloth venerated as having covered the head of Jesus, and it differs from the Shroud of Turin in one important respect: it carries a documentary trail centuries older. A cloth of this description at Jerusalem is mentioned by the pilgrim Antoninus of Piacenza around 570 CE; the tradition records its removal ahead of the Persian invasion of 614 and a route through Alexandria, North Africa, and Spain; Alfonso II of Asturias is recorded as building the Camara Santa to house it around 840; and a formal relic inventory of 14 March 1075 under Alfonso VI lists it explicitly. Against that: radiocarbon results reported at congresses in 1994 gave approximately 653-786 CE (Toronto) and 642-869 CE (Tucson), centuries later than the first century, and the scientist involved described the results as imprecise and in need of further testing. Pollen analysis by Max Frei was reported as identifying species consistent with the traditional route; Frei's palynological methods have been widely criticised elsewhere and his conclusions are not treated as secure by specialists.
"@

$core = @"
Start with the man who is supposed to have held the spear, because he is the clearest case in this book of a fact being manufactured rather than remembered.

The canonical Gospels do not name him. John says only that "one of the soldiers" pierced Jesus's side. The name Longinus turns up in the Gospel of Nicodemus, an apocryphal work of the fourth or fifth century, and in a miniature of the Rabbula Gospels, a Syriac manuscript completed in 586 CE, where LOGINOS is written in Greek letters above the soldier's head $em and even there, specialists note the inscription may have been added later. Then comes the detail that settles the matter for most historians: the name is almost certainly derived from lonche, the ordinary Greek word for a lance. Later tradition adds a first name, Cassius, and merges him with the centurion who speaks at the cross, who is a different man in the text [$([char]0)LONGINUS].

Read the sequence forward. A nameless soldier. A spear. A name generated from the spear. A saint with a biography, a feast day, a conversion narrative, and relics. There is no Roman record of him at any point. Nothing was falsified and nobody lied; a gap was filled, the filling was repeated, and the repetition became a person.

The spear itself did better $em three times over. Three relics are treated as candidates for the Holy Lance. One sits beneath the dome of St Peter's in Rome, sent to Pope Innocent VIII in 1492 by the Ottoman Sultan Bayezid II; the Catholic Church makes no claim about its authenticity. One is displayed in the Imperial Treasury of the Hofburg in Vienna, and it is a winged lance of Carolingian type $em far too ornate to be a soldier's field weapon. One is at Echmiadzin in Armenia, flat and diamond-shaped rather than pointed, with a history connecting it to the lance "found" at Antioch in 1098 by a crusader who reported a vision telling him where to dig. Paris and Nuremberg also held claims once [$([char]0)LANCES].

The Vienna lance is the one that has been tested, and the result is a model of how evidence actually behaves. In 2003 a metallurgist was allowed to remove its gold and silver bands and examine it directly. The spearhead dates to the seventh century CE at the earliest $em six hundred years too late. But an iron pin set into the blade behind small brass crosses, long claimed to be a nail from the crucifixion, turned out to be "consistent" in length and shape with a genuine first-century Roman nail [$([char]0)VIENNA].

Sit with how little that means and how much it feels like it means. Consistent is not identified. Millions of Roman nails were made. What the test established is that a first-century nail may be embedded in a seventh-century spearhead, which is interesting, and is not the same sentence as "this is the nail."

Now the wood, where the best-known fact of all turns out to be wrong $em and wrong in the sceptic's favour, which is why it belongs here. Everyone has heard Calvin's line, from his 1543 treatise on relics, that the fragments of the True Cross in circulation would fill a ship. In 1870 an architect and archaeologist named Charles Rohault de Fleury did the obvious thing and measured them. A complete cross of plausible dimensions comes to roughly 36,000 cubic centimetres. Every fragment he could locate and verify, added together, came to under 4,000 $em less than a tenth of a single cross. His survey has never been overturned [$([char]0)CROSS].

So the famous debunking is itself a myth, and the actual number refutes it. That does not make the fragments authentic. It makes the shipload a story people repeat because it is satisfying, which is precisely the behaviour this book is about, running in the opposite direction from the one readers expect.

The board is a different matter. A wooden placard venerated as the titulus $em the notice naming the charge, which Matthew, Mark, Luke, and John all report being fixed above him $em is kept at Santa Croce in Gerusalemme in Rome. It was radiocarbon dated at Roma Tre University in 2002 and published in the journal Radiocarbon. The wood dates to between 980 and 1146 CE [$([char]0)TITULUS].

The crown has the best paperwork and the strangest physical reality. Its trail is genuinely traceable: venerated at Jerusalem from the late fourth century, at Constantinople by the tenth, and in 1238 bought by Louis IX of France from Baldwin II, the cash-poor Latin Emperor of Constantinople, for something on the order of 135,000 livres $em roughly half the annual revenue of the French crown. It entered Paris on 19 August 1239 with the king carrying it barefoot in a linen tunic, and the Sainte-Chapelle was built to house it. Firefighters carried it out of Notre-Dame during the fire of April 2019 [$([char]0)CROWN].

And the object at the centre of all that? A circle of rushes bound with gold thread. It has no thorns. They were detached one at a time over the centuries and distributed as separate relics, so that the Crown of Thorns now contains none. Half a kingdom's yearly income was paid for a reed circlet, and the payment is the best-documented thing about it.

Which brings us to the cloth, and to the most tested object in Christendom. The Shroud of Turin was radiocarbon dated in 1988 by three laboratories $em Oxford, Arizona, Zurich $em to roughly 1260 to 1390 CE. That result is the central fact and every subsequent argument is conducted around it.

The DNA work is worth understanding precisely, because it is usually reported as something it is not. A 2015 study in Scientific Reports sequenced dust from a 1978 sampling and found human mitochondrial lineages from many populations at once $em on the order of 55 per cent Near Eastern, 39 per cent Indian, under 6 per cent European $em along with plant DNA from Europe, the Mediterranean, North America, and East Asia. That is a record of everybody who ever handled the thing and everywhere it was ever kept. It cannot identify a person and it cannot date the linen. A 2026 study claiming to establish Middle Eastern presence has been contested by other specialists on exactly this ground [$([char]0)SHROUD].

Its lesser-known companion deserves better than it usually gets. The Sudarium of Oviedo, in the Camara Santa of the cathedral there, is a small cloth said to have covered the head. Its documentary trail is genuinely older than the Shroud's: a cloth of this description is mentioned at Jerusalem around 570 CE, its removal ahead of the Persian invasion of 614 is recorded, a king of Asturias built a chamber for it around 840, and a dated inventory of 14 March 1075 lists it by name. Then radiocarbon put the fabric at roughly 650 to 870 CE $em better than the Shroud by some centuries, and still far too late [$([char]0)SUDARIUM].

Set them all in a row and a pattern emerges that no individual object shows on its own. Every relic of the Passion that has been submitted to a dateable test has come back medieval or late-antique: the Shroud, the titulus, the Sudarium, the Vienna spearhead. Not one has come back first-century. Meanwhile the objects that cannot be tested $em the Vatican lance, the crown of rushes, the fragments of wood $em retain their claims intact, because a claim that cannot be checked cannot be refuted either.

That is not a case against faith, and it is not offered as one. It is a fact about testing. Objects that were venerated for centuries before anyone could date them were assembled in a world that had no way to distinguish an authentic relic from a sincere one, and demand for them was enormous. What survives is not evidence about the first century. It is superb evidence about the tenth through the fourteenth, which is when most of it was made, bought, carried barefoot through Paris, and believed.
"@

# ---------------------------------------------------------------- write per book
$books = @(
 @{code='MATTHEW'; id='019FA049-322F-75EF-AAB7-0C0DE8DBDB85'; notes='019FA01D-FA22-76C6-976C-3EA4F4D54A14'; slug='matthew'; sk=2950.0;
   open=@"
This Gospel reports that they fixed a written charge above his head $em "This is Jesus, the King of the Jews" (27:37) $em and that a soldier stood guard, and that a rich man from Arimathea wrapped the body in a clean linen cloth (27:59). Every one of those objects is claimed to survive. The board is in Rome, the linen is in Turin, the spear is in three places at once, and the crown is in Paris.

This chapter is about what happens when you take those claims seriously enough to check them.
"@;
   close=@"
Matthew, of all four evangelists, is the one who cares most about the written charge $em he alone gives its wording in full. It is fitting, and a little bleak, that of all the objects claimed from that afternoon, the board bearing those words is among the few that has been dated, and came back a thousand years too young.
"@ }
 @{code='MARK'; id='019FA966-2F28-7A30-9662-F0F6F33C4D54'; notes='019FA968-1B3B-75DC-84CF-0C7D9C4E783C'; slug='mark'; sk=1650.0;
   open=@"
This Gospel gives the crucifixion three words in the Greek $em they crucified him $em and adds almost nothing about the apparatus. No description of the cross, no account of the nails, no notice of what was done with the clothes beyond the soldiers casting lots. Mark's restraint is total.

The centuries that followed were not restrained at all. Every physical object implied by those three words is now claimed by somebody, usually by several somebodies, and this chapter is about what is left of them and what testing has done to the claims.
"@;
   close=@"
There is a symmetry worth noticing at the end of the shortest Gospel. Mark declined to describe the instruments at all $em no cross, no nails, no cloth, no spear. Everything in this chapter was supplied later, by people who wanted the objects to exist. Mark's silence has aged better than any of the relics have.
"@ }
 @{code='LUKE'; id='019FA969-3232-772B-998A-BB2D5158F96E'; notes='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'; slug='luke'; sk=2450.0;
   open=@"
This Gospel says that Joseph of Arimathea took the body down, wrapped it in a linen cloth, and laid it in a rock-cut tomb where no one had yet been laid (23:53) $em and that the women, returning, found the linen wrappings lying by themselves (24:12). Luke is specific about cloth in a way the others are not.

Two cloths are claimed to be those cloths, one in Italy and one in Spain, and both have been dated. This chapter is about them, and about the spear, the wood, the board, and the crown $em every object the afternoon is supposed to have left behind.
"@;
   close=@"
Luke opened his Gospel by promising an orderly account so that his reader could judge its reliability. He would, one suspects, have approved of what was done to the relics in this chapter $em the sampling, the spectrometry, the published results $em and would have understood better than most that a negative finding, honestly reported, is also an orderly account.
"@ }
 @{code='JOHN'; id='019FA96B-CAD8-7769-BF17-363E3641048E'; notes='019FA96D-7D48-75E0-9BD9-2190171276DC'; slug='john'; sk=2150.0;
   open=@"
This Gospel is the only one that reports the piercing. "One of the soldiers pierced his side with a spear, and at once there came out blood and water" (19:34) $em a detail found nowhere in Matthew, Mark, or Luke. This Gospel is also the only one that pauses over the burial cloths, noting the linen wrappings and the face cloth folded by itself in a place apart (20:6-7).

Everything in this chapter descends from those two passages. The spear became three spears and the soldier acquired a name; the cloths became the two most examined objects in Christendom. None of it is in the text. All of it grew out of the text.
"@;
   close=@"
It is worth returning, at the end, to what this Gospel actually says. A soldier, unnamed. A spear, undescribed. Cloths, folded and left behind. That is the whole inventory, and its author showed no interest whatever in what became of any of it.

Everything else $em the name, the three lances, the shrines, the sum paid to a bankrupt emperor for a circle of rushes $em was added by people who could not leave an unnamed soldier unnamed or an object unfound. The impulse is human and, in its way, touching. It is simply not history, and this Gospel is oddly clear-eyed about the difference: it ends by admitting that not everything could be written down, and then stops.
"@ }
)

foreach ($b in $books) {
    Write-Host $b.code
    $n = Next-Note $b.notes
    $map = @{ 'LONGINUS'=$n; 'LANCES'=$n+1; 'VIENNA'=$n+2; 'CROSS'=$n+3; 'TITULUS'=$n+4; 'CROWN'=$n+5; 'SHROUD'=$n+6; 'SUDARIUM'=$n+7 }
    Add-Note $b.notes $map['LONGINUS'] "The soldier's name is not in the Gospels" $nbLonginus
    Add-Note $b.notes $map['LANCES']   "Three candidate Holy Lances" $nbLances
    Add-Note $b.notes $map['VIENNA']   "The Vienna lance, examined" $nbVienna
    Add-Note $b.notes $map['CROSS']    "Calvin's shipload, and the man who measured" $nbTrueCross
    Add-Note $b.notes $map['TITULUS']  "The titulus board, radiocarbon dated" $nbTitulus
    Add-Note $b.notes $map['CROWN']    "What the Crown of Thorns cost, and what it is" $nbCrown
    Add-Note $b.notes $map['SHROUD']   "The Shroud: the 1988 date and what the DNA shows" $nbShroud
    Add-Note $b.notes $map['SUDARIUM'] "The Sudarium of Oviedo" $nbSudarium

    $body = $b.open.Trim() + "`n`n" + $core.Trim() + "`n`n" + $b.close.Trim()
    foreach ($k in $map.Keys) { $body = $body.Replace("[$([char]0)$k]", "[$($map[$k])]") }
    Add-ObjectsChapter $b.id $b.slug $b.sk $body
}

$conn.Close()
Write-Host "DONE"
