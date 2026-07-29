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
$Ch1NodeId = [guid]"019FA049-5D94-766F-A919-4623FD605028"

# Hardened derivations: filter IsEnabled=1 everywhere (soft-deleted rows exist elsewhere
# in this shared Notes/Glossary universe and must not be counted), and guard the note-number
# derivation against a known stray non-numeric row in the shared Notes node.
$maxNoteSortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1")
$maxGlossarySortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA026-3758-7A57-AF65-EC1DB4303EF3' AND bn.IsEnabled=1")
$maxNoteNumber = [int](Exec-Scalar "SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0) FROM BeatNodes bn JOIN Beats b ON bn.BeatId=b.Id WHERE bn.NodeId='019FA01D-FA22-76C6-976C-3EA4F4D54A14' AND bn.IsEnabled=1 AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'")
$maxCh1SortKey = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0) FROM BeatNodes bn WHERE bn.NodeId='019FA049-5D94-766F-A919-4623FD605028' AND bn.IsEnabled=1")

Write-Host "Starting: MaxNumber=$script:MaxNumber MaxNoteSortKey=$maxNoteSortKey MaxNoteNumber=$maxNoteNumber MaxGlossarySortKey=$maxGlossarySortKey MaxCh1SortKey=$maxCh1SortKey"

