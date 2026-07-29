$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
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

# ============================ JOHN ============================

Add-ThenAndNow "019FA96C-1C8D-7943-95A0-EC520EBA1EA4" @"
John opens with an abstract noun and no story at all, and the noun does not survive translation. Logos carried a working range in first-century Greek $em speech, account, reason, the ordering principle behind things, the reckoning you give when audited $em and English has to pick one. It picked Word, which captures the speech end and loses the accountancy and the cosmology.

Every translated text has a handful of these, and they are where reading becomes an act of trust. A modern reader has more help than any previous generation: parallel translations, lexicons, interlinears, a search that returns every other use of the term in the corpus. What they do not have, and cannot get, is the experience of a word arriving already loaded. To John's first audience, logos came with philosophical freight attached. To us it arrives as a capitalised curiosity.

The opening move itself is the confident part. Three of the four Gospels begin with events; this one begins with a proposition and dares the reader to keep going. That is a choice any writer would recognise as risky, and it is the reason this Gospel reads differently from the other three all the way through. It has told you in the first sentence that it is going to argue rather than report.
"@

Add-ThenAndNow "019FA96C-2D2C-71D5-B6CB-4B13CC2CFD5B" @"
The jars at the wedding are specified as stone (2:6), and the material is not decoration. Stone vessels were manufactured in this period on a genuinely industrial scale $em quarried, lathe-turned, and distributed widely $em for a specific legal reason: stone was held not to contract ritual impurity the way pottery did. A ceramic jar that became impure had to be broken. A stone one could be cleaned and kept.

So a household with six large stone jars was making a considerable capital investment in order to comply with purity law without repeatedly destroying its own crockery. This is one of the more satisfying things archaeology has done for the Gospels: workshops and vessel fragments turn up in quantity, confirming that the detail is not colour but ordinary observation of an actual industry.

Choosing a material for its regulatory properties rather than its performance is a habit we have kept and expanded. The packaging selected because of what it may legally contain, the flooring specified to satisfy an inspection, the cladding chosen to a standard rather than to a purpose. Compliance shapes the physical world constantly and mostly invisibly, and future archaeologists will read our buildings the same way $em working out, from the materials alone, which rules we were trying to satisfy.
"@

Add-ThenAndNow "019FA96C-3D69-7470-ABE8-8DB126CEB6FB" @"
The conversation with Nicodemus runs on a pun, and the pun is the plot. The Greek word Jesus uses can mean either again or from above, and Nicodemus takes the first sense $em how can a grown man be born a second time $em while the speaker meant the second. The misunderstanding is not stupidity. It is the ordinary hazard of a word with two live meanings, and the whole exchange is built on it.

Which means the passage cannot fully work in any language that lacks the ambiguity, and English does lack it. Translators have to choose, footnote, or fudge. Countless sermons have been preached on born again to congregations who were, strictly, reading Nicodemus's mistake rather than the reply.

We have the same problem at industrial scale and have stopped noticing. Machine translation now moves enormous volumes of text between languages while flattening precisely this feature $em the double meaning, the register, the joke that depends on one word doing two jobs. It is remarkably good and it cannot preserve a pun. Anyone who has watched an idiom come back from an automatic translation as nonsense has met Nicodemus, who was not being obtuse. He simply heard the other meaning, which was there to be heard.
"@

Add-ThenAndNow "019FA96C-4DDB-7B4D-B990-A34B5D80427C" @"
The whole encounter happens because somebody had to fetch water, and fetching water was work. A household needed it daily, in quantity, carried by hand from a fixed source, and the task fell overwhelmingly to women. That is the labour the scene interrupts $em and the reason a well was one of the few places a stranger could reliably meet a local woman in public with a plausible reason to speak.

This is the single largest material change between that world and a piped one, and it is almost never listed among the great transformations. Running water abolished a daily, heavy, unpaid, gendered task that consumed a substantial share of the waking hours of half the population, in every settlement, everywhere, for all of recorded history until very recently. It is still not abolished for everyone, and where it is not, the same journeys are still being made by the same people.

The conversation itself turns on a boundary $em two groups with a long, specific quarrel about where God should properly be worshipped, both of whom knew exactly which side of it the other stood on. The well is neutral ground only in the sense that thirst is. Both parties needed water, and the water did not care.
"@

