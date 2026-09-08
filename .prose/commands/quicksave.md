# quicksave

Save a resumable portable handoff. Required fields are `Task` and `Next`; `Decisions` and `State`
are optional. The file is local session state and is not canon.

Implementation:

```powershell
powershell -File tools/prose-agent.ps1 save -Task "..." -Decisions "..." -State "..." -Next "..."
```
