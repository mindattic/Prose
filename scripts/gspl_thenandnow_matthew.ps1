$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
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
    # Skip if this chapter already has a Then and Now beat (idempotent re-runs)
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

# ============================ MATTHEW ============================

Add-ThenAndNow "019FA049-5D94-766F-A919-4623FD605028" @"
Matthew's opening is a list, and the list does its arithmetic out loud: three sets of fourteen, announced as such (1:17), with the third set delivering only thirteen names when you count them on your fingers. A modern reader reaches instinctively for the proofreading explanation. A first-century reader would more likely have reached for the pattern, because a genealogy in this world was an argument about entitlement rather than a record of biology. It existed to establish what a man had a right to be called, and compressing it to make a shape was ordinary practice, not fraud.

What has changed is that ancestry is now answered by a cheek swab, with a precision no one can negotiate with. What has not changed is the wanting. The same appetite that made Matthew count to fourteen three times sends people to a laboratory hoping to be told they are partly something interesting.

The difference is that we have pulled apart two questions Matthew held together. He was not asking where Jesus came from; he was asking what Jesus was owed. A test result answers the first question exactly and cannot touch the second $em which is roughly why people find the results faintly deflating, and then frame them anyway.
"@

Add-ThenAndNow "019FA049-8E60-70CC-BFAB-692BDB97D336" @"
Strip the pageantry from chapter two and what remains is a family leaving in the dark because someone in power has taken an interest in their child, crossing into Egypt because Egypt is where you went, and then choosing their return address based on which son of the dead king had inherited which province (2:22). Joseph is not consulting a prophecy at that moment. He is doing what displaced people have always done: pricing the risk of one jurisdiction against another.

That calculation is the most contemporary thing in the chapter. Egypt was the reflexive destination for Judean refugees because it was close, it was fertile, it was outside Herod's writ, and it already held communities who spoke your language $em the same four criteria any family fleeing a border today would recognise, unchanged.

What has changed is the paperwork, and the fact that we now count. The strongest thing history can say about the massacre of the infants is that no independent record of it survives, which for this period is genuinely weak evidence of absence: nobody was keeping a register of the children of a small hill village. That is the real distance between then and now. Not that power has stopped killing the inconvenient, but that it has become much harder to do it unrecorded.
"@

Add-ThenAndNow "019FA054-FBB7-7C0A-AFA4-DF042B65F960" @"
John's wardrobe is a credential. Camel hair and a leather belt (3:4) is not what a man wears because he owns nothing else; it is a legible costume, borrowed from the prophetic wardrobe of the Hebrew scriptures, and anyone standing on that riverbank would have read it instantly $em the way a stethoscope worn in a corridor is read now. The diet works the same way. Locusts and wild honey advertise a man who takes nothing from the ordinary economy and therefore owes it nothing.

We have kept the whole mechanism and moved it downmarket. Visible renunciation is still the fastest way to be believed: the founder who does not own a suit, the ascetic diet announced in the first thirty seconds, the deliberate ostentation of not having. The difference is that John's austerity cost him something irreversible, and ours is usually a purchase.

The other half has not moved at all. Crowds went out to the Jordan, according to the independent notice of a non-Christian historian, because there was an appetite for someone who was visibly not on the payroll. That appetite has never once gone out of season. We simply have more people willing to dress for it.
"@

Add-ThenAndNow "019FA063-93C6-708B-8B3E-90703FF50C84" @"
The most consequential sentence in chapter four is not spoken in the wilderness. It is the one where two brothers put down the nets and walk (4:18-22). A fishing operation on that lake was not a job in the sense we use the word $em it was a family asset, a boat, a set of gear, a share in a rhythm of labour that had produced your father and would have produced your sons. Leaving it was not resigning. It was walking away from the only inheritance on offer.

That is the part hardest to feel now, because we have spent a century learning to treat work as a thing one changes. We think of leaving a job as a risk to income. They would have understood it as a risk to identity, since the trade was the surname's business and the surname was the trade.

What survives unchanged is the wilderness half. Forty days without food, and the three propositions that follow (4:1-11), are all offers of a shortcut: proof, safety, scale. Anyone who has been offered a shortcut while exhausted knows the terms have not been renegotiated since. The devil in this scene does not tempt with pleasure. He tempts with efficiency.
"@