Add-ThenAndNow "019FA96C-5EA0-7E7A-B025-CF3F824AC465" @"
For a long stretch, the five porticoes at the pool were treated as a problem. The number looked schematic $em five colonnades is an odd, almost allegorical arrangement $em and the detail was cited as evidence that this Gospel's author was working from imagination rather than knowledge of Jerusalem. Then excavation in the area found a twin-pool complex whose layout can accommodate exactly that arrangement: four sides and a dividing colonnade between the two basins. Five.

That is the most useful cautionary tale in this book, and it cuts in a specific direction. It does not confirm the healing. It confirms the architecture $em which is to say it confirms that whoever wrote this knew the city. That is a real finding with tightly limited scope, and the temptation to spend it on the larger claim should be resisted, because the same discipline is what makes the finding worth anything.

We are structurally prone to the mistake that was made here. Absence of evidence for an odd detail gets promoted to evidence of invention, because the odd detail is the one we notice. The corrective is boring and reliable: an unverified specific is unverified, not false, and the difference matters most precisely when we are confident. The porticoes were there the whole time. Nobody had dug yet.
"@

Add-ThenAndNow "019FA96C-6EC5-7B1B-AD5A-1ABB5EAF35E9" @"
The loaves in this chapter are barley (6:9), and this Gospel is the only one that says so. It is a class marker. Barley was the cheaper grain $em hardier, less valued, priced well below wheat $em and it was what you ate if wheat was out of reach. A boy carrying barley loaves is not carrying a picnic. He is carrying poor food.

Grain hierarchy was legible to everyone in the ancient Mediterranean and has almost entirely inverted. Barley is now a specialty item, sold at a premium in places that also sell white bread cheaply, and coarse dark loaves cost more than refined white ones almost everywhere. The grain that signalled poverty for four thousand years is now shelved as a wellness product. Nothing about the barley changed. What changed is that scarcity moved: refinement became cheap, so roughage became a choice.

The chapter's other half is a crowd walking away. After a discourse they find intolerable, many of his own followers leave (6:66), and the text does not soften it or explain it away. It is the only place in the Gospels where a teaching is reported as having cost him his audience $em recorded, apparently, because it happened, which is not how anyone builds a recruitment document.
"@

Add-ThenAndNow "019FA96C-804E-712E-AAC7-D41FEF99213F" @"
The declaration about thirst is made on the last and greatest day of a festival that, by the period's own accounts, involved water being drawn and poured out at the altar. Standing up in that setting and inviting the thirsty to come is not a general metaphor. It is a man timing a claim to the exact moment the crowd is watching water being carried.

Water rituals concentrate where water is scarce, and this was a city that depended on cisterns, aqueducts, and the discipline of catching a limited rainy season and holding it through a long dry one. A festival organised around pouring water out is only extravagant $em only meaningful $em in a place where everybody present knows precisely what it cost to have it.

We have engineered our way out of that anxiety so thoroughly that the gesture has become opaque. Water arrives on demand, at drinking quality, in unlimited volume, for a price most households do not examine, and the result is that pouring it out ceremonially reads to us as waste rather than as offering. Whole regions are now rediscovering the older feeling through drought orders and reservoir levels, which is the fastest available route back into this chapter: scarcity is what made the ceremony legible.
"@

Add-ThenAndNow "019FA96C-918E-7017-B6C5-0FBFC1087903" @"
The story of the woman brought for judgement is the most beloved passage in this Gospel and it is not, on the manuscript evidence, part of it. The earliest and best copies do not have it. Later manuscripts that do have it cannot agree on where it goes $em it turns up at different points in John, and in at least one family of manuscripts it migrates into Luke entirely. It is a passage without a fixed address.

That is a strange status for a story nobody wants to lose, and the honest position is the awkward one: the manuscript case against it belonging here is strong, and the separate question of whether something like it happened is genuinely open, since a floating tradition can be old and true and still not be original to the book it ended up in.

We generate this constantly and are worse at tracking it. The quotation attributed to whichever famous person seems plausible, the anecdote that attaches itself to a different institution each decade, the line everyone knows and nobody can source. Our version has an advantage $em provenance can usually be checked in minutes $em and a disadvantage, which is that we almost never bother, because the story is good and the attribution is a detail.
"@

Add-ThenAndNow "019FA96C-A291-7DFA-B5F6-1BCA319ED9C1" @"
Most of this chapter is an interrogation, and it is conducted the way interrogations actually are: the same witness questioned more than once, in the hope that the account will shift. It does not. He gives a plain answer, is recalled, is instructed on what conclusion to reach, and declines $em then produces the driest line in the Gospels, that whatever else is unclear, he was blind and now sees (9:25).

