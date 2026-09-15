@echo off
rem Desktop entry point: republish the Prose Hub from source, then make sure ONE healthy Hub is up.
rem
rem The Hub is the resident server every CLI command and MCP tool forwards into, and it serves the
rem editor at /writer and the entity wiki at /repo. Starting it alone is what you want when you are
rem using the browser or the command line rather than the Writer window.
rem
rem After publishing, deploy-apps.ps1 checks http://127.0.0.1:5900/api/health first: a Hub that is
rem already running and healthy is connected to, never duplicated (two Hubs fight over port 5900).
rem Otherwise it starts the new build and waits — up to 90s, pending migrations apply on start —
rem until /api/health answers 200. If it never does, this icon reports DEPLOY FAILED rather than
rem claiming a Hub is up; re-running it reconnects once the Hub has come back.
rem
rem Lives in the repo, not in C:\Apps\MindAttic\Prose\: that folder is a build output that can be
rem deleted and regenerated at any time, so an icon pointing into it breaks the first time someone
rem clears it.
title Deploy Prose Hub
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0v3\tools\deploy-apps.ps1" -Apps Hub -Start Hub
if errorlevel 1 (
    echo.
    echo   DEPLOY FAILED -- see the output above. Nothing was launched.
    pause
    exit /b 1
)
