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
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql
    foreach ($k in $params.Keys) { $cmd.Parameters.AddWithValue("@$k", $params[$k]) | Out-Null }
    $cmd.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; return $cmd.ExecuteScalar() }
function New-BeatRow([string]$text) {
    $script:MaxNumber = $script:MaxNumber + 1
    $id = [guid]::NewGuid(); $hash = Sha256Hex $text
    $sql = "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())"
    Exec-NonQuery $sql @{ Id = $id; Text = $text; Hash = $hash; Number = $script:MaxNumber }
    return $id
}
function Add-BeatNode([guid]$nodeId, [guid]$beatId, [double]$sortKey) {
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = $nodeId; BeatId = $beatId; SortKey = $sortKey }
}
function Seed-Entity([string]$name, [string]$slug, [string]$type, [string]$desc) {
    $exists = Exec-Scalar "SELECT COUNT(*) FROM Entities WHERE UniverseId='0197E9C9-0003-7000-8000-000000000003' AND Slug='$slug'"
    if ($exists -gt 0) { Write-Host "  entity exists, skip: $name"; return }
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Entities (Id, EntityType, Name, Slug, Status, Description, CreatedAt, ModifiedAt, IsActive, UniverseId) VALUES (@Id, @Type, @Name, @Slug, 'canon', @Desc, SYSUTCDATETIME(), SYSUTCDATETIME(), 1, '0197E9C9-0003-7000-8000-000000000003')" @{ Id = $id; Type = $type; Name = $name; Slug = $slug; Desc = $desc }
    Write-Host "  seeded entity: $name"
}
function Append-ToExistingBeat([guid]$beatId, [string]$extraParagraph) {
    $cmd = $conn.CreateCommand(); $cmd.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"; $cmd.Parameters.AddWithValue("@Id", $beatId) | Out-Null
    $current = $cmd.ExecuteScalar()
    $updated = "$current`n`n$extraParagraph"
    $hash = Sha256Hex $updated
    Exec-NonQuery "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ Text = $updated; Hash = $hash; Id = $beatId }
}
function Find-GlossaryBeatId([string]$headingPrefix) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 b.Id FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530' AND b.Text LIKE @pat"
    $cmd.Parameters.AddWithValue("@pat", "$headingPrefix%") | Out-Null
    return $cmd.ExecuteScalar()
}

# ---- Live state ----
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")
$NotesNodeId = [guid]"019FA96B-18E2-7BB4-BAEB-11ACA24934F4"
$GlossaryNodeId = [guid]"019FA96B-29F5-7BB9-99D0-0F787960E530"
$Ch6NodeId = [guid]"019FA969-D0A9-713F-9478-4AA57F3D866C"
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA96B-29F5-7BB9-99D0-0F787960E530'")
$maxNoteNumber = [int](Exec-Scalar "SELECT MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'")
Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

$em = [char]8212