The pressure applied to him is social rather than physical, and the chapter is explicit about the instrument: his parents refuse to answer because they are afraid of being put out of the synagogue (9:22), which would have meant exclusion from the community they lived inside. The threat is not violence. It is unbelonging.

That instrument is fully operational and has been extended. Expulsion from a professional body, removal from a platform, exclusion from the group chat that runs the industry $em the sanction that works is still the one that removes you from the room where your life happens. What has changed is that most of us now belong to several rooms, so the threat is survivable in a way it was not for a family with one synagogue. And the witness's defence has not been improved on. Describe only what you know, and let them ask again.
"@

Add-ThenAndNow "019FA96C-B3A1-7AB4-B943-A964D30342D8" @"
The shepherd imagery in this chapter is not pastoral in the soft sense. Shepherding was low-status, outdoor, badly paid work performed at the edges of settlement, and the details the discourse leans on are technical: sheep sorted by voice rather than by marking, a fold shared by more than one flock, the risk that arrives when the person watching is paid rather than invested (10:11-13). Anyone who has kept animals recognises the hired-hand problem immediately.

The voice detail is the one worth pausing on because it is verifiably true of the animals. Sheep do learn and respond to a specific familiar voice, and a shepherd calling his own out of a mixed fold is describing ordinary practice, not a marvel. It works because the relationship is long and the sound is consistent.

We have replaced the voice with the tag. Identification now runs on ear tags, chips, and databases, which is enormously more reliable and completely indifferent to relationship $em a scanner does not need to have met the animal. That is the trade the whole modern world runs on: recognition transferred from people who know you to systems that can verify you. The chapter's claim, made in a mixed fold in winter, is that the first kind still knows something the second cannot check.
"@

Add-ThenAndNow "019FA96C-C4E0-73D8-8776-231EE5C145F8" @"
The four days are load-bearing. The narrative insists on them, and Martha's objection at the tomb about what four days will have done is the practical detail that makes the scene grim rather than gentle (11:39). In a hot climate without embalming, four days was past any threshold of doubt.

That threshold existed because determining death was genuinely difficult, and everyone knew it. Without instruments, the diagnosis rested on observation over time $em breath, pulse felt by hand, cooling, and finally the unambiguous evidence of decomposition. Premature burial was a real anxiety for a long time afterward for exactly this reason, and it produced waiting mortuaries and bells on coffins well into the modern era.

We fixed this and then discovered the fix had a philosophical bill attached. Death is now determined by criteria $em cardiac, or neurological $em administered by clinicians, written into statute, and argued over in court, because ventilators and transplantation made the moment consequential in ways no earlier society had to adjudicate. Martha's four days were crude and completely unambiguous. Our criteria are precise and contested. The story assumes a world where nobody needed a definition, only a calendar, and that is the part a modern reader has to work hardest to reinhabit.
"@

Add-ThenAndNow "019FA96C-D64E-70A6-BCC4-7027871C0FB3" @"
The perfume is priced in the text, and the price is the scandal: an objection is raised that it was worth about a year's wages for a labourer (12:5). Nard of that quality came a very long way $em its source lay in the mountains beyond the empire's eastern edge, and it reached Judea through a trade network running overland and by sea, handled and marked up at every stage. That is why a small quantity cost what it cost. The distance was in the price.

Long-distance luxury supply chains are older and more sophisticated than the modern world likes to assume, and they worked the same way ours do: a commodity with a high value-to-weight ratio, moved by intermediaries who never met each other, ending up in a household that could not have named the region it came from.

What has changed is the markup and the visibility. Distance has become nearly free, so the premium on a far-travelled thing has collapsed, and we can trace a shipment in real time and mostly choose not to look. A jar of imported fragrance today costs a rounding error against a year's earnings. The gesture in this chapter only registers if the number does, which means the objection $em that the money had other uses $em is the part that still needs no explaining at all.
"@

Add-ThenAndNow "019FA96C-E75D-71B3-9D66-C851C19E9A7B" @"
Foot washing was a real service performed on real feet, and the feet are the point. Everyone walked, on unpaved roads, in open sandals, through dust in the dry season and mud in the wet, past animals. Arriving anywhere meant arriving filthy from the ankle down, and washing a guest's feet was a genuine practical necessity as well as a courtesy $em which is why the task belonged to the lowest-ranked person available, and why a teacher doing it registered as a deliberate inversion rather than a gentle symbol.

