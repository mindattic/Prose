@echo off
REM Nightly Bushido Coda lore-triple sweep + commit + push.
REM Scheduled by Windows Task Scheduler at 01:00 America/Chicago (see tools/install-nightly-task.cmd).
REM
REM Steps:
REM   1. cd into the repo
REM   2. run extract-lore-triples.js in book mode against Bushido Coda
REM   3. if continuity changed, commit + push
REM
REM Output goes to engine/data/logs/nightly-lore-triple-sweep-YYYYMMDD.log so the run is auditable.

setlocal
set REPO=D:\Projects\MindAttic\StreetSamurai
set BOOK=eb91080d9c9c4f2b9b405fa5996bdea1
set NODE="C:\Program Files\nodejs\node.exe"
set GIT="C:\Program Files\Git\cmd\git.exe"
set STAMP=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%
set LOG=%REPO%\engine\data\logs\nightly-lore-triple-sweep-%STAMP%.log

cd /d %REPO%
echo [%DATE% %TIME%] Starting nightly lore-triple sweep on %BOOK% > "%LOG%"

%NODE% tools\extract-lore-triples.js %BOOK% --mode book --max-tokens 4096 >> "%LOG%" 2>&1
set EXIT=%ERRORLEVEL%
echo [%DATE% %TIME%] extract-lore-triples exited %EXIT% >> "%LOG%"

REM Commit + push only if engine\data\continuity changed.
%GIT% add engine\data\continuity >> "%LOG%" 2>&1
%GIT% diff --cached --quiet engine\data\continuity
if %ERRORLEVEL% neq 0 (
    echo [%DATE% %TIME%] Continuity store changed — committing >> "%LOG%"
    %GIT% commit -m "nightly lore-triple sweep: %BOOK%" >> "%LOG%" 2>&1
    %GIT% push >> "%LOG%" 2>&1
) else (
    echo [%DATE% %TIME%] No continuity-store changes — skipping commit >> "%LOG%"
)

endlocal
exit /b %EXIT%
