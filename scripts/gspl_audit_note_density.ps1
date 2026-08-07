$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data

$connStr = "Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$books = @(
    @{ Name = "Matthew"; BookId = "019FA049-322F-75EF-AAB7-0C0DE8DBDB85" },
    @{ Name = "Mark";    BookId = "019FA966-2F28-7A30-9662-F0F6F33C4D54" },
    @{ Name = "Luke";    BookId = "019FA969-3232-772B-998A-BB2D5158F96E" },
    @{ Name = "John";    BookId = "019FA96B-CAD8-7769-BF17-363E3641048E" }
)

foreach ($book in $books) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT n.Id, n.Title, n.SortKey FROM Nodes n WHERE n.ParentNodeId=@bid AND n.Title LIKE 'Chapter%' ORDER BY n.SortKey"
    $cmd.Parameters.AddWithValue("@bid", [guid]$book.BookId) | Out-Null
    $reader = $cmd.ExecuteReader()
    $chapters = @()
    while ($reader.Read()) { $chapters += [PSCustomObject]@{ Id = $reader.GetGuid(0); Title = $reader.GetString(1) } }
    $reader.Close()

    Write-Host "=== $($book.Name) ($($chapters.Count) chapters) ==="
    $totalRefs = 0
    foreach ($ch in $chapters) {
        $c2 = $conn.CreateCommand()
        $c2.CommandText = "SELECT b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@nid AND bn.IsEnabled=1"
        $c2.Parameters.AddWithValue("@nid", $ch.Id) | Out-Null
        $r2 = $c2.ExecuteReader()
        $allText = ""
        while ($r2.Read()) { $allText += $r2.GetString(0) + " " }
        $r2.Close()
        $matches = [regex]::Matches($allText, '\[\d+\]')
        $refCount = $matches.Count
        $totalRefs += $refCount
        $shortTitle = $ch.Title.Substring(0, [Math]::Min(70, $ch.Title.Length))
        Write-Host ("  {0,-72} refs={1}" -f $shortTitle, $refCount)
    }
    Write-Host "  TOTAL note-references in $($book.Name): $totalRefs  (avg $([math]::Round($totalRefs / $chapters.Count,1))/chapter)"
    Write-Host ""
}

$conn.Close()
