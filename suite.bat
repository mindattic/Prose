@echo off
taskkill /F /IM StreetSamurai.Writer.exe 2>NUL
taskkill /F /IM StreetSamurai.Codex.exe 2>NUL
taskkill /F /IM StreetSamurai.Blazor.exe 2>NUL
start "StreetSamurai.Writer" dotnet run --project v3\StreetSamurai.Writer --launch-profile https
start "StreetSamurai.Codex" dotnet run --project v3\StreetSamurai.Codex --launch-profile https
timeout /t 6 /nobreak >NUL
start "" chrome "https://localhost:7200"
start "" chrome "https://localhost:7201"
