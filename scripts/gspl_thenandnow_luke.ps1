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

# ============================ LUKE ============================

Add-ThenAndNow "019FA969-7C4A-7ABD-8064-462CC483E632" @"
Luke begins with a methodology statement, which is the single most unusual thing any of the four Gospels does. Many have undertaken accounts; he has investigated; he is writing an orderly narrative so that a named recipient can know the reliability of what he has been taught (1:1-4). That is a preface in the ancient historiographical manner, and it makes a specific promise: this has been checked.

We have built an elaborate apparatus for that promise and largely mislaid the promise itself. Footnotes, sourcing standards, peer review, a linked citation that can be clicked $em all of it exists to do what Luke's four opening verses claim to have done, and it does it far better. What it does not do is bind the author to anything. Luke stakes his personal credibility in the first sentence. A modern citation transfers the risk to somebody else's paper.

The other half is unchanged and slightly deflating. Luke is writing because accounts already in circulation were, in his judgement, not good enough $em which means the Gospel opens with a man dissatisfied by what everyone was already sharing. Every correction ever issued into a rumour mill has been written in that same posture, and none of them has ever fully caught up with the version that got there first.
"@

Add-ThenAndNow "019FA969-8DD2-71F0-9128-0ECCD459204C" @"
The nativity everyone can recite begins with an administrative instruction. A registration is ordered, people travel to comply, and a child is born in the overflow because the town is full of other people complying (2:1-7). Whatever one concludes about the census $em and this chapter has laid out at length why its dating is one of the hardest problems in the New Testament $em the mechanism it depends on is entirely familiar: a state wants to count people, and counting people means moving them.

We still do this, and we still do it badly. Every national census is a logistical event that reshapes households for a night, produces disputes about who belongs where, and generates edge cases nobody drafting the instruction imagined. The difference is that ours arrives as a form rather than a journey. Nobody is now made homeless for a week by the act of being enumerated.

What has not changed is the reason states count. Enumeration has never been curiosity; it is the precondition for taxation and conscription. That is what makes the chapter's juxtaposition land without any help: the imperial machinery is doing the largest thing in the scene, and the thing it accidentally produces is the part anyone remembers. Bureaucracy is remarkably good at generating consequences it has no interest in.
"@

Add-ThenAndNow "019FA969-9E9E-7651-9C3B-213724190883" @"
Luke opens his third chapter by fixing a date the only way his world could: the fifteenth year of one emperor's reign, cross-referenced against a governor, a tetrarch, his brother, another tetrarch, and a high priesthood (3:1-2). Six coordinates, because no single one was sufficient. There was no universal year-count in use across the Mediterranean $em you dated events by whoever was in power near you, which meant a date was only as portable as your reader's knowledge of local politics.

This is the most successful engineering problem humanity has quietly solved. A modern date needs no coordinates at all. It is the same number everywhere, indexed to a single origin, and it will still parse in four hundred years to a reader who knows nothing about any of our officeholders. We inherited that from a long argument this passage is a fossil of.

The joke, of course, is that the most over-specified verse in the New Testament remains genuinely difficult to convert. Regnal years could be counted from different starting points; co-regencies muddle the arithmetic. Luke did exactly the right thing by the standards available to him, with six independent handles, and we are still not entirely certain which year he meant.
"@

Add-ThenAndNow "019FA969-AF97-7AFA-991B-52064F38E463" @"
The Nazareth scene is a performance in a small room full of people who watched him grow up. He is handed a scroll, finds a passage, reads it aloud, sits down, and offers one sentence of comment $em and the room turns on him fast enough that the chapter ends with a crowd walking him to the edge of a hill (4:16-30).

Two things about this are hard to feel now. The first is that reading aloud was the normal way a text was encountered at all; silent private reading of your own copy was not an available experience for most people, so a text existed as an event in a room, performed by somebody, with a voice. The second is that the audience's fury is not about the reading. It is about the reader. They know his family. The offence is that a local has claimed standing.

That second half has not moved an inch. The hometown remains the hardest room, and the reason is structural rather than personal: everyone in it holds an earlier version of you and has not agreed to update it. The modern form is gentler $em nobody is walked to a cliff $em but the specific sting of being told what you were, by people entitled to remember, is fully intact.
"@

