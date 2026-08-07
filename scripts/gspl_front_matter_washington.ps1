$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;")
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

$books = @(
  @{code='MATTHEW'; notes='019FA01D-FA22-76C6-976C-3EA4F4D54A14'}
  @{code='MARK';    notes='019FA968-1B3B-75DC-84CF-0C7D9C4E783C'}
  @{code='LUKE';    notes='019FA96B-18E2-7BB4-BAEB-11ACA24934F4'}
  @{code='JOHN';    notes='019FA96D-7D48-75E0-9BD9-2190171276DC'}
)

$noteBody = @"
Mount Vernon Ladies' Association, "Washington's Teeth" and "Washington and Teeth from Enslaved People," George Washington's Mount Vernon (Mount Vernon, Virginia), the institution holding the surviving dentures; with Washington's own Mount Vernon plantation ledger for May 1784.

Materials: the surviving denture set at Mount Vernon combines human teeth, cow and horse teeth, and hippopotamus ivory set with metal, including a lead-alloy base. Across the several sets made for him the documented material range also includes donkey teeth, walrus and possibly elephant ivory, and gold, tin, copper, and silver alloys. No set was made of wood.

The purchase: the May 1784 ledger records, in Washington's own accounts, "By Cash pd Negroes for 9 Teeth on Acct of Dr. Lemoire" $em slightly more than six pounds for nine teeth obtained from enslaved people at Mount Vernon by his dentist, Jean-Pierre Le Mayeur, against the two guineas per tooth Le Mayeur advertised publicly to free donors. Mount Vernon's own historians note that enslaved people could not meaningfully refuse such a transaction. Whether those nine specific teeth are among those in any surviving set cannot be established; Mount Vernon states that historians consider it likely the dentures contain teeth taken from enslaved people.

The myth: the wooden-teeth story does not appear in print until decades after Washington's death and circulated widely from the mid-nineteenth century. The standard explanation offered by dental historians is that the porous hippopotamus ivory absorbed stains from food and wine and darkened and cracked over time, taking on a grain that later observers read as wood.
"@

$oldPattern = "A word about why that context goes missing\..*?Recovering them is most of what this book does\."

foreach ($b in $books) {
    # next note number for this book
    $n = [int](Exec-Scalar @"
SELECT ISNULL(MAX(CAST(LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) AS INT)),0)
FROM BeatNodes bn JOIN Beats bt ON bt.Id=bn.BeatId
WHERE bn.NodeId='$($b.notes)' AND bn.IsEnabled=1
  AND CHARINDEX(' ', bt.Text) > 1 AND LEFT(bt.Text, CHARINDEX(' ',bt.Text)-1) NOT LIKE '%[^0-9]%'
"@) + 1

    # locate this book's front matter beat
    $c = $conn.CreateCommand()
    $c.CommandText = @"
SELECT bt.Id, bt.Text FROM Nodes c
JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id
JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode=@Code AND c.Title='How to Read This Book' AND bn.IsEnabled=1
"@
    $c.Parameters.AddWithValue("@Code", $b.code) | Out-Null
    $r = $c.ExecuteReader()
    if (-not $r.Read()) { $r.Close(); Write-Host "  $($b.code): front matter not found"; continue }
    $beatId = $r.GetGuid(0); $cur = $r.GetString(1); $r.Close()

    if ($cur.Contains("hippopotamus")) { Write-Host "  $($b.code): already expanded, skip"; continue }
    if (-not [regex]::IsMatch($cur, $oldPattern, 'Singleline')) { Write-Host "  $($b.code): !! old paragraph not matched"; continue }

    $newBlock = @"
A word about why that context goes missing, because the standard example is this whole method in miniature.

Most people know George Washington had false teeth, and most people have been told they were wooden. They were not. His dentures were assembled from hippopotamus ivory, from cow and horse and donkey teeth, and from human teeth $em set, in the pair that survives at Mount Vernon, into a base of lead. The human teeth are the part that matters. A ledger entry in Washington's own Mount Vernon accounts for May 1784 records a payment of a little over six pounds "pd Negroes for 9 Teeth" on his dentist's account, against the two guineas a tooth that same dentist advertised to free sellers $em paid to people who were in no position to refuse. Whether those nine teeth are among the ones now on display cannot be proved; Mount Vernon's own historians consider it likely that the dentures contain teeth taken from enslaved people [$n].

None of this is a suppressed secret. It is in his account books, published, in his own hand. But it makes an unbearable sentence about the founder of a country, so the retelling reached for something softer, and "wooden teeth" is what everyone got instead $em a phrase that appears in print only decades after his death, in a century with an appetite for burnishing its founders. The likeliest explanation for the myth is duller and sadder than either version: hippopotamus ivory is porous, it drank up his food and his Madeira, and it darkened and cracked until the grain looked like wood.

That is the difference between history and heritage, and it is worth being clear that heritage is not lying. Heritage is what remains after the details that do not flatter have quietly stopped being repeated $em warmer, simpler, easier to hand to a child, and less true every time it is passed on. Scripture has accumulated a great many facts of that kind. Not scandals, mostly. Just details that complicated a good story and so fell out of the telling. Recovering them is most of what this book does.
"@

    $new = [regex]::Replace($cur, $oldPattern, { $newBlock.Trim() }, 'Singleline')
    $new = [regex]::Replace($new, "(?<!`n)`n(?!`n)", ("`n" + "`n"))
    $new = [regex]::Replace($new, "`n{3,}", ("`n" + "`n")).Trim()

    Exec-NonQuery "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id" @{ T = $new; H = (Sha256Hex $new); Id = $beatId }

    # add the note
    $sk = [double](Exec-Scalar "SELECT ISNULL(MAX(bn.SortKey),0)+50 FROM BeatNodes bn WHERE bn.NodeId='$($b.notes)'")
    $bnum = [int](Exec-Scalar "SELECT MAX(Number) FROM Beats") + 1
    $nid = [guid]::NewGuid()
    $ntext = "$n $em Washington's teeth: what the records actually say" + "`n`n" + $noteBody.Trim()
    Exec-NonQuery "INSERT INTO Beats (Id, Text, TextHash, Act, SceneType, Kind, Number, Stale, WasCorrected, IsChapterStart, Version, EntityStale, CreatedAt, UpdatedAt) VALUES (@Id, @Text, @Hash, 0, 'scene', 'prose', @Number, 0, 0, 0, 0, 0, SYSUTCDATETIME(), SYSUTCDATETIME())" @{ Id = $nid; Text = $ntext; Hash = (Sha256Hex $ntext); Number = $bnum }
    Exec-NonQuery "INSERT INTO BeatNodes (NodeId, BeatId, SortKey, IsEnabled) VALUES (@N, @B, @S, 1)" @{ N = [guid]$b.notes; B = $nid; S = $sk }

    Write-Host "  $($b.code): front matter expanded, note $n added ($($new.Length) chars)"
}

$conn.Close()
Write-Host "DONE"
