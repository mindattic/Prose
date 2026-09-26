@echo off
rem Desktop entry point: republish Prose Writer from source, then open it.
rem
rem Lives in the repo, not in C:\Apps\MindAttic\Prose\, because that folder is a build output that
rem can be deleted and regenerated at any time - a desktop icon pointing into it breaks the first
rem time someone clears it. The repo is the thing that survives.
rem
rem Publishes Writer.exe ONLY and never stops or replaces the Hub. On opening, the Writer connects
rem to the Hub that is already running, or starts the deployed Hub.exe if none is.
rem
rem Know what this does and does not update. Writer.exe is the window: WebView2, the splash, the
rem Connect button, the microphone permission. The editor inside it - every Razor component, the
rem save path, the entity dialog - is Prose.WriterUi compiled into Hub.exe. A change to the editor
rem needs deploy-hub.bat. deploy-apps.ps1 says so after this runs when the Hub is older than the
rem editor's source, rather than letting you edit in yesterday's UI without knowing.
title Deploy Prose Writer
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\tools\deploy-apps.ps1" -Apps Writer -Start Writer
if errorlevel 1 (
    echo.
    echo   DEPLOY FAILED -- see the output above. Nothing was launched.
    pause
    exit /b 1
)