Add-ThenAndNow "019FA969-C063-70F5-B56D-CA0D956B34FA" @"
The chapter ends on two throwaway images about maintenance: nobody patches an old coat with cloth cut from a new one, and nobody puts new wine into old skins (5:36-38). Both assume an audience who repaired things as a matter of course. A garment was an asset with a long service life and a known repair path; wineskins aged, stiffened, and eventually failed under pressure, and knowing when one had reached the end of its working life was practical knowledge, not a metaphor.

We are the first people for whom these images require explaining, because we replace rather than repair. The economics inverted: the new coat became cheap and the hour of skilled attention became expensive, so mending is now closer to a hobby than a household competence. A modern listener understands the sayings intellectually and has to be told why the patch would tear.

What survives is the underlying claim, which is oddly technical for a religious teaching: some containers cannot take some contents, and forcing the match destroys the container. Anyone who has tried to run a genuinely new practice through an old institution's approval process has watched an old skin split. The image was chosen because it was ordinary. It has become ordinary again by a different route.
"@

Add-ThenAndNow "019FA969-D0A9-713F-9478-4AA57F3D866C" @"
The grainfield argument turns on a detail worth stopping on: nobody in the scene disputes that the disciples were entitled to the grain. Walking through a field and eating what you pick was a recognised provision for the hungry, written into the law both sides are arguing from. The objection is exclusively about the day.

That is a startlingly humane piece of legislation to find at the bottom of a religious dispute, and its logic is a genuine road not taken. The entitlement is unconditional and untested $em no application, no means assessment, no register of who deserves it. You could eat because you were hungry and present. The cost was borne, invisibly and by design, by the field's owner, and it was accepted as the price of living in a place where nobody starved within sight of food.

We have kept the goal and rebuilt the mechanism around verification. Modern relief is more generous, vastly better funded, and reaches people no gleaning right could have helped $em and it is administered through eligibility, documentation, and review, which is why a certain kind of hunger now goes unfed for want of a form rather than for want of grain. Luke's sermon in this chapter, meanwhile, is blunter than Matthew's on exactly this point, and declines to soften it.
"@

Add-ThenAndNow "019FA969-E19C-72C6-BB63-117877953132" @"
A funeral procession leaves a town gate carrying the only son of a widow (7:11-17), and Luke expects his reader to understand instantly that this is a story about destitution as much as grief. A woman without husband or son in this society had lost her standing in the household economy and her claim on anyone's obligation. The crowd is not only mourning a young man. It is watching a woman become unsupportable.

That specific arithmetic is the thing modern readers most reliably miss, and its disappearance is one of the genuine improvements of the last century. A pension, a survivor's benefit, a bank account in one's own name, the legal capacity to hold property and sign a contract $em the machinery that means a bereavement is now a catastrophe of the heart and not simultaneously the loss of one's economic existence. It is a change so complete that the text has to be annotated for it.

What has not changed is the gate. Grief still happens in public, still arrives in the middle of other people's ordinary day, and still puts bystanders in the position of deciding whether to look away or stop. The chapter's centurion, elsewhere in it, is remembered for his faith. The crowd at Nain is remembered for having been there.
"@

Add-ThenAndNow "019FA969-F317-737B-98F0-E1CB606D1FF1" @"
Buried at the top of this chapter is a sentence about money. The travelling group included women who supported it out of their own means (8:1-3), several of them named. That is a funding model, stated plainly, and it is the only place in the Gospels where we are told who was paying.

It is also completely at odds with the picture most readers carry, in which the whole enterprise floats free of logistics. Somebody bought the food. Somebody covered the cost of a dozen adults not working. Luke $em who elsewhere is the Gospel most interested in wealth and its uses $em names the patrons, and the patrons are women with independent resources, which tells us something concrete about the period: money did reach women's hands, and when it did they spent it on causes of their own choosing.

Patronage has never gone away; it has only become embarrassed about itself. Every movement, laboratory, orchestra, and campaign still runs on people who write cheques and are named in small type at the back. What has changed is the direction of the credit. Luke puts the funders in the narrative. We tend to move them to an acknowledgements page and then discuss the work as though it happened by itself.
"@

