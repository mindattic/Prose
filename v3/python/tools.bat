@echo off
setlocal EnableDelayedExpansion

REM ── Parse --force flag ────────────────────────────────────────────────────
set FORCE=0
for %%A in (%*) do (
    if /I "%%A"=="--force" set FORCE=1
)

REM ── Working directory = this script's folder ──────────────────────────────
cd /d "%~dp0"

:MENU
cls
echo.
echo  ============================================================
echo   Street Samurai  Python Tools
echo  ============================================================
if %FORCE%==1 (
    echo   Mode: FORCE  ^(confirmation prompts skipped^)
) else (
    echo   Usage: tools.bat [--force]   to skip confirmation prompts
)
echo  ============================================================
echo.
echo   SINGLE SCRIPTS — CHARACTER
echo    1   Generate Ancestry              assign 3-tier genetics to all characters
echo    2   Generate Descriptions          create physical descriptions + image prompts
echo    3   Harmonize Descriptions         align descriptions to match ancestry
echo    4   Fix Description Names          replace stale character names in descriptions
echo    5   Migrate GUIDs + Surnames       convert refs to GUIDs, regenerate names
echo.
echo   SINGLE SCRIPTS — WORLD GENERATION
echo    6   Generate Language Documents    language evolution + slang lexicon
echo    7   Generate Legal Documents       legal system + shadow economy docs
echo    8   Generate New Weird Quotes      anomaly / strangeness quotes
echo    9   Generate SNT Documents         synthetic neurovascular tissue docs
echo   10   Generate Space Elevator Docs   space elevator + Galapagos docs
echo   11   Create Corponations            create Meridian Orbital + Liang-Petrova JSON
echo.
echo   SINGLE SCRIPTS — FACT DISCOVERY
echo   12   Extract Facts                  extract triples from entity JSON  (API)
echo   13   Embed Facts                    generate vector embeddings
echo   14   Cluster Facts                  group semantically equivalent triples
echo   15   Score Facts                    consensus vote, flag inconsistencies
echo   16   Repair Facts               ^!  write consensus values back to source JSON
echo   17   Query Facts                    read-only: show claims and pipeline stats
echo.
echo   PIPELINES — run multiple scripts in sequence
echo    A   Refresh Descriptions           2: generate  -^>  4: fix names  -^>  3: harmonize
echo    B   All World Documents            6: language  -^>  7: legal  -^>  8: quotes
echo                                       9: SNT  -^>  10: space elevator
echo    C   Full Character Regen           ancestry -^> names -^> harmonize  (via orchestrator)
echo    D   Full Fact Pipeline             extract -^> embed -^> cluster -^> score -^> repair
echo    E   Full World Build           ^!  B: world docs  -^>  C: char regen  -^>  D: facts
echo.
echo    Q   Quit
echo.
echo  ============================================================
set /p CHOICE=  Choice:

if /I "!CHOICE!"=="Q" goto :EOF
if "!CHOICE!"=="1"  goto :RUN_ANCESTRY
if "!CHOICE!"=="2"  goto :RUN_DESCRIPTIONS
if "!CHOICE!"=="3"  goto :RUN_HARMONIZE
if "!CHOICE!"=="4"  goto :RUN_FIX_NAMES
if "!CHOICE!"=="5"  goto :RUN_MIGRATE
if "!CHOICE!"=="6"  goto :RUN_LANGUAGE
if "!CHOICE!"=="7"  goto :RUN_LEGAL
if "!CHOICE!"=="8"  goto :RUN_QUOTES
if "!CHOICE!"=="9"  goto :RUN_SNT
if "!CHOICE!"=="10" goto :RUN_SPACE
if "!CHOICE!"=="11" goto :RUN_CORPS
if "!CHOICE!"=="12" goto :RUN_EXTRACT
if "!CHOICE!"=="13" goto :RUN_EMBED
if "!CHOICE!"=="14" goto :RUN_CLUSTER
if "!CHOICE!"=="15" goto :RUN_SCORE
if "!CHOICE!"=="16" goto :RUN_REPAIR
if "!CHOICE!"=="17" goto :RUN_QUERY
if /I "!CHOICE!"=="A" goto :PIPE_DESC_REFRESH
if /I "!CHOICE!"=="B" goto :PIPE_WORLD_DOCS
if /I "!CHOICE!"=="C" goto :PIPE_CHAR_REGEN
if /I "!CHOICE!"=="D" goto :PIPE_FACTS
if /I "!CHOICE!"=="E" goto :PIPE_FULL_WORLD

