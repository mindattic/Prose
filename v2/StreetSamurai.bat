@echo off
set "ROOT=%~dp0"

:: Detect PowerShell (prefer pwsh over powershell)
set PS=
where pwsh >nul 2>&1 && set PS=pwsh
if not defined PS where powershell >nul 2>&1 && set PS=powershell
if not defined PS (
    echo PowerShell not found. Install it from https://aka.ms/powershell
    pause
    exit /b 1
)

title Street Samurai

:: Run the menu
%PS% -NoProfile -ExecutionPolicy Bypass -File "%ROOT%StreetSamurai.ps1"

if %errorlevel% neq 0 (
    echo.
    echo Script exited with an error. See above for details.
    pause
)
