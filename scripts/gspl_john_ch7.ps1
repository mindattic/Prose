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
$NotesNodeId = [guid]"019FA96D-7D48-75E0-9BD9-2190171276DC"
$GlossaryNodeId = [guid]"019FA96D-8DD0-70E4-8D98-34AC48833B7E"
$Ch7NodeId = [guid]"019FA96C-804E-712E-AAC7-D41FEF99213F"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
# Guarded against a stray non-numbered row (e.g. a leftover test probe) that would otherwise crash
# LEFT/CHARINDEX parsing: only rows whose text begins with a space-terminated run of digits count.
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'brothers-unbelief-multiple-attestation' = @{ title='A skeptical family, independently attested'; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), discussion of the criteria of authenticity, especially multiple attestation and the criterion of embarrassment. John's flat statement that not even Jesus's brothers believed in him (7:5) surfaces independently in Mark's own account of Jesus's relatives setting out to restrain him because people were saying he was out of his mind (Mark 3:21, 31-35, with parallel material in Matthew 12:46-50 and Luke 8:19-21). A detail this unflattering to the movement's own founding family, attested across sources with different literary relationships to one another, is exactly the kind of evidence historical-critical scholars weigh as more likely early and authentic than invented." }
'james-jerusalem-leader-trajectory' = @{ title='From skeptic to pillar: the brother who changed his mind'; body="Galatians 1:19, where Paul names James, the Lord's brother, as a Jerusalem church leader within roughly two decades of the crucifixion; Flavius Josephus, Jewish Antiquities, Book 20, chapter 9, section 1 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press), recording that a man identified as the brother of Jesus, who was called Christ, James by name, was stoned to death around 62 CE at the instigation of the high priest Ananus. Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday, 1966), commentary ad loc. John 7:5, notes the real historical trajectory this implies: the same brother John here describes as an unbeliever becomes, within a generation, the most prominent leader of the Jerusalem church, a change of mind the Gospels themselves never narrate." }
'hoi-ioudaioi-translation-note' = @{ title='The Jews in John: a phrase that needs unpacking'; body="Urban C. von Wahlde, The Gospel and Letters of John, Volume 2: Commentary on the Gospel of John (Grand Rapids: Eerdmans, 2010), commentary on John's recurring use of the Greek phrase hoi Ioudaioi, here rendered in the fear of the Jews that keeps the Tabernacles crowd from speaking openly about Jesus (7:13). Von Wahlde, alongside Raymond E. Brown's Anchor Bible commentary, argues that in Johannine usage this phrase most often designates the Jerusalem-based religious authorities hostile to Jesus rather than the Jewish people as a whole, a distinction modern translations and readers can easily lose given English's single blanket rendering." }
'feast-of-tabernacles-historical-practice' = @{ title='Sukkot in the first century: booths, branches, and pilgrimage'; body="Mishnah, Sukkah, especially chapters 3-4 (trans. Herbert Danby, The Mishnah, Oxford: Oxford University Press, 1933); Flavius Josephus, Jewish Antiquities, Book 3, section 245 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press). The Feast of Tabernacles (Sukkot) was a seven-day autumn pilgrimage festival commemorating Israel's wilderness wandering through week-long booth-dwelling, carrying the lulav (palm, myrtle, and willow branches bound with a citron), and nightly illumination and dancing in the Temple's Court of the Women. John's account of Jesus teaching about the middle of the feast (7:14) and again on its last day, the great day (7:37) presupposes an audience already familiar with this festival calendar in a way the text itself never stops to explain." }
'john-chronology-three-passovers' = @{ title='Three Passovers, one Tabernacles: a longer ministry than the Synoptics suggest'; body="C. K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), and D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), both in their introductory discussions of Johannine chronology. John's narrative marks three separate Passovers (2:13; 6:4; and the Passover of the Passion, 11:55-13:1), with this Feast of Tabernacles falling between the second and third, implying a public ministry spanning roughly two to three years. The Synoptic Gospels, by contrast, narrate a single festal journey to Jerusalem and one Passover, a structure many readers take at face value as a one-year ministry; historical-critical scholarship generally treats John's multi-festival scaffolding as the more likely reflection of the actual span of time, even though the Synoptics never explicitly rule out additional, unnarrated Jerusalem visits." }
'temple-teaching-without-formal-training' = @{ title="An untrained teacher, and the crowd's surprise"; body="Craig S. Keener, The Gospel of John: A Commentary, Volume 1 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 7:15. Keener situates the crowd's astonishment that Jesus has learning without having studied against the ancient expectation that authoritative teaching required formal discipleship under a recognized master, the same expectation behind the wonder later directed at Peter and John in Acts 4:13, described there as uneducated, common men. The objection assumes a known social pathway to teaching authority that Jesus, on this telling, simply bypassed." }
'messianic-hidden-origin-tradition' = @{ title='A Messiah with no known address'; body="D. A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 7:27. Carson traces the crowd's objection that they know where Jesus is from, while no one will know where the Christ comes from when he appears, to a strand of Jewish messianic expectation attested later in Justin Martyr's Dialogue with Trypho (chapter 8) and the Babylonian Talmud (Sanhedrin 98b), which held that the Messiah would remain hidden and unrecognized until his sudden, unannounced appearance. John never confirms or denies the tradition itself, only that some in the crowd held it and used it to dismiss Jesus's claim." }
'bethlehem-objection-unresolved' = @{ title="The Bethlehem objection John never answers"; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday, 1966), commentary ad loc. John 7:41-42. Some in the crowd object that scripture requires the Christ to descend from David and come from Bethlehem, David's own village, and John's narrative simply moves on, never supplying the Bethlehem-birth answer that Matthew (2:1-6) and Luke (2:1-7) both build entire infancy narratives around. Brown treats this as a genuinely notable authorial choice: either John assumes his audience already knows the Bethlehem tradition and needs no reminder, or he is simply uninterested in resolving an objection his Gospel's larger argument, belief based on Jesus's works and identity rather than his birth record, does not require him to settle." }
'micah-5-2-primary-citation' = @{ title='The prophecy behind the objection'; body="Micah 5:2 (RSV): 'But you, O Bethlehem Ephrathah, who are too little to be among the clans of Judah, from you shall come forth for me one who is to be ruler in Israel, whose origin is from of old, from ancient days.' This eighth-century BCE oracle is the specific scriptural basis the Tabernacles crowd invokes in John 7:42 when objecting that the Christ cannot come from Galilee; the passage names Bethlehem as a place of origin, while John's Jesus is known publicly only as a Galilean." }
'water-libation-ceremony' = @{ title="Drawing water from Siloam: the ceremony behind living water"; body="Mishnah, Sukkah 4:9-10 (trans. Herbert Danby, The Mishnah, Oxford: Oxford University Press, 1933), describing the water-libation ceremony (Hebrew: nisuch ha-mayim) performed each morning of the Feast of Tabernacles: a priest processed to the Pool of Siloam, filled a golden flask, and carried it back to the Temple to be poured out at the base of the altar alongside the wine libation, accompanied by flute music and celebration so joyful the Mishnah elsewhere calls it the model for all rejoicing. Mainstream commentary, see Raymond E. Brown, Anchor Bible vol. 29, and Craig S. Keener, The Gospel of John, Volume 1, both ad loc. John 7:37-39, reads Jesus's cry that whoever thirsts should come to him and drink as a direct, concrete response to this ongoing ritual rather than a free-floating metaphor detached from its festival setting." }
'great-day-of-feast-timing' = @{ title="Which day was the great day?"; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday, 1966), commentary ad loc. John 7:37. Commentators are not fully agreed on which day John means by the last day, the great day of the feast: the seventh day, later called Hoshana Rabbah in rabbinic tradition, when the water-libation and willow-branch processions reached their most elaborate form, is the more commonly proposed candidate, though some argue for the added eighth day (Shemini Atzeret), a solemn assembly on which the water rite was not performed at all. This is a live ambiguity in reconstructing the festival calendar rather than a settled point." }
'living-water-spirit-not-yet-given' = @{ title='Whose heart? A punctuation crux, not a copying error'; body="Nestle-Aland, Novum Testamentum Graece, 28th ed., apparatus and paragraphing ad loc. John 7:37-38; discussed at length in Raymond E. Brown, Anchor Bible vol. 29, and C. K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), both ad loc. Ancient Greek manuscripts carried no punctuation, and John 7:37-38 can be divided two different ways: the traditional reading makes the believer's own heart the source of the rivers of living water, while an equally grammatical alternative places the sentence break after let him drink, making Jesus's own body the source instead. This is a genuine, still-debated interpretive crux rather than a manuscript variant in the ordinary sense, and it changes what the verse is actually claiming." }
'temple-guard-officers' = @{ title='Who were the officers?'; body="E. P. Sanders, Judaism: Practice and Belief, 63 BCE-66 CE (London: SCM Press; Philadelphia: Trinity Press International, 1992), discussion of Temple administration and personnel. The officers (Greek hyperetai) whom the chief priests and Pharisees dispatch to arrest Jesus (7:32, 45) were very likely members of the Temple's own Levitical guard, a police force under high-priestly authority responsible for order in the Temple precincts, not Roman soldiers and not acting on any Roman legal authority. That distinction matters for how the scene's failed arrest and Nicodemus's due-process objection read against actual first-century Jewish legal procedure." }
'bultmann-johannine-irony-misunderstanding' = @{ title='Truth spoken by the wrong mouth'; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Oxford: Basil Blackwell, 1971), commentary ad loc. John 7:45-46. Bultmann's classic treatment of Johannine style identifies a recurring pattern in which a minor or hostile character speaks a line that is truer than the character himself realizes, here the arresting officers returning empty-handed with the report that no one ever spoke like this man, a device Bultmann reads as central to the Gospel's ironic, layered narration rather than as incidental color." }
'nicodemus-three-appearances-arc' = @{ title='Nicodemus, in three acts'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday, 1966), introductory discussion of Nicodemus; D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), commentary ad loc. 7:50-51. Both commentators note that Nicodemus appears at exactly three points in John's narrative, as a cautious night-time questioner (3:1-21), as a lone, procedurally framed defender here (7:50-52), and finally as an open participant in Jesus's burial (19:39-40), and that the three scenes read as a deliberately staged, incremental arc rather than three unconnected cameos, moving him from private curiosity toward public, if still restrained, association with Jesus." }
'jonah-galilee-prophet-irony' = @{ title='A prophet did arise from Galilee'; body="D. A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 7:52. The Pharisees' retort to Nicodemus, that no prophet arises from Galilee, does not hold up against their own scriptures: the eighth-century BCE prophet Jonah is explicitly identified in 2 Kings 14:25 as coming from Gath-hepher, a town in Galilee. Whether John intends readers to catch this as a pointed irony against the Pharisees' scriptural sloppiness, or whether it is simply an overstated debating point of the kind people make in an argument, the objection itself is demonstrably false as scripture." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
After this, Jesus goes about in Galilee, avoiding Judea because "the Jews were seeking to kill him" (7:1), as the Feast of Tabernacles, the great autumn pilgrimage feast, draws near. His brothers press him to leave for Judea and put his works on display there, arguing that "no one works in secret if he seeks to be known openly" (7:4) — and the narrator adds, without comment, that "not even his brothers believed in him" (7:5). Jesus tells them his time has not yet come, lets them go up to the feast first, and then goes up himself "not publicly but in private" (7:10). At the feast, the crowds are already arguing about him before he arrives — some call him "a good man," others say "he is deceiving the people" — but "for fear of the Jews no one spoke openly of him" (7:11-13).

The brothers' unbelief is one of the more historically interesting throwaway lines in the Gospel, precisely because it is unflattering to the movement telling its own story. The same detail — Jesus's own family unconvinced, even alarmed, by him — turns up independently in Mark, where his relatives set out to restrain him because people were saying he was "out of his mind" (Mark 3:21, 31-35), a scene neither Matthew's nor Luke's parallel material simply invents out of nothing [[NOTE:brothers-unbelief-multiple-attestation]]. What makes the detail more than a passing character note is where it leads: within a generation, one of those same unbelieving brothers, James, is the recognized head of the Jerusalem church, a leadership Paul takes for granted by name (Galatians 1:19) and Josephus records ending in his execution at the high priest's order — a change of mind the Gospels themselves never bother to narrate [[NOTE:james-jerusalem-leader-trajectory]].

The phrase "fear of the Jews" (7:13) is worth pausing on before reading past it, because John uses "the Jews" as a recurring, loaded shorthand throughout this Gospel, and mainstream commentary generally reads it in contexts like this one as referring specifically to the Jerusalem religious establishment hostile to Jesus, not to the Jewish people as a whole [[NOTE:hoi-ioudaioi-translation-note]]. The feast itself, meanwhile, is never explained for readers who might not already know it: Tabernacles was a week-long autumn pilgrimage festival built around dwelling in temporary booths and a daily water rite at the Temple, practices well attested in the Mishnah and in Josephus but simply assumed here as background knowledge [[NOTE:feast-of-tabernacles-historical-practice]]. Its placement is also a chronological marker worth flagging early: this is the second of three separate Passovers John threads through his narrative (2:13; 6:4; and the Passion's, still to come), with Tabernacles falling between them — a festival calendar the single-year reading of the Synoptic Gospels has no real room for [[NOTE:john-chronology-three-passovers]].

There's a real narrative seam worth naming too, even without a citation behind it: Jesus insists his "time has not yet come" and goes up "in private," yet within the next few verses he is teaching openly in the Temple court in front of the assembled feast crowd (7:14) — a private departure followed by a public arrival. Whether that reflects two originally separate traditions stitched together, or a deliberate narrative rhythm building toward "his hour," the transition from secrecy to open teaching mid-feast is exactly the kind of joint worth noticing rather than smoothing over.
'@

$beat2 = @'
Midway through the festival, Jesus goes up to the Temple and begins to teach, astonishing the crowd, who ask, "How is it that this man has learning, when he has never studied?" (7:15). Jesus answers that his teaching is not his own but comes from the one who sent him, and points back to the Sabbath healing at Bethesda — the man made well all over on the Sabbath — asking why they're angry at a whole man healed on the day of rest when circumcision itself, by law, can override the Sabbath (7:16-24). Some in the crowd wonder aloud whether the authorities secretly know he really is the Christ, since here he is speaking openly and no one moves against him — "But we know where this man comes from," they add, "and when the Christ appears, no one will know where he comes from" (7:25-27). Jesus cries out in response that he does come from the one who sent him, whom they do not know; the attempt to seize him fails because "his hour had not yet come" (7:30), and meanwhile many in the crowd believe, asking whether the Christ could really do more signs than this man has already done (7:31). When the Pharisees hear the crowd's muttering, they and the chief priests send officers to arrest him; Jesus tells the crowd he will be with them only a little longer before going to the one who sent him, "where you cannot come" — leaving them to wonder, in a passage that never quite gets an answer, whether he means to go teach among the Greeks of the Dispersion (7:32-36).

The crowd's surprise at an untrained teacher isn't a throwaway detail; ancient teaching authority normally ran through a recognized chain of discipleship under an established master, the same expectation that later makes the Jerusalem council marvel that the "uneducated, common" Peter and John can speak with such confidence (Acts 4:13). Jesus's claim to teach without having gone through that visible pathway is precisely what needs an answer, and the answer he gives — that the teaching's real source is God, not a human teacher — reframes the objection rather than denying it [[NOTE:temple-teaching-without-formal-training]].

The crowd's back-and-forth about where the Christ is supposed to come from reflects a real, attested strand of Jewish messianic expectation: that the Messiah's origin would remain hidden and unknown until the moment of his sudden appearance, a tradition surfacing later in both Christian and rabbinic sources. Read against that expectation, "we know where this man comes from" functions as a disqualifying objection on its own terms, not an idle detail — this crowd knows Jesus is a known quantity from a known town, and for at least some of them that alone rules him out [[NOTE:messianic-hidden-origin-tradition]].

The officers sent to make the arrest are worth identifying precisely: not Roman soldiers acting under Roman authority, but members of the Temple's own Levitical guard, a police force operating under high-priestly command inside the Temple precincts. That the failed arrest and the later Sanhedrin debate over Jesus both stay entirely within Jewish legal and administrative structures — no Roman official appears until chapter 18 — is a detail the historical-critical reading takes seriously when reconstructing how an arrest like this would actually have worked [[NOTE:temple-guard-officers]].
'@

$beat3 = @'
On the last day of the feast, "the great day," Jesus stands and cries out that anyone who thirsts should come to him and drink, and that "out of his heart will flow rivers of living water" for whoever believes in him — a saying the narrator immediately glosses as referring to the Spirit believers would later receive, "for as yet the Spirit had not been given, because Jesus was not yet glorified" (7:37-39). The crowd splits over him: some call him "the Prophet," others "the Christ," and still others object that the Christ cannot come from Galilee, since scripture says he must descend from David and come from Bethlehem, David's own village (7:40-42) — a division sharp enough that some want him seized, though again no one lays a hand on him (7:43-44). The officers sent earlier return to the chief priests and Pharisees without their prisoner; asked why, they answer simply, "No one ever spoke like this man!" (7:45-46). The Pharisees answer with contempt — has any of the authorities believed in him? this crowd that doesn't know the Law is accursed (7:47-49) — until Nicodemus, "who had gone to him before," objects that their own law doesn't condemn a man without first giving him a hearing (7:50-51). Their reply closes the chapter: "Are you from Galilee too? Search and you will see that no prophet arises from Galilee" (7:52).

The "living water" saying is not free-floating imagery; it lands on the single day of the festival year when its audience would have just watched, or taken part in, the water-libation ceremony that was the ritual centerpiece of Tabernacles — a priest processing to the Pool of Siloam each morning to draw water and pour it out at the Temple altar, a rite documented in detail in the Mishnah and joyful enough that later rabbinic memory calls its accompanying celebration the model for all rejoicing. Reading Jesus's cry against that concrete, ongoing ritual, rather than as an isolated metaphor, is what mainstream commentary treats as the correct historical context for the verse [[NOTE:water-libation-ceremony]]. Exactly which day counts as "the great day," however, is itself a small unresolved question in reconstructing the festival calendar, with the water rite's own seventh-day climax and the separate, water-rite-free eighth day both defended by different commentators [[NOTE:great-day-of-feast-timing]].

The saying carries a second, quieter difficulty that has nothing to do with manuscript copying and everything to do with the fact that ancient Greek had no punctuation: depending on where the sentence break falls, "out of his heart will flow rivers of living water" can describe either the believer's own heart or Jesus's own body as the water's source. Both readings are grammatically defensible, both have real commentary support, and the choice between them changes what the verse is actually claiming about where living water comes from [[NOTE:living-water-spirit-not-yet-given]].

The crowd's Bethlehem objection deserves its own pause, because John's Gospel never answers it. Micah's prophecy that a ruler would come from Bethlehem, David's own town, is real and specific [[NOTE:micah-5-2-primary-citation]], and Matthew and Luke both build entire birth narratives around satisfying exactly this expectation. John does neither — he simply lets the objection stand unresolved, either trusting his readers already know the Bethlehem tradition from elsewhere or simply uninterested in settling a question his Gospel's larger argument doesn't require him to answer [[NOTE:bethlehem-objection-unresolved]].

Two literary observations round out the scene. The temple officers' report — "no one ever spoke like this man" — is a favorite example in Johannine scholarship of truth spoken by exactly the wrong mouth: minor, even hostile characters voicing something truer than they themselves grasp, a pattern this Gospel returns to again and again [[NOTE:bultmann-johannine-irony-misunderstanding]]. And Nicodemus's brief, careful objection here is the second of exactly three appearances he makes in John — night-time questioner in chapter 3, cautious procedural defender here, open participant at Jesus's burial in chapter 19 — an arc that reads as deliberately staged rather than three unrelated cameos [[NOTE:nicodemus-three-appearances-arc]]. As for the Pharisees' closing claim that "no prophet arises from Galilee" — it doesn't survive contact with their own scriptures: Jonah, of the whale, is explicitly a Galilean, from Gath-hepher (2 Kings 14:25), a detail that makes their parting shot demonstrably wrong on its own terms [[NOTE:jonah-galilee-prophet-irony]].
'@

$beats = @($beat1, $beat2, $beat3)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'FEAST OF TABERNACLES (SUKKOT)' = "The autumn pilgrimage festival during which the events of this chapter take place, commemorating Israel's wilderness wandering through week-long booth-dwelling, branch-carrying, and a daily water-libation ceremony centered on the Temple altar (7:2, 14, 37). See [[NOTE:feast-of-tabernacles-historical-practice]] for the festival's attested first-century practices and [[NOTE:water-libation-ceremony]] for the specific ritual behind Jesus's living water saying."
'WATER-LIBATION CEREMONY (NISUCH HA-MAYIM)' = "The daily Sukkot rite in which a priest drew water from the Pool of Siloam and poured it at the Temple altar alongside the wine libation, attested in the Mishnah tractate Sukkah and read by mainstream commentary as the concrete ritual backdrop for Jesus's rivers of living water declaration on the festival's last day (7:37-39) [[NOTE:water-libation-ceremony]]."
'POOL OF SILOAM' = "A spring-fed pool in the southeastern part of first-century Jerusalem, the water source drawn from each morning of the Feast of Tabernacles for the water-libation ceremony this chapter's living water saying presupposes (7:37-39) [[NOTE:water-libation-ceremony]]. The same pool later serves as the site of a healing in John 9."
'GREAT DAY OF THE FEAST (HOSHANA RABBAH)' = "John's designation for the last day, the great day of the feast (7:37), on which Jesus makes his living water declaration; likely the seventh day of Tabernacles, later known in rabbinic tradition as Hoshana Rabbah, though the added eighth day remains a live alternative in the commentary tradition [[NOTE:great-day-of-feast-timing]]."
'MICAH (PROPHET)' = "An eighth-century BCE Judean prophet whose oracle naming Bethlehem as the origin of a future ruler (Micah 5:2) supplies the scriptural objection some in the Tabernacles crowd raise against Jesus's messianic claim, since he is known publicly as a Galilean (7:41-42) [[NOTE:micah-5-2-primary-citation]] [[NOTE:bethlehem-objection-unresolved]]."
'JAMES, BROTHER OF JESUS' = "One of the brothers who, per John's flat statement, did not believe in him at the time of this chapter's events (7:5) [[NOTE:brothers-unbelief-multiple-attestation]]. Within roughly two decades this same brother becomes a recognized leader of the Jerusalem church (Galatians 1:19) and is later executed there at the high priest's instigation, per Josephus [[NOTE:james-jerusalem-leader-trajectory]] — a change of mind John's Gospel itself never narrates."
'TEMPLE OFFICERS (HYPERETAI)' = "Members of the Temple's Levitical guard, dispatched twice in this chapter by the chief priests and Pharisees to arrest Jesus (7:32, 45) and returning both times without him, the second time reporting only that no one ever spoke like this man [[NOTE:temple-guard-officers]]."
'THE JEWS (HOI IOUDAIOI) IN JOHN' = "John's recurring phrase the Jews, Greek hoi Ioudaioi, used here of the crowd's fear of speaking openly about Jesus (7:13), most often designates the Jerusalem-based religious authorities hostile to him rather than the Jewish people as a whole, a distinction the commentary tradition treats as important context for reading the term accurately across the Gospel [[NOTE:hoi-ioudaioi-translation-note]]."
'RIVERS OF LIVING WATER (JOHN 7:37-39)' = "Jesus's declaration on the festival's last day that out of his heart will flow rivers of living water, identified by the narrator as a reference to the Spirit believers would later receive, since the Spirit had not yet been given because Jesus was not yet glorified (7:39). The saying is anchored in the concrete water-libation ceremony of the feast rather than a free-floating metaphor [[NOTE:water-libation-ceremony]], and its exact grammatical subject, the believer's heart or Jesus's own, remains a genuine, debated crux [[NOTE:living-water-spirit-not-yet-given]]."
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
    Add-BeatNode $Ch7NodeId $id $sortKey
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
Seed-Entity "Feast of Tabernacles (Sukkot)" "feast-of-tabernacles-sukkot" "vocabulary" "Seven-day autumn pilgrimage festival commemorating the wilderness wandering; setting of John 7, with a daily water-libation ceremony as ritual centerpiece."
Seed-Entity "Water-Libation Ceremony (Nisuch HaMayim)" "water-libation-ceremony-nisuch-hamayim" "vocabulary" "Daily Sukkot rite of drawing water from the Pool of Siloam and pouring it at the Temple altar; the historical backdrop for Jesus's living water saying in John 7:37-39."
Seed-Entity "Micah (prophet)" "micah-prophet" "character" "Eighth-century BCE Judean prophet; his Bethlehem oracle (Micah 5:2) underlies the crowd's messianic-origin objection in John 7:41-42."
Seed-Entity "Temple Officers (Hyperetai)" "temple-officers-hyperetai" "vocabulary" "Members of the Temple's Levitical guard dispatched by the chief priests and Pharisees to arrest Jesus in John 7:32, 45; not Roman soldiers."

$conn.Close()
Write-Host "DONE Chapter 7."
