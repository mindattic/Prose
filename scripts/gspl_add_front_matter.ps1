$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
$GSPL = [guid]"0197E9C9-0003-7000-8000-000000000003"

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

function Add-FrontMatter([string]$bookId, [string]$code, [string]$slugBase, [string]$body) {
    $exists = [int](Exec-Scalar "SELECT COUNT(*) FROM Nodes WHERE ParentNodeId='$bookId' AND Title='How to Read This Book'")
    if ($exists -gt 0) { Write-Host "  $code : already present, skip"; return }

    $nodeId = [guid]::NewGuid()
    $slug = "$slugBase-how-to-read"
    Exec-NonQuery @"
SET QUOTED_IDENTIFIER ON;
INSERT INTO Nodes (Id, Slug, Title, Kind, Status, SortKey, StartedAt, CharsNarrated, CreatedAt, UpdatedAt,
                   NarratedBeatCount, TotalBeatsToNarrate, IsCanon, Version, UniverseId, NodeType, ParentNodeId)
VALUES (@Id, @Slug, 'How to Read This Book', 'chapter', 'draft', 50.0, SYSUTCDATETIME(), 0, SYSUTCDATETIME(), SYSUTCDATETIME(),
        0, 0, 0, 0, @Uni, 'chapter', @Parent)
"@ @{ Id = $nodeId; Slug = $slug; Uni = $GSPL; Parent = [guid]$bookId }

    $beatNum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $beatId = [guid]::NewGuid()
    $text = $body.Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 1, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $beatId; Text = $text; Hash = (Sha256Hex $text); Number = $beatNum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, 100.0, 1)" @{ N = $nodeId; B = $beatId }
    Write-Host "  $code : front matter added ($($text.Length) chars)"
}

$common = @"
This book sets what scripture says beside what the independent historical and archaeological record says, and beside what the full range of serious scholarship says in between. It is not an attempt to prove the Gospel true, and it is not an attempt to debunk it. Both of those books have been written many times. This one is trying to do something narrower and, I think, more useful: to supply the context a modern reader needs in order to read the text accurately.

A word about why that context goes missing. Everyone knows George Washington had false teeth. Rather fewer people know what they were made of, because the answer is unflattering and so it quietly drops out of the retelling. Nothing is being covered up; a true and verifiable fact simply stops getting repeated, and after enough generations the polished version is the only one anybody has heard. Scripture has accumulated a great many facts of that kind $em not embarrassing revelations, just details that got smoothed away because they complicated a good story. Recovering them is most of what this book does.

Four rules govern every page.

**Nothing is asserted without a source.** Every factual claim carries a numbered note, resolved in the Notes chapter at the back, with the author, the title, the year, and the specific passage or page. Where a claim rests on an ancient writer $em Josephus, Pliny, Tacitus, the Mishnah $em the note says which book and which section, so you can go and look. Where I could not verify something, the text says so. An honestly flagged gap is worth more than a confident-sounding invention, and you will find gaps flagged throughout.

**Disagreement is reported, not resolved.** Where scholars genuinely disagree $em and on some of these questions they have disagreed for centuries $em you will find the actual competing positions, argued in their own terms, by name. Jewish and rabbinic scholarship, Christian confessional scholarship, the mainstream historical-critical academy, and the strictly archaeological position that accepts only what physical evidence attests. Where one of those positions is a fringe view, it is named as a fringe view. Where the field has no consensus, no consensus is manufactured.

**Theological questions are left alone.** Whether God acted in history is not a question evidence can reach, and this book does not pretend otherwise. It will tell you that a tomb was a real tomb, that a governor was a real governor, that a coin bore a particular inscription. It will not tell you what any of that means for your soul. That is not modesty; it is the boundary of the method. A reader of any faith, or none, should be able to finish this book trusting that what they read was accurate.

**The strange details are kept.** Ancient life was specific, and the specifics are the best part $em what a day's labour bought, what a roof was made of, why the collection boxes in the Temple were shaped the way they were. Where a fact is genuinely funny, I have let it be funny. Where it is grim, I have not softened it.

Each chapter closes with a short section called **Then and Now**, which asks the only question this material really raises: what has actually changed between that world and ours, and what has not. Some of the answers are cheerful. Others are less so.

The Notes chapter at the back carries every citation. The Glossary explains every person, place, and term the chapters name in passing $em so if you have ever wondered what a Sadducee was, or where Idumea is, the answer is there rather than clogging the sentence you were reading.
"@

# --- MATTHEW ---
Add-FrontMatter "019FA049-322F-75EF-AAB7-0C0DE8DBDB85" "MATTHEW" "matthew" ($common + @"

A word on this volume in particular. Matthew is the Gospel most interested in proving something. It opens with a genealogy engineered to a pattern, it quotes the Hebrew scriptures at every turn, and it is constantly telling you that a thing happened so that a prophecy would be fulfilled. That makes it unusually rewarding to check, because a book making arguments from documents can be compared against those documents $em and sometimes the quotation does not say what Matthew says it says. Those moments are not gotchas. They are the most direct evidence we have of how a first-century author actually used scripture, which turns out to be nothing like how a modern writer uses a footnote.
"@)

# --- MARK ---
Add-FrontMatter "019FA966-2F28-7A30-9662-F0F6F33C4D54" "MARK" "mark" ($common + @"

A word on this volume in particular. Mark is the shortest Gospel, almost certainly the earliest, and by some distance the roughest. It has no birth story, its Greek is plain to the point of clumsiness, and it ends $em in the oldest manuscripts we have $em in the middle of a scene, on a group of frightened women saying nothing to anybody. It also keeps details a tidier writer would have cut: a naked man running from an arrest, the colour of the grass on a hillside, a healing that does not work the first time. Those useless-looking fragments are exactly why historians find Mark so interesting, and this volume spends a good deal of time on them.
"@)

# --- LUKE ---
Add-FrontMatter "019FA969-3232-772B-998A-BB2D5158F96E" "LUKE" "luke" ($common + @"

A word on this volume in particular. Luke is the only Gospel that opens by telling you how it was made $em that others had written accounts, that the author investigated, and that this one is set down in order so its reader can judge its reliability. That is a promise no other Gospel makes, and it invites precisely the kind of checking this book does. Luke also dates things: a census, the fifteenth year of an emperor, a list of officeholders. Some of those datestamps hold up beautifully. One of them is among the hardest problems in the New Testament. Both are in here.
"@)

# --- JOHN ---
Add-FrontMatter "019FA96B-CAD8-7769-BF17-363E3641048E" "JOHN" "john" ($common + @"

A word on this volume in particular. John is the odd one out. It shares almost no material with the other three, it opens with a philosophical proposition rather than a story, and its Jesus speaks in long discourses instead of short sayings. For a long time that strangeness was taken as a sign that John was the least historical of the four. Then archaeologists began digging up the specific places it names $em a pool with five porticoes, a stone pavement, a village with six stone jars $em and found that whoever wrote this knew the city. That does not make its theology true or false. It does mean the old assumption was wrong, and this volume tries to say exactly what the spade can and cannot settle.
"@)

$conn.Close()
Write-Host "FRONT MATTER DONE"