Add-ThenAndNow "019FA064-CBA6-7FF6-828B-72094212CB22" @"
Read chapter five as an event rather than a text and the first thing you notice is the physics. A man is talking outdoors to a crowd on a slope, unamplified, and the words have to be short enough to survive the trip. The Beatitudes are built accordingly: repeated opening formula, hard stresses, no subordinate clauses to lose. That is not a literary preference. It is what oral transmission does to a sentence when the only recording device in the field is the crowd's memory.

We have solved the volume problem entirely and inherited a new one. Everything can now be heard by everyone, which means nothing has to be built to be remembered. The sayings that still travel intact through our own noise $em the slogan, the four-word chant, the phrase you cannot unhear $em obey exactly the compression rules a hillside imposed for free.

The content, meanwhile, has lost none of its capacity to irritate. The sermon's move is not to relax the law but to tighten it: not only no murder but no contempt, not only no adultery but no appetite. Every generation since has approached that passage hoping it is hyperbole, and the passage has declined every time to say so.
"@

Add-ThenAndNow "019FA065-8069-7E01-98D2-686871E63831" @"
Chapter six is about an audience problem, and the audience problem has only grown teeth. Give without announcing it, pray without performing it, fast without advertising it (6:1-18): every one of the three assumes a world where charity, prayer, and abstention were public acts with public returns, and where the return was the point for a great many people. The instruction is not "be modest." It is that a good deed already paid out in reputation has been paid, and should not expect to be paid twice.

We did not abolish this economy. We built infrastructure for it. The named wing, the pledge announced at the fundraiser and the same amount announced again online, the fast documented daily, the prayer request that functions as a status report. The transaction is identical; the settlement is faster and the ledger is searchable.

What has genuinely changed is the possibility of the alternative. In a village, an anonymous gift was nearly impossible $em everyone knew who had grain. Total anonymity is now trivially available to anyone who wants it, which quietly turns the whole chapter into a live test rather than an ideal. The left hand can genuinely be kept ignorant of the right hand now. Mostly we decline the option.
"@

Add-ThenAndNow "019FA066-1F9A-7E33-8627-91085971065D" @"
The sermon ends in a builder's yard. Two men put up what is presumably the same house; one founds it on rock and one on sand, and the difference is invisible in fair weather and total in a flood (7:24-27). This is not a metaphor a Galilean audience had to be walked through. In a country of seasonal torrents and wadis that run dry for most of the year, the difference between a dry-season site and a flood channel was a routine, expensive, occasionally fatal judgment call, and everybody in earshot knew someone who had got it wrong.

The image survives because we have kept the exact problem and only changed its name. Building codes, foundation surveys, flood maps, the insurance premium priced off a zone boundary $em an entire industry now exists to answer the question this parable poses, and it still gets answered wrong, usually for the same reason: the sandy plot was cheaper and the weather was fine.

The unchanged part is the timing. Nothing about either house is knowable until the rain comes. We have added instrumentation, so the rain is now forecast rather than a surprise. It has not made anybody noticeably more inclined to move.
"@

Add-ThenAndNow "019FA066-CCC6-746B-A184-E781F3461446" @"
The centurion's argument in chapter eight is not about faith in the abstract. It is a technical claim about how authority propagates. He says he is a man under authority with soldiers under him, that he says go and they go, and that he therefore does not require the physical presence of the person issuing the order (8:8-9). He is reasoning from the org chart. He has spent his career watching instructions travel down a hierarchy and arrive intact without their author, and he assumes this works the same way.

We live inside that logic now to a degree he would find alarming. Almost nothing we obey arrives in person. The remote approval, the delegated signature, the automated instruction executing at three in the morning with no human awake anywhere in the chain $em the centurion's insight has become the ordinary plumbing of everything.

What has changed is who is impressed by it. His confidence reads, in the text, as remarkable; ours reads as bored. And there is a smaller reversal worth noticing: the man everybody in the story would have expected to be an outsider is the one who understands the situation fastest, purely because his day job had taught him the mechanism. Competence has always been an odd door into insight.
"@

