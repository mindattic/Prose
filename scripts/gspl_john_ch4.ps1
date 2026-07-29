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
$Ch4NodeId = [guid]"019FA96C-4DDB-7B4D-B990-A34B5D80427C"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND ISNUMERIC(LEFT(b.Text, CHARINDEX(' ',b.Text)-1))=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96C-4DDB-7B4D-B990-A34B5D80427C'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'jacobs-well-location-balata' = @{ title='A well with an unusually stable location'; body='Jerome Murphy-O''Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on Jacob''s Well, Nablus. Unlike most Gospel sites, Jacob''s Well has been shown to essentially the same deep rock-cut shaft at Balata, on the eastern edge of Nablus (ancient Shechem), since at least the fourth century CE, when the pilgrim Egeria and others describe visiting it. A well cut through bedrock rather than merely dug is a fixed feature of the landscape in a way a building or tomb is not, which is why this identification is unusually secure by Gospel-geography standards; the site now sits beneath a Greek Orthodox monastery church.' }
'sychar-identification-debate' = @{ title='Sychar: Shechem under another name, or a separate village?'; body='C. K. Barrett, The Gospel According to St John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 4:5. Barrett surveys the two live proposals for Sychar: that it is simply another name for Shechem itself, largely abandoned since the second century BCE but adjacent to Jacob''s Well, or that it is the separate, still-inhabited nearby village of ''Askar at the foot of Mount Ebal. Neither identification can be fully confirmed, though the well''s own fixed location constrains the possibilities to that immediate area either way.' }
'living-water-wisdom-literature-background' = @{ title='Living water and wisdom on tap'; body='Craig S. Keener, The Gospel of John: A Commentary, 2 vols. (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 4:10-14. Keener situates Jesus''s offer of water that becomes in the believer ''a spring of water welling up to eternal life'' within an established Jewish wisdom tradition in which Wisdom herself is imaged as water or an overflowing fountain (Proverbs 8-9; Sirach 24:21; Baruch 3:12, rebuking Israel for forsaking the fountain of wisdom). On this reading the Johannine Jesus claims, in image, to supply what wisdom literature had promised only Torah and the fear of the Lord could supply.' }
'john-4-9-jews-samaritans-clause-variant' = @{ title='A parenthetical clause some manuscripts do not have'; body='Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), ad loc. John 4:9. The explanatory clause ''For Jews have no dealings with Samaritans'' is omitted by a handful of early witnesses, including Codex Sinaiticus in its original hand and some Old Latin manuscripts. The UBS/Nestle-Aland editorial committee reads it as most likely an authentic Johannine aside later dropped by scribes who found it either obvious or overly broad, rather than a later gloss added to the text, though Metzger notes the committee''s confidence in that judgment is only moderate.' }
'josephus-cumanus-samaritan-galilean-clash' = @{ title='The narrator''s aside against the messier historical record'; body='Flavius Josephus, Jewish Antiquities, Book 20, sections 118-136 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press). Josephus describes a violent clash between Galilean pilgrims and Samaritan villagers under the procurator Cumanus in the mid-first century CE, alongside other episodes of mutual hostility, but he also describes elsewhere routine travel, trade, and even intermarriage across the Jewish-Samaritan boundary. John''s aside that Jews and Samaritans have no dealings with one another (4:9) captures a real social distance and periodic violence, historically attested, but overstates it as a total, uniform separation; the fuller archaeological and Josephan record together show a relationship closer to strained coexistence, punctuated by real hostility, than to total mutual avoidance.' }
'john-4-1-jesus-lord-textual-variant' = @{ title='Jesus, or the Lord, at the chapter''s opening'; body='Textual apparatus of the Nestle-Aland Novum Testamentum Graece, ad loc. John 4:1, discussed in Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), ad loc. Some early manuscripts, including Codex Sinaiticus, open the chapter with a clause naming ''the Lord'' rather than ''Jesus'' as the one who learned of the Pharisees'' report. The UBS committee prefers ''Jesus'' as the harder, less liturgically elevated reading, but the split is a reminder that even the chapter''s opening clause rests on a manuscript judgment call rather than a single uncontested wording.' }
'samaritan-jewish-schism-history' = @{ title='The Samaritan-Jewish schism: how far back does it go?'; body='Reinhard Pummer, The Samaritans: A Profile (Grand Rapids, MI: Eerdmans, 2016), chapters on Samaritan origins and the schism with Jerusalem. Pummer traces the split''s roots to the aftermath of the Assyrian resettlement of the northern kingdom in 722 BCE (2 Kings 17:24-41) and its sharpening through the Persian and Hellenistic periods, when the Samaritan community built its own temple on Mount Gerizim and rejected Jerusalem''s claim to be the sole legitimate sanctuary. He stresses that the schism describes a centuries-long, contested process of mutual exclusion rather than a single datable break.' }
'mount-gerizim-temple-archaeology' = @{ title='A Samaritan temple standing on Gerizim, excavated'; body='Yitzhak Magen, Mount Gerizim Excavations II: A Temple City, Judea and Samaria Publications 8 (Jerusalem: Israel Antiquities Authority, 2008). Magen''s excavation uncovered a large Samaritan sacred precinct and associated city atop Mount Gerizim, with construction phases running from the Persian period through the Hellenistic era, physically confirming that Samaritans maintained a genuine rival temple and holy city on the mountain the Samaritan woman names in 4:20, not merely a claim reported secondhand by hostile outsiders.' }
'josephus-gerizim-temple-destruction' = @{ title='John Hyrcanus burns the rival temple'; body='Flavius Josephus, Jewish Antiquities, Book 13, sections 254-256 (Loeb Classical Library, trans. Ralph Marcus, Cambridge, MA: Harvard University Press). Josephus reports that the Hasmonean ruler John Hyrcanus I destroyed the Samaritan temple on Mount Gerizim around 128 BCE, roughly a century and a half before this scene. The Samaritan woman''s Gerizim, by the time of Jesus, was a mountain that had once held a functioning rival temple and by then held only its ruins, a fact John''s narrative itself never states outright.' }
'samaritan-pentateuch-gerizim-variant' = @{ title='Whose mountain does the text itself name?'; body='Robert T. Anderson and Terry Giles, The Samaritan Pentateuch: An Introduction to Its Origin, History, and Significance for Biblical Studies, SBL Resources for Biblical Study 72 (Atlanta: Society of Biblical Literature, 2012). Anderson and Giles document that the Samaritan Pentateuch''s own text of Deuteronomy 27:4 names Mount Gerizim as the site where Israel is commanded to build its altar, where the Masoretic (Jewish) text names Mount Ebal instead. A one-word textual variant underlies the very dispute the Samaritan woman raises with Jesus, since each community''s own scripture grounds its claim to be the authentic sanctuary location.' }
'worship-spirit-truth-theology' = @{ title='Neither this mountain nor Jerusalem'; body='Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray, R. W. N. Hoare, and J. K. Riches (Philadelphia: Westminster Press, 1971), commentary ad loc. John 4:21-24. Bultmann reads Jesus''s answer that true worshipers will worship the Father ''in spirit and truth,'' independent of either Gerizim or Jerusalem, as programmatic for the Fourth Gospel''s broader theology, which repeatedly relocates the locus of true worship away from any physical sanctuary and onto the person of Jesus himself. On this reading the passage answers not just a local Samaritan-Jewish dispute but the Gospel''s own recurring argument that the Jerusalem Temple''s exclusive claim has been superseded.' }
'five-husbands-allegory-debate' = @{ title='Five husbands: a woman''s history, or Samaria''s?'; body='Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 4:16-18. Brown lays out a reading, traced to early patristic commentators, in which the woman''s five husbands allegorize 2 Kings 17:24-31''s report that the Assyrians resettled Samaria with people from five foreign nations, each bringing its own god — five gods for five peoples, echoed in five husbands for one woman, with her current unmarried partner standing for Samaria''s ongoing worship of a further, illegitimate cultic loyalty. Brown himself judges the allegory ingenious but unproven, noting most modern commentators read the detail as part of the individual woman''s own biography, whether historical or a literary device establishing her as morally compromised, rather than as coded national history; the text supports either reading without settling the question.' }
'samaritan-belief-acts8-mission-parallel' = @{ title='A first harvest before the harvest Acts describes'; body='D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), commentary ad loc. John 4:39-42. Smith notes that this episode, Samaritans coming to faith first through a woman''s testimony and then through direct encounter with Jesus, anticipates within John''s own narrative world the later and better-attested Samaritan mission described independently in Acts 8:4-25, where Philip preaches in Samaria and the Jerusalem apostles Peter and John follow up. Smith reads John 4 as narrating in miniature, and in advance, a mission Acts otherwise credits entirely to the post-resurrection church.' }
'savior-of-the-world-title' = @{ title='A title John''s Gospel uses exactly once'; body='Urban C. von Wahlde, The Gospel and Letters of John, 3 vols., Eerdmans Critical Commentary (Grand Rapids: Eerdmans, 2010), commentary ad loc. John 4:42. Von Wahlde observes that ''Savior of the world'' is applied to Jesus by the Samaritan townspeople here and nowhere else in the Fourth Gospel; its only other New Testament occurrence is 1 John 4:14. He treats its placement in the mouths of Samaritans, rather than Jewish disciples, as a deliberate Johannine signal that the title''s universal scope was recognized first by outsiders to the Jewish-Samaritan quarrel the chapter has just spent thirty verses narrating.' }
'royal-official-basilikos-identity' = @{ title='A courtier of Herod''s court, not a Roman centurion'; body='Craig S. Keener, The Gospel of John: A Commentary, 2 vols. (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 4:46-54. John''s Greek calls the petitioner a basilikos, literally ''royal'' or ''belonging to the king,'' almost certainly an officer or courtier attached to the Galilean tetrarch Herod Antipas''s household (Antipas held the courtesy title ''king'' in popular usage though Rome had granted him only tetrarch status), not a Roman military centurion. Keener treats this as the single clearest distinguishing detail separating John''s petitioner from the Synoptic centurion of Matthew 8 and Luke 7.' }
'royal-official-centurion-doublet-debate' = @{ title='One healing story, or two? A live, unsettled question'; body='Compare Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 4:46-54, with D. A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 4:46-54. Both stories involve a Capernaum-linked petitioner asking Jesus, at a distance, to heal a desperately ill dependent, with a healing accomplished by word alone and confirmed only afterward — similarities substantial enough that Brown, following Bultmann''s source-critical tradition, treats the accounts as likely two independent developments of one underlying healing tradition. Carson argues the differences are just as substantial and irreducible: petitioner and social status (a Herodian courtier versus a Gentile Roman officer credited with building the local synagogue), patient (a son versus a slave), geography (Jesus healing from Cana toward Capernaum versus Jesus already in or near Capernaum), and the centurion''s distinctive remark about authority, entirely absent from John. Mainstream scholarship remains genuinely divided between reading these as one tradition or two, and nothing in either text forces a verdict either way.' }
'second-sign-signs-source-hypothesis' = @{ title='A Gospel that counts its own miracles'; body='Robert T. Fortna, The Gospel of Signs: A Reconstruction of the Narrative Source Underlying the Fourth Gospel, Society for New Testament Studies Monograph Series 11 (Cambridge: Cambridge University Press, 1970); see also Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Philadelphia: Westminster Press, 1971), Introduction. John explicitly numbers this healing ''the second sign that Jesus did'' (4:54), echoing his equally explicit numbering of the Cana wine miracle as the first (2:11), an internal counting system found nowhere else in the canonical Gospels. Fortna and, earlier, Bultmann argued this numbering is a surviving seam from an underlying miracle-list source, a hypothetical Signs Source, that John''s author incorporated and expanded rather than composed from scratch; critics of the theory note the numbering could equally reflect the final author''s own narrative bookkeeping rather than a preserved editorial trace of a separate earlier document. The question remains open, but the numbering itself, unusual, deliberate, and internally consistent, is a genuine data point either way.' }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
John reports that Jesus, learning that the Pharisees had heard "Jesus was making and baptizing more disciples than John" — though the narrator immediately corrects the record, adding that in fact "Jesus himself did not baptize, but only his disciples" — leaves Judea for Galilee, and "had to pass through Samaria" to get there (4:1-4). Some early manuscripts open this sentence naming "the Lord" rather than "Jesus" as its subject, a small textual fork worth flagging before the chapter's larger disputes even begin [[NOTE:john-4-1-jesus-lord-textual-variant]]. He stops at a plot of ground near a town called Sychar, land tradition says Jacob gave to his son Joseph, and sits down at "Jacob's well," worn out from the journey, "about the sixth hour." A Samaritan woman comes to draw water; Jesus asks her for a drink, a request that visibly startles her, since, the narrator adds in a parenthetical aside, "Jews have no dealings with Samaritans." What follows is one of John's longest continuous dialogues: Jesus offers "living water" that becomes in the one who drinks it "a spring of water welling up to eternal life," and the woman asks for it, half in irony and half in earnest, while the conversation pivots to harder ground (4:5-15).

