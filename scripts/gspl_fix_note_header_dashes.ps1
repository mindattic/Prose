$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=Prose;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
$em = [char]8212
function Sha256Hex([string]$t){$s=[System.Security.Cryptography.SHA256]::Create();([System.BitConverter]::ToString($s.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-','').ToLower()}
$cmd=$conn.CreateCommand()
$cmd.CommandText=@"
SELECT p.NodeCode, bt.Id, bt.Text
FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId
JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId
WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN') AND c.Title='Notes' AND bn.IsEnabled=1
"@
$r=$cmd.ExecuteReader(); $rows=@()
while($r.Read()){$rows+=[pscustomobject]@{Book=$r.GetString(0);Id=$r.GetGuid(1);Text=$r.GetString(2)}}
$r.Close()
$per=@{}; $n=0
foreach($x in $rows){
  # only the header line, only the first " - " right after the leading note number
  $new = [regex]::Replace($x.Text, '^(\d+) - ', "`$1 $em ")
  if($new -ne $x.Text){
    $c=$conn.CreateCommand()
    $c.CommandText="UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
    $c.Parameters.AddWithValue("@T",$new)|Out-Null
    $c.Parameters.AddWithValue("@H",(Sha256Hex $new))|Out-Null
    $c.Parameters.AddWithValue("@Id",$x.Id)|Out-Null
    $c.ExecuteNonQuery()|Out-Null
    $n++; if(-not $per.ContainsKey($x.Book)){$per[$x.Book]=0}; $per[$x.Book]++
  }
}
$conn.Close()
foreach($k in $per.Keys){Write-Host ("  {0}: {1} headers normalised" -f $k,$per[$k])}
Write-Host "total: $n"
