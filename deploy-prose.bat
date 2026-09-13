@echo off
rem Desktop entry point: republish ALL four Prose apps from source, then open the Launcher.
rem
rem The Launcher is the "pick an app" shell; it starts the Hub itself if the Hub is not already up.
rem Use this icon when you want everything current; use deploy-writer / deploy-hub / deploy-kdp
rem when you want one app and a shorter wait.
rem
rem Lives in the repo, not in C:\Apps: that folder is a build output that can be regenerated at any
rem time, so an icon pointing into it breaks the first time someone clears it.
title Deploy Prose
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0v3\tools\deploy-apps.ps1" -Apps Hub,Writer,Launcher,KdpPublish -Start Launcher
if errorlevel 1 (
    echo.
    echo   DEPLOY FAILED -- see the output above. Nothing was launched.
    pause
    exit /b 1
)
