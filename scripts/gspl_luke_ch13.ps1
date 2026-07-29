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
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
    foreach ($k in $params.Keys) { $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null }
    $cmd.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; return $cmd.ExecuteScalar() }
function New-BeatRow([string]$text) {
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid(); $hash = Sha256Hex $text
    $sql = "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())"
    Exec-NonQuery $sql @{ Id = $id; Text = $text; Hash = $hash; Number = $script:MaxNumber }
    return $id
}
function Add-BeatNode([guid]$nodeId, [guid]$beatId, [double]$sortKey) {
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = $nodeId; BeatId = $beatId; SortKey = $sortKey }
}
function Seed-Entity([string]$name, [string]$slug, [string]$type, [string]$desc) {
    $exists = Exec-Scalar "SELECT COUNT(*) FROM Entities WHERE UniverseId='0197E9C9-0003-7000-8000-000000000003' AND Slug='$slug'"
    if ($exists -gt 0) { Write-Host "  entity exists, skip: $name"; return }
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId) VALUES (@Id, @Type, @Name, @Slug, 'canon', @Desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, '0197E9C9-0003-7000-8000-000000000003')" @{ Id = $id; Type = $type; Name = $name; Slug = $slug; Desc = $desc }
    Write-Host "  seeded entity: $name"
}
function Append-ToExistingBeat([guid]$beatId, [string]$extraParagraph) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"; $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $current = $cmd.ExecuteScalar()
    $updated = "$current`n`n$extraParagraph"
    $hash = Sha256Hex $updated
    Exec-NonQuery "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ Text = $updated; Hash = $hash; Id = $beatId }
}
function Find-GlossaryBeatId([string]$headingPrefix) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 b.Id FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530' AND b.Text LIKE @pat"
    $cmd.Parameters.AddWithValue("@pat", "$headingPrefix%") | Out-Null
    return $cmd.ExecuteScalar()
}
function Try-Append([string]$heading, [string]$extra, [hashtable]$slugMap) {
    $id = Find-GlossaryBeatId $heading
    if ($id) {
        foreach ($slug in $slugMap.Keys) { $extra = $extra.Replace("[[NOTE:$slug]]", "[$($slugMap[$slug])]") }
        Append-ToExistingBeat $id $extra
        Write-Host "Appended to $heading"
    } else { Write-Host "NOT FOUND: $heading" }
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$GlossaryNodeId = [guid]"019FA96B-29F5-7BB9-99D0-0F787960E530"
$Ch13NodeId = [guid]"019FA96A-4BD0-7EC1-BE40-6B20CFE5E550"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'pilate-galileans-luke-only' = @{ title="An incident attested nowhere outside Luke"; body="Norval Geldenhuys, Commentary on the Gospel of Luke, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 1951), ad loc. Luke 13:1. Geldenhuys and subsequent commentators note the honest state of the evidence: no ancient source outside Luke, Jewish, Roman, or Christian, mentions Pilate killing Galileans at or near a sacrifice; it is attested in exactly one verse of one Gospel." }
'josephus-pilate-atrocities' = @{ title="Josephus records three different Pilate atrocities"; body="Flavius Josephus, Jewish Antiquities 18.55-62 and 18.85-89 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press, 1965). Josephus records three distinct, separately dated atrocities under Pilate: the standards protest, the aqueduct-funds riot, and a massacre of Samaritans at Mount Gerizim which precipitated his recall. None of the three matches the Galileans-at-sacrifice incident Luke reports." }
'siloam-pool-2004' = @{ title='A real pool, excavated in 2004'; body="Hershel Shanks, 'The Siloam Pool: Where Jesus Healed the Blind Man,' Biblical Archaeology Review 31, no. 5 (2005). Reports archaeologists Ronny Reich and Eli Shukron's 2004 excavation of a large trapezoidal, stepped Second Temple-period pool, dated by coins of Alexander Jannaeus and Herodian-era pottery $em generally accepted as the biblical Pool of Siloam." }
'siloam-tower-debate' = @{ title="Fortification towers, a plausible but uncertain match"; body="Findings of archaeologist Nachshon Szanton's excavation of fortification towers adjoining the Siloam pool complex, building on an earlier identification by Edmond Weill. The towers were raised in the Hellenistic period and rebuilt in the early Roman era $em a plausible but not certain match for `"the tower in Siloam`" of Luke 13:4; no inscription confirms the identification." }
'orlah-fig-tree' = @{ title="A three-year agricultural horizon"; body="Mishnah, Orlah 1:1-2 (trans. Herbert Danby, The Mishnah, Oxford: Oxford University Press, 1933), codifying the Leviticus 19:23-25 prohibition against eating a fruit tree's produce for its first three years. Several commentators note the fig-tree parable's `"these three years... let it alone this year also`" timeframe sits naturally against this familiar agricultural-legal horizon." }
'bent-woman-wilkinson' = @{ title='A real, identifiable clinical picture'; body="J. Wilkinson, 'The Case of the Bent Woman in Luke 13:10-17,' Evangelical Quarterly 49 (1977): 195-205. Proposes, from the described symptoms (eighteen years' duration, fixed forward curvature), that the underlying condition is spondylitis ankylopoietica (ankylosing spondylitis), a chronic disease fusing the vertebrae; Luke's own framing remains spiritual rather than clinical." }
'mustard-seed-zohary' = @{ title='A real botanical exaggeration, not an invented one'; body="Michael Zohary, Plants of the Bible (Cambridge: Cambridge University Press, 1982), s.v. mustard/Brassica nigra. Identifies black mustard as the mustard of the Gospel parables, notes its seed as among the smallest sown in a Palestinian garden, and describes the annual plant as capable, in favorable soil, of reaching 2.5 to 4.5 meters, with a central stalk `"as thick as a man's arm.`"" }
'fox-idiom-bivin' = @{ title="'Fox' as insignificance, not cunning"; body="David Bivin, 'That Small-Fry Herod Antipas, or When a Fox Is Not a Fox,' Jerusalem Perspective. Argues from Hebrew and Aramaic idiom that calling a ruler a `"fox`" in first-century Jewish usage most naturally connoted insignificance and pettiness rather than cunning, positioning Jesus's remark as a put-down of Antipas's self-importance." }
'vaticinium-wendel-htr' = @{ title="A live disagreement: prophecy or hindsight?"; body="Jason S. Wendel, 'Weeping Over Jerusalem: Luke's Response to the Destruction of the Temple,' Harvard Theological Review 118, no. 4 (2025): 691-712. Argues Luke's Jerusalem-destruction material, including the lament of 13:34-35, is best understood as a literary and theological response composed with full knowledge of the Temple's actual fall in 70 CE $em the historical-critical majority's vaticinium ex eventu reading; a minority tradition assigns the underlying saying to a pre-70 source instead." }
}

# ---- Chapter beats ----
$beat1 = @"
Some in the crowd bring Jesus a piece of grim current-events talk: Galileans whose blood Pilate had mixed with their own sacrifices, evidently killed at or near worship (13:1). Jesus does not dispute the report -- he uses it, pairing it with a second local horror, eighteen people crushed when the Tower of Siloam fell on them (13:4), to argue against the folk theology that reads violent death as proof of unusual sin, before turning to the parable of a barren fig tree given one more year's grace (13:6-9).

The Galilean massacre is the harder case for this book's method, because the honest answer is that it sits nowhere but here. Flavius Josephus, whose own catalogue of Pilate's brutalities is extensive, never mentions it [[NOTE:josephus-pilate-atrocities]]. Josephus does record three other, different atrocities under the same governor -- soldiers ringing a crowd of protesters over the imperial standards, undercover soldiers clubbing rioters who objected to Temple funds spent on an aqueduct, and a massacre of Samaritans at Mount Gerizim -- but none of them is this one, and no other ancient writer fills the gap [[NOTE:pilate-galileans-luke-only]]. That leaves two honest and different things to say: the incident is completely consistent with the kind of governor Pilate demonstrably was, and it is, strictly, attested nowhere outside this one verse of Luke.

The Tower of Siloam fares slightly better, though not because the tower itself has been found. In 2004, repair work led archaeologists Ronny Reich and Eli Shukron to a monumental, trapezoidal Second Temple-period pool -- stepped, plastered, dated by coins of Alexander Jannaeus -- almost certainly the biblical Pool of Siloam [[NOTE:siloam-pool-2004]]. A separate excavation identified fortification towers flanking the pool's approach, raised in the Hellenistic period and rebuilt in the early Roman era -- plausible candidates for "the tower in Siloam," though the identification remains a reasoned inference, not a labeled ruin [[NOTE:siloam-tower-debate]]. What the archaeology firmly establishes is the setting: a real, densely built, actively-under-construction quarter of first-century Jerusalem where a collapsing tower killing eighteen people is exactly the kind of mundane, plausible accident the period would produce, attested or not.

The fig tree parable that follows carries its own quiet grounding: Jesus's gardener has come looking for fruit "these three years," a timeframe some commentators connect to the Torah's orlah law, which treats a tree's fruit as forbidden for its first three years and permissible only from the fourth [[NOTE:orlah-fig-tree]] -- a real, lived agricultural horizon a Judean audience would have recognized instinctively.
"@

$beat2 = @"
Jesus heals a woman who has been "bent double" for eighteen years, doing it in a synagogue on the Sabbath, and draws the same religious authorities' objection this book has already tracked through his earlier Capernaum and Galilean synagogue confrontations (13:10-17). The new, checkable wrinkle here is medical rather than legal: a body specifically unable to straighten itself for nearly two decades is a real clinical picture, and physician-scholar J. Wilkinson has argued the description matches ankylosing spondylitis, a progressive fusion of the spinal vertebrae producing exactly this fixed, forward-curved posture [[NOTE:bent-woman-wilkinson]]. Luke frames the condition as spiritual bondage rather than offering any diagnosis, but the physical picture is internally consistent with a real, identifiable disease process rather than a vague or symbolic ailment.
"@

$beat3 = @"
Jesus compares the kingdom of God to a mustard seed grown into a tree with birds nesting in its branches, and to yeast a woman works through a batch of dough (13:18-21). The mustard image gets treated online as a straightforward exaggeration, but the botany is more interesting than the caricature. Levantine black mustard, the species standardly identified as the plant of the parable, really does grow from one of the smallest seeds sown in a Palestinian garden -- roughly one to two millimeters -- into an annual herb that, in good soil, can reach two-and-a-half to four-and-a-half meters, with a central stalk described by botanist Michael Zohary as thick as a man's arm [[NOTE:mustard-seed-zohary]]. That is not literally a tree in the botanical sense -- it dies back every year -- but for a garden herb to tower well over head height from a seed that small is a genuinely striking, verifiable fact of Levantine agriculture, not a rhetorical stretch dressed up as science.
"@

$beat4 = @"
Warned that Herod wants him dead, Jesus calls Herod Antipas "that fox" and states his own itinerary will run its course regardless (13:31-32), before delivering a lament over Jerusalem (13:33-35). "Fox" reads to modern ears as a compliment to cunning, but the idiom's ancient connotations cut differently. Rabbinic usage regularly cast the fox as a destructive nuisance and a stand-in for a petty, small-time predator rather than an admirably clever one, and scholar David Bivin has specifically argued that calling Antipas a fox trades less on craftiness than on insignificance -- Jesus sizing Herod down as a minor irritant rather than the "lion" a tetrarch might imagine himself to be [[NOTE:fox-idiom-bivin]].

The lament's final line raises this chapter's genuinely contested historical-critical question. "Your house is left to you desolate" reads, in hindsight, as a description of the Jerusalem Temple's actual destruction by Roman forces in 70 CE. Mainstream scholarship divides over whether the saying is a vaticinium ex eventu -- a "prophecy" composed or sharpened after the event it describes, since most historical-critical scholars date Luke's final composition to roughly 80-90 CE -- or a genuinely earlier saying, since the material is shared nearly word-for-word with Matthew and is widely assigned by Q-source specialists to a hypothetical pre-Lukan collection [[NOTE:vaticinium-wendel-htr]]. Conservative and confessional scholarship holds that a genuinely predictive saying requires no compositional trick to explain -- a position the historical-critical majority does not share but does not have decisive external evidence to overturn either, since nothing outside the Gospels reports Jesus saying this particular line before the fact.
"@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- New glossary entries ----
$glossary = [ordered]@{
'TOWER OF SILOAM' = "A structure in Jerusalem whose collapse killed eighteen people, cited by Jesus as an example of death without special culpability (13:4). A separate excavation identified fortification towers near the Pool of Siloam, raised in the Hellenistic period and rebuilt in the early Roman era, as a plausible but not certain match [[NOTE:siloam-tower-debate]]."
'POOL OF SILOAM' = "A Second Temple-period stepped pool excavated in Jerusalem in 2004 by archaeologists Ronny Reich and Eli Shukron, dated by coins of Alexander Jannaeus and Herodian-era pottery [[NOTE:siloam-pool-2004]]. Located near the tower referenced in Luke 13:4."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum $em $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats ----
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) { $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch13NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert new glossary entries ----
foreach ($heading in $glossary.Keys) {
    $body = $glossary[$heading]
    foreach ($slug in $slugToNumber.Keys) { $body = $body.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    $text = "$heading`n`n$body"
    $id = New-BeatRow $text
    $maxGlossarySortKey += 50
    Add-BeatNode $GlossaryNodeId $id $maxGlossarySortKey
}
Write-Host "Inserted $($glossary.Count) glossary entries"

# ---- Append new claims to existing glossary beats ----
Try-Append "PONTIUS PILATE" "This chapter adds the Galileans-killed-at-sacrifice incident (13:1), flagged explicitly as attested nowhere outside Luke, alongside Josephus's three independently-attested Pilate atrocities (standards, aqueduct riot, Mount Gerizim massacre) [[NOTE:josephus-pilate-atrocities]] [[NOTE:pilate-galileans-luke-only]]." $slugToNumber
Try-Append "HEROD ANTIPAS" "This chapter adds the `"that fox`" characterization (13:31-32), with the Bivin argument that the idiom connotes insignificance rather than cunning [[NOTE:fox-idiom-bivin]]." $slugToNumber

# ---- Seed new entities ----
Seed-Entity "Black Mustard (Brassica nigra)" "black-mustard-brassica-nigra" "material" "Levantine garden herb identified as the plant of the mustard-seed parable, capable of tree-like growth from a tiny seed."

$conn.Close()
Write-Host "DONE Chapter 13."
