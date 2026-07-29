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
$Ch22NodeId = [guid]"019FA96A-E7D2-741F-BCDF-381A086FF909"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'judas-payment-amount-luke' = @{ title="Only Matthew names a sum"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994). Luke 22:5 and Mark 14:11 report only an agreed, unspecified payment ('silver,' a generic term); Matthew 26:15 alone supplies 'thirty pieces of silver,' a detail tied to Matthew's fulfillment-citation practice echoing Zechariah 11:12-13. The other two Synoptics never name a sum." }
'seder-structure-anachronism' = @{ title="The fixed Seder liturgy did not yet exist"; body="Baruch M. Bokser, The Origins of the Seder: The Passover Rite and Early Rabbinic Judaism (Berkeley: University of California Press, 1984). The fixed, scripted Passover Seder $em four cups, ordered Haggadah, question-and-answer liturgy $em is first codified in Mishnah Pesachim (redacted c. 200 CE) as a post-70 CE rabbinic reworking after the Temple's sacrificial center was destroyed; most scholars hold the full ritual as known today did not yet exist in 30 CE." }
'jeremiah-new-covenant' = @{ title="A direct citation of Jeremiah's 'new covenant'"; body="Joel B. Green, The Gospel of Luke, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 1997), commentary on 22:20. 'New covenant' as an exact phrase occurs only once in the Hebrew Bible, at Jeremiah 31:31-34; Luke's cup-saying is a direct citation of that specific prophetic text." }
'bezae-cup-sequence-variant' = @{ title="A real but narrow textual variant"; body="Rob James, 'Variant Readings of Luke 22:15-20 and the Relationship of Codex Bezae to Curetonian Syriac,' Journal of Theological Studies 75, no. 2 (2024): 336-355. Codex Bezae and related Old Latin/Old Syriac witnesses omit the second cup-saying (22:19b-20), producing a shorter cup-then-bread sequence; every other early text-type, including the earliest papyri, supports the longer reading." }
'sword-cost-peasant-economy' = @{ title="A sword was a genuine luxury expense"; body="Sakari Hakkinen, 'Poverty in the First-Century Galilee,' HTS Teologiese Studies/Theological Studies 72, no. 4 (2016): 1-9. Ordinary day-laborers earned roughly one denarius a day, with total peasant household income around 200-300 denarii annually after rents, tithes, and debt service $em leaving negligible discretionary income, making a specialist-made sword a genuine marker of unusual preparedness rather than an ordinary household item." }
'hikanon-estin-idiom' = @{ title="'It is enough' -- approval, or a cutoff?"; body="Joseph A. Fitzmyer, The Gospel According to Luke X-XXIV, Anchor Bible 28A (New York: Doubleday, 1985), commentary on 22:38. The Greek hikanon estin ('it is enough') is grammatically singular/neuter, sitting awkwardly with a claim that 'two swords' (plural) are numerically sufficient; the mismatch underlies real scholarly disagreement over whether Jesus approves the swords or dismissively ends the conversation." }
'luke-omits-gethsemane-name' = @{ title="Luke never says 'Gethsemane'"; body="Direct textual comparison of Luke 22:39-40 ('the Mount of Olives,' 'the place') against Mark 14:32 and Matthew 26:36 ('a place called Gethsemane'). Luke's Gospel never uses the word 'Gethsemane'; the name derives entirely from Mark's and Matthew's parallel accounts." }
'gethsemane-etymology' = @{ title="'Oil press,' not a garden's name"; body="'Gethsemane,' in The Anchor Bible Dictionary, ed. David Noel Freedman, vol. 2 (New York: Doubleday, 1992). The name transliterates Hebrew/Aramaic gat shemanim, 'oil press' or 'press of oils' $em describing a working agricultural installation, not an ornamental garden." }
'gethsemane-olive-tree-dating' = @{ title="Crusader-era trees, not first-century ones"; body="Mauro Bernabei, 'The age of the olive trees in the Garden of Gethsemane,' Journal of Archaeological Science 53 (2015): 43-48. Radiocarbon dating of three of the traditional garden's eight ancient olive trees dated all three to the twelfth century CE, not to antiquity; the hollow trunks make root-age undatable, but nothing in the data supports claims of 2,000-year-old trees." }
'hematohidrosis-medical' = @{ title="A real, rare medical condition"; body="Juvarez U. Ogbuneke and John C. Allen, 'Case Report and Review of the Pathophysiology and Therapeutics of Adult Hematohidrosis,' Cureus 15, no. 3 (2023): e36187. Hematohidrosis is a rare but medically documented condition in which blood is expressed through intact sweat glands under extreme stress; it is real and rare, not a confirmed clinical diagnosis of Luke 22:44's simile." }
'luke-22-43-44-textual-variant' = @{ title="A genuinely disputed textual passage"; body="Lincoln H. Blumell, 'Luke 22:43-44: An Anti-Docetic Interpolation or an Apologetic Omission?' TC: A Journal of Biblical Textual Criticism 19 (2014): 1-35. Some of the oldest manuscripts (Papyrus 75, the original hand of Codex Sinaiticus) omit Luke 22:43-44 entirely, while other early witnesses include it; scholars remain divided over whether the verses were excised or represent an early but secondary addition." }
'luke-ear-healing-unique' = @{ title="Only Luke reports the healing"; body="Henry J. Cadbury, The Style and Literary Method of Luke (Cambridge, MA: Harvard University Press, 1920). Only Luke (22:51) reports Jesus healing the servant's severed ear; Matthew, Mark, and John report the injury but not a healing. Cadbury's landmark study challenged the traditional 'Luke the physician' inference from Colossians 4:14, arguing Luke's 'medical' vocabulary was ordinary educated Greek usage rather than professional training." }
'rooster-ban-temple-purity' = @{ title="A real, pre-70 CE purity restriction"; body="Mishnah Bava Kamma 7:7, trans. Herbert Danby (Oxford: Clarendon Press, 1933); Dead Sea Scrolls Temple Scroll fragments (11Q21). The Mishnah bars raising chickens in Jerusalem because they scavenge ritually impure refuse that could contaminate sacrificial meat; a related restriction in the Second Temple-period Temple Scroll pushes attestation of the custom back before 70 CE." }
'gallicinium-roman-watch' = @{ title="'Cockcrow' as a named span of night"; body="Craig A. Evans, Mark 8:27-16:20, Word Biblical Commentary 34B (Nashville: Thomas Nelson, 2001), commentary on 13:35. Rome divided the night into four watches of roughly three hours each; the third watch (midnight to 3 a.m.) carried the Latin nickname gallicinium, 'cockcrow,' a conventional name for that stretch of night independent of an actual bird." }
'luke-daytime-sanhedrin-structure' = @{ title="A daytime trial that avoids a real legal problem"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 1 (New York: Doubleday, 1994); Mishnah Sanhedrin 4:1. Luke places the sole formal council interrogation at daybreak (22:66), while Mark and Matthew place it in the middle of the night; the Mishnah requires capital cases to be tried during the day, so Luke's arrangement does not run afoul of that rule as Mark's does." }
}

