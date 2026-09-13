@echo off
rem Desktop entry point: republish Prose Writer from source, then open it.
rem
rem Lives in the repo, not in C:\Apps\MindAttic\Prose\, because that folder is a build output that
rem can be deleted and regenerated at any time - a desktop icon pointing into it breaks the first
rem time someone clears it. The repo is the thing that survives.
rem
rem Writer rebuilds the HUB too. That is not belt-and-braces: the entire editor is Prose.WriterUi
rem compiled INTO Hub.exe, and Writer.exe is only a WebView2 window pointed at it. Publishing
rem Writer alone would leave you editing in yesterday's UI believing you had just updated it.
title Deploy Prose Writer
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0v3\tools\deploy-apps.ps1" -Apps Hub,Writer -Start Writer
if errorlevel 1 (
    echo.
    echo   DEPLOY FAILED -- see the output above. Nothing was launched.
    pause
    exit /b 1
)