Add-ThenAndNow "019FA067-8522-77F9-898C-52F3ACA42AD1" @"
The scandal in chapter nine is a seating chart. Jesus eats at the house of a man who collects taxes, in company described as tax collectors and sinners, and the objection raised is not about doctrine but about the meal (9:10-11). In a world where a shared table was a public statement of who you would be associated with, dining was a formal act with consequences. Sitting down with a tax farmer implied you had no objection to a tax farmer. Everyone at the table understood that; so did everyone watching from outside it.

Guilt by association did not die out when the tables got bigger. It migrated. A photograph, a shared platform, a donation list, an appearance on the wrong programme $em the mechanism is unchanged, and the modern version is worse in one specific respect: the ancient table had walls, and the objection came from people who were physically present and could be answered. Ours can be raised by anyone, permanently, from anywhere, about a lunch they were not at.

The defence offered in the chapter is still the only one available, and it is not a denial. It is a change of subject: physicians go where the illness is. Nobody has ever found that answer satisfying, which is a fair sign it is the right one.
"@

Add-ThenAndNow "019FA068-3D3A-7CE3-BD9C-F0463448908A" @"
The commissioning instructions in chapter ten are a logistics document. No gold or silver or copper, no bag, no second tunic, no sandals, no staff (10:9-10) $em and the mission is nonetheless expected to function, because it is built on a real institution. Hospitality to a travelling stranger was not a kindness in this world; it was closer to an obligation with rules, and a village that broke it lost standing. The Twelve are being sent out uninsured on purpose, into a system that was expected to catch them.

That system is the thing we have most thoroughly dismantled. We replaced obligatory hospitality with a priced hospitality industry, which is more reliable, vastly more comfortable, and completely non-obligatory. A stranger arriving in a town today will be housed if they can pay and will otherwise be a municipal problem. Nobody's reputation is at stake either way.

What has not changed is the calculation the instructions rest on. Arriving with nothing forces a relationship; arriving self-sufficient permits a transaction. Every traveller who has been fed by someone poorer than themselves knows the debt that creates, and knows it is not the same experience as a receipt. The Twelve were sent out to incur that debt deliberately.
"@

Add-ThenAndNow "019FA068-DA02-71F2-AB5E-E84E36383284" @"
Chapter eleven pronounces judgment on three named towns $em Chorazin, Bethsaida, Capernaum $em and the towns are the most checkable thing in it, because you can go and stand in all three. What you find is not uniform. One is a well-excavated ruin with a black basalt synagogue; one is a site archaeologists still argue about, with rival identifications and a shoreline that has moved; one is a national park. All three are, in the ordinary sense of the word, gone.

There is a temptation to read that as the judgment landing, and it is worth resisting: towns in this region were abandoned for earthquakes, silted harbours, shifting trade routes, and imperial reorganisation far more often than for anything a preacher said about them. The honest observation is smaller and stranger. These places were unremarkable enough that their names survive mainly because they appear in a list of towns that disappointed someone.

That is the part that has not changed at all. Being forgotten remains the ordinary outcome for a settlement, and the exceptions are almost always accidental. A place now persists because a road bypassed it or a poet was born there. Chorazin persists because it was scolded.
"@

Add-ThenAndNow "019FA069-844F-7A56-A07C-D5037832F038" @"
The dispute that opens chapter twelve is a labour-law argument. Disciples pick grain as they walk and eat it; Pharisees object; the objection is not petty, because the prohibition being invoked has real machinery behind it, with defined categories of work and genuine debate about their edges (12:1-8). This is a legal system trying to answer an unavoidable question: what counts as working, when the whole point of the rule is that for one day in seven nobody has to.

We have run the identical experiment at national scale and arrived at the identical difficulty. Every jurisdiction that legislates rest ends up litigating definitions $em what counts as being on duty, whether checking a message is work, whether being reachable is being employed. The categories multiply for exactly the reason they multiplied then: a rule protecting rest only works if it can say precisely what rest excludes, and nothing about human labour divides cleanly.

The lasting part is the case made against the rule's edge, which is not that the law is bad but that hunger outranks it. Rest was invented for people, not people for rest. Every argument since about whether the exhausted may be excused a requirement is a rerun, and the requirement usually wins.
"@

