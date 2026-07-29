$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$em = [char]8212

function Sha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text.Trim())
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLower()
}
function Exec-NonQuery([string]$sql, [hashtable]$params) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
    foreach ($k in $params.Keys) { $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null }
    $cmd.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; return $cmd.ExecuteScalar() }

# ---------- PART 1: strip the internal "Beat N:" engine jargon from Mark's section headings ----------
# "### Beat 3: The Corban Vow (7:9-13)" -> "### The Corban Vow (7:9-13)"
Write-Host "Stripping internal 'Beat N:' headings from MARK..."
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
SELECT bt.Id, bt.Text FROM Nodes c
JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id
JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode='MARK' AND bn.IsEnabled=1 AND bt.Text LIKE '%### Beat%'
"@
$rdr = $cmd.ExecuteReader()
$rows = @()
while ($rdr.Read()) { $rows += [pscustomobject]@{ Id = $rdr.GetGuid(0); Text = $rdr.GetString(1) } }
$rdr.Close()

$fixed = 0
foreach ($r in $rows) {
    $new = [regex]::Replace($r.Text, '###\s*Beat\s*\d+\s*:\s*', '### ')
    if ($new -ne $r.Text) {
        Exec-NonQuery "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ Text = $new; Hash = (Sha256Hex $new); Id = $r.Id }
        $fixed++
    }
}
Write-Host "  headings cleaned: $fixed"

# ---------- PART 2: Then and Now ----------
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")

function Add-ThenAndNow([string]$nodeId, [string]$body) {
    $existing = [int](Exec-Scalar "SELECT COUNT(*) FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId='$nodeId' AND bn.IsEnabled=1 AND b.Text LIKE 'Then and Now%'")
    if ($existing -gt 0) { Write-Host "  already present, skip: $nodeId"; return }
    $text = "Then and Now" + "`n`n" + $body.Trim()
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $script:MaxNumber }
    $sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0)+100 FROM BeatNodes bn WHERE bn.NodeId='$nodeId'")
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = [guid]$nodeId; BeatId = $id; SortKey = $sk }
    Write-Host ("  added at SortKey {0}: {1} chars" -f $sk, $text.Length)
}

Add-ThenAndNow "019FA966-FCDC-70EC-B729-D891E6C094DE" @"
Mark's Gospel has no childhood in it. No genealogy, no manger, no visitors $em it opens with an adult walking out of Nazareth to a river, and the whole first chapter moves at a pace the other three never attempt. Then, near the end of it, a healed man is told to keep quiet and immediately does the opposite, with the result that Jesus can no longer enter a town openly (1:45).

That sentence is the most underrated piece of social history in the chapter. It tells us how fast news travelled in a world with no technology for it at all: fast enough to close a town to a man within days. Word of mouth in dense village networks, carried at walking pace by people with a reason to talk, was not slow. It was merely uncontrollable.

We assume we invented virality and only invented the measurement. What genuinely changed is reach without relationship $em a story can now arrive from someone you will never meet, about a place you cannot picture. Mark's version required a person who had actually been there, walking to the next village, telling people who knew him. That is a slower system and a much harder one to lie in, because the messenger had to keep living among the people he told.
"@

Add-ThenAndNow "019FA967-0D77-73B8-A0B4-BA4423DF5219" @"
Four men cannot get a stretcher through a crowded doorway, so they go up on the roof and dig through it (2:1-4). The verb matters: they excavate. A roof of this kind was beams laid across walls, brushwood packed over them, and mud rolled flat on top $em resurfaced annually, walkable, and sometimes where the household slept in hot weather. It was breachable with hands and a tool, and it was repairable the same way.

That is why the scene is startling rather than absurd. The friends are not committing vandalism against something irreplaceable; they are doing damage everybody present could price, in a material everybody knew how to patch, and doing it in front of the owner. The cost is real and the point is that they paid it.

Modern housing has removed the option entirely. There is no way to improvise entry to a building today that is not either a crime or a structural disaster, which quietly deletes a whole category of determined, forgivable, practical trespass. And the underlying problem is worse: the barrier in the story is a crowd of people who got there first, and no amount of urgency moves them. Anyone who has watched a queue fail to part for someone who needed to be at the front knows the roof was the sane option.
"@

Add-ThenAndNow "019FA967-1DC4-7B12-8948-FC0C423511D4" @"
Halfway through this chapter, his family comes to collect him. The crowd is so dense he cannot eat; his relatives set out to take charge of him, and the explanation circulating is that he is out of his mind (3:20-21). Mark records this without softening it, which is one of the reasons scholars take the detail seriously $em it is not the sort of thing a later admirer would insert.