# ---- Notes ----
$notes = [ordered]@{
'deut-gleaning-law' = @{ title='The gleaning law behind the grainfield scene'; body="Deuteronomy 23:24-25 (NRSV); Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible vol. 28 (Garden City, NY: Doubleday, 1981), commentary ad loc. Luke 6:1-5. Fitzmyer identifies Deuteronomy's traveler's-right-to-hand-glean provision as the Torah backdrop that makes the disciples' underlying act lawful on any day; the entire Pharisaic objection in the pericope concerns the day, not the act itself." }
'mishnah-39-categories-date' = @{ title='The 39 forbidden labors: a later codification'; body="Mishnah Shabbat 7:2 (Sefaria Library digital edition, sefaria.org/Mishnah_Shabbat.7.2, consulted July 2026); redaction attributed to Rabbi Judah ha-Nasi, traditionally dated c. 200 CE. The complete, enumerated list of thirty-nine forbidden Sabbath labor categories $em including reaping and winnowing, directly relevant to the grain-plucking scene $em reaches its classic, closed form only in the Mishnah's redaction roughly 170 years after Luke 6's setting; an earlier partial version is hinted at, but a fully codified system operative in Galilee c. 30 CE is not directly attested." }
'pharisaic-sabbath-anachronism-risk' = @{ title='Reading the Mishnah backward onto the Pharisees'; body="E. P. Sanders, Jewish Law from Jesus to the Mishnah: Five Studies (Minneapolis: Fortress Press, 1990); Jacob Neusner, The Rabbinic Traditions about the Pharisees before 70, 3 vols. (Leiden: Brill, 1971). Both scholars caution against reading the Mishnah's finished Sabbath-labor taxonomy backward onto the historical Pharisees; Neusner's tally of pre-70 Pharisaic material found the large majority concerned with food purity rather than an elaborate labor grid, suggesting the Gospels' Pharisees are reacting to an emerging, less formalized set of Sabbath norms." }
'pikuach-nefesh-precedent' = @{ title='Life-or-death Sabbath exceptions predate medical ones'; body="1 Maccabees 2:39-41 (NRSV Apocrypha); Babylonian Talmud, Yoma 84a-85a (Sefaria.org digital edition, consulted July 2026). The principle that danger to life can override Sabbath restriction is documented as early as the Maccabean Revolt, when Mattathias's fighters decided to defend themselves in combat on the Sabbath rather than be killed $em but that precedent concerns literal battlefield survival; the explicit Talmudic exception covering medical treatment is a later rabbinic elaboration, meaning a non-life-threatening condition like a withered hand sat in a genuinely unresolved gray zone under any standard documented for the first-century period." }
'galilee-synagogue-archaeology' = @{ title='Pre-70 CE synagogues confirmed at Gamla and Magdala'; body="Zvi Ma'oz, 'The Synagogue of Gamla and the Typology of Second-Temple Synagogues,' in Ancient Synagogues Revealed, ed. Lee I. Levine (Jerusalem: Israel Exploration Society, 1981), on Shmarya Guttman's 1976-1978 excavation; Israel Antiquities Authority excavation of the Magdala synagogue by Dina Avshalom-Gorni and Arfan Najar beginning 2009, with a coin dated to 29 CE recovered in its debris. Both structures are physically dated to the first century CE and both were destroyed by Rome around 67 CE, independently confirming purpose-built synagogue assembly halls as a real, functioning first-century Galilean/Golan institution." }
'apostle-list-thaddaeus-judas-james' = @{ title='Thaddaeus or Judas son of James?'; body="Synoptic comparison of Luke 6:14-16, Mark 3:16-19, Matthew 10:2-4, and Acts 1:13 (NRSV); text-critical note on the Lebbaeus/Thaddaeus manuscript variant in Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), ad loc. Matthew 10:3. Matthew and Mark's twelfth apostle Thaddaeus never appears in either of Luke's two lists, which instead give Judas son of James in the same position; the two names never co-occur in a single source, so their identification as the same person is a scholarly inference from matching list-position, not a statement any one text makes." }
'simon-zealot-title-anachronism' = @{ title="'Zealot' as an organized party did not yet exist"; body="Flavius Josephus, Jewish War 4.160-161 (Loeb Classical Library, trans. H. St. J. Thackeray, Cambridge, MA: Harvard University Press); Martin Hengel, Die Zeloten (1961), trans. as The Zealots: Investigations into the Jewish Freedom Movement in the Period from Herod I until 70 A.D. (Edinburgh: T&T Clark, 1989). Josephus does not use 'Zealots' as the proper name of an organized faction until his narrative reaches 66-68 CE; Hengel's study argues the coalescing of scattered anti-Roman religious zeal into a named political-military party is a phenomenon of the 60s CE, roughly three and a half decades after the events of Luke 6." }
'simon-cananaean-etymology' = @{ title="Mark's 'Cananaean' clarifies, not confuses"; body="International Standard Bible Encyclopedia, rev. ed., s.v. 'Simon the Cananaean' (Grand Rapids: Eerdmans, 1988). Mark 3:18's 'Cananaean' transliterates Aramaic qanan (zealous), not a reference to Canaan or Cana; Luke's own rendering of the same epithet as Greek zelotes in 6:15 confirms he read it as a description of zeal, though this doesn't by itself settle whether it also implied formal party membership." }
'judas-iscariot-etymology-debate' = @{ title="Where does 'Iscariot' come from?"; body="Scholarly survey of the Ish-Kerioth (man of Kerioth, a Judean town) versus sicarius (Latin, dagger-man) derivations of Iscariot, as summarized in EBSCO Research Starters, 'Judas Iscariot' (2024 revision), cross-checked against the chronological objection that the Sicarii as an organized group are attested only from the 40s-50s CE. The majority linguistic position favors the geographic 'man of Kerioth' reading on chronological and Semitic-linguistic grounds, though no derivation has fully satisfied specialists." }
'q-hypothesis-sermon-divergence' = @{ title="'Poor' versus 'poor in spirit': Luke and Matthew diverge"; body="B. H. Streeter, The Four Gospels: A Study of Origins (London: Macmillan, 1924), foundational statement of the two-source (Mark + Q) model; Joseph A. Fitzmyer, The Gospel According to Luke I-IX, Anchor Bible 28 (Garden City, NY: Doubleday, 1981), commentary ad loc. Luke 6:20-26. Under the two-source model, Luke's unqualified 'Blessed are you who are poor' and Matthew's 'poor in spirit' (5:3) represent independent adaptations of a shared underlying sayings-tradition; Fitzmyer characterizes Luke's version as the more socially literal and Matthew's as spiritualized. The rival Farrer hypothesis (Luke used Matthew directly, no Q needed) is a live minority position among specialists, not a fringe one." }
'hillel-golden-rule-parallel' = @{ title="Hillel's negative Golden Rule"; body="Babylonian Talmud, Shabbat 31a (Sefaria.org digital edition, consulted July 2026); Tobit 4:15 (NRSV Apocrypha). The Talmud attributes to Hillel the Elder the negative-reciprocity summary 'that which is hateful to you, do not do to your fellow,' though the account is preserved only in a document redacted centuries after Hillel; the same negative formula already appears in the 2nd-century-BCE Book of Tobit, establishing it as a genuine pre-Christian Jewish ethical commonplace rather than a rabbinic response formed in competition with Christianity." }
'confucius-isocrates-golden-rule' = @{ title='Independent parallels in Confucius and Isocrates'; body="Confucius, Analects 15.24 (also cited as 12.2 in some editions), consulted via the D. C. Lau translation (London: Penguin Classics, 1979); Isocrates, To Nicocles 61, in Isocrates, vol. 1, trans. George Norlin, Loeb Classical Library (Cambridge, MA: Harvard University Press, 1928). Confucius's 'what you do not want done to yourself, do not do to others' and Isocrates's 'do not do to others what angers you when done to you by others' both predate Luke 6 by centuries and articulate a near-identical negative reciprocity ethic independently of any Jewish or Christian source." }
'tekton-carpenter-trade' = @{ title="What tekton actually meant"; body="Mark 6:3 and Matthew 13:55 (the only two New Testament occurrences of tekton applied to Jesus/Joseph); John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, vol. 1 (New York: Doubleday, 1991), discussion of Jesus's occupation. Meier argues tekton denotes a general craftsman/builder in wood or stone rather than narrowly a furniture-carpenter, and identifies Sepphoris $em rebuilt as Herod Antipas's Galilean capital in a major Roman-style construction boom roughly spanning Jesus's boyhood and early adulthood, located about four miles from Nazareth $em as the most plausible real-world source of steady building-trade work for a Nazareth craftsman's household." }
}