Start with where this happens, because for once a Gospel scene sits on genuinely stable geography. Jacob's Well has been shown to essentially the same deep, bedrock-cut shaft at Balata, on the edge of Nablus, since at least the fourth century CE — a well is a fixed feature of terrain in a way a building or a tomb is not, which makes this one of the more secure site identifications in the whole Gospel record [[NOTE:jacobs-well-location-balata]]. Sychar itself is the harder call: it may simply be another name for the largely abandoned site of ancient Shechem next door, or the separate, still-inhabited village of 'Askar nearby; the well's fixed location narrows the search without settling it [[NOTE:sychar-identification-debate]].

The offer of "living water" is doing more theological work than the plain image of flowing water suggests. Jewish wisdom literature already pictured Wisdom herself as a fountain or overflowing spring — Sirach's Wisdom promises that those who drink of her will only thirst for more, and Baruch rebukes Israel for having forsaken "the fountain of wisdom" — so that Jesus's promise of water that permanently satisfies reads, to an audience steeped in that literature, as a claim to supply what only Torah and reverence for God had previously been said to supply [[NOTE:living-water-wisdom-literature-background]].

Then there is the narrator's own aside, and it deserves a "wait, actually." The claim that "Jews have no dealings with Samaritans" is missing from a handful of early manuscripts, including Codex Sinaiticus in its first hand — textual critics lean toward reading it as original but flag their own confidence as only moderate [[NOTE:john-4-9-jews-samaritans-clause-variant]]. And even read as original, the claim is broader than the historical record supports. Josephus, writing as a contemporary, records real bloodshed between Galilean pilgrims and Samaritan villagers under the procurator Cumanus — but the same historian also describes ordinary travel, trade, and intermarriage across that same boundary in other periods. "No dealings" captures a real social distance and periodic violence; it overstates a uniform, total separation that the fuller record does not actually show [[NOTE:josephus-cumanus-samaritan-galilean-clash]].
'@