Family intervention on grounds of sanity is one of the oldest and least changed human events in the book. The particulars are recognisable in detail: the person at the centre is working past exhaustion and not eating; the concern is real; the concern is also about the family's standing in a village where his behaviour reflects on all of them. Nobody in the scene is villainous. They are embarrassed and frightened, in that order.

What we have added is a legal framework $em capacity assessments, involuntary holds, criteria, appeal rights $em built precisely because relatives acting in good faith are not reliable judges of whether someone is unwell or merely inconvenient. That machinery is a genuine advance and it has not resolved the underlying ambiguity at all. The question the family is asking in this chapter is still, in every jurisdiction, decided by somebody making a judgement call about a person they cannot see inside.
"@

Add-ThenAndNow "019FA967-2E1B-79EA-B4B3-8BE2ABD82905" @"
The two household objects in the middle of this chapter are both standardised measures. The lamp is a mass-produced type recovered by the thousand from first-century houses; the vessel the lamp gets hidden under is a named Roman dry measure of a defined capacity, used for issuing grain across the empire. When the saying warns about a measure being measured back to you, it is not reaching for an abstraction. It is naming a calibrated object.

That is a state achievement, and an underappreciated one. An empire that can put the same grain measure in every provincial market has solved a problem of trust: a buyer in one province and a seller in another can transact without either trusting the other's honesty, because they are both trusting a standard. Every complaint about short measure in the ancient world is a complaint that the standard was cheated, which presupposes there was one.

We inherited that and pushed it to an extreme the period could not imagine $em a kilogram defined by physical constants, traceable calibration chains, weights and measures inspectors. And the saying still lands, because it never depended on the technology. Any system of standards can be gamed by whoever holds the scale, which is why the warning is addressed to the person doing the measuring.
"@

Add-ThenAndNow "019FA967-3F66-72FB-BAEE-2028F37C38C5" @"
One detail in the middle of this chapter is a complete economic biography in half a sentence: a woman with a bleeding condition of twelve years' standing had spent everything she had on physicians and had got worse rather than better (5:26). Mark, uniquely, itemises the financial ruin.

Every part of that is verifiable as a general condition of ancient medicine. Treatment was available, it was private, it was paid for directly, and its efficacy for chronic conditions was close to nil $em so a long illness reliably converted into poverty, and the poverty was caused by the treatment rather than the disease. She is not poor because she was ill. She is poor because she sought care.

That sequence is the one thing in this chapter a modern reader may recognise from their own life, and where in the world they live determines whether they recognise it at all. Systems that pool the cost have made ruinous medical debt a historical curiosity for some populations and a routine event for others, within the same century, on the same planet. The medicine has been transformed beyond recognition. The mechanism by which a long illness eats a household's savings is, in much of the world, exactly as Mark describes it, and requires no annotation whatsoever.
"@

Add-ThenAndNow "019FA967-4F79-781A-A0F2-C090C5D418C8" @"
Mark says the crowd sat down on green grass (6:39), and the detail is doing something no other Gospel bothers with: it timestamps the scene. Grass in this landscape is green for a limited window after the rains and brown for the rest of the year. Anyone living on that land read the ground the way we read a calendar $em the colour of a hillside told you the month, what was ripe, what was still weeks away, and whether the roads were passable.

That literacy is almost entirely gone, and its loss is recent enough to be measurable in a single family. Most people alive now could not date the season from a hillside within six weeks, and have no need to, because the information arrives from a device and the food arrives regardless. We have traded an embodied competence for an accurate abstraction, which is a good trade with an odd side effect: the world outside has become scenery rather than data.

Mark almost certainly kept the phrase because it was what somebody remembered, not because it was significant. That is what makes it useful. A pointless-seeming detail that happens to fix a season is worth more to a historian than a paragraph of interpretation.
"@

Add-ThenAndNow "019FA967-6138-7D58-A9FA-A44A98DA8B34" @"
The sharpest argument in this chapter is not about handwashing. It is about a legal device: property dedicated by vow could be withheld from the parents it would otherwise have supported, so a man could technically honour a religious obligation while leaving his mother and father unprovided for (7:9-13). The accusation is that the letter of one rule has been used to void the substance of another.

This is tax planning. Not metaphorically $em structurally. The mechanism is identical to every arrangement that satisfies a rule's wording while defeating its purpose: the transfer that is legally a gift, the residence that exists on paper, the structure whose only function is to make an obligation land somewhere it cannot be collected. Nothing illegal is happening in the vow, which is precisely the complaint.

We have written thousands of pages against this and lost every round on points. Anti-avoidance provisions, purposive interpretation, general rules aimed at arrangements whose main purpose is avoidance $em an entire branch of law now exists to argue that the spirit of a provision can override its letter, which is the argument being made in this chapter, about parents, in a courtyard. The dispute is twenty centuries old and the winning side has always been whoever could afford better advice.
"@

