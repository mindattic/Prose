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
    $hash = $sha.ComputeHash($bytes)
    return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLower()
}

function Merge-Text([guid]$keepId, [string]$appendClause) {
    $c = $conn.CreateCommand()
    $c.CommandText = "SELECT Text FROM Beats WHERE Id=@Id"
    $c.Parameters.AddWithValue("@Id", $keepId) | Out-Null
    $existing = [string]$c.ExecuteScalar()
    $merged = $existing.TrimEnd() + " " + $appendClause
    $hash = Sha256Hex $merged
    $u = $conn.CreateCommand()
    $u.CommandText = "UPDATE Beats SET Text=@Text, TextHash=@Hash, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id"
    $u.Parameters.AddWithValue("@Text", $merged) | Out-Null
    $u.Parameters.AddWithValue("@Hash", $hash) | Out-Null
    $u.Parameters.AddWithValue("@Id", $keepId) | Out-Null
    $u.ExecuteNonQuery() | Out-Null
    Write-Host "  merged into $keepId"
}

$emdash = [char]8212

# 1. FEAST OF TABERNACLES (SUKKOT) -- keep 862B98D3 (ch7), delete E78A832F (ch8)
Merge-Text ([guid]"862B98D3-9A58-4826-BF25-B663143EFC06") "The festival's nightly Temple illumination ceremony in the Court of Women $emdash towering lit menorahs and dancing crowds, per the Mishnah $emdash forms the ritual backdrop for Jesus's later `"I am the light of the world`" declaration (8:12) [54] [55]."

# 2. FAREWELL DISCOURSE (TESTAMENT LITERATURE) -- keep 67BFF61A (ch14), delete 7D11CCBE (ch17)
Merge-Text ([guid]"67BFF61A-0AFD-4608-B816-8B50AEF1869A") "The discourse's closing High Priestly Prayer (John 17) itself follows the same genre convention, since ancient testament/farewell literature regularly closes with a blessing or prayer spoken by the departing figure $emdash Jacob's deathbed blessing in Genesis 49 again supplying scriptural precedent [261]."

# 3. PONTIUS PILATE -- keep 24E5AC6E (ch19, fuller), delete 60CF8BB0 (ch18)
Merge-Text ([guid]"24E5AC6E-6267-4E2E-A106-103C26454577") "Judea's Roman governors normally resided at Caesarea Maritima on the coast, coming to Jerusalem chiefly for festival crowds such as this one [287]."

# 4. MARY MAGDALENE -- keep A715E2C8 (ch20, fuller), delete 580EFAFF (ch19)
Merge-Text ([guid]"A715E2C8-A6B6-436C-AF3E-838A7BC10BA8") "She is also named among the women standing at the foot of the cross in the preceding chapter (19:25), her presence there attested independently across all four canonical Passion narratives."

Write-Host "Merges complete."
$conn.Close()
