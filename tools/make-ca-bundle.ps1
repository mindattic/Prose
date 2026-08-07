# Export the Windows trusted-root + intermediate CA store to a PEM bundle, then
# append certifi's public bundle. On TLS-intercepting corporate networks this lets
# every Python process (pip, subprocess pip, httpx, requests, urllib) verify against
# the same CAs the OS/browser already trusts — no verification disabling.
param([string]$Python = "python")
$ErrorActionPreference = 'Stop'
$out = "D:\Projects\MindAttic\Prose\tools\corp-ca-bundle.pem"

$stores = @('Cert:\LocalMachine\Root','Cert:\LocalMachine\CA','Cert:\CurrentUser\Root','Cert:\CurrentUser\CA')
$seen = @{}
$blocks = foreach ($s in $stores) {
    Get-ChildItem $s -ErrorAction SilentlyContinue | ForEach-Object {
        if (-not $seen.ContainsKey($_.Thumbprint)) {
            $seen[$_.Thumbprint] = $true
            $b64 = [Convert]::ToBase64String($_.RawData, 'InsertLineBreaks')
            "# $($_.Subject)`n-----BEGIN CERTIFICATE-----`n$b64`n-----END CERTIFICATE-----"
        }
    }
}
Set-Content -Path $out -Value ($blocks -join "`n") -Encoding ascii
Write-Host "wrote $($seen.Count) OS certs to $out"

# append certifi's bundle so public CAs keep working
try {
    $certifi = (& $Python -c "import certifi,sys; sys.stdout.write(certifi.where())").Trim()
    if ($certifi -and (Test-Path $certifi)) {
        Add-Content -Path $out -Value "`n# --- certifi public bundle ---"
        Add-Content -Path $out -Value (Get-Content $certifi -Raw)
        Write-Host "appended certifi bundle from $certifi"
    }
} catch { Write-Host "certifi append skipped: $_" }

Write-Host "BUNDLE: $out  ($([math]::Round((Get-Item $out).Length/1kb)) KB)"
