@echo off
taskkill /F /IM Prose.Codex.exe 2>NUL
taskkill /F /IM Prose.Blazor.exe 2>NUL
start "Prose.Codex" dotnet run --project v3\Prose.Codex --launch-profile https
timeout /t 5 /nobreak >NUL
start "" chrome "https://localhost:7201"