The physical fact has quietly disappeared. Paved surfaces, closed shoes, and the removal of livestock from streets mean the modern equivalent of arriving on foot from a day's journey does not exist for most people. The gesture has become abstract, and abstraction has made it safer: it is easier to admire as a symbol of humility than it was to receive from someone holding your actual dirty foot.

What survives exactly is the discomfort of the recipient. The objection raised at the table is not modesty $em it is the specific awkwardness of being served by someone you have placed above yourself, which reorders a relationship you were relying on. Anyone who has watched a senior person pick up the wrong end of a job knows that flinch.
"@

Add-ThenAndNow "019FA96C-F81C-74B1-82C9-81F4FB8F94B1" @"
The word promised here $em rendered Helper, Comforter, Counsellor, or Advocate depending on the translation $em is drawn from the vocabulary of legal support. A paraklētos was someone called in to stand alongside a party, to speak for them, to lend their standing to a case. It is not primarily a word about consolation, and the softer English options have quietly moved it in that direction.

Which matters, because being represented is not the same as being comforted. An advocate does not make you feel better; they make you answerable in a forum you could not otherwise navigate. The promise, read closely, is not warmth. It is representation.

That distinction has become one of the sharpest in modern life, and the gap between having a case and being able to make it is now where most outcomes are decided. Legal aid, duty solicitors, patient advocates, union representation, the ombudsman $em an entire class of institutions exists because the ancient insight holds: an unrepresented person facing a system loses, regardless of merit.

What has changed is that we discovered the same thing and priced it. Representation is now a purchasable good, which was true in this period too, and remains the least defensible feature of every legal system that has ever operated.
"@

Add-ThenAndNow "019FA96D-090A-7773-A9D3-7CDDE6929C7D" @"
Pruning is skilled destruction. A vine left to grow puts its energy into wood and foliage and produces poor fruit; the cutting back, done hard, in the right season, by someone who can tell a productive cane from a passenger, is what makes a crop possible. The image assumes an audience who had watched it done and knew that the person with the knife was not damaging the plant.

That expertise has become invisible rather than obsolete. Viticulture is more technical now than it has ever been, and almost nobody outside it has watched a vine be cut. The result is that the metaphor lands abstractly $em pruning as a synonym for loss $em when its original force was about judgement: knowing what to remove, which is a competence, not a sentiment.

The other half of the chapter is a promotion. He tells them he no longer calls them servants but friends (15:15), and the distinction was concrete in a household economy. A servant received instructions; a friend $em in the sense that mattered $em received the reasoning. Being told why is the whole content of the upgrade.

Every organisation still runs this distinction and mostly gets it wrong, on the assumption that people need tasks. The chapter's claim is that what separates the two roles is access to the reason.
"@

Add-ThenAndNow "019FA96D-19B5-756C-9B0B-ABDE952E3C34" @"
The disciples cannot parse the phrase a little while, and say so repeatedly (16:16-18). They are not being slow. They are asking a question their language could not easily answer: how long is a little while? Duration without instruments is comparative $em a walk to the next village, until the lamp needs filling, till the barley comes in $em and there is no way to convert any of that into a number that would settle the matter.

Then the passage does something better than an answer and offers a comparison instead: a woman in labour, for whom the pain is total while it lasts and afterwards is not remembered in the same way (16:21). That is an appeal to a specific, embodied, widely shared piece of knowledge about how the memory of pain behaves, and it is doing real work $em duration explained through experience because it cannot be explained through measurement.

We have the number now, and the number turns out not to help much. Anyone told a procedure will take twenty minutes, or a recovery six weeks, knows the figure does almost nothing to the experience of being inside it. We can measure the interval precisely and have made no progress whatsoever on the thing the disciples were actually asking, which is how long it will feel.
"@

Add-ThenAndNow "019FA96D-2AAE-75B5-89EC-8C7D54E10248" @"
This chapter is a prayer, spoken aloud in a room, and written down by somebody who was in it. That is a strange artefact when you look at it directly: private speech, preserved because it had an audience it was not addressed to.

The situation is not unusual for the ancient world $em there was no expectation that speech in a room was confidential, and no technology for keeping it so. What was said in front of people was, effectively, published, and anybody's account of it was as good as their memory. Rumour, testimony, and record ran on the same rail.

We built a wall between overheard and recorded, and then knocked it down with a device in every pocket. Speech in a room is now capturable perfectly, permanently, and without consent, and the resulting etiquette is a mess: we hold a strong intuition that private words should stay private and a weaker one about what to do when they have already been captured. Careers now turn on the difference between what somebody said and what somebody kept.