# ---- Chapter beats ----
$beat1 = @"
Luke stages the first controversy in a grainfield, and the striking thing $em easy to miss on a fast read $em is that nobody disputes whether the disciples were allowed to eat the grain. Deuteronomy had already settled that centuries earlier: a traveler passing through someone else's field could pluck heads of grain by hand and eat on the spot, so long as he didn't bring a sickle and turn the gesture into harvesting (Deuteronomy 23:25) [[NOTE:deut-gleaning-law]]. The entire fight in (6:1-5) is about the calendar, not the grain.

That narrows the real question to: what, specifically, did rubbing grain heads between your palms on a Sabbath violate, according to the Pharisees confronting Jesus? Popular retellings often borrow their precision from a document that didn't exist yet. The full, closed list of thirty-nine forbidden labor categories reaches its classic form in Mishnah Shabbat 7:2, redacted around 200 CE $em roughly a hundred and seventy years after this pericope's setting [[NOTE:mishnah-39-categories-date]]. E. P. Sanders and Jacob Neusner, pulling from different directions, both conclude that reading the Mishnah's polished labor taxonomy backward onto the historical Pharisees risks anachronism; the enforcement mechanism was almost certainly a looser, evolving customary practice, not the finished code a reader might assume is already sitting in a rabbi's back pocket in a Galilean field [[NOTE:pharisaic-sabbath-anachronism-risk]]. None of this touches the theological escalation Luke closes the scene with $em "the Son of Man is lord of the Sabbath" (6:5) is a claim about authority over the institution, not a claim about facts on the ground.
"@

