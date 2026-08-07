@echo off
rem ──────────────────────────────────────────────────────────────────────────
rem  prose — Prose CLI shim
rem
rem  Forwards every argument to the standalone CLI project.
rem
rem  Usage examples:
rem    prose --migrate-sql --rebuild
rem    prose --migrate-sql --schema
rem    prose --migrate-sql --import all
rem    prose --migrate-sql --character-relational
rem    prose --ask "what does Kyle carry?"
rem    prose --book export <book-id>
rem    prose --repair --continuity
rem ──────────────────────────────────────────────────────────────────────────

setlocal

rem Locate this script's directory (the repo root) so `prose` works from anywhere.
set "REPO_ROOT=%~dp0"
set "CLI_PROJ=%REPO_ROOT%v3\Prose.Cli"

rem `dotnet run` does an incremental build automatically — fast on warm builds,
rem and always up-to-date when source has changed. Pass-through every arg.
dotnet run --project "%CLI_PROJ%" -- %*

endlocal
exit /b %ERRORLEVEL%
