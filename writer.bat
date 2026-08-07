@echo off
taskkill /F /IM Prose.Writer.exe 2>NUL
taskkill /F /IM Prose.Blazor.exe 2>NUL
start "Prose.Writer" dotnet run --project v3\Prose.Writer --launch-profile https
timeout /t 5 /nobreak >NUL
start "" chrome "https://localhost:7200"