$beat2 = @"
The setting itself $em "he entered the synagogue and taught" (6:6) $em is worth pausing on before the conflict, because it's one of the more solidly attested background details in this chapter. Purpose-built synagogue assembly halls, not merely informal house-gatherings, are now physically confirmed for first-century Galilee independent of any Gospel text: the Gamla synagogue in the Golan and the Magdala synagogue on the Sea of Galilee's shore both predate the Jewish Revolt and both show stepped stone benches and a central assembly space [[NOTE:galilee-synagogue-archaeology]].

The controversy itself turns on a genuinely harder edge case than the grainfield scene. A documented principle that danger to life can override Sabbath law is old $em it predates the Mishnah by roughly two centuries, tracing to the Maccabean Revolt, when Mattathias's fighters formally decided to defend themselves in combat on the Sabbath rather than be slaughtered as earlier resisters had been (1 Maccabees 2:39-41). But that decision was about literal battlefield survival. The later Talmudic exception explicitly covering medicine is a rabbinic elaboration from a still-later period. A withered hand is uncomfortable and disabling, but it isn't life-threatening, which means it sat in a genuinely unresolved gray zone under any standard actually documented for the period [[NOTE:pikuach-nefesh-precedent]] $em precisely why Luke frames it as a live confrontation ("they watched him closely," 6:7) rather than an obvious call either way.
"@

$beat3 = @"
Luke's roster is where a close synoptic comparison pays off, because the four New Testament lists of the Twelve $em Luke 6:14-16, Mark 3:16-19, Matthew 10:2-4, and Acts 1:13 $em don't match cleanly. Matthew and Mark both give a twelfth-position name of "Thaddaeus"; that name never appears anywhere in Luke, who instead gives "Judas son of James" in the same list-position. The now-standard harmonization $em that Thaddaeus and Judas son of James are the same person $em is an inference built from matching list positions across documents, not something any single source states outright [[NOTE:apostle-list-thaddaeus-judas-james]].

The best "wait, actually" in this pericope belongs to Simon "called the Zealot" (6:15). Read straight, it sounds like a job title. But Flavius Josephus, our primary source for the actual Zealot movement, doesn't use "Zealots" as the proper name of an organized faction until his narrative reaches 66-68 CE. Martin Hengel's classic study argues that scattered anti-Roman zealotry only coalesced into a named political-military party in the 60s CE, some thirty-five years after the events Luke 6 narrates $em which means "Simon the Zealot," if it's meant as party membership, is very likely read backward from a movement that hadn't happened yet [[NOTE:simon-zealot-title-anachronism]]. Mark's version of the same epithet, "Simon the Cananaean," actually clarifies rather than confuses this: it transliterates the Aramaic for "zealous" and has nothing to do with Canaan despite the surface resemblance [[NOTE:simon-cananaean-etymology]].

One more name carries its own etymological dispute: "Judas Iscariot" (6:16). The majority scholarly position derives "Iscariot" from Hebrew Ish-Kerioth, "man of Kerioth," naming a Judean town. A minority position proposes a derivation from the Latin sicarius, "dagger-man" $em tying Judas to the Sicarii, a documented cadre of Jewish assassins. The problem is chronological: the Sicarii as an organized group are attested only from the 40s-50s CE, after Judas's death, which leaves most historians favoring the geographic reading [[NOTE:judas-iscariot-etymology-debate]].
"@

$beat4 = @"
Set this sermon next to Matthew's Sermon on the Mount and the differences are immediate and real, not cosmetic. Luke's opening line is "Blessed are you who are poor" (6:20) $em flatly economic, no qualifier. Matthew's parallel reads "Blessed are the poor in spirit" (5:3) $em a phrase that shifts the blessing from an economic condition to an inward disposition. Under the widely-held two-source model of Synoptic origins, both evangelists are independently adapting a shared underlying sayings-tradition to different theological and pastoral emphases; Fitzmyer characterizes Luke's version as the more socially literal of the two and Matthew's as the spiritualized reading [[NOTE:q-hypothesis-sermon-divergence]]. Luke's added "woes" (6:24-26), with no direct Matthean parallel, sharpen the same economic focus further.
"@

