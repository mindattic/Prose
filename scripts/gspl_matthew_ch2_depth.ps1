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
$Ch2NodeId = [guid]"019FA049-8E60-70CC-BFAB-692BDB97D336"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh2SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA049-8E60-70CC-BFAB-692BDB97D336' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh2SortKey=$maxCh2SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'macrobius-augustus-herod-joke' = @{ title="Augustus's joke, four centuries later"; body="Macrobius, Saturnalia, Book 2, chapter 4, section 11 (Loeb Classical Library, trans. Robert A. Kaster, Cambridge, MA: Harvard University Press, 2011). Macrobius attributes to Augustus a bilingual pun, on hearing Herod had ordered boys under two killed and that his own son died among them, Augustus is said to have remarked it was better to be Herod's pig (Greek hus) than his son (huios), since a pig would be spared under Jewish dietary law. Writing some four centuries after Herod's reign, in a Christianized empire where Matthew's account had long circulated, Macrobius is a far weaker and later witness than the joke's frequent citation as Roman corroboration suggests, and the anecdote conflates the Bethlehem story with Herod's genuinely documented execution of his adult son Antipater in 4 BCE." }
'davies-allison-massacre-historicity' = @{ title="Silence weighed against population"; body="W. D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel According to Saint Matthew, International Critical Commentary, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 2:16-18. Davies and Allison identify the silence of Josephus, who elsewhere documents Herod's cruelty in granular detail, as the most serious argument against the massacre's historicity, while noting the traditional counterargument that the real death toll, plausibly under twenty children in a small town, may have been too minor an incident for a historian focused on court intrigue and national politics to record separately." }
'bethlehem-population-estimates' = @{ title="How big was Bethlehem, really"; body="Jerome Murphy-O'Connor, The Holy Land: An Oxford Archaeological Guide from Earliest Times to 1700, 5th ed. (Oxford: Oxford University Press, 2008), entry on Bethlehem. Estimates of first-century Bethlehem's population vary substantially in the scholarly literature, from older figures of only a few hundred residents to larger estimates of a Judean town of a few thousand; some scholars point to Josephus's own description of Bethlehem as a city (polis) rather than a village as grounds for the higher figure, and no excavation to date has settled the question definitively." }
'meier-moses-exodus-typology' = @{ title="The Moses pattern"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), discussion of the infancy narratives. Meier situates the massacre within a recognized literary pattern shared with Exodus 1-2, a ruler, warned of a coming deliverer, orders the killing of infant boys, and the deliverer alone escapes, and reads Matthew's account as a theological composition built from that pattern to make a christological claim, the position held by the majority of historical-critical scholars." }
'humphreys-comet-5bce' = @{ title="A comet in the Chinese record"; body="Colin J. Humphreys, 'The Star of Bethlehem, a Comet in 5 BC, and the Date of the Birth of Christ,' Quarterly Journal of the Royal Astronomical Society 32 (1991): 389-407. Humphreys proposes that Chinese court astronomers' records of a broom star (comet) visible for roughly seventy days in 5 BCE, combined with a Jupiter-Saturn conjunction in 7 BCE and a further planetary massing in 6 BCE, together supply a real astronomical sequence that would fit a star appearing, moving, and then remaining visible over a period of months." }
'adair-skeptical-view-star' = @{ title="A theological sign, not a sky report"; body="Aaron Adair, The Star of Bethlehem: A Skeptical View (Wilton, UK: Onus Books, 2013). Adair surveys the leading naturalistic proposals, Kepler's original seventeenth-century case for a 7 BCE Jupiter-Saturn conjunction, Humphreys's 5 BCE comet, and Michael Molnar's Jupiter occultation, and argues each fails to match Matthew's actual description of a star that went before the Magi and stood over one specific house, behavior no real celestial object performs; he concludes the star functions as a theological sign built from Jewish messianic expectation rather than a report of an observed astronomical event." }
'brown-balaam-star-jacob' = @{ title="A star out of Jacob"; body="Raymond E. Brown, The Birth of the Messiah: A Commentary on the Infancy Narratives in Matthew and Luke, updated ed. (New York: Doubleday, 1993), discussion of the Magi and the star. Brown notes that Numbers 24:17's Balaam oracle, a star shall come out of Jacob, was already read as a messianic prediction in Second Temple Judaism, and argues this scriptural background, more than any specific astronomical event, likely shaped both the Magi's supposed recognition of the star's meaning and Matthew's telling of it." }
'pliny-frankincense-myrrh-trade' = @{ title="Real commodities, a real trade route"; body="Pliny the Elder, Natural History, Book 12, sections 51-65 (Loeb Classical Library, trans. H. Rackham, Cambridge, MA: Harvard University Press, 1945). Pliny describes frankincense and myrrh as products of southern Arabia, harvested under hereditary monopoly and carried some sixty-two days by camel caravan along a single guarded route through a string of oases to Gaza, accumulating tolls at every stage, documenting the genuine, high-value trade economy behind two of the three gifts Matthew names." }
'excerpta-latina-barbari-magi-names' = @{ title="Where the names come from"; body="Excerpta Latina Barbari, a Latin chronicle epitome of a lost Greek original (the extant manuscript, Paris, Bibliotheque nationale de France, MS Lat. 4884, was copied around the eighth century from a source usually dated to the sixth century); on its transmission and content see Richard C. Trexler, The Journey of the Magi: Meanings in History of a Christian Story (Princeton: Princeton University Press, 1997). This chronicle is the earliest surviving source to give the Magi individual names, Bithisarea, Melichior, and Gathaspa, ancestors of the familiar Balthasar, Melchior, and Caspar, roughly five centuries after Matthew's Gospel and with no claim to independent historical knowledge of who, if anyone, actually made the journey." }
'trexler-kings-psalm-isaiah' = @{ title="From wise men to crowned kings"; body="Richard C. Trexler, The Journey of the Magi: Meanings in History of a Christian Story (Princeton: Princeton University Press, 1997). Trexler traces how Matthew's unnumbered magoi, a term for Persian or Babylonian priest-astrologers, not royalty, became three kings through later typological readings of Psalm 72:10-11 (the kings of Sheba and Seba shall offer gifts) and Isaiah 60:3, 6 (nations and their kings coming to a rising light, bearing gold and frankincense); the royal status, like the number three and the names, is a demonstrably later theological overlay on a text that itself calls its visitors neither royal nor three in number." }
'josephus-archelaus-passover-massacre' = @{ title="A massacre Josephus does record"; body="Flavius Josephus, Jewish War, Book 2, sections 10-13, and Jewish Antiquities, Book 17, sections 213-218 (Loeb Classical Library). Josephus reports that early in his rule, Archelaus sent troops into the Jerusalem Temple during Passover to suppress a protest, killing some three thousand pilgrims, a documented mass killing by the same man Matthew names as the reason Joseph avoided Judea, and a useful contrast: when Josephus does have a Herodian-family atrocity to report in detail, he reports it in detail." }
'josephus-archelaus-deposition-exile' = @{ title="Deposed within a decade"; body="Flavius Josephus, Jewish Antiquities, Book 17, sections 339-355, and Jewish War, Book 2, sections 111-113 (Loeb Classical Library). Josephus records that in the tenth year of his rule a joint delegation of Judean and Samaritan leaders traveled to Rome to accuse Archelaus of misgovernment and cruelty before Augustus, who deposed him, confiscated his property, and exiled him to Vienne in Gaul, independent confirmation, outside the Gospels entirely, that Archelaus's reign was exactly the kind of dangerous, short-lived rule Matthew's Joseph is depicted as fearing." }
'schurer-archelaus-ethnarch-title' = @{ title="Ethnarch, not king, but Matthew says king anyway"; body="Emil Schurer, The History of the Jewish People in the Age of Jesus Christ (175 B.C.-A.D. 135), revised English ed., ed. Geza Vermes, Fergus Millar, and Matthew Black, vol. 1 (Edinburgh: T&T Clark, 1973), section on Herod's successors. Augustus withheld the royal title Herod had held from Archelaus, granting him only the lesser rank of ethnarch over Judea, Samaria, and Idumea pending good behavior he never demonstrated; Matthew's Greek nonetheless describes him with the verb basileuei, reigns as king (2:22), a loose, non-technical usage rather than a claim about Archelaus's precise constitutional standing." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The chapter's four citations get their due above, but the massacre itself is worth a second, closer pass, because it is the single largest claim in this chapter that rests on Matthew's word alone.

Start with the one piece of pseudo-evidence that keeps circulating as if it settles the matter. Writing some four centuries after Herod's reign, the Roman writer Macrobius records a joke he attributes to the emperor Augustus: on hearing that Herod had ordered boys under two killed and that his own son had died among them, Augustus is supposed to have said it was better to be Herod's pig than his son — a pun that only works in Greek, where "pig" (hus) and "son" (huios) sound alike, and that only lands if the audience assumes a Jewish king would spare his own pig under dietary law [[NOTE:macrobius-augustus-herod-joke]]. It gets cited constantly online as if a Roman historian independently confirms the Bethlehem killings. He doesn't, not really — Macrobius is writing in a Christianized empire where Matthew's story had circulated for centuries, and the joke openly conflates two different things: Herod's real, well-documented execution of his grown son Antipater in 4 BCE, and Matthew's story of infant boys in one small town years earlier. A late garbling of a familiar story is not an independent record of it.

The stronger version of the historicity question, already raised above, deserves its full citation. W. D. Davies and Dale C. Allison, in the standard critical commentary on Matthew, treat the silence of Josephus — who elsewhere documents Herod's cruelty in granular, almost gossipy detail — as the single most serious argument against the massacre as reported, while noting the traditional counterargument fairly: if the true toll was small, plausibly under twenty children, it may simply not have registered next to purges and executions running into the thousands elsewhere in Herod's reign [[NOTE:davies-allison-massacre-historicity]]. How small "small" really was depends on how small Bethlehem was, and that figure is itself unsettled — estimates range from a few hundred residents in older surveys to a few thousand in newer ones, with some scholars pointing to Josephus's own description of Bethlehem as a "city" rather than a village to argue for the higher number; no excavation has closed the question [[NOTE:bethlehem-population-estimates]].

Put the silence, the population dispute, and the story's own shape together, and the majority position in historical-critical scholarship becomes easy to state plainly. John P. Meier's standard treatment of the infancy narratives places the massacre inside a literary pattern lifted directly from Exodus 1-2: a ruler, warned of a coming deliverer, orders the killing of infant boys, and the deliverer alone escapes [[NOTE:meier-moses-exodus-typology]]. Read that way, the massacre isn't a separately attested event that happens to echo Moses — it's a theological composition built from the Moses pattern to make a claim about who Jesus is. That's the mainstream reading, and it is a genuine majority, not a bare plurality. It is not, however, the only reading available, and the fairest summary is that no evidence currently on hand — for the killings or against them — closes the question outright.
'@

$beat2 = @'
The star deserves the same treatment. This chapter already noted Michael Molnar's case for a specific astrological event behind it — a lunar occultation of Jupiter in April of 6 BCE [12] — but that is one entry in a much longer list of attempts to find something real behind Matthew's brief description, and the range of both the naturalistic proposals and the reasons to doubt all of them is worth laying out.

The oldest naturalistic theory belongs to Johannes Kepler, who in the early seventeenth century calculated a rare conjunction of Jupiter and Saturn in Pisces in 7 BCE, followed by Mars joining them in 6 BCE, and proposed that a similar planetary massing had produced a genuine "new star" bright enough to prompt a journey. A newer entry comes from physicist Colin Humphreys, who in 1991 argued that Chinese court astronomers' records of a "broom star" — a comet — visible for roughly seventy days in 5 BCE, combined with the same 7-6 BCE planetary activity, better fits a star that both moves and then holds still over months rather than appearing once [[NOTE:humphreys-comet-5bce]]. Both proposals reach for real, dateable astronomical records; neither is universally accepted even among scholars who take the naturalistic approach seriously.

Aaron Adair's book-length skeptical survey works through Kepler's conjunction, Humphreys's comet, and Molnar's occultation in turn and finds each one falling short of what Matthew's text actually describes: a star that "went before" the Magi on their road and then "stood over" one specific house in one specific town, behavior no real celestial object performs regardless of which candidate is proposed [[NOTE:adair-skeptical-view-star]]. Adair's conclusion, shared by a meaningful share of historical-critical scholars, is that the star is doing theological work rather than reporting an observation. Numbers 24:17 already gave Second Temple Judaism a ready-made messianic image — "a star shall come out of Jacob" — read as a prediction of a coming ruler well before Matthew wrote a word, and Raymond Brown's commentary argues that scriptural background, not any specific night sky, most likely shaped both the Magi's supposed recognition of the sign and Matthew's account of it [[NOTE:brown-balaam-star-jacob]].

None of this settles the question either way. The naturalistic candidates are real, dateable astronomical events that a first-century audience could plausibly have connected to a royal birth; the literary reading is a real, attested scriptural background that doesn't require any specific event in the sky at all. Both readings remain live in the scholarship, and nothing here picks a winner between them.
'@

$beat3 = @'
The gifts themselves — gold, frankincense, and myrrh (2:11) — are worth separating from the tradition that grew up around them, because the text's own claim and the tradition's later additions belong in two different categories entirely.

Start with what's real and checkable. Frankincense and myrrh were not exotic inventions; they were two of the ancient world's genuine high-value luxury commodities, harvested almost exclusively in southern Arabia and traded north along a single, tightly guarded caravan route. Pliny the Elder describes the trip from the incense-producing region to Gaza as some sixty-two days by camel along a chain of oases, with a hereditary monopoly controlling the harvest and tolls accumulating at every stop along the way [[NOTE:pliny-frankincense-myrrh-trade]]. Gold needs no such footnote — it was a universal luxury good across the ancient Mediterranean and Near East. Whatever one makes of the Magi themselves, the three items Matthew names are drawn from real first-century economic life, not invented props.

Everything built on top of the gifts is a different matter. Matthew names three gifts and gives no count of visitors at all — "wise men" (magoi, a term for Persian or Babylonian priest-astrologers) is plural, nothing more specific. The number three is a folk inference from the number of gifts, old enough and universal enough that it barely registers as an inference anymore. The names now attached to that inferred trio — Melchior, Caspar, and Balthasar — first surface roughly five centuries after Matthew, in a Latin chronicle known as the Excerpta Latina Barbari, itself an epitome of an earlier Greek source; the earliest form of the names there is actually Bithisarea, Melichior, and Gathaspa, ancestors of the familiar three [[NOTE:excerpta-latina-barbari-magi-names]]. And the crowns are later still. Richard Trexler's history of the tradition traces how Matthew's unnamed, unnumbered, and unroyal visitors became "three kings" through later typological readings of Psalm 72:10-11 ("the kings of Sheba and Seba shall offer gifts") and Isaiah 60:3, 6 (nations and kings coming to a rising light, bearing gold and frankincense) — passages that describe kings bringing exactly these kinds of gifts to Israel's God, reread centuries later as predictions fulfilled by Matthew's magoi [[NOTE:trexler-kings-psalm-isaiah]].

None of that later embroidery is dishonest, exactly — it's the ordinary way a spare, three-item detail in an ancient text accumulates centuries of devotional elaboration. But "three wise men, kings, named Melchior, Caspar, and Balthasar" is a composite built in stages over five hundred-plus years, and Matthew's own text supplies only the first, real-economy piece of it: unnamed, uncounted magi, and three named, tradeable, real-world gifts.
'@

$beat4 = @'
Archelaus gets one sentence in Matthew — Joseph hears he now rules in his father's place and, afraid, avoids Judea for Galilee instead (2:22) — but that one sentence is also this chapter's best example of a Gospel detail that checks out cleanly against independent history.

Start with what Josephus actually records about the man Joseph is supposedly avoiding. Archelaus did not wait long to earn the fear Matthew gives Joseph: early in his rule he sent troops into the Jerusalem Temple during Passover to put down a protest, and Josephus reports some three thousand pilgrims killed in the resulting crackdown — a real, specific, independently documented mass killing carried out by this same man, and worth noting precisely because it shows Josephus was entirely willing to report a Herodian-family atrocity in detail when he had one to report [[NOTE:josephus-archelaus-passover-massacre]]. The reign that followed matched its opening. In its tenth year, a joint delegation of Judean and Samaritan leaders traveled to Rome to lay formal complaints of misrule and cruelty before Augustus, who deposed Archelaus, confiscated his property, and exiled him to Vienne in Gaul — an ending entirely outside the Gospels, recorded because a Roman administrative crisis produced its own paper trail [[NOTE:josephus-archelaus-deposition-exile]].

One more precise detail is worth flagging on the way out. Augustus had specifically withheld from Archelaus the royal title his father Herod held, granting him only the lesser rank of ethnarch over Judea, Samaria, and Idumea, pending good behavior he plainly never delivered. Matthew's Greek nonetheless describes him with the verb basileuei — "reigns," or more literally "is king" (2:22) — which is loose, everyday usage rather than a precise claim about Archelaus's actual constitutional rank [[NOTE:schurer-archelaus-ethnarch-title]]. It's a small imprecision, and not the kind of error that undercuts the passage's larger point: whatever title Rome gave him, Archelaus was exactly the dangerous, short-lived ruler that made a family fleeing Herod's dynasty want to settle somewhere else.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'THE MAGI (WISE MEN)' = "Unnamed, uncounted visitors from ``the East'' who come to Jerusalem asking after a newborn king of the Jews, guided by a star, and who present the infant Jesus with gold, frankincense, and myrrh before being warned in a dream not to return to Herod (2:1-12). ``Magi'' (Greek magoi) denotes a recognized class of Persian or Babylonian priest-astrologers, not royalty. Matthew gives no number of visitors and no names; both the traditional count of three and the later names Melchior, Caspar, and Balthasar are demonstrably later accretions, the names first attested roughly five centuries after this Gospel [[NOTE:excerpta-latina-barbari-magi-names]], and their royal status a still-later typological reading of Psalm 72 and Isaiah 60 [[NOTE:trexler-kings-psalm-isaiah]]."
'STAR OF BETHLEHEM' = "The celestial sign that leads the Magi first to Jerusalem and then to the house where the child is (2:2, 2:9-10). Proposed naturalistic candidates span a rare Jupiter-Saturn conjunction in 7 BCE, a comet recorded in Chinese astronomical annals in 5 BCE [[NOTE:humphreys-comet-5bce]], and a lunar occultation of Jupiter in 6 BCE (see note 12, earlier in this chapter); a substantial body of scholarship argues instead that the star is a literary device drawing on Numbers 24:17's messianic ``star out of Jacob'' rather than a claim about an observed astronomical event [[NOTE:brown-balaam-star-jacob]], and a full skeptical survey finds none of the naturalistic candidates matching the text's description of a star that moves ahead of travelers and stops over a single house [[NOTE:adair-skeptical-view-star]]."
'MASSACRE OF THE INNOCENTS (HISTORICITY QUESTION)' = "Herod's order, per Matthew alone, to kill every boy two years old or under in and around Bethlehem after the Magi fail to report back to him (2:16-18). No source outside Matthew records the event, including Josephus, whose extensive, unfriendly catalogue of Herod's real cruelties never mentions it [[NOTE:davies-allison-massacre-historicity]]; a fifth-century joke attributed to Augustus is sometimes cited as external confirmation but is too late and too derivative to serve as independent evidence [[NOTE:macrobius-augustus-herod-joke]]. The minority argument for historicity rests on Bethlehem's small, disputed population [[NOTE:bethlehem-population-estimates]]; the majority historical-critical reading treats the episode as a theological composition modeled on Pharaoh's decree against Hebrew infants in Exodus 1-2 [[NOTE:meier-moses-exodus-typology]]."
'THREE GIFTS (GOLD, FRANKINCENSE, MYRRH)' = "The Magi's presentation gifts to the infant Jesus (2:11), and the sole textual basis for the popular tradition of ``three'' wise men — Matthew names three gifts, never a number of visitors. Frankincense and myrrh were genuine high-value commodities of the period, harvested in southern Arabia and carried to the Mediterranean along a single long-distance caravan route [[NOTE:pliny-frankincense-myrrh-trade]]; gold needs no such explanation, being a universal ancient luxury good. The gifts' real economic grounding stands in contrast to everything later ages built on top of them — a specific count of visitors, names, and royal status — none of which the text itself supplies."
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

# ---- Insert chapter beats (appended after existing Ch2 beats) ----
$sortKey = [double]1000
if ($maxCh2SortKey -ge $sortKey) { $sortKey = $maxCh2SortKey }
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch2NodeId $id $sortKey
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
Seed-Entity "The Magi (Wise Men)" "the-magi-wise-men" "vocabulary" "Unnamed, uncounted visitors from the East who follow a star to Jerusalem and Bethlehem seeking a newborn king of the Jews, per Matthew 2:1-12; a Persian/Babylonian priest-astrologer class, not royalty."
Seed-Entity "Star of Bethlehem" "star-of-bethlehem" "vocabulary" "The celestial sign guiding the Magi in Matthew 2:2-10; subject of both naturalistic astronomical proposals (comet, planetary conjunction, occultation) and literary/theological readings tied to Numbers 24:17."
Seed-Entity "Massacre of the Innocents (Historicity Question)" "massacre-of-the-innocents-historicity-question" "vocabulary" "Herod's order in Matthew 2:16-18 to kill Bethlehem's infant boys age two and under; uncorroborated by any source outside Matthew, including Josephus."
Seed-Entity "Three Gifts (Gold, Frankincense, Myrrh)" "three-gifts-gold-frankincense-myrrh" "material" "The Magi's presentation gifts in Matthew 2:11; real first-century luxury commodities, and the textual origin of the traditional (but unstated) count of three visitors."

$conn.Close()
Write-Host "DONE Matthew Chapter 2 depth pass."
