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
$Ch16NodeId = [guid]"019FA06D-89CE-7DFF-871E-E5AACFEA94DA"
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"

$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06D-89CE-7DFF-871E-E5AACFEA94DA' AND bn.IsEnabled=1")
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxChapterSortKey=$maxChapterSortKey MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'aramaic-kepha-single-word' = @{ title="Kepha: one word, not two"; body="Joseph A. Fitzmyer, `"Aramaic Kepha and Peter's Name in the New Testament,`" in Text and Interpretation: Studies in the New Testament Presented to Matthew Black, ed. Ernest Best and R. McL. Wilson (Cambridge: Cambridge University Press, 1979), 121-132; reprinted in Fitzmyer, To Advance the Gospel: New Testament Studies (New York: Crossroad, 1981). Fitzmyer's philological study establishes that Jesus, speaking Aramaic rather than Greek in ordinary life, would have used the single noun kepha (`"rock`") for both Simon's new name and the wordplay's second term, reconstructing the underlying saying as something close to `"You are Kepha, and on this kepha I will build,`" with no distinction at all between the two occurrences." }
'petros-petra-greek-grammatical-gender' = @{ title="Why Greek needed two different words"; body="R. T. France, The Gospel of Matthew, New International Commentary on the New Testament (Grand Rapids: William B. Eerdmans Publishing Co., 2007), commentary ad loc. Matthew 16:18. France explains the Greek text's use of two different forms — the masculine Petros for Simon's new name and the feminine petra for `"rock`" — as a translation necessity rather than a deliberate distinction: Greek grammar requires a man's name to carry a masculine ending, so a masculine Petros was coined from the ordinary feminine noun petra, producing an apparent difference in the Greek that the underlying Aramaic never had." }
'peter-in-nt-ecumenical-scholarly-consensus' = @{ title="A joint Protestant-Catholic-Lutheran verdict"; body="Raymond E. Brown, Karl P. Donfried, and John Reumann, eds., Peter in the New Testament: A Collaborative Assessment by Protestant and Roman Catholic Scholars (Minneapolis: Augsburg Publishing House; New York: Paulist Press, 1973). This jointly authored study, produced by a Lutheran-Catholic dialogue commission specifically convened to work through disputed New Testament texts across confessional lines, concludes that the Petros/petra distinction is very likely an artifact of Greek grammar rather than a meaningful part of Jesus's own wordplay, since the two words would not have existed as separate terms in the Aramaic he is thought to have spoken day to day. The volume also documents that this same verse was, for centuries before such joint study became possible, a live point of Protestant-Catholic polemic, with some readings pressing the grammatical gender difference to argue that Peter himself was not the `"rock`" in view." }
'matthew-16-17-19-special-material' = @{ title="Verses with no parallel"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 16:17-19. Davies and Allison note that Mark's version of the same Caesarea Philippi scene (Mark 8:27-30) and Luke's (Luke 9:18-21) both include Peter's confession but neither includes Jesus's naming of Peter as the rock, the keys of the kingdom, or the binding-and-loosing grant — material found in Matthew alone. Source-critical scholars generally classify verses 17-19 as special Matthean material (conventionally labeled `"M`"), a real, checkable feature of the passage's transmission history, distinct from any question about what the material means theologically." }
'isaiah-22-eliakim-key-of-david' = @{ title="A steward's key, six centuries earlier"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 16:19. Davies and Allison trace Jesus's promise of `"the keys of the kingdom of heaven`" to Isaiah 22:15-22, where the prophet announces that the royal steward Eliakim son of Hilkiah will be given `"the key of the house of David`" on his shoulder, with authority to open what no one can shut and shut what no one can open. That is a genuine, attested ancient Near Eastern image of a king's chief household officer holding delegated administrative authority over the palace, not a metaphor original to the Gospel." }
'caesarea-philippi-pan-votive-inscriptions' = @{ title="Names carved into the rock"; body="Zvi Uri Ma'oz, excavation reports on the Sanctuary of Pan at Caesarea Philippi/Banias, published in Excavations and Surveys in Israel, vols. 13 and 15 (Jerusalem: Israel Antiquities Authority, 1993 and 1996). Ma'oz's excavations of the votive niches cut into the cliff face beside the Pan grotto recovered Greek dedicatory inscriptions naming the god Pan and his consort Echo directly, including one dedication carved by a priest named Victor son of Lysimachos. That is physical, datable, on-site textual evidence of an operating cult establishment at the exact spot named in this chapter — worshippers' own carved words surviving in the same cliff face, supplementing rather than repeating the site's already-established Herodian building history." }
'luz-confession-rebuke-literary-tension' = @{ title="Blessed, then rebuked, in the same breath"; body="Ulrich Luz, Matthew 8-20: A Commentary, Hermeneia, trans. James E. Crouch, ed. Helmut Koester (Minneapolis: Fortress Press, 2001), commentary ad loc. Matthew 16:21-23, and excursus `"Peter in the Gospel of Matthew.`" Luz treats the juxtaposition as a deliberate structural feature of the passage rather than an accident of composition: the same Peter who is blessed for Spirit-given insight and handed the keys of the kingdom at 16:17-19 is, six verses later, called `"Satan`" and a `"stumbling block`" for resisting the passion prediction at 16:23 — the sharpest reversal of any single figure's standing across so short a span anywhere in Matthew's narrative." }
'satan-generic-adversary-term' = @{ title="An epithet before it was a name"; body="Peggy L. Day, An Adversary in Heaven: satan in the Hebrew Bible, Harvard Semitic Monographs 43 (Atlanta: Scholars Press, 1988). Day's lexical study establishes that the Hebrew common noun satan (`"adversary`" or `"accuser`") functioned in the Hebrew Bible as a role-description applicable to human and heavenly figures alike, well before it hardened into a proper name for a singular cosmic antagonist. That older, more flexible sense of the word is the relevant background for reading Jesus's use of the term against Peter at Matthew 16:23: an adversarial epithet aimed at a specific momentary role Peter is playing, distinct from any claim that Peter is being identified with the tempter of the wilderness scene." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The Caesarea Philippi entry already established above covers the site's Herodian building history and its long-standing dedication to the god Pan; the epigraphic record recovered from the cliff face itself supplies a second, independent layer of evidence for the same cult rather than repeating that ground. Israel Antiquities Authority excavations of the votive niches cut into the rock beside the Pan grotto recovered Greek dedicatory inscriptions naming Pan and his consort Echo directly, including one dedication carved by a priest named Victor son of Lysimachos [[NOTE:caesarea-philippi-pan-votive-inscriptions]]. That is a different order of evidence than a building foundation or a passage in Josephus: it is the worshippers' own carved words, still legible in the same cliff face the text places Jesus standing in front of when he makes his declaration about the rock and the gates of Hades.

None of this touches whether the theological claim is true. What it does is close the gap between "a real Herodian city with a temple to Augustus" and "an actively used pagan shrine with named priests and named donors operating at the very moment this scene is set" — a gap that matters for readers weighing how loaded the setting actually was, independent of any argument about what Jesus meant by it [[NOTE:caesarea-philippi-pan-votive-inscriptions]].
'@

$beat2 = @'
Jesus's declaration that he will build his church on "this rock" turns on a wordplay that only exists, in this exact form, in Greek: "You are Petros [Peter], and on this petra [rock] I will build my church" (16:18). Centuries of doctrinal argument have been built on the fact that Petros and petra are not identical words — Petros carries a masculine ending, petra a feminine one, and some readings have pressed that difference to argue Jesus meant something narrower by "this rock" than Peter himself. The linguistic ground under that argument is thinner than it looks. Jesus is understood to have spoken Aramaic in daily life, not Greek, and in Aramaic there was only one word doing both jobs: kepha, "rock," used without variation for both Simon's new name and the wordplay's second term [[NOTE:aramaic-kepha-single-word]]. Reconstructed back into Jesus's own likely wording, the line reads as a single repeated word, not two related-but-different ones.

The Greek text's two different forms are best explained as a translation effect rather than a deliberate distinction: Greek grammar requires a man's name to take a masculine ending, so a masculine Petros had to be coined from the ordinary feminine noun petra when the underlying kepha wordplay was rendered into Greek [[NOTE:petros-petra-greek-grammatical-gender]]. A landmark joint Protestant-Catholic-Lutheran commission, convened specifically to work through disputed texts like this one across confessional lines, reached the same conclusion: the grammatical-gender distinction most likely reflects Greek necessity, not a meaningful part of Jesus's own wordplay — while documenting, honestly, that the same verse fueled real interconfessional polemic for centuries before that joint study was possible [[NOTE:peter-in-nt-ecumenical-scholarly-consensus]]. Worth adding to the textual picture: this naming of Peter as the rock, along with the keys and binding-and-loosing grant that follow it, has no parallel at all in Mark's or Luke's versions of this same Caesarea Philippi scene — material found in Matthew alone [[NOTE:matthew-16-17-19-special-material]]. That the underlying pun is very likely a single Aramaic word, not a deliberately graded pair of Greek ones, is now the position most mainstream scholarship converges on, across confessional lines, even as the historical debate over the verse's implications remains very much alive [[NOTE:aramaic-kepha-single-word]].
'@

$beat3 = @'
The grant that follows the rock naming — "I will give you the keys of the kingdom of heaven" (16:19) — is not an image invented for this scene. It draws on Isaiah 22:15-22, where the prophet announces that Judah's royal steward, Eliakim son of Hilkiah, will be given "the key of the house of David" laid on his shoulder, with the power to open what no one can shut and shut what no one can open — language describing a king's chief household officer holding delegated administrative authority over the palace and who may enter the royal presence [[NOTE:isaiah-22-eliakim-key-of-david]]. That is a genuine, checkable Old Testament background for the keys image, six centuries older than the Gospel scene, not a metaphor built from nothing.

Handing that specific image to Peter, alongside the rock-naming and the binding-and-loosing grant already covered above, again belongs to the block of material — 16:17-19 — that has no parallel in Mark's or Luke's tellings of the same Caesarea Philippi confession [[NOTE:matthew-16-17-19-special-material]]. What the Eliakim background can establish is where the image of "keys" as an authority-transfer symbol comes from and what kind of office it evoked for a first-century Jewish audience; what it was meant to grant Peter specifically, and to whom that grant might extend beyond him, sits on the doctrinal side of the line this method cannot adjudicate.
'@

$beat4 = @'
Six verses after being blessed for Spirit-given insight, Peter is on the receiving end of the sharpest rebuke in Matthew's narrative. When Jesus tells the disciples plainly that he will suffer, die, and rise (16:21), Peter pulls him aside and rebukes him — and Jesus turns on Peter in front of the others: "Get behind me, Satan! You are a stumbling block to me; for you are not thinking the things of God, but the things of men" (16:23). Commentary on the passage treats the juxtaposition as a deliberate structural feature of the scene rather than an accident of how the material was assembled: the same figure singled out for the highest praise anyone receives from Jesus in this Gospel is, a few sentences later, addressed with the same word used for the tempter of the wilderness narrative — the most extreme reversal of any one character's standing across so short a span in the entire book [[NOTE:luz-confession-rebuke-literary-tension]].

The word itself is worth pausing on before reading too much into the repetition. In the Hebrew Bible, satan functioned first as a common noun — "adversary" or "accuser" — applicable to human and heavenly figures alike, and only later hardened into a proper name for a singular cosmic antagonist [[NOTE:satan-generic-adversary-term]]. Read against that older, more flexible sense, Jesus's word to Peter reads as an adversarial epithet aimed at the specific role Peter is playing in that moment — pulling Jesus away from the path he has just described — rather than a claim that Peter has been possessed by, or identified with, the tempter from chapter four. That distinction doesn't soften how sharp the rebuke is; it is still the same word, aimed at the same man Jesus has just called the rock of his church, and the tension between the two moments is real and worth naming on its own terms, independent of any doctrinal reading of either verse [[NOTE:luz-confession-rebuke-literary-tension]].
'@

$beat5 = @'
Stepping back, the whole Caesarea Philippi scene — confession, blessing, rock-naming, keys, silence order, passion prediction, rebuke — covers just eleven verses (16:13-23), and it moves through more reversal of a single character's standing than almost any comparable stretch in the Gospel. Peter answers correctly, is told his answer came from the Father rather than from himself, is renamed and handed the keys of the kingdom, and is then, within the same conversation, called an obstacle working against God's plan [[NOTE:luz-confession-rebuke-literary-tension]]. Both the height and the depth of that swing rest on material — 16:17-19's naming, keys, and binding-and-loosing on one side, 16:22-23's rebuke on the other — that appears nowhere in Mark's or Luke's parallel accounts of this same encounter, which makes the whole arc a distinctly Matthean shaping of the shared underlying tradition [[NOTE:matthew-16-17-19-special-material]].

What can be traced with some confidence is the raw material behind each half of that swing: an Aramaic wordplay on a single word for "rock" that Greek grammar had to split into two forms [[NOTE:aramaic-kepha-single-word]], a nearly six-centuries-old prophetic image of a royal steward's key of office [[NOTE:isaiah-22-eliakim-key-of-david]], and a Hebrew Bible term for "adversary" broad enough to be aimed at a friend mid-argument without implying demonic possession [[NOTE:satan-generic-adversary-term]]. Whether the theological weight later generations placed on the rock-naming and the keys is the weight Jesus himself intended — and whether that weight belongs to Peter alone or to something broader — is exactly the kind of question a joint Protestant-Catholic-Lutheran commission convened a full study to work through, and on which honest scholarship across confessional lines still holds real, acknowledged disagreement [[NOTE:peter-in-nt-ecumenical-scholarly-consensus]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'PETROS VS. PETRA (PETER/ROCK WORDPLAY)' = "The Greek wordplay in Matthew 16:18 — `"You are Petros [masculine], and on this petra [feminine, `"rock`"] I will build my church`" — that has generated centuries of doctrinal argument over whether Jesus meant Peter himself, or Peter's confession, as the `"rock`" in view. The linguistic background most mainstream scholars now converge on, across confessional lines, is that Jesus almost certainly spoke Aramaic day to day, in which a single word, kepha, covers both `"Peter`" and `"rock`" with no distinction at all [[NOTE:aramaic-kepha-single-word]]; the Greek text's two different forms are best explained as a grammatical necessity (a man's name needs a masculine ending) rather than a deliberate theological distinction [[NOTE:petros-petra-greek-grammatical-gender]]. A joint Protestant-Catholic-Lutheran scholarly commission reached this same conclusion while documenting that the verse fueled genuine interconfessional polemic for centuries beforehand [[NOTE:peter-in-nt-ecumenical-scholarly-consensus]]. The rock-naming itself is special Matthean material with no parallel in Mark's or Luke's versions of the same scene [[NOTE:matthew-16-17-19-special-material]]."
'KEYS OF THE KINGDOM (ISAIAH 22:22 BACKGROUND)' = "Jesus's grant to Peter, `"I will give you the keys of the kingdom of heaven`" (Matthew 16:19), paired with the power to `"bind`" and `"loose`" on earth and in heaven. The image is not invented for this scene: it draws on Isaiah 22:15-22, where the prophet Eliakim son of Hilkiah, Judah's royal steward, is given `"the key of the house of David`" on his shoulder, with authority to open what no one can shut and shut what no one can open — a genuine ancient Near Eastern image of a king's chief household officer holding delegated administrative authority [[NOTE:isaiah-22-eliakim-key-of-david]]. Like the rock-naming beside it, this material appears only in Matthew's telling of the Caesarea Philippi scene, with no parallel in Mark or Luke [[NOTE:matthew-16-17-19-special-material]]."
'BINDING AND LOOSING (RABBINIC LEGAL AUTHORITY)' = "The pair of verbs in Jesus's grant to Peter, `"whatever you bind on earth will be bound in heaven, and whatever you loose on earth will be loosed in heaven`" (Matthew 16:19) — standard, technical legal language in Second Temple and early rabbinic Judaism for the authority to rule something forbidden or permitted, as this chapter's own beat on the passage traces in detail through Josephus's description of Pharisaic authority under Salome Alexandra and the Mishnah's routine `"forbid/permit`" formula [61]. Handing Peter this specific pair of verbs casts him, on the text's own account, in a role a first-century audience would have recognized immediately: the authority to issue binding rulings for the community, not a phrase coined for this scene alone."
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
    Add-BeatNode $Ch16NodeId $id $maxChapterSortKey
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
Seed-Entity "Eliakim (Royal Steward)" "eliakim-royal-steward" "character" "Judah's royal steward under Hezekiah given the key of the house of David (Isaiah 22:15-22); Old Testament background for the keys of the kingdom given to Peter in Matthew 16:19."
Seed-Entity "Kepha (Aramaic Rock/Peter Wordplay)" "kepha-aramaic-rock-peter-wordplay" "vocabulary" "The single Aramaic word for 'rock' underlying both Simon's new name and the wordplay of Matthew 16:18, rendered in Greek as the distinct forms Petros and petra."

$conn.Close()
Write-Host "DONE Chapter 16 depth pass."
