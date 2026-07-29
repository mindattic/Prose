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
$Ch3NodeId = [guid]"019FA054-FBB7-7C0A-AFA4-DF042B65F960"

$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh3SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA054-FBB7-7C0A-AFA4-DF042B65F960' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh3SortKey=$maxCh3SortKey"

# ---- Notes (slug -> title/body) in order ----
$notes = [ordered]@{
'josephus-antiquities-locus' = @{ title="Pinning down the Josephus reference"; body="Flavius Josephus, Jewish Antiquities, Book 18, chapter 5, section 2, sections 116-119 in the standard Niese section numbering (Loeb Classical Library, vol. IX: Books XVIII-XX, trans. Louis H. Feldman, Cambridge, MA: Harvard University Press, 1965). This is the exact locus this chapter's discussion of Josephus's independent account of John the Baptist draws on; it sits in Book 18's account of Herod Antipas's reign, in a passage unconnected in Josephus's own text to his separate, much-disputed mention of Jesus earlier in the same book (18.3.3)." }
'josephus-antiquities-date' = @{ title="How independent, and how late"; body="Steve Mason, Josephus and the New Testament, 2nd ed. (Peabody, MA: Hendrickson Publishers, 2003), chapter on Josephus's value as an independent source for earliest Christianity. Mason places the Jewish Antiquities' composition at approximately 93-94 CE, under Flavian patronage in Rome, roughly six decades after John the Baptist's death; Josephus shows no sign of drawing on Synoptic material for his account, and his own framing of John's baptism (a purification available to those already made righteous by right living) differs in emphasis from the Synoptics' baptism for the forgiveness of sins, a divergence read as evidence the two traditions developed independently of one another rather than one copying the other." }
'josephus-testimonium-contrast' = @{ title="A cleaner passage than its more famous neighbor"; body="John P. Meier, A Marginal Jew: Rethinking the Historical Jesus, Volume One: The Roots of the Problem and the Person (New York: Doubleday, 1991), chapter 3, ``Josephus.'' Meier's survey of non-Christian sources treats Josephus's brief, disputed mention of Jesus (the Testimonium Flavianum, Antiquities 18.3.3) as authentic only in part, reworked by later Christian scribes into something closer to a confession of faith than Josephus himself would have written; by contrast, Meier and the great majority of Josephan scholars regard the separate John the Baptist passage (18.5.2) as essentially undisputed, since nothing in its content or placement reads as a Christian insertion serving Christian interests." }
'mandaean-baptist-veneration' = @{ title="A separate tradition that still venerates John"; body="Edmondo Lupieri, The Mandaeans: The Last Gnostics (Grand Rapids, MI: William B. Eerdmans Publishing Co., 2002), part one, on Mandaean origins and their relationship to John the Baptist. The Mandaeans, a small Gnostic religious community historically centered in Iraq and Iran and still practicing today, venerate John the Baptist as a central prophetic figure independent of Christianity. Lupieri cautions against reading this as ancient corroboration in its own right: the community's surviving scriptures date many centuries after the first century, and its adoption of John appears at least partly bound up with defining itself against the Christian movement rather than preserving an unbroken first-century memory." }
'elijah-2kings-echo-davies-allison' = @{ title="Dressed as a citation"; body="W.D. Davies and Dale C. Allison Jr., A Critical and Exegetical Commentary on the Gospel according to Saint Matthew, International Critical Commentary, vol. 1 (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 3:4. Davies and Allison read Matthew's description of John's camel's-hair garment and leather belt as a deliberate compositional echo of 2 Kings 1:8's description of Elijah, functioning as visual shorthand identifying John with Elijah before the narrative states the connection outright." }
'2kings-hairy-ambiguity-sweeney' = @{ title="Was Elijah hairy, or was his coat?"; body="Marvin A. Sweeney, I & II Kings: A Commentary, Old Testament Library (Louisville, KY: Westminster John Knox Press, 2007), commentary ad loc. 2 Kings 1:8. The underlying Hebrew phrase describing Elijah (literally something like ``a man, an owner of hair'') is genuinely ambiguous between ``a hairy man'' and ``a man wearing a hairy garment,'' an ambiguity Sweeney discusses at the verse; most translations and the Matthew evangelist's own use of the image read it as clothing, which is also the reading that makes the parallel to John's camel's-hair garment work as a matched costume rather than a physical resemblance." }
'malachi-elijah-typology-allison' = @{ title="The prophet who was supposed to come back"; body="Dale C. Allison Jr., ``Elijah Must Come First,'' Journal of Biblical Literature 103, no. 2 (1984): 256-258. Malachi 4:5 (3:23 in the Hebrew numbering) promises that God will send ``Elijah the prophet'' before ``the great and terrible day of the LORD.'' Allison's note, part of a running exchange in JBL with M.M. Faierstein and Joseph Fitzmyer over how this expectation was actually understood in first-century Judaism, situates Matthew's Elijah-costuming of John in chapter 3 as a setup for the Gospel's own later, explicit identification of John as that promised Elijah figure (Matthew 11:14; 17:10-13)." }
'john-gospel-elijah-denial' = @{ title="A Gospel that has John deny it himself"; body="Raymond E. Brown, The Gospel According to John I-XII, Anchor Bible vol. 29 (Garden City, NY: Doubleday & Company, 1966), commentary ad loc. John 1:19-21. The Fourth Gospel has priests and Levites ask John directly, ``Are you Elijah?'' and has him answer, ``I am not.'' Brown treats this as a real point of tension with the Synoptic tradition (including Matthew's own clothing-echo in this chapter and its later explicit identification at 11:14): one Gospel dresses John as Elijah and eventually says so outright, another has him personally reject the title, and the New Testament as a whole never resolves the two readings against each other." }
'qumran-immerser-taylor' = @{ title="The closest attested parallel practice"; body="Joan E. Taylor, The Immerser: John the Baptist within Second Temple Judaism (Grand Rapids, MI: William B. Eerdmans Publishing Co., 1997). Taylor situates John's baptism within the wider landscape of Second Temple Jewish ritual immersion, arguing that the initiatory washings practiced at Qumran are the closest attested parallel to what John was doing in the same general region and period, while cautioning against reading John himself as a lapsed or associate Essene: he appears in the sources as a solitary figure who did not found or join a closed sect the way the Qumran community did." }
'qumran-direct-connection-debate-charlesworth' = @{ title="An old debate that has never been settled either way"; body="James H. Charlesworth, ed., John and Qumran (London: Geoffrey Chapman, 1972). This volume, gathering scholarship from the first generation of Dead Sea Scrolls research, opened a debate over whether John the Baptist himself had direct contact with the Qumran community (a monastery within walking distance of the wilderness John preached in) that has never been resolved in either direction: there is no explicit ancient statement placing John at Qumran or naming him a member, but the shared vocabulary of repentance, wilderness, and washing has kept the question alive in the scholarship for decades since." }
'qumran-1qs-repentance-parallel' = @{ title="A washing that does not work without repentance first"; body="Geza Vermes, trans., The Complete Dead Sea Scrolls in English, rev. ed. (London: Penguin Books, 2004), Community Rule (1QS), columns 3 and 5. The Qumran community's own Community Rule states that the wicked ``shall not enter the water'' of purification, ``for they shall not be cleansed unless they turn from their wickedness'' (1QS 5:13-14), tying ritual washing to prior moral repentance in a way frequently compared to John's demand in this same chapter that his own candidates ``bear fruit worthy of repentance'' (Matthew 3:8) rather than relying on washing, or ancestry, alone. The same document also invokes Isaiah 40:3 (``prepare the way of the Lord'') as its own community's foundational text, the identical verse Matthew quotes for John a few lines earlier in this chapter (3:3), a coincidence of shared scriptural self-description across two separate wilderness-based renewal movements that this chapter's earlier discussion of the Isaiah citation did not address." }
'qumran-archaeology-magness' = @{ title="Same wilderness, overlapping decades"; body="Jodi Magness, The Archaeology of Qumran and the Dead Sea Scrolls (Grand Rapids, MI: William B. Eerdmans Publishing Co., 2002), chapters on the site's occupation history. Magness's excavation-based chronology places sustained sectarian occupation of Qumran from roughly 100 BCE to 68 CE, meaning the community was an active, functioning presence in the Judean wilderness for the entire span of John's likely ministry and lifetime, on the same stretch of desert bordering the Dead Sea and the lower Jordan region this chapter locates John's own preaching in." }
'matt-3-16-opened-to-him-variant' = @{ title="Opened — but opened to whom, exactly?"; body="Bruce M. Metzger, A Textual Commentary on the Greek New Testament, 2nd ed. (Stuttgart: Deutsche Bibelgesellschaft / United Bible Societies, 1994), commentary ad loc. Matthew 3:16. The oldest and best manuscripts of Matthew 3:16 simply read ``the heavens were opened,'' while a number of other manuscripts add the dative pronoun ``to him'' (autoi), specifying that the heavens opened for Jesus in particular. Modern critical editions generally favor the shorter reading as original, treating the added pronoun as a natural scribal clarification rather than a lost piece of the earliest text; the variant is minor in substance but a genuine textual difference all the same." }
'luke-3-22-adoptionist-variant' = @{ title="A more dramatic variant one Gospel over"; body="Bart D. Ehrman, The Orthodox Corruption of Scripture: The Effect of Early Christological Controversies on the Text of the New Testament (Oxford: Oxford University Press, 1993), chapter on anti-adoptionistic corruptions. Codex Bezae and a scattering of Old Latin manuscripts and patristic citations (Justin Martyr, Clement of Alexandria) preserve a variant of the divine voice's words at the parallel baptism scene in Luke 3:22 reading ``You are my Son, today I have begotten you'' — a direct quotation of Psalm 2:7 — rather than the majority text's ``with you I am well pleased.'' Ehrman argues the majority reading is the later, theologically motivated correction, softening a phrase that could otherwise be read as saying Jesus became God's son at his baptism rather than before it. Matthew's own version of the voice in this chapter (3:17) does not carry this particular variant, but the dispute over Luke's wording is a reminder that even this scene's most theologically loaded sentence was not transmitted with perfect uniformity across the manuscript tradition." }
'isaiah-42-servant-echo-voice' = @{ title="An echo of the Servant Songs, not just a blessing"; body="Joel Marcus, Mark 1-8: A New Translation with Introduction and Commentary, Anchor Bible vol. 27 (New York: Doubleday, 2000), commentary ad loc. Mark 1:11 (parallel to Matthew 3:17 and Luke 3:22). Marcus reads the divine voice's language — ``my beloved'' and ``well pleased'' — as deliberately echoing Isaiah 42:1, the opening line of the first Servant Song (``my chosen, in whom my soul delights''), so that the baptismal declaration doubles as an allusion identifying Jesus with Isaiah's suffering-servant figure, layered on top of its more obvious echo of royal-messianic language from Psalm 2:7. Matthew, Mark, and Luke each render the sentence slightly differently — Matthew alone casts it in the third person (``This is my Son''), where Mark and Luke both have the voice address Jesus directly (``You are my Son'') — a small but real difference in address across the three otherwise-parallel accounts of the same moment." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders) ----
$beat1 = @'
The chapter has already leaned on Josephus once, for his independent description of John and his baptism, and for Herod Antipas's own stated motive (political fear of a popular movement, not personal grievance) for having him killed. That reference is worth returning to and pinning down more precisely, because how much weight a piece of outside corroboration can bear depends on exactly how solid and how independent it really is.

The passage sits at Jewish Antiquities 18.5.2, sections 116 through 119, in the account of Herod Antipas's reign [[NOTE:josephus-antiquities-locus]]. Josephus composed the Antiquities around 93 or 94 CE in Rome, under Flavian patronage — roughly six decades after John's death — and nothing in his phrasing suggests he is drawing on the Synoptic Gospels; if anything, his framing of John's baptism as bodily purification for people already made righteous by right living reads as a different tradition rather than a borrowed one [[NOTE:josephus-antiquities-date]].

That independence carries more weight here than it does for Josephus's other, far more famous brush with the Christian story. Nineteen sections earlier in the same book, Josephus mentions Jesus directly, in the passage scholars call the Testimonium Flavianum (18.3.3) — and that passage has been argued over for well over a century, with most historians today concluding it was reworked by later Christian copyists into something closer to a confession of faith than whatever Josephus originally wrote. The John the Baptist passage carries none of that baggage. Its authenticity is barely disputed, precisely because nothing about its content or its placement in the text reads as a Christian insertion serving Christian interests [[NOTE:josephus-testimonium-contrast]].

One more data point belongs in the same conversation, if only as a caution against overreading it: John the Baptist is still venerated today, independent of Christianity, as a central prophetic figure by the Mandaeans, a small Gnostic religious community historically rooted in Iraq and Iran. It would be a mistake to read that as ancient corroboration in its own right — the Mandaeans' surviving scriptures date many centuries after the first century, and their adoption of John looks at least partly bound up with distinguishing themselves from the Christian movement rather than preserving some unbroken living memory [[NOTE:mandaean-baptist-veneration]]. What Josephus supplies, and Mandaean tradition does not, is a contemporary-adjacent, non-Christian, largely undisputed textual witness that a man named John existed, drew crowds, preached something recognizable as this chapter's account, and was killed for it.
'@

$beat2 = @'
Matthew's description of John's clothing — "camel's hair, and a leather belt around his waist" (3:4) — is not incidental costuming, and the chapter's earlier discussion is right to call it a direct echo of 2 Kings 1:8's description of Elijah. It is worth going one step further into how that echo works and where it leads.

Mainstream commentary reads the wardrobe detail as exactly that: a compositional signal, dressing John as Elijah before the narrative says a word about it directly, so that an audience that knew its scriptures would recognize the type on sight [[NOTE:elijah-2kings-echo-davies-allison]]. There is a genuine wrinkle underneath the source text, though. The Hebrew phrase 2 Kings uses to describe Elijah is ambiguous — it can be read either as "a hairy man" or as "a man wearing a hairy garment," and commentators are divided on which the original author meant. Most translations, and Matthew's own use of the image, take it as clothing, which is also the reading that makes the parallel to John's camel's-hair garment work as a matched costume rather than a claim about anyone's actual body hair [[NOTE:2kings-hairy-ambiguity-sweeney]].

The costume is a setup, not just an allusion for its own sake. Malachi 4:5 promises that God will send "Elijah the prophet" before "the great and terrible day of the LORD," and this chapter's Elijah-dressed John is Matthew laying the groundwork for a payoff that arrives later in the same Gospel, when Jesus states outright that John is the Elijah who was to come (11:14; 17:10-13) [[NOTE:malachi-elijah-typology-allison]]. Whether first-century Judaism generally expected a literal return of Elijah in this sense, or something looser, was itself argued over in a running exchange among biblical scholars in the 1980s — a reminder that even the background assumption Matthew is building on wasn't a single settled idea across all of Judaism, only one live strand of expectation among others.

It's worth noting, in the interest of giving the full picture rather than just Matthew's side of it, that the identification is not uncontested within the New Testament itself. The Gospel of John has priests and Levites put the question to John directly — "Are you Elijah?" — and has him answer, flatly, "I am not" (John 1:21). One Gospel dresses John as Elijah and later says so outright; another has him personally reject the title on the page. The New Testament never resolves the two readings against each other, and this chapter's own Elijah-costuming is only one half of that conversation [[NOTE:john-gospel-elijah-denial]].
'@

$beat3 = @'
John's baptism did not appear out of nowhere in an otherwise dry religious landscape, and one specific parallel is worth setting out on its own, since the earlier discussion of Josephus and the criterion of embarrassment does not touch it: the ritual-washing practice of the Qumran community, whose settlement on the shore of the Dead Sea sat within the same wilderness-of-Judea region this chapter locates John's preaching in.

Scholars widely treat the initiatory and repeated purification washings practiced at Qumran as the closest attested parallel to what John was doing, in the same general period and country. That comparison comes with a real caution attached, though: John appears in every source as a solitary figure operating on his own, not as someone who founded or joined a closed sect the way the Qumran community's members did [[NOTE:qumran-immerser-taylor]]. Whether John had any direct, personal connection to the Qumran community itself is a question scholarship has argued over since the first wave of Dead Sea Scrolls research in the mid-twentieth century, and it remains genuinely unresolved: no ancient source places John at Qumran or names him a member, but the shared vocabulary of repentance, wilderness, and washing has kept the possibility alive in the literature for decades without ever being settled either way [[NOTE:qumran-direct-connection-debate-charlesworth]].

The overlap in substance, not just geography, is worth a closer look. The Qumran community's own Community Rule states that the wicked "shall not enter the water" of purification, "for they shall not be cleansed unless they turn from their wickedness" — washing tied explicitly to prior repentance, not treated as effective on its own. That is close kin to John's demand, a few lines earlier in this same chapter, that his candidates "bear fruit worthy of repentance" (3:8) rather than leaning on water or ancestry alone. The same Qumran document also cites Isaiah 40:3 — "prepare the way of the LORD" — as its own community's founding self-description, the identical verse Matthew has already quoted for John in this chapter (3:3): two separate wilderness renewal movements, in the same century and the same general region, independently reaching for the same verse to explain themselves [[NOTE:qumran-1qs-repentance-parallel]]. And the timing lines up: excavation-based dating places sustained occupation at Qumran from roughly 100 BCE to 68 CE, meaning the community was an active presence in the Judean wilderness for the entire span of John's likely lifetime and ministry [[NOTE:qumran-archaeology-magness]]. None of this proves contact between the two movements. It does establish that John's baptism was not an isolated invention; it belonged to a recognizable regional current of Jewish repentance-and-purification practice that more than one group was drawing on at the same time.
'@

$beat4 = @'
The baptism scene's most theologically weighted sentence — the voice from heaven declaring "This is my Son, the Beloved, with whom I am well pleased" (3:17) — is worth a closer look at the level of the actual words, both within Matthew's own manuscript tradition and against its parallels in Mark and Luke.

Start with a small, genuine variant inside Matthew's own text. The oldest and best manuscripts of 3:16 read simply "the heavens were opened," while a number of other manuscripts add a dative pronoun specifying that the heavens opened "to him" — clarifying, redundantly, that the opening was for Jesus in particular. Modern critical editions generally favor the shorter reading as original and treat the added pronoun as a natural scribal clarification rather than a lost piece of the earliest text. It is a minor variant in substance, but a real one — a small, checkable reminder that even a single Gospel's own wording for this scene was not transmitted with perfect uniformity [[NOTE:matt-3-16-opened-to-him-variant]].

A more consequential variant shows up one Gospel over. Codex Bezae, some Old Latin manuscripts, and a handful of early Christian writers (Justin Martyr, Clement of Alexandria among them) preserve a version of the divine voice at Luke's parallel baptism scene (Luke 3:22) that reads "You are my Son, today I have begotten you" — a direct quotation of Psalm 2:7 — rather than the majority text's "with you I am well pleased." The scholarly argument is that the majority reading is the later, theologically motivated correction, softening a phrase that on its face could be read as saying Jesus became God's son at his baptism rather than before it. Matthew's version in this chapter does not carry this particular variant, but the dispute over Luke's wording is a reminder that this scene's most loaded sentence was not fixed identically everywhere it survives [[NOTE:luke-3-22-adoptionist-variant]].

Even setting textual variants aside, the wording Matthew, Mark, and Luke each settle on differs in a way worth naming plainly: Matthew alone casts the voice in the third person, announcing Jesus to the crowd ("This is my Son"), where Mark and Luke both have the voice address Jesus directly ("You are my Son"). And the phrase "in whom I am well pleased" is itself widely read as reaching past Psalm 2's royal-messianic language to echo Isaiah 42:1, the opening line of the first Servant Song ("my chosen, in whom my soul delights") — layering a second scriptural identity, the suffering servant, on top of the more obvious royal one, in a sentence three Gospels tell just slightly differently from one another [[NOTE:isaiah-42-servant-echo-voice]].
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'PHARISEES AND SADDUCEES (FIRST-CENTURY JEWISH SECTS)' = "The two Jewish religious-political parties John confronts when he sees them coming to his baptism and calls them a ``brood of vipers'' (3:7). The Pharisees emphasized meticulous observance of both the written Torah and an oral tradition of interpretation, along with belief in resurrection and angels; the Sadducees, drawn largely from priestly and aristocratic circles tied to Temple administration, rejected the oral tradition and denied resurrection, and largely disappeared as a group after the Temple's destruction in 70 CE removed the institution their authority depended on. The two groups did not typically make common cause in this period, which is part of why John singling out both together, arriving as a body, has drawn scholarly comment — though nothing in the text explains why they came together on this occasion."
'QUMRAN / DEAD SEA SCROLLS COMMUNITY' = "A Jewish sectarian community settled beside the Dead Sea, in the same general wilderness-of-Judea region this chapter locates John's preaching in, active from roughly 100 BCE to 68 CE and responsible for the manuscript library known as the Dead Sea Scrolls, discovered beginning in 1947. Its own initiatory and repeated ritual-washing practice is widely regarded by scholars as the closest attested parallel to John's baptism in the surrounding record, and its Community Rule ties washing explicitly to prior repentance in language often compared to John's own demand for ``fruit worthy of repentance'' [[NOTE:qumran-1qs-repentance-parallel]]. Whether John had any personal, direct connection to the community itself remains a genuinely open scholarly question, unresolved in either direction since debate on it began in the 1950s [[NOTE:qumran-direct-connection-debate-charlesworth]]."
'BAPTISM (FIRST-CENTURY JEWISH IMMERSION PRACTICE)' = "Ritual water immersion, the practice John is best known for and the rite Jesus undergoes at the end of this chapter (3:13-17). First-century Judaism already practiced various forms of ritual washing for purity, and the initiatory washings at Qumran are the closest attested regional parallel to John's version of the practice [[NOTE:qumran-immerser-taylor]], though John's baptism is generally read by scholars as distinctive in being a one-time, public rite of repentance rather than the Qumran community's repeated purificatory washing. Josephus's independent account frames the purpose slightly differently than the Synoptic Gospels do — purification of the body for people already made righteous, rather than the Synoptics' baptism ``for the forgiveness of sins'' — a real, if subtle, divergence in how the same basic practice was understood by two separate ancient sources."
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
$sortKey = $maxCh3SortKey
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 1000
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch3NodeId $id $sortKey
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

# ---- Seed new entities (checked against existing catalog first; John the Baptist, Elijah, Jordan River, Pharisees, Sadducees, Qumran Community, and Flavius Josephus already exist and are NOT reseeded here) ----
Seed-Entity "Testimonium Flavianum" "testimonium-flavianum" "vocabulary" "The disputed passage about Jesus in Josephus's Jewish Antiquities 18.3.3, widely regarded as authentic in part but reworked by later Christian copyists; discussed here as a contrast case against the essentially undisputed John the Baptist passage nineteen sections later (18.5.2)."
Seed-Entity "Mandaeans" "mandaeans" "vocabulary" "A small Gnostic religious community historically rooted in Iraq and Iran, still practicing today, that venerates John the Baptist as a central prophetic figure independent of Christianity; its surviving scriptures date many centuries after the first century."
Seed-Entity "Baptism (first-century Jewish immersion practice)" "baptism-first-century-jewish-immersion-practice" "vocabulary" "Ritual water immersion as practiced in first-century Judaism, including John's baptism and its closest attested parallel, the initiatory washings of the Qumran community."

$conn.Close()
Write-Host "DONE Matthew Chapter 3 depth pass."
