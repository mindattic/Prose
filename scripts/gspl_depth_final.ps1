$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$em = [char]8212

function Sha256Hex([string]$t) {
    $s = [System.Security.Cryptography.SHA256]::Create()
    return ([System.BitConverter]::ToString($s.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-','').ToLower()
}
function Exec-NonQuery([string]$sql, [hashtable]$p) {
    $c = $conn.CreateCommand(); $c.CommandText = $sql
    foreach ($k in $p.Keys) { $c.Parameters.AddWithValue("@$k", $p[$k]) | Out-Null }
    $c.ExecuteNonQuery() | Out-Null
}
function Exec-Scalar([string]$sql) { $c = $conn.CreateCommand(); $c.CommandText = $sql; return $c.ExecuteScalar() }

$MtNotes = "019FA01D-FA22-76C6-976C-3EA4F4D54A14"
$MkNotes = "019FA968-1B3B-75DC-84CF-0C7D9C4E783C"
$script:MaxNumber = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats")

function Next-NoteNumber([string]$notes) {
    return [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(b.Text, CHARINDEX(' ',b.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId
WHERE bn.NodeId='$notes' AND bn.IsEnabled=1
  AND CHARINDEX(' ', b.Text) > 1 AND LEFT(b.Text, CHARINDEX(' ',b.Text)-1) NOT LIKE '%[^0-9]%'
"@)
}
function Add-Note([string]$notesNode, [int]$num, [string]$title, [string]$body) {
    $sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId='$notesNode'")
    $script:MaxNumber++
    $text = "$num $em $title" + "`n`n" + $body.Trim()
    $id = [guid]::NewGuid()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $id; Text = $text; Hash = (Sha256Hex $text); Number = $script:MaxNumber }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@NodeId, @BeatId, @SortKey, 1)" @{ NodeId = [guid]$notesNode; BeatId = $id; SortKey = $sk }
    Write-Host "  note $num added"
}
function Append-ToLastBeatBefore([string]$nodeId, [string]$prefix, [string]$extra) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT TOP 1 b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1 AND b.Text LIKE @P ORDER BY bn.SortKey"
    $c.Parameters.AddWithValue("@N", [guid]$nodeId) | Out-Null
    $c.Parameters.AddWithValue("@P", "$prefix%") | Out-Null
    $r = $c.ExecuteReader()
    if (-not $r.Read()) { $r.Close(); throw "not found: $prefix" }
    $id = $r.GetGuid(0); $cur = $r.GetString(1); $r.Close()
    $new = $cur.TrimEnd() + "`n`n" + $extra.Trim()
    Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $new; H = (Sha256Hex $new); Id = $id }
    Write-Host "  appended to: $prefix"
}

# ============ MATTHEW ============
$n = Next-NoteNumber $MtNotes
$nGematria = $n + 1
Add-Note $MtNotes $nGematria "Why fourteen: David's own name adds up to it" @"
Matthew 1:17, read with the standard interpretive method known as gematria, in which each Hebrew letter carries a numerical value. The Hebrew consonants of David's name $em dalet, vav, dalet $em total four plus six plus four, that is, fourteen. David is also the fourteenth name in Matthew's own list. The proposal that Matthew's three sets of fourteen encode David's name numerically goes back to nineteenth-century German scholarship and was taken up through the twentieth century by G. H. Box (1905), Joachim Jeremias (1961), and A. W. Argyle (1963) among others; it is now the most widely cited explanation for the structure, though it remains an inference from the arithmetic rather than a claim Matthew makes in his own words.
"@
$nBaptism = $nGematria + 1
Add-Note $MtNotes $nBaptism "Acts never records anyone baptised with Matthew's formula" @"
Matthew 28:19, compared with Acts 2:38, 8:16, 10:48, and 19:5. Matthew's commission directs baptism "in the name of the Father and of the Son and of the Holy Spirit." Every baptism actually narrated in Acts is performed in the name of Jesus alone, and Acts 19:5 has Paul re-baptise believers specifically "in the name of the Lord Jesus." The observation is textual and internal to the New Testament: the divergence between the commission's threefold wording and the practice Acts describes is plain on the face of both books, and is read variously as a difference between liturgical formula and shorthand description, as evidence of developing practice, or as a sign that Matthew's wording reflects the usage of his own community.
"@

Append-ToLastBeatBefore "019FA049-5D94-766F-A919-4623FD605028" "Matthew closes the family tree with his own scorecard" @"
There remains the question the scorecard itself provokes: why fourteen? The most durable answer is arithmetical, and it depends on a standard ancient interpretive habit rather than on anything hidden. Hebrew letters carry numerical values, and the consonants of David's name $em dalet, vav, dalet $em total four, six, and four: fourteen [$nGematria]. David is also, as it happens, the fourteenth name Matthew lists.

On this reading the genealogy is not counting generations so much as spelling a name three times over in the shape of the list, which would explain both the insistence on the number and the willingness to drop kings to reach it. The proposal has been in circulation since the nineteenth century and has been endorsed by a long line of scholars, and it is worth stating its status precisely: it is an inference from the arithmetic, not something Matthew ever says. He gives the count and lets the reader notice. What can be said with confidence is that a first-century Jewish reader was far better equipped to notice than a modern one, because letter-arithmetic was a live and familiar way of reading, not a curiosity.
"@

Append-ToLastBeatBefore "019FA078-392B-7408-B4A9-4CA5E15931F7" "The Great Commission's baptismal instruction" @"
One internal comparison belongs alongside that discussion, because it is checkable without leaving the New Testament. The commission's wording is threefold $em Father, Son, and Holy Spirit $em and yet not one baptism narrated in Acts uses it. Peter's instruction at Pentecost is to be baptised in the name of Jesus Christ; the Samaritan converts are baptised in the name of the Lord Jesus; so are Cornelius's household; and at Ephesus Paul re-baptises a group specifically in the name of the Lord Jesus [$nBaptism].

The gap is a plain textual fact, and it admits several honest readings. It may be that Acts is describing baptisms by their distinguishing feature rather than reciting a liturgy, much as one might say a couple were married in church without listing the vows. It may reflect a practice that developed toward the fuller form over decades. It may indicate that Matthew's wording records the usage of his own community at the time he wrote. Nothing in either book settles it, and the honest position is to notice that the most familiar baptismal sentence in Christianity is attested in exactly one verse, and that the book narrating the earliest baptisms consistently reports something shorter.
"@

# ============ MARK ============
$m = Next-NoteNumber $MkNotes
$nIsaiah = $m + 1
Add-Note $MkNotes $nIsaiah "Mark's opening quotation is three passages, credited to one prophet" @"
Mark 1:2-3, against Exodus 23:20, Malachi 3:1, and Isaiah 40:3. Mark introduces the quotation as written "in Isaiah the prophet," but the composite runs together the sending of a messenger before the traveller (Exodus 23:20 and Malachi 3:1) with the voice crying in the wilderness (Isaiah 40:3). Only the second half is Isaiah. The practice of welding proof-texts into a single chain is well documented in Jewish and early Christian writing and is usually discussed as a testimonia chain, with the attribution going to the most authoritative source in the group. The manuscript record preserves the discomfort: a substantial body of witnesses reads "in the prophets," plural, rather than "in Isaiah the prophet," and the plural is generally judged the later, smoothing correction rather than the original.
"@
$nChests = $nIsaiah + 1
Add-Note $MkNotes $nChests "The treasury chests were shaped to defeat thieves" @"
Mishnah, Shekalim 6:5. The tractate describes thirteen collection receptacles in the Temple, each labelled for its purpose $em shekels of the current year, shekels of the previous year, the offering of two doves, and so on. Each was shaped like a shofar, a ram's horn: narrow at the mouth and wide at the base, with the tapered end upward. The rabbinic explanation of the design is explicitly anti-theft $em the narrow opening prevented a person from reaching a hand inside and withdrawing coins while appearing to deposit them. The detail bears directly on Mark 12:41-44, since it establishes what the scene assumes: a public, audible act of giving, into fixed vessels, in a space where onlookers could see and hear what each donor contributed.
"@

Append-ToLastBeatBefore "019FA966-FCDC-70EC-B729-D891E6C094DE" "Mark doesn't open with a birth" @"
Before any of that, the Gospel's second sentence contains a small problem worth naming, because it is exactly the kind of thing this book exists to point at. Mark introduces his opening quotation as written "in Isaiah the prophet," and then quotes three passages at once: the messenger sent ahead of the traveller, which comes from Exodus and Malachi, and the voice crying in the wilderness, which is the only part that is actually Isaiah [$nIsaiah].

This is not carelessness so much as convention. Welding proof-texts into a single chain was ordinary practice, and the chain took the name of the weightiest source in it $em a citation habit closer to modern paraphrase-with-attribution than to a footnote. What makes it interesting is the manuscript trail, which records later readers noticing. A substantial group of witnesses reads "in the prophets," plural, which tidily removes the difficulty, and which textual critics generally judge to be the correction rather than the original. Someone, early, spotted that the sentence named the wrong prophet and fixed it $em which tells us both that the difficulty is real and that it was visible from the beginning.
"@

Append-ToLastBeatBefore "019FA967-B4EF-7F1B-B44A-506365CDE94A" "### The Widow's Offering (12:41-44)" @"
The physical setting of this scene can be reconstructed with unusual precision, and the reconstruction sharpens it. Rabbinic tradition describes thirteen collection receptacles standing in the Temple, each labelled for a specific purpose, and each shaped like a ram's horn $em narrow at the top, widening below, tapered end upward. The stated reason for the shape is not aesthetic. A narrow mouth stops a person from putting a hand in and taking coins out while appearing to put them in [$nChests].

Two things follow for the story. First, the vessels were designed on the assumption that some worshippers would steal from the offering while performing generosity $em which is a remarkable thing to build into the furniture of a temple, and a useful corrective to any reading that treats this courtyard as a place of uncomplicated piety. Second, the scene's central observation was physically available to anyone standing nearby. Coins dropped into a horn-shaped bronze vessel are audible, and the amount is legible by sound and by the motion of the hand. Jesus is not described as reading hearts here. He is described as watching, in a space engineered so that giving could be seen and heard $em and noticing the one contribution that made almost no noise at all.
"@

$conn.Close()
Write-Host "FINAL DEPTH DONE"