Add-ThenAndNow "019FA967-716F-7E45-AB49-7BDA85AC3C9E" @"
This chapter contains the only healing in the four Gospels that does not work the first time. The blind man at Bethsaida is touched, reports seeing people who look like trees walking, and has to be touched again before his sight resolves (8:22-25). No other Gospel includes it, and it is difficult to think of a reason to invent it.

The two-stage recovery is also, incidentally, a decent description of how vision returns when it returns at all: not as a switch but as a resolution problem, shapes before edges, movement before detail. Whatever one concludes about the event, the reported sequence is closer to clinical than to miraculous convention.

What has changed is our tolerance for the partial result. We are the first culture with a genuine expectation of complete cures, and it has made us peculiarly bad at the interim $em the treatment that half works, the recovery that stalls at trees walking. Ancient patients expected improvement and negotiated with what they got. We expect resolution and experience anything less as a failure of the medicine.

The chapter then turns to a man being asked who people say he is, and getting a partial answer, and asking again. Mark has put the two-stage story immediately before it. He is not usually credited with subtlety.
"@

Add-ThenAndNow "019FA967-8259-7FB3-B9F0-8DDDC93745AA" @"
The description of the boy in the valley is the most clinical passage in Mark: an episode that throws him down, rigidity, foaming at the mouth, grinding teeth, a history from childhood, and a father's report that it happens near fire and water (9:17-22). Mark records observations, in sequence, with a case history $em and no diagnostic vocabulary, because none existed.

That gap is the whole distance between then and now. The symptoms described are recognisable to any modern reader within a sentence, and the framework available to describe them was a spirit. It is worth being careful about what follows from that. The father is not stupid; he has watched his son closely for years and reports accurately. He simply has no available category that is not a person doing it. Naming is not a small technology.

We have the name, the mechanism, medication that works for most people, and imaging that can show the origin. What we have not much improved on is the father's line, which is the most quoted sentence in the chapter for a reason: he believes, and asks for help with the part of him that does not. Every parent who has consented to a treatment they do not understand, on the advice of someone they have decided to trust, has said the same thing in different words.
"@

Add-ThenAndNow "019FA967-9352-7FD4-A6DE-9380C8B29296" @"
When Bartimaeus is called over, Mark says he throws off his cloak and goes (10:50). It is a throwaway line and it is the most revealing thing in the scene. For a man begging by a roadside, the outer garment was the single most valuable object he owned: bedding at night, shelter in rain, and $em spread on the ground in front of him $em the actual instrument of his trade, the thing coins landed on. Under the law it was protected property, the one item a creditor could not keep overnight.

So the gesture is not enthusiasm. He is abandoning his livelihood, his bed, and his roof in a crowd, blind, on the assumption that he will not need them again. Nobody in that street would have read it as anything other than reckless.

The modern equivalent is not a coat. It is the phone $em identity, payment, work, contact, and shelter-arrangement in one object, the thing a person sleeping rough guards most carefully because losing it means losing access to every system that could help. Handing it to a stranger and walking away is roughly the register of what Mark is describing. Read it that way and the sentence stops being scenery and starts being the risk the chapter is actually about.
"@

Add-ThenAndNow "019FA967-A43B-7A7E-8DCC-A2D3581571FC" @"
A man is hungry, sees a fig tree in leaf, finds no fruit, and curses it $em and Mark adds, almost apologetically, that it was not the season for figs (11:13). Readers have found this the least attractive miracle in the Gospels for two thousand years, largely because the narrator has helpfully explained that the tree was innocent.

The horticulture is real and worth knowing: a fig in leaf can carry early immature fruit, so leaves are a plausible signal even out of season, which makes the disappointment rational even when the demand is not. The tree is being judged for advertising a capacity it did not have.

Here is what has actually changed. We no longer know what season anything is in. Figs are available continuously, flown from wherever it is currently the season, and an entire generation has grown up for whom out of season is not a category that applies to food. That is a genuine marvel and it has quietly removed the one piece of knowledge this passage assumes its reader has. The scene depends on the audience feeling both things at once $em the hunger and the unfairness $em and we now have to be told that figs have a season at all before the story can land.
"@

Add-ThenAndNow "019FA967-B4EF-7F1B-B44A-506365CDE94A" @"
The parable that opens this chapter assumes a working knowledge of a specific business arrangement: an owner develops a vineyard $em wall, winepress, watchtower $em then leases it out and leaves, expecting a share of the produce to be collected by agents (12:1-9). The audience does not need any of this explained. Absentee ownership with tenant cultivation was the ordinary shape of agricultural wealth, and the friction in it was equally ordinary. Tenants had the labour and the local knowledge; the owner had the title and the distance.