Add-ThenAndNow "019FA96A-0586-741F-A105-2CB2DEB94ADE" @"
Somewhere in the middle of this crowded chapter $em a mass feeding, a confession, a mountain, a boy healed in a valley $em sits a line about having nowhere to lay his head. Read against the rest of the chapter it functions as a job description. The ministry Luke is describing has no premises. It moves, it is fed by whoever is willing, and it sleeps where the day ends.

Itinerancy of that kind was not exotic. Roads carried traders, pilgrims, day labourers, soldiers, and the genuinely destitute, and the difference between a traveller and a vagrant was mostly whether someone at the far end was expecting you. Being on the road with a purpose and being on the road without one looked identical from a doorway.

They still do, which is the uncomfortable half. We have become efficient at the paperwork of belonging $em an address is now the precondition for a bank account, a job, a benefit, a registration $em with the result that having nowhere to lay one's head is no longer merely uncomfortable but administratively disabling. The itinerant preacher of this chapter would find the modern world materially kinder in almost every respect and would struggle, immediately and completely, to prove he existed.
"@

Add-ThenAndNow "019FA96A-17B1-74B5-9EF1-2DBF1B6097A2" @"
The road from Jerusalem down to Jericho is a real road with real gradient, descending steeply through country where a traveller could be out of sight of help for a long stretch. That is why the parable puts a robbery on it (10:30-37). The setting is not decorative; it is the ancient equivalent of naming a stretch of highway everyone in the room knows to be dangerous.

The parable's design is often missed because we have made its hero a compliment. Calling someone a good Samaritan is now unambiguous praise, which drains the story of the thing it was built to do: the Samaritan was the audience's out-group, and the two men who walked past were the respectable ones. The story is engineered so that its listeners cannot enjoy identifying with the person who helps.

What has aged best is the mechanism of not stopping. Neither passer-by is described as cruel. They had reasons $em plausible, professional, possibly religious reasons $em and reasons are what the bystander problem actually runs on. Every study of why people fail to intervene has found the same thing the parable assumes: nobody decides to be callous. They decide that this is not their errand, and keep walking at a normal pace.
"@

Add-ThenAndNow "019FA96A-2935-7B65-A953-0250EC861825" @"
Most readers arrive at this chapter already holding a version of the prayer it contains, memorised in childhood, and Luke's text is shorter than the version in their head. That gap is the interesting thing. The wording almost everyone recites is not the shorter form Luke gives; centuries of communal recitation smoothed the two versions toward the fuller, more balanced one, because that is what liturgical use does to a text. Repetition standardises.

We should recognise the process, since we run it constantly and faster. Any phrase repeated by enough people converges: the misquoted film line that replaces the real one, the proverb that acquires a tidier rhythm, the corporate value statement that drifts toward the version that scans. Once a text is said aloud in unison, the group becomes its editor, and the group edits for cadence rather than accuracy.

The chapter's other half is domestic and funnier. A man is woken after bedtime by a neighbour banging on the door, and protests that the children are already asleep with him $em a household where everyone shares a room, and one knock wakes all of them. The point being made concerns persistence. The detail that survives is the exasperation, which is the most universally verifiable fact in the passage.
"@

Add-ThenAndNow "019FA96A-3A6B-7D8C-9960-89F2E212469B" @"
Five sparrows for two of the smallest bronze coins in circulation (12:6). That is a real market price, and the sum involved was a small fraction of a day's unskilled wage $em which is the whole point of quoting it. The bird is named because it is the cheapest thing in the marketplace, plucked and skewered at the bird-stalls, and the argument being built requires an item whose triviality nobody would dispute.

Cheap protein is still the benchmark, and it has got dramatically cheaper. What has moved in the opposite direction is attention. The scarce, expensive good in Luke's marketplace was food; the scarce, expensive good in ours is an hour of someone's undivided notice. Sparrows were sold five to the coin because they were abundant and small. We now spend the equivalent freely on things that are abundant and small, and treat being properly looked at as a luxury purchase.

Then the passage escalates, and the escalation is what makes it worth reading twice: from the cheapest item on the stall to the hairs of your head, all numbered. It is a claim about accounting, not sentiment. Somebody, it insists, is keeping a ledger at a resolution finer than anything the market bothers to price.
"@

