@echo off
rem Desktop entry point: republish the Prose Hub from source, then start it.
rem
rem The Hub is the resident server every CLI command and MCP tool forwards into, and it serves the
rem editor at /writer and the entity wiki at /repo. Starting it alone is what you want when you are
rem using the browser or the command line rather than the Writer window.
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