# ---- Chapter beats ----
$beat1 = @"
The Feast of Unleavened Bread, called Passover, was drawing near, and the chief priests and scribes were looking for a way to kill Jesus (22:1-2). Then Satan entered Judas Iscariot, and Judas went to confer with the chief priests about how he might hand Jesus over; "they were glad, and agreed to give him money" (22:3-5). Popular memory collapses all four Gospels into one image here -- Judas pocketing thirty silver coins, the price of blood fixed and countable. But that number belongs to only one Gospel. Luke's Greek says simply that the priests agreed to give him "silver" -- a generic word for money, no sum attached -- and Mark's parallel is equally silent on any figure. "Thirty pieces of silver" is Matthew's detail alone [[NOTE:judas-payment-amount-luke]]. Ask what Judas was actually paid, according to the earliest and most independent-seeming account, and the honest answer is: an unspecified sum.
"@

$beat2 = @"
On the day the Passover lamb was to be sacrificed, Jesus sent two disciples ahead to prepare the meal -- and Luke alone among the Synoptics names them: Peter and John (22:7-13). The errand is logistics, not doctrine, and matches everything otherwise known about the festival: Passover lambs were slaughtered at the Jerusalem Temple on the afternoon of 14 Nisan and eaten that night in private and rented rooms throughout a city swollen with pilgrims.