Add-ThenAndNow "019FA06B-7580-76BD-93D9-2ADDCEE9AF4C" @"
Seven parables in one sitting, and not one of them explains itself. That is the technique, and the chapter is unusually candid about it: asked privately why he teaches this way at all, the answer offered is not that stories are more accessible (13:10-17). Stories, on this account, sort the audience. Whoever will chase the meaning gets it; whoever wants a summary leaves with a farming anecdote.

Every teacher since has felt the pull of both halves of this. The story that will not resolve is by far the most durable delivery mechanism ever found for an idea $em it survives retelling, it travels between languages, it lodges in people who were not paying attention $em and it is also completely uncontrollable. A parable cannot stop itself being misread, and this one has been. The seed and the soils have been enlisted for so many incompatible arguments over so many centuries that the interpretive fight is now older than most of the languages it is conducted in.

We tell ourselves the modern preference is for clarity: the executive summary, the explainer, the bullet point. Then we watch what actually spreads $em the anecdote, the parable, the story with the ending withheld $em and quietly go on doing what worked on that hillside.
"@

Add-ThenAndNow "019FA06C-2D81-7D7E-87EE-BC3BA620B663" @"
A man dies in this chapter because of a promise made at a party. The birthday banquet, the dance, the oath given in front of guests, the request relayed and honoured (14:6-11): what kills John the Baptist is not policy or a trial or even settled malice. It is a ruler who has said something in public and cannot be seen to take it back.

That mechanism is the least dated thing in the Gospels. Power that has committed itself in front of an audience will pay almost any price rather than be seen reversing $em and the more private the room, the smaller the number of people whose opinion has to be managed, and the faster a life becomes the cheapest available way to manage it. The independent historical record supplies the same king with a colder and more plausible motive: a preacher drawing crowds is a political problem before he is a moral one. Both accounts can be true. Rulers generally have a reason and a pretext, and the pretext is usually the one that gets told at dinner.

The second half of the chapter feeds a crowd on a hillside. It is worth noticing which of the two events required organisation and which required only a room, a table, and nobody willing to say stop.
"@

Add-ThenAndNow "019FA06C-F68B-7BC8-96BA-0F00A6216BD7" @"
The handwashing dispute that opens chapter fifteen deserves a specific kind of care, because the modern reader arrives holding a piece of information nobody in the scene has. We wash our hands because of germ theory. They washed because of ritual purity, a category with nothing to do with contagion, and the argument in the chapter is not about hygiene at all $em it is about whether a practice with no basis in the written law can be binding.

So the ancients were, by our lights, accidentally right, for reasons that were entirely wrong, about a practice we now enforce in hospitals under pain of dismissal. It is a useful humbling. Sound advice frequently survives inside a bad explanation, and dismissing the practice because you have refuted the reasoning is how each generation discards something it later reinstalls at considerable expense.

The chapter's other half has aged less comfortably. A foreign woman asks for help, is initially refused in terms that make most readers wince, argues back, and prevails by out-arguing the refusal (15:21-28). She does not win by being pitiable. She wins on the merits, in public, against a boundary drawn to exclude her. That remains, by a wide margin, the most difficult and most contemporary scene in the chapter.
"@

Add-ThenAndNow "019FA06D-89CE-7DFF-871E-E5AACFEA94DA" @"
An institution starts here, on a wordplay, in a foreign language. You are Petros, and on this petra I will build (16:18) $em a pun that works cleanly in Greek and is doing something rather different in Aramaic, which is one reason the sentence has been fought over for most of two millennia. Whatever else it is, it is the moment a nickname becomes an office.

That is a more ordinary origin story than the scale of what followed makes it look. Institutions with cathedrals, jurisdictions, and legal codes routinely begin as a phrase said to one person, in a specific place, about a specific afternoon. What separates the phrase that becomes an institution from the thousands that evaporate is almost never the phrase. It is whether anyone builds the filing system.

The genuinely contemporary detail is what happens six verses later, when the same man who has just been congratulated for insight is told, in the harshest terms in the Gospel, to get behind (16:23). Anyone who has watched an organisation elevate someone and then discover the elevation did not come with omniscience will recognise the sequence exactly. We tend to imagine that authority, once conferred, settles the question of who is right. This chapter takes eleven verses to demonstrate otherwise.
"@

