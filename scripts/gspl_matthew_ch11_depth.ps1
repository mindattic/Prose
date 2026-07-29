$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
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
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"
$Ch11NodeId = [guid]"019FA068-DA02-71F2-AB5E-E84E36383284"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxChapterSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA068-DA02-71F2-AB5E-E84E36383284' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxChapterSortKey=$maxChapterSortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'josephus-machaerus-location' = @{ title="Josephus names the prison: Machaerus"; body="Flavius Josephus, Jewish Antiquities, Book 18, sections 116-119 (numbered 18.5.2 in the older Whiston enumeration; Loeb Classical Library, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press). Writing independently of the Gospel tradition and with no interest in corroborating it, Josephus states that Herod Antipas sent John as a prisoner to Machaerus, a fortress on the border of Antipas's territory and that of the Nabatean king Aretas, east of the Dead Sea, and had him executed there. Matthew's own account never names a location for John's imprisonment (11:2); the specific place comes entirely from this external source." }
'machaerus-voros-excavation' = @{ title="A fortress confirmed on the ground"; body="Gyozo Voros, Machaerus I: History, Archaeology and Architecture of the Fortified Herodian Royal Palace and City Overlooking the Dead Sea in Transjordan. Final Report of the Excavations and Surveys 1807-2012, Studium Biblicum Franciscanum Collectio Maior 53 (Milan: Edizioni Terra Santa, 2013), continued in Machaerus II (Collectio Maior 55, 2015) and Machaerus III (Collectio Maior 56, 2019) in the same series. Voros's Hungarian Academy of Arts mission, directed under the Studium Biblicum Franciscanum in Jerusalem since 2009 and building on earlier American-Baptist and Italian-Franciscan survey and excavation work at the site reaching back to 1968, recovered and reconstructed more than 100,000 architectural fragments confirming a fortified Herodian palace-citadel at the site, matching its known history as a border fortress and royal residence." }
'matthew-11-3-embarrassment-argument' = @{ title="Why scholars read John's doubt as a point against invention"; body="W. D. Davies and Dale C. Allison Jr., Matthew 8-18, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 11:2-6. Davies and Allison note that a scene depicting John the Baptist — the very figure Matthew's own narrative has already shown recognizing Jesus and even protesting that Jesus should be baptizing him instead (3:14) — expressing uncertainty about Jesus's identity from prison sits awkwardly against the early church's evident interest in presenting John as Jesus's unambiguous forerunner and herald, which is part of why the episode is commonly read as unlikely to have been invented outright by the later tradition." }
'malachi-4-5-6-elijah-prophecy' = @{ title="The prophecy behind the identification"; body="Andrew E. Hill, Malachi: A New Translation with Introduction and Commentary, Anchor Bible vol. 25D (New York: Doubleday, 1998), commentary ad loc. Malachi 4:5-6 (numbered 3:23-24 in the Hebrew Bible/Jewish Publication Society versification). Malachi closes the book with a promise that God will send 'the prophet Elijah before the great and terrible day of the LORD comes,' tasked with turning the hearts of parents and children toward one another so the land is not struck with a curse. Hill's commentary situates this promise as a distinct, later prophetic layer within the book rather than part of Malachi's earlier oracles; it is this specific promise that Matthew's Jesus applies directly to John the Baptist at 11:14." }
'elijah-explicit-identification-escalation' = @{ title="From costume to claim"; body="W. D. Davies and Dale C. Allison Jr., Matthew 8-18, International Critical Commentary (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 11:14. Davies and Allison read Matthew 11:14 as the text's decisive move from implication to assertion: where Matthew 3:4's description of John's clothing only echoes 2 Kings 1:8's description of Elijah without saying so outright, 11:14 has Jesus state the identification as a direct claim in his own voice, conditioned only by 'if you are willing to accept it' — phrasing the commentary reads as itself acknowledging the identification was a contested, non-obvious claim even within the narrative's own world." }
'chorazin-basalt-synagogue-yeivin' = @{ title="A settlement older than its surviving synagogue"; body="Ze'ev Yeivin, The Synagogue at Korazim: The 1962-1964, 1980-1987 Excavations, Israel Antiquities Authority Reports (Jerusalem: Israel Antiquities Authority, 2000). Alongside the well-known basalt synagogue building (dated on stylistic and stratigraphic grounds to the late third or fourth century CE), Yeivin's excavation recovered an olive press establishing that the site was already a working settlement by the second century CE — meaning Chorazin was an active town for roughly two centuries before the synagogue building visible at the site today was ever constructed, consistent with (though not proof of) continuous habitation back through the first century." }
'yoke-of-torah-mishnah-berakhot' = @{ title="Accepting the yoke, twice, in one line of liturgy"; body="Mishnah Berakhot 2:2, in Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933). The Mishnah frames the twice-daily recitation of the Shema as an act of formally 'taking upon oneself the yoke of the kingdom of heaven' before 'the yoke of the commandments,' per Rabbi Joshua ben Korchah's ruling on the proper order of the two. Though this specific Mishnaic formulation postdates the Gospels by a century or more in its written form, it documents a durable, well-attested Jewish idiom describing religious obligation itself as a 'yoke' one deliberately takes on — the same image, not a novel one, that Matthew's Jesus uses in 11:29-30." }
'yoke-avot-3-5-nehunya' = @{ title="The yoke of Torah versus the yoke of the world"; body="Mishnah Avot 3:5, in Herbert Danby, trans., The Mishnah (Oxford: Oxford University Press, 1933). A saying attributed to the sage Nehunya ben ha-Kanah states that whoever takes upon himself 'the yoke of the Torah' has the yoke of civil government and of worldly cares removed from him, while whoever throws off the yoke of the Torah has both of those other yokes laid on him instead. The same 'yoke' vocabulary used elsewhere in rabbinic tradition for religious obligation is applied here specifically to Torah study and observance as a chosen discipline rather than an imposed burden — the same reversal (a yoke described as freeing rather than crushing) that Matthew's Jesus makes explicit at 11:30." }
'sirach-51-wisdom-yoke-parallel' = @{ title="Wisdom's own invitation, in the same order"; body="Ben Sira (Sirach) 51:23-27, in Patrick W. Skehan and Alexander A. Di Lella, The Wisdom of Ben Sira, Anchor Bible vol. 39 (New York: Doubleday, 1987); the structural parallel to Matthew 11:28-30 is developed further in Celia Deutsch, Hidden Wisdom and the Easy Yoke: Wisdom, Torah and Discipleship in Matthew 11:25-30, Journal for the Study of the New Testament Supplement Series 18 (Sheffield: JSOT Press, 1987). Sirach has personified Wisdom issue an invitation — 'draw near to me, you who are uneducated ... put your neck under her yoke, and let your souls receive instruction' — promising that those who submit will find rest close by. The sequence of moves (an invitation to come, a yoke to take up, an offer of instruction, a promise of rest) runs in the same order in both passages; Deutsch and others read Matthew's Jesus here as speaking with Wisdom's own voice from Sirach, not composing an unprecedented image." }
'matthew-11-30-chrestos-translation' = @{ title="Easy, or well-fitting? A word under fresh scrutiny"; body="Lincoln H. Blumell, 'Is Jesus's Yoke Easy? Reconsidering the Translation of Chrestos in Matthew 11:30,' BYU Studies 65, no. 1 (2026). Blumell argues that the Greek adjective chrestos in Matthew 11:30, rendered 'easy' in English translations for centuries, more consistently means 'useful,' 'fitting,' or 'well-suited' across its wider range of use in period Greek sources — describing a yoke shaped correctly to the animal wearing it rather than one requiring no effort at all. The distinction doesn't change the verse's basic sense of relief and rest, but it reframes the specific claim being made about the yoke itself." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders; existing notes 16, 88, 89, 91 cited directly by number) ----
$beat1 = @'
John's imprisonment is stated by Matthew only in the vaguest terms — he is simply "in prison" when he hears about the deeds of the Messiah and sends his disciples (11:2). Matthew never names the prison, and nothing in the immediate text lets a reader place it on a map. External evidence fills that gap directly: Josephus, describing John's arrest and eventual execution as a straightforward historical episode with no reference to the Gospel tradition at all, names the specific fortress — Machaerus, a hilltop citadel east of the Dead Sea in what is now Jordan, built up by Herod the Great and used by Herod Antipas as a prison [[NOTE:josephus-machaerus-location]] (see JOHN THE BAPTIST for the same passage's divergence from the Gospels on John's motive for arrest).