Add-ThenAndNow "019FA96A-4BD0-7EC1-BE40-6B20CFE5E550" @"
The chapter opens with people bringing news of two disasters $em one a state killing, one a building collapse $em and the question underneath them is one nobody has ever stopped asking: were the victims worse than everyone else (13:1-5)? It is put as a theological question. It is functionally a question about whether the world is safe for the innocent, and the answer given refuses the premise flatly.

That refusal has aged extremely well, because the instinct it refuses is undiminished. Every disaster now generates the identical search within hours: a pre-existing condition, a lifestyle, a decision, a warning ignored. The reasoning is not usually cruelty. It is self-protection $em if the collapsed tower selected for something, then a person who lacks that something is safe. Attributing catastrophe to a fault in the victim is how a bystander buys immunity.

What has genuinely changed is the second question we now ask, which the chapter does not consider at all: who built the tower, and was it built properly. We have engineering standards, inquiries, liability, and the concept of a preventable death. The ancient instinct asked what the dead had done. The modern one at least sometimes asks what the builder did $em though it still asks about the dead first, and usually louder.
"@

Add-ThenAndNow "019FA96A-5DD7-7E47-8245-15A9F5A3CFC4" @"
Two-thirds of this chapter happens at a dinner party and is about the dinner party: where to sit, who gets invited, who accepts, and what an invitation costs (14:7-24). The seating advice is practical because the seating was legible $em a table had a top and a bottom, guests were placed according to standing, and being moved down in front of everybody was a public demotion. Choosing a low seat and being moved up was the safe play. This is etiquette as risk management.

We abolished the visible hierarchy and kept every bit of the game. The round table, the open-plan office, the conference with no head seat: the ranking simply moved to things that cannot be photographed. Who is invited to the smaller dinner afterwards. Who is on the thread. Whose calendar the meeting is arranged around. The modern version is harder to read, which makes it harder to opt out of, since you cannot deliberately take the low seat if nobody will tell you where it is.

The parable's sting is in the RSVPs. Every guest declines with a reason that is entirely reasonable $em property, livestock, a marriage. Nobody is rude. The banquet fails for want of a single person willing to treat it as more important than their own perfectly good excuse.
"@

Add-ThenAndNow "019FA96A-701D-705E-86AB-2A1586EC255D" @"
The middle parable of the three is the one that quietly explains the other two. A woman loses one of ten silver coins, lights a lamp, and sweeps the house until she finds it (15:8-10). Her behaviour looks disproportionate until you price the coin. A single silver piece of that kind approached a day's wage; ten of them were plausibly a household's accessible savings. She is not fussing over a trinket. She has lost roughly a tenth of everything liquid she has, in a dirt-floored house, in lamplight.

We can still feel this, but only by translating it out of coins and into a category. The modern equivalent is not a dropped coin; it is a payment that fails, a card frozen abroad, a deposit that has not landed. The scale is what makes the search rational, and the scale is exactly what a modern reader loses when the word coin arrives carrying its current triviality.

The third parable then runs the same logic through an inheritance, where the sums are enormous and the loss is a person. Its most historically grounded detail is the least sentimental: the younger son asks for his share early, which was a real and legally awkward request, and the father grants it. The extravagance in that story starts long before the party.
"@

Add-ThenAndNow "019FA96A-82EC-7827-8630-C88454A484EE" @"
This chapter contains the New Testament's strangest piece of financial advice and its most pointed act of record-keeping, and they belong together. A manager facing dismissal goes to his employer's debtors and writes down what they owe, buying himself goodwill with money that is not his $em and is commended for shrewdness (16:1-9). Generations of readers have tried to make this parable behave. It declines.

The second half is where the accounting turns. A rich man and a beggar both die, and the beggar has a name $em Lazarus $em while the rich man, in the most quietly devastating editorial choice in the Gospel, does not. In a world where being remembered depended almost entirely on having the resources to be commemorated, that inversion is the whole argument, executed without a word of commentary.

We have industrialised commemoration and the same asymmetry holds. The wealthy are named on buildings, in registers, in obituaries written in advance. The poor are counted. A modern reader can look up the net worth of almost anybody and will struggle to find the name of a single person who died unhoused in their own city this year. Luke knew exactly what he was doing when he handed out the one name in the story.
"@