echo   Unknown option. Press any key to continue.
pause >nul
goto :MENU

REM ═══════════════════════════════════════════════════════════
REM  HELPERS
REM ═══════════════════════════════════════════════════════════

:CONFIRM
REM Usage: call :CONFIRM "warning text"
REM Returns errorlevel 1 if user declines
if %FORCE%==1 exit /b 0
echo.
echo   WARNING: %~1
set /p YESNO=   Proceed? [y/N]
if /I "!YESNO!"=="y" exit /b 0
echo   Cancelled.
pause >nul
exit /b 1

:STEP
REM Usage: call :STEP N TOTAL "label" "python_command_args"
REM Runs the step and aborts the pipeline on non-zero exit
set STEP_N=%~1
set STEP_TOTAL=%~2
set STEP_LABEL=%~3
set STEP_CMD=%~4
echo.
echo  ── Step !STEP_N!/!STEP_TOTAL!: !STEP_LABEL!
echo     ^> python !STEP_CMD!
echo.
python !STEP_CMD!
if errorlevel 1 (
    echo.
    echo  !! Step !STEP_N! FAILED: !STEP_LABEL!
    echo     Pipeline aborted. Fix the error above and re-run from step !STEP_N!.
    echo.
    pause
    exit /b 1
)
exit /b 0

:PAUSE_RETURN
echo.
echo   Done. Press any key to return to menu.
pause >nul
goto :MENU

REM ═══════════════════════════════════════════════════════════
REM  SINGLE SCRIPTS — CHARACTER
REM ═══════════════════════════════════════════════════════════

:RUN_ANCESTRY
cls
echo   Generate Ancestry
echo   Assigns 3-tier genetic ancestry to all characters.
echo   Resume-safe — skips characters that already have ancestry.
echo   Pass --force to this tool to overwrite existing ancestry.
echo.
if %FORCE%==1 (
    python generate_ancestry.py --force
) else (
    python generate_ancestry.py
)
goto :PAUSE_RETURN

:RUN_DESCRIPTIONS
call :CONFIRM "Generates/updates physical descriptions for all entities. Existing descriptions will be overwritten."
if errorlevel 1 goto :MENU
python generate_descriptions.py
goto :PAUSE_RETURN

:RUN_HARMONIZE
cls
echo   Harmonize Descriptions
echo   Adjusts physical descriptions to match genetic ancestry.
echo   Resume-safe — skips already-harmonized characters unless --force.
echo.
if %FORCE%==1 (
    python harmonize_descriptions.py --force
) else (
    python harmonize_descriptions.py
)
goto :PAUSE_RETURN

:RUN_FIX_NAMES
call :CONFIRM "Scans all character descriptions and replaces stale names. Modifies files in-place."
if errorlevel 1 goto :MENU
python fix_description_names.py
goto :PAUSE_RETURN

:RUN_MIGRATE
call :CONFIRM "Converts entity cross-references to GUIDs and regenerates surnames from ancestry. Large-scale file modification."
if errorlevel 1 goto :MENU
python migrate_guids_and_surnames.py
goto :PAUSE_RETURN

REM ═══════════════════════════════════════════════════════════
REM  SINGLE SCRIPTS — WORLD GENERATION
REM ═══════════════════════════════════════════════════════════

:RUN_LANGUAGE
call :CONFIRM "Generates language evolution and slang lexicon documents. Creates new worldbuilding files."
if errorlevel 1 goto :MENU
python generate_language_documents.py
goto :PAUSE_RETURN

:RUN_LEGAL
call :CONFIRM "Generates legal system and shadow economy documents. Creates new worldbuilding files."
if errorlevel 1 goto :MENU
python generate_legal_documents.py
goto :PAUSE_RETURN

:RUN_QUOTES
call :CONFIRM "Generates New Weird quotes documenting GLMZ anomalies. Creates new document files."
if errorlevel 1 goto :MENU
python generate_new_weird_quotes.py
goto :PAUSE_RETURN

:RUN_SNT
call :CONFIRM "Generates 33 Synthetic Neurovascular Tissue documents. Creates new worldbuilding files."
if errorlevel 1 goto :MENU
python generate_snt_documents.py
goto :PAUSE_RETURN

