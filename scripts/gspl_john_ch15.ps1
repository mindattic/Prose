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
$NotesNodeId = [guid]"019FA96D-7D48-75E0-9BD9-2190171276DC"
$GlossaryNodeId = [guid]"019FA96D-8DD0-70E4-8D98-34AC48833B7E"
$Ch15NodeId = [guid]"019FA96D-090A-7773-A9D3-7CDDE6929C7D"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'vine-as-israel-ot-background' = @{ title="The vine as Israel's own self-image, long before John"; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), commentary ad loc. John 15:1-8. Brown surveys the vine and vineyard as one of the Hebrew Bible's most established metaphors for Israel itself: Psalm 80:8-16 pictures Israel as a vine God transplanted out of Egypt and now abandoned to ruin; Isaiah 5:1-7's song of the vineyard indicts Israel for producing wild grapes instead of the justice God expected; Jeremiah 2:21 asks how a vine planted as a choice vine, wholly of pure seed, degenerated into the wild shoots of a foreign vine; Ezekiel 15 and 19:10-14 both use vine imagery for a nation now fit only for burning; and Hosea 10:1 calls Israel a luxuriant vine whose very fruitfulness became the occasion for idolatry. Against that dense background, Jesus's declaration I am the true vine is not a neutral pastoral image but a direct claim to be what Israel itself was called, and had failed, to be." }
'true-vine-substitution-claim' = @{ title="A claim to replace, not merely illustrate"; body="C.K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 15:1. Barrett reads the adjective true (alethine) in I am the true vine the same way it functions elsewhere in John's ego eimi sayings: not one vine among several, but the genuine reality toward which Israel's own vine imagery had only ever pointed. On this reading the allegory is a substitution or fulfillment claim in the fullest sense, placing Jesus, not the nation, at the center of the covenant relationship the vine had always symbolized." }
'isaiah-5-song-of-vineyard' = @{ title="The song of the vineyard, read straight"; body="Brevard S. Childs, Isaiah, Old Testament Library (Louisville, KY: Westminster John Knox Press, 2001), commentary on Isaiah 5:1-7. Childs reads Isaiah's song of the vineyard as a love song turned legal indictment: the prophet sings of a friend's vineyard, planted with every care, that nonetheless yields wild grapes, before the song's final line names the vineyard outright as the house of Israel and the planting as the men of Judah. The genre, a love song curdling into a courtroom accusation, is itself part of the rhetorical shock Isaiah's original audience would have felt, a shock John's Gospel does not reproduce but clearly assumes its own audience can supply." }
'viticulture-pruning-burning-practice' = @{ title="Pruning and burning were real farm labor, not invented imagery"; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 15:2, 6. Keener situates the allegory's pruning and burning within documented first-century Mediterranean viticulture practice, drawing on Greco-Roman agricultural writers such as Columella (De Re Rustica) who describe cutting back unproductive vine wood each season and burning the cuttings removed from the vine. The passage's agricultural detail assumes real, familiar farm labor its first audience would have recognized on sight, not a metaphor invented for theological effect." }
'abide-menein-key-term' = @{ title="A verb repeated on purpose"; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids, MI: Eerdmans, 1991), commentary ad loc. John 15:4-10. Carson notes that the Greek verb menein (abide or remain) occurs roughly a dozen times across this short passage alone, more densely than almost anywhere else in the Gospel, and argues the repetition is deliberate: continuance in relationship, not a single decisive moment, is the allegory's real subject." }
'johannine-discourse-seam-14-31' = @{ title="Rise, let us go hence, and then three more chapters"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray (Philadelphia: Westminster Press, 1971), commentary ad loc. John 14:31. Jesus's words closing chapter 14, Rise, let us go hence, read like the discourse's natural ending, yet three further chapters of teaching follow before anyone actually leaves the room (18:1). Bultmann treated the seam as evidence that the Farewell Discourse material reached its final form through rearrangement of originally separate source units rather than as a single continuous speech transcribed in real time." }
'brown-aporia-farewell-discourse' = @{ title="Aporias: the commentator's word for a seam"; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), introduction, section on the composition of the Farewell Discourse. Brown uses the term aporia, a Greek word for a difficulty or impasse, for exactly this kind of internal seam in John's narrative, cataloguing the 14:31/chapter 15 transition among the Gospel's clearest examples; he is more cautious than Bultmann about reconstructing the precise editorial history behind it, but agrees the seam itself is real and not a modern illusion." }
'john-15-8-textual-variant' = @{ title="A mood swing in the manuscripts"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), discussion ad loc. John 15:8. The Nestle-Aland apparatus records a split between genesthe (aorist subjunctive, that you become or prove to be my disciples) and genesesthe (future indicative, that you will become my disciples) at 15:8, a small grammatical variant with a real interpretive stake: the subjunctive reading ties discipleship to bearing fruit as an ongoing condition, while the future indicative reads more like a straightforward prediction." }
'douloi-to-philoi-friendship-shift' = @{ title="A change of address, not a compliment in passing"; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 15:15. Keener treats Jesus's shift from calling the disciples douloi (servants or slaves) to philoi (friends) as a deliberate register change rather than incidental wording, tied explicitly in the text to the reason given: a servant does not know his master's business, while a friend has been told everything the master has heard from his own Father." }
'konstan-greco-roman-friendship' = @{ title="What Greco-Roman friendship actually required"; body="David Konstan, Friendship in the Classical World (Cambridge: Cambridge University Press, 1997), chapters on Greek and Roman ethical writing about philia and amicitia. Konstan documents that Greco-Roman philosophical convention treated full, mutual disclosure between parties as one of the defining marks distinguishing genuine friendship from a hierarchical or merely useful relationship; commentators including Keener and Brown read John 15:15's I have made known to you everything I have heard from my Father against exactly this convention, so that Jesus's language of friendship carries a recognizable, loaded social meaning for a Greco-Roman as much as a Jewish audience." }
'greater-love-dramatic-irony' = @{ title="A saying that names its own author's death, hours early"; body="C.K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 15:13. Barrett notes the saying's position within the narrative's own dramatic irony: Jesus speaks of laying down one's life for one's friends as the greatest possible love only hours before his own arrest and execution, so that the general maxim doubles, for John's reader if not yet for the disciples at table, as a direct anticipation of what is about to happen to the speaker himself." }
'hated-without-cause-psalms' = @{ title="A citation with an address, not a footnote"; body="George R. Beasley-Murray, John, Word Biblical Commentary vol. 36, 2nd ed. (Nashville: Thomas Nelson, 1999), commentary ad loc. John 15:25. Beasley-Murray identifies the unattributed their law citation, they hated me without a cause, as drawn from Psalm 35:19 and/or Psalm 69:4, both psalms of an innocent sufferer surrounded by enemies who hate him for no just reason; John's Jesus applies the psalmist's own complaint to himself, reading his rejection as the fulfillment of Israel's own scripture about unjust hatred of the righteous." }
'persecuted-righteous-sufferer-psalms-pattern' = @{ title="A pattern, not a one-off proof text"; body="D. Moody Smith, The Theology of the Gospel of John (Cambridge: Cambridge University Press, 1995), discussion of John's use of Scripture in the Farewell Discourse. Smith situates the Psalm 35/69 citation within a wider Johannine pattern of applying Israel's own psalms of the persecuted righteous sufferer to Jesus, and, by extension in passages like this one, to the disciples who will share his rejection; the move lets the Gospel present hostility toward the community not as an anomaly needing explanation but as continuous with Israel's own scriptural memory of what righteousness costs." }
'brown-community-persecution-context' = @{ title="Pastoral encouragement to people already living it"; body="Raymond E. Brown, The Community of the Beloved Disciple (New York: Paulist Press, 1979), discussion of the Johannine community's social history. Brown reads the Farewell Discourse's insistence that the world will hate the disciples as it hated Jesus (15:18-25) as most plausibly addressed to a real, historically situated community already experiencing hostility and ostracism, most concretely from the synagogue relationship already explored in connection with chapter 9's account of exclusion (John 9:22; 12:42; 16:2): pastoral encouragement to an audience living the persecution being described, not abstract theology composed at a comfortable distance from it." }
'ehrman-johannine-dualism' = @{ title="A Gospel built on hard opposites"; body="Bart D. Ehrman, The New Testament: A Historical Introduction to the Early Christian Writings, 7th ed. (New York: Oxford University Press, 2019), chapter on the Gospel of John. Ehrman describes John's governing rhetorical habit of sorting reality into stark opposed pairs, light and darkness, above and below, life and death, and here the disciples' own circle set against a world that hates them, as a developed, distinctive Johannine theological idiom, not a neutral description of social reality but a deliberate framework the Gospel uses to make sense of the community's own experience." }
'paraclete-dual-testimony' = @{ title="Two witnesses named side by side"; body="Urban C. von Wahlde, The Gospel and Letters of John, vol. 2: Commentary on the Gospel of John, Eerdmans Critical Commentary (Grand Rapids, MI: Eerdmans, 2010), commentary ad loc. John 15:26-27. Von Wahlde notes the deliberate pairing at the close of the chapter: the Spirit of truth will testify about Jesus, and you also will testify, because you have been with me from the beginning, joining a promised future testimony to the disciples' own eyewitness testimony as two complementary, not competing, forms of witness the Gospel wants its readers to trust together." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Jesus's closing words at the end of the previous chapter, "Rise, let us go hence" (14:31), read like the natural end of a speech — and then nobody moves for three more chapters [[NOTE:johannine-discourse-seam-14-31]] [[NOTE:brown-aporia-farewell-discourse]]. Whatever the compositional history behind that seam, what follows here is one of the most famous images in the whole Gospel: Jesus declares "I am the true vine, and my Father is the vinedresser." Every branch that fails to bear fruit, the Father removes; every branch that does bear fruit, he prunes to make it bear more. The disciples are told to "abide" in the vine as branches, because a branch cut off can do nothing and, gathered up, is thrown into the fire and burned. Whoever abides in Jesus and lets his words abide in them may ask what they will, and it will be done for them; bearing much fruit is how the Father is glorified and how the disciples prove themselves to be Jesus's own (15:1-8).

The claim "I am the true vine" is not a piece of generic pastoral scenery, and readers steeped in Israel's own scripture would not have heard it that way. The vine and the vineyard are among the most worked-over metaphors in the Hebrew Bible for Israel itself: a transplanted, ravaged vine in Psalm 80, a vineyard gone to wild grapes in Isaiah's "song of the vineyard," a "choice vine" degenerated into foreign wild shoots in Jeremiah, a vine fit only for burning in Ezekiel, a fruitful vine turned to idolatry in Hosea [[NOTE:vine-as-israel-ot-background]]. Isaiah's version is worth pausing on for its own sake: a love song that turns, mid-poem, into a courtroom indictment, ending by naming the vineyard outright as "the house of Israel" [[NOTE:isaiah-5-song-of-vineyard]]. Against that whole tradition, "I am the true vine" is a direct substitution claim — Jesus presenting himself as what Israel itself was called, and had repeatedly been charged with failing, to be, not merely borrowing a pleasant image from the family farm [[NOTE:true-vine-substitution-claim]].

The agricultural detail underneath the allegory is equally concrete. Cutting back unproductive vine wood and burning the cuttings removed from the plant were standard, unremarkable first-century Mediterranean viticulture, attested in the period's own agricultural writers [[NOTE:viticulture-pruning-burning-practice]]. The repeated verb "abide" (menein) is doing real work too — it recurs roughly a dozen times in this short passage, a density found almost nowhere else in the Gospel, which is itself a sign that continuance in relationship, not a single decisive moment of belief, is the allegory's actual subject [[NOTE:abide-menein-key-term]]. And even the chapter's closing promise carries its own small text-critical wrinkle: whether the disciples are told to "prove to be" (subjunctive) or simply "will become" (future indicative) Jesus's disciples by bearing fruit is a genuine split in the Greek manuscript tradition, not a difference smoothed over by every translation the same way [[NOTE:john-15-8-textual-variant]].
'@

$beat2 = @'
Jesus tells the disciples to abide in his love as he has abided in his Father's love, by keeping his commandments, "that my joy might remain in you, and that your joy might be full." His own commandment, restated here, is that they love one another as he has loved them — and no one, he says, has greater love than this: that a man lay down his life for his friends. He calls them friends now, not servants, because a servant does not know what his master is doing, while Jesus has made known to them everything he has heard from his Father. They did not choose him; he chose and appointed them, that they should go and bear lasting fruit, and that whatever they ask the Father in his name, he will give them. His command to them, once more, is simply this: love one another (15:9-17).

The saying about greater love deserves to be read with its own dramatic timing in view. Jesus is describing laying down one's life for one's friends as the greatest love there is, at a table only hours before his own arrest and execution — so that a saying offered here as a general maxim about love reads, for John's audience if not yet for the men listening at the table, as a direct anticipation of what is about to happen to the speaker himself [[NOTE:greater-love-dramatic-irony]]. That's a literary observation worth making plainly: the text is not simply teaching an ideal, it is quietly narrating its own author's death in advance.

The shift from "servants" to "friends" is a real change of register, not a passing compliment. Jesus explicitly ties it to disclosure — a servant isn't told the master's business, a friend is told everything — and commentators read that move as deliberate [[NOTE:douloi-to-philoi-friendship-shift]]. It also lands on ground a Greco-Roman audience, and not only a Jewish one, would recognize: Greek and Roman ethical writing about friendship treated full mutual disclosure between parties as one of the very things that distinguished a genuine friend from a servant, a client, or a merely useful acquaintance, and several commentators read Jesus's language here as trading directly on that shared cultural convention [[NOTE:konstan-greco-roman-friendship]].
'@

$beat3 = @'
Jesus turns from love to hatred. If the world hates the disciples, he says, they should know it hated him first; because he chose them out of the world, the world hates them precisely because they no longer belong to it. Servants are not greater than their master — if the world persecuted Jesus, it will persecute the disciples too; if it kept his word, it will keep theirs also, though all of it will be done to them on account of his name, because those who do it do not know the one who sent him. Had Jesus not come and spoken to them, the world would have no sin, but now it has no excuse; whoever hates him hates his Father also. His works among them, unlike anyone else's, have left them no excuse either — both Jesus and his Father are now hated, fulfilling the word written in "their law": "They hated me without a cause." And when the Helper comes, the Spirit of truth who proceeds from the Father, he will testify about Jesus; and the disciples too will testify, because they have been with him from the beginning (15:18-27).

The citation closing out the chapter is doing real, specific work, not decorating a general complaint. "They hated me without a cause" draws on Psalm 35:19 and/or Psalm 69:4, both psalms spoken by an innocent sufferer surrounded by enemies who hate him for no just reason at all [[NOTE:hated-without-cause-psalms]]. That citation belongs to a wider pattern across the Farewell Discourse of reading Israel's own psalms of the persecuted righteous sufferer onto Jesus, and here, by direct extension, onto the disciples who are told they will share his rejection [[NOTE:persecuted-righteous-sufferer-psalms-pattern]]. Much of the historical-critical reading of this material turns on who, exactly, is being comforted. Mainstream scholarship reads "the world hates you" less as abstract theology composed at a safe distance and more as pastoral encouragement addressed to a real community already living the hostility being described — concretely, the same synagogue estrangement already explored at chapter 9's account of expulsion [[NOTE:brown-community-persecution-context]]. The sharp, binary shape of the language itself — Jesus and the Father on one side, "the world" arrayed against them on the other — is characteristic of John's whole Gospel, which habitually sorts reality into opposed pairs (light and darkness, above and below, life and death) as its own developed theological idiom rather than a neutral report of social conditions [[NOTE:ehrman-johannine-dualism]]. The chapter's final promise pairs two forms of testimony deliberately: the Spirit's future witness about Jesus, set beside the disciples' own witness from having been present from the beginning, offered together rather than as competing claims [[NOTE:paraclete-dual-testimony]].
'@

$beats = @($beat1, $beat2, $beat3)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'TRUE VINE (JOHN 15 ALLEGORY)' = "Jesus's declaration `"I am the true vine`" (15:1), opening an extended allegory in which the Father is the vinedresser, Jesus is the vine, and the disciples are branches that must abide in him to bear fruit. In its original Jewish context this is not a generic pastoral image but a loaded claim to be what Israel itself was called, and repeatedly indicted for failing, to be [[NOTE:vine-as-israel-ot-background]] [[NOTE:true-vine-substitution-claim]]. The allegory's pruning and burning of unproductive branches (15:2, 6) reflects documented first-century viticulture practice rather than invented imagery [[NOTE:viticulture-pruning-burning-practice]]."
'VINE AND VINEYARD AS ISRAEL (OLD TESTAMENT METAPHOR TRADITION)' = "One of the Hebrew Bible's most established images for the nation of Israel, appearing across the Torah, Prophets, and Writings: Psalm 80:8-16's transplanted and ravaged vine, Isaiah 5:1-7's song of the vineyard, Jeremiah 2:21's degenerated wild shoots of a foreign vine, Ezekiel 15 and 19:10-14's vine fit only for burning, and Hosea 10:1's luxuriant vine turned to idolatry [[NOTE:vine-as-israel-ot-background]]. John 15's true vine allegory assumes this whole tradition as background rather than introducing the image from scratch."
'SONG OF THE VINEYARD (ISAIAH 5:1-7)' = "A short poem in Isaiah, structured as a love song for a friend's vineyard that curdles into a courtroom indictment once the vineyard yields only wild grapes; its final lines name the vineyard outright as `"the house of Israel`" [[NOTE:isaiah-5-song-of-vineyard]]. It stands as one of the most fully developed instances of the broader vine-as-Israel tradition John 15 draws on [[NOTE:vine-as-israel-ot-background]]."
'FRIENDS, NOT SERVANTS (JOHN 15:15)' = "Jesus's statement that he no longer calls the disciples `"servants`" (douloi) but `"friends`" (philoi), because a servant does not know his master's business while a friend has been told everything the master has heard from his own Father (15:15). Commentators read the shift as a deliberate register change resonating with Greco-Roman philosophical convention, in which full mutual disclosure was itself a defining mark of genuine friendship [[NOTE:douloi-to-philoi-friendship-shift]] [[NOTE:konstan-greco-roman-friendship]]."
'"GREATER LOVE HAS NO MAN THAN THIS" (JOHN 15:13)' = "Jesus's statement that no one has greater love than to lay down his life for his friends (15:13), spoken at table only hours before his own arrest and execution. Read within the narrative's own dramatic irony, the general maxim doubles as an anticipation of what is about to happen to the speaker himself [[NOTE:greater-love-dramatic-irony]]."
'"THEY HATED ME WITHOUT A CAUSE" (JOHN 15:25)' = "Jesus's citation of an unnamed scriptural line, drawn from Psalm 35:19 and/or Psalm 69:4, applied to explain the world's hatred as fulfilling `"their law`" (15:25) [[NOTE:hated-without-cause-psalms]]. The citation belongs to a wider Johannine pattern of reading Israel's own psalms of the persecuted righteous sufferer onto Jesus and, by extension, onto the disciples who share his rejection [[NOTE:persecuted-righteous-sufferer-psalms-pattern]]."
"THE SPIRIT'S TESTIMONY (JOHN 15:26-27)" = "The chapter's closing promise that the Spirit of truth, sent from the Father, will testify about Jesus, paired immediately with the disciples' own testimony, `"because you have been with me from the beginning`" (15:26-27): two complementary forms of witness named side by side rather than as competing claims [[NOTE:paraclete-dual-testimony]]."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum — $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats with placeholder replacement ----
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch15NodeId $id $sortKey
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
Seed-Entity "True Vine (John 15 Allegory)" "true-vine-john-15-allegory" "vocabulary" "Jesus's I am the true vine allegory (John 15:1-8); loaded claim to be what Israel's own vine/vineyard scriptural self-image was called to be, not a generic pastoral image."
Seed-Entity "Song of the Vineyard (Isaiah 5)" "song-of-the-vineyard-isaiah-5" "vocabulary" "Isaiah 5:1-7's love-song-turned-indictment poem naming Israel as the vineyard; part of the Old Testament vine-as-Israel tradition John 15 assumes as background."

$conn.Close()
Write-Host "DONE Chapter 15."
