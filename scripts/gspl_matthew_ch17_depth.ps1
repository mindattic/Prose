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
$Ch17NodeId = [guid]"019FA06E-4B5A-7AAB-80B3-3EF0B408119C"
$NotesNodeId = [guid]"019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$GlossaryNodeId = [guid]"019FA026-3758-7A57-AF65-EC1DB4303EF3"

$maxCh17SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA06E-4B5A-7AAB-80B3-3EF0B408119C' AND bn.IsEnabled=1")
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxCh17SortKey=$maxCh17SortKey MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'sinai-cloud-glory-moses-typology' = @{ title='The cloud, the glory, the mountain: borrowed from Sinai'; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 17:1-8. Davies and Allison read the cloud overshadowing the mountain, the voice speaking from within it, and Jesus's shining transformation as a deliberate set of echoes of Moses's own mountaintop encounters with God at Sinai — a cloud covering the mountain and glory manifesting within it (Exodus 24:15-18), and Moses's own face left shining afterward (Exodus 34:29-35) — treating the parallel as a structured compositional choice rather than an incidental resemblance." }
'elijah-horeb-theophany-law-prophets' = @{ title="Elijah's own mountain, and why the pair matters"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 17:3-4. Alongside the Moses parallel, commentators read Elijah's presence as recalling his own theophany at Horeb — another name for Sinai — where, fleeing Jezebel, he too travels to the mountain of God and encounters the divine presence there (1 Kings 19:8-13). Staging Moses and Elijah together on one mountain draws together the two figures Jewish tradition already used as shorthand for the whole of scripture, the Law and the Prophets, into a single scene." }
'law-and-prophets-idiom-matthew' = @{ title="The Law and the Prophets, as Matthew's own phrase"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 5:17. Matthew has Jesus use the paired phrase 'the Law and the Prophets' as shorthand for the whole of Israel's scripture in his own recorded teaching (Matthew 5:17; 7:12; 22:40), the same two-part division the Transfiguration scene stages visually through Moses and Elijah's joint appearance." }
'matthew-six-days-exodus-echo' = @{ title='A number Matthew almost never gives'; body="Dale C. Allison Jr., The New Moses: A Matthean Typology (Minneapolis: Fortress Press, 1993), discussion of the Transfiguration's Mosaic background. Matthew ordinarily links episodes with vague connective phrases — 'at that time,' 'in those days,' 'then' — and rarely supplies a specific elapsed-day count; landing on an exact 'after six days' here (17:1) is unusual enough within Matthew's own habits that Allison and others read it as a further, deliberate echo of Exodus 24:16, where a cloud covers Sinai for six days before Moses is called into it on the seventh — extending the Moses parallel from imagery into the narrative's own clock." }
'riesenfeld-messianic-sukkot-transfiguration' = @{ title="Messianic-age Sukkot expectations layered onto the mountain"; body="Harald Riesenfeld, Jésus Transfiguré: L'arrière-plan du récit évangélique de la Transfiguration de Notre-Seigneur, Acta Seminarii Neotestamentici Upsaliensis 16 (Copenhagen: Munksgaard, 1947). Riesenfeld's influential study argues the Transfiguration narrative draws on Feast of Tabernacles imagery and the messianic, eschatological associations that festival had accumulated by the late Second Temple period — divine cloud-dwelling, kingly enthronement, the ingathering of Israel — reading Peter's offer to build three booths as an attempt to physically stage the messianic-age version of the festival, not a random idea reached for under pressure." }
'luz-tabernacles-background-survey' = @{ title="Surveying the proposed backgrounds to Peter's offer"; body="Ulrich Luz, Matthew 8-20: A Commentary, Hermeneia series, trans. James E. Crouch (Minneapolis: Fortress Press, 2001), commentary ad loc. Matthew 17:4. Luz surveys the range of proposed backgrounds scholars have offered for Peter's proposal to build three tabernacles — including the Sukkot-messianism reading — without treating any single explanation as settled, consistent with the pattern this book has already applied to other underdetermined narrative details." }
'exodus-30-half-shekel-origin' = @{ title="Where the temple tax comes from: a census-ransom, not a fee"; body="William Horbury, `"The Temple Tax,`" in Jesus and the Politics of His Day, ed. Ernst Bammel and C. F. D. Moule (Cambridge: Cambridge University Press, 1984), 265-286. Horbury traces the annual half-shekel Temple tax this chapter's closing scene turns on back to its legal origin in Exodus 30:11-16, which ties a required contribution from every counted Israelite male aged twenty and up to a census — a 'ransom' payment rather than a simple fee — and details how, by the first century, this had become a fixed annual didrachma payment funding ongoing Temple operations." }
'diaspora-temple-tax-collection' = @{ title="Collected across the empire, not just in Judea"; body="Philo, De Specialibus Legibus 1.77-78 (Loeb Classical Library, trans. F. H. Colson, Cambridge, MA: Harvard University Press, 1937); Cicero, Pro Flacco 28.66-69 (Loeb Classical Library, trans. Louis E. Lord, Cambridge, MA: Harvard University Press, 1937). Philo describes the annual Temple contribution as gathered locally by diaspora Jewish communities and sent to Jerusalem; Cicero, defending a Roman governor of Asia accused of confiscating a shipment of Jewish gold collected for this same purpose, independently and from a hostile courtroom confirms that a diaspora-wide collection-and-transport system for the tax was already a going concern decades before Jesus's ministry." }
'theissen-gift-miracle-category' = @{ title="A different shape of miracle story: the 'gift miracle'"; body="Gerd Theissen, The Miracle Stories of the Early Christian Tradition, trans. Francis McDonagh (Edinburgh: T&T Clark, 1983), discussion of miracle-story classification. Theissen's form-critical taxonomy sets healing and exorcism accounts — built around a request, a diagnosed affliction, and a demonstrated cure — apart from what he classifies as 'gift miracles': unsolicited, unrequested provisions of a specific material need, narrated with no interest in explaining the mechanism by which the provision happens." }
'rofe-elisha-provision-legenda' = @{ title="An Old Testament ancestor for the same story shape"; body="Alexander Rofé, The Prophetical Stories: The Narratives about the Prophets in the Hebrew Bible, Their Literary Types and History (Jerusalem: Magnes Press, 1988), discussion of Elisha's provision narratives. Rofé's form-critical study of the prophetic legenda classifies Elisha's multiplying jar of oil and his feeding of a hundred men from twenty barley loaves (2 Kings 4:1-7, 4:42-44) as an Old Testament instance of the same basic gift or blessing story type Theissen later identifies in the Gospels, centuries earlier." }
'fish-coin-matthew-only-isolated' = @{ title="A story with no parallel and a different texture"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 2: Matthew VIII-XVIII (Edinburgh: T&T Clark, 1991), commentary ad loc. Matthew 17:24-27. Davies and Allison note this pericope's isolated attestation — no parallel in Mark, Luke, or John — and its distinct narrative texture compared to the surrounding healing and exorcism material: no crowd, no faith-saying, no named beneficiary beyond Peter, and a resolution built around a coin found inside a fish's mouth, which even sympathetic commentators have described as one of the more folkloric-sounding notes in the Synoptic miracle tradition." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders; existing note numbers cited directly) ----
$beat1 = @'
The two named guests atop the mountain are not a random pair, and the scene's construction rewards a closer look at what each one brings. Moses and Elijah are already established in this book as unusual cases — Old Testament figures given actual on-page narrative presence here rather than mere allusion — and the mainstream historical-critical reading treats their joint appearance as a deliberately built piece of Old Testament typology, not simply "two prophets happened to show up." The cloud that overshadows the group, the voice speaking out of it, and Jesus's own transformed appearance closely track the vocabulary of Moses's own mountaintop encounters with God: a cloud covering the mountain, glory manifesting within it, a summons into the cloud itself (Exodus 24:15-18), and Moses's own face left shining afterward (Exodus 34:29-35) [[NOTE:sinai-cloud-glory-moses-typology]]. Elijah's presence draws on a matching, if less obvious, precedent: fleeing after his confrontation with the prophets of Baal, Elijah himself travels to Horeb — another name for Sinai — and there encounters God's presence in his own mountain theophany (1 Kings 19:8-13). Putting Moses and Elijah together, staged on a mountain, front-loads the two figures Jewish tradition already used as shorthand for "the Law and the Prophets" into one scene [[NOTE:elijah-horeb-theophany-law-prophets]] — the same summary phrase Matthew elsewhere has Jesus invoke in his own recorded teaching (Matthew 5:17; 7:12) [[NOTE:law-and-prophets-idiom-matthew]]. This on-mountain appearance is a different matter from the separate forerunner question this same chapter raises a few verses later, over whether John the Baptist fulfills an expected "Elijah must come first" office [62] — one is Elijah literally present in a scene, the other is an argument about whether a later figure retroactively satisfies an expected role, and the two shouldn't be merged. Whether either Old Testament figure's appearance here is historical in any sense this book's method can test isn't answerable; what is checkable is that the scene is built, piece by piece, out of recognizable fragments of Israel's own scriptural memory.

The chapter's opening clause, "after six days" (17:1), deserves its own pause, because it breaks Matthew's own habits. Across this Gospel, Matthew typically links episodes with vague connective tissue — "at that time," "in those days," "then" — and rarely bothers to count elapsed days at all; landing on a specific number here, in a Gospel that is usually chronologically loose everywhere else, is unusual enough that commentators ask what work the number is doing rather than treating it as incidental scene-setting. One recurring answer in the scholarship again reaches for Sinai: Exodus 24:16 has the cloud cover the mountain for six days before Moses is called into it on the seventh, and reading Matthew's six days as a deliberate echo of that same interval extends the Moses parallel from imagery into the story's actual clock [[NOTE:matthew-six-days-exodus-echo]]. That is an intertextual argument, not a claim any external evidence could independently verify — but it is a real, specific, and checkable observation about how the number functions in the text, not an idle coincidence.
'@

$beat2 = @'
Peter's offer to build three "tabernacles" — skenai, the same word used for the temporary booths built each autumn during the pilgrimage festival of Sukkot — is a concrete, checkable piece of vocabulary, not vague enthusiasm dressed up as a proposal. This book has already established Sukkot's basic character as a real annual Jewish festival built around constructing and dwelling in temporary branch-and-thatch shelters, commemorating the wilderness wandering, in its earlier examination of the Feast of Tabernacles material in John's Gospel; that groundwork doesn't need repeating here. What is worth adding at this specific point in Matthew is a more particular scholarly argument: Peter's proposal to build three of these booths, on the spot, for Moses, Elijah, and Jesus, has been read by some scholars not simply as a well-meaning but confused gesture, but as Peter reaching for the vocabulary of a specific, live expectation — that Sukkot itself, by the late Second Temple period, had accumulated messianic and eschatological associations, so that what Peter proposes is less "let's camp here" and more an attempt to physically stage the messianic-age version of the festival he takes himself to be witnessing [[NOTE:riesenfeld-messianic-sukkot-transfiguration]]. Standard commentary treatment of the verse surveys this reading alongside several competing proposals for what exactly Peter thought he was doing, without settling on any one of them as certain [[NOTE:luz-tabernacles-background-survey]]. What the text itself supplies is only the offer; Mark's parallel adds that Peter "did not know what to say, for they were terrified," a note Matthew's own version omits — a small, honest reminder that Peter's precise motive is being reconstructed by later readers, not stated outright by the narrative itself.
'@

$beat3 = @'
The temple tax that closes the chapter is worth examining as an administrative institution in its own right, not only as backdrop to the coin-in-the-fish detail. Its legal basis reaches back centuries before Jesus: Exodus 30:11-16 establishes a required half-shekel "ransom" contribution from every counted Israelite male twenty years and older, tied explicitly to a census, and by the first century this had become a fixed annual didrachma (two-drachma) payment funding ongoing Temple operations and sacrifice [[NOTE:exodus-30-half-shekel-origin]]. The tax's geographic reach is the more surprising part: this was not a tax collected only from residents of Judea. Jewish communities scattered across the Mediterranean diaspora collected and physically transported their own contributions to Jerusalem each year, a practice attested independently by two writers with no reason to invent it on the Gospels' behalf. Philo, writing from Alexandria, describes the annual contribution as gathered locally and sent up with evident enthusiasm; and Cicero, defending a Roman governor of Asia accused of confiscating a shipment of Jewish gold collected for exactly this purpose, incidentally confirms — from a hostile, non-Jewish courtroom, decades before Jesus's public ministry — that the diaspora-wide collection-and-transport system was already a going concern [[NOTE:diaspora-temple-tax-collection]]. The specific currency this tax had to be paid in, the Tyrian shekel — prized for its unusually reliable silver content despite bearing the graven image of the Tyrian god Melqart — has already been established in this book's later examination of the money-changers Jesus drives from the Temple courts [45], and doesn't need re-deriving here; the same coin problem underlies both scenes. What this chapter's version of the tax scene adds, rather than repeats, is the postscript already flagged elsewhere in this book: within a few years of this scene, and within living memory of anyone present for it, Vespasian redirected the same annual sum toward Rome's Temple of Jupiter instead of abolishing it once its original object was destroyed [41] — a policy later enforced with particular, invasive severity under Domitian, and one modern historians treat as a documented turning point in how the Roman state began legally distinguishing Jews and Jewish practice from the rest of the empire.
'@

$beat4 = @'
The fish-and-coin detail that resolves the tax question is worth flagging as a distinct type of miracle story, not merely as one more wonder in a long list. Form-critical scholarship on the Gospel miracle tradition sorts healing and exorcism accounts — built around a request, a diagnosed affliction, and a demonstrated cure — into a different category from what one influential classification calls "gift miracles": unsolicited, unrequested provisions of a specific material need, told with no interest in explaining how the provision actually happens [[NOTE:theissen-gift-miracle-category]]. The fish-and-coin story fits that second category cleanly, and it sits alongside this Gospel's own feeding miracles — Matthew's version of the feeding doublet already examined in this book [247] — as another instance of Jesus meeting a concrete, practical shortfall (bread for a crowd; a coin for a tax bill) rather than curing a body or expelling a spirit. Older Israelite prophetic legend supplies a genuine ancestor for the same narrative shape: Elisha's multiplying jar of oil and his feeding of a hundred men from twenty barley loaves are themselves classified, in the standard form-critical treatment of Elisha's prophetic legenda, as the same basic gift-or-blessing story type, centuries before the Gospels [[NOTE:rofe-elisha-provision-legenda]].

Two further honest observations belong here. First, this pericope appears only in Matthew — it has no parallel in Mark, Luke, or John — and its narrative texture reads differently from the healing and exorcism material surrounding it: no crowd, no faith-saying, no named beneficiary beyond Peter, and a resolution built around a coin found inside a fish's mouth, which even sympathetic commentators have called one of the more folkloric-sounding notes in the Synoptic miracle tradition [[NOTE:fish-coin-matthew-only-isolated]]. Second, nothing about this literary-form observation settles whether the event happened; it is a claim about the story's shape and transmission history, sitting alongside — not replacing — this chapter's earlier point that the tax custom itself is well documented while the specific miracle sits outside anything external evidence could confirm or rule out.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'TRANSFIGURATION (MOSES/ELIJAH TYPOLOGY)' = "The mountain scene of Matthew 17:1-8 in which Jesus's appearance changes before Peter, James, and John, and Moses and Elijah appear speaking with him. The scene's cloud, glory, and voice-from-the-cloud imagery is built out of Moses's own Sinai encounters (Exodus 24:15-18; 34:29-35) [[NOTE:sinai-cloud-glory-moses-typology]], while Elijah's presence recalls his matching theophany at Horeb (1 Kings 19:8-13) [[NOTE:elijah-horeb-theophany-law-prophets]]; together the two figures stage Matthew's own recurring phrase 'the Law and the Prophets' (Matthew 5:17; 7:12) [[NOTE:law-and-prophets-idiom-matthew]] as an on-page tableau. The chapter's unusually precise 'after six days' opening (17:1) is itself read as a further echo of Exodus 24:16's own six-day interval before Moses is called into the cloud [[NOTE:matthew-six-days-exodus-echo]]. See also MOSES and ELIJAH for the figures individually."
'TEMPLE TAX / DIDRACHMA (FISCUS JUDAICUS POSTSCRIPT)' = "The annual half-shekel (didrachma) levy on adult Jewish men described in Matthew 17:24-27, rooted in the census-ransom law of Exodus 30:11-16 [[NOTE:exodus-30-half-shekel-origin]] and collected not only in Judea but from Jewish communities across the Mediterranean diaspora, a reach independently attested by Philo and by Cicero's Pro Flacco [[NOTE:diaspora-temple-tax-collection]]. The tax had to be paid in Tyrian shekels [45], and its later history is itself a documented postscript: after the Temple's destruction in 70 CE, Vespasian redirected the same sum, still collected empire-wide, to fund Rome's Temple of Jupiter instead, a policy known as the Fiscus Judaicus and later enforced with particular severity under Domitian [41]."
'GIFT MIRACLE (FORM-CRITICAL CATEGORY)' = "A form-critical classification, distinct from healing and exorcism accounts, for miracle stories in which Jesus unexpectedly and without request supplies a specific material need, narrated with no interest in the mechanism of provision [[NOTE:theissen-gift-miracle-category]]. The fish-with-a-coin story of Matthew 17:24-27 is a clear instance, appearing only in Matthew with no Synoptic or Johannine parallel and a distinctly folkloric narrative texture compared to surrounding healing material [[NOTE:fish-coin-matthew-only-isolated]]; Elisha's Old Testament provision miracles (2 Kings 4:1-7, 4:42-44) supply a form-critical ancestor for the same basic story shape [[NOTE:rofe-elisha-provision-legenda]], and this Gospel's own feeding-miracle doublet [247] belongs to the same category."
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
$sortKey = $maxCh17SortKey
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
Seed-Entity "Fiscus Judaicus" "fiscus-judaicus" "vocabulary" "Post-70 CE Roman tax redirecting the former Jerusalem Temple half-shekel levy toward Rome's Temple of Jupiter Capitolinus; imposed by Vespasian, enforced severely under Domitian."

$conn.Close()
Write-Host "DONE Matthew Chapter 17 depth pass."