Machaerus is not a name resting only on later legend; it is a real, extensively excavated Herodian site. Since 2009 a Hungarian Academy of Arts mission directed by Gyozo Voros, working under the Studium Biblicum Franciscanum in Jerusalem, has surveyed and excavated the fortress across multiple campaigns, publishing full architectural and material findings as a three-volume final report; the recovered building fragments and reconstructed floor plan confirm a fortified Herodian royal palace-citadel matching the general scale and character Josephus describes [[NOTE:machaerus-voros-excavation]]. This is a case where the Gospel text itself supplies no location at all, and the specific answer comes entirely from outside it — not a retrofitted pilgrimage tradition invented centuries later, the pattern this book has flagged elsewhere for Nazareth's cliff or the Mount of Temptation, but a name already fixed by a contemporary historian and since confirmed on the ground by archaeology.
'@

$beat2 = @'
John's question itself is worth pausing on as a matter of method, not just narrative color. "Are you the one who is to come, or are we to wait for another?" is a strange thing for Matthew's own Gospel to put in John's mouth, since Matthew has already staged John recognizing Jesus at the Jordan and even protesting that Jesus should be baptizing him instead (3:14). A church composing this exchange from scratch, decades after the fact, had every incentive to make its own founding prophet's recognition of Jesus clean and uncomplicated; it had essentially nothing to gain by depicting him wavering from a prison cell. Historical-critical commentary reads that mismatch as an application of the criterion of embarrassment introduced earlier in this book [16]: material cutting against the community's own interests is judged more likely preserved than invented, and John's doubt here is one of the passages commentators flag specifically along those lines [[NOTE:matthew-11-3-embarrassment-argument]].

