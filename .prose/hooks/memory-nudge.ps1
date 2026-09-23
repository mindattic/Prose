# PostToolUse (Write|Edit): when Claude writes into its private memory directory, remind it that
# canon belongs in the world, not in a private note (RFC 0015 section 8). A nudge, not a block:
# collaboration feedback legitimately lives in memory.
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
try {
    $raw = [Console]::In.ReadToEnd()
    if (-not $raw) { exit 0 }
    $in = $raw | ConvertFrom-Json
    $path = "$($in.tool_input.file_path)"
    if ($path -match '[\\/]\.claude[\\/]projects[\\/][^\\/]+[\\/]memory[\\/]') {
        $msg = 'Memory holds collaboration feedback only. If what you just wrote is canon (a character, place, object, event, or an author ruling), it belongs in the world: record_ruling (law/metric/incidental) or the entity record (set_character_fields / create_*), with the read-back pasted.'
        @{ hookSpecificOutput = @{ hookEventName = 'PostToolUse'; additionalContext = $msg } } | ConvertTo-Json -Compress
    }
} catch { }
exit 0