:RUN_SPACE
call :CONFIRM "Generates space elevator and Galapagos destruction documents. Creates new worldbuilding files."
if errorlevel 1 goto :MENU
python generate_space_elevator_documents.py
goto :PAUSE_RETURN

:RUN_CORPS
call :CONFIRM "Creates JSON entity files for Meridian Orbital Dynamics and Liang-Petrova Consortium."
if errorlevel 1 goto :MENU
python create_corps.py
goto :PAUSE_RETURN

REM ═══════════════════════════════════════════════════════════
REM  SINGLE SCRIPTS — FACT DISCOVERY
REM ═══════════════════════════════════════════════════════════

:RUN_EXTRACT
cls
echo   Extract Facts — reads entity JSON, calls Anthropic API, writes triples to facts.db
echo.
python fact_extract.py
goto :PAUSE_RETURN

:RUN_EMBED
cls
echo   Embed Facts — generates vector embeddings for triples in facts.db
echo.
python fact_embed.py
goto :PAUSE_RETURN

:RUN_CLUSTER
cls
echo   Cluster Facts — groups semantically equivalent triples using HDBSCAN
echo.
python fact_cluster.py
goto :PAUSE_RETURN

:RUN_SCORE
cls
echo   Score Facts — consensus voting within clusters, flags disagreements
echo.
python fact_score.py
goto :PAUSE_RETURN

:RUN_REPAIR
call :CONFIRM "Repairs flagged inconsistencies by writing consensus values back to source entity JSON."
if errorlevel 1 goto :MENU
python fact_repair.py
goto :PAUSE_RETURN

:RUN_QUERY
cls
echo   Query Facts — read-only: show claims by subject, inconsistencies, and stats
echo.
python fact_query.py
goto :PAUSE_RETURN

REM ═══════════════════════════════════════════════════════════
REM  PIPELINES
REM ═══════════════════════════════════════════════════════════

REM ── A: Refresh Descriptions ──────────────────────────────
:PIPE_DESC_REFRESH
cls
echo   Pipeline A — Refresh Descriptions
echo.
echo   Step 1/3   generate_descriptions.py
echo   Step 2/3   fix_description_names.py
echo   Step 3/3   harmonize_descriptions.py
echo.
echo   Generates fresh physical descriptions, fixes any stale character
echo   names in them, then re-aligns all traits with genetic ancestry.
echo.
call :CONFIRM "This will overwrite existing descriptions for all entities."
if errorlevel 1 goto :MENU

call :STEP 1 3 "Generate Descriptions" "generate_descriptions.py"
if errorlevel 1 goto :MENU

call :STEP 2 3 "Fix Description Names" "fix_description_names.py"
if errorlevel 1 goto :MENU

if %FORCE%==1 (
    call :STEP 3 3 "Harmonize Descriptions" "harmonize_descriptions.py --force"
) else (
    call :STEP 3 3 "Harmonize Descriptions" "harmonize_descriptions.py"
)
if errorlevel 1 goto :MENU

goto :PAUSE_RETURN

REM ── B: All World Documents ────────────────────────────────
:PIPE_WORLD_DOCS
cls
echo   Pipeline B — All World Documents
echo.
echo   Step 1/5   generate_language_documents.py
echo   Step 2/5   generate_legal_documents.py
echo   Step 3/5   generate_new_weird_quotes.py
echo   Step 4/5   generate_snt_documents.py
echo   Step 5/5   generate_space_elevator_documents.py
echo.
echo   Generates all worldbuilding documents in one pass.
echo   Each step is independent — a failure stops the pipeline at that point.
echo.
call :CONFIRM "This will generate all world documents. Files will be created/overwritten."
if errorlevel 1 goto :MENU

call :STEP 1 5 "Language Documents" "generate_language_documents.py"
if errorlevel 1 goto :MENU

call :STEP 2 5 "Legal Documents" "generate_legal_documents.py"
if errorlevel 1 goto :MENU

call :STEP 3 5 "New Weird Quotes" "generate_new_weird_quotes.py"
if errorlevel 1 goto :MENU

call :STEP 4 5 "SNT Documents" "generate_snt_documents.py"
if errorlevel 1 goto :MENU

call :STEP 5 5 "Space Elevator Documents" "generate_space_elevator_documents.py"
if errorlevel 1 goto :MENU