$beat5 = @"
"Do to others as you would have them do to you" (6:31) is presented here as teaching, and it is $em but it's worth being honest that the reciprocity ethic itself long predates this sermon and shows up independently across traditions with no plausible line of direct influence. Hillel the Elder, a leading Pharisaic sage active in the generation just before Jesus, is remembered in the Babylonian Talmud responding to a would-be convert who demanded the whole Torah taught "while standing on one foot": "That which is hateful to you, do not do to your fellow. That is the whole Torah; the rest is commentary." The negative-reciprocity formula behind that anecdote is demonstrably older than Hillel himself $em it already appears in the deuterocanonical Book of Tobit, "Do not do to anyone what you yourself would hate" (4:15), a text usually dated to the 2nd century BCE [[NOTE:hillel-golden-rule-parallel]]. The same principle turns up, independently, well outside the Jewish tradition entirely: Confucius states it in the negative form, and the Athenian rhetorician Isocrates gives an almost structurally identical negative formulation [[NOTE:confucius-isocrates-golden-rule]]. None of this is a claim that Luke's Jesus derived the saying from Confucius or Isocrates $em there's no plausible transmission line $em it's a genuine, independent convergence across unconnected ethical traditions on the same basic moral architecture.
"@

$beat6 = @"
Most of this closing unit is parabolic and ethical rather than factually checkable $em the blind leading the blind, good and bad fruit from good and bad trees, wise and foolish builders $em and its moral content isn't something evidence can weigh either way. One image inside it, though, has a real, verifiable material-culture backdrop. "Why do you see the speck in your neighbor's eye but do not notice the log in your own?" (6:41-42) is often softened in translation into something splinter-sized; the underlying Greek word, dokos, actually denotes a structural beam or roof-joist $em a load-bearing timber, not a sliver. That's a deliberately absurd, physically oversized image, and it's also a piece of construction-trade vocabulary that lines up with the only two New Testament descriptions of Jesus's and Joseph's actual occupation: tekton, applied to Joseph in Matthew 13:55 and to Jesus in Mark 6:3. John P. Meier argues tekton denotes a general builder or craftsman working wood, stone, or both $em and that Nazareth's proximity to Sepphoris, which Herod Antipas was rebuilding in an extravagant Roman style as his first Galilean capital, is the most plausible real-world source of steady building-trade work for a Nazareth craftsman's household [[NOTE:tekton-carpenter-trade]].
"@

$beats = @($beat1, $beat2, $beat3, $beat4, $beat5, $beat6)

