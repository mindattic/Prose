$ErrorActionPreference = 'Stop'   # fail loudly: a SqlException is otherwise non-terminating
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=StreetSamurai;Trusted_Connection=True;TrustServerCertificate=True;")
$conn.Open()
function Sha256Hex([string]$t){$s=[System.Security.Cryptography.SHA256]::Create();([System.BitConverter]::ToString($s.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($t.Trim()))) -replace '-','').ToLower()}
$cmd=$conn.CreateCommand()
$cmd.CommandText="SELECT bt.Id, bt.Text FROM Nodes c JOIN Nodes p ON p.Id=c.ParentNodeId JOIN BeatNodes bn ON bn.NodeId=c.Id JOIN Beats bt ON bt.Id=bn.BeatId WHERE p.NodeCode IN ('MATTHEW','MARK','LUKE','JOHN') AND bn.IsEnabled=1 AND bt.Text LIKE '% Cited in:%'"
$r=$cmd.ExecuteReader(); $rows=@()
while($r.Read()){$rows+=[pscustomobject]@{Id=$r.GetGuid(0);Text=$r.GetString(1)}}
$r.Close()
$n=0
foreach($x in $rows){
  $new = $x.Text -replace ' Cited in:', ("`n`n" + 'Cited in:')
  if($new -ne $x.Text){
    $c=$conn.CreateCommand()
    $c.CommandText="UPDATE Beats SET Text=@T, TextHash=@H, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
    $c.Parameters.AddWithValue("@T",$new)|Out-Null
    $c.Parameters.AddWithValue("@H",(Sha256Hex $new))|Out-Null
    $c.Parameters.AddWithValue("@Id",$x.Id)|Out-Null
    $c.ExecuteNonQuery()|Out-Null; $n++
  }
}
$conn.Close(); Write-Host "restored Cited in: paragraph break in $n notes"
