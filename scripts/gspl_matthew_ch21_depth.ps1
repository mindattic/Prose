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
$Ch21NodeId = [guid]"019FA070-F866-7A49-8157-5E6B429D1C37"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA070-F866-7A49-8157-5E6B429D1C37' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'zechariah-99-poetic-parallelism-kugel' = @{ title='Restating one line twice: Hebrew synonymous parallelism'; body="James L. Kugel, The Idea of Biblical Poetry: Parallelism and Its History (New Haven: Yale University Press, 1981), the foundational modern study of the device. Kugel's analysis of biblical Hebrew's 'A, and what's more, B' verse structure treats naming the same referent twice in adjacent clauses — restating one idea in intensified, parallel language rather than introducing a second item — as the ordinary grammar of Hebrew poetic composition, not a stylistic ornament requiring special pleading; Zechariah 9:9's 'a donkey, on a colt, the foal of a donkey' is a textbook instance of exactly this pattern, one donkey named twice." }
'matthew-two-donkeys-davies-allison' = @{ title='Two saddles, one prophecy: the ICC verdict'; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 3: Matthew XIX-XXVIII (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 21:1-7. Davies and Allison's standard critical commentary reads Matthew's doubled animals — absent from Mark 11:2, Luke 19:30, and John 12:14, all of which describe a single colt — as the direct product of Matthew reading Zechariah 9:9's Hebrew synonymous parallelism in a flatly literal register, generating a second animal out of a second line of poetry describing the same one; the commentary treats this as one of the clearer, less contested instances in the Gospel of a citation's own literary form reshaping the narrative built to fulfill it. The same volume's commentary on 21:12-13 treats the money changers' presence in the outer court as unremarkable Temple infrastructure by this date, a routine service rather than a scandal in itself — Jesus's objection falls on the conduct and location of the trade, not on its mere existence." }
'sanders-temple-action-historicity' = @{ title='The one episode Sanders would bet on'; body="E. P. Sanders, Jesus and Judaism (Philadelphia: Fortress Press, 1985), chapter 1, 'Jesus and the Temple.' Sanders argues that some genuine disruptive act by Jesus in the Temple's outer court belongs among the historically best-attested episodes in the whole Gospel tradition, precisely because it is the single scene that most economically explains why the Jerusalem authorities moved against Jesus specifically when they did — at Passover, in the Temple itself, after a Galilean ministry the same authorities had otherwise left alone. Sanders reads the act itself as a symbolic enactment of the Temple's coming destruction and replacement rather than a moral protest against corrupt commerce, and treats whether Jesus intended reform or judgment as a real, currently unresolved question that the historicity argument does not by itself settle." }
'evans-cave-of-robbers-context' = @{ title='A Jewish context for the accusation, not an anti-Jewish one'; body="Craig A. Evans, 'Jesus and the Cave of Robbers: Toward a Jewish Context for the Temple Action,' Bulletin for Biblical Research 3 (1993): 93-110. Evans situates Jesus's combined citation of Isaiah 56:7 and Jeremiah 7:11 within a documented strand of first-century Jewish protest against the Temple establishment's own conduct — priestly corruption and exploitation attested independently in rabbinic and Qumran sources critical of the ruling priesthood — arguing the Temple action reads less as a rejection of Temple worship itself and more as a prophetic indictment delivered in the Temple's own idiom, aimed at its current management." }
'josephus-passover-census-crowds' = @{ title="Josephus's own head count at Passover"; body="Flavius Josephus, The Jewish War, Book 6, section 9.3, paragraphs 422-427 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press, 1928). Josephus reports that the Roman governor Cestius, wanting Nero to grasp Jerusalem's real scale, had the high priests count the Passover lambs slain in a single feast — 256,500 — and, reckoning a minimum of ten diners to a lamb as the law required, arrived at a crowd of roughly 2.7 million people gathered for the festival. The figure is almost certainly a rhetorical exaggeration rather than a literal census, but even a heavily discounted reading of it establishes Passover Jerusalem as swollen many times past its ordinary population — exactly the kind of crowd size this chapter's Temple-court commerce, and the money changers serving it, presupposes." }
'milgrom-dove-offering-poor' = @{ title='The sacrifice priced for someone with nothing'; body="Jacob Milgrom, Leviticus 1-16: A New Translation with Introduction and Commentary, Anchor Bible vol. 3 (New York: Doubleday, 1991), commentary ad loc. Leviticus 12:8 and 14:21-22. Milgrom's standard critical commentary documents the graduated Levitical offering scale built directly into Torah law: a woman completing childbirth purification, or a person restored from skin disease, who could not afford a lamb was required to bring 'two turtledoves or two young pigeons' instead (Leviticus 12:8; 14:21-22) — a real, textually mandated affordability provision, not an improvised discount. The dove-sellers this chapter names alongside the money changers existed specifically to supply that legally required substitute offering to worshippers too poor for the standard sacrifice." }
'telford-barren-temple-withered-tree' = @{ title='A whole monograph on one withered tree'; body="William R. Telford, The Barren Temple and the Withered Tree: A Redaction-Critical Analysis of the Cursing of the Fig-Tree Pericope in Mark's Gospel and Its Relation to the Cleansing of the Temple Tradition, JSOT Supplement Series 1 (Sheffield: JSOT Press, 1980). Telford's book-length study argues the fig-tree cursing (Mark 11:12-14, 20-21, and Matthew's compressed version at 21:18-19) functions as an enacted prophetic sign-act against the Temple itself, deliberately framed in Mark around the Temple action rather than reported as a stray nature miracle; the fig tree stands for Israel's religious establishment found fruitless at the moment of inspection, not for trees generally. Telford also documents that Matthew's version compresses Mark's two-stage telling (cursed on the way in, found withered on the way out) into a single scene where the tree withers 'at once' (21:19-20), sharpening the symbolic point at some cost to Mark's own slower narrative structure — a real, checkable editorial compression, not a separate incident." }
'derrett-figtrees-nt' = @{ title="The Old Testament's own withered-vine oracles"; body="J. Duncan M. Derrett, 'Figtrees in the New Testament,' Heythrop Journal 14, no. 3 (1973): 249-265. Derrett traces the Gospel fig-tree scenes back to an established Hebrew prophetic figure: fruitless or withered fig trees and vines used as a stock image for a nation or generation under divine judgment, present already in Jeremiah 8:13 ('there are no grapes on the vine, nor figs on the fig tree, and the leaf is withered'), Hosea 9:10 (Israel as 'the first fruit on the fig tree' gone bad), and Micah 7:1 (lamenting 'no first-ripe fig' left to find). Read against that background, Jesus cursing a fruitless fig tree is a recognizable prophetic-symbolic act operating in an existing genre, not an isolated or arbitrary miracle invented for this scene." }
'snodgrass-wicked-tenants-monograph' = @{ title="A parable ending on somebody else's poem"; body="Klyne R. Snodgrass, The Parable of the Wicked Tenants: An Inquiry into Parable Interpretation, Wissenschaftliche Untersuchungen zum Neuen Testament 27 (Tubingen: J. C. B. Mohr [Paul Siebeck], 1983). Snodgrass's dedicated monograph on this single parable reads its closing citation of Psalm 118:22-23 — 'the stone that the builders rejected has become the cornerstone' — as original to the parable's earliest layer rather than a later editorial addition, arguing the wordplay works underneath the Greek: in Hebrew and Aramaic, 'son' (ben) and 'stone' (eben) sound alike, tying the parable's rejected son directly to the closing citation's rejected stone as one continuous image rather than two spliced sources bolted together. Few single verses in the Hebrew Bible do this much reused work this early and this often; Snodgrass treats Matthew's closing citation here as one of the New Testament's genuine all-star proof texts, not an obscure or occasional one." }
'juel-messianic-exegesis-psalm118' = @{ title='One verse, cited everywhere the church needed a rejected messiah'; body="Donald Juel, Messianic Exegesis: Christological Interpretation of the Old Testament in Early Christianity (Philadelphia: Fortress Press, 1988). Juel's study of how the earliest church selected and reused specific Old Testament passages to argue Jesus's messianic identity documents Psalm 118:22 ('the stone that the builders rejected has become the cornerstone') as one of the small set of texts — alongside Psalm 110:1 and 2 Samuel 7 — that recur across independent early Christian sources precisely because they let the movement address its central embarrassment, a crucified messiah, in scripture's own vocabulary of rejection-then-vindication. Beyond this parable's own telling in Mark 12:10-11 and Luke 20:17, the same verse resurfaces in Peter's speech at Acts 4:11 and in 1 Peter 2:7, making it one of the most frequently redeployed single verses in the whole New Testament's messianic proof-texting." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
This chapter's Triumphal Entry contains Matthew's single most-discussed textual peculiarity, and it deserves more than a passing note. Matthew alone has the disciples bring back two animals — a donkey and her colt — and has Jesus somehow astride both at once, "sat on them" (21:7), where Mark, Luke, and John all describe Jesus riding one colt (Mark 11:7; Luke 19:35; John 12:14). Matthew's own text supplies the reason he thought two animals were required: Zechariah 9:9, "humble, and mounted on a donkey, on a colt, the foal of a donkey" (21:5).

Read as biblical Hebrew poetry rather than as a shopping list, Zechariah's line is doing something ordinary and well understood: restating a single image in two parallel phrases, the standard device — called synonymous parallelism — by which Hebrew verse intensifies a thought by saying it twice in different words rather than adding a second thing [[NOTE:zechariah-99-poetic-parallelism-kugel]]. One donkey, named twice, is exactly what the poetic form predicts; Zechariah is not describing a rider who needs two mounts.

The mainstream critical reading of Matthew's doubled animal is that he read this line in a flatter, less poetically attuned register than it was written in, and staged the scene to match — one animal for each half of the Hebrew parallelism, ridden as though the prophecy demanded a literal pair. W. D. Davies and Dale C. Allison's International Critical Commentary treats this as one of the cleaner, less contested instances in the Gospel of a citation's own literary form reshaping the narrative built to fulfill it [[NOTE:matthew-two-donkeys-davies-allison]]. That reading is not unanimous — a minority of scholars argue Matthew handled scripture too carefully elsewhere to have simply missed the parallelism, and read the second animal as a deliberate theological choice rather than a misreading — but the majority critical position, and the one the commentary tradition keeps returning to, is that this is Matthew's own redactional hand turning a poetic couplet into a doubled, and literally unworkable, physical detail [[NOTE:matthew-two-donkeys-davies-allison]]. Whichever explanation one prefers, this is a textbook case worth naming plainly: a small, checkable seam where translation and redaction, not an invented event, produced the version of the story now on the page.
'@

$beat2 = @'
The Temple action this chapter narrates (21:12-13) is, on the evidence, one of the more historically probable episodes in this entire Gospel — not because any outside record confirms it, but because of an influential argument from a different direction entirely. E. P. Sanders, in Jesus and Judaism, argues that some genuine disruptive act by Jesus in the Temple's outer court belongs among the historically best-attested episodes in the whole Gospel tradition, precisely because it is the single scene that most economically explains why the Jerusalem authorities moved against Jesus specifically when they did — at Passover, in the Temple itself, after a Galilean ministry the same authorities had otherwise left alone [[NOTE:sanders-temple-action-historicity]]. An itinerant teacher drawing crowds in Galilee was one kind of problem; the same teacher staging a disruptive, symbolically loaded confrontation inside the Temple courts at the festival that drew the largest crowds of the Jewish year is a different, much more urgent one, and Sanders reads that asymmetry as the strongest circumstantial case the Gospels have for any single episode's basic historicity.

This project's discussion of the same event in the Gospel of John's own version of the Temple action (John 2:13-22) already covers that account's separate, genuinely puzzling question of where the episode belongs in Jesus's chronology — John places it near the start of a three-year ministry, the Synoptics all place it in the final week. That is a real, open placement question, and it is not re-argued here. What Sanders adds is a different angle entirely: independent of where the episode sits on the timeline, some version of it happening at all is more, not less, historically credible than most of what surrounds it, because it does explanatory work that a merely symbolic or invented scene would not need to do. Craig A. Evans's study of the episode situates Jesus's combined citation of Isaiah 56:7 and Jeremiah 7:11 within a documented current of first-century Jewish protest against the Temple leadership's own conduct, reading the action as a prophetic indictment delivered in the Temple's own religious vocabulary rather than a wholesale rejection of Temple worship [[NOTE:evans-cave-of-robbers-context]]. What remains genuinely unresolved — and Sanders himself treats it as unresolved — is whether Jesus intended a reform of current practice or a symbolic enactment of the Temple's coming destruction; the historicity argument does not by itself settle that question [[NOTE:sanders-temple-action-historicity]].
'@

$beat3 = @'
The money changers and dove-sellers Jesus overturns were not staged as an arbitrary target; this book's own Notes have already established why Tyrian shekels, bearing a pagan god's image, were nonetheless the one coin the Temple tax legally required [45], and the Gospel of John's discussion of the same Temple-court economy covers that currency requirement in its own depth without needing to be repeated here. Davies and Allison's commentary on this same passage treats the money changers' presence in the outer court as unremarkable Temple infrastructure by this date, a routine service rather than a scandal in itself — Jesus's objection falls on the conduct and location of the trade, not on its mere existence [[NOTE:matthew-two-donkeys-davies-allison]].

What is worth adding at this specific point in the narrative is the sheer scale the scene assumes. Passover drew Jerusalem's largest crowds of the year by a wide margin, and Josephus reports a startling, if almost certainly inflated, attempt to measure just how large: the historian records that the governor Cestius, wanting Rome to appreciate Jerusalem's true size, had the priests count a single Passover's slaughtered lambs at 256,500, and, reckoning the required minimum of ten diners per lamb, arrived at a crowd of roughly 2.7 million people [[NOTE:josephus-passover-census-crowds]]. Even discounted heavily as festival-crowd rhetoric rather than a literal census, the figure establishes what this project's discussion of Passover crowds in the Gospel of Luke has already made clear: a Jerusalem swollen many times past its ordinary population, exactly the kind of pilgrim volume that would require a large, continuously running currency-exchange operation inside the Temple's outer court, not two or three tables set up as a narrative convenience.

The dove-sellers alongside the money changers answer a separate, equally real requirement. Leviticus sets a graduated offering scale precisely so poverty would not bar someone from the required sacrifice: a person too poor to afford a lamb after childbirth purification or skin-disease cleansing was permitted to bring "two turtledoves or two young pigeons" instead (Leviticus 12:8; 14:21-22), a substitution Jacob Milgrom's standard critical commentary on Leviticus documents as a genuine, textually mandated affordability provision rather than an improvised discount [[NOTE:milgrom-dove-offering-poor]]. Read together, the two trades this chapter names — currency exchange and dove sales — existed to serve Torah-mandated religious requirements that the Temple system itself could not function without; the commerce Jesus disrupts is real commerce built into a real, and by the numbers genuinely enormous, festival economy, not a straw target invented for the scene.
'@

$beat4 = @'
The withered fig tree (21:18-22) can look, out of context, like the oddest miracle in the Gospels — a seemingly petty curse against a tree "since it was not the season for figs" in Mark's more detailed version of the same event (Mark 11:13). Read against its real Old Testament background, though, it stops looking arbitrary and starts looking like a recognized prophetic genre. A fruitless or withered fig tree, or vine, standing for a nation or generation found wanting under divine judgment, is already an established prophetic image well before the Gospels: Jeremiah laments "there are no grapes on the vine, nor figs on the fig tree, and the leaf is withered" (Jeremiah 8:13); Hosea pictures Israel as "the first fruit on the fig tree in its first season," then gone bad (Hosea 9:10); Micah mourns that "there is no cluster to eat, no first-ripe fig that my soul desires" (Micah 7:1) [[NOTE:derrett-figtrees-nt]]. Set beside those three passages, Jesus cursing a fruitless fig tree at exactly this point in the narrative — directly bracketing the Temple action in Mark's fuller version, and following it immediately in Matthew's compressed one — reads as an enacted version of the same prophetic charge, not an isolated nature miracle or a display of bad temper.

William R. Telford's book-length study of this exact pericope argues the fig tree stands specifically for the Temple establishment found fruitless at the moment of inspection, deliberately staged around the Temple action rather than narrated as a stray incident [[NOTE:telford-barren-temple-withered-tree]]. Matthew's version compresses Mark's two-stage telling (cursed on the way in, found withered on the way out) into a single scene where the tree withers "at once" (21:19-20), sharpening the symbolic point at some cost to Mark's own slower narrative structure — a real, checkable editorial compression, not a separate incident [[NOTE:telford-barren-temple-withered-tree]].
'@

$beat5 = @'
The parable of the wicked tenants closes with Jesus quoting Psalm 118:22-23: "the stone that the builders rejected has become the cornerstone... this was the Lord's doing, and it is marvelous in our eyes" (21:42). It is worth being direct about how heavily worked this one citation is across the whole New Testament, because that reception history is itself a genuine, checkable fact rather than a matter of interpretation. The same verse closes the same parable in both Mark (12:10-11) and Luke (20:17), Peter cites it again in his speech before the Sanhedrin in Acts 4:11 ("this Jesus is the stone that was rejected by you, the builders, which has become the cornerstone"), and it resurfaces once more in 1 Peter 2:7 — one verse, independently redeployed across at least five separate New Testament texts.

Klyne Snodgrass's dedicated monograph on this parable argues the stone citation belongs to the parable's earliest layer rather than a later editorial graft, because the wordplay works underneath the Greek: in Hebrew and Aramaic, "son" (ben) and "stone" (eben) sound alike, tying the parable's rejected son directly to the closing citation's rejected stone as one continuous image rather than two spliced sources bolted together [[NOTE:snodgrass-wicked-tenants-monograph]]. Donald Juel's study of how the earliest church selected Old Testament texts to argue Jesus's messianic identity places Psalm 118:22 alongside Psalm 110:1 as one of the small set of passages that recur across independent early Christian sources precisely because they let the movement address its central embarrassment — a rejected, executed messiah — in scripture's own vocabulary of rejection followed by vindication [[NOTE:juel-messianic-exegesis-psalm118]]. Few single verses in the Hebrew Bible do this much reused work this early and this often; Matthew closes his wicked-tenants parable on one of the New Testament's genuine all-star proof texts, not an obscure or occasional citation [[NOTE:snodgrass-wicked-tenants-monograph]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'ZECHARIAH 9:9 (TWO-ANIMAL MISREADING)' = "The prophetic verse Matthew cites for the Triumphal Entry (21:5), 'humble, and mounted on a donkey, on a colt, the foal of a donkey' — Hebrew synonymous parallelism naming one animal twice [[NOTE:zechariah-99-poetic-parallelism-kugel]]. Matthew alone stages the scene with two literal animals, a doubling read by the mainstream critical commentary tradition as a literalized misreading of the poetic form rather than a separately intended detail [[NOTE:matthew-two-donkeys-davies-allison]]."
'FIG TREE (PROPHETIC JUDGMENT SYMBOL)' = "The tree Jesus curses for bearing no fruit (21:18-22), read by mainstream scholarship not as an isolated nature miracle but as an enacted version of an established Old Testament prophetic image — fruitless fig trees and vines standing for a nation found wanting under judgment, already present in Jeremiah 8:13, Hosea 9:10, and Micah 7:1 [[NOTE:derrett-figtrees-nt]]. A dedicated redaction-critical study of the scene argues it functions as a symbolic indictment of the Temple establishment specifically, staged around the Temple action rather than reported as a separate incident [[NOTE:telford-barren-temple-withered-tree]]."
'WICKED TENANTS (PSALM 118 / REJECTED STONE)' = "The parable of the vineyard tenants who kill the owner's son (21:33-46), closing on Jesus's citation of Psalm 118:22-23, 'the stone that the builders rejected has become the cornerstone' (21:42). A dedicated monograph on this parable argues the stone citation belongs to its earliest layer, tied to the rejected-son plot by a Hebrew/Aramaic sound-play between 'son' (ben) and 'stone' (eben) [[NOTE:snodgrass-wicked-tenants-monograph]]. The same verse is independently cited in Mark 12:10-11, Luke 20:17, Acts 4:11, and 1 Peter 2:7, making it one of the most frequently reused single verses in early Christian messianic proof-texting [[NOTE:juel-messianic-exegesis-psalm118]]."
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
    Add-BeatNode $Ch21NodeId $id $sortKey
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
Seed-Entity "Zechariah 9:9 (Two-Animal Misreading)" "zechariah-9-9-two-animal-misreading" "vocabulary" "Matthew's doubling of the single donkey of Zechariah 9:9's Hebrew poetic parallelism into two literal animals at the Triumphal Entry (21:1-7); read by mainstream commentary as a literalized misreading of a poetic device."
Seed-Entity "Fig Tree (Prophetic Judgment Symbol)" "fig-tree-prophetic-judgment-symbol" "vocabulary" "The fig tree cursed for bearing no fruit (Matthew 21:18-22), grounded in the Old Testament prophetic genre of fruitless fig-tree/vine judgment oracles (Jeremiah 8:13; Hosea 9:10; Micah 7:1)."
Seed-Entity "Wicked Tenants (Psalm 118 / Rejected Stone)" "wicked-tenants-psalm-118-rejected-stone" "vocabulary" "The parable of the vineyard tenants (Matthew 21:33-46) closing on Psalm 118:22-23's rejected-stone citation, one of the most frequently reused verses in early Christian messianic proof-texting."
Seed-Entity "James Kugel" "james-kugel" "character" "Biblical scholar; author of The Idea of Biblical Poetry: Parallelism and Its History (Yale University Press, 1981), the foundational modern study of Hebrew poetic parallelism."
Seed-Entity "J. Duncan M. Derrett" "j-duncan-m-derrett" "character" "Biblical and legal scholar; author of 'Figtrees in the New Testament,' Heythrop Journal 14 (1973), tracing the Gospel fig-tree scenes to Old Testament prophetic background."

$conn.Close()
Write-Host "DONE Chapter 21 depth pass."