$beat2 = @'
Jesus tells the woman to call her husband, and when she answers that she has none, he tells her she has spoken truly: she has had five husbands, and the man she now lives with is not her husband. Startled, she calls him a prophet and pivots immediately to the region's live theological dispute — "our fathers worshiped on this mountain," meaning Gerizim, rising visibly nearby, "but you say that in Jerusalem is the place where people ought to worship." Jesus answers that a time is coming when true worshipers will worship the Father "neither on this mountain nor in Jerusalem," but "in spirit and truth," adding that "salvation is from the Jews." The woman says she knows Messiah is coming; Jesus tells her, "I who speak to you am he" — one of his most direct self-disclosures anywhere in the Gospels, and made, remarkably, to a Samaritan woman rather than to any Jewish authority. The disciples return, surprised to find him talking with her; she leaves her water jar behind and runs to tell the town (4:16-30).

The five husbands have been read two ways, and mainstream scholarship does not force a choice between them. One reading treats the detail as biography: whatever her actual history, a woman who has had five husbands and now lives with a sixth man outside marriage is drawn as someone whose life does not fit polite categories, which sets up the moral surprise of Jesus's ready acceptance of her. The other, older reading treats the five husbands as an allegory for 2 Kings 17's report that the Assyrians resettled Samaria with people from five foreign nations, each importing its own god — five gods for five peoples, echoed in five husbands for one woman, with her present, unmarried partner standing for an ongoing, illegitimate cultic loyalty. The allegorical reading is judged ingenious but unproven even by scholars who lay it out carefully; most modern commentators favor the individual, biographical reading, but the text itself does not adjudicate between them [[NOTE:five-husbands-allegory-debate]].

