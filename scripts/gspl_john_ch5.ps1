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
$Ch5NodeId = [guid]"019FA96C-5EA0-7E7A-B025-CF3F824AC465"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'bethesda-porticoes-archaeology' = @{ title='Five porticoes, confirmed by the trowel'; body="Urban C. von Wahlde, 'Archaeology and John's Gospel,' in Jesus and Archaeology, ed. James H. Charlesworth (Grand Rapids: Eerdmans, 2006), chapter on Jerusalem's Johannine sites; see also Jerome Murphy-O'Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on the Pools of Bethesda. Nineteenth- and twentieth-century excavation beside the Church of St. Anne in Jerusalem's Old City uncovered a genuine twin trapezoidal pool complex ringed by covered colonnades on four sides with a fifth crossing the wall dividing the two pools, matching John 5:2's description in a level of architectural detail no other Gospel scene receives, and confirming a topographical claim earlier generations of critics had assumed was theological invention." }
'bethesda-pre-excavation-skepticism' = @{ title='A detail once read as pure symbol'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 5:2. Brown notes that before the pool's rediscovery, commentators in the older critical tradition, working from the nineteenth-century assumption that no such structure was known to exist, proposed that the 'five porticoes' were a theological cipher for the five books of the Law rather than an architectural report; the subsequent excavation left that symbolic reading without an evidentiary foundation, even as it settled nothing about the healing narrative itself." }
'john-5-3b4-textual-variant' = @{ title="The angel who isn't in the earliest manuscripts"; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece (NA28), ad loc. John 5:3b-4; see also Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), comment ad loc. The clause describing an angel periodically stirring the water, with the first person into the pool afterward being healed, is absent from the earliest and best manuscripts, including Papyrus 66, Papyrus 75, Codex Sinaiticus, and Codex Vaticanus, and is judged by the critical consensus to be a later scribal explanation inserted to account for the invalid man's remark in 5:7 about someone else stepping down before him; modern critical translations including the NRSV, NIV, and ESV accordingly bracket, footnote, or omit the verse outright." }
'feast-of-the-jews-unspecified' = @{ title='Which feast? The text will not say'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 5:1; C.K. Barrett, The Gospel According to St John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 5:1. The Greek text reads simply 'a feast of the Jews' without the definite article, and commentators have proposed Passover, Pentecost, Tabernacles, and Purim as candidates without reaching consensus; the identification matters beyond antiquarian interest, since scholars reconstructing a chronology of Jesus's ministry from John's several named Passovers depend partly on how this unnamed feast is slotted into the sequence." }
'mishnah-shabbat-sabbath-law' = @{ title='Carrying a mat: the letter of later law'; body="The Mishnah, trans. Herbert Danby (Oxford: Oxford University Press, 1933), tractate Shabbat, chapters 1 and 7, listing carrying an object from a private domain to a public one among the thirty-nine categories of forbidden Sabbath labor; compare Jacob Neusner, The Mishnah: A New Translation (New Haven: Yale University Press, 1988), introduction, on the Mishnah's compilation under Rabbi Judah ha-Nasi around 200 CE. The specific prohibition the healed man is accused of violating in John 5:10 corresponds closely to this later Mishnaic carrying law, though that law reached its written, codified form more than a century after the Gospel's narrative setting, leaving open how much of it already applied by informal consensus in early first-century Judean practice." }
'sabbath-healing-life-threat-debate' = @{ title='When was healing on the Sabbath permitted?'; body="Craig S. Keener, The Gospel of John: A Commentary, 2 vols. (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 5:9-18. Keener surveys the developing rabbinic consensus, attested later but likely reflecting earlier practice, that Sabbath restrictions could be set aside to save a life, while healing a chronic but non-life-threatening condition, such as the invalid man's thirty-eight-year illness, remained a live point of dispute among Jewish teachers; the chapter's controversy accordingly sits inside a genuine, unresolved first-century legal debate rather than a violation every contemporary would have recognized as such without argument." }
'equal-with-god-agency-christology' = @{ title="'Equal with God': the charge and its logic"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray with R.W.N. Hoare and J.K. Riches (Oxford: Basil Blackwell, 1971), commentary ad loc. John 5:18-30; John Ashton, Understanding the Fourth Gospel, 2nd ed. (Oxford: Oxford University Press, 2007), chapter on Johannine christology. Bultmann and, following him, Ashton read the accusation that Jesus called God his own Father, making himself equal with God, as the narrative surface of a deeper claim the Gospel is building throughout the discourse: an agency christology in which the Son's authority derives entirely from, and remains dependent on, the Father who sent him, a formulation continuous with Jewish agency law and, in first-century Jewish ears, unmistakably provocative regardless." }
'son-can-do-nothing-of-himself' = @{ title="'The Son can do nothing of his own accord'"; body="John Ashton, Understanding the Fourth Gospel, 2nd ed. (Oxford: Oxford University Press, 2007), chapter on Johannine christology and Jesus as the Father's agent; D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 5:19-23. Both commentators note the discourse immediately qualifies the equality charge rather than embracing it outright: Jesus's claim in 5:19 that the Son does only what he sees the Father doing functions as a claim of derived, dependent authority rather than independent divine rivalry, a distinction central to how the Fourth Gospel's developing christology positions Jesus relative to Jewish monotheism without abandoning it." }
'realized-future-eschatology-tension' = @{ title="'Now is' and 'is coming': two eschatologies in one paragraph"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray with R.W.N. Hoare and J.K. Riches (Oxford: Basil Blackwell, 1971), commentary ad loc. John 5:24-29. Bultmann argued that John 5:28-29's description of a future hour when all who are in the tombs will hear the Son's voice and rise to a bodily judgment sits awkwardly beside 5:24-25's claim that the deciding hour is already present in hearing and believing, and proposed that the futurist verses were added by a later ecclesiastical redactor uncomfortable with John's fully realized eschatology; most subsequent commentators accept the tension as real while resisting Bultmann's redactional solution as unprovable, reading John instead as holding a present-and-future eschatology in deliberate, unresolved combination." }
'deuteronomy-18-prophet-like-moses' = @{ title='Moses, who wrote of me'; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 5:45-47. Carson connects Jesus's closing appeal, that Moses himself will accuse his hearers since he wrote of me, to the developing Second Temple expectation, rooted in Deuteronomy 18:15-19's promise of a future prophet like Moses, that Israel's own scriptures pointed forward to a coming authoritative figure; the discourse trades on that expectation without pausing to argue for it, assuming its hearers already know the text it invokes." }
'deuteronomy-19-15-witness-law' = @{ title='Two or three witnesses: the forensic frame'; body="C.K. Barrett, The Gospel According to St John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 5:31-40. Barrett situates the discourse's insistence on multiple witnesses, John the Baptist, Jesus's own works, the Father's testimony, and Scripture through Moses, against the background of Jewish forensic law codified at Deuteronomy 19:15, which requires the testimony of two or three witnesses to establish any legal matter; John 5:31, where Jesus concedes his own testimony about himself would not be valid alone, reads as a deliberate concession to that legal standard before the chapter supplies four independent witnesses to meet it." }
'john-baptist-first-witness' = @{ title='The first witness, already offstage'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 5:33-35. Brown notes that Jesus's reference to John the Baptist as a burning and shining lamp whose testimony the crowd once welcomed reaches back to the Baptist's own witness in John 1:19-34, even though the Baptist himself has already exited the Gospel's narrative by this point, compare 3:24; the discourse treats his earlier testimony as a standing legal deposit rather than something requiring his continued on-page presence." }
'works-second-witness-signs-theology' = @{ title='The works themselves as testimony'; body="D. Moody Smith, The Theology of the Gospel of John (Cambridge: Cambridge University Press, 1995), chapter on signs and testimony. Smith treats Jesus's appeal in John 5:36 to the works that the Father has given him to accomplish as a direct extension of the Fourth Gospel's broader signs theology, in which the healing just narrated functions simultaneously as a compassionate act and as forensic evidence entered into the developing dispute over Jesus's identity and authority." }
'father-and-scripture-witness' = @{ title="The Father's voice and the Scriptures that never mention seeing him"; body="D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), commentary ad loc. John 5:37-40. Smith reads the closing pair of witnesses, the Father's own testimony and the Scriptures, as the discourse's climactic and most difficult claims: Jesus asserts that his hearers have neither heard the Father's voice nor seen his form, despite Sinai traditions in which Israel did hear God speak, and that the Scriptures they diligently search testify to Jesus without their recognizing it, a claim about scriptural meaning that the Gospel asserts rather than demonstrates from any text the discourse actually quotes." }
'johannine-geographic-seam' = @{ title='Galilee, Jerusalem, Galilee: a seam in the map'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), Introduction, section on the Gospel's geographical and chronological problems. Brown documents the abrupt geography across chapters 4 through 6: chapter 4 ends with Jesus in Galilee (4:43-54), chapter 5 relocates him to Jerusalem for a feast with no travel notice given, and chapter 6 opens with Jesus again on the other side of the Sea of Galilee (6:1) as though chapter 5 had not intervened at all, a sequence many source-critical scholars read as evidence that the material has been rearranged from an earlier, more geographically coherent order." }
'bultmann-rearrangement-hypothesis' = @{ title="Bultmann's proposed reordering"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray with R.W.N. Hoare and J.K. Riches (Oxford: Basil Blackwell, 1971), Introduction and commentary at the chapter 4-7 seam. As part of his larger source-critical reconstruction of the Fourth Gospel, dividing it into a signs source, a revelation-discourse source, and a passion narrative, later stitched together by an ecclesiastical redactor, Bultmann proposed that chapter 6 originally followed directly on chapter 4, both set in Galilee, with chapter 5's Jerusalem material displaced from a different point in the underlying sequence, restoring a more natural Galilee-then-Jerusalem itinerary." }
'brown-critique-rearrangement' = @{ title='A real seam, an unrecoverable original'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), Introduction, section responding to displacement theories. Brown grants that the geographical seam between chapters 4, 5, and 6 is a genuine feature of the text needing explanation, but argues that no manuscript tradition anywhere preserves the alternate order Bultmann and others propose, making any specific rearrangement an unprovable modern reconstruction; Brown suggests instead that John's sources may simply have circulated as loosely sequenced units of tradition that the Gospel's final composition did not smooth into strict chronological or geographic order." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
John dates this episode only vaguely — "a feast of the Jews" — and Jesus goes up to Jerusalem for it, a detail commentators have spent considerable energy trying to pin to a specific holiday, without full success [[NOTE:feast-of-the-jews-unspecified]]. Near the Sheep Gate stands a pool, called in Hebrew Bethesda, with five covered porticoes, where a crowd of the disabled — blind, lame, and paralyzed — waits for the water to move; many manuscripts and older translations add that an angel of the Lord went down at certain times and stirred the water, and that whoever stepped in first afterward was healed of whatever disease he had, though the earliest and best-attested manuscripts do not contain that clause at all [[NOTE:john-5-3b4-textual-variant]]. Jesus singles out one man in the crowd who has been ill for thirty-eight years, learns he has no one to carry him into the water when it stirs, and simply tells him to rise, take up his mat, and walk. The man is healed on the spot, picks up his mat, and walks — and it is the Sabbath (5:1-9).

That single fact — the day — turns the healing into a legal incident before it can finish being a mercy. The Jewish authorities stop the healed man and tell him it is unlawful to carry his mat on the Sabbath; he answers that the man who healed him told him to carry it, and once Jesus finds him again in the Temple and identifies himself, the man reports Jesus to the authorities. John says this is what set the persecution in motion — "because he was doing these things on the Sabbath" (5:16). Jesus's own defense does not concede the point; he answers, "My Father is working still, and I am working," and the narrator reports that this made the authorities want to kill him all the more, "because he not only broke the Sabbath but also called God his own Father, making himself equal with God" (5:17-18) [[NOTE:equal-with-god-agency-christology]]. The chapter's remaining forty verses are, in effect, Jesus's answer to that second, larger charge.

The pool itself is one of the better-attested pieces of Johannine topography, and its history runs backward from where most Gospel archaeology runs. Nineteenth- and twentieth-century excavation beside the Church of St. Anne, in Jerusalem's Old City near the site of the ancient Sheep Gate, uncovered a genuine twin trapezoidal pool complex ringed by covered colonnades on four sides with a fifth crossing the wall dividing the two pools — a five-porticoed structure matching John's description in a level of architectural detail no other Gospel scene receives [[NOTE:bethesda-porticoes-archaeology]]. Before that excavation, the detail had nowhere to land: no pool answering to it was known to exist, and commentators in the older critical tradition proposed instead that the "five porticoes" were a theological cipher for the five books of the Law rather than an architectural report [[NOTE:bethesda-pre-excavation-skepticism]]. The dig did not resolve whether the healing happened; it did settle, in the direction opposite to what earlier skepticism expected, that the writer knew the site.

The Sabbath charge, by contrast, sits inside a genuinely unsettled area of first-century practice rather than a single fixed rule everyone would have applied the same way. Carrying an object from a private space into a public one is one of the categories of forbidden Sabbath labor later codified in the Mishnah, and while that written code postdates the Gospel's setting by more than a century, it likely reflects legal reasoning already circulating in less formal terms in the first century [[NOTE:mishnah-shabbat-sabbath-law]]. Whether healing itself broke the Sabbath was separately disputed: later rabbinic consensus allowed setting Sabbath restrictions aside to save a life, while healing a chronic, non-life-threatening condition — exactly the invalid man's situation, thirty-eight years in and clearly not about to die that day — remained a live question among Jewish teachers rather than something already settled one way [[NOTE:sabbath-healing-life-threat-debate]]. The authorities' objection, in other words, was not manufactured hostility dressed up as legal principle; it was one live position inside a real, contested debate.
'@

$beat2 = @'
Jesus's reply to the equal-with-God charge does not retreat from it so much as redefine it: "the Son can do nothing of his own accord, but only what he sees the Father doing" — whatever the Father does, the Son does likewise, and the Father, who loves the Son, shows him all that he himself is doing (5:19-20) [[NOTE:son-can-do-nothing-of-himself]]. From there the discourse escalates in scope rather than backing down: the Father has given all judgment to the Son, so that all may honor the Son as they honor the Father; whoever hears Jesus's word and believes has already passed from death to life, and "the hour is coming, and now is," when the dead will hear the Son's voice and live (5:21-25). Then, without any signaled shift, the same discourse adds a second, differently shaped promise: an hour is coming when all who are in the tombs will hear his voice and come out, some to a resurrection of life and some to a resurrection of judgment (5:28-29).

That juxtaposition — a decision already settled in the present tense of hearing and believing, right beside a future, bodily, general resurrection scene — is one of the most discussed cruxes in Johannine scholarship [[NOTE:realized-future-eschatology-tension]]. It matters for more than tidiness: John's Gospel is often characterized, relative to the Synoptics, as trading a future apocalyptic kingdom for a fully "realized" eschatology available now in believing; this passage is Exhibit A for why that characterization has to be qualified rather than simply asserted, since the same discourse holds both registers within a handful of verses.

The claim of derived authority in 5:19 is doing real theological work of its own. Rather than answering the charge of self-deifying blasphemy by disowning it, Jesus's response reframes the relationship: everything the Son does, he does because the Father shows him first — a dependent, sent, agency-based authority rather than a rival or independent divinity [[NOTE:equal-with-god-agency-christology]]. Whether first-century listeners would have found that qualification reassuring rather than more provocative — a human figure claiming this close and this total a derivation of divine authority — is exactly the dispute the rest of the Gospel keeps returning to.
'@

$beat3 = @'
Jesus turns from claim to evidence: "If I alone bear witness about myself, my testimony is not deemed true" (5:31), and the discourse marshals four independent witnesses in turn — John the Baptist, whose testimony the crowd once welcomed "for a while" (5:33-35); Jesus's own works, which testify that the Father has sent him (5:36); the Father himself, who has "himself borne witness" though the crowd has "never heard his voice nor seen his form" (5:37); and finally Scripture, and specifically Moses, whose writings Jesus says testify of him — "if you believed Moses, you would believe me, for he wrote of me" (5:39-47). The chapter ends there, mid-argument, with Jesus warning that Moses himself, in whom his accusers have set their hope, will be the one to accuse them.

The four-witness structure is not incidental scaffolding; it is built directly on Jewish forensic law. Deuteronomy 19:15 requires that no accusation stand on one witness alone — a matter is established only "by the mouth of two witnesses, or three witnesses" [[NOTE:deuteronomy-19-15-witness-law]]. Read against that statute, Jesus's opening concession in 5:31 is not humility for its own sake; it is an acknowledgment that his own uncorroborated testimony would not meet the legal bar, followed immediately by the production of witnesses sufficient to meet it. John the Baptist supplies the first: his testimony to Jesus, delivered back in chapter 1, is treated here as a standing deposition that does not require his continued presence in the story to remain in force [[NOTE:john-baptist-first-witness]]. The works — the healing just narrated chief among them — supply the second, read within the Gospel's broader pattern of treating miraculous signs as evidentiary rather than merely wondrous [[NOTE:works-second-witness-signs-theology]]. The Father's own testimony and the witness of Scripture close out the four, though both are asserted rather than demonstrated from any text the discourse actually quotes [[NOTE:father-and-scripture-witness]]. And Moses's role at the very end reaches back to a specific scriptural expectation: Deuteronomy 18:15-19 promises Israel a future prophet like Moses whom they must heed, a text the discourse leans on without pausing to argue for it, assuming its hearers already know what it means to say Moses "wrote of me" [[NOTE:deuteronomy-18-prophet-like-moses]].

One more puzzle belongs to this chapter, though it is a puzzle about the book's construction rather than its content. Chapter 4 ends with Jesus in Galilee; chapter 5, with no travel notice at all, relocates him to Jerusalem for the unnamed feast; and chapter 6 opens with Jesus crossing the Sea of Galilee as if chapter 5's entire Jerusalem interlude had not happened [[NOTE:johannine-geographic-seam]]. Rudolf Bultmann treated this seam as evidence that the Gospel's underlying sources had been reordered in transmission, proposing that chapter 6 originally followed directly on chapter 4 and that chapter 5 belongs somewhere else in the true sequence [[NOTE:bultmann-rearrangement-hypothesis]]. Raymond E. Brown's more cautious response has become something close to the mainstream position: the geographic seam is real and worth naming, but no manuscript anywhere preserves the alternate order Bultmann reconstructs, so any specific rearrangement remains an unprovable modern hypothesis rather than a recovered original text [[NOTE:brown-critique-rearrangement]]. What both agree on is the more basic point: John's chapters did not necessarily arrive in the order the finished Gospel now presents them.
'@

$beats = @($beat1, $beat2, $beat3)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'THE POOL OF BETHESDA' = "The five-porticoed pool complex near Jerusalem's Sheep Gate where Jesus heals a man ill for thirty-eight years (5:1-9). Nineteenth- and twentieth-century excavation beside the Church of St. Anne uncovered a genuine twin-pool structure matching John's architectural description in unusual detail, vindicating a feature earlier critics had assumed was symbolic invention [[NOTE:bethesda-porticoes-archaeology]] [[NOTE:bethesda-pre-excavation-skepticism]]."
'THE SICK MAN AT BETHESDA' = "An unnamed man, ill for thirty-eight years, healed by Jesus at the pool on a Sabbath (5:5-9); the text never gives his name or the specific nature of his condition beyond an inability to reach the water unaided. He reports Jesus to the Jewish authorities after being confronted for carrying his mat on the Sabbath, setting the chapter's controversy in motion [[NOTE:mishnah-shabbat-sabbath-law]] [[NOTE:sabbath-healing-life-threat-debate]]."
'SABBATH (SECOND TEMPLE LAW)' = "The seventh-day rest commanded in the Decalogue, elaborated by the first century into a body of case law about which activities counted as forbidden work. The man's carrying of his mat, and Jesus's act of healing itself, both become flashpoints in this chapter precisely because Sabbath boundaries were a live, contested area of Jewish legal reasoning rather than a single settled rule [[NOTE:mishnah-shabbat-sabbath-law]] [[NOTE:sabbath-healing-life-threat-debate]]."
'THE ANGEL AT THE POOL (JOHN 5:3B-4)' = "A textual passage, present in many later manuscripts and traditional English translations, describing an angel who periodically stirred the pool's water so that the first person into it afterward was healed. The clause is absent from the earliest and best-attested Greek manuscripts and is treated by the critical consensus as a later scribal explanation added to make sense of the healed man's remark in 5:7; modern critical translations bracket, footnote, or omit it [[NOTE:john-5-3b4-textual-variant]]."
'MOSES (ACCUSER AND WITNESS)' = "Israel's lawgiver, invoked twice in this chapter's discourse: first as part of the underlying Deuteronomic tradition of a future prophet like Moses that shapes the chapter's christological claims, and finally by name as a witness who will himself accuse Jesus's hearers, since he wrote of me (5:45-47) [[NOTE:deuteronomy-18-prophet-like-moses]]."
'TWO OR THREE WITNESSES (DEUTERONOMY 19:15)' = "The Torah's forensic principle that no legal matter may be established on a single person's testimony, requiring instead two or three independent witnesses. The discourse of John 5:31-47 is built around this principle, conceding that Jesus's testimony about himself would not meet the legal standard alone and supplying four further witnesses — John the Baptist, Jesus's works, the Father's testimony, and Scripture through Moses — to satisfy it [[NOTE:deuteronomy-19-15-witness-law]] [[NOTE:john-baptist-first-witness]] [[NOTE:works-second-witness-signs-theology]] [[NOTE:father-and-scripture-witness]]."
'MAKING HIMSELF EQUAL WITH GOD (JOHN 5:18)' = "The charge leveled against Jesus after he defends healing on the Sabbath by saying, My Father is working still, and I am working (5:17), a claim the narrative says intensified the authorities' hostility, since calling God his own Father was heard as making himself God's equal. The discourse that follows immediately qualifies rather than embraces that reading, presenting the Son's authority as entirely derived from and dependent on the Father [[NOTE:equal-with-god-agency-christology]] [[NOTE:son-can-do-nothing-of-himself]]."
'THE JOHANNINE DISPLACEMENT THEORY (JOHN 5-6)' = "A source-critical proposal, associated above all with Rudolf Bultmann, that the Fourth Gospel's underlying material has been rearranged from a more geographically coherent original order: chapter 4 ends in Galilee, chapter 5 relocates without explanation to Jerusalem, and chapter 6 resumes in Galilee as though chapter 5 had never happened. Raymond E. Brown and other commentators accept the seam as real while doubting any specific reconstruction can be proven, since no manuscript preserves an alternate sequence [[NOTE:johannine-geographic-seam]] [[NOTE:bultmann-rearrangement-hypothesis]] [[NOTE:brown-critique-rearrangement]]."
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
    Add-BeatNode $Ch5NodeId $id $sortKey
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
Seed-Entity "Sabbath (Second Temple law)" "sabbath-second-temple-law" "vocabulary" "The seventh-day rest and its developing case law about forbidden labor, the legal backdrop for the Bethesda healing controversy in John 5."
Seed-Entity "The Angel at the Pool (John 5:3b-4)" "angel-at-the-pool-john-5" "vocabulary" "The textually disputed clause describing an angel stirring the Bethesda water, absent from the earliest New Testament manuscripts and bracketed in modern critical translations."
Seed-Entity "Two or Three Witnesses (Deuteronomy 19:15)" "two-or-three-witnesses-deuteronomy-19-15" "vocabulary" "The Torah's forensic principle requiring multiple witnesses to establish a legal matter, underlying the fourfold witness structure of John 5:31-47."
Seed-Entity "Making Himself Equal with God (John 5:18)" "making-himself-equal-with-god" "vocabulary" "The charge brought against Jesus after he defends Sabbath healing by calling God his own Father, prompting the chapter's discourse on the Son's derived authority."
Seed-Entity "The Johannine Displacement Theory (John 5-6)" "johannine-displacement-theory" "vocabulary" "The source-critical proposal, associated with Rudolf Bultmann, that John's underlying material has been rearranged from a more geographically coherent original sequence across chapters 4 through 6."

$conn.Close()
Write-Host "DONE Chapter 5."
