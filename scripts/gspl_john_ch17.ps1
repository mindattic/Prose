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
$Ch17NodeId = [guid]"019FA96D-2AAE-75B5-89EC-8C7D54E10248"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'chytraeus-high-priestly-title' = @{ title='A sixteenth-century title, not the Gospel''s own'; body="David Chytraeus, In Evangelium Ioannis Enarratio (Wittenberg, 1568/1571 editions), commentary on John 17. The now-standard label 'High Priestly Prayer' for John 17 is not found anywhere in the Gospel's own text; it is traced by the modern commentary tradition to the sixteenth-century Lutheran theologian David Chytraeus, a student of Melanchthon at Wittenberg, who applied the phrase to the chapter's intercessory and consecratory character. See Raymond E. Brown, The Gospel According to John XIII-XXI, Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), 747, for the standard modern citation and discussion of the title's post-antiquity origin." }
'priestly-resonances-hagiazo' = @{ title='Consecration language and Levitical resonance'; body="Raymond E. Brown, The Gospel According to John XIII-XXI, Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), 746-751, appendix discussion on the chapter's priestly character. Brown surveys the case, associated especially with earlier scholars reading the chapter alongside Leviticus 16 and the Day of Atonement, that Jesus's repeated use of hagiazo ('sanctify,' 17:17, 17:19) and his intercessory posture for others cast him in a priest-like consecrating role; Brown treats the resonance as real but cautions that the Gospel never explicitly invokes priestly office or ritual here, so the label remains an inference from language rather than an authorial self-description." }
'bultmann-theological-testament' = @{ title="Bultmann: a composed theological summation"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Oxford: Basil Blackwell, 1971; German original 1941/1964), commentary on John 17, 486-513. Bultmann reads John 17 as the evangelist's own carefully constructed theological recapitulation of the entire Gospel's message, placed on Jesus's lips at the climactic close of the Farewell Discourse, rather than a word-for-word record of an overheard prayer; for Bultmann the chapter functions as a summary confession of faith in dramatic form, gathering themes (glory, unity, mission, knowledge of God) stated earlier in the Gospel's discourse material." }
'farewell-testament-genre-precedent' = @{ title="A closing prayer within a recognized genre"; body="George R. Beasley-Murray, John, Word Biblical Commentary vol. 36, 2nd ed. (Nashville: Thomas Nelson, 1999), 296-300, introductory discussion of John 17 within the Farewell Discourse. Beasley-Murray notes that ancient testament and farewell literature — the genre already shaping John 13-16, with Jacob's deathbed blessing of his sons in Genesis 49 as a scriptural precedent — conventionally closes with a formal prayer or blessing spoken by the departing figure, so the chapter's liturgical, treatise-like shape has a real ancient generic parallel even on a critical reading that treats it as composed rather than verbatim." }
'eternal-life-knowledge-definition' = @{ title="Eternal life defined as knowing, uniquely here"; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: Eerdmans / Leicester: Inter-Varsity Press, 1991), 552-556, commentary ad loc. John 17:3. Carson highlights that 17:3 is the only place in the Fourth Gospel where 'eternal life' receives an explicit definition, and that definition is cognitive and relational — 'that they know you, the only true God, and Jesus Christ whom you have sent' — a formulation notably different in emphasis from the Gospel's other uses of the phrase, which more often carry a future, resurrection-oriented sense (e.g., 5:28-29, 6:39-40, 11:24-26); Carson treats this as a genuine internal richness in John's usage rather than a contradiction to be explained away." }
'son-of-destruction-title' = @{ title="A rare title, its only other New Testament use"; body="C.K. Barrett, The Gospel According to St. John, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 17:12. Barrett notes that 'the son of destruction' (ho huios tes apoleias) applied to Judas at 17:12 is a strikingly rare construction, its only other occurrence anywhere in the New Testament being 2 Thessalonians 2:3, applied there to the eschatological 'man of lawlessness'; Barrett is careful to note the verbal parallel without asserting a direct literary dependence between the two texts, which belong to different authors, genres, and probable dates." }
'judas-scripture-fulfilled-allusion' = @{ title="Judas as the one lost 'that the Scripture might be fulfilled'"; body="Urban C. von Wahlde, The Gospel and Letters of John, Volume 2: Commentary on the Gospel of John, Eerdmans Critical Commentary (Grand Rapids: Eerdmans, 2010), commentary ad loc. John 17:12. Von Wahlde connects the prayer's statement that Jesus guarded the disciples and 'not one of them perished, except the son of destruction, that the Scripture might be fulfilled' back to the betrayal predictions and Psalm citations of John 13 (e.g., Psalm 41:9 at John 13:18), reading 17:12 as a retrospective theological gloss on Judas's defection rather than new narrative information." }
'future-believers-through-word' = @{ title="The Gospel's own acknowledgment of a later audience"; body="D. Moody Smith, John, Abingdon New Testament Commentaries (Nashville: Abingdon Press, 1999), 311-314, commentary ad loc. John 17:20-23. Smith identifies 17:20 — 'I do not ask for these only, but also for those who will believe in me through their word' — as one of the Fourth Gospel's clearest self-aware moments, an explicit textual acknowledgment that the prayer, and by extension the Gospel itself, is written with an eye toward generations of believers well beyond Jesus's original eyewitness circle, directly relevant to how the Gospel conceives its own intended audience and purpose (compare the stated purpose statement at 20:30-31)." }
'unity-prayer-ecumenical-reception' = @{ title="'That they may all be one' in the ecumenical movement"; body="John P. Meier, 'The Gospel of John: A Reader's Guide,' in The Oxford Handbook of the Gospels (Oxford: Oxford University Press, 2018), survey discussion of John 17's reception history; the underlying reception claim is also documented in standard ecumenical-movement histories tracing the influence of John 17:21-23 on the World Council of Churches (founded 1948) and on Vatican II's Decree on Ecumenism, Unitatis Redintegratio (1964), both of which cite the passage directly. The unity prayer is widely reported, across twentieth-century Christian unity literature, as the single most frequently cited New Testament passage in modern ecumenical documents; this note records that reception-historical fact without adjudicating the merits of the ecumenical movement itself." }
'jesus-completed-work-glory' = @{ title="'I have glorified you... having accomplished the work'"; body="Raymond E. Brown, The Gospel According to John XIII-XXI, Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), 752-756, commentary ad loc. John 17:4-5. Brown notes that Jesus's claim in 17:4 to have already 'accomplished the work' the Father gave him, spoken before the crucifixion narrated in chapters 18-19, reflects the Fourth Gospel's distinctive theology in which the cross itself is understood as the culminating moment of glorification rather than a defeat later reversed by resurrection — a Johannine emphasis distinct from the Synoptic Gospels' Gethsemane agony scenes, which this Gospel omits entirely." }
'not-praying-for-the-world' = @{ title="'I am not praying for the world'"; body="Craig S. Keener, The Gospel of John: A Commentary, Volume 2 (Peabody, MA: Hendrickson Publishers, 2003), 1057-1063, commentary ad loc. John 17:9. Keener addresses the apparent tension between Jesus's explicit statement that he prays only for the disciples 'given' to him and 'not for the world' (17:9) and the Gospel's better-known affirmation elsewhere that 'God so loved the world' (3:16); Keener reads 17:9 as a scoped, situational prayer for a specific missionary task rather than a blanket statement withdrawing divine love from the world at large, noting that the disciples' own mission in 17:18 is explicitly directed back into that same world." }
'bultmann-gnostic-redeemer-background' = @{ title="Bultmann's proposed background for the 'sent one' language"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G. R. Beasley-Murray (Oxford: Basil Blackwell, 1971), 490-494, excursus on the Father-Son 'sending' formula recurring through John 17 (17:3, 17:8, 17:18, 17:21, 17:23, 17:25). Bultmann proposed that the Gospel's dense repetition of sending/mission language across this chapter reflects an underlying Gnostic or proto-Gnostic redeemer-myth pattern he believed lay behind Johannine theology; this specific source-hypothesis has been substantially rejected by later mainstream scholarship (including D. Moody Smith and Raymond E. Brown) for lack of a clearly pre-Christian Gnostic redeemer myth, even though Bultmann's separate literary judgment that chapter 17 is a composed theological summation has remained widely influential independent of the rejected background theory." }
'name-revelation-exodus-background' = @{ title="'I have manifested your name' and the divine Name tradition"; body="Raymond E. Brown, The Gospel According to John XIII-XXI, Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), 755, 764-765, commentary ad loc. John 17:6, 17:26. Brown connects Jesus's statement that he has 'manifested your name' to the disciples with the Old Testament tradition of the revealed divine Name (Exodus 3:14, 6:3), reading the Johannine 'I am' sayings threaded through the Gospel as the narrative enactment of exactly this name-revelation the prayer now looks back on as completed." }
'textual-variant-those-you-have-given' = @{ title="A manuscript wrinkle in verse 11's Greek"; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece (28th ed., Stuttgart: Deutsche Bibelgesellschaft, 2012), ad loc. John 17:11. The clause rendered 'keep them in your name, which you have given me' shows a documented manuscript variation between hina (Papyrus 66, Codex Sinaiticus correctors) and ho (Codex Vaticanus, Codex Alexandrinus) governing how the relative clause attaches to 'name,' a minor but genuine textual-critical crux affecting whether the name itself or the disciples are the antecedent of the gift-verb; modern critical editions favor readings reconstructed from the strongest early witnesses without full unanimity in the secondary literature." }
'brown-structure-of-chapter-17' = @{ title="The prayer's three-part internal structure"; body="Raymond E. Brown, The Gospel According to John XIII-XXI, Anchor Bible vol. 29A (Garden City, NY: Doubleday & Company, 1970), 745-747, introductory outline of John 17. Brown's widely followed structural outline divides the chapter into Jesus's prayer for himself (17:1-5), his prayer for the disciples present with him (17:6-19), and his prayer for future believers (17:20-26), a three-part movement that most subsequent commentaries (Beasley-Murray, Carson, Keener, von Wahlde) adopt with only minor variation." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Jesus closes the Farewell Discourse by lifting his eyes to heaven and praying aloud, first for himself: the hour has come, so let the Father glorify the Son that the Son may glorify the Father, giving him authority over all flesh to give eternal life to those the Father has given him — and eternal life, Jesus says, means this: to know the only true God, and Jesus Christ whom he has sent. He reports having glorified the Father on earth, having accomplished the work given him to do, and asks now to be glorified with the glory he had with the Father before the world existed (17:1-5).

The chapter's traditional name — the "High Priestly Prayer" — is worth flagging before anything else, because it is not a label the Gospel itself ever uses. The phrase traces to the sixteenth-century Lutheran theologian David Chytraeus, a Wittenberg-trained student of Melanchthon, who applied it to the chapter in his own commentary on John [[NOTE:chytraeus-high-priestly-title]]. That doesn't make the title baseless — commentators have long noted real intercessory and consecratory resonances in the prayer's language, particularly Jesus's repeated use of "sanctify" (hagiazo, later in the chapter) alongside his stance of praying on behalf of others, which invites comparison to priestly mediation even though the text nowhere invokes priestly office or Levitical ritual outright [[NOTE:priestly-resonances-hagiazo]]. It is later interpretive nomenclature, not scripture's own word for itself, but not an interpretive reach either.

The prayer's whole register is worth pausing on before its content. It doesn't read like a spontaneous, overheard petition — it reads like a formal theological composition, dense with the Gospel's own signature vocabulary (glory, name, knowledge, sending, world) recapitulated in prayer form. Rudolf Bultmann's classic critical reading takes this formality seriously: he treats John 17 as the evangelist's own carefully constructed summation of the entire Gospel's message, placed on Jesus's lips at the climactic close of the Farewell Discourse, rather than a transcript remembered word for word [[NOTE:bultmann-theological-testament]]. That reading doesn't leave the prayer's form without ancient precedent, though — testament and farewell literature, the genre already shaping chapters 13 through 16, conventionally closes with exactly this kind of formal blessing or prayer from the departing figure, with Jacob's deathbed blessing of his sons in Genesis 49 as a scriptural example of the same closing convention [[NOTE:farewell-testament-genre-precedent]].

Verse 3's definition of "eternal life" deserves its own close look, because it is the only place in the entire Gospel where the phrase is explicitly defined, and the definition given is strikingly cognitive and relational rather than temporal: eternal life is knowing the only true God and Jesus Christ whom he sent [[NOTE:eternal-life-knowledge-definition]]. That sits in real, productive tension with other uses of "eternal life" across John, several of which carry a more future-facing, resurrection-oriented sense — a rising up "on the last day" (6:39-40, 54) or a passing "from death to life" tied to a coming hour (5:24-29). The Gospel doesn't resolve that tension for its reader; it simply uses the same phrase in more than one register, and 17:3 is the register that makes knowing God itself the substance of the life promised. The claim that Jesus has already "accomplished the work" given him (17:4), spoken before the crucifixion narrated two chapters later, reflects the same Johannine tendency to read the cross itself as the culminating act of glory rather than a defeat later reversed — notably, a reading that has no Gethsemane agony scene to work against, since John's Gospel omits that scene entirely [[NOTE:jesus-completed-work-glory]].
'@

$beat2 = @'
Jesus turns from himself to the disciples specifically: he has manifested the Father's name to these men the Father gave him out of the world, they have kept the Father's word, and now Jesus prays for them — not for the world, but for these — asking the Father to keep them in his name as he himself did while with them, guarding them so that none was lost except "the son of destruction," so that Scripture would be fulfilled. He asks that they be sanctified in the truth (the Father's word is truth), and sends them into the world exactly as the Father sent him (17:6-19).

"I am not praying for the world" (17:9) reads, on first pass, like a jarring contradiction of this same Gospel's most quoted line — "God so loved the world" (3:16) — and it's worth sitting with that friction rather than smoothing it over. The mainstream reading treats 17:9 as a scoped, situational prayer tied to a specific missionary handoff, not a blanket revocation of divine love for humanity at large: the very next verses send these same disciples back out into that world on a mission (17:18), which would be an odd thing to arrange for a world Jesus has just declared outside his concern [[NOTE:not-praying-for-the-world]].

"The son of destruction" (ho huios tes apoleias, 17:12) — the allusion to Judas, though Judas is not named in this verse — is a rare and loaded title worth flagging on its own. Its only other occurrence anywhere in the New Testament is 2 Thessalonians 2:3, where the same Greek construction names the eschatological "man of lawlessness"; the verbal parallel is genuinely striking, though the two texts differ in author, genre, and probable date, and the parallel shouldn't be overclaimed as a direct literary borrowing in either direction [[NOTE:son-of-destruction-title]]. Read against the betrayal material of chapter 13 — where Jesus already applies a citation from Psalm 41:9 to Judas's coming defection — 17:12's brief aside reads less like new information about Judas and more like a retrospective theological gloss, tying his loss back to a fulfillment-of-Scripture pattern already set in motion three chapters earlier [[NOTE:judas-scripture-fulfilled-allusion]].

The Father's "name," repeated across this section (17:6, 17:11, 17:12) and again at the chapter's close (17:26), carries its own background worth noting. Jesus's claim to have "manifested" that name to the disciples reaches back to the Old Testament tradition of God's revealed Name at the burning bush and beyond (Exodus 3:14, 6:3); the Gospel's own "I am" sayings, threaded through its earlier chapters, read on this account as the narrative enactment of exactly the name-revelation this prayer now treats as an accomplished, completed fact [[NOTE:name-revelation-exodus-background]]. And even a phrase as central as "keep them in your name, which you have given me" (17:11) carries a documented textual-critical wrinkle: the earliest Greek witnesses split on a small relative-pronoun form governing whether the "name" or the disciples are the intended recipient of the gift-verb, a genuine manuscript crux rather than a settled reading [[NOTE:textual-variant-those-you-have-given]].
'@

$beat3 = @'
Jesus then widens the prayer past the disciples in the room, praying explicitly "not for these only, but also for those who will believe in me through their word" — future generations of believers reached through the disciples' testimony. For all of them together he asks for unity: "that they may all be one, just as you, Father, are in me, and I in you, that they also may be in us, so that the world may believe that you have sent me." He gives them the glory the Father gave him so that they may be one as he and the Father are one, closing with the desire that they be with him to see his glory, that the world's failure to know the Father not diminish what the disciples do know, and that the very love the Father has for the Son may be in them, with Christ himself in them (17:20-26).

Verse 20 is one of the clearest places anywhere in this Gospel where the text steps outside its own narrative moment and acknowledges the reader directly. "Those who will believe in me through their word" names, in so many words, every later Christian reader who was never in the upper room — a rare and explicit self-awareness about audience that lines up with the Gospel's own stated purpose, given a few chapters later, that it was written so that readers "may believe" (20:30-31) [[NOTE:future-believers-through-word]]. It is one of the strongest textual signals that this Gospel understands itself as reaching well past its first eyewitness generation.

The unity prayer that follows — "that they may all be one" (17:21-23) — has had a reception history disproportionate to its length. It is documented, across twentieth-century Christian unity literature, as the single most frequently cited New Testament passage in modern ecumenical documents, invoked directly by both the World Council of Churches (founded 1948) and Vatican II's 1964 Decree on Ecumenism, Unitatis Redintegratio [[NOTE:unity-prayer-ecumenical-reception]]. That's a reception-historical fact worth recording plainly, without any claim here about the ecumenical movement's merits one way or the other.

Threaded through both this section and the two before it is a dense repetition of "sending" language — the Father sent the Son (17:3, 17:8, 17:18, 17:21, 17:23, 17:25), the Son sends the disciples (17:18) — and Bultmann, beyond his broader reading of the chapter as composed theology, proposed that this specific density reflected an underlying Gnostic or proto-Gnostic redeemer-myth pattern he believed stood behind Johannine theology generally. That particular source-hypothesis has been substantially set aside by later mainstream scholarship, including D. Moody Smith and Raymond E. Brown, for lack of a clearly pre-Christian Gnostic redeemer myth to draw on — even as Bultmann's separate, more durable judgment that the chapter is a composed theological summation rather than verbatim transcript has remained widely influential on its own terms, independent of the rejected background theory [[NOTE:bultmann-gnostic-redeemer-background]]. Commentators since — Beasley-Murray, Carson, Keener, von Wahlde among them — have generally followed Brown's now-standard three-part outline for the whole chapter: prayer for Jesus himself (17:1-5), prayer for the disciples present (17:6-19), and prayer for future believers (17:20-26) [[NOTE:brown-structure-of-chapter-17]].
'@

$beats = @($beat1, $beat2, $beat3)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'HIGH PRIESTLY PRAYER (JOHN 17)' = "The traditional name for the extended prayer Jesus offers to the Father at the close of the Farewell Discourse (17:1-26), covering his own glorification, his prayer for the disciples present with him, and his prayer for future believers. The title is not used by the Gospel text itself; it derives from the sixteenth-century Lutheran theologian David Chytraeus's commentary on John, later becoming the standard designation in the commentary tradition [[NOTE:chytraeus-high-priestly-title]]. The label rests on genuine intercessory and consecratory (hagiazo, ``sanctify'') resonances in the prayer's own language, even though the chapter never explicitly invokes priestly office or Levitical ritual [[NOTE:priestly-resonances-hagiazo]]."
'DAVID CHYTRAEUS' = "A sixteenth-century Lutheran theologian (1531-1600), a student of Philipp Melanchthon at Wittenberg, whose commentary on John's Gospel (In Evangelium Ioannis Enarratio) is the traced source of the now-standard title ``High Priestly Prayer'' for John 17 — a label the biblical text itself never uses [[NOTE:chytraeus-high-priestly-title]]."
'ETERNAL LIFE (JOHN 17:3)' = "The only place in the Fourth Gospel where ``eternal life'' receives an explicit definition: ``that they know you, the only true God, and Jesus Christ whom you have sent'' (17:3). The definition is cognitive and relational, in genuine internal tension with other Johannine uses of the same phrase that carry a more future, resurrection-oriented sense (5:28-29, 6:39-40) [[NOTE:eternal-life-knowledge-definition]]."
'SON OF DESTRUCTION (HO HUIOS TES APOLEIAS)' = "A rare title applied without naming him to Judas Iscariot at John 17:12, describing the one disciple lost ``that the Scripture might be fulfilled.'' Its only other New Testament occurrence is 2 Thessalonians 2:3, describing the eschatological ``man of lawlessness'' — a striking verbal parallel across two otherwise unrelated texts, not evidence of direct literary dependence [[NOTE:son-of-destruction-title]]. Commentators connect the verse back to the Psalm 41:9 citation applied to Judas's betrayal in John 13:18 [[NOTE:judas-scripture-fulfilled-allusion]]."
'UNITY PRAYER (JOHN 17:21-23, ''THAT THEY MAY ALL BE ONE'')' = "Jesus's prayer that all future believers ``may all be one,'' modeled on the Father-Son unity, ``so that the world may believe that you have sent me'' (17:21). The passage has an outsized reception history: it is documented as the single most frequently cited New Testament text in twentieth-century Christian ecumenical documents, invoked directly by the World Council of Churches (1948) and Vatican II's Decree on Ecumenism, Unitatis Redintegratio (1964) [[NOTE:unity-prayer-ecumenical-reception]]."
'FAREWELL DISCOURSE' = "The extended block of Jesus's final teaching to his disciples at the Last Supper (John 13-17), of which the High Priestly Prayer (John 17) forms the closing formal prayer. Critical scholarship situates the discourse's closing prayer within the recognized genre conventions of ancient testament and farewell literature, which regularly ends with a blessing or prayer spoken by the departing figure — Jacob's deathbed blessing in Genesis 49 is a scriptural precedent for the same closing convention [[NOTE:farewell-testament-genre-precedent]]."
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
    Add-BeatNode $Ch17NodeId $id $sortKey
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
Seed-Entity "High Priestly Prayer (John 17)" "high-priestly-prayer-john-17" "vocabulary" "Traditional (post-antiquity) name for Jesus's extended prayer at the close of the Farewell Discourse, John 17:1-26; title not used by the Gospel text itself."
Seed-Entity "David Chytraeus" "david-chytraeus" "character" "Sixteenth-century Lutheran theologian and Wittenberg student of Melanchthon whose commentary on John is the traced source of the title ``High Priestly Prayer.''"

$conn.Close()
Write-Host "DONE Chapter 17."