# ---- Notes (slug -> title/body) in order ----
# This is a DEPTH PASS: existing Matthew ch1 prose and its 11 note-references are untouched.
# These 5 notes are new, additive, and cover checkable-claim territory the original pass did
# not cite: the genealogy's own internal arithmetic (omitted kings, gematria), Luke's rival
# genealogy, a manuscript check on the almah/parthenos question already raised at note 11,
# and Herod the Great's own regnal chronology (his glossary entry already exists; this note
# supplies the citation his dates never got there).
$notes = [ordered]@{
'genealogy-omitted-kings-chronicles' = @{ title='Three kings deleted: Matthew compresses Joram to Uzziah'; body="W. D. Davies and Dale C. Allison Jr., Matthew 1-7, International Critical Commentary (Edinburgh: T&T Clark, 1988), commentary ad loc. Matthew 1:8. Matthew 1:8 moves directly from `"Joram the father of Uzziah,`" but the parallel record of the same royal line, 1 Chronicles 3:11-12, lists three additional kings standing between them — Ahaziah, Joash, and Amaziah. Davies and Allison and the wider critical-commentary tradition read this as a deliberate compression rather than a memory error: pruning the list to hold the fourteen-generation count Matthew announces in 1:17 required dropping names somewhere, and this is one of the places it happened. Some commentators additionally note that all three omitted kings descend through Athaliah, daughter of the northern house of Ahab and Jezebel (2 Chronicles 22:1-9), which may have made them unwelcome names in an official reckoning of David's line for reasons beyond arithmetic, though the mnemonic explanation is the one most consistently argued." }
'genealogy-gematria-david' = @{ title='Why fourteen: the numerical value of David''s name'; body="Raymond E. Brown, The Birth of the Messiah: A Commentary on the Infancy Narratives in Matthew and Luke, updated ed. (New York: Doubleday, 1993), excursus on the structure of the genealogy (commentary ad loc. Matthew 1:1-17). Hebrew had no separate system of numerals in this period; its letters served double duty as numbers, so any name could also be read as a sum. David's name in Hebrew consonants — dalet-vav-dalet — totals four plus six plus four: fourteen. Brown treats this gematria as the most widely accepted explanation for why Matthew organizes the genealogy into three sets of exactly fourteen (1:17) rather than any other number. Brown and other critical commentators also flag the count's own internal problem: reading Matthew's own third set of names (1:12-16) yields thirteen entries, not fourteen, unless Jechoniah is counted twice — once as the deposed king of verse 11, once again as the first name of the third set in verse 12. Proposed harmonizations for this shortfall go back to antiquity and number more than a dozen; none is demanded by the text itself, and Matthew never states the double-count explicitly." }
'luke-3-genealogy-divergence' = @{ title='Two family trees for one Joseph: Luke''s rival genealogy'; body="Raymond E. Brown, The Birth of the Messiah: A Commentary on the Infancy Narratives in Matthew and Luke, updated ed. (New York: Doubleday, 1993), comparative appendix on the two genealogies; I. Howard Marshall, The Gospel of Luke, New International Greek Testament Commentary (Grand Rapids: Eerdmans, 1978), commentary ad loc. Luke 3:23-38; Julius Africanus's harmonization proposal is preserved in Eusebius, Historia Ecclesiastica 1.7.1-16 (Loeb Classical Library edition). Luke's genealogy runs backward from Jesus to Adam (3:23-38) rather than forward from Abraham, and it diverges from Matthew immediately below David: Matthew traces the royal line through Solomon, the throne-holding son; Luke traces it through Nathan, a different son of David and Bathsheba named at 2 Samuel 5:14 who never reigned. The two lists converge only at two names, Shealtiel and Zerubbabel, then diverge again at once; Matthew names Joseph's father Jacob (1:16), Luke names him Heli (3:23). Julius Africanus, writing in the third century, already proposed a levirate-marriage solution making Heli and Jacob half-brothers; a more recent, widely popularized proposal reads Luke's list as Mary's genealogy rather than Joseph's, though Luke's own Greek text names Joseph, not Mary, as the one `"the son (as was thought) of Heli.`" Mainstream historical-critical scholarship generally treats the two genealogies as independent theological constructions rather than reconcilable historical records." }
'great-isaiah-scroll-almah' = @{ title='A Hebrew manuscript from before Jesus, already reading almah'; body="Eugene Ulrich, ed., The Biblical Qumran Scrolls: Transcriptions and Textual Variants, Supplements to Vetus Testamentum 134 (Leiden: Brill, 2010), critical transcription of 1QIsa^a at Isaiah 7:14; Peter W. Flint and James C. VanderKam, The Meaning of the Dead Sea Scrolls: Their Significance for Understanding the Bible, Judaism, Jesus, and Christianity (San Francisco: HarperSanFrancisco, 2002), on the scroll's discovery and dating. The Great Isaiah Scroll (1QIsa^a), one of the original seven Dead Sea Scrolls recovered from Qumran Cave 1 in 1947, is the oldest complete copy of any book of the Hebrew Bible yet found, paleographically dated to roughly the 2nd century BCE — more than a century before Jesus's birth. At Isaiah 7:14 it reads ha-almah, the identical Hebrew word later preserved in the medieval Masoretic Text. This settles a question distinct from the Greek translation question already raised in this chapter: the Hebrew wording itself is not a late alteration made in reaction to Christian claims about the birth, since a documented Hebrew manuscript already carried that wording before there was a Christian claim to react to." }
'herod-death-chronology-josephus' = @{ title='Herod''s own clock: dating a reign this book will need next'; body="Flavius Josephus, Jewish Antiquities 17.6.4 and 17.9.3 (Loeb Classical Library, trans. Ralph Marcus and Allen Wikgren, Cambridge, MA: Harvard University Press), on the lunar eclipse and death before Passover; Emil Schurer, The History of the Jewish People in the Age of Jesus Christ (175 B.C.-A.D. 135), rev. ed. Geza Vermes, Fergus Millar, and Matthew Black, vol. 1 (Edinburgh: T&T Clark, 1973), for the dominant chronology; W. E. Filmer, `"The Chronology of the Reign of Herod the Great,`" Journal of Theological Studies n.s. 17 (1966): 283-298, and Andrew E. Steinmann and Rodger C. Young, `"Dating the Death of Herod and the Reigns of His Sons,`" Bibliotheca Sacra 178 (October-December 2021): 436-454, for the minority redating. Josephus places Herod's death shortly after a lunar eclipse and before that year's Passover. The long-dominant reconstruction, following Schurer, identifies the eclipse as the one visible over Judea on 13 March 4 BCE and dates Herod's death to early 4 BCE; Filmer, followed more recently by Steinmann and Young, argues Josephus's description fits a far more visible eclipse on 29 December 1 BCE better, placing Herod's death three years later than the conventional date. Either figure fixes the same structural point: a birth placed `"in the days of Herod the king`" (Matthew 2:1) must fall before Herod's own death, and so before the era later numbered from that birth even begins." }
}

# ---- Chapter beats (with [[NOTE:slug]] placeholders; [11] below is an already-resolved
# cross-reference to this chapter's existing almah/parthenos note, written as a literal
# number since it was assigned by an earlier pass and is not a placeholder here) ----
$beat1 = @'
Matthew closes the family tree with his own scorecard: "all the generations from Abraham to David are fourteen generations, and from David to the deportation to Babylon fourteen generations, and from the deportation to Babylon to the Messiah fourteen generations" (1:17). It is worth going back once more to where that structure comes from, and where it strains.

Start with the strain. Verse 8 moves in a single breath from "Jehoshaphat the father of Joram, and Joram the father of Uzziah" — but the parallel record of the same royal line, 1 Chronicles 3:11-12, lists three more kings standing between them: Ahaziah, Joash, and Amaziah. Matthew does not misremember these three so much as delete them outright, a compression scholars read as deliberate pruning to hold the first fourteen-count intact rather than a simple error [[NOTE:genealogy-omitted-kings-chronicles]]. It is the same move, in miniature, that this chapter's own text already flagged for the run of names between Zerubbabel and Joseph — names present in Matthew that do not match 1 Chronicles's own post-exilic list. Selective compression, not faithful transcription, is what a "fourteen generations" scorecard requires once the names being counted are kings who ruled for decades apiece against a fixed target number.

And the target number itself is not arbitrary. Hebrew, like Greek, had no separate system of numerals in this period; its letters did double duty as numbers, so any name could also be added up as a sum. David's name in Hebrew consonants — dalet-vav-dalet — totals four, plus six, plus four: fourteen [[NOTE:genealogy-gematria-david]]. Matthew's three sets of fourteen are not a neutral head-count of generations; they are a pun on the one name in the whole list the evidence actually corroborates, spelled out in arithmetic three times over. The honest complication is worth repeating once more with the citation attached: recount the names Matthew himself gives for the third set (1:12-16) and there are only thirteen, not fourteen, unless Jechoniah is counted twice — once as the deported king in verse 11, once again as the first name of the third set in verse 12. Proposed fixes for the shortfall go back to antiquity and number well over a dozen; none is required by the text itself, and Matthew never flags the double-count.
'@

$beat2 = @'
Matthew is not the only Gospel with a genealogy, and it is worth setting his ledger against Luke's, because the two do not tell the same story about Joseph's ancestors.

Luke's version (3:23-38) runs the family tree backward, from Jesus all the way to Adam, rather than forward from Abraham, and structurally the two lists overlap at only two names between David and Joseph: Shealtiel and Zerubbabel [[NOTE:luke-3-genealogy-divergence]]. Everywhere else in that stretch the names simply do not match, including the one that matters most for identifying Joseph specifically — Matthew names his father Jacob (1:16); Luke names him Heli (3:23). The two Gospels also part ways on which of David's sons carries the royal line forward to that point: Matthew goes through Solomon, the throne-holding son; Luke goes through Nathan, a different son of David and Bathsheba named at 2 Samuel 5:14 who never reigned at all.

This is not a modern discovery. A third-century Christian chronicler, Julius Africanus, already knew about it and proposed a levirate-marriage solution, preserved secondhand in Eusebius's Ecclesiastical History, under which Heli and Jacob were half-brothers by the same mother, and Jacob married his childless half-brother's widow to raise up an heir in Heli's name — which is why Joseph could be called son of both. A more recent, more popular proposal reads Luke's list as Mary's genealogy rather than Joseph's, sidestepping the conflict by assigning each Gospel a different parent. Luke's own Greek text, however, names Joseph as the one "the son (as was thought) of Heli" (3:23), not Mary; the theory has to import an assumption the text itself does not state. Mainstream historical-critical scholarship generally reads the two genealogies as independent theological compositions, each building its own case for who Jesus is through whom it chooses to name, rather than as two halves of one reconcilable family record.
'@

$beat3 = @'
One more piece of evidence belongs alongside this chapter's discussion of the almah/parthenos translation question [11], because it settles a narrower, adjacent question that sometimes gets folded into the same debate: is the underlying Hebrew word itself reliable, or could it have been altered later, after Christian claims about a virgin birth made the original wording inconvenient?

That answer is dateable. In 1947, a Bedouin shepherd's discovery in a cave above the Dead Sea turned up the Great Isaiah Scroll, the oldest complete copy of any book of the Hebrew Bible ever recovered, paleographically dated to roughly the second century BCE — on the order of a hundred years or more before Jesus was born [[NOTE:great-isaiah-scroll-almah]]. At the exact verse in question, the scroll reads ha-almah — the same word, spelled the same way, that the much later medieval Masoretic tradition also preserves. There was no possibility of a post-Christian scribal change here: this copy already existed before the debate it might have been altered to influence.

What that confirms is narrow but real — the Hebrew wording is stable and predates any Christian stake in the outcome. What it does not touch is the separate question this chapter already covers: that Matthew is quoting the Greek Septuagint's rendering of that Hebrew word as parthenos, "virgin," a translator's choice made centuries before Matthew wrote, applied to a sign originally given to King Ahaz about a child expected in his own lifetime [11]. Two different questions, two different pieces of evidence, both settled — and neither one answering the theological question of whether the pregnancy Matthew describes happened by the means he says it did.
'@

$beat4 = @'
This chapter's naming scene is also this book's on-ramp into a chronological problem the very next chapter cannot avoid, because Matthew 2:1 opens with "in the days of Herod the king" — and Herod's own clock is worth setting before that name arrives.

Herod died, as the historian Josephus records it, sometime after a lunar eclipse and before that year's Passover. Working from that description, the long-dominant scholarly reconstruction identifies the eclipse as the one visible over Judea on 13 March, 4 BCE, and dates Herod's death to early 4 BCE [[NOTE:herod-death-chronology-josephus]]. A minority of scholars argue Josephus's description fits a different, far more visible lunar eclipse on 29 December, 1 BCE, better, which would place Herod's death three years later than the conventional date.

Either figure creates the same structural fact: a birth of Jesus placed "in the days of Herod the king" has to fall before Herod's own death — before an era that would eventually be numbered from that very birth even begins. That is Matthew's chronology working exactly as this genealogy-and-naming chapter sets it up: a real king, with a real, independently documented death date, anchoring a story that unfolds entirely within his reign. It also opens a separate synchronization problem on Luke's own side of the same birth, where Luke ties it to a differently dated Roman census — a discrepancy this project's treatment of Luke's own nativity chapter addresses in full. What belongs here is narrower: simply that Herod's own dates are a fixed point against which any account of this birth, Matthew's included, has to be checked.
'@

$beats = @($beat1, $beat2, $beat3, $beat4)

# ---- Glossary additions (heading -> body) ----
$glossary = [ordered]@{
'GEMATRIA (NUMERICAL VALUE OF NAMES)' = "The Hebrew and Greek practice of computing a numerical value for a word or name from its own letters, each of which also served as a numeral in a period before either language had a separate number-writing system. Matthew's genealogy (1:1-17) organizes itself into three sets of exactly fourteen generations, and fourteen is the sum of the Hebrew consonants in David's own name — dalet-vav-dalet, four plus six plus four [[NOTE:genealogy-gematria-david]]. Recounting Matthew's own third set of names (1:12-16) yields only thirteen entries unless one name is counted twice, a shortfall long noted and never resolved by the text itself."
'SEPTUAGINT (GREEK OLD TESTAMENT)' = "The pre-Christian Greek translation of the Hebrew scriptures, produced in stages from roughly the 3rd to the 2nd century BCE, chiefly at Alexandria. New Testament authors writing in Greek, including Matthew, generally quote and rely on this translation rather than working from the Hebrew text directly; the clearest example in this chapter is the fulfillment-quotation of Isaiah 7:14 (1:22-23), where Matthew follows the Septuagint's parthenos ('virgin') rather than the Hebrew almah ('young woman') that Isaiah himself wrote [11]. A Hebrew manuscript predating both the Septuagint's wide circulation among Christians and the New Testament itself confirms the underlying Hebrew wording was already stable centuries earlier [[NOTE:great-isaiah-scroll-almah]]."
'GREAT ISAIAH SCROLL (1QISA^A)' = "The oldest complete surviving copy of the biblical Book of Isaiah, one of the original Dead Sea Scrolls recovered from Qumran Cave 1 in 1947 and paleographically dated to roughly the 2nd century BCE. At Isaiah 7:14 it reads the Hebrew ha-almah, identical to the wording later preserved in the medieval Masoretic Text, confirming that reading was already fixed more than a century before Jesus's birth and could not have been altered afterward in reaction to Christian claims about a virgin birth [[NOTE:great-isaiah-scroll-almah]]. Housed today in the Shrine of the Book, Israel Museum, Jerusalem."
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

# ---- Insert chapter beats with placeholder replacement, appended after existing content ----
$sortKey = if ($maxCh1SortKey -ge 1000) { [math]::Ceiling($maxCh1SortKey / 100.0) * 100 } else { 900.0 }
foreach ($beatText in $beats) {
    $resolved = $beatText
    foreach ($slug in $slugToNumber.Keys) {
        $resolved = $resolved.Replace("[[NOTE:$slug]]", "[$($slugToNumber[$slug])]")
    }
    $sortKey += 100
    $id = New-BeatRow $resolved
    Add-BeatNode $Ch1NodeId $id $sortKey
}
Write-Host "Inserted $($beats.Count) chapter beats (supplementary, appended after existing content)"

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
Seed-Entity "Great Isaiah Scroll (1QIsa^a)" "great-isaiah-scroll-1qisaa" "document" "Oldest complete surviving copy of the Book of Isaiah, one of the original Dead Sea Scrolls found at Qumran Cave 1 in 1947, paleographically dated to roughly the 2nd century BCE; preserves the Hebrew almah reading at Isaiah 7:14 more than a century before the New Testament."
Seed-Entity "Septuagint" "septuagint" "document" "The pre-Christian Greek translation of the Hebrew scriptures, produced in stages from the 3rd to 2nd century BCE, chiefly in Alexandria; Matthew's fulfillment-quotation of Isaiah 7:14 follows this translation's parthenos rendering rather than the Hebrew almah."
Seed-Entity "Gematria" "gematria" "vocabulary" "The practice of computing the numerical value of a word or name from its Hebrew letters, each of which also functions as a numeral; the basis of Matthew's three sets of fourteen generations (1:17), since David's name totals fourteen."

$conn.Close()
Write-Host "DONE Matthew Chapter 1 depth pass."
