@echo off
rem Desktop entry point: republish KdpPublish from source, then open it.
rem
rem KdpPublish deploys into its own subfolder (C:\Apps\MindAttic\Prose\KdpPublish\) rather than
rem flat with the others, because it ships its own wwwroot\ and writes run logs beside its exe -
rem publishing it into the Hub's directory would merge two unrelated static-asset trees.
rem
rem Lives in the repo, not in C:\Apps: that folder is a build output that can be regenerated at any
rem time, so an icon pointing into it breaks the first time someone clears it.
title Deploy KdpPublish
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0v3\tools\deploy-apps.ps1" -Apps KdpPublish -Start KdpPublish
if errorlevel 1 (
    echo.
    echo   DEPLOY FAILED -- see the output above. Nothing was launched.
    pause
    exit /b 1
)
