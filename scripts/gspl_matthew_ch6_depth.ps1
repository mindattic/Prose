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
$Ch6NodeId = [guid]"019FA065-8069-7E01-98D2-686871E63831"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh6SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA065-8069-7E01-98D2-686871E63831' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh6SortKey=$maxCh6SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'kaddish-daily-bread-ninth-benediction' = @{ title="The 'daily bread' petition and the Amidah's ninth blessing"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 1, International Critical Commentary (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 6:9-13. Davies and Allison line the Lord's Prayer up against the Amidah's Eighteen Benedictions petition by petition and find a structural parallel beyond mere shared themes: the ninth benediction, a petition for the year's sustenance, occupies almost exactly the same hinge position inside the eighteen-part Jewish prayer that 'give us this day our daily bread' occupies inside the Lord's Prayer's shorter set of petitions." }
'elbogen-kaddish-origin' = @{ title="The Kaddish's synagogue setting, older than the comparison itself"; body="Ismar Elbogen, Jewish Liturgy: A Comprehensive History, trans. Raymond P. Scheindlin (Philadelphia and New York: Jewish Publication Society and the Jewish Theological Seminary, 1993; German original 1913), chapters on the origin and development of the Kaddish and the Amidah. Elbogen's classic critical history traces the Kaddish's 'magnified and sanctified be his great name... may he establish his kingdom' language to old, pre-Mishnaic synagogue vocabulary that closed out the service, not to phrasing coined for comparison with the Lord's Prayer after the fact." }
'instone-brewer-eighteen-benedictions-dating' = @{ title="How early is 'early'? A dating caveat on the Amidah"; body="David Instone-Brewer, 'The Eighteen Benedictions and the Minim before 70 CE,' Journal of Theological Studies 54, no. 1 (April 2003): 25-44. Rabbinic tradition credits Gamaliel II, presiding at Yavneh after 70 CE, with fixing the Eighteen Benedictions into their standard order, making the Amidah as a single stable text a slightly later institution than Jesus's own lifetime. Instone-Brewer's analysis of Genizah fragment evidence argues that specific constituent petitions of the Eighteen predate the Temple's destruction even though the fixed, numbered sequence does not — meaning any claim that Jesus drew on the Amidah must rest at the level of shared petitions and vocabulary, not a claim that he quoted an already-published prayer book." }
'hammer-amidah-english-text' = @{ title="Reading the petitions side by side"; body="Reuven Hammer, Entering Jewish Prayer: A Guide to Personal Devotion and the Worship Service (New York: Schocken Books, 1994), chapters on the Amidah and its Eighteen Benedictions. Hammer's standard modern English translation of the Eighteen Benedictions preserves petitions for God's name, God's kingdom, forgiveness, and rescue from harm in roughly the sequence familiar from the Lord's Prayer, making the family resemblance visible in translation rather than only in scholarly paraphrase." }
'metzger-matt-6-13-doxology' = @{ title="Absent from the earliest and best manuscripts"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: United Bible Societies, 1994), 16-17, commentary ad loc. Matthew 6:13. Metzger's standard textual-critical reference work documents that the doxology ('for thine is the kingdom, and the power, and the glory, for ever, Amen') is absent from the earliest and most reliable Greek manuscripts, including Codex Sinaiticus and Codex Vaticanus, from the Old Latin and early Sahidic and Fayumic versions, and from citations of the prayer by Tertullian, Origen, and Cyprian — evidence Metzger and the near-universal consensus of textual critics read as showing the doxology to be a later liturgical addition rather than part of Matthew's original text." }
'luke-11-no-doxology-parallel' = @{ title="Luke's version never had one to lose"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: United Bible Societies, 1994), commentary ad loc. Luke 11:2-4; cf. the apparatus of the Nestle-Aland Novum Testamentum Graece at the same verses. No manuscript tradition of Luke's parallel version of the Lord's Prayer carries a closing doxology of any form, consistent with the doxology being a later addition to the prayer's liturgical use rather than an authentic saying independently preserved by both Evangelists." }
'didache-8-2-shorter-doxology' = @{ title="An early doxology, but a different one"; body="Didache 8.2, in The Apostolic Fathers, vol. 1, ed. and trans. Bart D. Ehrman, Loeb Classical Library 24 (Cambridge, MA: Harvard University Press, 2003). The Didache, usually dated to roughly 80-120 CE and so possibly nearly contemporary with Matthew's Gospel, already attaches a doxology to its own version of the Lord's Prayer, but its wording reads only 'for thine is the power and the glory for ever,' omitting the word 'kingdom' found in the now-familiar threefold form — evidence that the doxology's exact wording was still unsettled in Christian liturgical practice well into the second century." }
'davies-allison-opheilemata-paraptomata' = @{ title="Two different Greek words, three verses apart"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, vol. 1, International Critical Commentary (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 6:12 and 6:14-15. Davies and Allison note that Matthew's Greek uses two distinct words within six verses: opheilemata (6:12), a genuine financial-debt term used elsewhere for owing money, and paraptomata (6:14-15), a separate word for a moral misstep or transgression, used when Jesus explains the petition — meaning 'forgive us our debts' is the literal rendering of the prayer's own verse, not a softened or symbolic one." }
'tyndale-trespasses-1526' = @{ title="Where 'trespasses' actually comes from"; body="William Tyndale, The New Testament: A Reprint of the Edition of 1526, ed. N. Hardy Wallis (Cambridge: Cambridge University Press for the British and Foreign Bible Society, 1938), Matthew 6:12. Tyndale's 1526 English New Testament renders Matthew 6:12 as 'forgeve vs oure treaspases,' apparently carrying the sense of the neighboring verse's different Greek word (paraptomata, 6:14-15) backward into his translation of the debt-word in verse 12. Once absorbed into the 1549 Book of Common Prayer, 'trespasses' became the standard wording across English Catholic, Anglican, and Methodist recitation for centuries, regardless of what the prayer's own Greek word in verse 12 actually says." }
'fitzmyer-luke-11-4-sins-debt-mix' = @{ title="Luke's own version splits the difference"; body="Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday & Company, 1981), commentary ad loc. Luke 11:4. Luke's parallel version of the prayer asks forgiveness for 'sins' (using hamartia, the ordinary New Testament word for sin) while in the same verse describing what we forgive others in debt-language ('everyone who is indebted to us'), showing that debt and sin were already being used as near-interchangeable metaphors for the same thing across independent early Christian prayer traditions, well before any English translator had to fix a single word." }
'mishnah-shekalim-chamber-of-secrets' = @{ title="A Temple room built for exactly this purpose"; body="Mishnah Shekalim 5:6, in The Mishnah, trans. Herbert Danby (Oxford: Oxford University Press, 1933). The Mishnah describes the Jerusalem Temple's 'Chamber of Secrets' (lishkat hasha'im), a designated room where the discreetly pious deposited charitable funds and the respectable poor collected support, with neither side learning the other's identity — a pre-70 CE, in-Temple institutional analog to 'do not let your left hand know what your right hand is doing,' independent of anything in the Gospels." }
'maimonides-ladder-of-charity' = @{ title="A ladder with anonymity near the top"; body="Moses Maimonides, Mishneh Torah, Sefer Zeraim, Hilkhot Matnot Aniyim (Laws of Gifts to the Poor) 10:7-14, trans. in Isadore Twersky, ed., A Maimonides Reader (New York: Behrman House, 1972), 137-140. Maimonides's twelfth-century systemization ranks giving to an unknown recipient, and giving anonymously to an unknown poor person, among the highest rungs of an eight-level scale of charitable giving, formalizing centuries later a concern for concealed giving that the text of Matthew 6:3 already assumes as an established value." }
'bava-batra-9b-secret-charity' = @{ title="Rabbi Elazar's ranking"; body="Babylonian Talmud, Bava Batra 9b (William Davidson Talmud translation, Sefaria; cf. the Soncino Talmud translation). The Talmud credits Rabbi Elazar with the teaching that one who gives charity in secret is 'greater than Moses,' grounding the claim in Proverbs 21:14, 'a gift in secret pacifies anger' — a rabbinic-era textual attestation of the anonymous-giving value independent of, and earlier in written form than, Maimonides's later systematic ladder." }
'anderson-tobit-charity' = @{ title="Older than the rabbis: Tobit's almsgiving ethic"; body="Gary A. Anderson, Charity: The Place of the Poor in the Biblical Tradition (New Haven: Yale University Press, 2013), drawing on Tobit 4:7-11 (a Second Temple-period text usually dated to the third or second century BCE). Anderson traces the ethic of almsgiving performed without self-display back into Second Temple Jewish wisdom literature, including Tobit and Ben Sira, well before both Jesus and the later rabbinic and Maimonidean formulations of the same value." }
'betz-cynic-stoic-anxiety' = @{ title="A recognized philosophical genre, not a novelty"; body="Hans Dieter Betz, The Sermon on the Mount: A Commentary on the Sermon on the Mount, Including the Sermon on the Plain (Matthew 5:3-7:27 and Luke 6:20-49), Hermeneia (Minneapolis: Fortress Press, 1995), commentary on Matthew 6:25-34 ('On Anxiety'). Betz situates the birds-and-lilies passage within the conventions of Hellenistic Cynic-Stoic popular philosophical diatribe on freedom from material anxiety (merimna), a recognized genre of moral teaching current in the wider first-century Mediterranean world, while explicitly framing the resemblance as structural and rhetorical rather than a claim of direct literary borrowing in either direction." }
'downing-cynic-maximalist-contrast' = @{ title="A stronger reading, and the pushback it drew"; body="F. Gerald Downing, Cynics and Christian Origins (Edinburgh: T&T Clark, 1992). Downing argues for a considerably stronger, more direct Cynic influence on the Jesus tradition generally than Betz's cautious structural-parallel reading allows; reviewers in the Scottish Journal of Theology and the Journal of Ecclesiastical History found the case for direct dependence overreached, illustrating that mainstream scholarly opinion sits closer to Betz's more modest comparison than to Downing's maximalist thesis." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The Kaddish/Amidah comparison already drawn for the Lord's Prayer's opening lines holds up under closer, petition-by-petition scrutiny, and the parallel runs deeper than a shared mood. W. D. Davies and Dale C. Allison's standard critical commentary on Matthew lines the two prayers up clause by clause and finds that the ninth of the Amidah's Eighteen Benedictions — the petition for the year's harvest and sustenance — occupies almost exactly the same structural position inside that eighteen-part prayer that "give us this day our daily bread" occupies inside the Lord's Prayer's much shorter set of petitions: a turn, partway through, from praise of God's name and reign toward the community's daily material need [[NOTE:kaddish-daily-bread-ninth-benediction]]. Ismar Elbogen's classic history of Jewish liturgy traces the Kaddish itself to the same vocabulary world — "magnified and sanctified be his great name... may he establish his kingdom" is old, pre-Mishnaic synagogue language closing out the service, not phrasing coined for the sake of this comparison after the fact [[NOTE:elbogen-kaddish-origin]].

One caution belongs alongside the parallel, because it is easy to overstate. Rabbinic tradition credits Gamaliel II, presiding at Yavneh after the Temple's fall in 70 CE, with fixing the Eighteen Benedictions into their standard order, which makes the Amidah as a single, stable, recitable text a slightly later institution than Jesus's own lifetime. David Instone-Brewer's study of Genizah fragment evidence argues that specific petitions inside the Eighteen predate the Temple's destruction even if the fixed, numbered sequence does not [[NOTE:instone-brewer-eighteen-benedictions-dating]]. The honest claim, then, is not that Jesus quoted an already-published prayer book; it is that he assembled his own prayer from petitions — God's name hallowed, God's kingdom sought, daily bread requested, debts forgiven, evil escaped — that were already the common devotional vocabulary of first-century synagogue Judaism, the same building blocks Yavneh's rabbis were independently arranging into the Amidah's fixed form a generation or two later. Reading the two prayers' English side by side makes the family resemblance immediate rather than academic: modern translations of the Eighteen Benedictions preserve petitions for God's name, God's kingdom, forgiveness, and rescue from harm in roughly the same sequence the Lord's Prayer uses [[NOTE:hammer-amidah-english-text]]. None of this makes the Lord's Prayer a copy. It supports what the mainstream historical-critical reading has long argued: a distinctively compressed, Christian condensation of forms a Jewish congregation in Nazareth or Capernaum would have recognized instantly, not an unprecedented new genre of prayer.
'@

$beat2 = @'
The line that closes the Lord's Prayer in most familiar English versions — "for thine is the kingdom, and the power, and the glory, for ever, Amen" — is one of the cleanest, most settled cases in all of New Testament textual criticism, and the settled answer is that Matthew almost certainly never wrote it. Bruce Metzger's standard textual commentary lays out the manuscript evidence plainly: the doxology is absent from the earliest and most reliable Greek manuscripts — Codex Sinaiticus and Codex Vaticanus among them — absent from the Old Latin and early Sahidic and Fayumic versions, and absent from the Lord's Prayer as quoted by early commentators including Tertullian, Origen, and Cyprian, none of whom show any sign of knowing it [[NOTE:metzger-matt-6-13-doxology]]. Luke's version of the same prayer, a different Gospel over (Luke 11:2-4), never had a doxology to lose in the first place; no manuscript tradition of Luke's version carries one, which is exactly what should be true if the line is a later liturgical addition rather than an authentic saying both Evangelists independently preserved [[NOTE:luke-11-no-doxology-parallel]].

That does not mean the doxology came from nowhere, or that it is a modern invention. The Didache, a Christian instructional text usually dated to somewhere around 80-120 CE — meaning it could be nearly as old as Matthew's Gospel itself — already attaches a doxology to its own version of the Lord's Prayer. But the Didache's ending reads only "for thine is the power and the glory for ever," missing the word "kingdom" that later, familiar versions include [[NOTE:didache-8-2-shorter-doxology]]. That mismatch is itself informative: if the doxology had been part of the prayer from the start, its wording should have stayed put across independent early copies. Instead, the earliest evidence shows a prayer whose ending was still being composed and adjusted in Christian liturgical practice well into the second century, converging on the now-standard threefold form only later still — a textbook liturgical accretion, not a genuine text-critical dispute.
'@

$beat3 = @'
Ask most English-speaking Christians to recite the Lord's Prayer and a good number will say "forgive us our trespasses, as we forgive those who trespass against us." Ask what Matthew's Greek actually says, and the answer is different: opheilemata, a plain financial-debt word — the same root used elsewhere for owing money, not for sin in the abstract [[NOTE:davies-allison-opheilemata-paraptomata]]. "Debts" is not a softened or symbolic translation; it is the literal one. "Trespasses" enters English Bible tradition through William Tyndale's 1526 New Testament, and it enters at exactly this verse for a traceable, non-mysterious reason: two verses later, in 6:14-15, Matthew's Greek genuinely does switch words, to paraptomata — a term for a moral misstep or falling-aside, not a debt — when Jesus explains the petition ("if you forgive others their paraptomata, your heavenly Father will also forgive you"). Tyndale appears to have carried that second word's sense backward into his rendering of verse 12, and once his phrasing was absorbed into the 1549 Book of Common Prayer, "trespasses" became the standard wording across English Catholic, Anglican, and Methodist recitation for centuries afterward, regardless of what the prayer itself actually says in Greek [[NOTE:tyndale-trespasses-1526]].

The two words sitting three verses apart are not a scribal accident; Matthew uses them deliberately, one right after the other. Joseph Fitzmyer's commentary on the Lukan parallel shows the same instability running through the wider Gospel tradition rather than being unique to Matthew's manuscript history: Luke's version of the prayer asks forgiveness for "sins" (using hamartia, the ordinary New Testament word for sin) while in the very same verse describing what we forgive others in debt-language, "everyone who is indebted to us" [[NOTE:fitzmyer-luke-11-4-sins-debt-mix]]. Debt and sin, in other words, were already being used as near-interchangeable metaphors for the same thing across independent early Christian prayer traditions, well before any English translator had to choose a single word and settle the matter for the next five hundred years.
'@

$beat4 = @'
"Do not let your left hand know what your right hand is doing" (6:3) sounds like a vivid one-off metaphor, but it names a real, load-bearing value in Second Temple and rabbinic Jewish ethical practice: giving that protects a recipient's dignity by concealing the giver's identity, and vice versa. The clearest institutional case is architectural rather than literary. The Jerusalem Temple maintained an actual "Chamber of Secrets" (lishkat hasha'im), a designated room where, according to the Mishnah, the quietly pious deposited funds and the respectable poor collected support, neither side ever learning the other's identity [[NOTE:mishnah-shekalim-chamber-of-secrets]] — a pre-70 CE, in-the-Temple version of exactly the "left hand, right hand" concern Jesus names, existing independently of anything in the Gospels. Centuries later, Maimonides's Mishneh Torah systematized the value into a famous eight-rung ladder of charity, placing anonymous giving to an unknown recipient near the very top, just below helping someone become self-sufficient before they ever have to ask [[NOTE:maimonides-ladder-of-charity]] — but the value long predates Maimonides's twelfth-century system-building. The Babylonian Talmud already credits Rabbi Elazar with the claim that whoever gives charity in secret is "greater than Moses," reading Proverbs 21:14's "a gift in secret pacifies anger" as scriptural warrant [[NOTE:bava-batra-9b-secret-charity]], and Gary Anderson's study of biblical charity ethics traces the underlying value back further still, into the apocryphal book of Tobit's second-century-BCE instructions on almsgiving performed without self-display [[NOTE:anderson-tobit-charity]]. Jesus's saying, in other words, assumes an audience that already prizes concealment in giving as a mark of righteousness; it sharpens an existing value rather than introducing a new one.

The chapter's closing image — birds fed without sowing, lilies clothed without spinning, "do not be anxious about tomorrow" — belongs to a different, wider comparative context: not Jewish liturgy or halakha, but Greco-Roman popular moral philosophy. Hans Dieter Betz's Hermeneia commentary situates this passage within the conventions of Hellenistic Cynic-Stoic diatribe on freedom from material anxiety (merimna), a recognized genre of popular ethical teaching current in the wider first-century Mediterranean world, built around exactly this kind of appeal to nature's provision as evidence against needless worry [[NOTE:betz-cynic-stoic-anxiety]]. Betz is careful to frame this as a structural and rhetorical resemblance, not a claim that Jesus had read Greek philosophers or that either tradition borrowed directly from the other. Not every scholar draws the line as cautiously: F. Gerald Downing has argued for a stronger, more direct Cynic influence on the Jesus tradition generally, a thesis that drew real scholarly pushback for overreading the parallels [[NOTE:downing-cynic-maximalist-contrast]]. The passage's real evidentiary weight sits with Betz's more modest claim: first-century Galilee sat inside a Mediterranean world where "do not be anxious about possessions" was already a recognizable philosophical move, available to a teacher independent of any specific book he might or might not have read.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
"LORD'S PRAYER (TEXTUAL HISTORY)" = "The prayer Jesus teaches his disciples at 6:9-13, whose transmission history contains two well-documented, checkable puzzles distinct from the prayer's content itself. First, the familiar closing doxology ('for thine is the kingdom, and the power, and the glory, for ever, Amen') is absent from the earliest and best Greek manuscripts and from Luke's parallel version entirely, and is judged by the near-universal consensus of textual critics to be a later liturgical addition rather than part of Matthew's original text [[NOTE:metzger-matt-6-13-doxology]] [[NOTE:luke-11-no-doxology-parallel]]; the Didache already attests an early but differently-worded doxology by the early second century [[NOTE:didache-8-2-shorter-doxology]]. Second, the familiar English 'forgive us our trespasses' rests on a conflation: Matthew's actual Greek word at 6:12 is opheilemata, a financial-debt term, while 'trespasses' derives from paraptomata, the different word Matthew uses two verses later at 6:14-15 [[NOTE:davies-allison-opheilemata-paraptomata]], a substitution traceable to William Tyndale's 1526 translation choice [[NOTE:tyndale-trespasses-1526]]."
'KADDISH AND AMIDAH (JEWISH LITURGICAL PARALLELS)' = "Two closely related first-century Jewish liturgical forms whose petitions the Lord's Prayer (6:9-13) recognizably compresses: the Kaddish, an Aramaic prayer sanctifying God's name and petitioning for God's kingdom that concluded the synagogue service, and the Amidah (the Eighteen Benedictions), Second Temple Judaism's standing prayer of fixed daily petitions for sustenance, forgiveness, and deliverance. Joachim Jeremias's classic study first drew the comparison in detail; later critical commentary finds the parallel holds petition by petition, not just thematically, with the Amidah's ninth benediction occupying roughly the same structural position as the Lord's Prayer's bread petition [[NOTE:kaddish-daily-bread-ninth-benediction]] [[NOTE:elbogen-kaddish-origin]] [[NOTE:hammer-amidah-english-text]]. The Amidah's fixed, numbered form is somewhat later than Jesus's own lifetime, codified at Yavneh after 70 CE, though specific constituent petitions are argued to predate the Temple's destruction [[NOTE:instone-brewer-eighteen-benedictions-dating]]. None of this makes the Lord's Prayer's content less original to Jesus; it situates it as a distinctively Christian condensation of recognizably Jewish prayer forms, not an unprecedented new genre."
'MATAN BA-SETER (ANONYMOUS CHARITY)' = "The Hebrew term for 'giving in secret,' the developed halakhic concept most famously systematized by Maimonides in his twelfth-century eight-rung ladder of charity, which ranks anonymous giving to an unknown recipient near its highest tiers [[NOTE:maimonides-ladder-of-charity]]. The underlying value is considerably older than Maimonides's system: the Jerusalem Temple maintained an actual 'Chamber of Secrets' where donations and support changed hands anonymously [[NOTE:mishnah-shekalim-chamber-of-secrets]], the Babylonian Talmud credits Rabbi Elazar with ranking secret charity above Moses [[NOTE:bava-batra-9b-secret-charity]], and the ethic traces back further still into Second Temple-period wisdom literature such as the book of Tobit [[NOTE:anderson-tobit-charity]]. Jesus's instruction that the left hand not know what the right hand is doing (6:3) assumes this already-established Jewish value rather than introducing a new one."
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
$sortKey = $maxCh6SortKey
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
Seed-Entity "Kaddish (Jewish Prayer)" "kaddish-jewish-prayer" "vocabulary" "Aramaic prayer sanctifying God's name and petitioning for God's kingdom that concluded the first-century synagogue service; a recognized liturgical parallel to the Lord's Prayer's opening petitions."
Seed-Entity "Amidah / Eighteen Benedictions" "amidah-eighteen-benedictions" "vocabulary" "Second Temple and rabbinic Judaism's standing prayer of fixed daily petitions (sustenance, forgiveness, deliverance from evil), fixed in numbered order at Yavneh after 70 CE though built from older constituent petitions."
Seed-Entity "Matan Ba-Seter (Anonymous Charity)" "matan-ba-seter-anonymous-charity" "vocabulary" "Hebrew term for 'giving in secret'; the Jewish ethical value of concealed charitable giving, attested from the Second Temple Chamber of Secrets through the Talmud to Maimonides's ladder of charity."
Seed-Entity "Maimonides" "maimonides" "character" "Twelfth-century Jewish philosopher and legal scholar (Moses ben Maimon) whose Mishneh Torah systematizes the eight-rung ladder of charity, cited as a later formalization of the anonymous-giving ethic assumed in Matthew 6:3."

$conn.Close()
Write-Host "DONE Chapter 6 Depth Pass."