# ---- New glossary entries (unique to ch6) ----
$glossary = [ordered]@{
'ANDREW' = "Apostle, brother of Simon Peter, previously introduced among the called fishermen (Luke 5); named second in Luke's roster of the Twelve (6:14). Beyond his inclusion in all four New Testament apostle lists, there is no independent, extra-biblical corroboration of his individual activity in this period; later Christian tradition assigns him missionary travel and martyrdom, but those accounts surface only centuries later and read as legendary accretion around a bare name."
'PHILIP (APOSTLE)' = "One of the Twelve, named in Luke's roster (6:14) and in all four New Testament lists, first appearing here with no prior introduction in Luke. There is no independent historical record of his activity beyond the Gospel and Acts material; the later, non-canonical Acts of Philip (probably 4th-century) supplies extensive legendary detail about his missionary career that cannot be independently verified and postdates him by centuries."
'BARTHOLOMEW' = "One of the Twelve, named in Luke's roster (6:14). Some scholars identify him with 'Nathanael' from John's Gospel, though the identification rests on inference rather than any text stating it directly. No independent, contemporary source corroborates him individually."
'THOMAS (APOSTLE)' = "One of the Twelve, named in Luke's roster (6:15). No independent first-century source corroborates his individual activity; his later association with the apocryphal Gospel of Thomas and the Acts of Thomas (both non-canonical, dated well into the 2nd-3rd centuries) is legendary accretion, not attested history."
'JAMES (SON OF ALPHAEUS)' = "One of the Twelve, named in Luke's roster (6:15), distinguished from James son of Zebedee by patronymic. Sometimes conflated in later tradition with 'James the Just,' leader of the Jerusalem church mentioned by Josephus and Paul, though this identification is disputed and not made by Luke's text itself."
'SIMON CALLED THE ZEALOT' = "One of the Twelve, named in Luke's roster (6:15) with the epithet ho zelotes; called 'the Cananaean' in Mark's parallel list. The title most likely describes personal religious zeal rather than membership in the organized Zealot political-military faction, which did not coalesce under that name until the 60s CE, decades after Jesus's ministry [[NOTE:simon-zealot-title-anachronism]] [[NOTE:simon-cananaean-etymology]]."
'JUDAS (SON OF JAMES) / THADDAEUS' = "Named 'Judas son of James' in Luke's two apostle lists (6:16; Acts 1:13); the parallel figure in Matthew and Mark's lists is named 'Thaddaeus' instead. The two names never appear together in a single source, making their conventional identification as one person an inference from list-position rather than a stated fact [[NOTE:apostle-list-thaddaeus-judas-james]]."
'JUDAS ISCARIOT' = "The twelfth apostle named in Luke's roster (6:16), identified here $em well before his later act $em as 'who became a traitor.' The epithet 'Iscariot' is most plausibly derived from Hebrew Ish-Kerioth ('man of Kerioth,' a Judean town), against a minority theory linking it to the Latin sicarius, a theory chronologically difficult since the Sicarii as an organized group are attested only from the 40s-50s CE, after Judas's death [[NOTE:judas-iscariot-etymology-debate]]."
'MISHNAH' = "The foundational codification of Jewish oral law, redacted under Rabbi Judah ha-Nasi around 200 CE, roughly 170 years after the events of this chapter. Its Shabbat tractate (7:2) supplies the classic list of 39 categories of Sabbath-forbidden labor frequently $em and somewhat anachronistically $em read back into first-century Gospel Sabbath controversies [[NOTE:mishnah-39-categories-date]]."
'HILLEL THE ELDER' = "A leading Pharisaic sage active in the generation immediately preceding Jesus, remembered in later rabbinic tradition for summarizing the Torah in negative-reciprocity terms strikingly close to Luke's Golden Rule (6:31), though the anecdote survives only in the Babylonian Talmud, redacted centuries after his lifetime [[NOTE:hillel-golden-rule-parallel]]."
'CONFUCIUS' = "Chinese philosopher (traditionally 6th-5th century BCE), whose Analects articulate an independent negative-reciprocity ethic paralleling Luke's Golden Rule with no plausible line of direct transmission between the two traditions [[NOTE:confucius-isocrates-golden-rule]]."
'ISOCRATES' = "Athenian rhetorician and educator (436-338 BCE), whose advice to King Nicocles articulates a near-identical negative-reciprocity formula to Confucius's and to Luke's Golden Rule, independently of both [[NOTE:confucius-isocrates-golden-rule]]."
'BOOK OF TOBIT' = "A Jewish deuterocanonical/apocryphal narrative text, generally dated to the 2nd century BCE, whose ethical instruction 'Do not do to anyone what you yourself would hate' (4:15) is the earliest clearly datable written parallel to both Hillel's later-attributed saying and Luke's Golden Rule [[NOTE:hillel-golden-rule-parallel]]."
'GAMLA' = "A Jewish town on a steep ridge in the Golan Heights, site of one of the earliest archaeologically confirmed purpose-built synagogues (1st century BCE-1st century CE), destroyed by Rome in 67 CE during the Jewish Revolt [[NOTE:galilee-synagogue-archaeology]]."
'MAGDALA' = "A Galilean town on the western shore of the Sea of Galilee, site of a first-century synagogue excavated from 2009 onward, dated by a recovered coin to no later than 29 CE and destroyed around 67 CE; associated by name with Mary Magdalene [[NOTE:galilee-synagogue-archaeology]]."
'SEPPHORIS' = "A city roughly four miles from Nazareth, rebuilt by Herod Antipas in an extravagant Roman architectural style as his first Galilean capital, driving a major construction boom spanning roughly the first three decades of the first century CE $em plausibly the economic backdrop to Nazareth's craftsman households, including Joseph's and Jesus's own building trade [[NOTE:tekton-carpenter-trade]]."
}