The mountain she names is not rhetorical. Mount Gerizim held a genuine Samaritan temple and associated city, confirmed by twentieth-century excavation, with building phases running from the Persian period into the Hellenistic era [[NOTE:mount-gerizim-temple-archaeology]] — and the Samaritan Pentateuch's own text of Deuteronomy 27:4 names Gerizim, not the Masoretic text's Ebal, as the mountain where Israel's altar belonged, a one-word variant each community's own scripture wields for its side of the argument [[NOTE:samaritan-pentateuch-gerizim-variant]]. What the woman does not mention, and Jesus does not either, is that by this point Gerizim's temple was already a ruin: the Hasmonean ruler John Hyrcanus I had destroyed it roughly a century and a half earlier, a fact known from Josephus rather than from anything in John's own text [[NOTE:josephus-gerizim-temple-destruction]]. The dispute she raises, in other words, concerns a temple that no longer stood.

Jesus's answer — that true worship happens neither on this mountain nor in Jerusalem but "in spirit and truth" — is read by much of the commentary tradition as programmatic for the Fourth Gospel as a whole: a recurring argument, made again and again across the book, that the locus of true worship has shifted away from any physical sanctuary and onto Jesus's own person. On that reading, this answer settles not only a local Samaritan-Jewish quarrel but restates John's larger claim about the Jerusalem Temple's superseded status [[NOTE:worship-spirit-truth-theology]]. Older still is the schism this whole exchange sits on top of: a split whose roots reach back to the Assyrian resettlement of the northern kingdom in 722 BCE and that hardened, over centuries, through the Persian and Hellenistic periods into the rival sanctuary claims voiced here [[NOTE:samaritan-jewish-schism-history]].
'@

