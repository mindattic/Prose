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
$Ch16NodeId = [guid]"019FA96D-19B5-756C-9B0B-ABDE952E3C34"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'john-16-2-killing-beyond-expulsion' = @{ title="Expulsion escalates to killing"; body="Urban C. von Wahlde, The Gospel and Letters of John, Volume 2: Commentary on the Gospel of John, Eerdmans Critical Commentary (Grand Rapids: Eerdmans, 2010), commentary ad loc. John 16:1-2. Where John 9's aposynagogos material (already discussed in this commentary's ninth chapter) establishes synagogue expulsion as the Johannine community's lived experience, 16:2 adds a second, starker clause: the hour is coming when whoever kills you will think he is offering service to God. Von Wahlde and the broader redaction-critical tradition read this as the same community-history layer intensified, not a new theme, moving from social exclusion to a remembered fear of lethal violence." }
'martyrdom-precedents-stephen-james-zebedee-james-just' = @{ title="A fear with names attached"; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 2 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 16:2. Keener surveys the documented first-generation cases behind this verse's killing clause: Stephen, stoned by a Jerusalem crowd after a Sanhedrin hearing (Acts 7:54-60); James son of Zebedee, executed by Herod Agrippa I (Acts 12:2); and James the brother of Jesus, executed under the high priest Ananus, per Josephus's Antiquities 20.9.1, a citation already established in this commentary's discussion of John 7. Keener argues these are not distant abstractions but living memory within a generation of the Gospel's composition, making 16:2's killing clause less a hypothetical worst case than a retrospective naming of what had, in fact, already happened to specific known people." }
'life-setting-retrojection-vaticinium' = @{ title="Whose voice, whose hindsight"; body="Bart D. Ehrman, The New Testament: A Historical Introduction to the Early Christian Writings, 6th ed. (New York: Oxford University Press, 2015), chapter on the Gospel of John and the Johannine community. Ehrman represents the mainstream historical-critical judgment that predictive sayings this specific, and this thoroughly matched to known later events, are best explained as vaticinium ex eventu, prophecy after the fact, composed or sharpened by the Gospel's writer using the community's own remembered persecution to voice warnings back into Jesus's mouth decades earlier. Ehrman treats this as a literary and pastoral strategy, framing the community's trauma as foreseen and therefore not faith-destroying, rather than as a transcript of exact words spoken before any of it occurred." }
'paraclete-forensic-triad-16-8-11' = @{ title="The Advocate takes the stand"; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), commentary ad loc. John 16:8-11; see also George R. Beasley-Murray, John, Word Biblical Commentary vol. 36, 2nd ed. (Nashville: Thomas Nelson, 1999), ad loc. This chapter's description of the Paraclete convicting the world concerning sin and righteousness and judgment extends the courtroom and advocate sense of the term already flagged in this commentary's fourteenth chapter; here the forensic role is made explicit and specific, with the Paraclete cast as prosecuting counsel against the world (kosmos) rather than defense counsel for the disciples, a role reversal within the same legal metaphor." }
'sin-righteousness-judgment-interpretive-debate' = @{ title="What exactly is the world convicted of"; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 16:9-11; compare C.K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), ad loc. Jesus glosses each term in a way commentators still debate rather than settling: sin is defined as not believing in Jesus, righteousness as Jesus's vindication by his return to the Father, and judgment as the ruler of this world already having been judged. Carson and Barrett agree the passage argues for a christologically redefined sense of each term rather than a generic moral or legal meaning, though they differ on how far to press the courtroom imagery once the definitions are supplied." }
'spirit-hears-and-declares-dependent-mode' = @{ title="A Spirit that only relays"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray (Philadelphia: Westminster Press, 1971), commentary ad loc. John 16:13-15. Bultmann reads the Spirit's described mode of operation here, speaking whatever he hears rather than on his own authority, and thereby glorifying Jesus, as a deliberate structural echo of the Son's own earlier claimed dependence on the Father (5:19, 5:30, 7:16). The pattern extends the Gospel's chain-of-derived-authority one link further: the Father authorizes the Son, the Son authorizes the Spirit, and each stage of the chain insists on its own non-autonomy as the guarantee of its trustworthiness." }
'little-while-enigmatic-double-saying' = @{ title="A little while, said twice, understood by no one"; body="C.K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 16:16-19; see also George R. Beasley-Murray, John, Word Biblical Commentary vol. 36, 2nd ed. (Nashville: Thomas Nelson, 1999), ad loc. Barrett catalogs the range of proposed referents for the paired little while clauses, including death and resurrection, crucifixion and Parousia, and departure and Pentecost, and concludes the saying's deliberate opacity is itself part of its design: the disciples' confusion, staged explicitly in the text (we do not know what he means, 16:18), models the reader's own interpretive difficulty rather than resolving it." }
'john-16-16-textual-variant' = @{ title="A phrase that migrated one verse early"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: United Bible Societies, 1994), ad loc. John 16:16; Novum Testamentum Graece, 28th ed. (Nestle-Aland; Stuttgart: Deutsche Bibelgesellschaft, 2012), textual apparatus ad loc. A number of manuscripts append the clause because I go to the Father to the end of 16:16, matching the disciples' own paraphrase of the saying one verse later in 16:17. Metzger judges this a secondary scribal expansion, most plausibly triggered by a copyist's eye jumping ahead to the nearly identical wording in verse 17 and pulling its extra clause backward; the oldest and best manuscripts lack the addition in verse 16 itself." }
'birth-pangs-isaiah-background' = @{ title="An old image for the world's labor"; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), commentary ad loc. John 16:21. Brown traces the labor-pangs-into-joy image Jesus uses here to a well-established Hebrew Bible pattern for describing eschatological distress that gives way to restoration: Isaiah 26:17-19 compares national anguish to a woman in labor, and Isaiah 66:7-14 extends the image into Zion's sudden, joyful, painless delivery of a people. The Fourth Gospel's use of the figure draws on this older prophetic image cluster rather than inventing a new metaphor for the disciples' coming grief-to-joy transition." }
'hevlei-mashiach-later-rabbinic-phrase' = @{ title="A phrase that comes later than the image"; body="Hermann L. Strack and Paul Billerbeck, Kommentar zum Neuen Testament aus Talmud und Midrasch, vol. 1 (Munich: C.H. Beck, 1922), excursus on the birth pangs of the Messiah (hevlei mashiach) at Matthew 24:8; compare Dale C. Allison Jr., Jesus of Nazareth: Millenarian Prophet (Minneapolis: Fortress Press, 1998), on eschatological woes traditions. Strack-Billerbeck's catalog of rabbinic parallels locates the fixed technical phrase hevlei mashiach in sources no earlier than the Tannaitic and Amoraic periods, later than the New Testament itself; Allison and the wider comparative scholarship caution that the underlying image, eschatological distress figured as labor preceding birth, is considerably older and shared across Second Temple Jewish apocalyptic writing, even though the crisp rabbinic label for it postdates John's Gospel. The two should not be treated as identical: John 16:21 draws on the older image pattern, not on a fully formed rabbinic doctrine that did not yet exist in its later fixed form." }
'ask-father-directly-no-mediation' = @{ title="No longer asking through an intermediary"; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans, 1991), commentary ad loc. John 16:23-26. Carson notes a real development beyond this Gospel's earlier ask in my name promises: here Jesus states plainly that on that day the disciples will ask the Father directly, and explicitly denies that he himself will need to ask the Father on their behalf, since the Father already loves them. Carson reads this less as a doctrinal correction of earlier verses and more as a description of a changed relational access made possible only after Jesus's departure and glorification; direct filial standing before the Father becomes available to the disciples in a way it was not before." }
'father-loves-you-directly-bultmann' = @{ title="A love that does not need winning"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray (Philadelphia: Westminster Press, 1971), commentary ad loc. John 16:26-27. Bultmann treats the Father himself loves you as a deliberate corrective against any reading in which Jesus's death or intercession functions to change the Father's disposition toward the disciples from wrath to favor. In Bultmann's account the Father's love is prior and unconditioned, and Jesus's role is revelatory, making that love known, rather than transactional, purchasing or unlocking it; the reading is widely cited as a caution against popular but exegetically unsupported angry-Father, pleading-Son paraphrases of Johannine atonement language." }
'plain-speech-vs-figures-parrhesia-paroimia' = @{ title="Riddles, then plainly"; body="C.K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 16:25, 29; see also D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), ad loc. Barrett and Smith both note the Greek contrast at work: Jesus has been speaking in paroimia, figurative and riddling speech, the same word used of the shepherd discourse in John 10:6, and now promises a coming shift to parrhesia, plain and open speech. The disciples' response in 16:29, now you are speaking plainly, registers that shift inside the narrative itself, though commentators note the irony that their sense of new clarity arrives moments before Jesus predicts they will scatter and fail to understand what is actually about to happen to him." }
'embarrassment-criterion-scattering-prediction' = @{ title="A prediction no later church would invent"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), discussion of the criterion of embarrassment. Meier's standard treatment of this criterion, material unlikely to have been invented by a later church because it reflects poorly on figures the church later venerated, applies naturally to Jesus's prediction here that his own closest followers will be scattered, each to his own home, abandoning him. A later community with reason to burnish the Twelve's reputation had no obvious motive to invent or retain a saying in which Jesus himself foretells their failure; its survival in the text is more easily explained as remembered and preserved than as flattering fiction." }
'disciples-premature-confidence-irony' = @{ title="Confidence, timed badly"; body="C.K. Barrett, The Gospel According to St John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 16:29-32. Barrett draws attention to the deliberate literary placement here: the disciples confidently declare, now we know that you know all things, by this we believe that you came from God (16:29-30), and Jesus's reply is not agreement but a prediction that they are about to scatter and abandon him within the hour. Many commentators read this juxtaposition as intentionally ironic, a Johannine narrative habit of letting characters speak their most confident faith claims at precisely the moment the text is about to undercut them, rather than as a straightforward affirmation of the disciples' newly professed understanding." }
'zechariah-scattering-echo-no-citation' = @{ title="A scattered-flock image, unquoted"; body="Raymond E. Brown, The Gospel According to John (XIII-XXI), Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), commentary ad loc. John 16:32. Jesus's prediction that the disciples will be scattered, each to his own home, shares its basic image with Zechariah 13:7, strike the shepherd, and the sheep will be scattered, a verse the Synoptic tradition quotes explicitly at Gethsemane (Mark 14:27; Matthew 26:31). Brown notes that John's version of the scattering prediction, unlike the Synoptics', does not cite Zechariah by name or wording here, an allusive rather than citational use of the same underlying prophetic image, consistent with the Fourth Gospel's general tendency to handle certain traditions independently of the Synoptic pattern even where the content overlaps." }
'not-alone-father-with-me-dereliction-contrast' = @{ title="No cry of abandonment in this Gospel"; body="Raymond E. Brown, The Death of the Messiah: From Gethsemane to the Grave, 2 vols. (New York: Doubleday, 1994), comparative discussion of the Synoptic and Johannine passion narratives. Jesus's assurance here, yet I am not alone, for the Father is with me, anticipates a broader and much-discussed divergence Brown traces through to the crucifixion itself: the Synoptic Jesus cries out in apparent abandonment, my God, my God, why have you forsaken me (Mark 15:34; Matthew 27:46), while John's Gospel contains no such cry anywhere in its passion account, replacing it with a serene it is finished (19:30). The Farewell Discourse's repeated insistence on unbroken Father-Son unity, voiced here in 16:32, is best read as preparing that distinctly Johannine christological trajectory rather than as an incidental aside." }
'i-have-overcome-the-world-realized-eschatology' = @{ title="A victory claimed before it happens"; body="C.H. Dodd, The Interpretation of the Fourth Gospel (Cambridge: Cambridge University Press, 1953), discussion of Johannine realized eschatology. Dodd's classic account of John's tendency to relocate future, still-pending events into a present or already-accomplished frame applies directly to the chapter's closing claim, I have overcome the world (16:33), a perfect-tense victory declared before the arrest, trial, and crucifixion that would, in the narrative's own terms, have to happen first. Dodd reads this not as a chronological error but as a deliberate theological stance: from the standpoint of the Gospel's own confessed faith, the outcome of the passion is already settled, so the tense of victory can run ahead of the plot." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Jesus tells the disciples plainly what is coming: they will be put out of synagogues, and a time is coming when whoever kills them will think he is offering service to God. He has told them this, he says, so that when the hour comes they will remember he warned them, and so they will not fall away. He had not said these things from the beginning because he was with them; now that he is going to the one who sent him, he sends the Paraclete instead, whose coming depends on his own departure, and whose arrival will convict the world concerning sin, righteousness, and judgment, guide the disciples into all truth, speak only what he hears, declare the things that are coming, and glorify Jesus by taking what is his and making it known (16:1-15).

The synagogue-expulsion clause is not new ground for this commentary; it restates, in Jesus's own predictive voice, the aposynagogos reality already established in the discussion of John 9's healed blind man and his interrogated parents. What 16:2 adds is starker: not just exclusion from the assembly but killing, framed as an act its perpetrators will believe honors God. Mainstream redaction-critical scholarship reads this as the same community-history layer intensifying rather than a new theme introduced from nowhere — social exclusion escalating, in the text's own telling, to a remembered fear of lethal violence [[NOTE:john-16-2-killing-beyond-expulsion]].

That fear had names attached to it well within a generation of this Gospel's composition. Stephen was stoned by a Jerusalem crowd after a Sanhedrin hearing (Acts 7:54-60). James son of Zebedee was executed by Herod Agrippa I (Acts 12:2). And James the brother of Jesus was executed under the high priest Ananus, a killing this commentary has already cited from Josephus's Antiquities in its discussion of John 7 [[NOTE:martyrdom-precedents-stephen-james-zebedee-james-just]]. Read against that record, 16:2's warning looks less like a hypothetical worst case Jesus is imagining and more like a retrospective naming of what, by the time this Gospel reached its final form, had already happened to specific, known people. The mainstream historical-critical judgment is that a prediction this precisely matched to known later events is most plausibly explained as the Gospel's writer voicing the community's own remembered persecution back into Jesus's mouth decades earlier — not a claim that Jesus said nothing like this, but a recognition that the specificity here likely owes something to hindsight [[NOTE:life-setting-retrojection-vaticinium]].

The Paraclete material returns to ground already partly covered when this term first appeared, but it sharpens considerably here. Where the earlier occurrences established the word's courtroom and advocate connotations, this chapter makes the forensic role explicit: the Paraclete will convict the world concerning sin, righteousness, and judgment, cast as something closer to prosecuting counsel against the world than defense counsel for the disciples — a role reversal within the same legal metaphor [[NOTE:paraclete-forensic-triad-16-8-11]]. What each of those three terms actually means once glossed is itself a live interpretive question: sin defined as unbelief in Jesus, righteousness as Jesus's vindication through return to the Father, judgment as the ruler of this world already condemned. Commentators broadly agree the passage is redefining each term christologically rather than using it in a generic moral or legal sense, though they differ on how literally to press the courtroom imagery afterward [[NOTE:sin-righteousness-judgment-interpretive-debate]].

The description of the Spirit's operating mode in 16:13-15 — speaking only what he hears, not on his own authority, and thereby glorifying Jesus — echoes a pattern this Gospel has already used for the Son's relationship to the Father: the Son does nothing of his own accord, only what he sees the Father doing. Here the chain of derived, non-autonomous authority extends one link further, from Father to Son to Spirit, with each stage's claimed dependence functioning as its own guarantee of trustworthiness rather than a diminishment [[NOTE:spirit-hears-and-declares-dependent-mode]].
'@

$beat2 = @'
Jesus says something the disciples cannot parse: "A little while, and you will see me no longer; and again a little while, and you will see me." They ask each other what he means, and whether it has to do with his talk of going to the Father, and admit outright that they do not know what he is talking about. Jesus answers with an extended analogy: a woman in labor has sorrow, because her hour has come, but once the child is born she no longer remembers the anguish, for joy that a human being has been born into the world. So it is with the disciples now — sorrow, but a sorrow Jesus promises will turn to a joy no one can take from them. On that day, he says, they will ask nothing more of him; whatever they ask the Father in his name, the Father will give them, and their joy will be made full (16:16-24).

The paired "little while" clauses are genuinely opaque, and the text stages that opacity rather than resolving it: the disciples say outright that they do not know what he means. Commentators have proposed death-and-resurrection, crucifixion-and-Parousia, and departure-and-Pentecost as the intended referents, without settling on one; the ambiguity looks deliberate, modeling the reader's own interpretive difficulty rather than clearing it up [[NOTE:little-while-enigmatic-double-saying]]. The manuscript tradition adds its own small wrinkle at exactly this point: a number of witnesses tack "because I go to the Father" onto the end of verse 16, matching the disciples' paraphrase one verse later, but the oldest and best manuscripts lack it there — a plausible case of a scribe's eye jumping ahead and pulling an extra clause backward [[NOTE:john-16-16-textual-variant]].

The labor-pangs-into-joy image is not new to Jesus's audience even if it is new to this discourse; it draws on an established Hebrew Bible pattern for eschatological distress giving way to restoration — Isaiah 26:17-19 compares national anguish to a woman in labor, and Isaiah 66:7-14 extends the figure into Zion's sudden, painless delivery of a people [[NOTE:birth-pangs-isaiah-background]]. It is worth being precise about what that background does and doesn't establish. Later rabbinic literature has a fixed technical phrase for this pattern, "the birth pangs of the Messiah" (hevlei mashiach), but the earliest clear attestation of that specific phrase comes from sources later than the New Testament itself. The underlying image — distress figured as labor preceding birth — is considerably older and shared across Second Temple Jewish apocalyptic writing; the crisp rabbinic label for it is not. The two should not be treated as one and the same: Jesus's analogy here draws on the older image-pattern, not on a fully formed doctrine that did not yet exist in its later fixed rabbinic form [[NOTE:hevlei-mashiach-later-rabbinic-phrase]].

The promise that follows — asking the Father directly, in Jesus's name, rather than through him — marks a real development beyond this Gospel's earlier "ask in my name" sayings. Jesus explicitly denies that he himself will need to ask the Father on the disciples' behalf, since the Father already loves them. The shift reads less like a correction of anything said earlier and more like a description of a changed relational access that becomes available only after Jesus's departure and glorification: a direct filial standing before the Father that was not available to the disciples before [[NOTE:ask-father-directly-no-mediation]].
'@

$beat3 = @'
Jesus tells them he has been speaking in figures of speech, but a time is coming when he will no longer speak in figures but will tell them plainly of the Father. On that day they will ask in his name, and he does not say he will ask the Father on their behalf, for the Father himself loves them, because they have loved Jesus and believed he came from God. The disciples respond that now he is speaking plainly, not in any figure of speech; now they know that he knows all things and does not need anyone to question him, and by this they believe he came from God. Jesus asks whether they now believe, then predicts that the hour is coming, indeed has come, when they will be scattered, each to his own home, and will leave him alone — "yet I am not alone, for the Father is with me." He closes: "I have said these things to you, that in me you may have peace. In the world you will have tribulation. But take heart; I have overcome the world" (16:25-33).

Jesus's assurance that "the Father himself loves you" is worth pausing on, because it forecloses a reading many popular presentations of the atonement lean toward: that Jesus's death or intercession is what changes the Father's disposition from wrath to favor. Read this way, the Father's love is prior and unconditioned; Jesus's role is to make that love known, not to purchase or unlock it — a distinction commentators treat as a needed corrective to an "angry Father, pleading Son" framing that this text does not support [[NOTE:father-loves-you-directly-bultmann]]. The disciples' own response registers a real shift in the Greek terms at work: Jesus has been speaking in paroimia, figurative and riddling speech (the same word used of the shepherd discourse in chapter 10), and now, in their perception, has moved to parrhesia, plain and open speech. Their declaration — "now you are speaking plainly" — marks that shift inside the narrative itself [[NOTE:plain-speech-vs-figures-parrhesia-paroimia]].

But the timing of that declaration is where the chapter turns pointed. The disciples confidently proclaim full understanding and belief in almost the same breath that Jesus predicts they are about to scatter and abandon him. Many commentators read this juxtaposition as deliberately ironic — a recognizable Johannine habit of letting characters voice their most confident faith claims at precisely the moment the narrative is about to undercut them, rather than a straightforward validation of what the disciples think they now understand [[NOTE:disciples-premature-confidence-irony]]. It is also, on strictly historical grounds, an unlikely thing for a later church to have invented: a saying in which Jesus himself foretells that his own closest followers will fail and scatter does nothing to flatter the Twelve's reputation, which is exactly the kind of embarrassing detail historical-critical method treats as more probably remembered than fabricated [[NOTE:embarrassment-criterion-scattering-prediction]].

The scattering image itself has scriptural roots the text does not spell out. "Scattered, each to his own home" shares its basic shape with Zechariah 13:7's shepherd struck and sheep scattered, a verse the Synoptic tradition quotes by name at Gethsemane. John's version uses the same underlying image without citing Zechariah here, an allusion rather than a quotation, consistent with this Gospel's habit of handling shared traditions independently of the Synoptic pattern even where the content overlaps [[NOTE:zechariah-scattering-echo-no-citation]]. And Jesus's accompanying claim, "yet I am not alone, for the Father is with me," anticipates a divergence this commentary will need to return to at the crucifixion itself: the Synoptic Jesus cries out in apparent abandonment on the cross, a cry John's passion narrative contains nowhere, replacing it instead with a serene "it is finished." The unbroken Father-Son unity insisted on here is best read as preparing that distinctly Johannine trajectory rather than as incidental comfort-language [[NOTE:not-alone-father-with-me-dereliction-contrast]].

The chapter's closing line, "I have overcome the world," is stated in the perfect tense before the arrest, trial, and crucifixion that would, by the narrative's own logic, have to happen first. That is not a chronological slip; it is characteristic of this Gospel's habit of relocating future, still-pending outcomes into an already-accomplished frame — from the standpoint of the Gospel's own confessed faith, the passion's outcome is treated as already settled, so the tense of victory is allowed to run ahead of the plot [[NOTE:i-have-overcome-the-world-realized-eschatology]].
'@

$beats = @($beat1, $beat2, $beat3)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'BIRTH PANGS OF THE MESSIAH (HEVLEI MASHIACH)' = "The image Jesus uses in 16:21 of a woman's labor-sorrow turning to joy at a child's birth, applied to the disciples' coming grief-to-joy transition. The figure draws on an older Hebrew Bible pattern for eschatological distress preceding restoration (Isaiah 26:17-19; Isaiah 66:7-14) [[NOTE:birth-pangs-isaiah-background]]. The fixed rabbinic technical phrase for this pattern, hevlei mashiach, is attested only in sources later than the New Testament; the underlying image is older and shared across Second Temple Jewish apocalyptic writing, and the two should not be treated as identical [[NOTE:hevlei-mashiach-later-rabbinic-phrase]]."
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
    Add-BeatNode $Ch16NodeId $id $sortKey
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
Seed-Entity "Birth Pangs of the Messiah (Hevlei Mashiach)" "birth-pangs-of-the-messiah-hevlei-mashiach" "vocabulary" "Labor-into-joy image (John 16:21) rooted in Isaiah 26 and 66; the fixed rabbinic phrase hevlei mashiach is attested later than the New Testament, though the underlying image is older."

$conn.Close()
Write-Host "DONE Chapter 16."