goto :PAUSE_RETURN

REM ── C: Full Character Regen ───────────────────────────────
:PIPE_CHAR_REGEN
cls
echo   Pipeline C — Full Character Regen
echo.
echo   Step 1/4   generate_ancestry.py
echo   Step 2/4   migrate_guids_and_surnames.py  (surnames)
echo   Step 3/4   migrate_guids_and_surnames.py  (first names)
echo   Step 4/4   harmonize_descriptions.py
echo.
echo   Orchestrated by run_character_regen.py which handles the argument
echo   passing and step tracking internally.
echo.
call :CONFIRM "This regenerates ancestry, names, and descriptions for ALL characters. Major write operation."
if errorlevel 1 goto :MENU

if %FORCE%==1 (
    python run_character_regen.py --force
) else (
    python run_character_regen.py
)
if errorlevel 1 (
    echo.
    echo  !! Pipeline C failed. Check output above.
    pause
    goto :MENU
)
goto :PAUSE_RETURN

REM ── D: Full Fact Pipeline ─────────────────────────────────
:PIPE_FACTS
cls
echo   Pipeline D — Full Fact Pipeline
echo.
echo   Step 1/5   fact_extract.py
echo   Step 2/5   fact_embed.py
echo   Step 3/5   fact_cluster.py
echo   Step 4/5   fact_score.py
echo   Step 5/5   fact_repair.py   ^! writes back to source JSON
echo.
echo   Orchestrated by run_fact_pipeline.py.
echo.
call :CONFIRM "Runs full fact discovery including repair, which writes consensus values to source JSON."
if errorlevel 1 goto :MENU

python run_fact_pipeline.py
if errorlevel 1 (
    echo.
    echo  !! Pipeline D failed. Check output above.
    pause
    goto :MENU
)
goto :PAUSE_RETURN

REM ── E: Full World Build ────────────────────────────────────
:PIPE_FULL_WORLD
cls
echo   Pipeline E — Full World Build
echo.
echo   Phase 1   All World Documents   ^(B: 5 generators^)
echo   Phase 2   Full Character Regen  ^(C: ancestry -^> names -^> harmonize^)
echo   Phase 3   Full Fact Pipeline    ^(D: extract -^> embed -^> cluster -^> score -^> repair^)
echo.
echo   This is a long-running operation hitting the Anthropic API heavily.
echo   Each phase is independent — a failure stops the build at that point.
echo.
call :CONFIRM "Full world build: generates all docs, regenerates all characters, then runs full fact pipeline."
if errorlevel 1 goto :MENU

REM Phase 1 — World Documents
echo.
echo  ════════════════════════════════════════════════════════
echo   Phase 1 / 3  ^|  All World Documents
echo  ════════════════════════════════════════════════════════
call :STEP 1 5 "Language Documents"       "generate_language_documents.py"
if errorlevel 1 goto :MENU
call :STEP 2 5 "Legal Documents"          "generate_legal_documents.py"
if errorlevel 1 goto :MENU
call :STEP 3 5 "New Weird Quotes"         "generate_new_weird_quotes.py"
if errorlevel 1 goto :MENU
call :STEP 4 5 "SNT Documents"            "generate_snt_documents.py"
if errorlevel 1 goto :MENU
call :STEP 5 5 "Space Elevator Documents" "generate_space_elevator_documents.py"
if errorlevel 1 goto :MENU

REM Phase 2 — Character Regen
echo.
echo  ════════════════════════════════════════════════════════
echo   Phase 2 / 3  ^|  Full Character Regen
echo  ════════════════════════════════════════════════════════
if %FORCE%==1 (
    python run_character_regen.py --force
) else (
    python run_character_regen.py
)
if errorlevel 1 (
    echo.
    echo  !! Phase 2 failed. Fix the error and resume from Pipeline C.
    pause
    goto :MENU
)

REM Phase 3 — Fact Pipeline
echo.
echo  ════════════════════════════════════════════════════════
echo   Phase 3 / 3  ^|  Full Fact Pipeline
echo  ════════════════════════════════════════════════════════
python run_fact_pipeline.py
if errorlevel 1 (
    echo.
    echo  !! Phase 3 failed. Fix the error and resume from Pipeline D.
    pause
    goto :MENU
)

goto :PAUSE_RETURN