The prayer's own content includes a request for people who are not in the room and do not yet exist $em which is either the most gracious line in the Gospel or the least verifiable, and is in any case addressed past the disciples to the reader.
"@

Add-ThenAndNow "019FA96D-3AF3-70C2-8D25-0CB23E7F4203" @"
This Gospel sends the prisoner first to Annas, who was not the high priest that year $em his son-in-law was $em and holds the hearing there anyway (18:13). No other Gospel has this stop, and it is the most politically astute detail in the chapter.

Annas had held the office, been removed by Rome, and remained the centre of gravity of a family that supplied high priests for decades. Formal authority sat with the incumbent. Actual authority sat with the man who had been there longest, knew everyone, and could not be dismissed because he held no office to be dismissed from.

That figure exists in every institution and has never been legislated out of one. The chair emeritus who is still consulted, the founder without a title whose opinion decides the matter, the former minister whose call the department still returns, the retired partner with an office. Organisation charts describe reporting lines; they do not describe influence, and anyone who has worked anywhere has learned to find the room where the decision is actually made before the meeting where it is announced.

The person brought to Annas first was, on this account, taken to the correct address. The formal hearing came afterward, which is the usual order.
"@

Add-ThenAndNow "019FA96D-4B04-7A19-B374-9FFEAD467C48" @"
The notice fixed above the cross was written in three languages $em Hebrew, Latin, and Greek (19:20) $em and the list is a precise description of how that city functioned. Greek was the shared language of the eastern Mediterranean and of trade; Latin was the language of the occupying administration and the army; Hebrew, with Aramaic alongside it in daily use, was the language of the local population and its scripture. A public notice intended to be read by everybody present needed all three.

Multilingual officialdom is not a modern invention and not a courtesy. It is what a state does when it needs a message to be understood by populations it governs but does not share a language with, and the order of the languages usually tells you who the state thinks matters.

We produce the same object constantly $em the multilingual sign, the safety notice in four languages, the ballot paper, the form issued in the languages a district actually speaks $em and the same politics attaches to it: which languages are included, which are omitted, which appears first. The notice in this chapter was written to be read by everyone on that road. Its trilingual form is the clearest surviving evidence of who was expected to be walking past.
"@

Add-ThenAndNow "019FA96D-5C2E-7177-B5DE-1F6484205004" @"
Thomas wants to put his hands on the evidence before he accepts the report, and for this he has been the byword for faithlessness for two thousand years, which is a fairly harsh outcome for a man asking to see the primary source. He is not refusing testimony in general. He is declining second-hand testimony about an extraordinary claim, which is the standard every serious investigative practice has since adopted.

The chapter, to its credit, gives him what he asks for and does not pretend the request was unreasonable, though it does add a line about those who believe without it.

Our position is stranger than his. We have built the largest apparatus for verification in history $em records, provenance, forensics, replication $em and we personally verify almost nothing. Nearly everything a modern person knows arrives on trust: the medicine works because a system says so, the bridge holds because someone certified it, the event happened because a source reported it. We are structurally more credulous than Thomas and vastly better protected, because the checking has been institutionalised and delegated rather than done.

Which makes his demand the recognisable one after all. Everybody wants to touch it themselves. Almost nobody can, about anything, which is why the systems that check on our behalf are worth more than our own eyes.
"@

Add-ThenAndNow "019FA96D-6C8A-712C-AB07-9F831ADF857D" @"
A hundred and fifty-three (21:11). The number is exact, it is unnecessary, and it has generated more allegorical mathematics than any other figure in the New Testament $em triangular numbers, species counts, gematria, all of it ingenious and none of it verifiable.

The plainer possibility is the one commercial fishing makes obvious: they counted because that is what you do with a catch. Fish had to be divided among crew, tithed, sold, salted, or accounted for to whoever owned the boat, and a haul was therefore tallied on the beach as a matter of course. An exact number in a fishing story is the least surprising detail available. It is what the morning after a good night looks like in a ledger.

That habit has scaled into everything we do. We count the catch, and then the click, the step, the hour, the engagement $em on the same instinct, which is that a thing counted can be divided, compared, and defended. The tally is how a shared endeavour avoids an argument.

So the last chapter of this Gospel ends, almost, on an inventory. After the discourses and the trial and the empty tomb, seven men on a beach with a fire going, and somebody sitting down to count the fish.
"@

$conn.Close()
Write-Host "JOHN DONE"
