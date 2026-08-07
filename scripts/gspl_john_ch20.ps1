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
$Ch20NodeId = [guid]"019FA96D-5C2E-7177-B5DE-1F6484205004"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
# Hardened: guards against the stray TEST_PROBE_DELETE_ME row (and any other space-less row) in the
# shared Notes node, which crashes the template's plain MAX(CAST(LEFT(...))) derivation.
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'empty-tomb-synoptic-differences' = @{ title='One empty tomb, four different memories'; body="Raymond E. Brown, The Virginal Conception and Bodily Resurrection of Jesus (New York: Paulist Press, 1973), in his comparative treatment of the four canonical resurrection narratives. Brown catalogs the real differences among Mark 16:1-8, Matthew 28:1-10, Luke 24:1-12, and John 20:1-18: the number of women at the tomb (one in John, at least two in the others), the number of angelic figures encountered there (one young man in Mark, one angel in Matthew, two men in Luke, two angels in John), and whether the risen Jesus himself is seen at the tomb on that first visit (only in Matthew and John). Brown treats the underlying tradition of a genuinely empty tomb, present in some form across all four Gospels, as historically probable while treating the surrounding narrative details as independently shaped by each evangelist's own sources and theological interests." }
'ehrman-visionary-experience' = @{ title='Grief, guilt, and the psychology of seeing someone who has died'; body="Bart D. Ehrman, How Jesus Became God: The Exaltation of a Jewish Preacher from Galilee (New York: HarperOne, 2014), chapter on the resurrection appearances. Ehrman argues the appearance traditions, including the one narrated in John 20, are best explained as visionary or hallucinatory experiences of grief-stricken followers, a documented bereavement phenomenon in which the recently bereaved report seeing, hearing, or being addressed by the deceased; on this reading the appearances are subjectively real to the witnesses without requiring an objectively risen body." }
'ludemann-psychological-experience' = @{ title="Peter's guilt, Mary's grief: a psychological account of Easter faith"; body="Gerd Ludemann, The Resurrection of Jesus: History, Experience, Theology, trans. John Bowden (Minneapolis: Fortress Press, 1994). Ludemann, working from a history-of-religions and psychological framework, argues the resurrection appearances — including Mary Magdalene's encounter at the tomb in John 20:11-18 — originated as subjective visionary experiences rooted in the specific emotional states of individual disciples (guilt in Peter's case, grief in Mary Magdalene's), which the earliest community then interpreted, and later Gospel writers narrated, as encounters with a bodily risen Jesus." }
'wright-resurrection-anomaly' = @{ title='An event without precedent needs an explanation, not a category'; body="N. T. Wright, The Resurrection of the Son of God, Christian Origins and the Question of God, vol. 3 (Minneapolis: Fortress Press, 2003). Wright argues at length that neither an empty tomb alone nor appearance-reports alone would have generated the specific, historically unprecedented claim that a first-century Jew had already been bodily raised in the middle of history, rather than at the end of all things, per mainstream Second Temple Jewish resurrection expectation; he contends the combination narrated across John 20 — a tomb found empty and a body encountered as tangible, speaking, and yet transformed — is the phenomenon requiring explanation, and that a genuine bodily resurrection is, in his judgment, the explanation that best accounts for both strands together." }
'folded-face-cloth-detail' = @{ title='A folded cloth, not a ransacked grave'; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), commentary ad loc. John 20:6-7. Brown notes that the detail of the face-cloth (soudarion) 'rolled up in a place by itself,' separate from the linen wrappings, is unique to John among the Gospels, and that the scene's own internal logic uses the orderly arrangement to rule out the possibility the disciples might otherwise have assumed — that the body had been hastily stolen — since grave robbers moving quickly would have had no reason, and likely no time, to refold anything." }
'kloner-rolling-stone' = @{ title="Most first-century tombs did not roll shut"; body="Amos Kloner, 'Did a Rolling Stone Close Jesus' Tomb?' Biblical Archaeology Review 25, no. 5 (1999): 22-29, 76. Kloner's survey of excavated Second Temple-period Jerusalem-area tombs found that of more than nine hundred tombs catalogued, only a small handful — around four — were sealed with the large round rolling stone depicted in the Gospel burial and resurrection accounts; the great majority used smaller square or rectangular blocking stones instead. Kloner takes this as evidence that a rolling-stone tomb, while genuinely attested for the period, marked out an unusually elaborate and costly installation." }
'kloner-zissu-tomb-typology' = @{ title="What a first-century Jerusalem tomb actually looked like"; body="Amos Kloner and Boaz Zissu, The Necropolis of Jerusalem in the Second Temple Period (Leuven: Peeters, 2007). Kloner and Zissu's catalog of excavated Jerusalem-area burial caves documents the standard forms available to a family of means in this period — kokhim (long shaft-like burial niches) and arcosolia (shelf-and-arch recesses) cut into a rock chamber reached through a low entrance — against which the Gospels' shared picture of a rock-cut tomb someone could stoop to look or step into (John 20:5-6) can be checked as a broadly typical, period-appropriate installation." }
'bauckham-mary-magdalene-first-witness' = @{ title='The apostle to the apostles'; body="Richard Bauckham, Gospel Women: Studies of the Named Women in the Gospels (Grand Rapids: Eerdmans, 2002), chapter on Mary Magdalene. Bauckham notes that all four canonical Gospels agree, despite their many other differences over the resurrection narrative's specific details, that Mary Magdalene was among the first — in John, the very first — to find the tomb empty and to become the first witness and proclaimer of the resurrection message, a role later Christian tradition would come to call her 'apostle to the apostles.'" }
'josephus-women-testimony' = @{ title='Whose testimony counted, by first-century Jewish legal reckoning'; body="Flavius Josephus, Jewish Antiquities, Book 4, section 219 (Loeb Classical Library, trans. H. St. J. Thackeray and Ralph Marcus, Cambridge, MA: Harvard University Press). Josephus states plainly that 'let not the testimony of women be admitted, on account of the levity and boldness of their sex,' reflecting a widely shared, though not universally codified, low evidentiary weight placed on female witnesses in the period. Scholars across a wide spectrum, including several otherwise skeptical of the resurrection narratives' historicity, weigh this background consideration heavily: a wholly invented legitimizing story composed after the fact would have had an obvious motive to name male, socially credentialed witnesses first, rather than lead with a woman whose testimony carried reduced legal standing." }
'meier-resurrection-beyond-method' = @{ title="Where the historian's method runs out"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), Introduction, on the boundaries of his project's method. Meier explicitly declines to adjudicate the resurrection itself as a strictly historical question within his 'marginal Jew' project, on the grounds that the historical-critical method as he defines it is suited to investigating what can be reconstructed about Jesus's life and death using ordinary historical evidence and inference, not to confirming or disconfirming a claimed act of God; his caution is a useful marker of how much of the debate over John 20 turns on what kind of question is being asked, not only on what evidence is available." }
'spirit-giving-vs-pentecost' = @{ title='Two very different calendars for the same gift'; body="C. K. Barrett, The Gospel According to St John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 20:22. Barrett addresses directly what he and others call the 'Johannine Pentecost': in John, the risen Jesus breathes the Holy Spirit into the gathered disciples on the very evening of the first Easter Sunday, while Luke-Acts places the Spirit's descent on the separate, later occasion of Pentecost, fifty days afterward and following Jesus's ascension (Acts 2:1-4). The two timelines are not easily harmonized into a single sequence of events, and Barrett treats the discrepancy as a genuine difference in theological chronology between the two traditions rather than a puzzle solvable by narrative reconciliation." }
'genesis-ezekiel-breath-echo' = @{ title='A gesture that reaches back to the first creation'; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 20:22. Keener reads Jesus's act of breathing on the disciples as a deliberate echo of two Old Testament scenes: God breathing the breath of life into the first human in Genesis 2:7, and the prophet's vision of breath entering the dry bones in Ezekiel 37:9-10; on this reading the scene casts the Spirit's giving as a new-creation and national-restoration act, layered meanings available to Greek-literate Jewish readers of the period rather than an incidental physical gesture." }
'allison-bereavement-visions' = @{ title='Grief-visions of the dead, a documented cross-cultural pattern'; body="Dale C. Allison Jr., Resurrecting Jesus: The Earliest Christian Tradition and Its Interpreters (New York: T&T Clark, 2005), chapter on the resurrection appearances. Allison surveys a substantial cross-cultural and comparative-religion literature on bereavement visions — documented experiences of grieving people seeing, hearing, or being addressed by a recently deceased loved one — and weighs how far that literature can and cannot account for the specific, escalating physicality of the Gospel appearance traditions, including Thomas's demand to touch Jesus's wounds in John 20:24-29, without settling the question either way." }
'thomas-confession-high-christology' = @{ title="The most direct 'my God' in the whole Gospel"; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), commentary ad loc. John 20:28; see also D. A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 20:28. Both commentators identify Thomas's confession, ho kyrios mou kai ho theos mou ('my Lord and my God'), as among the most direct and unambiguous ascriptions of full divinity to Jesus anywhere in the New Testament, a high point of Johannine christological confession placed, notably, in the mouth of the Gospel's most skeptical disciple." }
'thomas-twin-epithet' = @{ title='Thomas the Twin, and the traditions that grew around the name'; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 20:24, on the epithet 'Didymus' ('the Twin,' repeated at John 11:16, 14:5, and 21:2). Keener notes the name itself receives no explanation in John and probably functioned simply to distinguish this Thomas from others in the tradition; the elaborate later traditions built on it, including the apocryphal Acts of Thomas and its claim that Thomas was Jesus's literal twin brother, developed well after and independently of the canonical Gospel's own text." }
'john-20-original-ending-consensus' = @{ title='Where the book was first meant to stop'; body="D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), commentary ad loc. John 20:30-31 and the introduction to chapter 21. Smith represents the broad, though not absolutely unanimous, scholarly judgment that John 20:30-31, with its explicit statement of the book's purpose, was written as the Gospel's original concluding sentence, and that chapter 21 — which carries its own separate concluding statement at 21:24-25 — is a later appended epilogue, whether by the same author revisiting the text or by a different hand within the same Johannine community." }
'von-wahlde-composition-layers' = @{ title='A gospel built in stages, and an ending caught in the seam'; body="Urban C. von Wahlde, The Gospel and Letters of John, Eerdmans Critical Commentary, vol. 1 (Grand Rapids: Eerdmans, 2010), on the Gospel's three-stage compositional history. Von Wahlde's source- and redaction-critical model, which traces the Fourth Gospel through three successive editions, places chapter 21 in a later editorial stage than the bulk of chapters 1-20, treating the purpose statement of 20:30-31 as the closing seam of an earlier, self-contained edition of the book rather than as a mid-book aside." }
'bultmann-signs-source-purpose-statement' = @{ title='A conclusion built for a book of signs'; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Philadelphia: Westminster Press, 1971; German original 1941), commentary ad loc. John 20:30-31. Bultmann's source-critical reconstruction identifies 20:30-31 as the original closing statement of the hypothesized Signs Source underlying the Gospel's miracle narratives, its language of 'signs... written in this book' reading naturally as a conclusion to a completed work rather than as a transition to further material; on this view, chapter 21's continuation is source-critically secondary." }
'carson-purpose-textual-variant' = @{ title='One Greek letter, two different purposes for the whole book'; body="D. A. Carson, 'The Purpose of the Fourth Gospel: John 20:31 Reconsidered,' Journal of Biblical Literature 106, no. 4 (1987): 639-651; see also the Nestle-Aland Novum Testamentum Graece textual apparatus ad loc. John 20:31. The verb in 'that you may believe' is textually disputed between an aorist subjunctive (pisteusete, 'come to believe,' suggesting the Gospel's purpose is evangelistic, aimed at bringing outsiders to faith) and a present subjunctive (pisteuete, 'continue believing,' suggesting a pastoral purpose of confirming an already-believing community); Carson's widely discussed article argues the manuscript evidence and grammatical usage lean toward the present-tense reading, making Carson a notable dissent from the older majority view that the Gospel's stated purpose was primarily missionary." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
While it is still dark on the first day of the week, Mary Magdalene comes alone to the tomb and finds the stone already rolled away. She runs to Simon Peter and "the other disciple, the one whom Jesus loved," reporting only that "they have taken the Lord out of the tomb, and we do not know where they have laid him." The two disciples run together; the Beloved Disciple outruns Peter and reaches the tomb first, stooping to look in at the linen wrappings lying there, but it is Peter who arrives and actually goes inside, seeing the wrappings and the face-cloth that had been on Jesus's head "not lying with the linen wrappings but rolled up in a place by itself." Only then does the Beloved Disciple also go in, and see, and believe — followed immediately by the narrator's own aside that neither of them yet understood the Scripture that Jesus must rise from the dead. The two disciples simply go back to their homes (20:1-10).

Start with the scale of the question this scene opens onto, because it is genuinely one of the most argued in all of Gospel scholarship: an empty tomb tradition is present, in some form, in every one of the four canonical Gospels, which on its face looks like strong multiple attestation for a real, shared core event. But the four accounts disagree on almost every surrounding detail — how many women came, how many angelic figures were there, and whether Jesus himself was seen at the tomb on that first visit at all [[NOTE:empty-tomb-synoptic-differences]]. Historical-critical scholarship has never fully resolved what to make of that combination, and this book will not adjudicate it either; the honest range runs from more skeptical treatments that read the appearance traditions built on top of the empty-tomb claim as visionary or psychological experiences of grief-stricken followers, comparable to well-documented bereavement-vision phenomena [[NOTE:ehrman-visionary-experience]] [[NOTE:ludemann-psychological-experience]], to more traditionally argued historical cases contending that the specific, combined shape of an empty tomb plus repeated, insistently physical appearance-claims is itself the anomaly requiring an explanation, and that a genuine bodily resurrection is the best available one [[NOTE:wright-resurrection-anomaly]]. Both readings take the same text seriously; they simply weigh the same evidence differently.

The detail of the face-cloth "rolled up in a place by itself" is unique to John among the Gospels, and it is doing narrative work: a hasty grave-robbery would have had no reason, and likely no time, to fold anything neatly before leaving [[NOTE:folded-face-cloth-detail]]. That is worth stating precisely as what it is — an argument the text itself is making within its own story, not an independent, external historical claim the way an excavation report would be.

And an excavation report is exactly the next thing worth checking the story against. The large round "rolling stone" that popular imagination attaches to Jesus's tomb turns out to have been a genuinely attested but archaeologically uncommon installation: of the many hundreds of Second Temple-period tombs catalogued around Jerusalem, only a small handful were sealed that way, with the great majority instead using smaller square or rectangular blocking stones [[NOTE:kloner-rolling-stone]]. The broader typology of these tombs — shaft-like kokhim niches and shelf-like arcosolia recesses reached through a low entrance — confirms the Gospels' shared picture of a chamber someone could stoop to look into, or step inside of, as period-appropriate, even where the specific stone described marks an unusually elaborate installation [[NOTE:kloner-zissu-tomb-typology]].
'@

$beat2 = @'
Mary Magdalene, having gone back to the tomb, stays there weeping. Bending to look inside, she sees two angels sitting where the body had lain, one at the head and one at the feet, who ask her why she is weeping; she answers that they have taken away her Lord and she does not know where. Turning around, she sees Jesus standing there but does not recognize him, supposing him to be the gardener, and asks him where he has laid the body so she can retrieve it. Jesus says one word — her name, "Mary" — and she turns and answers, "Rabboni," which the text glosses as "Teacher." Jesus tells her not to cling to him, "for I have not yet ascended to the Father," and sends her instead to the other disciples with a message. Mary Magdalene goes and announces to them, "I have seen the Lord," and reports what he had said to her (20:11-18).

Whatever else scholars disagree about across the four resurrection accounts, they do not disagree about this: in every one of them, a woman — Mary Magdalene specifically — is the first person named as either finding the tomb empty or encountering the risen Jesus, and in John she is both [[NOTE:bauckham-mary-magdalene-first-witness]]. That agreement matters because of how female testimony was generally weighed in the period. Josephus states the common assumption bluntly: women's testimony was not to be admitted in court, on the grounds of what he calls "the levity and boldness of their sex" [[NOTE:josephus-women-testimony]]. Scholars across the range on this material, including several who read the wider appearance traditions skeptically, take this seriously as a version of the criterion of embarrassment: a story invented from nothing to legitimize a movement had an obvious motive to lead with more socially credentialed witnesses, and did not need to make its first proclaimer a woman whose word carried reduced legal standing. That argument has real force, and it also has real limits — it can make a tradition's antiquity more likely without proving that the underlying event happened exactly as narrated.

It is worth being honest, too, about where the historian's tools stop being the right tools. John P. Meier, in laying out the method for his own historical-Jesus project, explicitly declines to treat the resurrection itself as a question his method can settle one way or the other, since it asks about a claimed act of God rather than something reconstructable from ordinary historical evidence and inference [[NOTE:meier-resurrection-beyond-method]]. Mary Magdalene's announcement, "I have seen the Lord," is the hinge on which the whole chapter turns from empty-tomb puzzle to appearance-claim — and it is precisely there that the disciplines of history and faith are asking different kinds of questions of the same eleven words.
'@

$beat3 = @'
That evening, with the doors of the room locked "for fear of the Jews," Jesus comes and stands among the disciples and says, "Peace be with you." He shows them his hands and his side, and the disciples rejoice at seeing the Lord. He repeats the greeting, then adds, "As the Father has sent me, even so I am sending you." He breathes on them and says, "Receive the Holy Spirit. If you forgive the sins of any, they are forgiven them; if you withhold forgiveness from any, it is withheld" (20:19-23).

The timing of that gift is worth pausing on plainly, because it creates a real and rarely-harmonized discrepancy with the other place the New Testament narrates the Spirit's coming. Here in John, the risen Jesus gives the Spirit to the gathered disciples on the evening of Easter Sunday itself. Luke-Acts, by contrast, places the Spirit's descent on a wholly separate occasion, Pentecost, fifty days later and after a distinct ascension scene (Acts 2:1-4) [[NOTE:spirit-giving-vs-pentecost]]. The two Gospels are not telling the same chronological story about when the Spirit was given, and the honest response to that is to say so rather than to smooth it into a single invisible sequence the text itself never states.

The gesture Jesus uses — breathing on them — is not incidental stage direction. It deliberately recalls God breathing life into the first human in Genesis 2:7, and the prophet's vision of breath entering dry bones in Ezekiel 37 [[NOTE:genesis-ezekiel-breath-echo]]. Read against those two texts, the scene casts the Spirit's giving as an act of new creation and national restoration at once, a set of resonances available to a Greek-literate Jewish audience of the period, layered on top of whatever historical moment underlies the scene itself.
'@

$beat4 = @'
Thomas, one of the Twelve, was not with the others when Jesus came. Told afterward, "We have seen the Lord," he answers that unless he sees the mark of the nails in Jesus's hands, and puts his finger into the mark of the nails, and his hand into Jesus's side, he will not believe. A week later the disciples are again in the house, doors locked, and this time Thomas is with them. Jesus comes and stands among them, says "Peace be with you," and turns directly to Thomas: "Put your finger here, and see my hands; and put out your hand, and place it in my side. Do not disbelieve, but believe." Thomas answers, "My Lord and my God!" Jesus tells him, "Have you believed because you have seen me? Blessed are those who have not seen and yet have believed" (20:24-29).

Thomas's confession is worth stopping on in its own right. "My Lord and my God" is among the most direct, unhedged ascriptions of full divinity to Jesus found anywhere in the New Testament — and the Gospel places it, pointedly, in the mouth of the disciple who had just demanded the most concrete physical proof before he would say anything at all [[NOTE:thomas-confession-high-christology]]. That juxtaposition — the most skeptical disciple giving the highest confession — reads as a deliberate literary and theological choice, whatever one concludes about the history behind it.

The scene's insistence on touch belongs to a wider pattern worth naming honestly: the appearance traditions across the Gospels get more physically emphatic, not less, the later and more elaborated they are — Luke has the risen Jesus eat broiled fish to prove he is not a ghost (Luke 24:41-43); John has Thomas invited to put his hand in the wound itself. Scholars working from a comparative bereavement-vision framework read that trajectory as consistent with a tradition growing more concrete over time, in response to the skepticism its own claims provoked, rather than as cumulative independent confirmation of one static fact [[NOTE:allison-bereavement-visions]]. Others read the same trajectory as the tradition insisting, with increasing clarity, on something it had held from the start. The evidence does not settle which reading is correct, and this book will not pretend otherwise.

One small detail closes the scene with less theological weight but its own interest: Thomas is called "Didymus," the Twin, every time he appears in this Gospel, and John never explains the name. Later apocryphal tradition — centuries after John — would run with the epithet and make Thomas Jesus's literal twin brother, an elaboration that has no basis in, and developed independently of, the canonical text itself [[NOTE:thomas-twin-epithet]].
'@

$beat5 = @'
The chapter closes with the narrator stepping outside the story to address the reader directly: "Now Jesus did many other signs in the presence of the disciples, which are not written in this book; but these are written so that you may believe that Jesus is the Christ, the Son of God, and that by believing you may have life in his name" (20:30-31).

This is a genuine and unusually confident source-critical judgment, so it is worth stating plainly rather than hedging it into vagueness: a broad body of scholarship, spanning very different methodological schools, reads 20:30-31 as the Gospel's original planned ending, with chapter 21 as a later appended epilogue [[NOTE:john-20-original-ending-consensus]]. Rudolf Bultmann's source-critical reconstruction treats the verse as the closing line of the hypothesized written Signs source lying beneath the Gospel's miracle narratives, its language of "signs... written in this book" reading far more naturally as a conclusion than as a mid-book pause [[NOTE:bultmann-signs-source-purpose-statement]]. Urban C. von Wahlde's multi-stage compositional model reaches a compatible conclusion from a different angle, placing chapter 21 in a demonstrably later editorial layer than the bulk of chapters 1-20 [[NOTE:von-wahlde-composition-layers]]. That said, the judgment remains a critical inference, not something the manuscript tradition itself directly attests: no surviving copy of John's Gospel, however early, actually stops at 20:31 — chapter 21 is present in every manuscript that has come down to us.

Even the purpose clause's own wording carries a live textual dispute. The Greek verb behind "that you may believe" survives in two forms across the manuscript tradition — an aorist subjunctive, suggesting the Gospel was written to bring outsiders to faith for the first time, and a present subjunctive, suggesting it was written to sustain a community already believing. D. A. Carson's widely discussed study of the question argues the manuscript evidence and Johannine usage favor the second reading, against the older majority assumption that the Gospel's stated purpose was primarily missionary [[NOTE:carson-purpose-textual-variant]]. Either way, the sentence remains what the Gospel calls its own reason for existing — a claim about purpose that the rest of the book, chapter 21 included, was built to serve.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'MARY MAGDALENE' = "The first person, in all four canonical Gospels, to encounter either the empty tomb or the risen Jesus; in John specifically, she is the first to find the stone removed (20:1), the first to see and speak with the risen Jesus, mistaking him for the gardener until he says her name (20:14-16), and the first to proclaim, 'I have seen the Lord' (20:18). All four Gospels agree on her priority despite disagreeing on many surrounding details [[NOTE:empty-tomb-synoptic-differences]] [[NOTE:bauckham-mary-magdalene-first-witness]], a point of some evidentiary weight given the reduced legal standing of women's testimony in the period [[NOTE:josephus-women-testimony]]."
'THOMAS (DIDYMUS)' = "One of the Twelve, called 'Didymus' ('the Twin') at every appearance in this Gospel (11:16; 14:5; 20:24; 21:2), absent from the disciples' locked-room gathering on Easter evening and refusing to believe the others' report without touching Jesus's wounds himself (20:24-25); a week later he is given exactly that opportunity and responds with the confession 'my Lord and my God' (20:28) [[NOTE:thomas-confession-high-christology]]. The unexplained epithet 'Twin' later generated its own apocryphal traditions [[NOTE:thomas-twin-epithet]]."
'"MY LORD AND MY GOD" (JOHN 20:28)' = "Thomas's confession on touching the risen Jesus's wounds, ho kyrios mou kai ho theos mou, widely identified as among the most direct ascriptions of full divinity to Jesus anywhere in the New Testament [[NOTE:thomas-confession-high-christology]], answered immediately by Jesus's own blessing on 'those who have not seen and yet have believed' (20:29)."
'LINEN WRAPPINGS AND FACE-CLOTH (JOHN 20:6-7)' = "The grave clothes Peter and the Beloved Disciple find inside the empty tomb (20:6-7): linen wrappings (othonia) and a separate face-cloth (soudarion), the latter 'rolled up in a place by itself.' Unique to John among the Gospels, the detail's orderly arrangement functions within the narrative as an implicit argument against the possibility of hasty grave-robbery [[NOTE:folded-face-cloth-detail]]."
'JOHANNINE PENTECOST (JOHN 20:22)' = "The scholarly shorthand for Jesus breathing the Holy Spirit into the gathered disciples on the evening of the first Easter Sunday (20:22), a scene often read against Genesis 2:7 and Ezekiel 37 [[NOTE:genesis-ezekiel-breath-echo]] and set alongside, without easy harmonization with, Luke-Acts's separate account of the Spirit's descent fifty days later, at Pentecost (Acts 2:1-4) [[NOTE:spirit-giving-vs-pentecost]]."
'ORIGINAL ENDING OF JOHN (JOHN 20:30-31)' = "The Gospel's explicit statement of purpose — that 'these are written so that you may believe... and that by believing you may have life in his name' — read by a broad, though not absolutely unanimous, body of scholarship as the book's originally intended conclusion [[NOTE:john-20-original-ending-consensus]] [[NOTE:bultmann-signs-source-purpose-statement]] [[NOTE:von-wahlde-composition-layers]], with chapter 21 understood as a later appended epilogue. No surviving manuscript preserves the Gospel without chapter 21, so the judgment remains a critical inference rather than something the manuscript tradition itself directly attests."
'CRITERION OF EMBARRASSMENT' = "A historical-critical rule of thumb holding that a detail unhelpful, or even awkward, for the community that transmitted it is less likely to have been invented and more likely to be historically grounded, since a purely fabricated account would tend to remove such friction rather than introduce it. Applied to John 20, the criterion is often invoked regarding Mary Magdalene's priority as first witness [[NOTE:bauckham-mary-magdalene-first-witness]] [[NOTE:josephus-women-testimony]]; like all such criteria, it can indicate a tradition's antiquity without proving the underlying event occurred exactly as narrated."
'ANGELS AT THE TOMB (SYNOPTIC VARIATION)' = "The heavenly figures found at the empty tomb differ across the four Gospels — one young man in Mark, one angel in Matthew, two men in Luke, and two angels in John (20:12) — a variation historical-critical scholarship reads either as independent, individually shaped memories of a shared core event or as evidence of a developing tradition growing more elaborate over time [[NOTE:empty-tomb-synoptic-differences]]."
'ROLLING-STONE TOMBS (FIRST-CENTURY JERUSALEM)' = "The large disc-shaped blocking stone depicted sealing Jesus's tomb was a genuinely attested but archaeologically uncommon installation in first-century Jerusalem-area burial; most excavated tombs from the period instead used smaller square or rectangular blocking stones [[NOTE:kloner-rolling-stone]], set within a broader excavated tomb typology of kokh niches and arcosolia recesses [[NOTE:kloner-zissu-tomb-typology]]."
'N. T. WRIGHT' = "British New Testament scholar and bishop whose multi-volume Christian Origins and the Question of God project culminates in a lengthy historical argument that the specific, combined shape of the empty-tomb and appearance traditions — including the encounters narrated across John 20 — is best explained by something genuinely anomalous having occurred, against more skeptical visionary-experience readings of the same evidence [[NOTE:wright-resurrection-anomaly]]."
'GERD LUDEMANN' = "German New Testament scholar who argued, from a history-of-religions and psychological framework, that the resurrection appearance traditions originated as subjective visionary experiences tied to specific disciples' grief and guilt, rather than encounters with an objectively risen body [[NOTE:ludemann-psychological-experience]], representing one clear pole of the wider historical-critical range on how to read John 20's appearance narratives [[NOTE:ehrman-visionary-experience]] [[NOTE:allison-bereavement-visions]]."
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
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch20NodeId $id $sortKey
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
Seed-Entity "C. K. Barrett" "c-k-barrett" "character" "British New Testament scholar; his commentary on John identifies and addresses the 'Johannine Pentecost' of John 20:22 against Luke-Acts's separate Pentecost timeline."
Seed-Entity "D. Moody Smith" "d-moody-smith" "character" "American New Testament scholar whose Abingdon commentary on John represents the broad scholarly view that 20:30-31 was the Gospel's original ending, with chapter 21 a later appendix."
Seed-Entity "Urban C. von Wahlde" "urban-c-von-wahlde" "character" "New Testament scholar whose three-stage compositional model of the Fourth Gospel places chapter 21 in a later editorial layer than chapters 1-20."
Seed-Entity "Gerd Ludemann" "gerd-ludemann" "character" "German New Testament scholar who argued the resurrection appearance traditions originated as subjective, grief- and guilt-rooted visionary experiences rather than encounters with an objectively risen body."
Seed-Entity "D. A. Carson" "d-a-carson" "character" "New Testament scholar whose Pillar commentary on John and JBL article on John 20:31 argue the Gospel's textually disputed purpose clause favors a pastoral, confirming-the-believing reading over a purely evangelistic one."
Seed-Entity "Criterion of Embarrassment" "criterion-of-embarrassment" "vocabulary" "Historical-critical rule of thumb: a detail awkward for the transmitting community is less likely invented and more likely historically grounded, since fabrication would tend to remove such friction rather than introduce it."

$conn.Close()
Write-Host "DONE Chapter 20 (John)."