None of this proves John actually harbored doubts in a Herodian cell; the criterion argues for probability, not certainty, and a scene can be both awkward for the church and still a later composition serving some purpose not yet identified. What the argument does establish is why this particular verse gets cited so often in historical-Jesus scholarship specifically as a data point cutting against wholesale invention, rather than as one more devotional episode among many.
'@

$beat3 = @'
"If you are willing to accept it, he is Elijah who is to come" (11:14) is Jesus speaking, not Matthew's narrator, and that distinction matters. Earlier in this book, Matthew's description of John's camel's-hair garment and leather belt was read as a compositional echo of 2 Kings 1:8's description of Elijah [88] — a visual, unstated identification built for a reader who already knows the older text, resting on an underlying Hebrew phrase itself ambiguous between "a hairy man" and "a man wearing a hairy garment" [89]. Eight chapters later, the identification stops being implicit. Jesus states it outright, as a claim about who John is, tying it directly to Malachi's prophecy that God would send "the prophet Elijah before the great and terrible day of the LORD comes" (Malachi 4:5-6) [[NOTE:malachi-4-5-6-elijah-prophecy]]. Historical-critical commentary treats this as a real escalation in the text's own argument, not a repetition of the earlier clothing detail: what chapter 3 only dressed the reader into inferring, chapter 11 has the story's central figure assert as settled fact [[NOTE:elijah-explicit-identification-escalation]].