Add-ThenAndNow "019FA96A-9507-7DFD-8E43-53E7682266FD" @"
Ten are healed; one comes back to say so; the one who returns is a Samaritan (17:11-19). The arithmetic is the point, and it is the most quietly recognisable statistic in the Gospels, because the ratio has never improved. Nine people received exactly what they asked for and went straight on with their lives, which is not villainy. It is what relief does to gratitude: the urgency that produced the request evaporates the moment the request is granted.

The social mechanics behind it are worth naming. Thanks, in this culture, was not merely a courtesy $em acknowledging a benefit created an ongoing relationship and a debt of honour. Going back to thank the healer was a substantive act with obligations attached. Nine men declining to incur that is a rational choice, not just an ungracious one.

We have made gratitude cheap and frictionless, and thereby changed what it costs to withhold. A thank-you now takes seconds and creates no obligation whatsoever, and we still do not send it. What has genuinely changed is the ninth man's excuse. He had to walk back. We have to open an application, and somehow the ratio comes out about the same.
"@

Add-ThenAndNow "019FA96A-A5E2-7CEA-A466-C3D869572B74" @"
A widow with a grievance keeps returning to a judge who has no interest in her case, and eventually wins because he cannot stand the repetition (18:1-8). The judge is described as neither pious nor fair. He grants the claim to be left alone.

This is a portrait of access to justice, and its realism is the point. A woman in her position had no leverage, no advocate, and no procedural clock forcing anyone to act; the only instrument available to her was her own persistence, deployed against an official who could ignore her indefinitely at no cost. The parable's premise is that this actually works, which is a bleak thing to be able to assume.

We built the clock. Filing deadlines, statutory time limits, a right to have a case heard, legal aid where it survives $em an entire architecture designed so that outcomes do not depend on who can outlast whom. Then we underfunded it, and produced backlogs measured in years, with the result that persistence has quietly returned as the deciding factor. Every clerk who has watched one claimant call every week and another give up after the second letter knows how this parable ends, and knows the judge in it is not a caricature.
"@

Add-ThenAndNow "019FA96A-B6EA-72C1-8FC8-CD6FA1ABA179" @"
Zacchaeus is introduced by job title, and the job title is the story. A chief tax collector in this system was not a salaried official but a contractor with a franchise: the right to collect was bought, the sum owed upward was fixed, and whatever could be extracted above that line was the collector's income. The role was legally legitimate and socially poisonous for a coherent reason $em its profit margin was, structurally, whatever the collector could squeeze.

That is why his response lands as it does. He offers restitution at multiples, not an apology (19:8). He is doing accounting, in public, against a figure he can calculate because he kept the books. Repentance here is denominated.

We removed the incentive and kept the resentment. Tax collection is now performed by salaried officials whose pay does not rise with what they extract, which is one of the more successful institutional reforms in history and almost never celebrated as one. The tax collector remains the least-loved figure in public life anyway, on inherited feeling rather than present evidence. And the specific mechanism Zacchaeus escaped $em paying for the right to collect, then recouping it from whoever cannot argue $em survives wherever fees, fines, and enforcement quotas fund the body doing the enforcing.
"@

Add-ThenAndNow "019FA96A-C70E-7958-AB73-945FD5159F40" @"
The whole chapter is one confrontation in a public courtyard, conducted by rules everybody present understood. A delegation asks a question designed to trap; the answer is a counter-question they cannot answer without cost; they withdraw and send the next group (20:1-8). Nobody is persuaded of anything. That is not a failure of the format $em it is the format.

Public disputation was a recognised genre, with a live audience, and the audience is who both sides are playing to. Winning meant your opponent falling silent in front of witnesses. That is why the counter-question is such an effective weapon: it transfers the cost of answering, in public, to the person who set the trap.

We kept the genre and lost the discipline. The televised debate, the select committee hearing, the hostile interview, the thread that exists to be screenshotted $em all of it runs on the same understanding that the exchange is a performance for third parties and the participants will not change their minds. What we abandoned is the requirement to actually answer. A first-century questioner who deflected would lose the room. A modern one can talk past the question at length and expect to be scored on delivery.
"@

