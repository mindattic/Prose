# Portable Prose commands

These command definitions are host-neutral. A slash-command host may expose them as `/do`,
`/quicksave`, or another alias, but the behavior lives in `tools/prose-agent.ps1` and must not be
reimplemented in a vendor-specific command folder.

Run from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/prose-agent.ps1 bootstrap -Json
powershell -NoProfile -ExecutionPolicy Bypass -File tools/prose-agent.ps1 do
```
