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
$Ch28NodeId = [guid]"019FA078-392B-7408-B4A9-4CA5E15931F7"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA078-392B-7408-B4A9-4CA5E15931F7' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
# NOTE: [16] and [52] below are already-resolved cross-references to this chapter's
# own existing apparatus (note 16 = criterion of embarrassment, established at Matthew
# 3:13-17; note 52 = Josephus on women's testimony, established at Matthew 28:1-10) --
# written as literal numbers, not placeholders, since they were assigned by earlier passes.
$notes = [ordered]@{
'guard-at-tomb-unique-to-matthew' = @{ title='A detail only Matthew tells, and only Matthew has to explain away'; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, vol. 2, Anchor Bible Reference Library (New York: Doubleday, 1994), commentary on the guard-at-the-tomb pericope (Matthew 27:62-66; 28:11-15). Brown treats the guard story as found only in Matthew among the four canonical Gospels — Mark, Luke, and John all move directly from burial to the women's visit, with no guard, no seal, and no bribed soldiers anywhere in the scene. Brown reads Matthew's own closing formula, `"this story has been spread among the Jews to this day`" (28:15), as clear evidence the evangelist is answering a real, contemporary rival explanation circulating at the time of writing, even while Brown himself states plainly that he doubts the guard's own historicity. The apologetic function and the story's historicity are, on his reading, two separate questions, and only one of them is settled." }
'koustodia-guard-ambiguity' = @{ title="Whose guard, exactly? A word that leaves the question open"; body="Craig S. Keener, The Gospel of Matthew: A Socio-Rhetorical Commentary (Grand Rapids: Eerdmans, 2009), commentary ad loc. Matthew 27:65-66; R. T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 2007), commentary ad loc. Matthew 27:65-66. Pilate's reply to the chief priests — `"you have a guard [Greek koustodia, a Latin loanword from custodia]; go, make it as secure as you know how`" (27:65) — is genuinely ambiguous about whose personnel are meant. Keener reads it as Pilate assigning actual Roman soldiers, reasoning that the priests would have had no need to approach the prefect at all if they only meant to post their own temple police. France reads the same line the other way, arguing Pilate's deliberately indirect phrasing hands responsibility for the tomb back to the priests' own guard rather than committing Roman troops to it. The two readings do not agree, and the choice matters: it decides whose testimony, Roman or Temple, is being bought in the very next scene (28:12-14)." }
'justin-martyr-trypho-108-stolen-body' = @{ title="A rival story still being told a century later"; body="Justin Martyr, Dialogue with Trypho, chapter 108, trans. Thomas B. Falls, rev. Thomas P. Halton, ed. Michael Slusser, Selections from the Fathers of the Church 3 (Washington, DC: Catholic University of America Press, 2003). Writing in the mid-second century, roughly a hundred years after the crucifixion and decades after Matthew's own Gospel, Justin states that Jewish teachers had sent chosen men throughout the world reporting that Jesus's disciples had stolen his body from the tomb by night and were deceiving people by claiming a resurrection. This is independent, external confirmation that the counter-explanation Matthew's text already argues against in 28:15 was not a straw man invented for the scene: it was a real, circulating rival account with real staying power, still being told long after the Gospel that answers it was written." }
'toledot-yeshu-later-counter-narrative' = @{ title="The rival story's long afterlife"; body="Peter Schafer, Michael Meerson, and Yaacov Deutsch, eds., Toledot Yeshu (`"The Life Story of Jesus`") Revisited: A Princeton Conference (Tubingen: Mohr Siebeck, 2011). The body-theft counter-explanation Matthew answers, and Justin Martyr still reports circulating a century later [[NOTE:justin-martyr-trypho-108-stolen-body]], resurfaces much further on, reworked and expanded, in the medieval Jewish counter-Gospel tradition known as the Toledot Yeshu — compiled from older, in places much older, oral material, with surviving manuscripts dating from the early medieval period onward. Some versions of the tradition have a gardener move the body before the disciples arrive, then sell it to the authorities once the empty tomb causes a stir. The editors' collected volume traces this long, layered transmission history rather than arguing for the story's historicity; the relevant point for this chapter is only that a rival explanation for the empty tomb had remarkable staying power across many centuries, not that any version of it is more likely true." }
'empty-tomb-four-independent-accounts' = @{ title="One more account feeding a larger, already-examined debate"; body="Raymond E. Brown, The Virginal Conception and Bodily Resurrection of Jesus (New York: Paulist Press, 1973), comparative treatment of the four canonical resurrection narratives. Matthew's account of the empty tomb (28:1-10) is one of four independent, variously overlapping resurrection narratives across the Gospels, alongside Mark 16:1-8, Luke 24:1-12, and John 20:1-18. This book's examination of John chapter 20 lays out the fuller historical-Jesus debate over what these four accounts jointly establish at length — more skeptical readings that treat the appearance traditions as visionary or psychological experiences of grief-stricken followers, weighed against arguments that the specific, combined shape of an empty tomb plus repeated physical appearance-claims is itself a historical anomaly requiring explanation. That debate is not re-derived here. The point worth making at this chapter, briefly, is only that Matthew's version is one more data point feeding into it, not a separate case requiring separate adjudication." }
'matthew-28-19-trinitarian-formula-liturgical-development' = @{ title="A fuller formula than the earliest baptisms show"; body="Donald A. Hagner, Matthew 14-28, Word Biblical Commentary vol. 33B (Dallas: Word Books, 1995), commentary ad loc. Matthew 28:19. Hagner reads the triadic baptismal formula — `"in the name of the Father and of the Son and of the Holy Spirit`" — as most plausibly a liturgical expansion reflecting the practice of Matthew's own later community rather than a verbatim record of Jesus's own words on a Galilean mountain, noting that every actual baptism narrated in the book of Acts instead uses a simpler, Jesus-only formula (Acts 2:38; 8:16; 10:48; 19:5). Hagner also notes that the church historian Eusebius, writing before Nicaea, cites Matthew 28:19 often enough in a shorter form — `"make disciples of all nations in my name`" — to have led some scholars to argue for an earlier, non-triadic reading behind the text. It is worth being exact about the limits of that argument: no surviving Greek manuscript of Matthew, in any textual family, actually preserves the shorter reading. The case for an earlier form rests entirely on patristic citation habit, not on any manuscript variant." }
'didache-7-early-triadic-baptism' = @{ title="The same three names, independently, very early"; body="Didache 7:1-3, a Christian catechetical text of disputed but early date, most commonly placed in the late first or early second century CE; Kurt Niederwimmer, The Didache: A Commentary, trans. Linda M. Maloney, Hermeneia (Minneapolis: Fortress Press, 1998), commentary ad loc. chapter 7. Without a literary dependence on Matthew's finished Gospel text that can be firmly established either way, the Didache already instructs its readers to baptize `"in the name of the Father, and of the Son, and of the Holy Spirit.`" Whatever the exact relationship between this formula and Matthew 28:19's own composition history, the triadic wording itself was clearly established in real liturgical use very early — not a late invention layered on centuries after the events it describes." }
'matthew-28-17-doubt-authenticity-criterion' = @{ title="Doubt inside the very scene meant to end all doubt"; body="R. T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids: Eerdmans, 2007), commentary ad loc. Matthew 28:17. France notes that Matthew's verb for `"doubted`" here (distazo) occurs in only one other place in his Gospel: 14:31, where Peter, walking on water, starts to sink, and Jesus asks him, `"why did you doubt?`" The link ties both moments to the same kind of wavering hesitation rather than flat, settled disbelief. What makes the detail worth pausing on is where it sits: `"when they saw him they worshipped him; but some doubted`" (28:17) places worship and doubt in the same eleven-word sentence, at the Gospel's single most triumphant moment. That is exactly the kind of detail the criterion of embarrassment treats as a mark of preserved memory rather than invention [16]: a scene built from nothing to end a book on maximum triumphant certainty had little reason to admit that some of its own central, named witnesses still hesitated." }
'daniel-7-authority-echo-2818' = @{ title="Enthronement language for an ending built on an execution"; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary, vol. 3 (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 28:18. Jesus's opening claim in the Great Commission — `"all authority in heaven and on earth has been given to me`" — echoes Daniel 7:13-14, where `"one like a son of man`" is brought before the Ancient of Days and given `"dominion and glory and a kingdom, that all peoples, nations, and languages should serve him.`" Davies and Allison read the echo as deliberate rather than coincidental: Matthew has already built Jesus's own preferred self-designation, `"Son of Man,`" on that same Danielic scene earlier in the Gospel, and the risen Jesus's closing claim to universal authority, paired immediately with a command to make disciples `"of all nations,`" completes the picture that title was pointing toward all along." }
'allison-new-moses-mountain-typology' = @{ title="One more unnamed mountain in a Moses-shaped book"; body="Dale C. Allison Jr., The New Moses: A Matthean Typology (Minneapolis: Fortress Press, 1993). Allison traces a sustained pattern across Matthew's Gospel in which the most decisive moments of revelation and authority happen on an unnamed mountain: the Sermon on the Mount (5:1), the Transfiguration (17:1), and here, the Great Commission (28:16), each deliberately recalling Moses's own repeated mountain-top encounters with God on Sinai. Allison reads the Great Commission's mountain setting as the pattern's culminating instance — a new Moses, on one final unnamed mountain, handing down not commandments received from someone greater but authority already possessed in his own right." }
'matthew-1-23-28-20-immanuel-inclusio' = @{ title="The book's own bracket: Emmanuel to `"I am with you`""; body="W. D. Davies and Dale C. Allison Jr., Matthew 19-28, International Critical Commentary, vol. 3 (Edinburgh: T&T Clark, 1997), commentary ad loc. Matthew 28:16-20. Davies and Allison read the Gospel's final line — `"I am with you always, to the end of the age`" — as a deliberate inclusio, a structural device bracketing the whole book with a matching opening and closing theme, answering the naming of Jesus as Emmanuel, `"God with us,`" at the Gospel's very opening (1:23, quoting Isaiah 7:14), with a further echo already sounded in between at 18:20 (`"where two or three are gathered in my name, there am I among them`"). The presence-with-us theme brackets the entire narrative: stated once at the outset as a name given to an unborn child, and once at the close as a promise a risen teacher extends to his followers for as long as the age itself lasts." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders; [16] and [52] are literal
# already-resolved cross-references to this chapter's own existing notes) ----
$beat1 = @'
The guard story that opens this chapter's real controversy was set up at the end of the previous chapter and nowhere else: only Matthew reports the chief priests and Pharisees asking Pilate for a guard at all, "lest his disciples go and steal him away, and tell the people, 'He has risen from the dead'" (27:62-66) — and only Matthew reports what happens to that guard once the tomb is found empty. Terrified soldiers report the earthquake and the angel to the chief priests, who pay them a substantial bribe to say instead that the disciples came by night and stole the body while they slept, promising to smooth things over with Pilate if the story reaches him — "and this story has been spread among the Jews to this day" (28:11-15). No other Gospel — not Mark, not Luke, not John — includes a guard, a seal, or a bribe anywhere in its resurrection account; the entire sequence is unique to Matthew [[NOTE:guard-at-tomb-unique-to-matthew]].

That uniqueness is itself the interesting fact, because it means Matthew is doing something none of the other evangelists needed to do: answering, on the page, a specific rival explanation that his own community was apparently still having to argue against at the time of writing. "This story has been spread among the Jews to this day" is not vague color — it is Matthew directly conceding that a counter-account of the empty tomb was in real, contemporary circulation, and then building an entire narrative sequence, guard included, whose sole function is to rebut it. Whichever side of that ancient argument a reader finds more persuasive, this passage preserves clear evidence that the resurrection's factual basis was a live, disputed question in the first century, not a story invented in a vacuum with no one left to contradict it.

Even the guard's own chain of custody carries a small, genuine ambiguity worth naming. Pilate's reply — "you have a guard [Greek koustodia]; go, make it as secure as you know how" (27:65) — can be read as handing the priests Roman soldiers, or as telling them to use their own Temple security instead, and serious commentators land on both sides [[NOTE:koustodia-guard-ambiguity]]. That distinction decides whose word, exactly, gets quietly bought off in the very next scene: a Roman detachment's, or the priests' own men's.

And the rival story Matthew answers did not end with Matthew. Roughly a century later, the Christian apologist Justin Martyr reports that Jewish teachers were still sending men out with the same claim — that the disciples had stolen the body by night and were deceiving people about a resurrection [[NOTE:justin-martyr-trypho-108-stolen-body]]. Centuries further on still, a reworked and expanded version of the same core accusation resurfaces in the medieval Jewish counter-Gospel tradition known as the Toledot Yeshu, compiled from older oral material and preserved in manuscripts from the early medieval period onward [[NOTE:toledot-yeshu-later-counter-narrative]]. None of that later evidence can settle what actually happened at the tomb. What it does establish, clearly, is that this was never a dispute invented after the fact with no opposing side left to answer — it was argued, on both sides, for centuries.
'@

$beat2 = @'
Set beside that guard-and-bribe sequence, worth noticing in the same breath, is who Matthew has already told his readers found the tomb first: Mary Magdalene and "the other Mary," going to see the tomb at dawn (28:1) — women whose testimony, as this chapter's own opening beat already notes from Josephus's blunt statement on the subject, carried reduced legal standing in this period [52]. There is a real irony sitting quietly inside this chapter's two halves: the male guards' testimony is precisely what the chief priests find worth buying and redirecting, while the women's testimony is what the Gospel tradition preserves unaltered and unbought. Neither fact proves what happened at the tomb. Both are worth noticing for what they say about which account, in this culture, would have needed no incentive to invent.

Zooming out one more step: Matthew's version of the empty tomb is not a case that stands alone. It is one of four independent, variously overlapping resurrection accounts across the canonical Gospels, and this book's examination of the parallel scene in John chapter 20 lays out, at real length, the wider historical-Jesus debate those four accounts jointly feed — more skeptical readings that treat the appearance traditions as visionary or psychological experiences of grief, weighed against arguments that the specific, combined shape of an empty tomb plus repeated physical appearance-claims is itself an anomaly needing explanation [[NOTE:empty-tomb-four-independent-accounts]]. That fuller debate belongs to that chapter, not this one; the honest thing to say here is only that Matthew's account is one more data point inside it, not a separate case this book needs to argue all over again.
'@

$beat3 = @'
The Great Commission's baptismal instruction — "baptizing them in the name of the Father and of the Son and of the Holy Spirit" (28:19) — is worth pausing on for a real, substantive textual-historical question, not just a devotional one. Every account of an actual baptism narrated anywhere in the book of Acts uses a simpler formula instead: people are baptized "in the name of Jesus" and nothing more (Acts 2:38; 8:16; 10:48; 19:5). That gap between Matthew's fuller, triadic wording and Acts's consistently simpler one is exactly the kind of pattern historical-critical scholarship reads as evidence of later liturgical language: a formula reflecting the developed practice of Matthew's own community, decades after the events it depicts, placed retrospectively into Jesus's own mouth at the story's climactic moment [[NOTE:matthew-28-19-trinitarian-formula-liturgical-development]]. It is worth being precise about what is, and is not, actually in dispute here: no surviving Greek manuscript of Matthew, in any textual family, lacks the triadic formula — this is not a manuscript-variant question, only a question about how a wording that is definitely original to the finished Gospel text came to be there.

The other side of that same coin is that the triadic formula was not a later invention centuries removed from the text. A separate, independent early Christian catechetical document, the Didache, already instructs its own readers to baptize "in the name of the Father, and of the Son, and of the Holy Spirit" — without any literary dependence on Matthew's Gospel that can be firmly established either way [[NOTE:didache-7-early-triadic-baptism]]. Whatever the precise relationship between the two texts, the wording itself was clearly in real liturgical use very early. The honest state of the evidence, then, is not "Jesus definitely said exactly these fourteen words on a mountain in Galilee" versus "the church made this up out of nothing centuries later" — it is a real, worth-stating-plainly question about how quickly and how thoroughly the earliest church's own developing worship language found its way onto the page of a Gospel written to record what Jesus said.
'@

$beat4 = @'
One small, honest textual detail sits inside the very scene built to deliver the Gospel's most triumphant line: when the eleven disciples see the risen Jesus on the mountain in Galilee, "they worshipped him; but some doubted" (28:17) — worship and doubt, named in the same sentence, among the same small group of named, central witnesses, at the climactic moment the whole book has been building toward. Matthew's word for "doubted" here appears in only one other place in his Gospel: chapter 14, where Peter, walking on water toward Jesus, starts to sink, and is asked, "why did you doubt?" (14:31) — the same wavering hesitation, not flat disbelief, in both places [[NOTE:matthew-28-17-doubt-authenticity-criterion]].

This is the same criterion of embarrassment already at work earlier in this book's own account of Jesus's baptism [16]: a detail that costs the storyteller something is judged more likely to be a preserved memory than an invention, precisely because a community free to invent this scene from nothing had an obvious reason to leave the doubt out and no obvious reason to put it in. A resurrection-appearance scene composed to settle the matter, once and for all, at the very end of the book, is not the kind of scene a community writing pure triumphant fiction had any reason to complicate with its own central witnesses still hesitating. That doesn't prove the appearance happened as narrated. It is a real, textually preserved reason to think this particular detail was not manufactured for effect.
'@

$beat5 = @'
Jesus's opening claim in the Great Commission itself — "all authority in heaven and on earth has been given to me" (28:18) — is not free-floating language. It echoes Daniel 7:13-14, where "one like a son of man" is brought before the Ancient of Days and given dominion, glory, and a kingdom, so that "all peoples, nations, and languages" would serve him — the same Danielic scene Matthew has already built Jesus's preferred self-designation, "Son of Man," on earlier in the book. The risen Jesus's claim to universal authority, paired immediately with the command to make disciples "of all nations" (28:19), completes the picture that title was pointing toward from the start [[NOTE:daniel-7-authority-echo-2818]]. And the mountain itself is not incidental scenery: it is the last in a sustained pattern of unnamed mountains across this Gospel where its most decisive moments of revelation happen — the Sermon on the Mount, the Transfiguration, and now the Great Commission — each one recalling Moses's own repeated encounters with God on Sinai, a new Moses handing down, on one final unnamed mountain, not commandments received but authority already his own [[NOTE:allison-new-moses-mountain-typology]].

The book's very last line — "I am with you always, to the end of the age" (28:20) — closes a bracket this Gospel opened at its very first chapter: Jesus named there as Emmanuel, "God with us" (1:23, quoting Isaiah 7:14), with a further echo already sounded in between at 18:20. A deliberate inclusio, a structural device bracketing the whole book with a matching opening and closing theme, is a real, checkable literary feature of the finished text, not an imposed reading [[NOTE:matthew-1-23-28-20-immanuel-inclusio]] — worth naming plainly as the compositional choice it is, whichever hand or hands assembled the final book.

It is worth closing this book the same way it has tried to open every chapter: honestly, and without either debunking the claim or overclaiming it. This is the last chapter of a four-Gospel project, and it ends the way John's own Gospel ends — not quite the same way. John closes with a colophon, a communal "we" formally vouching for a named eyewitness's testimony (21:24-25), a recognized genre borrowed from ancient book production. Matthew closes with no such formal attestation at all — no editorial "we," no named witness vouched for — only a claim of continuing presence, stated once as a name given to an unborn child and once more as a promise extended by a risen teacher to people who, on the book's own admission, do not all agree even in this closing scene [[NOTE:matthew-28-17-doubt-authenticity-criterion]]. Two Gospels, two different ways of telling a reader how much to trust what they have just read, and this book leaves both exactly where it has left every claim like them across four Gospels now: real, contested at the time they were written [[NOTE:justin-martyr-trypho-108-stolen-body]], and outside what evidence alone can finally settle.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'GUARD AT THE TOMB (MATTHEW''S UNIQUE APOLOGETIC)' = "The sequence — a guard requested from Pilate (27:62-66), terrified soldiers, and a bribe to say the disciples stole the body (28:11-15) — found only in Matthew among the four canonical Gospels, closing with Matthew's own concession that the rival stolen-body explanation `"has been spread among the Jews to this day`" (28:15) [[NOTE:guard-at-tomb-unique-to-matthew]]. The word for the guard itself, koustodia, leaves genuinely open whether Roman or Temple personnel are meant [[NOTE:koustodia-guard-ambiguity]]. The rival explanation Matthew answers is independently attested still circulating a century later in Justin Martyr [[NOTE:justin-martyr-trypho-108-stolen-body]], and resurfaces, reworked, centuries further on in the medieval Toledot Yeshu tradition [[NOTE:toledot-yeshu-later-counter-narrative]]."
'GREAT COMMISSION (TRINITARIAN FORMULA''S TEXTUAL HISTORY)' = "The baptismal instruction closing the Great Commission — `"baptizing them in the name of the Father and of the Son and of the Holy Spirit`" (28:19) — distinct from this book's existing entry on the mission's widening geographic scope (see ISRAEL-FIRST MISSION VS. GREAT COMMISSION). Every baptism actually narrated in Acts instead uses a simpler, Jesus-only formula (Acts 2:38; 8:16; 10:48; 19:5), a gap mainstream scholarship reads as evidence the fuller triadic wording reflects Matthew's own later liturgical practice [[NOTE:matthew-28-19-trinitarian-formula-liturgical-development]]. No manuscript of Matthew lacks the triadic reading, and an independent early Christian text, the Didache, already uses the same three-part formula on its own [[NOTE:didache-7-early-triadic-baptism]]."
'IMMANUEL INCLUSIO (MATTHEW 1:23 / 28:20)' = "The Gospel's closing line, `"I am with you always, to the end of the age`" (28:20), read by mainstream commentary as a deliberate inclusio answering the naming of Jesus as Emmanuel, `"God with us,`" at the book's opening (1:23, quoting Isaiah 7:14), with a further echo sounded in between at 18:20 [[NOTE:matthew-1-23-28-20-immanuel-inclusio]]. The same closing scene sets the Great Commission's `"all authority`" claim against Daniel 7:13-14's enthroned `"son of man`" [[NOTE:daniel-7-authority-echo-2818]], and its mountain setting against a sustained Mosaic mountain-typology running through the whole Gospel [[NOTE:allison-new-moses-mountain-typology]]."
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
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $maxChapterSortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch28NodeId $id $maxChapterSortKey
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
Seed-Entity "Justin Martyr" "justin-martyr" "character" "Mid-second-century CE Christian apologist; Dialogue with Trypho ch. 108 reports Jewish teachers still spreading the stolen-body counter-explanation of the empty tomb a century after Matthew."
Seed-Entity "Toledot Yeshu" "toledot-yeshu" "vocabulary" "Medieval Jewish counter-Gospel tradition, compiled from older oral material, preserving a reworked version of the body-theft explanation Matthew's guard-at-the-tomb pericope answers."
Seed-Entity "Didache" "didache" "vocabulary" "Early Christian catechetical text (late first/early second century CE) whose baptismal instructions (ch. 7) independently attest the same triadic Father/Son/Holy-Spirit formula found in Matthew 28:19."

$conn.Close()
Write-Host "DONE Matthew Chapter 28 depth pass."