Add-ThenAndNow "019FA06E-4B5A-7AAB-80B3-3EF0B408119C" @"
The chapter closes on a tax question, and the tax is worth seeing as an administrative object rather than a plot device. An annual levy collected from adult males, denominated in a specific coin, funding the upkeep of a single institution everybody was assumed to have a stake in $em collected across a diaspora, in provinces with different currencies, requiring money-changing and record-keeping and a season in which it fell due.

Anyone who has filed a return recognises the whole apparatus, including the specific irritation the conversation captures: the question is not whether the institution deserves support but whether this particular payer falls inside the category. That is the entire modern tax dispute, in miniature, twenty centuries early. The categories, the exemptions, the argument from status about why the rule was written for somebody else.

The resolution $em pay it anyway, to avoid giving offence $em is the most quietly recognisable sentence in the chapter. Not that the levy is just, not that the objection is wrong, but that this particular hill is not worth dying on. Every taxpayer who has ever concluded that fighting a small assessment would cost more than paying it has arrived at the same place by the same road, minus the fish.
"@

Add-ThenAndNow "019FA06E-E541-76D7-8867-57D1241C3DDC" @"
Chapter eighteen ends on a debt so large it is a joke, and the joke depends on arithmetic. A servant owes a fortune measured in the largest unit of account in circulation, is forgiven it, and then jails a colleague over a sum that would fit in a pocket (18:23-35). The scale is not a rounding error in the storytelling; it is the whole point, and it lands only if the audience has a feel for both numbers. They did, the way we feel the difference between a mortgage and a coffee.

What we have added is machinery for exactly this problem. Bankruptcy, statutory limitation, debt forgiveness programmes, the whole legal apparatus that exists because societies discovered that unpayable debt destroys not just the debtor but the creditor's own market. The parable's premise $em that a debt beyond any possibility of repayment is a problem requiring cancellation rather than enforcement $em is now, in many jurisdictions, black-letter law.

The unchanged part is the second half. Institutional forgiveness at scale has never once produced personal forgiveness at small scale. People whose own crushing obligations have been written off still pursue the twenty owed to them, with real energy, and would be genuinely baffled to be told these were the same transaction.
"@

Add-ThenAndNow "019FA06F-661F-7830-9AD8-BF91C0C5F560" @"
The divorce question in chapter nineteen is posed in the technical language of a live legal dispute $em whether a man may divorce for any cause $em and it matters that the question is asked about a man. In the framework being argued over, the initiative was his. The stakes for a woman turned out of a household were not primarily emotional; they were about food, shelter, and whether anyone was obliged to house her.

That is the substantive change, and it is nearly total. Divorce is now, in most of the world, symmetrically available, adjudicated by courts rather than by the husband, and accompanied by property and maintenance rules designed to keep the ending of a marriage from becoming a destitution event. The argument the Pharisees are having has been settled in a way neither side would recognise.

Then the chapter turns to a rich man and a camel, and here the modern reader usually arrives holding a piece of folklore: that a gate in Jerusalem was called the Needle. There was no such gate. The line is an absurd image, deliberately, and every attempt to domesticate it into a manageable narrow-doorway metaphor is a two-thousand-year-old record of how badly people want it to mean something less inconvenient.
"@

Add-ThenAndNow "019FA070-1F63-7891-8ABA-40A617CF7273" @"
The vineyard parable opens in a public square where men are standing around waiting to be hired, and it keeps going back there $em at dawn, mid-morning, noon, mid-afternoon, and an hour before the end of the day (20:1-7). That is not a narrative device. It is a labour market. Casual workers gathered where employers looked for them, were engaged for a day, and were paid at day's end, and the ones still standing there in the afternoon were not lazy. They were unhired.

We rebuilt this market with a phone in the middle of it. The shift offered at short notice, the app that fills tomorrow's rota, the day rate agreed in the morning and paid the same night $em the structure is identical, including the part where availability is the worker's problem and demand is somebody else's. The square has better lighting and no fewer people waiting in it.

The parable's provocation is still where it always was: the landowner pays the last-hired the same as the first-hired, and the first-hired are furious. They are not underpaid. They received exactly what they agreed to. They are furious because someone else did better than they deserved, which is a species of grievance no wage negotiation in history has ever managed to price.
"@