That is why the story escalates so plausibly. A remote proprietor's only instrument is a representative who arrives with a claim and no power, and tenants who have worked the land for years develop a strong sense of whose vineyard it actually is. The violence is extreme; the underlying grievance is not exotic.

We have not dismantled this structure. Ownership at a distance has become more abstract, not less $em the fund holding the freehold, the landlord reachable only through a managing agent, the farmland owned by an institution whose name the farmer has never said aloud. The agent still arrives with a claim and no power, and is still the person who absorbs everything the tenants feel about an owner they have never met.
"@

Add-ThenAndNow "019FA967-C575-7C03-BFD7-F7441D9A2723" @"
The most remarkable sentence in this chapter is a disclaimer. After a long passage of prediction $em wars, earthquakes, persecutions, a desolation standing where it should not $em the discourse states that concerning the day and hour, nobody knows, not the angels, and not the Son (13:32). A text making large claims about the future has stopped to say that its own central figure lacks the schedule.

That is a genuinely unusual thing for a religious document to do, and it has caused visible discomfort ever since; the parallel in Matthew has a textual history around this exact phrase, and later readers have worked hard on it. Whatever else it is, it is a limit voluntarily stated.

We are much worse at this. Forecasting is now an industry $em models, projections, scenarios, confidence intervals $em and the confidence intervals are the first thing removed when a forecast is repeated. Every projection that reaches the public arrives with its uncertainty stripped, because uncertainty does not travel. The chapter's structure is exactly the opposite: the detailed material comes first, and then the explicit statement that the timing is unknown. Anyone who has watched a model's central estimate become a headline will recognise which half of that gets quoted.
"@

Add-ThenAndNow "019FA967-D620-7761-9914-709A1C2F8240" @"
At the arrest, Mark reports that a young man following along in a linen cloth is grabbed, wriggles out of it, and runs off naked (14:51-52). He is not named, he does nothing, he never reappears, and no other Gospel mentions him. Two verses, no function.

Detail like this is the reason historians find Mark interesting. A story being shaped for effect sheds material that does no work; a story being repeated by people who were told it by someone who was there keeps odd fragments because the fragments are what they remember. That is not proof of anything $em people invent vivid detail too, and centuries of readers have proposed identities for him $em but the pointlessness is the point. Nobody adds an anonymous naked man to an arrest scene to strengthen a case.

Memory still works exactly like this, which is why the passage feels familiar rather than strange. Ask anyone about the worst night of their life and you will get the significant events in outline and one absurd, useless, luminous detail: what was playing, what somebody was wearing, a dropped cup. The trivial fragment is often the most securely remembered thing in the account, and the first thing a tidier narrator would cut.
"@

Add-ThenAndNow "019FA967-E6B6-731C-A794-C79789E9A3AD" @"
Mark's account of the crucifixion itself is three words long in the Greek $em they crucified him $em and this chapter has taken care not to fill the silence with invention. What can be said about the mechanics comes from outside the text: a heel bone recovered from a first-century burial with an iron nail still through it, the single piece of direct physical evidence for the practice, found once in the entire archaeological record despite thousands of documented executions.

That scarcity is itself the finding, and it explains something. Crucifixion was common and left almost nothing behind, because the victims were mostly not buried in a way that preserves evidence. The one nail we have survives by accident. A practice can be widespread, well-documented in writing, and materially almost invisible.

That is the honest measure of what the physical record can do here. It establishes that the method was real, that iron nails were used, that a victim could be given a proper burial. It cannot reach a particular execution on a particular afternoon, and no amount of pressure on the evidence will make it.

Mark's restraint at this point is not squeamishness. He is writing for readers who had seen it and needed no description.
"@

Add-ThenAndNow "019FA967-F770-7A10-A82F-B8A69F2C7506" @"
The oldest manuscripts of Mark stop at a group of frightened women saying nothing to anyone (16:8). No appearance, no commission, no reunion $em the earliest complete copies simply end, mid-scene, on fear. What most readers know as the rest of the chapter is later material, in different Greek, absent from the best witnesses, and now printed with a footnote in serious editions.

So somebody finished it. Perhaps several people, at different times, because more than one ending survives. That is the most quietly astonishing fact in the book: a text this consequential was circulating with an ending its own readers found unbearable, and they wrote one.

We do this constantly and have industrialised it. The sequel nobody asked for, the alternate cut, the fan continuation, the epilogue added because test audiences wanted to know what happened to the dog. The impulse is not disrespect. It is that an unresolved ending is genuinely difficult to live with, and a community holding a story it cares about will eventually supply the resolution the author withheld.

What has changed is that we now label it. The added ending is bracketed, the variant is noted, the reader is told. Mark's readers received the addition as scripture. Ours arrives marked as a special feature.
"@

$conn.Close()
Write-Host "MARK DONE"
