@echo off
rem StreetSamurai ML Nightly Run
rem Invoked by Windows Task Scheduler at 02:00 daily.
rem Runs as current user — Windows Auth to LocalDB (no credentials needed).

setlocal
set ROOT=D:\Projects\MindAttic\StreetSamurai\v3\ml
set LOG=D:\Projects\MindAttic\StreetSamurai\v3\ml_nightly.log

cd /d "%ROOT%"
call .venv\Scripts\activate.bat

echo. >> "%LOG%"
echo ===== %DATE% %TIME% ===== >> "%LOG%"
python orchestrate\nightly_run.py --phases all >> "%LOG%" 2>&1

echo Exit code: %ERRORLEVEL% >> "%LOG%"
exit /b %ERRORLEVEL%
