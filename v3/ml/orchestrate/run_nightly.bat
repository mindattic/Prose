@echo off
rem Prose ML Nightly Run
rem Invoked by Windows Task Scheduler at 02:00 daily.
rem Runs as current user — Windows Auth to LocalDB (no credentials needed).

setlocal
set ROOT=D:\Projects\MindAttic\Prose\v3\ml
set LOG=D:\Projects\MindAttic\Prose\v3\ml_nightly.log
set PYTHONIOENCODING=utf-8
set PYTHONUTF8=1
set NO_COLOR=1

cd /d "%ROOT%"
call .venv\Scripts\activate.bat

echo. >> "%LOG%"
echo ===== %DATE% %TIME% ===== >> "%LOG%"
python orchestrate\nightly_run.py --phases all >> "%LOG%" 2>&1

echo Exit code: %ERRORLEVEL% >> "%LOG%"
exit /b %ERRORLEVEL%