Add-ThenAndNow "019FA070-F866-7A49-8157-5E6B429D1C37" @"
The money changers in chapter twenty-one were not an arbitrary target. They existed because the temple's dues had to be paid in one particular coinage while pilgrims arrived carrying whatever their own province used $em so a currency conversion was structurally necessary before anyone could pay anything, and where a conversion is necessary a spread can be charged. The tables Jesus overturns are the foreign exchange desk at the point where nobody has the option of walking away.

We have kept every part of that arrangement. The airport bureau positioned after passport control, the card fee applied at the moment of purchase, the mandatory conversion on a bill you must settle in a currency you do not hold: the business model is unchanged, and it is still built on the same insight, which is that captivity is worth more than volume. Nobody negotiates a rate while holding a queue ticket.

There is a wrinkle worth sitting with. This episode is one of the more historically defensible events in the Gospel, and the reason is unsentimental: it is exactly the kind of public disturbance that gets a man noticed by the authorities running a crowded festival city. Overturning a table in a commercial precinct is not primarily a theological act. It is a policing incident.
"@

Add-ThenAndNow "019FA071-9611-73CB-A9C2-8DD1ADEAD70C" @"
Bring me a coin, he says, and look at whose head is on it (22:19-21). The trap being set was real and the escape works because money in this world was a portable political statement. The specific denarius most likely in that hand carried the emperor's portrait and an inscription making claims about his parentage that a devout Jewish reader would have found not merely objectionable but blasphemous $em which means the object produced as evidence was already, before anyone said anything, the argument.

Our money has gone quiet. Notes still carry faces, but the faces are mostly historical and the claims are modest, and in any case a growing share of what we spend has no face at all: a balance, a tap, a number. It is easy to read that as neutrality. It is closer to a change of medium. The claims moved to the terms of service.

What has not changed is the trick the question was designed to spring. Answer one way and you are a collaborator; answer the other and you are a rebel; the questioner wins either way and has no interest in the subject. Every interview conducted in bad faith since has used the identical two-door structure, and the only known escape is still to make the questioner produce the evidence.
"@

Add-ThenAndNow "019FA072-2582-769C-A2B5-85E052E09347" @"
Two of the accusations in this chapter name physical objects: phylacteries made broad and fringes made long (23:5). These were real, ordinary, commanded items of observance, and the charge is not that wearing them is wrong. It is about sizing $em that the devotional accessory has been scaled up until its function is to be seen from across a courtyard.

This is a criticism with no shelf life. The mechanism reappears wherever an object certifies commitment: the visible badge slightly larger than necessary, the ostentatiously worn symbol, the bio that lists the observance. Nothing about the item is false. The item is doing its job. It has simply also been recruited into a second job, which is advertising, and the second job tends to eat the first.

The other image in the chapter has aged into something almost too apt. Whitewashed tombs, handsome outside and full of bones (23:27), describes a genuine practice with a practical purpose $em marking burial sites so that people could avoid contracting ritual impurity by walking over one unawares. The whitewash was a warning label. Which makes the insult sharper than it first reads: the complaint is not that the surface is beautiful, but that a system built to warn people has been repurposed to reassure them.
"@

Add-ThenAndNow "019FA073-292F-7D88-973F-2FB76C93F677" @"
Not one stone left upon another (24:2) reads as apocalyptic rhetoric until you consult the eyewitness on the other side. A historian who was physically present in the Roman camp during the siege describes the outcome in terms just as total, and offers a reason for it that is entirely without mystique: the site was systematically dismantled, and buildings are quarried by their conquerors because dressed stone is valuable and a levelled city is easier to hold.

That is the sober centre of this chapter. The destruction of a great building is rarely a single dramatic event. It is a demolition, conducted over time, for reasons of logistics and control, by people carrying tools rather than torches. Anything left standing was left standing because it was useful to somebody.

What has changed is that we now photograph it. A modern reader has seen levelled cities $em in footage, from the air, in before-and-after frames $em and has therefore lost a specific kind of innocence this text still assumes it must overcome. Its first audience needed to be persuaded that a building that large could simply stop existing. We need no persuading whatsoever. We have simply substituted a different disbelief, which is that it could happen to ours.
"@