$beat3 = @'
While the disciples urge Jesus to eat something they've brought back from town, he tells them, "I have food to eat that you do not know about," and when they wonder aloud whether someone else has fed him, he explains: "My food is to do the will of him who sent me and to accomplish his work." He turns their attention to the fields, ready for harvest ahead of season, reading the moment itself as a harvest of souls rather than of grain. Meanwhile, the Samaritan woman's testimony brings "many Samaritans from that town" to believe in Jesus on her word alone; when they meet him directly and ask him to stay, he remains two days, and by the end of it the townspeople tell the woman that they no longer believe merely because of what she said — "for we have heard for ourselves, and we know that this is indeed the Savior of the world" (4:31-42).

That belief, arriving in two stages — first secondhand testimony, then direct encounter — reads as a small preview of a much larger, better-documented Samaritan mission. Acts 8 independently narrates Philip preaching in Samaria after the Jerusalem church's dispersal, with Peter and John following up from Jerusalem to confirm the new believers; John's Gospel narrates, in miniature and in advance of that later history, essentially the same movement of Samaritans toward the Jesus movement [[NOTE:samaritan-belief-acts8-mission-parallel]].

The title the townspeople land on is worth pausing over, because John's Gospel uses it exactly once. "Savior of the world" appears nowhere else across the Fourth Gospel's twenty-one chapters; its only other New Testament use is 1 John 4:14. That it is placed here in Samaritan mouths, rather than in the mouths of Jewish disciples who have spent three chapters watching Jesus's signs, reads as a deliberate signal: the title's full, universal scope is recognized first by people standing outside the very Jewish-Samaritan quarrel the chapter has just spent thirty verses narrating [[NOTE:savior-of-the-world-title]].
'@