# ---- Insert Notes ----
$slugToNumber = @{}
foreach ($slug in $notes.Keys) {
    $n = $notes[$slug]
    $noteNum = $maxNoteNumber + 1
    $maxNoteNumber = $noteNum
    $text = "$noteNum $em $($n.title)`n`n$($n.body)"
    $id = New-BeatRow $text
    $maxNoteSortKey += 50
    Add-BeatNode $NotesNodeId $id $maxNoteSortKey
    $slugToNumber[$slug] = $noteNum
}
Write-Host "Inserted $($notes.Count) notes, ending at [$maxNoteNumber]"

# ---- Insert chapter beats ----
$sortKey = 0.0
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) { $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch6NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats"

# ---- Insert new glossary entries ----
foreach ($heading in $glossary.Keys) {
    $body = $glossary[$heading]
    foreach ($slug in $slugToNumber.Keys) { $body = $body.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
    $text = "$heading`n`n$body"
    $id = New-BeatRow $text
    $maxGlossarySortKey += 50
    Add-BeatNode $GlossaryNodeId $id $maxGlossarySortKey
}
Write-Host "Inserted $($glossary.Count) glossary entries"

# ---- Append new claims to existing glossary beats ----
function Try-Append([string]$heading, [string]$extra) {
    $id = Find-GlossaryBeatId $heading
    if ($id) {
        foreach ($slug in $slugToNumber.Keys) { $extra = $extra.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]") }
        Append-ToExistingBeat $id $extra
        Write-Host "Appended to $heading"
    } else { Write-Host "NOT FOUND: $heading" }
}

Try-Append "THE PHARISEES" "This chapter adds: Jacob Neusner's count of pre-70 traditions attributed to the Pharisees found the large majority concerned with food-purity practice rather than an elaborate Sabbath-labor code, and the fully systematized 39-category Sabbath framework often read into scenes like Luke 6:1-5 wasn't compiled until the Mishnah, around 200 CE [[NOTE:mishnah-39-categories-date]] [[NOTE:pharisaic-sabbath-anachronism-risk]]."
Try-Append "JAMES (SON OF ZEBEDEE)" "This chapter adds: named in Luke's roster of the Twelve (6:14) as part of the inner circle of three (with Peter and John); his later death by execution under Herod Agrippa I is independently datable via Acts 12:2."
Try-Append "JOHN (SON OF ZEBEDEE)" "This chapter adds: named in Luke's roster of the Twelve (6:14) as part of the same inner three; his later association with the Fourth Gospel and Johannine letters is a separate and much-debated authorship question."
Try-Append "LEVI (TAX COLLECTOR)" "This chapter adds: named 'Matthew' in Luke's roster of the Twelve (6:15), a name divergence paralleling the Thaddaeus/Judas-son-of-James pattern seen elsewhere in the apostle lists [[NOTE:apostle-list-thaddaeus-judas-james]]."
Try-Append "NAZARETH" "This chapter adds: its economic base was plausibly linked to unskilled and craft labor for Herod Antipas's Sepphoris building boom roughly four miles away [[NOTE:tekton-carpenter-trade]]."
Try-Append "JOSEPH (HUSBAND OF MARY)" "This chapter adds: his trade, tekton, most likely meant general builder/craftsman in wood or stone, not narrowly 'carpenter,' plausibly tied to the Sepphoris building economy [[NOTE:tekton-carpenter-trade]]."
Try-Append "HEROD ANTIPAS" "This chapter adds: made Sepphoris his first Galilean capital and drove a major Roman-style construction boom across roughly Jesus's boyhood and early adulthood [[NOTE:tekton-carpenter-trade]]."
Try-Append "FLAVIUS JOSEPHUS" "This chapter adds: his Jewish War (4.160-161) is the earliest source to use 'Zealots' as an organized faction's proper name, and he doesn't apply the term until his narrative reaches 66-68 CE, a generation-plus after Luke 6's setting [[NOTE:simon-zealot-title-anachronism]]."
Try-Append "GALILEE" "This chapter adds: archaeologically confirmed first-century synagogues at Gamla and Magdala independently corroborate synagogue assembly as a real, contemporary Galilean/Golan institution rather than a later literary invention [[NOTE:galilee-synagogue-archaeology]]."

$conn.Close()
Write-Host "DONE Chapter 6."
