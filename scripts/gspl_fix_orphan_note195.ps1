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
    return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLower()
}

$Ch2 = "019FA967-0D77-73B8-A0B4-BA4423DF5219"
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 1 b.Id, b.Text FROM BeatNodes bn JOIN Beats b ON b.Id=bn.BeatId WHERE bn.NodeId=@N AND bn.IsEnabled=1 AND b.Text LIKE @P ORDER BY bn.SortKey"
$cmd.Parameters.AddWithValue("@N", [guid]$Ch2) | Out-Null
$cmd.Parameters.AddWithValue("@P", "Someone asks Jesus why John's disciples%") | Out-Null
$rdr = $cmd.ExecuteReader(); $rdr.Read() | Out-Null
$id = $rdr.GetGuid(0); $cur = $rdr.GetString(1); $rdr.Close()

$extra = @"
It is worth being precise about what was and was not required, because the question assumes a distinction the English flattens. The Torah imposes exactly one annual fast, the Day of Atonement. Everything else in view here was voluntary supererogation: Luke's Pharisee announces that he fasts twice a week, and the Didache, a Christian manual from the late first or early second century, names the same two days while pointedly steering its own readers off them $em "let not your fasts be with the hypocrites, for they fast on the second and fifth day of the week" $em recommending the fourth day and Friday instead. Counted from the Sabbath, the second and fifth days are Monday and Thursday [195].

Two things follow. The practice Jesus's questioners treat as the obvious baseline was pious custom rather than commanded law, which is why the answer can decline it without touching the Torah at all. And the Didache shows the two communities differentiating themselves by calendar within a generation or two $em choosing different days for the same discipline, which is a remarkably efficient way to make a group visible without changing a single doctrine.
"@

$new = $cur.TrimEnd() + "`n`n" + $extra.Trim()
$c2 = $conn.CreateCommand()
$c2.CommandText = "UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
$c2.Parameters.AddWithValue("@T", $new) | Out-Null
$c2.Parameters.AddWithValue("@H", (Sha256Hex $new)) | Out-Null
$c2.Parameters.AddWithValue("@Id", $id) | Out-Null
$c2.ExecuteNonQuery() | Out-Null
$conn.Close()
Write-Host "note 195 now referenced in MARK ch2 fasting beat"