Add-ThenAndNow "019FA96A-D7AB-79F9-B44B-5930429E810C" @"
A woman drops two small coins into a temple collection box, and the observation made about it is arithmetical rather than sentimental: the wealthy gave from surplus, she gave what she had to live on (21:1-4). This is a claim about proportion, and it requires knowing that the coins involved were the smallest denominations in circulation $em the kind of money that could not be subdivided further.

Proportional giving is now a solved measurement problem and an unsolved cultural one. We can calculate exactly what a gift represents as a share of income, and philanthropy is nevertheless still reported in absolute totals, because absolute totals are what buy naming rights. The gift that costs the giver nothing continues to be announced, and the gift that costs everything continues to be invisible unless somebody is standing there doing the maths.

The rest of the chapter looks at the same building and predicts it will come down, stone by stone. The two halves are usually read separately. Together they make a sharper point than either does alone: the institution collecting her last two coins had, on the text's own account, a limited remaining lifespan, and nobody administering the collection had any idea. She was funding something with an expiry date. So, generally, are we.
"@

Add-ThenAndNow "019FA96A-E7D2-741F-BCDF-381A086FF909" @"
Almost everything in this chapter happens after dark, and the darkness is doing real work. A meal at night, a walk to the Mount of Olives, an arrest by torchlight, a courtyard fire, three denials, and a rooster (22:34, 22:54-62). The rooster is not a picturesque detail. It is the clock. Night was divided into watches, and the bird marked the approach of dawn for a population with no reliable way to measure the hours in between.

We abolished the night as a constraint and thereby abolished the specific dread this chapter runs on. Artificial light means proceedings can be held at any hour and nobody is disoriented by it; a modern reader has no bodily sense of a night that could not be shortened, brightened, or checked against a clock. The rooster has to be footnoted.

But the courtyard has not changed at all. A man stands at a fire among people who are discussing him, is recognised by his accent, and denies it three times in escalating panic. Being identified as one of them by the way you speak, in a group that has not decided yet how it feels about them, is an experience available in any city on earth tonight. That part needs no explanation whatsoever.
"@

Add-ThenAndNow "019FA96A-F7DB-7C55-A3FB-B537AC6B6848" @"
Luke alone reports the referral. Learning that the accused is a Galilean, the governor sends him to the tetrarch whose territory that is, and the tetrarch sends him back (23:6-12). No other Gospel has this hearing, and its historicity is genuinely debated. What is not in doubt is that the manoeuvre itself was available and would have made sense to anyone reading.

It is jurisdiction-shopping, and it is one of the oldest instruments in government. A politically expensive decision arrives; the official who holds it looks for someone else with a colourable claim to own it; the case travels. Nothing about the accused changes during the journey. What changes is who will be blamed.

We have written whole bodies of law to constrain this $em venue rules, extradition standards, the principle that a case must be heard by the court that properly holds it $em and it happens anyway, at every level, because the incentive is structural rather than personal. Any official facing a decision that will cost them something has a reason to discover that it is somebody else's decision.

The chapter's other half is an execution, and the record around it is thinner than tradition suggests. The darkness at midday, in particular, cannot be corroborated by anything astronomical.
"@

Add-ThenAndNow "019FA96B-0852-788F-A080-F71808F0DC08" @"
Two people walk about seven miles and talk the whole way (24:13-35), and the destination cannot be located. Manuscripts disagree about the distance; the candidate sites that match the shorter reading have no ancient claim to the name, and the site with the oldest claim sits at roughly the longer one. Emmaus is a real village that has mislaid its coordinates.

That is a useful thing to end a Gospel on, and it is worth resisting the urge to solve it. Places do get lost. A settlement can be named in a text, excavated in three candidate locations, and still not be identified, because the evidence that would settle it $em a name on a stone, an unambiguous itinerary $em was never written down by anyone who assumed the question would arise.

What has changed is that we can no longer be lost in this way. Everything is coordinates now; nothing walked is unrecorded. What has not changed is the seven miles. That was an ordinary afternoon's distance for people who moved at three or four miles an hour as a matter of course, and it is exactly long enough for the conversation the chapter needs. Nothing in the story would work if they had been able to drive.
"@

$conn.Close()
Write-Host "LUKE DONE"
