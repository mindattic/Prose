<#
  SessionStart hook - keeps Prose.Mcp pre-built so its `--no-build` MCP registration
  (see v3/Prose.Mcp/README.md) always launches against fresh code instead of failing
  the stdio handshake on `dotnet run`'s own restore/build preamble polluting stdout.
  Incremental build - a few seconds when nothing changed. Never blocks session start:
  a build failure is reported (see below) but does not stop the session.

  2026-08-24 - this hook was silently failing on EVERY session and had been since 2026-08-23.
  A long-lived Prose.Mcp.exe (the MCP server Claude Code launched in an earlier session and
  keeps alive across /clear and across new sessions) holds bin\Release\net10.0\*.dll open, so
  the incremental build compiles into obj\ and then dies on MSB3021/MSB3027 "the file is being
  used by another process" trying to copy into bin\. Three separate things hid that:

    * `*> $null` redirected ALL streams - stdout AND stderr - to null, so the "logged to
      stderr" this comment used to promise never happened;
    * `dotnet build` returning a non-zero EXIT CODE is not a PowerShell terminating error, so
      the try/catch never fired either; and
    * the stale server keeps answering normally, so nothing downstream looks broken.

  The consequence is worse than a failed build: the MCP server goes on advertising the schema
  it was built with, so every tool PARAMETER added since that build is missing from the schema,
  gets stripped from the call by the client, and the tool still returns ok:true having ignored
  it. That is how `create_character(aliases:)` - shipped 2026-08-24 - silently did nothing.

  So: capture the output, check $LASTEXITCODE, and when the build fails, say so in
  additionalContext (which the model reads) as well as stderr (which the user sees), naming the
  stale-schema consequence and the fix, rather than failing quietly.
#>
$ErrorActionPreference = 'Continue'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$proj     = Join-Path $repoRoot 'v3\Prose.Mcp\Prose.Mcp.csproj'
$notice   = ''

if (Test-Path $proj) {
    $buildLog = & dotnet build $proj --configuration Release 2>&1
    if ($LASTEXITCODE -ne 0) {
        $locked = $buildLog | Select-String -Pattern 'MSB3021|MSB3027|being used by another process'
        $tail   = ($buildLog | Select-Object -Last 5) -join "`n"

        if ($locked) {
            # The specific, recurring case - name it precisely instead of dumping MSBuild noise.
            $holders = Get-Process -Name 'Prose.Mcp' -ErrorAction SilentlyContinue |
                       ForEach-Object { "$($_.ProcessName) (PID $($_.Id), started $($_.StartTime))" }
            $notice = @"
[build-prose-mcp] BUILD FAILED - Prose.Mcp output is LOCKED by the running MCP server.
Holder: $(if ($holders) { $holders -join '; ' } else { 'a Prose.Mcp process (not enumerable from this hook)' })

CONSEQUENCE: the MCP server is serving a STALE TOOL SCHEMA. Any tool parameter added to
Prose.Mcp since that process started is absent from the advertised schema, is stripped from
the call by the client, and the tool STILL RETURNS ok:true having ignored it. Do not trust an
ok:true from an MCP write until this is resolved - verify the write with a read-back, or use
the equivalent `prose --<flag>` CLI command, which runs in the Hub and is not affected.

FIX: exit Claude Code (which shuts the MCP server down), then start a new session so this hook
can rebuild against unlocked output.
"@
        } else {
            $notice = "[build-prose-mcp] BUILD FAILED (exit $LASTEXITCODE). MCP tools may be stale or unavailable.`n$tail"
        }

        Write-Error $notice
    }
}

# additionalContext puts the warning in front of the model too - stderr alone only reaches the
# user, and the model is the one about to trust an ok:true from a tool it cannot see is stale.
if ($notice) {
    @{ hookSpecificOutput = @{ hookEventName = 'SessionStart'; additionalContext = $notice } } |
        ConvertTo-Json -Depth 5 -Compress
} else {
    Write-Output '{}'
}
exit 0