That escalation doesn't resolve the wider disagreement this book has already flagged. John's Gospel still has the Baptist personally deny being Elijah when asked directly (John 1:21) [91], and Matthew's own later Transfiguration scene (17:10-13) has Jesus repeat the identification a second time, glossing it again as John — meaning Matthew doubles down on a claim its sister Gospel has the man himself reject. The four Gospels are not brought into agreement here; this chapter simply marks the point where Matthew's Jesus moves from visual echo to direct assertion.
'@

$beat4 = @'
The three towns in Jesus's woe — Chorazin, Bethsaida, Capernaum — are not equally well anchored on the ground, and the differences are worth separating out. Capernaum's archaeology (the basalt-versus-limestone synagogue dispute, the excavated "House of Peter") has already been covered at length earlier in this book and isn't repeated here.

Chorazin is the more securely identified of the other two: the site is Korazim National Park, on the basalt plateau roughly two miles north of Capernaum, and Ze'ev Yeivin's excavations for Israel's Department of Antiquities (1962-1964, resumed 1980-1987) uncovered an elaborately carved basalt synagogue dated to the late third or fourth century CE. What's less often noted is that Yeivin's dig also turned up an olive press establishing the site was already an active settlement by the second century CE — occupied well before the synagogue standing there today was ever built, and consistent with a working Galilean town on Jesus's own circuit two centuries earlier still [[NOTE:chorazin-basalt-synagogue-yeivin]] (fuller detail at CHORAZIN).

Bethsaida is the harder case, and this book has already laid that dispute out in full: which of two excavated sites, et-Tell or el-Araj, is the historical Bethsaida remains a genuine, unresolved argument between working archaeological teams (see BETHSAIDA). Nothing in this chapter's own text — "woe to you, Bethsaida" — depends on resolving that argument; Matthew's Jesus addresses the town by name and reputation, not by grid reference.
'@

$beat5 = @'
The chapter's closing image — "take my yoke upon you ... for my yoke is easy, and my burden is light" (11:28-30) — is not Jesus inventing a metaphor from nothing. "Yoke" was already a fixed, working image in Second Temple and early rabbinic Judaism for the discipline of living under God's law: the Mishnah frames the daily recitation of the Shema as first "accepting the yoke of the kingdom of heaven," then, in the same breath, "the yoke of the commandments" (m. Berakhot 2:2) [[NOTE:yoke-of-torah-mishnah-berakhot]], and a separate saying attributed to the sage Nehunya ben ha-Kanah holds that whoever takes on "the yoke of the Torah" is thereby freed from "the yoke of government and the yoke of worldly concerns" (m. Avot 3:5) [[NOTE:yoke-avot-3-5-nehunya]]. Jesus offering his own yoke as something to "take up" places him inside a recognizable idiom his audience would already have understood as being about submission to religious obligation, not against a blank cultural slate.

The invitation has an even closer literary parallel than the rabbinic sayings supply on their own. The non-canonical wisdom book Sirach, roughly two centuries older than Matthew, has personified Wisdom herself issue an almost identically shaped call: "draw near to me, you who are uneducated ... put your neck under her yoke, and let your souls receive instruction" (Sirach 51:23, 26), promising rest to those who submit [[NOTE:sirach-51-wisdom-yoke-parallel]]. The sequence — come, take the yoke, learn, find rest — runs in the same order in both texts, and mainstream commentary reads Matthew's Jesus here as speaking in Wisdom's own voice, claiming a role Sirach assigns to divine Wisdom itself.