At the meal, Jesus took bread, broke it, and said, "This is my body, given for you" -- and, "likewise the cup after supper," "This cup is the new covenant in my blood" (22:14-20). Anyone picturing this as the Seder familiar from any modern telling -- four fixed cups, a scripted Haggadah, an ordered sequence of ritual courses -- is picturing something that did not yet exist in that form. The elaborate liturgy is first codified in the Mishnah's tractate Pesachim, redacted around 200 CE, more than a century and a half after this meal [[NOTE:seder-structure-anachronism]]. Luke's "cup after supper" is consistent with a meal that included more than one cup; it is not evidence for the later fixed sequence.

"The new covenant in my blood" is a precise citation, not a loose phrase. "New covenant" appears exactly once in the entire Hebrew Bible, at Jeremiah 31:31 [[NOTE:jeremiah-new-covenant]]. And a genuine manuscript puzzle sits inside these six verses: Codex Bezae and a cluster of Old Latin and Old Syriac witnesses omit the second cup-saying, leaving a shorter cup-then-bread sequence with no second cup and no "do this in remembrance" [[NOTE:bezae-cup-sequence-variant]]. Every other early text-type, including the earliest papyri, supports the fuller, familiar reading -- a real but narrow variant confined mostly to Western witnesses.
"@

$beat3 = @"
Jesus tells Simon that Satan has asked to "sift you like wheat," and predicts Peter will deny him three times before the rooster crows (22:31-34). Then he asks the disciples to take a purse, a bag, and a sword: "let the one who has no sword sell his cloak and buy one." They answer, "Lord, look, here are two swords." "It is enough," he says (22:35-38). In translation this reads like casual approval of light armament. Look at what a sword actually cost in this economy and the scene gets stranger. Ordinary peasant household income left negligible discretionary income for anything, let alone a specialist-made sword worth multiple weeks' wages [[NOTE:sword-cost-peasant-economy]]. That two swords have already turned up among a band of Galilean fishermen and tradesmen is itself worth noticing.

And what "it is enough" meant is genuinely disputed. The Greek hikanon estin is grammatically singular and neuter; a claim that "two swords" (plural) are a sufficient number would more naturally take a plural form. That mismatch underlies a real interpretive split -- some read the line as approving the two swords as adequate, others as an idiomatic cutoff closer to "enough of this" [[NOTE:hikanon-estin-idiom]]. No manuscript variant exists to settle it; it's a live grammatical ambiguity, not a scribal one.
"@

$beat4 = @"
Jesus went out, "as was his custom," to the Mount of Olives (22:39). Worth pausing on the wording: this scene, like nearly every retelling, gets called "Gethsemane" -- but Luke's text never uses that word. Only Matthew and Mark name the place "Gethsemane"; Luke says only "the Mount of Olives" [[NOTE:luke-omits-gethsemane-name]]. The borrowed name does have a plain, well-attested meaning: it transliterates gat shemanim, "oil press" [[NOTE:gethsemane-etymology]]. The traditional site's olive trees have themselves been the subject of real scientific dating, and the popular claim that visitors can touch wood Jesus might have touched doesn't hold up: a 2014-2015 radiocarbon study dated three of the garden's ancient olive trees to the twelfth century CE, roughly 1,100 years too late for 30 CE [[NOTE:gethsemane-olive-tree-dating]].

Luke alone reports an angel strengthening Jesus and that "his sweat became like drops of blood falling to the ground" (22:43-44). Two separate questions live inside those two verses. First, medically: hematohidrosis, blood expressed through intact sweat glands under extreme stress, is a real but rare documented condition -- a different claim from treating it as the confirmed explanation for a description that is, on its own terms, a simile [[NOTE:hematohidrosis-medical]]. Second, and arguably more consequential: were these two verses even in Luke's original text? Some of the oldest manuscripts omit 22:43-44 entirely; other early, important witnesses include it. Scholars remain genuinely divided over whether the verses were excised or added [[NOTE:luke-22-43-44-textual-variant]].
"@

