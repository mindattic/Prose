@echo off
taskkill /F /IM Prose.Writer.exe 2>NUL
taskkill /F /IM Prose.Codex.exe 2>NUL
taskkill /F /IM Prose.Blazor.exe 2>NUL
start "Prose.Writer" dotnet run --project v3\Prose.Writer --launch-profile https
start "Prose.Codex" dotnet run --project v3\Prose.Codex --launch-profile https
timeout /t 6 /nobreak >NUL
start "" chrome "https://localhost:7200"
start "" chrome "https://localhost:7201"
