@echo off
taskkill /F /IM StreetSamurai.Writer.exe 2>NUL
taskkill /F /IM StreetSamurai.Blazor.exe 2>NUL
start "StreetSamurai.Writer" dotnet run --project v3\StreetSamurai.Writer --launch-profile https
timeout /t 5 /nobreak >NUL
start "" chrome "https://localhost:7200"
