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
$Ch8NodeId = [guid]"019FA96C-918E-7017-B6C5-0FBFC1087903"
$NotesNodeId = [guid]"019FA96D-7D48-75E0-9BD9-2190171276DC"
$GlossaryNodeId = [guid]"019FA96D-8DD0-70E4-8D98-34AC48833B7E"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND ISNUMERIC(LEFT(b.Text, CHARINDEX(' ',b.Text)-1))=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'pa-p66-p75-absence' = @{ title='Missing from the two oldest papyri'; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), discussion ad loc. John 7:53–8:11. Metzger's textual committee documents that the pericope adulterae is absent from Papyrus 66 (Bodmer II, copied around 200 CE) and Papyrus 75 (Bodmer XIV–XV, early third century) — the two oldest substantially intact manuscripts of John's Gospel, both of which move directly from 7:52 to 8:12 with no gap or marginal note indicating an omission." }
'pa-vaticanus-sinaiticus-absence' = @{ title='Missing from the two oldest complete Bibles'; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), discussion ad loc. John 7:53–8:11; see also the apparatus of Nestle-Aland Novum Testamentum Graece, ad loc. Codex Vaticanus and Codex Sinaiticus, the two great fourth-century majuscule manuscripts that form the backbone of the modern critical text, both omit the passage entirely, again passing straight from 7:52 to 8:12. Several other early manuscripts and versions, including the original hand of a number of Old Latin and Syriac witnesses, likewise show no trace of it." }
'pa-floating-manuscript-location' = @{ title="A passage that can't settle on an address"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), discussion ad loc. John 7:53–8:11; Bart D. Ehrman, Misquoting Jesus: The Story Behind Who Changed the Bible and Why (San Francisco: HarperSanFrancisco, 2005), chapter on the woman taken in adultery. Manuscripts that do include the story disagree sharply on where it belongs: most place it here, after John 7:52, but some instead insert it after John 7:36, others tack it onto the very end of the Gospel after John 21:25, and the family-13 group of minuscules inserts it into an entirely different Gospel, after Luke 21:38. A tradition that migrates between two Gospels and multiple positions within one of them is not behaving like an original, fixed piece of a text's composition." }
'pa-johannine-style-divergence' = @{ title="Prose that doesn't sound like the rest of John"; body="Raymond E. Brown, The Gospel According to John I–XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), appendix discussion of the pericope adulterae following the commentary on chapter 8. Brown catalogues a run of vocabulary and connective particles in John 7:53–8:11 that appear rarely or never elsewhere in the Fourth Gospel — including the very word for `"scribes,`" grammateis, which occurs here and nowhere else in John — while the passage's simple, paratactic narrative style and its concern for scripturally grounded legal procedure read far closer to Luke's narrative manner than to John's own. Style alone cannot date a passage, but it is independent of, and consistent with, the manuscript evidence that the material entered the text from outside." }
'metzger-textual-commentary-verdict' = @{ title="The textual committee's considered judgment"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), discussion ad loc. John 7:53–8:11. Weighing the manuscript, versional, and patristic evidence together, the United Bible Societies' editorial committee concluded that the pericope was not part of the Fourth Gospel as originally composed, while stopping well short of declaring the underlying incident fictional; their judgment concerns transmission history, not the truth-value of the story it tells." }
'ehrman-misquoting-jesus-case-study' = @{ title="Ehrman's flagship textbook example"; body="Bart D. Ehrman, Misquoting Jesus: The Story Behind Who Changed the Bible and Why (San Francisco: HarperSanFrancisco, 2005), chapter on the woman taken in adultery. Ehrman uses this passage as his lead popular-level illustration of how a beloved, familiar Bible story can be demonstrated, on ordinary text-critical grounds, to be a later scribal addition — precisely because the manuscript, versional, and stylistic evidence converge unusually cleanly here, in a way most other disputed readings in the New Testament do not." }
'keith-transmission-vs-historicity' = @{ title='Two separate questions: how the text traveled, and what actually happened'; body="Chris Keith, The Pericope Adulterae, the Gospel of John, and the Literacy of Jesus, Supplements to Novum Testamentum 132 (Leiden: Brill, 2009), especially the introduction and opening chapter. Keith's monograph presses a distinction popular discussion often collapses: the near-unanimous critical verdict that John 7:53–8:11 was not written by the author of the Fourth Gospel is a claim about manuscript transmission and composition history, not a verdict on whether the episode it narrates ever happened. Keith, alongside Metzger, Brown, and scholars across the theological spectrum, treats it as plausible, even likely, that the passage preserves a genuinely ancient tradition about Jesus, quite possibly rooted in a real incident, that simply was not part of John's own text until later scribes and editors folded it in, evidently because it was too well loved or too plainly Jesus-like to let go missing." }
'eusebius-papias-similar-story' = @{ title='A story Papias already knew, a century before it entered John'; body="Eusebius of Caesarea, Ecclesiastical History, Book 3, chapter 39, section 16 (Loeb Classical Library, trans. Kirsopp Lake, Cambridge, MA: Harvard University Press, 1926). Eusebius reports that Papias of Hierapolis, writing in the early second century, knew and related a story concerning a woman accused of many sins before the Lord — a tradition many textual critics take as independent, early external evidence that some version of this episode circulated in Christian memory long before it was ever copied into a manuscript of John's Gospel, reinforcing Keith's point that an ancient origin and a late manuscript arrival are not in tension." }
'mishnah-sukkah-illumination-rite' = @{ title='The night the Temple courtyard lit up'; body="Mishnah, Sukkah 5:2–4 (trans. Herbert Danby, The Mishnah, Oxford: Clarendon Press, 1933). The Mishnah describes the Sukkot illumination ceremony, Simchat Beit HaShoeivah, held in the Temple's Court of Women: four towering golden menorahs, each with four golden bowls reached by ladders, were lit using worn-out priestly garments for wicks, casting light bright enough, the tractate says, to illuminate every courtyard in Jerusalem, while the pious danced before them through the night." }
'brown-light-of-world-sukkot-backdrop' = @{ title="Brown on the festival's living backdrop"; body="Raymond E. Brown, The Gospel According to John I–XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 8:12. Brown reads `"I am the light of the world`" as continuing, not opening, a scene: the Sukkot setting introduced in chapter 7 (`"on the last day, the great day of the feast,`" 7:37) is still running, and Jesus's saying lands against the immediate visual memory of those same great menorahs blazing in the Court of Women, where John locates the discourse at 8:20 — a ritual object turned into a christological claim in the same festival week." }
'ego-eimi-exodus-burning-bush' = @{ title='An echo of the voice from the bush'; body="Raymond E. Brown, The Gospel According to John I–XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), appendix on the Ego Eimi sayings. The Greek Septuagint renders God's self-identification to Moses at the burning bush, `"I am who I am`" (Exodus 3:14), using the same construction, ego eimi, that recurs across John's Gospel in Jesus's absolute, object-less `"I am`" statements, of which the discourse in this chapter (8:24, 8:28, 8:58) supplies the most concentrated cluster in the entire Gospel. Brown treats the echo as deliberate rather than coincidental, arguing John's absolute usage goes well beyond ordinary Greek self-identification." }
'ball-williams-ego-eimi-scholarship' = @{ title='Tracking the divine-name reading through later scholarship'; body="David Mark Ball, 'I Am' in John's Gospel: Literary Function, Background and Theological Implications, Journal for the Study of the New Testament Supplement Series 124 (Sheffield: Sheffield Academic Press, 1996); Catrin H. Williams, I am He: The Interpretation of 'Ani Hu' in Jewish and Early Christian Literature, Wissenschaftliche Untersuchungen zum Neuen Testament 2/113 (Tübingen: Mohr Siebeck, 2000). Ball traces the absolute ego eimi's literary function across the whole Gospel, while Williams broadens the background to include the Hebrew Scriptures' own `"I am He`" (ani hu) self-declarations (Deuteronomy 32:39; Isaiah 43:10); both concur that whatever the precise route, the Fourth Evangelist places on Jesus's lips language reserved elsewhere for God's own self-naming, and that first-century hearers steeped in that idiom would have caught the resonance." }
'carson-two-witnesses-law' = @{ title='Why the crowd keeps asking for witnesses'; body="D. A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids, MI: Wm. B. Eerdmans, 1991), commentary ad loc. John 8:13–18. Carson situates the Pharisees' objection, that Jesus is merely bearing witness about himself and so his testimony is invalid, against the background of the biblical two-or-three-witness rule for establishing a legal matter (Deuteronomy 19:15). Jesus's countermove, naming the Father as a second witness alongside himself, works only within that same Jewish evidentiary framework; the exchange presumes shared legal ground between Jesus and his questioners rather than staging an argument from nowhere." }
'barrett-abraham-descent-dispute' = @{ title='A dispute that runs on ethnicity and freedom, not just theology'; body="C. K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 8:31–59. Barrett reads the escalating exchange over Abrahamic descent, `"we are Abraham's descendants and have never been enslaved to anyone,`" a claim awkward on its face for a nation that had in fact been conquered repeatedly, as reflecting a real, sharpening argument within Second Temple Judaism over who legitimately counts as Abraham's true heir, an argument the Johannine community appears to be refighting in the discourse's late-first-century composition setting as much as narrating a single afternoon's conversation." }
'bultmann-discourse-source-theory' = @{ title="Bultmann's hypothesized source behind the discourses"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Philadelphia: Westminster Press, 1971; German original, Das Evangelium des Johannes, 1941), commentary on the Light of the World discourse. Bultmann proposed that John's long revelatory `"I am`" discourses, including this one, derive from a distinct written source of Gnostic-tinged revelation speeches, his hypothesized Offenbarungsreden, subsequently reworked, reordered, and supplied with narrative settings by the evangelist. Few scholars today accept the source theory's specific Gnostic derivation, but Bultmann's underlying observation, that the discourse material carries its own compositional layer worked over more than once before reaching its present form, remains widely influential." }
'smith-before-abraham-climax' = @{ title="The discourse's highest point"; body="D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), commentary ad loc. John 8:58. Smith identifies `"before Abraham was, I am`" as the discourse's climactic and most theologically loaded line: not merely a claim to precede Abraham in time, but a grammatically unusual present tense standing in place of an expected past tense, read across the commentary tradition as the sharpest of John's ego eimi sayings and the one the crowd's very next action treats as an unmistakable claim to deity." }
'leviticus-24-16-blasphemy-stoning' = @{ title='Why stones, specifically'; body="Craig S. Keener, The Gospel of John: A Commentary, 2 vols. (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 8:58–59. Keener grounds the crowd's sudden move to stone Jesus in Leviticus 24:16, which prescribes death by stoning for anyone who blasphemes the divine Name; read against that statute, `"before Abraham was, I am`" functions in the narrative as exactly the kind of self-identification with God's own Name that the law was written to punish, making the crowd's reaction legally coherent within the world the text depicts rather than a generic mob response." }
'brown-anti-jewish-reading-caution' = @{ title="A verse later weaponized far beyond its own argument"; body="Raymond E. Brown, An Introduction to the Gospel of John, ed. Francis J. Moloney (New York: Doubleday, 2003), discussion of Johannine polemic and its later reception. Brown cautions against reading the chapter's sharp exchange, including the line accusing Jesus's opponents of having `"the devil`" as their father (8:44), as a claim about Jewish people as such; he situates it instead within a pattern of intra-Jewish sectarian polemic common to the period, comparable in tone to some Qumran writing against fellow Jews it judged unfaithful, while noting soberly that the passage was in fact read the other way by later Christian readers, with severe historical consequences." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
John's text as most Bibles print it has the scribes and Pharisees dragging a woman caught in the very act of adultery before Jesus in the Temple, reminding him that Moses commanded such women be stoned, and asking what he says — a trap, the narrator tells us, meant to give them grounds to accuse him. Jesus bends down and writes on the ground with his finger, straightens up only to say "let him who is without sin among you be the first to throw a stone at her," bends down again, and one by one, beginning with the eldest, the accusers leave until only Jesus and the woman remain standing in the emptied crowd. "Woman, where are they? Has no one condemned you?" "No one, Lord." "Neither do I condemn you; go, and from now on sin no more" (7:53-8:11).

Before anything else can be said about this scene, its address needs correcting: for essentially the entire modern critical-scholarly consensus, and for every mainstream translation that prints a bracketed note or italicized warning around it, John 7:53-8:11 was not originally part of the Gospel of John. The case rests on stacked, independent lines of evidence rather than one lone argument. First, manuscripts: the passage is missing from Papyrus 66 and Papyrus 75 [[NOTE:pa-p66-p75-absence]], the two oldest substantially complete copies of John that survive, and it is equally absent from Codex Vaticanus and Codex Sinaiticus [[NOTE:pa-vaticanus-sinaiticus-absence]], the two great fourth-century majuscules that anchor the modern critical text — all four move directly from 7:52 to 8:12 with nothing missing from their own internal sense.

Second, geography: manuscripts that do contain the story cannot agree on where it goes. Most place it here; some place it after John 7:36 instead; others tack it onto the very end of the Gospel after 21:25; and one manuscript family relocates it entirely into a different Gospel, after Luke 21:38 [[NOTE:pa-floating-manuscript-location]]. A passage genuinely original to a text's first composition doesn't wander between two books and four addresses within one of them; that pattern is the signature of a free-floating oral or written tradition being inserted, at different points, by different scribal traditions trying to find it a home.

Third, style: read closely against the rest of John, the Greek itself sounds like it belongs to somebody else. The word John uses here for "scribes" doesn't appear anywhere else in his Gospel, and the passage's plain, connective storytelling manner reads closer to Luke's narrative habits than to John's own [[NOTE:pa-johannine-style-divergence]]. None of this is a single knockout blow on its own — a rare word, a variant location, an absent papyrus — but together, weighed the way the Bible societies' own editorial committee weighs such evidence [[NOTE:metzger-textual-commentary-verdict]], they add up to about as clean a case for a later insertion as exists anywhere in the New Testament, which is exactly why Bart Ehrman uses this passage as his lead popular illustration of how textual criticism actually works [[NOTE:ehrman-misquoting-jesus-case-study]].
'@

$beat2 = @'
None of that manuscript case, though, settles the separate question of whether something like this actually happened in Jesus's ministry — and it is worth being precise about why those are different questions. Chris Keith's monograph on the passage is the standard modern treatment of exactly this distinction: the verdict that John did not write these twelve verses is a claim about transmission and composition history, not a verdict on the underlying event's historicity [[NOTE:keith-transmission-vs-historicity]]. Keith's own reading, shared by scholars well outside the passage's textual-critical specialists, is that the pericope quite plausibly preserves a genuinely early and independent memory of Jesus — a memory that simply was not fixed into John's own text until later copyists, for reasons no one can fully reconstruct, folded a beloved floating story into the Gospel that seemed to fit it best.

There is a real, if thin, external thread supporting that "early and independent" half of the claim. Eusebius, writing his Ecclesiastical History in the early fourth century, reports that Papias of Hierapolis, writing a good two centuries earlier in the first decades of the second century, already knew and told a story about a woman accused of many sins brought before the Lord [[NOTE:eusebius-papias-similar-story]]. That reference cannot be matched word for word against the text as it now stands in John, and it may or may not describe precisely this incident. What it does establish is that some version of this kind of story about Jesus was already circulating in Christian memory a full century or more before any surviving manuscript inserts the pericope adulterae into John at all — which is the strongest evidence available that the tradition's age and its late manuscript arrival are two separate facts, not a contradiction.

One detail invites speculation the text itself refuses to satisfy: what Jesus writes on the ground, twice, is never stated. Guesses stretch back into the early church — a list of the accusers' own sins, a scriptural citation, a doodle without content — but none of it rests on anything beyond conjecture, and the honest answer is that the earliest form of the story, whatever its origin, simply does not tell us.
'@

$beat3 = @'
Whether or not the intervening scene belongs to John's own hand, the text everyone agrees is his resumes at 8:12: Jesus, still in the Temple, declares "I am the light of the world; whoever follows me will not walk in darkness, but will have the light of life." The Pharisees object that he is testifying about himself and so his testimony is worthless; Jesus answers that his testimony stands because the Father testifies with him, and when they ask where his Father is, he tells them they know neither him nor the Father. He warns that he is going away, that they will seek him and die in their sin, and that where he is going they cannot come. The exchange escalates into a dispute over ancestry — "we are the descendants of Abraham," they insist, then, further on, that Abraham is their father — until Jesus tells them Abraham rejoiced to see his day, and they answer that he is not yet fifty years old and has seen Abraham. Jesus replies, "Truly, truly, I say to you, before Abraham was, I am," and the crowd picks up stones to throw at him; he hides himself and leaves the Temple (8:12-59).

The discourse's opening claim lands inside a specific, still-running festival moment rather than a vacuum. Chapter 7 places all of this on Sukkot, the autumn Feast of Tabernacles, and the Mishnah describes an actual illumination ceremony performed at that festival in the Temple's Court of Women: towering golden menorahs lit with worn-out priestly garments for wicks, casting light the tractate says was bright enough to illuminate the whole city, while crowds danced before them through the night [[NOTE:mishnah-sukkah-illumination-rite]]. John situates the Light of the World discourse in that same Court of Women (8:20), and the mainstream commentary reading takes "I am the light of the world" as a direct claim laid against that specific, recently lit ritual backdrop, not a floating metaphor detached from the feast [[NOTE:brown-light-of-world-sukkot-backdrop]].

The phrase "I am" itself carries more freight across this discourse than ordinary self-identification usually does. The Greek Septuagint renders God's self-naming to Moses at the burning bush, "I am who I am," with the same construction, ego eimi, that recurs through John in Jesus's absolute, object-less "I am" statements, and this chapter supplies the densest cluster of them anywhere in the Gospel (8:24, 8:28, 8:58) [[NOTE:ego-eimi-exodus-burning-bush]]. Later scholarship has widened the case beyond Exodus alone: the Hebrew Scriptures' own recurring divine self-declaration "I am He" supplies a second, equally live background, and the accumulated argument across this literature is that first-century Jewish hearers steeped in that idiom would have caught exactly what was being claimed [[NOTE:ball-williams-ego-eimi-scholarship]].

The testimony dispute that opens the chapter is not free-floating rhetorical sparring either: it runs on the biblical rule that a legal claim requires two or three witnesses (Deuteronomy 19:15), which is why Jesus's opponents can dismiss self-testimony as inadmissible and why Jesus's countermove is specifically to name the Father as a second witness rather than simply repeating his claim louder [[NOTE:carson-two-witnesses-law]]. The Abraham argument that follows works the same way: the crowd's claim to have "never been enslaved to anyone" is strange on its face for a people who had in fact been conquered by Babylon, Persia, Greece, and Rome in turn, and the mainstream reading treats the whole exchange as reflecting a real, sharpening late-first-century argument within Judaism over who counts as Abraham's true heir — a debate the Johannine community appears to be refighting through this narrative as much as reporting a single afternoon's conversation [[NOTE:barrett-abraham-descent-dispute]].

None of that argues the discourse is a verbatim transcript. Rudolf Bultmann's classic source-critical proposal held that John's long revelatory "I am" discourses derive from an earlier, once-independent source of revelation speeches, later reworked and given narrative settings by the evangelist; his specific Gnostic derivation for that source persuades few scholars today, but his underlying observation, that this material carries its own compositional history worked over more than once before landing in its present form, remains broadly influential [[NOTE:bultmann-discourse-source-theory]].
'@

$beat4 = @'
The discourse's last exchange is its sharpest. When Jesus tells his questioners that Abraham rejoiced to see his day, they answer with the obvious math problem — he is not yet fifty, and claims to have seen a man dead for the better part of two millennia — and his reply is not tensed the way the objection expects: "before Abraham was, I am," a present tense standing in the exact place ordinary grammar would put a past one. Commentary across the tradition treats this as the discourse's climactic and most theologically loaded line, the sharpest of John's ego eimi sayings, and the one moment the crowd's own next action treats as an unmistakable claim (8:58) [[NOTE:smith-before-abraham-climax]].

Their next action is to reach for stones. That response is not a generic mob eruption; Leviticus 24:16 prescribes death by stoning for anyone who blasphemes God's own Name, and read against that statute, an "I am" placed before Abraham reads within the story's own legal world as exactly the kind of self-identification with the divine Name the law exists to punish — which is what makes the crowd's reaction cohere with the narrative's internal logic rather than reading as an arbitrary escalation [[NOTE:leviticus-24-16-blasphemy-stoning]].

One further caution belongs here, and mainstream scholarship raises it soberly rather than defensively. Earlier in the same exchange, Jesus tells his opponents they have "the devil" as their father (8:44) — language that later Christian readers, across many centuries, lifted out of its own setting and used as a charge against Jewish people as such, with grave historical consequences. The more careful reading situates the line within a pattern of intra-Jewish sectarian polemic common to the period — comparable in temperature, though not in outcome, to some of the Dead Sea Scrolls community's own writing against fellow Jews it judged unfaithful — a dispute over legitimate descent from Abraham conducted in the harsh idiom two Jewish factions used against each other, not a claim about ethnicity as a whole [[NOTE:brown-anti-jewish-reading-caution]]. Reading the line responsibly means holding both halves at once: what it likely meant in its own late-first-century setting, and what it was later made to mean, at terrible cost, once that setting was forgotten.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'PERICOPE ADULTERAE' = "The Latin technical name (literally `"the passage of the adulteress`") scholars use for the story of the woman caught in adultery in John 7:53–8:11, treated in this chapter as a case study in New Testament textual criticism [[NOTE:pa-p66-p75-absence]] [[NOTE:metzger-textual-commentary-verdict]]. The near-universal critical judgment is that it was not part of John's Gospel as originally composed, a conclusion about manuscript transmission that leaves the story's underlying historicity a separate, open question [[NOTE:keith-transmission-vs-historicity]]."
'WOMAN CAUGHT IN ADULTERY' = "The unnamed woman brought before Jesus in the Temple by scribes and Pharisees who cite the Mosaic penalty of stoning for adultery (8:3-5), and to whom Jesus finally says, after her accusers disperse one by one, `"neither do I condemn you; go, and from now on sin no more`" (8:11). Like the man born blind and the Samaritan woman at the well elsewhere in John, she is never given a name in the text."
'PAPIAS OF HIERAPOLIS' = "An early second-century bishop of Hierapolis in Asia Minor whose now-lost writings are known chiefly through quotations preserved by the fourth-century historian Eusebius of Caesarea. Eusebius reports that Papias related a story about a woman accused of many sins before the Lord, cited in this chapter as early, independent evidence that some version of the pericope adulterae tradition circulated in Christian memory long before it entered any surviving manuscript of John [[NOTE:eusebius-papias-similar-story]]."
'SUKKOT (FEAST OF TABERNACLES)' = "The autumn pilgrimage festival, still underway from the previous chapter, during which the Mishnah describes a nightly Temple illumination ceremony in the Court of Women — towering lit menorahs and dancing crowds — read by mainstream commentary as the immediate ritual backdrop against which Jesus's `"I am the light of the world`" (8:12) would have landed for a Temple audience [[NOTE:mishnah-sukkah-illumination-rite]] [[NOTE:brown-light-of-world-sukkot-backdrop]]."
'EGO EIMI ("I AM") SAYINGS' = "The Greek construction, literally `"I am,`" that recurs through John's Gospel in Jesus's absolute, object-less self-declarations, most densely clustered in this chapter (8:24, 8:28, 8:58). Scholarship connects the usage to the same Greek phrase the Septuagint uses for God's self-naming to Moses at the burning bush (Exodus 3:14) and to the Hebrew Scriptures' own recurring divine self-declaration `"I am He`" [[NOTE:ego-eimi-exodus-burning-bush]] [[NOTE:ball-williams-ego-eimi-scholarship]]."
'COURT OF WOMEN' = "An outer court of the Herodian Temple, open to Jewish women and men alike, where the Mishnah locates the Sukkot illumination ceremony and where John situates the treasury (8:20) — placing the entirety of the Light of the World discourse within sight of the festival's own recently lit lamps [[NOTE:mishnah-sukkah-illumination-rite]]."
'STONING (LEVITICUS 24:16 BLASPHEMY PENALTY)' = "The capital penalty Leviticus 24:16 prescribes for blaspheming God's Name, cited by mainstream commentary as the legal backdrop for both the accusers' demand in 8:3-5 and the crowd's attempt to stone Jesus after `"before Abraham was, I am`" (8:58-59), where a claim understood as self-identification with the divine Name triggers exactly the response the statute anticipates [[NOTE:leviticus-24-16-blasphemy-stoning]]."
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
    Add-BeatNode $Ch8NodeId $id $sortKey
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
Seed-Entity "Woman Caught in Adultery" "woman-caught-in-adultery" "character" "Unnamed woman brought before Jesus in the Temple on a charge of adultery (John 8:3-11); told, after her accusers disperse, 'neither do I condemn you.'"
Seed-Entity "Papias of Hierapolis" "papias-of-hierapolis" "character" "Early second-century bishop of Hierapolis whose lost writings, preserved by Eusebius, report an early tradition about a woman accused of many sins before Jesus."
Seed-Entity "Pericope Adulterae" "pericope-adulterae" "vocabulary" "Technical name for the story of the woman caught in adultery (John 7:53-8:11), textually absent from the earliest manuscripts of John."
Seed-Entity "Sukkot (Feast of Tabernacles)" "sukkot-feast-of-tabernacles" "vocabulary" "Autumn pilgrimage festival featuring a Temple illumination ceremony in the Court of Women, the ritual backdrop for John 8:12's 'light of the world' saying."
Seed-Entity 'Ego Eimi ("I Am") Sayings' "ego-eimi-i-am-sayings" "vocabulary" "Absolute, object-less 'I am' self-declarations recurring through John's Gospel, echoing God's self-naming to Moses at the burning bush (Exodus 3:14)."
Seed-Entity "Court of Women" "court-of-women" "place" "Outer court of the Herodian Temple where the Mishnah locates the Sukkot illumination ceremony and John situates the Light of the World discourse (8:20)."
Seed-Entity "Stoning (Leviticus 24:16 Blasphemy Penalty)" "stoning-leviticus-24-16" "vocabulary" "Capital penalty for blaspheming the divine Name, the legal backdrop for both the accusers' charge in John 8 and the crowd's attempt to stone Jesus after 8:58."

$conn.Close()
Write-Host "DONE Chapter 8."
