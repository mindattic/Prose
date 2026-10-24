# /run — alias for `/launch writer`

Opens the Prose Writer. This command is a **forwarder**: follow
[`.prose/commands/launch.md`](launch.md) and run it with `-App writer`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File src\tools\launch-app.ps1 -App writer
```

## Why this is now an alias

The previous version of this command was stale and would have failed. It described starting
`src/Prose.Writer` with `dotnet run --launch-profile http` and opening **http://localhost:5200** —
a Blazor web host with launch profiles.

`Prose.Writer` stopped being that on 2026-09-11. It is now a **WPF + WebView2 window** with no
`launch-profile`, no port of its own, and no web host: the entire editor is `Prose.WriterUi`
compiled into `Hub.exe` and served at `http://127.0.0.1:5900/writer`. Following the old runbook
would have killed the wrong process and then failed on an unknown `--launch-profile`.

Kept as an alias rather than deleted because `/run` is short and in the author's fingers.

## See also

`/launch` takes other targets — `hub`, `kdp`, `launcher`, and `wiki` (the entity browser at
`/repo`). `/launch` never publishes; use `/redeploy` when you want new code.
