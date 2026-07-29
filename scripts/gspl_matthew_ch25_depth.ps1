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
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch25NodeId = [guid]"019FA073-7BBC-79E9-B2A8-F4F23309C2A3"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA073-7BBC-79E9-B2A8-F4F23309C2A3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'ten-virgins-delayed-groom' = @{ title='A delay built into the custom, not just the plot'; body="Klyne R. Snodgrass, Stories with Intent: A Comprehensive Guide to the Parables of Jesus (Grand Rapids, MI: Eerdmans, 2008), commentary on Matthew 25:1-13. Snodgrass treats the bridegroom's delay as structurally essential to the parable rather than an arbitrary complication - in his own words, without the delay there is no parable - because delay is exactly what separates the five attendants who planned for a long wait from the five who didn't. A groom's actual arrival time in first-century village practice was genuinely not fixed in advance and could run past midnight, which is what makes the ten virgins' wait a recognizable social situation rather than a scenario invented solely to argue for vigilance." }
'bailey-wedding-procession-route' = @{ title='Why the whole village had reason to expect a wait'; body="Kenneth E. Bailey, Poet and Peasant and Through Peasant Eyes: A Literary-Cultural Approach to the Parables in Luke, combined ed. (Grand Rapids: Eerdmans, 1983/2008). Bailey, drawing on decades of firsthand observation of Middle Eastern village wedding custom, describes the groom's party deliberately taking the longest and most visible route back from the bride's house rather than the most direct one, so that as much of the village as possible could see the procession pass. A custom built around maximizing display, not minimizing travel time, produces exactly the kind of unpredictable, potentially late-night arrival hour this parable assumes. Bailey's fieldwork documents a broadly attested regional practice rather than a first-century Judean record specifically, a caveat worth stating as plainly as the point itself." }
'trapezitai-ancient-bankers' = @{ title='The bankers were real: trapezitai and interest-bearing deposits'; body="Klyne R. Snodgrass, Stories with Intent: A Comprehensive Guide to the Parables of Jesus (Grand Rapids, MI: Eerdmans, 2008), commentary on Matthew 25:14-30; W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 25:27. The master's rebuke - you ought to have invested my money with the bankers, and at my coming I should have received what was my own with interest (25:27) - names a real, attested first-century financial practice, not an anachronism. Greek trapezitai, table-men, from trapeza, table, were professional money-changers and depositaries operating across the Greco-Roman world from at least the fourth century BCE onward, and they genuinely took deposits and paid interest on them as an ordinary commercial service. The parable's economic logic assumes an audience that already knew this was an available, unremarkable option." }
'bava-metzia-burying-money' = @{ title='Burying it was the legally cautious choice, not an eccentric one'; body="Babylonian Talmud, Bava Metzia 42a (Soncino edition translation); compare the general bailee-liability categories at Mishnah Bava Metzia 3:1-3. The ruling recorded there, attributed to the Amora Shmuel, holds in its standard rendering that there is no safekeeping for money except in the ground - meaning that burying an entrusted deposit, specifically, was treated in rabbinic legal tradition as the one method of custody that reliably absolved a bailee of liability if the money was later lost or stolen. Read against that legal background, the third servant's excuse in the parable - I was afraid, and I went and hid your talent in the ground (25:25) - describes conduct ancient Jewish audiences would have recognized as the maximally cautious, legally defensible option available to him, which sharpens rather than softens the master's judgment: he is condemned not for failing to find a clever investment, but for treating bare safety as sufficient when more was expected of him." }
'talents-vs-minas-synoptic-variant' = @{ title="Matthew's talents, Luke's minas: the same core story, a different scale"; body="Klyne R. Snodgrass, Stories with Intent: A Comprehensive Guide to the Parables of Jesus (Grand Rapids, MI: Eerdmans, 2008), comparative discussion of Matthew 25:14-30 and Luke 19:11-27. Luke's Gospel preserves a version of the same core story under a different scale and setting: ten servants, not three, are each given one mina, a much smaller sum than a talent, and the parable there is folded into a companion story about a nobleman seeking a kingship his own citizens reject. Snodgrass and the wider synoptic-comparison literature generally read the two versions as independent developments of a shared core tradition rather than one Gospel simply copying and relabeling the other's numbers, since the differences in scale, servant count, and surrounding narrative frame run too deep and too consistently to read as ordinary copying variation." }
'sheep-goats-day-night-herding' = @{ title='Mixed herds by day, separated by night'; body="Joachim Jeremias, The Parables of Jesus, rev. ed., trans. S. H. Hooke (London: SCM Press, 1963), translated from the German 6th ed., Die Gleichnisse Jesu (Gottingen: Vandenhoeck & Ruprecht, 1962), discussion of the parable's Palestinian pastoral setting. Jeremias's classic form-critical treatment notes that sheep and goats were commonly herded together by day across the region's shared pastureland but separated at night, since goats, lacking sheep's heavier wool coat, needed additional shelter or warmth that a single mixed fold didn't supply. The underlying husbandry logic, goats' comparatively lower cold tolerance, is not a claim needing first-century-specific corroboration on its own; it is a basic, still-observable feature of small-ruminant herding generally. Together the two points make the judgment image of a shepherd separating the sheep from the goats a recognizable, everyday pastoral action turned into an end-time picture, not an invented image with no working referent." }
'ezekiel-34-shepherd-goat-crossref' = @{ title="The sheep-and-goats distinction is already in Ezekiel's own judgment scene"; body="Ezekiel 34:17, in the same shepherd-king chapter already examined at length in this project's discussion of the Good Shepherd material in the Gospel of John: as for you, my flock, thus says the Lord GOD: I shall judge between one sheep and another, between the rams and the male goats. The sheep/goat pairing this chapter's judgment scene turns into its central image is not a detail original to the parable; Ezekiel's own vision of God as shepherd-judge already singles out rams and goats specifically, ahead of the flock's other sheep, as animals needing separated judgment. This chapter draws on the wider Ezekiel 34 shepherd-king and divine-judgment tradition already established elsewhere in this project; the point specific to this passage is that the sheep/goat distinction itself, not only the shepherd-judge role, has a scriptural precedent." }
'least-of-these-brothers-debate' = @{ title='Whose brothers? A genuinely open, long-argued question'; body="Sherman W. Gray, The Least of My Brothers: Matthew 25:31-46, A History of Interpretation, SBL Dissertation Series 114 (Atlanta: Scholars Press, 1989); W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 25:31-46. Gray's dissertation surveys more than six hundred works of interpretation, from the second century through the twentieth, on exactly this question: does the least of these my brothers (25:40, 45) mean all of suffering humanity, regardless of any tie to Jesus, or does it mean specifically Jesus's own missionary followers, his brothers in this Gospel's own narrower, repeated sense of that word? Gray's own conclusion leans toward the narrower reading as more likely original to Matthew, and Davies and Allison's commentary treats the split as a live, seriously argued crux rather than a settled matter. This book takes no position the text itself doesn't settle: both readings remain actively defended in current scholarship." }
'matthew-brothers-usage-elsewhere' = @{ title="Matthew's own narrower sense of brothers"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 25:40, cross-referencing Matthew 12:48-50 and 28:10. The case for the narrower, missionary-disciples reading of Matthew 25:40 rests partly on how this Gospel uses brothers, adelphoi, elsewhere: at 12:48-50 Jesus redefines the term away from blood kinship toward whoever does the will of my Father in heaven, and at 28:10 the risen Jesus instructs the women at the tomb to tell his brothers to go to Galilee, plainly meaning the eleven remaining disciples, not humanity generally. Read against that pattern, and against this project's earlier discussion of how itinerant missionaries in Matthew's own mission charge depended entirely on the hospitality of those who received them, a narrower reading has real textual footing, though, as the previous note states, it does not settle the question outright." }
'outer-darkness-weeping-gnashing-redaction' = @{ title="A phrase that is Matthew's own, repeated six times"; body="Zoltan L. Erdey and Kevin G. Smith, The Function of Weeping and Gnashing of Teeth in Matthew's Gospel, Acta Theologica 32.1 (2012): 26-45. Weeping and gnashing of teeth appears six times across this Gospel (8:12; 13:42; 13:50; 22:13; 24:51; 25:30), always describing final judgment, and Erdey and Smith's study identifies the phrase as one of Matthew's own distinctive, repeated pieces of vocabulary: the first occurrence, at 8:12, has a parallel at Luke 13:28 and likely derives from a shared earlier source, but the remaining five occurrences have no parallel in Mark, Luke, or John at all, making them look like Matthew's own recurring redactional formula rather than a phrase inherited from tradition each time. Outer darkness specifically, as opposed to the fuller weeping-and-gnashing phrase, appears just three times, and always paired with it in the same verse - 8:12, 22:13, and this chapter's own 25:30 - a narrower, even more tightly Matthean sub-pattern within the larger one." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
These three scenes continue directly from the Olivet discourse examined in the previous chapter, still addressed to the same private audience of disciples on the Mount of Olives (24:3), and all three take the form this book has already discussed at length: the mashal, a short, realistic narrative built to make a point, a form Jesus inherited rather than invented [217]. The interpretive debate already laid out in earlier chapters — whether a parable like this one carries one clean point, as Jülicher's rule and, in a more historically grounded form, Jeremias's method both argue, or was always meant to carry several points of contact with reality at once, the case more recent scholars have pushed back with — applies to all three parables here exactly as it did to the sower [220] [221]. That question stays genuinely open across this chapter; nothing about the ten virgins, the talents, or the sheep and goats settles it definitively one way or the other.

The first parable's plot leans entirely on one fact: nobody in the wedding party, foolish or wise, knows when the bridegroom will actually arrive. That uncertainty is not a device invented to make a point about vigilance — it describes how first-century village weddings in the region actually worked. Klyne Snodgrass's standard modern treatment of the parable insists on this directly: the bridegroom's delay is not incidental complication but the parable's whole engine — "without the delay there is no parable," in his own phrasing — because delay is exactly what separates the five who planned for a long wait from the five who didn't [[NOTE:ten-virgins-delayed-groom]]. Kenneth Bailey's decades of fieldwork on Middle Eastern village wedding custom supply the social mechanism behind that unpredictability: a groom's party did not take the most direct route back from the bride's house to begin the procession, but deliberately the longest and most visible one, so that as much of the village as possible could see it pass — a custom built around maximizing display rather than minimizing travel time, which is exactly what turns "the bridegroom is coming" into an announcement that could arrive at any hour, sometimes well past midnight (25:6) [[NOTE:bailey-wedding-procession-route]]. Bailey's fieldwork describes a broadly attested regional practice, not a first-century Judean census record; the caveat is worth stating as plainly as the point itself.

Read against that background, the five foolish virgins' failure isn't carelessness in the abstract — it's a specific, locally recognizable failure to plan for a known regional possibility (real delay) rather than an assumed certainty (a fixed, predictable hour). The bridegroom's blunt refusal, "I do not know you" (25:12), previews the same judgment-vocabulary pattern this chapter's closing parable makes explicit with its "outer darkness" language [[NOTE:outer-darkness-weeping-gnashing-redaction]] — a chapter that returns to the same judgment phrasing three times over, not three unrelated endings stitched together. The parable's own closing line, "Watch therefore, for you know neither the day nor the hour" (25:13), applies a real social fact about weddings to an unrelated question about the end of the age; it is not a claim that first-century weddings themselves were somehow unknowable in every particular.
'@

$beat2 = @'
The master's rebuke to the third servant assumes an audience that already knew a specific commercial option existed: "you ought to have invested my money with the bankers, and at my coming I should have received what was my own with interest" (25:27). That is not a rhetorical stand-in for "you should have tried harder" — trapezitai, literally "table-men" (from trapeza, "table"), were real professional bankers and money-changers operating across the Greco-Roman world from at least the fourth century BCE onward, taking deposits and paying interest on them as an ordinary, unremarkable financial service [[NOTE:trapezitai-ancient-bankers]]. The parable's logic depends on that being common knowledge to its first hearers: burying the money was never the servant's only alternative to investing it, and the master's anger targets a choice between two known options, not ignorance of finance itself.

That makes the third servant's own defense worth taking seriously on its own terms rather than dismissing it as mere excuse-making. "I was afraid, and I went and hid your talent in the ground" (25:25) describes conduct that rabbinic legal tradition treated as the maximally cautious, liability-clearing option available to anyone holding someone else's money: the Babylonian Talmud records the ruling that there is no safekeeping for money except in the ground, meaning burial specifically was the one recognized method that reliably absolved a custodian if the deposit was later lost or stolen [[NOTE:bava-metzia-burying-money]]. Set beside that legal backdrop, the parable's condemnation reads sharper, not softer: the servant is not damned for choosing a legally reckless method of custody — burial was, if anything, the safe move — he is damned for treating bare safety as sufficient when active use of what he had been given was what his master actually expected.

Luke's Gospel preserves a version of this same core story under a different name and scale, worth noting briefly rather than treating as an untouched duplicate: ten servants, not three, are each given one mina (a much smaller sum than a talent), and the parable there is folded into a companion story about a nobleman seeking a kingship his own citizens reject (Luke 19:11-27). The two versions are generally read as independent developments of a shared core tradition rather than one Gospel simply copying and relabeling the other's numbers, since the differences in scale, servant count, and surrounding narrative frame run too deep and too consistently to read as ordinary copying variation [[NOTE:talents-vs-minas-synoptic-variant]].
'@

$beat3 = @'
"All the nations will be gathered before him, and he will separate people one from another as a shepherd separates the sheep from the goats" (25:32). The image is drawn from an actual, everyday feature of Palestinian herding, not an invented metaphor built solely to stage a two-sided judgment scene: sheep and goats were commonly grazed together by day across the region's shared pastureland, then separated at night, because goats — lacking sheep's heavier wool coat — needed additional shelter or warmth that a single mixed fold didn't supply [[NOTE:sheep-goats-day-night-herding]]. A shepherd sorting his own mixed flock at day's end was an entirely ordinary sight; what the parable does is take that ordinary sorting action and scale it up to cosmic size.

The sheep/goat pairing itself, not just the shepherd-judge role in general, also has a specific scriptural precedent worth naming directly rather than re-deriving from nothing, since this project has already examined the wider Ezekiel 34 shepherd-king and divine-judgment tradition at length in its discussion of the Good Shepherd material in the Gospel of John. Ezekiel's own vision of God as shepherd includes this precise pairing: "As for you, my flock, thus says the Lord GOD: I shall judge between one sheep and another, between the rams and the male goats" (Ezekiel 34:17) [[NOTE:ezekiel-34-shepherd-goat-crossref]]. Matthew's judgment scene doesn't need to invent the idea that a shepherd-judge singles out goats for separate treatment; Ezekiel had already put that specific image on the table centuries earlier.
'@

$beat4 = @'
The judgment scene's actual criterion — feeding the hungry, giving drink to the thirsty, welcoming the stranger, clothing the naked, visiting the sick and imprisoned, done or not done "to one of the least of these my brothers" (25:40, 45) — has produced one of the longest-running interpretive disputes attached to any single verse in this Gospel, worth presenting here as the genuinely open question it remains rather than settled one way. Sherman Gray's book-length study of the passage's interpretive history surveys more than six hundred readings of it across nearly two thousand years and finds two live options still standing at the end: a broad, universalist reading, in which "the least of these my brothers" means all of suffering humanity regardless of any connection to Jesus, against a narrower reading in which it means specifically Jesus's own missionary disciples — his "brothers" in this Gospel's own repeated, particular sense of the word [[NOTE:least-of-these-brothers-debate]]. Gray's own conclusion leans toward the narrower reading as more likely original to Matthew; the standard critical commentary on the passage treats the split as a live, seriously argued crux rather than settled either way.

The case for the narrower reading rests on how this Gospel actually uses "brothers" elsewhere, not on this verse read in isolation. At 12:48-50, Jesus redefines the term away from blood kinship entirely — "whoever does the will of my Father in heaven is my brother" — and at 28:10, the risen Jesus instructs the women at the tomb to tell his "brothers" to go to Galilee, meaning, unambiguously in context, the eleven remaining disciples, not humanity in general [[NOTE:matthew-brothers-usage-elsewhere]]. That pattern connects directly to ground this project has already covered: the itinerant missionaries of Matthew's own mission charge depended entirely on the hospitality of whoever received them along the way [196], the same dependence-on-strangers situation the sheep in this judgment scene are credited with having met. Neither reading is disprovable from the text alone, and both remain actively defended today; what can be said honestly is that Matthew's own vocabulary gives the narrower reading more direct textual support than the popular, all-humanity reading of this verse usually assumes.
'@

$beat5 = @'
All three scenes in this chapter end on strikingly similar notes, and the middle one is worth isolating as a stylistic fact about this specific Gospel, independent of any question about what final judgment is or isn't. The foolish virgins are shut out with the bridegroom's "I do not know you" (25:12); the "wicked and slothful" servant is cast "into the outer darkness," where "there will be weeping and gnashing of teeth" (25:30); the accursed at the final judgment go away "into eternal punishment" (25:46). "Weeping and gnashing of teeth" appears six times across Matthew's Gospel (8:12; 13:42; 13:50; 22:13; 24:51; 25:30), always attached to a judgment scene, and a focused study of the phrase's distribution finds that only the first occurrence, at 8:12, has a parallel elsewhere (Luke 13:28), while the remaining five have no parallel in Mark, Luke, or John at all — exactly the signature of a writer's own recurring formula rather than a phrase inherited fresh from tradition each time it appears [[NOTE:outer-darkness-weeping-gnashing-redaction]]. "Outer darkness" specifically, the narrower phrase, appears only three times, always in the same verse as "weeping and gnashing" — 8:12, 22:13, and this chapter's own 25:30 — a tightly Matthean sub-pattern inside the larger one.

None of that is a claim about whether the judgment these three parables describe actually happens, or in what form; it is a checkable observation about this one Gospel's own vocabulary habits, the kind of fingerprint that shows up when a single author's hand, not a shared inherited source, is doing the describing across three otherwise unrelated scenes.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'TEN VIRGINS / BRIDESMAIDS (FIRST-CENTURY WEDDING CUSTOM)' = "The ten young women who wait for a bridegroom's arrival to begin a wedding procession (25:1-13), five prepared with extra oil for a long wait and five not. The scenario reflects genuine first-century village wedding practice: a groom's actual arrival time was not fixed in advance and could run past midnight, a real, unpredictable feature of the custom rather than an invented plot device built solely to argue for vigilance [[NOTE:ten-virgins-delayed-groom]] [[NOTE:bailey-wedding-procession-route]]."
'TALENT PARABLE (ANCIENT BANKING CONTEXT)' = "The parable of a master entrusting three servants with five, two, and one talent respectively before a journey (25:14-30; see TALENT (ANCIENT CURRENCY UNIT) for the sum itself). Its economic logic rests on two real, attested ancient practices: professional bankers (trapezitai) who took deposits and paid interest were a genuine, ordinary commercial option [[NOTE:trapezitai-ancient-bankers]], while burying an entrusted deposit was, under rabbinic legal tradition, the maximally cautious, liability-clearing method of safekeeping available to a custodian [[NOTE:bava-metzia-burying-money]]. Luke preserves an independent version of the same core story at a different scale, the parable of the ten minas (Luke 19:11-27) [[NOTE:talents-vs-minas-synoptic-variant]]."
'THE LEAST OF THESE (INTERPRETIVE DEBATE)' = "The phrase used twice in the sheep-and-goats judgment scene (25:40, 45) to describe who the righteous fed, clothed, and visited. Whether it means all of suffering humanity or specifically Jesus's own missionary disciples (his 'brothers' in this Gospel's narrower, repeated sense of the word) is a genuinely open question with a long interpretive history, not a settled one [[NOTE:least-of-these-brothers-debate]] [[NOTE:matthew-brothers-usage-elsewhere]]."
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
$sortKey = $maxChapterSortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch25NodeId $id $sortKey
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
Seed-Entity "Trapezitai (Ancient Bankers)" "trapezitai-ancient-bankers" "vocabulary" "Greco-Roman professional money-changers and depositaries (table-men, from trapeza, table) who took deposits and paid interest; named directly in the parable of the talents (Matthew 25:27)."

$conn.Close()
Write-Host "DONE Matthew Chapter 25 depth pass."