One further wrinkle sits in the single word usually rendered "easy." The underlying Greek is chrestos, and recent philological work argues the word's ordinary range in period Greek runs closer to "useful," "fitting," or "well-suited" than to "not difficult" — the sense of a yoke shaped correctly to the animal wearing it, not one that requires no effort at all [[NOTE:matthew-11-30-chrestos-translation]]. If that reading holds, Jesus's closing promise is less "this will be effortless" and more "this will fit you" — a smaller shift in English, but a real one in the underlying Greek.
'@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'MACHAERUS (FORTRESS)' = "A Herodian hilltop fortress east of the Dead Sea, in modern Jordan, identified by the first-century historian Flavius Josephus as the prison where Herod Antipas held John the Baptist and where John was executed (Jewish Antiquities 18.5.2) — a location Matthew's own account of John's imprisonment (11:2) never names (see JOHN THE BAPTIST for the fuller Josephus comparison, including where his account diverges from the Gospels on John's motive). The site is not a matter of later pilgrimage guesswork: since 2009 a Hungarian Academy of Arts mission led by Gyozo Voros, working under the Studium Biblicum Franciscanum in Jerusalem, has excavated and reconstructed the fortress across multiple campaigns, publishing full findings in the three-volume Machaerus series (Collectio Maior 53, 55, 56; 2013-2019), confirming a fortified Herodian royal palace-citadel matching Josephus's description on the ground [[NOTE:machaerus-voros-excavation]].

Cited in: Matthew (beat covering 11:1-2)."
'YOKE OF THE TORAH / KINGDOM OF HEAVEN (RABBINIC METAPHOR)' = "The Jewish idiom, attested in the Mishnah, of describing religious obligation itself as a 'yoke' voluntarily taken up: reciting the Shema is framed as first accepting 'the yoke of the kingdom of heaven' and then 'the yoke of the commandments' (m. Berakhot 2:2) [[NOTE:yoke-of-torah-mishnah-berakhot]], while a separate saying attributed to Nehunya ben ha-Kanah holds that taking on 'the yoke of the Torah' removes the yoke of government and worldly cares (m. Avot 3:5) [[NOTE:yoke-avot-3-5-nehunya]]. Both Mishnaic formulations postdate the Gospels by a century or more in their written form, but they document a durable image already available in the wider Second Temple and early rabbinic world — the same image Matthew's Jesus uses when inviting hearers to 'take my yoke upon you' at 11:29-30, and which Ben Sira's Wisdom figure uses in nearly the same sequence two centuries earlier (Sirach 51:23-27) [[NOTE:sirach-51-wisdom-yoke-parallel]].

Cited in: Matthew (beat covering 11:28-30)."
'MALACHI (PROPHET)' = "The prophetic book (traditionally dated to the fifth century BCE, in the early post-exilic Persian period) that closes the Hebrew Bible's Twelve Minor Prophets and, in its final verses, promises that God will send 'the prophet Elijah before the great and terrible day of the LORD comes' to turn the hearts of parents and children toward each other (Malachi 4:5-6; numbered 3:23-24 in the Hebrew Bible/Jewish Publication Society tradition) [[NOTE:malachi-4-5-6-elijah-prophecy]]. This specific promise is the scriptural basis for Jesus's direct identification of John the Baptist with the returning Elijah at Matthew 11:14 (see ELIJAH for the fuller history of that identification across the four Gospels, including the Fourth Gospel's contrary account of John denying the title himself).

Cited in: Matthew (beat covering 11:2-15)."
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
$sortKey = $maxChapterSortKey + 1000
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch11NodeId $id $sortKey
    $sortKey += 100
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

# ---- Seed new entities (Machaerus and Gyozo Voros already exist in the entity catalog; not re-seeded) ----
Seed-Entity "Malachi (Prophet)" "malachi-prophet" "character" "Fifth-century BCE post-exilic prophet whose closing oracle (Malachi 4:5-6) promises the return of Elijah before the day of the LORD; the scriptural basis for Matthew 11:14's identification of John the Baptist with Elijah."
Seed-Entity "Nehunya ben ha-Kanah" "nehunya-ben-ha-kanah" "character" "Tannaitic-era rabbinic sage credited in Mishnah Avot 3:5 with the saying contrasting the 'yoke of the Torah' with the yoke of government and worldly cares."

$conn.Close()
Write-Host "DONE Matthew Chapter 11 depth-pass."