$beat4 = @'
After the two days, Jesus continues on to Galilee, where the Galileans welcome him — they had seen what he did at the Passover festival in Jerusalem — and he comes again to Cana, where he had turned water into wine. There, a royal official whose son lies sick at Capernaum, having heard Jesus had arrived, comes and begs him to come down and heal the boy before he dies. Jesus tells him, "Unless you see signs and wonders you will not believe," but the official presses again — "Sir, come down before my child dies" — and Jesus simply tells him, "Go; your son will live." The man believes the word Jesus spoke and starts for home; on the way, his servants meet him with the news that the boy is recovering, and when he asks the hour, it turns out to be the exact hour Jesus had spoken. He and his whole household believe. John closes the episode by counting it: "This was now the second sign that Jesus did when he had come from Judea to Galilee" (4:43-54).

The petitioner's title matters more than it might seem. John's Greek calls him a basilikos — "royal," or "belonging to the king" — almost certainly an officer or courtier of Herod Antipas's Galilean court rather than a Roman military officer; Antipas was popularly called "king" though Rome had granted him only the lesser title of tetrarch. That single detail is the clearest thing separating this man from the Synoptic centurion whose servant Jesus heals in Matthew 8 and Luke 7 [[NOTE:royal-official-basilikos-identity]].

Whether it is, underneath the difference in rank, the same underlying story told twice is a genuinely live and unsettled question, not a dispute where mainstream scholarship has quietly picked a winner. The similarities are real: a Capernaum-linked petitioner begging Jesus, at a distance, to heal a desperately ill dependent, and a healing accomplished by word alone, confirmed only afterward by messengers. Some scholars, following an older source-critical tradition, read the two accounts as independent developments of one shared healing tradition passed down through different channels. Others weigh the differences as just as substantial: royal courtier against Roman centurion, a son against a slave, Cana-to-Capernaum against Jesus already at or near Capernaum, and the centurion's distinctive line about military authority and command, entirely absent from John. Both positions are defended by serious commentators, and nothing in either text supplies the missing evidence that would settle it [[NOTE:royal-official-centurion-doublet-debate]].

