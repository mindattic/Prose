@echo off
rem ──────────────────────────────────────────────────────────────────────────
rem  ss — StreetSamurai CLI shim
rem
rem  Forwards every argument to the Blazor host project, which dispatches to
rem  the appropriate CLI handler (MigrateSqlCli, BookCli, AskCli, etc.).
rem
rem  Usage examples:
rem    ss --migrate-sql --rebuild
rem    ss --migrate-sql --schema
rem    ss --migrate-sql --import all
rem    ss --migrate-sql --character-relational
rem    ss --ask "what does Kyle carry?"
rem    ss --book export <book-id>
rem    ss --repair --continuity
rem ──────────────────────────────────────────────────────────────────────────

setlocal

rem Locate this script's directory (the repo root) so `ss` works from anywhere.
set "REPO_ROOT=%~dp0"
set "BLAZOR_PROJ=%REPO_ROOT%v3\StreetSamurai.Blazor"

rem `dotnet run` does an incremental build automatically — fast on warm builds,
rem and always up-to-date when source has changed. Pass-through every arg.
dotnet run --project "%BLAZOR_PROJ%" -- %*

endlocal
exit /b %ERRORLEVEL%
