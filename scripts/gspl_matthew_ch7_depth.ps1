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
$Ch7NodeId = [guid]"019FA066-1F9A-7E33-8627-91085971065D"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh7SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA066-1F9A-7E33-8627-91085971065D' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh7SortKey=$maxCh7SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'hillel-golden-rule-shabbat-31a' = @{ title="Hillel's one-foot answer"; body="The Babylonian Talmud, Shabbat 31a (Soncino Talmud translation, ed. Isidore Epstein, London: Soncino Press, 1938; also available via the Sefaria digital library). The Talmud — itself compiled and redacted centuries after Hillel's own lifetime — records a gentile asking to be converted on condition of being taught the whole Torah while standing on one foot; the sterner sage Shammai turns him away, but Hillel accepts him, answering, `"What is hateful to you, do not do to your neighbor; that is the whole Torah, and the rest is commentary; go and learn it.`" Hillel the Elder is traditionally dated to roughly 110 BCE-10 CE, placing the attributed teaching itself a generation or more before Jesus's own ministry, even though the text recording the story postdates both by centuries." }
'tobit-4-15-golden-rule' = @{ title="An older, negative form: Tobit"; body="Tobit 4:15 (deuterocanonical/apocryphal), in standard critical editions such as the New Revised Standard Version with Apocrypha. The book of Tobit, generally dated by critical scholarship to the second century BCE, has the dying Tobit instruct his son: `"what you hate, do not do to anyone.`" This places a negative-form reciprocity principle inside Jewish scripture itself roughly two centuries before Hillel's own teaching activity and well over a century before Jesus, making it the oldest securely-dated Jewish attestation of the principle discussed in this chapter." }
'confucius-analects-golden-rule' = @{ title="A separate tradition, five centuries earlier"; body="Confucius, Analects 15.23 (numbered 15.24 in some editions), trans. James Legge, The Chinese Classics, vol. 1 (Oxford: Clarendon Press, 1893). Asked by his student Zigong whether there is one word to serve as a guide for one's whole life, Confucius replies, `"Is not reciprocity such a word? What you do not want done to yourself, do not do to others.`" Confucius is traditionally dated 551-479 BCE, roughly five centuries before Jesus's ministry and in an entirely separate cultural and religious tradition with no plausible line of transmission to first-century Galilee, making this a genuinely independent parallel rather than a borrowed one." }
'isocrates-nicocles-golden-rule' = @{ title="The Greek rhetorical tradition's version"; body="Isocrates, Nicocles or the Cyprians (Oration 3), section 61, in Isocrates, vol. 1, trans. George Norlin, Loeb Classical Library (Cambridge, MA: Harvard University Press, 1928). Isocrates, a leading Athenian rhetorician and teacher active in the fourth century BCE, has the speech's speaker instruct, `"Do not do to others that which angers you when they do it to you`" — a negative-form reciprocity principle from within the classical Greek moral tradition, independent of both the Jewish and Confucian lines of transmission cited elsewhere in this chapter." }
'betz-sermon-mount-commentary-golden-rule' = @{ title="Reading the convergence as convergence, not borrowing"; body="Hans Dieter Betz, The Sermon on the Mount: A Commentary on the Sermon on the Mount, Including the Sermon on the Plain (Matthew 5:3-7:27 and Luke 6:20-49), Hermeneia series (Minneapolis: Fortress Press, 1995), commentary ad loc. Matthew 7:12. Betz's standard critical commentary situates Matthew's Golden Rule within the broader history-of-religions context of independently attested reciprocity ethics across Jewish, Greek, and other ancient traditions, treating the parallels as evidence of a widely and separately reached moral insight rather than of literary dependence in either direction." }
'theophrastus-enquiry-plants-grafting' = @{ title="Grafting and true fruit: Theophrastus"; body="Theophrastus, Enquiry into Plants (Historia Plantarum), Book 2, trans. Arthur Hort, Loeb Classical Library (London: William Heinemann; New York: G. P. Putnam's Sons, 1916). Writing in Athens in the late fourth century BCE, Theophrastus describes contemporary Mediterranean grafting practice in detail, including grafting cultivated olive and fig scions onto different rootstock, and states that the graft relies on its host stock for nourishment much as a cutting relies on soil, without the stock altering what fruit the scion itself produces." }
'columella-de-re-rustica-grafting' = @{ title="The same practice, a Roman generation before the Gospels"; body="Lucius Junius Moderatus Columella, De Re Rustica (On Agriculture), Books 3-5, in Columella, On Agriculture, Loeb Classical Library, 3 vols., trans. Harrison Boyd Ash, E. S. Forster, and Edward H. Heffner (Cambridge, MA: Harvard University Press, 1941-1955). Writing under the emperor Claudius in the middle of the first century CE — within a generation of the events narrated in the Gospels — Columella devotes extended sections of his twelve-book agricultural manual to grafting, pruning, and cultivating fig, olive, and vine varieties across the Roman Mediterranean, confirming that the horticultural logic behind `"you will know them by their fruits`" (Matthew 7:16-20) reflects live, contemporary agricultural technique rather than an antiquated or foreign practice." }
'davies-allison-icc-matthew-scribal-authority' = @{ title="The default mode: citation chains"; body="W. D. Davies and Dale C. Allison Jr., Matthew 1-7, International Critical Commentary, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 7:28-29. Davies and Allison's standard critical commentary treats the closing note about Jesus teaching `"as one having authority, and not as their scribes`" against the background of conventional scribal practice, in which a teacher's authority typically rested on citing a chain of earlier named teachers rather than speaking in an unattributed personal voice." }
'jeremias-amen-formula-authority' = @{ title="A speech pattern without a real precedent"; body="Joachim Jeremias, New Testament Theology, Volume One: The Proclamation of Jesus, trans. John Bowden (London: SCM Press; New York: Charles Scribner's Sons, 1971), discussion of Jesus's characteristic modes of speech. Jeremias's influential survey of Jesus's distinctive diction singles out the introductory `"Amen, I say to you`" formula — and, by extension, the flatly personal `"but I say to you`" of the Sermon on the Mount's antitheses — as lacking a clear parallel introducing a speaker's own words elsewhere in surviving Jewish literature of the period, reading it as a genuinely distinctive feature of how the Jesus tradition remembers his teaching voice." }
'neusner-rabbinic-chain-critique' = @{ title="The citation-chain convention, examined critically on its own terms"; body="Jacob Neusner, Invitation to the Talmud: A Teaching Book, rev. ed. (San Francisco: Harper & Row, 1984), discussion of the formation and reliability of rabbinic attribution chains. Neusner's source-critical scholarship on rabbinic literature has argued that the standard `"Rabbi X said in the name of Rabbi Y`" transmission formula does not by itself guarantee an unbroken, verifiable line back to the named source, particularly given how thoroughly the 70 CE destruction of Jerusalem disrupted direct living memory between many earlier and later named teachers — a reminder that the comparison drawn in this chapter concerns a difference in rhetorical convention, not a contrast between an unverified Jesus tradition and a fully verified rabbinic one." }
'kasemann-criterion-dissimilarity-origin' = @{ title="Naming the method: Kasemann, 1953"; body="Ernst Kasemann, `"The Problem of the Historical Jesus,`" a 1953 lecture published in Essays on New Testament Themes, trans. W. J. Montague (London: SCM Press; Naperville, IL: Alec R. Allenson, 1964). Kasemann's essay is the standard point of origin credited for what became known as the criterion of dissimilarity (or discontinuity): material attributed to Jesus is judged a stronger candidate for authenticity when it resembles neither the Judaism of his own day nor the concerns of the later church that transmitted it, since neither source would have an obvious motive to supply such material independently." }
'meier-marginal-jew-dissimilarity' = @{ title="The criterion, applied"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), discussion of the criteria of authenticity, especially the criterion of discontinuity/dissimilarity. Meier's standard methodological survey treats self-authorizing, non-citational teaching authority of the kind running through the Sermon on the Mount as exactly the sort of feature the criterion of dissimilarity is built to weigh: material neither derived from contemporary Jewish teaching convention nor obviously supplied by the church's later interests." }
'sanders-historical-figure-authority' = @{ title="An itinerant teacher's motive-poor claim"; body="E. P. Sanders, The Historical Figure of Jesus (London: Allen Lane/Penguin Press, 1993), discussion of Jesus's teaching authority and self-understanding. Sanders argues that an early Christian movement dependent on appeals to apostolic succession and scriptural precedent to settle its own internal disputes had little obvious motive to invent, after the fact, a founder who taught in his own voice without anchoring his rulings to any such precedent — treating the Gospels' consistent portrait of Jesus's personally authoritative teaching style as one of the more historically plausible features of the tradition on those grounds." }
'iakovidis-mycenae-postern-gate' = @{ title="A narrow second gate, physically excavated"; body="Spyros E. Iakovidis, Late Helladic Citadels on Mainland Greece, Monumenta Graeca et Romana 4 (Leiden: E. J. Brill, 1983). Iakovidis's standard archaeological survey of Late Bronze Age Mycenaean fortifications documents the citadel of Mycenae's Postern Gate, a narrow, single-file secondary entrance on the north side of the fortification circuit, standing alongside the much larger and more heavily decorated Lion Gate — a physically excavated example of the general ancient Mediterranean pattern of pairing one broad public gate with one or more smaller, more defensible secondary gates." }
'zieminska-needles-eye-gate-myth' = @{ title="A different verse, a later and traceable legend"; body="Agnieszka Zieminska, `"The Origin of the Needle's Eye Gate Myth: Theophylact or Anselm?`" Novum Testamentum 63, no. 4 (2021): 358-361. Zieminska's close study of the manuscript tradition behind the popular claim that Jerusalem once had a small gate nicknamed `"the eye of the needle`" (attached to the separate saying at Matthew 19:24) finds no first-century evidence for any such gate and traces the legend's documentary origin only as far back as a medieval gloss associated with either the Byzantine commentator Theophylact or, on the closer reading, Anselm of Canterbury, later transmitted through Thomas Aquinas's thirteenth-century Catena Aurea — a legend, unlike the excavated postern gates cited elsewhere in this chapter, with no supporting ancient archaeological or textual basis at all." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
Matthew 7:12 states the positive form of what is now called the Golden Rule: "So whatever you wish that others would do to you, do also to them, for this is the Law and the Prophets." This is one of the more thoroughly cross-checked ethical statements in the whole Gospel, because close, independently attested relatives of the same principle circulate well outside the Jesus tradition and, in several cases, well before it — not a case of vaguely gesturing that "many cultures say something like this," but of specific, datable, citable texts.

The nearest and best-known Jewish parallel is attributed to the great Jerusalem sage Hillel the Elder, who died sometime around the start of the first century CE and so was active a generation or two before Jesus's ministry. The Babylonian Talmud preserves a story — compiled centuries after Hillel's own lifetime, though the attributed teaching itself is far older than the text that records it — in which a prospective convert asks to be taught the whole Torah while standing on one foot; the sterner sage Shammai turns him away, but Hillel accepts him, answering: "What is hateful to you, do not do to your neighbor; that is the whole Torah, and the rest is commentary; go and learn it" [[NOTE:hillel-golden-rule-shabbat-31a]]. Hillel's version is negative in form (refrain from harm) where Matthew's is positive (initiate good), a real and often-noted difference in emphasis, but the underlying reciprocity principle, and even Hillel's own framing of it as the whole Torah in miniature, sits close enough to Jesus's "this is the Law and the Prophets" that the parallel is more than coincidental phrasing.

The principle is older than Hillel within Judaism's own textual tradition. The deuterocanonical Book of Tobit, generally dated to the second century BCE and so predating both Hillel and Jesus by a century or more, already states the negative form outright: "what you hate, do not do to anyone" [[NOTE:tobit-4-15-golden-rule]].

Nor is the reciprocity principle a peculiarly Jewish one. Confucius, traditionally dated 551-479 BCE and teaching roughly five centuries before either Hillel or Jesus in a wholly separate cultural and religious tradition, is recorded giving functionally the same negative formulation when a student asked for a single word to live by: "Is not reciprocity such a word? What you do not want done to yourself, do not do to others" [[NOTE:confucius-analects-golden-rule]]. And within the Greek rhetorical tradition, the fourth-century-BCE Athenian teacher Isocrates instructed his students in near-identical terms: "Do not do to others that which angers you when they do it to you" [[NOTE:isocrates-nicocles-golden-rule]].

None of this means Matthew's Jesus is quoting Confucius or Isocrates, or even necessarily Hillel directly — there is no evidence of direct literary dependence running in any direction between these texts. The standard critical commentary on the Sermon on the Mount treats the convergence as exactly what it looks like: an ethical principle independently arrived at, in broadly similar form, across multiple unrelated ancient traditions, of which Jesus's positive-voice version in Matthew 7:12 is one late restatement among several, distinctive chiefly in its positive rather than negative phrasing [[NOTE:betz-sermon-mount-commentary-golden-rule]].
'@

$beat2 = @'
A few verses later, Matthew's Jesus turns to agricultural imagery that does not carry the same cross-cultural echo but does rest on a real, checkable point of ancient horticultural fact: "You will know them by their fruits. Are grapes gathered from thornbushes, or figs from thistles? ... every healthy tree bears good fruit, but the diseased tree bears bad fruit" (7:16-18). The ethical application — judge the prophet by his effects, not his claims — is the text's own point and is not something outside evidence can confirm or deny. But the horticultural logic underneath it was, in fact, precisely observed and written up by ancient agricultural writers working in exactly this period and region of the Mediterranean world.

The relevant background is grafting. Mediterranean farmers had understood for centuries before Jesus that inserting a scion — a cutting of the desired variety — into a different rootstock does not change what fruit the resulting tree bears: the graft still produces fruit true to the scion, regardless of what stock it is growing on. The Greek philosopher and botanist Theophrastus, writing in Athens in the fourth century BCE, describes the practice at length in his Enquiry into Plants, including grafting cultivated olive and fig varieties onto wild rootstock, and states that the grafted twig relies on its host stock for nourishment much as a cutting relies on soil, without the stock altering the scion's own fruit [[NOTE:theophrastus-enquiry-plants-grafting]]. Three centuries later, and much closer to Jesus's own lifetime, the Roman agricultural writer Columella, writing under the emperor Claudius in the middle of the first century CE, devotes substantial sections of his twelve-book De Re Rustica to the same grafting and pruning techniques for fig, olive, and vine cultivation across the Roman Mediterranean, confirming that this was current, widely practiced technical knowledge in exactly Jesus's own century, not an isolated antiquarian curiosity [[NOTE:columella-de-re-rustica-grafting]].

What that background supplies is the literal, non-metaphorical truth the metaphor leans on: in the actual orchards and vineyards of the first-century Mediterranean, appearance and rootstock origin were not reliable guides to what a tree would produce — a grafted branch on a wild rootstock does not become wild, and a wild shoot does not spontaneously bear cultivated fruit. The fruit itself, and only the fruit, told a working farmer what he actually had planted. That the Sermon on the Mount reaches for exactly this image to make a point about discernment is not a coincidence available only in hindsight; it would have been immediately, physically legible to an agrarian audience for whom checking a tree by its fruit rather than its look was routine practice, not poetry.
'@

$beat3 = @'
The chapter's structure keeps returning to a first-person voice that does not lean on any external authority: "Many will say to me on that day, Lord, Lord... And then I will declare to them, I never knew you" (7:21-23) is Jesus speaking as the one who will personally judge, not reporting a ruling handed down from Moses, a prophet, or an earlier teacher. That rhetorical stance — an itinerant Galilean teacher speaking entirely in his own voice about his own authority to judge — is worth separating from the theological content of the claim, because the stance itself is a real, checkable feature of how this material differs from its contemporary teaching environment, whatever one makes of the content.

Second Temple and early rabbinic teaching conventionally worked by citation chain: a ruling's authority rested on tracing it back through named predecessors — "Rabbi so-and-so said in the name of Rabbi so-and-so" is the standard form throughout early rabbinic literature, and the convention itself, not just isolated examples of it, is treated as the default mode of transmission in the standard critical commentary on Matthew's Gospel [[NOTE:davies-allison-icc-matthew-scribal-authority]]. It is worth flagging, before leaning too hard on that contrast, that the citation-chain convention is not always as historically airtight as it sounds even on its own rabbinic terms: Jacob Neusner's source-critical study of the Talmud has argued that the formal "X said in the name of Y" format does not by itself guarantee an unbroken, verified line back to the named source, particularly given how thoroughly the 70 CE destruction of Jerusalem disrupted living memory between many earlier and later named teachers [[NOTE:neusner-rabbinic-chain-critique]] — a reminder that this is a contrast in rhetorical convention, not a clean, verified rabbinic paper trail set against an unverified Jesus tradition.

Set against that background, the repeated formula running through the whole Sermon on the Mount — "You have heard that it was said... but I say to you" — and the emphatic "Amen, I say to you" introducing sayings elsewhere in the Gospels are conspicuous by not doing what the convention calls for: no attribution, no chain, no appeal to precedent. Joachim Jeremias's standard treatment of Jesus's characteristic speech patterns singles out exactly this "Amen" formula as lacking a clear parallel introducing a speaker's own words elsewhere in the surviving Jewish literature of the period — a personal, unmediated authorization that the surrounding teaching culture did not otherwise sanction [[NOTE:jeremias-amen-formula-authority]].

Historical-Jesus scholarship has a specific, named tool for weighing exactly this kind of feature: the criterion of dissimilarity (also called the criterion of discontinuity), formulated by Ernst Kasemann in a landmark 1953 lecture and still a standard, if contested, part of the historical-critical toolkit — material is judged a stronger candidate for authenticity precisely when it does not look like something either contemporary Judaism or the later church would be inclined to invent on its own [[NOTE:kasemann-criterion-dissimilarity-origin]]. John P. Meier's standard methodological survey applies the criterion to exactly this kind of self-authorizing teaching style [[NOTE:meier-marginal-jew-dissimilarity]], and E. P. Sanders has argued along similar lines that an early church dependent on appeals to apostolic succession and scriptural precedent to settle its own disputes had little obvious motive to invent, after the fact, a founder who dispensed with exactly that kind of appeal [[NOTE:sanders-historical-figure-authority]].

None of this proves what a first-century Galilean crowd actually felt on a specific hillside. But it does mean the chapter's own closing line — "the crowds were astounded at his teaching, for he taught them as one who had authority, and not as their scribes" (7:28-29) — is not merely a flattering aside Matthew invented to praise his subject. It names, in its own words, a real and identifiable departure from how authority conventionally worked in the teaching culture it describes.
'@

$beat4 = @'
Two verses earlier, the same sermon offers a second image resting on real, checkable ancient architecture rather than agriculture: "Enter by the narrow gate... For the gate is narrow and the way is hard that leads to life, and those who find it are few" (7:13-14). The theological point — that few find the harder way — is not something archaeology can confirm or refute. But the physical premise behind the image, that ancient walled settlements routinely had more than one kind of gate, some deliberately built narrow, is a genuine and well-documented feature of ancient Mediterranean fortification, independent of anything in the text.

Large public gates in walled ancient cities were built wide enough for wagons, livestock, and crowds, and were correspondingly the most vulnerable point in a city's defenses — exactly where an attacking force would concentrate its effort. Alongside or behind these main gates, excavated fortifications across the ancient Mediterranean regularly show smaller secondary entrances — posterns or wicket gates — built narrow enough to admit only one person at a time, easier for a small guard to control, and often used after the main gate had been shut for the night. The best-excavated and most thoroughly published example is the Late Bronze Age citadel of Mycenae in Greece, where the small Postern Gate on the north side of the fortification wall functioned alongside the famous, much larger Lion Gate — a secondary, single-file entrance distinct from the city's main ceremonial approach, as documented in the standard archaeological survey of these fortifications [[NOTE:iakovidis-mycenae-postern-gate]]. The general principle — one broad public gate plus one or more narrow subsidiary gates built for security and controlled foot traffic rather than volume — recurs across walled settlements throughout the ancient Mediterranean and Near East, of which Mycenae is simply the most completely excavated and published case.

It is worth being precise about what this note is, and is not, claiming, because Matthew's own Gospel supplies a cautionary example of exactly the wrong kind of specificity nearby, at a different verse. Later in Matthew (19:24), Jesus says it is easier for a camel to pass through the eye of a needle than for a rich man to enter the kingdom of God — and a popular, still widely repeated legend holds that "the eye of the needle" was itself the name of an actual small gate in Jerusalem's wall, through which an unloaded, kneeling camel could just barely squeeze. That specific story has no supporting evidence from antiquity at all; recent source-critical work has traced the legend only as far back as a medieval gloss, popularly attributed to the Byzantine commentator Theophylact but, on closer manuscript study, more likely associated with Anselm of Canterbury, later transmitted through Thomas Aquinas's thirteenth-century Catena Aurea — centuries after the Gospels, with no first-century attestation of any such gate [[NOTE:zieminska-needles-eye-gate-myth]]. The narrow gate of 7:13-14 and the needle's eye of 19:24 are two different sayings about two different images, and only one of them has a real, excavated architectural fact standing behind it; the other has a genuine, documented legend standing in the empty space where a fact might otherwise have gone.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'GOLDEN RULE (CROSS-CULTURAL ATTESTATION)' = "The reciprocity principle stated positively in Matthew 7:12 (`"whatever you wish that others would do to you, do also to them`") and attested, mostly in negative form, across a range of ancient ethical traditions predating or independent of the Gospels. Confirmed close parallels cited in this chapter include Hillel the Elder's negative formulation recorded in the Babylonian Talmud (Shabbat 31a) [[NOTE:hillel-golden-rule-shabbat-31a]], the deuterocanonical Book of Tobit (4:15), dated roughly two centuries before Jesus [[NOTE:tobit-4-15-golden-rule]], Confucius's Analects (15.23), five centuries earlier still and from an entirely separate tradition [[NOTE:confucius-analects-golden-rule]], and the Athenian rhetorician Isocrates's Nicocles (61) [[NOTE:isocrates-nicocles-golden-rule]]. Mainstream critical commentary treats the convergence as genuine, independent ethical convergence rather than either borrowing or coincidence [[NOTE:betz-sermon-mount-commentary-golden-rule]]."
'HILLEL THE ELDER' = "A leading Pharisaic sage active in Jerusalem in the decades before and around the turn of the era, traditionally dated to roughly 110 BCE-10 CE — a real, datable figure whose teaching activity predates Jesus's own ministry by a generation or so. He is remembered, in a story first written down centuries later in the Babylonian Talmud, for summarizing the whole Torah for a prospective convert as `"what is hateful to you, do not do to your neighbor,`" the closest and best-attested Jewish parallel to the positive Golden Rule of Matthew 7:12 [[NOTE:hillel-golden-rule-shabbat-31a]]. Cited in this chapter for comparative purposes only; nothing here claims Matthew's Jesus knew or quoted Hillel directly."
'CRITERION OF DISSIMILARITY' = "A method used in historical-Jesus scholarship for weighing the likely authenticity of a saying or action attributed to Jesus: material is judged a stronger candidate for going back to Jesus himself when it does not closely resemble either the surrounding Judaism of his own day or the concerns of the later church that transmitted the Gospels, on the reasoning that such material is less likely to have been supplied by either side. First formulated by Ernst Kasemann in a 1953 lecture later published in his Essays on New Testament Themes [[NOTE:kasemann-criterion-dissimilarity-origin]], and applied since by scholars including John P. Meier [[NOTE:meier-marginal-jew-dissimilarity]] and E. P. Sanders [[NOTE:sanders-historical-figure-authority]] to features such as Jesus's unmediated, non-citational teaching authority in the Sermon on the Mount (7:21-23, 7:28-29). Related to, but methodologically distinct from, the criterion of embarrassment, which weighs material the early church would have had reason to suppress rather than material that simply lacks a clear precedent."
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
$sortKey = $maxCh7SortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch7NodeId $id $sortKey
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
Seed-Entity "Confucius" "confucius" "character" "Sixth-to-fifth-century BCE Chinese philosopher and teacher; the Analects preserve an early, independent negative-form Golden Rule parallel to Matthew 7:12."
Seed-Entity "Isocrates" "isocrates" "character" "Fourth-century BCE Athenian rhetorician and teacher; his Nicocles preserves a Greek negative-form Golden Rule parallel independent of the Jewish and Confucian traditions."
Seed-Entity "Theophrastus" "theophrastus" "character" "Fourth-century BCE Greek philosopher and botanist, successor to Aristotle at the Lyceum; his Enquiry into Plants documents ancient Mediterranean grafting technique underlying the Matthew 7:16-20 fruit-tree metaphor."
Seed-Entity "Columella" "columella" "character" "First-century CE Roman agricultural writer (De Re Rustica); documents grafting and orchard cultivation contemporary with the Gospels."
Seed-Entity "Ernst Kasemann" "ernst-kasemann" "character" "Twentieth-century German New Testament scholar; originated the criterion of dissimilarity for historical-Jesus research in a 1953 lecture."
Seed-Entity "Spyros Iakovidis" "spyros-iakovidis" "character" "Twentieth-century Greek archaeologist; standard excavator/surveyor of Late Bronze Age Mycenaean citadel fortifications, including the Mycenae Postern Gate."
Seed-Entity "Book of Tobit" "book-of-tobit" "vocabulary" "Deuterocanonical/apocryphal Jewish text, generally dated to the second century BCE; contains an early negative-form Golden Rule parallel at 4:15."
Seed-Entity "Postern Gate (Ancient Fortification)" "postern-gate-ancient-fortification" "vocabulary" "A narrow, single-file secondary gate built alongside or within a larger city gate in ancient fortifications, used for controlled foot traffic and after the main gate's closure; documented archaeologically at sites including Mycenae."
Seed-Entity "Golden Rule (Cross-Cultural Ethical Principle)" "golden-rule-cross-cultural-ethical-principle" "vocabulary" "The reciprocity ethic stated positively in Matthew 7:12 and independently attested, in negative form, in Hillel, Tobit, Confucius, and Isocrates."
Seed-Entity "Criterion of Dissimilarity" "criterion-of-dissimilarity" "vocabulary" "Historical-Jesus research method (Ernst Kasemann, 1953) weighing material as more likely authentic when it resembles neither contemporary Judaism nor later church interests; related to but distinct from the criterion of embarrassment."

$conn.Close()
Write-Host "DONE Matthew Chapter 7 depth pass."