The chapter's closing arithmetic is its own kind of clue. John counts the Cana wine miracle as "the first of his signs" back in 2:11 and now flags this healing explicitly as "the second sign" — a habit of numbering found nowhere else across the canonical Gospels. Source critics have taken that numbering as a seam left behind from an earlier, underlying list of miracles that John's author incorporated rather than composed outright, a hypothetical document usually called a Signs Source; skeptics of the theory note the same numbering could just as easily be the final author's own narrative bookkeeping. Either way, a Gospel that counts its own miracles out loud, twice, in its first four chapters, is doing something no other Gospel does [[NOTE:second-sign-signs-source-hypothesis]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'JACOB''S WELL' = 'The deep, rock-cut well near Sychar where Jesus meets the Samaritan woman (4:5-6), on land traditionally associated with the plot Jacob gave his son Joseph (Genesis 33:19; 48:22; Joshua 24:32). Unusually for a Gospel site, this well has been shown to the same physical shaft at Balata, on the eastern edge of Nablus, since at least the fourth century CE, and now sits beneath a Greek Orthodox monastery church [[NOTE:jacobs-well-location-balata]].'
'SYCHAR' = 'The Samaritan town near Jacob''s well where this chapter''s events unfold (4:5), home to the townspeople who come to believe in Jesus after the woman''s testimony (4:39-42). Whether Sychar is simply another name for the largely abandoned site of ancient Shechem or the separate, still-inhabited village of ''Askar nearby is not settled by the available evidence [[NOTE:sychar-identification-debate]].'
'SAMARIA' = 'The central hill-country region between Galilee and Judea, home to the Samaritan community and the territory Jesus and his disciples pass through on their return to Galilee (4:4). Its own sanctuary tradition, centered on Mount Gerizim, stood in longstanding rivalry with Jerusalem''s Temple claim [[NOTE:samaritan-jewish-schism-history]].'
'SAMARITANS' = 'The ethnic-religious community, centered on Shechem/Nablus and Mount Gerizim, that the woman at the well and the townspeople of Sychar belong to in this chapter. Their split from Jerusalem-centered Judaism has roots reaching back to the Assyrian resettlement of the northern kingdom in 722 BCE and hardened over the Persian and Hellenistic periods [[NOTE:samaritan-jewish-schism-history]]. John''s aside that Jews and Samaritans have no dealings with one another (4:9) reflects real social distance and periodic violence but overstates a uniform, total separation against the fuller archaeological and Josephan record [[NOTE:josephus-cumanus-samaritan-galilean-clash]].'
'SAMARITAN WOMAN AT THE WELL' = 'An unnamed Samaritan woman who meets Jesus at Jacob''s well, engages him in an extended dialogue about living water and worship, and becomes one of the first people in John''s Gospel to hear Jesus openly identify himself as the Messiah (4:26). The text never gives her a name; that detail comes only from later, non-canonical tradition. Her five husbands have been read either as a piece of personal biography or as an allegory for Samaria''s mixed religious history under 2 Kings 17, a debate the text itself does not resolve [[NOTE:five-husbands-allegory-debate]].'
'MOUNT GERIZIM' = 'The Samaritans'' holy mountain overlooking Shechem/Nablus, named by the woman at the well as the place "our fathers worshiped" (4:20) in contrast to Jerusalem. Excavation has confirmed a genuine Samaritan temple and city occupied the summit from the Persian through Hellenistic periods [[NOTE:mount-gerizim-temple-archaeology]], until the Hasmonean ruler John Hyrcanus I destroyed it roughly a century and a half before this scene [[NOTE:josephus-gerizim-temple-destruction]]. The Samaritan Pentateuch''s own text of Deuteronomy 27:4 names this mountain, not Ebal, as the place Israel was commanded to build its altar [[NOTE:samaritan-pentateuch-gerizim-variant]].'
'ROYAL OFFICIAL (CAPERNAUM)' = 'An unnamed officer of Herod Antipas''s court (Greek basilikos, "royal" or "belonging to the king") whose son lies near death at Capernaum. He travels to Cana to beg Jesus for healing, and his son recovers at the exact hour Jesus pronounces him well, without Jesus ever traveling to see him (4:46-54). His court rank, rather than any Roman military office, is the clearest textual distinction between his story and the Synoptic centurion whose servant Jesus heals in a similar way [[NOTE:royal-official-basilikos-identity]] [[NOTE:royal-official-centurion-doublet-debate]].'
'SIGNS SOURCE (JOHANNINE)' = 'A hypothetical earlier document, proposed by source critics including Rudolf Bultmann and reconstructed at length by Robert T. Fortna, imagined as an underlying miracle-list that John''s author incorporated into the finished Gospel. The theory rests substantially on John''s own explicit numbering of its miracles: the Cana wine miracle as "the first of his signs" (2:11) and this chapter''s Capernaum healing as "the second sign" (4:54) [[NOTE:second-sign-signs-source-hypothesis]].'
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
$sortKey = $maxChapterSortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch4NodeId $id $sortKey
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
Seed-Entity "Mount Gerizim" "mount-gerizim" "place" "Samaritan holy mountain overlooking Shechem/Nablus, site of the Samaritans' own temple (destroyed by John Hyrcanus I, c. 128 BCE) and the location the Samaritan woman names in John 4:20 as the proper place of worship."
Seed-Entity "Samaritans" "samaritans" "character" "Ethnic-religious community centered on Mount Gerizim and Shechem/Nablus, holding to its own Pentateuch and temple tradition distinct from Jerusalem Judaism; the community the woman at Jacob's well and the townspeople of Sychar belong to in John 4."
Seed-Entity "Signs Source (Johannine)" "signs-source-johannine" "vocabulary" "Hypothetical earlier miracle-list document proposed by source critics (Bultmann, Fortna) as underlying the Fourth Gospel's numbered signs, based on the explicit numbering of the Cana miracle as the first sign (2:11) and the Capernaum healing as the second (4:54)."

$conn.Close()
Write-Host "DONE Chapter 4."
