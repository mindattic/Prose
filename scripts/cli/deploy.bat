@echo off
REM Thin batch wrapper so the /deploy slash command can be invoked from
REM any shell. Forwards every argument to deploy.ps1.
REM
REM   scripts\cli\deploy.bat                  -> sync + build (no push)
REM   scripts\cli\deploy.bat -Push            -> sync + build + commit + push
REM   scripts\cli\deploy.bat -NoSync -NoBuild -> only the git push gate

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy.ps1" %*
exit /b %ERRORLEVEL%
