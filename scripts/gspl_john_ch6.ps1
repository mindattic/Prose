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
$Ch6NodeId = [guid]"019FA96C-6EC5-7B1B-AD5A-1ABB5EAF35E9"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96D-8DD0-70E4-8D98-34AC48833B7E'")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96D-7D48-75E0-9BD9-2190171276DC' AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'feeding-5000-fourfold-attestation' = @{ title='The only miracle story in all four Gospels'; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume Two: Mentor, Message, and Miracles (New York: Doubleday, 1994), discussion of the multiplication-of-loaves tradition. Meier notes that the feeding of the multitude is the sole miracle narrative besides the resurrection appearances attested independently in all four canonical Gospels (Mark 6:32-44, Matthew 14:13-21, Luke 9:10-17, John 6:1-15), a breadth of independent attestation historical-critical method treats as unusually strong evidence that some memorable feeding event, however later elaborated in the retelling, stood behind the written accounts." }
'mark-matthew-feeding-doublet' = @{ title='Two feedings, or one story remembered twice?'; body="Rudolf Bultmann, The History of the Synoptic Tradition, trans. John Marsh, rev. ed. (Oxford: Basil Blackwell, 1968; German original 1921), discussion of the nature-miracle and feeding narratives. Mark (6:32-44; 8:1-10) and Matthew (14:13-21; 15:32-39) each report two distinct feeding miracles — one for five thousand, one for four thousand — an oddity Bultmann and the form-critical tradition following him read as most plausibly a single underlying oral tradition that had already split into two separately localized retellings, with differing numbers, basket-words, and audiences, before reaching the evangelists, rather than two historical multiplication events staged weeks apart in Jesus's ministry." }
'barley-loaves-elisha-typology' = @{ title='An Elisha echo, down to the grain'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 6:1-13. Brown identifies 2 Kings 4:42-44 — where the prophet Elisha feeds a hundred men from twenty barley loaves over a servant's objection that it isn't enough, with food left over afterward — as the Gospel's clear scriptural type-scene, down to the specific grain and the detail of surplus remaining; John's twelve leftover baskets, on this reading, additionally gesture toward the twelve tribes of Israel, a symbolic doubling largely uncontested across mainstream commentary." }
'counting-men-only' = @{ title='Five thousand — of whom, exactly?'; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 1 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 6:10. John's Greek specifies andres ('men') in giving the five-thousand figure, following the same convention as Mark 6:44 and Luke 9:14; Matthew's parallel (14:21) makes the convention explicit by adding 'besides women and children.' Keener reads this as an ordinary ancient counting practice — tallying adult males as the socially countable unit — rather than any claim that women and children were absent from the crowd." }
'crowd-kingship-messianic-context' = @{ title='They wanted to make him king by force'; body="Paula Fredriksen, Jesus of Nazareth, King of the Jews: A Jewish Life and the Emergence of Christianity (New York: Alfred A. Knopf, 1999), discussion of popular messianic expectation in Roman Galilee. Fredriksen situates John 6:15's report — that the fed crowd wanted to seize Jesus and 'make him king by force' — within a broader first-century Galilean and Judean pattern of popular, often agrarian-rooted messianic and prophetic movements that Roman administrators treated as insurrectionary threats regardless of a movement leader's own intentions; Jesus's withdrawal alone into the hills, on this reading, is a deliberate refusal of exactly the kind of populist kingship the crowd was offering." }
'passover-mention-ministry-length' = @{ title="Three Passovers, or one? What the length of Jesus's ministry rests on"; body="D.A. Carson, The Gospel According to John, Pillar New Testament Commentary (Grand Rapids: William B. Eerdmans Publishing Company, 1991), commentary ad loc. John 6:4. John names a Passover here — the second of three named across the whole Gospel, alongside 2:13 and the Passion Passover of chapters 11-19 — a detail the Synoptic Gospels give no equivalent basis for counting. The conventional scholarly reconstruction of a roughly two-to-three-year public ministry rests substantially on John's three Passovers, whereas a reading confined to the Synoptics alone, which narrate only a single Passover, would suggest a ministry that could fit within a single year; which framework is correct remains a live, unresolved question in historical-Jesus chronology rather than a settled fact either way." }
'walking-on-water-job-9-8' = @{ title='Treading the waves: a divine prerogative in Job'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 6:16-21. Brown notes that Job 9:8, praising God's cosmic power, describes God as the one who alone 'trampled the waves of the Sea' (following the Septuagint's wording); in the Hebrew scriptural tradition, walking upon the sea is not a generic wonder but a specifically divine act, so that Jesus's walking on the water in this scene carries an implicit theophany — a claim about who he is — rather than functioning as a stand-alone display of power." }
'walking-on-water-epiphany-form' = @{ title='A recognized ancient form: the epiphany story'; body="Rudolf Bultmann, The History of the Synoptic Tradition, trans. John Marsh, rev. ed. (Oxford: Basil Blackwell, 1968), discussion of nature miracles and epiphany narratives. Bultmann classified the walking-on-water pericope (paralleled in Mark 6:45-52 and Matthew 14:22-33, though absent from Luke) as belonging to a recognized Hellenistic and Jewish narrative form — the epiphany story, in which a god or divine agent suddenly manifests to frightened followers, typically at sea or at night, with a self-identifying declaration that dispels their fear — so that the story's shape is doing theological work about Jesus's identity independent of whatever specific event, if any, gave rise to it." }
'ego-eimi-divine-self-declaration' = @{ title="'It is I' — or 'I am'?"; body="C.K. Barrett, The Gospel According to St. John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 6:20. The Greek behind 'it is I' is simply ego eimi ('I am'), the same absolute construction John's Gospel later uses in its distinctive 'I am' sayings (6:35, 8:12, and elsewhere) and which echoes the divine self-naming of Isaiah 43:10 and Exodus 3:14 in the Greek Septuagint. Barrett and the broader Johannine commentary tradition read the phrase here as carrying, at minimum, a double register — a plain reassurance ('it's me, don't be afraid') and a deeper theological claim available to readers attuned to the Gospel's own idiom." }
'sea-of-tiberias-naming' = @{ title="Why the lake carries an emperor's name"; body="Flavius Josephus, Jewish Antiquities, Book 18, sections 36-38 (Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press). Josephus records that Herod Antipas, tetrarch of Galilee, founded the city of Tiberias on the lake's western shore around 20 CE, naming it for the reigning emperor Tiberius; John's Gospel is among the earliest surviving texts to reflect the resulting habit of calling the lake itself 'the Sea of Tiberias' (6:1, 6:23) alongside its older name, the Sea of Galilee — a naming shift datable to within living memory of the Gospel's narrated events." }
'eucharistic-language-bultmann-skepticism' = @{ title="A later hand at the table: Bultmann's redactor theory"; body="Rudolf Bultmann, The Gospel of John: A Commentary, trans. G.R. Beasley-Murray, R.W.N. Hoare, and J.K. Riches (Philadelphia: Westminster Press, 1971; German original 1941), commentary ad loc. John 6:51c-58. Bultmann argued that the sharply sacramental language of 'eating my flesh' and 'drinking my blood' (6:53-56) sits awkwardly against the discourse's earlier, more purely revelatory register ('I am the bread of life... whoever believes has eternal life,' 6:35, 6:47), and proposed that this section was added by a later 'ecclesiastical redactor' retrojecting the church's developed eucharistic theology into a scene set roughly a year before the actual Last Supper — a source-critical judgment that remains influential but contested." }
'eucharistic-language-brown-integrated' = @{ title='One evangelist, two registers: a more integrated reading'; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 6:51-58. Against Bultmann's redactor theory, Brown reads the discourse's eucharistic-sounding section as belonging to the evangelist's own composition rather than a later interpolation, arguing the chapter deliberately layers two Jewish exegetical traditions about bread from heaven — a Wisdom/Torah-as-bread motif (6:35-50) and a specifically eucharistic application of it (6:51-58) — into a single unified meditation on Jesus as the source of life, produced by one hand working in stages rather than by two hands working at cross purposes." }
'von-wahlde-source-layers' = @{ title='Reading the seams: a three-edition theory of John'; body="Urban C. von Wahlde, The Gospel and Letters of John, 3 vols., Eerdmans Critical Commentary (Grand Rapids: William B. Eerdmans Publishing Company, 2010), vol. 1, introduction on the Gospel's compositional history. Von Wahlde proposes that the Fourth Gospel took shape across three successive editions, each adding material to and revising the last, and identifies the seam between the bread-of-life discourse's revelatory and sacramental sections as one of several places in the Gospel where this layered composition history is detectable in the text's own internal joints — an independent line of source-critical argument that arrives, by a different method, at a diagnosis of the discourse's composite character similar to Bultmann's." }
'hard-saying-disciples-leave-community-conflict' = @{ title="'Many of his disciples drew back': a schism remembered"; body="Raymond E. Brown, The Community of the Beloved Disciple (New York: Paulist Press, 1979), chapter on the Gospel's evidence for the Johannine community's own history. Brown reads the mass departure of disciples over 'a hard saying' (6:60-66) — and Jesus's pointed question to the remaining Twelve, 'Do you also wish to go away?' — as very likely reflecting, at some level, the Johannine community's own later experience of internal division and departures (compare 1 John 2:19's 'they went out from us'), projected back onto a scene set during Jesus's earthly ministry rather than reporting the precise words and numbers of an actual first-century walkout." }
'peter-confession-holy-one-of-god-variant' = @{ title="'The Holy One of God,' or a harmonized confession?"; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece, ad loc. John 6:69. The earliest and best manuscripts (including Codex Sinaiticus and Codex Vaticanus) have Peter confess Jesus as 'the Holy One of God,' a title distinctive to this scene; a number of later manuscripts instead read some form of 'the Christ, the Son of the living God,' matching Peter's confession at Caesarea Philippi in Matthew 16:16. Modern critical editions treat 'the Holy One of God' as original and the Matthean-sounding variant as a scribal harmonization toward the more familiar Synoptic confession." }
'judas-iscariot-patronymic-variant' = @{ title='Simon Iscariot, or Simon of Kerioth?'; body="Textual apparatus of the Nestle-Aland Novum Testamentum Graece, ad loc. John 6:71. Manuscripts vary on whether Judas is identified as the son of 'Simon Iscariot' (treating 'Iscariot' as a family name shared by father and son) or, in some witnesses, with a phrase more transparently read as 'man of Kerioth' (a Judean town), reflecting an underlying uncertainty — never fully resolved in scholarship — over whether 'Iscariot' originated as a place-name designation (ish-Kerioth) or some other term entirely." }
'barley-bread-poor-food' = @{ title='Barley: the grain of the poor'; body="Craig S. Keener, The Gospel of John: A Commentary, vol. 1 (Peabody, MA: Hendrickson Publishers, 2003), commentary ad loc. John 6:9. Barley was the cheaper, coarser, and more heat-tolerant of the two staple ancient Mediterranean grains, generally eaten by those who could not afford wheat bread; Keener and the broader social-historical commentary tradition read the detail that a boy is carrying barley loaves — rather than the finer wheat loaves a wealthier household might supply — as a small, incidental marker of ordinary peasant poverty rather than a detail invented for theological effect." }
'manna-background-bread-of-heaven' = @{ title='Manna, and the midrash it invites'; body="C.K. Barrett, The Gospel According to St. John: An Introduction with Commentary and Notes on the Greek Text, 2nd ed. (Philadelphia: Westminster Press, 1978), commentary ad loc. John 6:31-35. The crowd's demand for a sign like Moses's manna (quoting a composite drawn from Exodus 16:4 and Psalm 78:24, 'he gave them bread from heaven to eat') sets up the discourse's central move: Jesus does not merely repeat the manna sign but reinterprets its scriptural source, declaring himself — not the bread Moses gave — to be the true 'bread from heaven.' Barrett situates this pattern within known first-century Jewish exegetical technique, in which a scriptural text is cited specifically in order to be reworked and surpassed, comparable to techniques visible in Philo of Alexandria's own allegorical readings of the manna narrative." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
A large crowd, drawn by the signs Jesus has been performing on the sick, follows him across the Sea of Galilee — which John, alone among the evangelists, also calls "the Sea of Tiberias" — as Passover, the Jewish feast, draws near (6:1-4). Seeing the multitude, Jesus tests Philip by asking where they might buy bread; Philip protests that two hundred denarii wouldn't buy enough for even a little for each person, and Andrew points out a boy in the crowd who has five barley loaves and two fish, though he adds "what are they among so many?" Jesus has the crowd sit down in the grass, gives thanks, and distributes the loaves and fish — and everyone eats until they are satisfied, with twelve baskets of leftover fragments gathered afterward (6:5-13). Seeing this, the crowd declares "this is indeed the Prophet who is to come into the world," moves to seize Jesus and make him king by force, and Jesus withdraws alone to the mountain (6:14-15).

Start with what this story doesn't need historical-critical scholarship to notice: it is the one miracle story, apart from the resurrection appearances, that shows up independently in all four canonical Gospels [[NOTE:feeding-5000-fourfold-attestation]]. That breadth of attestation is genuinely unusual and genuinely significant by the field's own standards — but it sits right alongside a much messier fact that the fourfold-attestation observation tends to undersell: Mark and Matthew don't just each report this feeding once, they each report a second, structurally near-identical feeding of four thousand later in the same Gospel. Rather than reading that as two separate historical multiplication events staged weeks apart, the mainstream form-critical reading treats the doublet as a single oral tradition that had already forked into two differently-numbered, differently-located retellings before either evangelist ever wrote it down [[NOTE:mark-matthew-feeding-doublet]]. Multiple attestation and doublet formation aren't in tension here so much as two sides of the same coin: a story vivid and important enough to travel widely also tends to pick up variant details as it travels.

The specific grain matters too. Barley bread was the cheaper, plainer staple of the ancient Mediterranean diet — what a family without much money ate instead of wheat bread [[NOTE:barley-bread-poor-food]] — and five barley loaves and two fish feeding a multitude with twelve baskets left over is not a detail invented from nothing: it tracks, closely, a story already in Israel's own scripture. In 2 Kings 4:42-44, the prophet Elisha feeds a hundred men from twenty barley loaves over a servant's skeptical objection that it won't be enough, with food remaining afterward. The parallel — barley loaves, an objection about insufficient quantity, surplus at the end — is close enough that mainstream commentary treats it as a deliberate scriptural type-scene rather than coincidence, with the twelve leftover baskets further gesturing toward Israel's twelve tribes [[NOTE:barley-loaves-elisha-typology]]. Whether that shaping happened as the story was told and retold, or at John's own writing desk, the text is doing something more than reporting a headcount.

That headcount is itself worth a pause. "Five thousand" counts andres — men — following the same convention Mark and Luke use; only Matthew's parallel makes the ancient practice explicit by tacking on "besides women and children." That's a counting convention of the period, not a claim that the crowd was entirely male [[NOTE:counting-men-only]].

And the crowd's reaction is the chapter's real hinge. Wanting to "take him by force to make him king" isn't a throwaway detail; first-century Galilee and Judea saw a real pattern of popular, often agrarian-rooted movements rallying around a leader as a prophetic or messianic king, movements Roman administrators treated as dangerous whether or not the man at the center wanted the crown being offered him [[NOTE:crowd-kingship-messianic-context]]. Jesus's response — withdrawing alone, up the mountain, away from the crowd — reads as a pointed refusal of exactly that role, on exactly those terms. One more small chronological marker closes out the setup: the Passover named here is the second of three Passovers John counts across his whole Gospel, a detail with real consequences for how long historians reconstruct Jesus's public ministry as having lasted [[NOTE:passover-mention-ministry-length]].
'@

$beat2 = @'
That evening, Jesus's disciples go down to the sea without him and set out by boat for Capernaum on the far shore. Night falls, Jesus still hasn't come, and a strong wind kicks up rough water; three or four miles out, they see him walking on the sea, approaching the boat, and they're terrified. He says to them, "It is I; do not be afraid." They take him into the boat, and immediately the boat reaches the shore they were making for (6:16-21).

The single most important background fact for this scene isn't Greco-Roman folklore about wonder-working sages; it's a line from Job. Job 9:8, praising God's singular cosmic power, says God alone "trampled the waves of the Sea." In the Hebrew scriptural imagination this isn't a generic display of strength — walking on the sea is specifically something only God does [[NOTE:walking-on-water-job-9-8]]. Set against that background, Jesus walking on the water isn't simply another item on a list of impressive deeds; it's a claim, embedded in the story's very shape, about who is doing the walking.

Form-critical scholarship going back to Bultmann classifies this scene as an epiphany story — a recognized ancient narrative pattern, found across both Hellenistic and Jewish literature, in which a divine or semi-divine figure suddenly appears to frightened followers, often at sea or at night, and calms their fear with a self-identifying declaration [[NOTE:walking-on-water-epiphany-form]]. That the same broad episode appears in Mark and Matthew but not in Luke is itself worth noting: this is real, multiple attestation, just not the fourfold kind the feeding enjoys.

The declaration itself carries the theological weight. "It is I" translates a Greek phrase that is, word for word, simply ego eimi — "I am" — the same absolute construction John's Gospel will use again and again in Jesus's distinctive "I am" sayings, a phrase that in the Greek Old Testament echoes God's own self-naming to Moses and in Isaiah. Mainstream commentary reads a double register here: on the surface, an ordinary reassurance ("it's me, don't be afraid"); underneath, for a reader attentive to the Gospel's idiom, something considerably larger [[NOTE:ego-eimi-divine-self-declaration]]. And the lake's double name gets its own footnote here too — "Sea of Tiberias" is not John's invention but reflects the real, datable renaming of the region around the city Herod Antipas founded and named for the emperor barely a decade or two before this scene is set [[NOTE:sea-of-tiberias-naming]].
'@

$beat3 = @'
The next day, the crowd realizes Jesus and his disciples are both gone and follows by boat to Capernaum, finding him on the far shore. Jesus tells them plainly that they're looking for him because they ate their fill of bread, not because they understood the sign, and urges them to work for food that endures to eternal life rather than food that perishes. When they ask what work God requires, he answers: believe in the one God has sent. They ask for a sign — reminding him that their ancestors ate manna in the wilderness, as scripture says, "he gave them bread from heaven to eat." Jesus corrects the attribution — it was not Moses but his Father who gave the true bread from heaven — and declares: "I am the bread of life; whoever comes to me shall not hunger, and whoever believes in me shall never thirst" (6:22-35).

The manna exchange is where the discourse shows its scriptural hand most openly. The crowd's citation is itself a composite, drawing on Exodus 16:4 and Psalm 78:24 together, and Jesus's answer doesn't dispute the manna tradition — it reroutes it, insisting the true "bread from heaven" was never the wilderness food itself but something Moses's story was always pointing toward. That move — citing scripture specifically in order to rework and surpass it — is a recognized first-century Jewish exegetical technique, visible in comparable form in Philo of Alexandria's own allegorical treatments of the same manna narrative [[NOTE:manna-background-bread-of-heaven]].

"I am the bread of life" is the first of the Fourth Gospel's famous predicated "I am" sayings, and it doesn't come from nowhere in this chapter: it answers, directly, the absolute "I am" Jesus spoke to his frightened disciples on the water the night before [[NOTE:ego-eimi-divine-self-declaration]]. The crowd wanted another sign, another meal; what they get instead is Jesus reframing the entire encounter — the feeding, the boat, the manna precedent — around a single claim about his own identity as the thing Israel's whole scriptural memory of bread from heaven was reaching toward.
'@

$beat4 = @'
The discourse escalates. The crowd grumbles that Jesus, whose father and mother they know, cannot claim to have "come down from heaven"; Jesus presses further, saying the bread he gives for the life of the world is his own flesh. The language sharpens past metaphor into something starker still: "unless you eat the flesh of the Son of Man and drink his blood, you have no life in you... my flesh is true food, and my blood is true drink" (6:41-59), taught, John notes, in the synagogue at Capernaum.

This is the passage that has generated the single sharpest disagreement in the chapter's scholarship, and it deserves to be presented fairly on both sides. Bultmann argued that 6:51c-58's specifically sacramental language — eating flesh, drinking blood — sits awkwardly against the discourse's earlier, purely revelatory register of believing and never hungering, and concluded that a later "ecclesiastical redactor" spliced in developed eucharistic theology, retrojecting the church's own later liturgical practice into a scene set roughly a year before the actual Last Supper [[NOTE:eucharistic-language-bultmann-skepticism]]. Brown, working from the same text, reached a different conclusion: rather than two hands at cross purposes, he reads one evangelist deliberately layering two exegetical traditions about bread from heaven — a wisdom/Torah motif and a eucharistic application of it — into a single unified meditation on Jesus as life's source [[NOTE:eucharistic-language-brown-integrated]]. Von Wahlde's independent, source-layered account of the Gospel's three-edition compositional history arrives, by a different method, at a broadly similar diagnosis of a discourse built up in stages [[NOTE:von-wahlde-source-layers]]. What the honest reader should take from this three-way disagreement isn't a resolved answer but a genuinely open question: whether the Bread of Life discourse's most eucharistic-sounding language is an original stratum of the evangelist's own theology, or a later liturgical overlay — both readings remain live in the field, and neither can be waved away.
'@

$beat5 = @'
The saying lands hard. Many of Jesus's own disciples say, "This is a hard saying; who can listen to it?" Jesus asks if it offends them, adds that "the Spirit gives life; the flesh is no help at all... the words I have spoken to you are spirit and life" — and from that point on, many of his disciples turn back and no longer walk with him. Jesus asks the Twelve, "Do you want to go away as well?" and Simon Peter answers: "Lord, to whom shall we go? You have the words of eternal life, and we have believed, and have come to know, that you are the Holy One of God." Jesus replies that he chose the Twelve, and yet one of them is a devil — speaking, John notes, of Judas, son of Simon Iscariot, who would betray him (6:60-71).

The mass walkout is the chapter's most historically loaded moment, and mainstream scholarship reads it on two levels at once. On its face, it's a scene of first-generation disciples abandoning Jesus over a doctrinally hard teaching. Raymond Brown's reconstruction of the Johannine community's own history reads it as very likely doing double duty: reflecting, at some level, that same community's later experience of internal division and departure — language that echoes 1 John 2:19's "they went out from us" — projected back onto Jesus's earthly ministry rather than transcribing an actual first-century walkout word for word [[NOTE:hard-saying-disciples-leave-community-conflict]].

Peter's confession answers the Synoptic Gospels' Caesarea Philippi confession scene from a different angle and in different words — "the Holy One of God" here, rather than "the Christ, the Son of God" — and that specific wording is itself contested at the manuscript level: a number of later copies read a Matthew-sounding confession instead, which critical editions generally treat as a scribal harmonization toward the more familiar Synoptic phrasing rather than John's original text [[NOTE:peter-confession-holy-one-of-god-variant]]. And the chapter's grim closing note — Judas identified here, at his very first appearance in this Gospel, as "a devil" and the coming betrayer — carries its own small manuscript puzzle: whether he is "Simon Iscariot's son," treating Iscariot as an inherited family name, or something closer to "a man of Kerioth" naming his hometown, is a question the manuscript tradition itself does not fully settle [[NOTE:judas-iscariot-patronymic-variant]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'SEA OF GALILEE (SEA OF TIBERIAS)' = "The freshwater lake in Galilee that Jesus and the crowd cross in this chapter (6:1, 6:16-21), called by John both ``the Sea of Galilee'' and ``the Sea of Tiberias'' — the second name reflecting the real first-century renaming of the region after the lakeside city Herod Antipas founded and named for the emperor Tiberius [[NOTE:sea-of-tiberias-naming]]."
'TIBERIAS (CITY)' = "A city on the western shore of the Sea of Galilee, founded by Herod Antipas, tetrarch of Galilee, around 20 CE and named for the reigning emperor Tiberius. Some boats from Tiberias are noted arriving near the feeding site the following day (6:23); the city's founding within living memory of Jesus's ministry is independently attested by Josephus [[NOTE:sea-of-tiberias-naming]]."
'CAPERNAUM' = "The fishing village on the Sea of Galilee's northern shore that serves as the destination of the disciples' boat during the storm (6:17, 6:21) and the setting, per John's own note, for the second half of the Bread of Life discourse, taught ``in the synagogue, as he taught at Capernaum'' (6:59)."
'ELISHA' = "The ninth-century BCE Israelite prophet and successor to Elijah, whose own feeding of a hundred men from twenty barley loaves in 2 Kings 4:42-44 — complete with a servant's objection about insufficient quantity and food left over afterward — is read by mainstream commentary as the direct scriptural type-scene behind John's feeding of the five thousand [[NOTE:barley-loaves-elisha-typology]]."
'THE BOY WITH THE LOAVES AND FISH' = "An unnamed boy in the crowd, pointed out by Andrew, whose five barley loaves and two fish (6:9) become the material for the feeding miracle. Barley marks the loaves as the cheaper grain of the ordinary poor rather than a wealthier household's wheat bread [[NOTE:barley-bread-poor-food]], and the boy's provision is read as recalling the Elisha feeding story's own emphasis on a small, seemingly insufficient offering [[NOTE:barley-loaves-elisha-typology]]."
'BREAD OF LIFE (I AM SAYING)' = "Jesus's declaration ``I am the bread of life'' (6:35, 6:48), the first of the Fourth Gospel's distinctive predicated ``I am'' sayings and a direct answer, within the chapter's own logic, to the absolute ``I am'' he speaks walking on the water the night before [[NOTE:ego-eimi-divine-self-declaration]]. The discourse's later, sharply eucharistic-sounding extension of this claim — ``eat my flesh, drink my blood'' (6:53-56) — is one of the most contested passages in Johannine scholarship [[NOTE:eucharistic-language-bultmann-skepticism]] [[NOTE:eucharistic-language-brown-integrated]]."
'MANNA (BREAD FROM HEAVEN)' = "The miraculous wilderness food described in Exodus 16, cited by the crowd (6:31, quoting a composite drawn from Exodus 16:4 and Psalm 78:24) as the sign-precedent they expect Jesus to match or exceed. Jesus's answer reroutes the tradition, insisting the true bread from heaven was never the manna itself but what it pointed toward [[NOTE:manna-background-bread-of-heaven]]."
'JUDAS ISCARIOT' = "One of the Twelve, identified at his first appearance in John's Gospel as the coming betrayer and, in Jesus's own words, ``a devil'' (6:70-71). He is named here as the son of Simon Iscariot, though the manuscript tradition itself is divided over whether ``Iscariot'' names a family line or, in an older sense, marks Judas (or his father) as a ``man of Kerioth'' [[NOTE:judas-iscariot-patronymic-variant]]."
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
    Add-BeatNode $Ch6NodeId $id $sortKey
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
Seed-Entity "Elisha" "elisha" "character" "Ninth-century BCE Israelite prophet, successor to Elijah; his 2 Kings 4:42-44 barley-loaves feeding miracle is the scriptural type-scene behind John 6's feeding of the five thousand."
Seed-Entity "Tiberias (city)" "tiberias-city" "place" "City on the western shore of the Sea of Galilee, founded by Herod Antipas circa 20 CE and named for Emperor Tiberius; source of the lake's alternate name, the Sea of Tiberias."
Seed-Entity "The boy with the loaves and fish" "boy-with-the-loaves-and-fish" "character" "Unnamed boy in the John 6 feeding crowd whose five barley loaves and two fish, pointed out by Andrew, become the material for the miracle."
Seed-Entity "Bread of Life (I Am saying)" "bread-of-life-i-am-saying" "vocabulary" "Jesus's declaration 'I am the bread of life' (John 6:35, 48), the first of the Fourth Gospel's predicated 'I am' sayings."
Seed-Entity "Manna (bread from heaven)" "manna-bread-from-heaven" "vocabulary" "The Exodus 16 wilderness food, cited in John 6:31 as the sign-precedent the crowd expects Jesus to match or exceed."

$conn.Close()
Write-Host "DONE Chapter 6."