$beat5 = @"
Judas arrived leading a crowd and approached to kiss Jesus, the arranged signal (22:47-48). One of the disciples struck the high priest's servant and cut off his right ear -- and Jesus touched the man's ear and healed it (22:49-51). That on-the-spot healing, in the middle of his own arrest, is unique to Luke. Matthew and Mark both report the ear cut off; neither reports it healed [[NOTE:luke-ear-healing-unique]]. The detail has long fed a tradition -- rooted in Colossians 4:14's "Luke, the beloved physician" -- that Luke's Gospel shows a doctor's eye for medical particulars, though Henry Cadbury's landmark 1920 study challenged that inference directly.
"@

$beat6 = @"
Jesus was led to the high priest's house; Peter denied knowing him three times, and "immediately, while he was still speaking, the rooster crowed" (22:54-61). The crowing rooster deserves checking from two directions. One claim circulating fairly widely holds that raising roosters was actually banned inside Jerusalem for ritual-purity reasons -- and that claim holds up better than it might sound: the Mishnah bars raising chickens in Jerusalem because they scavenge ritually impure refuse, and a related restriction among the Dead Sea Scrolls' Temple Scroll fragments pushes the custom's attestation back before 70 CE [[NOTE:rooster-ban-temple-purity]]. Separately, "gallicinium" -- Rome's own nickname for the third night watch, "cockcrow," independent of any actual bird -- was already ordinary period vocabulary, meaning "the rooster crowed" could carry a double sense a modern reader easily misses [[NOTE:gallicinium-roman-watch]].

Only "when day came" did the assembled elders, chief priests, and scribes convene their council and interrogate Jesus formally (22:63-71). That timing is Luke's most distinctive structural choice in the whole sequence. Mark and Matthew place a full formal Sanhedrin session in the middle of the night, immediately after the arrest; Luke reserves the one explicit council session and formal charge for daybreak. That matters because a daytime-only capital hearing sidesteps a real legal problem a nighttime one would raise: the Mishnah states that capital cases must be tried and concluded during the day [[NOTE:luke-daytime-sanhedrin-structure]]. What can be said cleanly: the Synoptics do not agree with each other on when Jesus was formally tried, and Luke's version is the one that never has to answer the daytime-trial objection.
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries ----
$glossary = [ordered]@{
'GALLICINIUM' = "Latin name for the third of the four Roman night watches (roughly midnight-3 a.m.), literally `"cockcrow`"; used conventionally for that time span independent of an actual rooster [[NOTE:gallicinium-roman-watch]]."
'HEMATOHIDROSIS' = "A rare, medically documented condition in which blood is expressed through intact sweat glands under extreme stress; relevant to but not a confirmed explanation of Luke 22:44's simile `"like drops of blood`" [[NOTE:hematohidrosis-medical]]."
'CODEX BEZAE (WESTERN TEXT)' = "A fifth-century bilingual (Greek-Latin) New Testament manuscript, the principal witness to the `"Western`" textual tradition, notable for a cluster of shorter or reordered readings, including at Luke 22:17-20 [[NOTE:bezae-cup-sequence-variant]]."
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
    Add-BeatNode $Ch22NodeId $id $sortKey
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
Try-Append "JUDAS ISCARIOT" "This chapter adds: Luke 22:5-6 leaves the payment amount unspecified (`"silver,`" no sum); the famous `"thirty pieces of silver`" is Matthew's detail alone (26:15). Also: Luke's specific framing that `"Satan entered into Judas`" before the betrayal negotiation (22:3) [[NOTE:judas-payment-amount-luke]]." $slugToNumber
Try-Append "SIMON PETER" "This chapter adds: uniquely named (with John) as the disciple sent ahead to prepare the Passover (22:8), where Mark/Matthew say only `"his disciples`"; also the specific three-exchange denial sequence and rooster-crow timing (22:54-62) [[NOTE:gallicinium-roman-watch]]." $slugToNumber
Try-Append "THE TEMPLE" "This chapter adds: `"officers of the temple guard`" (22:4, 22:52) named as the enforcement arm involved in negotiating with Judas and in Jesus's arrest." $slugToNumber

$conn.Close()
Write-Host "DONE Chapter 22."