Add-ThenAndNow "019FA073-7BBC-79E9-B2A8-F4F23309C2A3" @"
One word in this chapter escaped into English and changed jobs on the way. A talent, in the middle parable, is a unit of weight and money $em a large one. The story of a master entrusting sums to three servants and auditing them later (25:14-30) was so widely retold that the coin's name slid across into the meaning we now use exclusively: an aptitude, a gift, something you are born with rather than handed. English speakers now say a person has talents without any sense that they are quoting an accounting term.

The migration is a small monument to how the parable was received. A story about capital under management became the standard vocabulary for personal ability, which is a fairly complete summary of what several centuries of readers decided it was about.

The mechanism, meanwhile, is untouched. The rebuke to the third servant assumes an audience who knew perfectly well that money could have been placed with bankers to earn a return $em that leaving capital idle was itself a decision with a cost. Every discussion of opportunity cost since has restated it. And the sorting scene at the end of the chapter still declines, pointedly, to ask anyone what they believed. It asks what they did about the hungry stranger.
"@

Add-ThenAndNow "019FA074-4A7C-7119-AF4B-1F2DB39E5FC8" @"
The proceedings in chapter twenty-six happen at night, at speed, and the Gospel's own account of them sits awkwardly against the procedural rules later recorded for capital cases $em rules about when a trial may be held, how testimony must agree, and how long a verdict must wait. Whether those rules were in force in this exact form at this exact date is genuinely disputed. What is not disputed is that somebody thought it mattered enough to write them down.

That is the durable observation. Procedure is a technology for slowing power down, and it is always at its weakest at night, at pace, in a small room, with a decision that everyone present has already reached. The safeguards that fail here $em daylight, delay, corroboration, an independent witness $em are precisely the safeguards every legal system since has kept reinventing, and precisely the ones that get suspended first when an outcome is wanted quickly.

The betrayal is priced, too. A named sum, agreed in advance with the people who wanted the arrest (26:14-16). It is a small, deflating detail, and it is the one that most reliably survives translation: this was not managed by an ideology. It was managed by a payment, arranged in a corridor.
"@

Add-ThenAndNow "019FA076-F16B-74ED-8384-66A7A6DE8052" @"
Crucifixion was not a punishment in the sense we use the word. It was a public communication, aimed at the living: conducted beside a road, at eye level, over hours, with a written charge fixed above the condemned so that passers-by could read the offence (27:37). The written notice is the tell. Somebody has taken the trouble to caption it, because the death is a message and a message requires a legible subject.

The state has not stopped executing people. What it has largely stopped doing is inviting an audience. Modern executions happen indoors, on a schedule, with limited witnesses and a procedure designed to look clinical, and that shift is a genuine change in what the act is for. A punishment carried out privately is being aimed at the person; a punishment carried out on a roadside is being aimed at everyone who walks past.

Two more things in this chapter are worth naming plainly. A bystander named in the text is pressed into carrying the crossbeam $em conscripted labour, the ordinary prerogative of occupying soldiers. And the governor performs a hand-washing before the crowd, which is either a striking gesture or an implausible one, but functions in either case as the oldest recorded attempt to be present at a decision without owning it.
"@

Add-ThenAndNow "019FA078-392B-7408-B4A9-4CA5E15931F7" @"
The final chapter contains the only account in the Gospels of what the other side was saying, and it is a rumour-management story. Guards are posted, a report is brought, money changes hands, and an explanation is agreed and circulated: the body was stolen while we slept (28:11-15). Only this Gospel preserves it, and it preserves it in order to rebut it $em which means the counter-story was current enough that ignoring it was not an option.

That is a recognisably modern shape. A denial that names the rival account keeps the rival account alive; the rebuttal is the vehicle by which we know the rumour existed at all. Everyone who has watched an institution respond to an allegation, and thereby introduce it to people who had not heard it, has seen this dynamic run.

The chapter's other feature is who found the tomb. Women $em in a period when their formal testimony carried, in the surviving sources, reduced weight in legal settings. That has been argued about at length as evidence, and the argument cuts more than one way. But the plain narrative fact stands without needing the argument: the account was built on witnesses the surrounding culture was least inclined to credit, and it did not quietly swap them out for better ones.
"@

$conn.Close()
Write-Host "MATTHEW DONE"
