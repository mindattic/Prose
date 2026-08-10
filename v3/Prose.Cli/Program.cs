using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MindAttic.Authentication;
using MindAttic.Authentication.Web;
using Prose.Cli;
using Prose.Core.Data;
using Prose.Core.Extensions;
using Prose.Core.Services;
using MindAttic.Vault.Configuration;

// Force UTF-8 console I/O so piped prose (em-dashes, curly quotes, etc.) round-trips
// correctly through `Get-Content <file> | dotnet run -- --beat update --text -`.
// Without this, Windows defaults to OEM 437, which maps E2 80 94 (UTF-8 em dash) to
// the mojibake sequence "ΓÇö" and corrupts every non-ASCII character in stored beats.
Console.InputEncoding  = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

// QuestPDF Community license — required call before the first Document.Create.
// This project is the non-commercial indie use case the Community tier exists for.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Multi-universe: a global `--universe <slug>` flag selects which universe this
// process targets (SS-LAW-15). UniverseContext also honors the PROSE_UNIVERSE env
// var (per terminal), so two CLIs can write different universes at once. Parsed
// here before the dispatch chain so every CLI block + the web host inherit it.
// `prose --universe <verb>` is universe MANAGEMENT, not universe SCOPING. The token after
// --universe is then a verb (list/current/use), not a slug, so it must not be parsed as one:
// doing so made `prose --universe current` die with "Unknown universe slug 'current'" during
// service construction, before UniverseCli ever ran. Detected once and reused by the dispatch
// block further down, so the two cannot disagree about what counts as a management command.
var isUniverseManagementCommand =
    args.Length > 0 && args[0] == "--universe"
    && (args.Length == 1 || UniverseCli.IsSubcommand(args[1]));

if (!isUniverseManagementCommand)
    UniverseBootstrap.RequestedSlug ??= UniverseBootstrap.ParseSlug(args);

// HARD RULE: no silent GLMZ default. Before this check, an omitted --universe/PROSE_UNIVERSE
// fell through to UniverseContext's persisted "current_universe" default (in practice, GLMZ)
// — every content-touching command would silently scope to the wrong universe with no error,
// the exact failure mode "Universe division absolute" exists to prevent (a `--slug <SCRY node>`
// lookup against a GLMZ-scoped query filter just returns "not found", never "wrong universe").
// UNIVERSE_AGNOSTIC_COMMANDS is a short, explicit allowlist of the few flags that are genuinely
// cross-universe utilities (each resolves its own per-row UniverseId, not the ambient scope) or
// touch no universe-scoped data at all (auth). Everything else must name its universe explicitly.
if (UniverseBootstrap.RequestedSlug == null
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROSE_UNIVERSE")))
{
    string[] UniverseAgnosticCommands =
    [
        "--reset-password", "--sync-markdown", "--generate-canon-md", "--migrate-canon-docs",
        "--schema", "--universe", "--help", "-h", "--sql-export", "--gpu", "--runpod",
        "--kdp-status", "--kdp-manifest", "--kdp-mark-published", "--audit-consistency",
        // 2026-08-09: --seed applies raw SqlSeedService.Seeds scripts, each of which either
        // touches no universe-scoped row (schema ALTER/CREATE) or targets its own explicit,
        // hardcoded row (e.g. add_universe_*.sql inserts a specific new Universe row) — there
        // is no ambient universe scope for a seed script to need or default. Found missing
        // while registering the universe_nonfiction/horror/erotica seeds (fresh-machine
        // reproducibility audit): the guard blocked exactly the fresh-DB-bootstrap use case
        // this flag exists for.
        "--seed", "--migrate-sql",
    ];
    var isAgnostic = args.Length == 0 || UniverseAgnosticCommands.Any(args.Contains);
    if (!isAgnostic)
    {
        Console.Error.WriteLine(
            "[universe] No universe scope given. Pass --universe glmz|scry|gspl (or set PROSE_UNIVERSE) — " +
            "this command touches universe-scoped data and no longer silently defaults to GLMZ.");
        Environment.ExitCode = 2;
        return;
    }
}

// CLI mode: dotnet run --project ... -- --rebuild-graph [--universe <slug>]
// Rebuilds the scoped universe's <slug>_universe_graph.json cache from source data
// without starting the web server. One universe per invocation (scope is pinned below).
if (args.Contains("--rebuild-graph"))
{
    var sp = BuildCoreServices(args);
    // Pin the universe scope BEFORE building so it can't shift mid-rebuild. Resolving the context
    // forces its lazy catalog load + applies the --universe/PROSE_UNIVERSE/default selection, so every
    // builder in this rebuild sees one stable scope (the non-deterministic node/edge counts came
    // from the scope resolving partway through the multi-builder pass). Defaults to GLMZ.
    var cliUniverse = sp.GetRequiredService<Prose.Core.Services.IUniverseContext>();
    Console.WriteLine($"[rebuild-graph] Universe scope: {cliUniverse.CurrentSlug} ({cliUniverse.CurrentId})");
    var graph = sp.GetRequiredService<WorldGraphService>();
    Console.WriteLine("[rebuild-graph] Rebuilding world graph from source data...");
    graph.Rebuild();
    Console.WriteLine($"[rebuild-graph] Done: {graph.NodeCount} nodes, {graph.EdgeCount} edges saved to {cliUniverse.CurrentSlug}_universe_graph.json");
    return;
}

// CLI mode: prose --reset-password --email <e> --password <p> [--require-change]
// Operator password reset over the MindAttic.Authentication store, no web server.
if (args.Contains("--reset-password"))
{
    var sp = BuildServicesWithVaultAndAuth(args);
    Environment.ExitCode = await ResetPasswordCli.RunAsync(args, sp);
    return;
}

// CLI mode: dotnet run --project ... -- --write-story <mode> [options]
// Generates a story via the same pipeline as the /stories UI and saves it as a Chapter.
if (args.Contains("--write-story"))
{
    var sp = BuildCoreServices(args);
    var (proceed1, est1) = await CostGateCli.ConfirmAsync("--write-story", args, sp);
    if (!proceed1) return;
    var before1 = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await StoryWriterCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--write-story", est1, before1, sp);
    return;
}

// CLI mode: dotnet run --project ... -- --refine-story <projectId> [-o notes.json]
// Analyzes a completed story and writes refinement notes (no rewrites).
if (args.Contains("--refine-story"))
{
    var sp = BuildCoreServices(args);
    var (proceed2, est2) = await CostGateCli.ConfirmAsync("--refine-story", args, sp);
    if (!proceed2) return;
    var before2 = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await StoryRefineCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--refine-story", est2, before2, sp);
    return;
}

// CLI mode: book operations — list / new / show / chapters / absorb / review / apply / export / delete.
// Run `dotnet run --project Prose.Blazor -- --book` (no subcommand) to see full usage.
if (args.Contains("--book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BookCli.RunAsync(args, sp);
    return;
}

// CLI mode: unified continuity store — migrate / stats / contradictions / resolve / entity.
// Run `dotnet run --project Prose.Blazor -- --continuity` (no subcommand) to see full usage.
if (args.Contains("--continuity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = ContinuityCli.Run(args, sp);
    return;
}

// CLI mode: SQL Server migration — apply EF migrations and import JSON entities.
//   prose --migrate-sql --schema           apply EF migrations
//   prose --migrate-sql --import people    import character JSON files
//   prose --migrate-sql --all              schema + import all supported types
if (args.Contains("--migrate-sql"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateSqlCli.RunAsync(args, sp);
    return;
}

// CLI mode: prose → entities + edges. LLM-driven.
//   prose --interpret --text "..."  | --file path.txt
//   add --commit to apply, --auto-create to stub missing entities, --tag <source>
if (args.Contains("--interpret"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await InterpretCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a worldbuilding Document directly into canon.
//   prose --add-doc --title "…" --body-file path.md [--category essay] [--tags "a,b,c"] [--filename slug.md]
if (args.Contains("--add-doc"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddDocCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a Character from a CharacterData JSON file.
//   prose --add-character --file path.json
if (args.Contains("--add-character"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddCharacterCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert OR update a Place/District from a DistrictData JSON file.
// Upsert: include "id" to update, omit to create. Safe service-layer path
// (DistrictRepository.Save) — no hand-SQL, collision-safe slugs.
//   prose --add-place --file path.json [--print]
if (args.Contains("--add-place"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddPlaceCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a CorpoNation from a CorponationData JSON file.
//   prose --add-corponation --file path.json
if (args.Contains("--add-corponation"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddCorponationCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a Weapon from a WeaponryData JSON file.
//   prose --add-weapon --file path.json
if (args.Contains("--add-weapon"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddWeaponryCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert an Apparel item from an ApparelData JSON file.
//   prose --add-apparel --file path.json
if (args.Contains("--add-apparel"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddApparelCli.RunAsync(args, sp);
    return;
}

// CLI mode: generate a resource-tracked combat sequence via CombatSceneWriter.
//   prose --combat --file scene.json [--out prose.txt]
//   prose --combat --location "Hegewisch" --objective "..." --exchanges 6 --tone Cinematic
if (args.Contains("--combat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CombatCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert OR update a Faction from a FactionData JSON file.
// Upsert: include "id" to update, omit to create. Safe service-layer path
// (FactionRepository.Save) — no hand-SQL, collision-safe slugs.
//   prose --add-faction --file path.json
if (args.Contains("--add-faction"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddFactionCli.RunAsync(args, sp);
    return;
}

// CLI mode: insert a News article from a NewsData JSON file.
//   prose --add-news --file path.json
if (args.Contains("--add-news"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AddNewsCli.RunAsync(args, sp);
    return;
}

// CLI mode: per-table schema operations (snapshot + safe column-reorder rebuild).
//   prose --schema snapshot --table NAME [--out path.sql]
//   prose --schema rebuild  --table NAME --order "col1,col2,col3,…"
if (args.Contains("--schema"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SchemaCli.RunAsync(args, sp);
    return;
}

// CLI mode: dump the entire Prose DB to a re-runnable .sql script.
//   prose --sql-export --schema             schema-only DDL
//   prose --sql-export --data               schema + INSERT data
//   prose --sql-export --schema --out path  override output path
if (args.Contains("--sql-export"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SqlExportCli.RunAsync(args, sp);
    return;
}

// prose --swain-audit [--slug <slug> | --code <code> | --all] [--repair] [--blockers]
// Classifies every enabled beat as Scene / Sequel / Ambiguous / Deficient against
// Dwight Swain's Scene/Sequel doctrine. Deficient = BLOCKER; Ambiguous = MODERATE.
// Add --repair to auto-splice the missing structural element (disaster turn, decision, etc.)
// into BLOCKER beats via Haiku (classify) + Sonnet (splice). Exit 0 = success.
// MUST appear before the bare --repair handler below.
if (args.Contains("--swain-audit"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SwainAuditCli.RunAsync(args, sp);
    return;
}

// CLI mode: dossier-driven story repair — walks every chapter, augments character
// records with timeline entries and (optionally) LLM-extracted continuity claims.
//   prose --repair                # cheap timeline-only pass
//   prose --repair --continuity   # also run continuity extraction (LLM-heavy)
if (args.Contains("--repair"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RepairCli.RunAsync(args, sp);
    return;
}

// CLI mode: cloud RAG over the canon corpus. Replaces the retired Ollama path.
//   prose --ask "Question" [--k 8] [--type character]
if (args.Contains("--ask"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AskCli.RunAsync(args, sp);
    return;
}

// CLI mode: idempotent stub-creator for the seeded "Vultures on the Doorstep"
// future story. Creates the Book + Draft outline only; writes no prose.
//   prose --seed-vultures
if (args.Contains("--seed-vultures"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = VulturesSeedCli.Run(args, sp);
    return;
}

// CLI mode: report Character columns that disagree with their latest
// matching EntityStateEvents row. Lights up the static-vs-dynamic recipe
// only for columns that actually drifted.
//   prose --audit-drift           pretty-printed report
//   prose --audit-drift --json    JSON dump
if (args.Contains("--audit-drift"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AuditDriftCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill EntityStateEvents from the dynamic columns currently
// sitting on Characters (Location, LifeStatus, Role, Affiliation, Belongings*,
// Territory*, DailyLife). One-shot, idempotent.
if (args.Contains("--backfill-character-state"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StateBackfillCli.RunAsync(args, sp);
    return;
}

// CLI mode: rewrite ethnicity-keyed visual descriptors in image prompts to
// match a character's current genetic_ancestry. Cost-aware via stored hash.
//   prose --image-prompts regen --id <id|slug> [--force]
//   prose --image-prompts regen --all-changed
if (args.Contains("--image-prompts"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImagePromptsCli.RunAsync(args, sp);
    return;
}

// CLI mode: propose a plausible immediate family for one character.
//   prose --family-gen propose --of <id|slug>           dry run
//   prose --family-gen propose --of <id|slug> --commit  write characters + edges + propagate genetics
//   --seed N for reproducible RNG
if (args.Contains("--family-gen"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await FamilyGenCli.RunAsync(args, sp);
    return;
}

// CLI mode: propagate genetic_ancestry from parents to children via the
// family graph (with ±5% recombination noise). Currently a no-op until family
// ties are seeded.
//   prose --genetics propagate                     full graph
//   prose --genetics propagate --id <id|slug>      single character
//   prose --genetics propagate --seed 42           reproducible RNG
if (args.Contains("--genetics"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GeneticsCli.RunAsync(args, sp);
    return;
}

// CLI mode: family ties — hand-seed parent/sibling/spouse links between characters.
//   prose --family parent  --parent <id|slug> --child <id|slug>
//   prose --family sibling --a <id|slug> --b <id|slug>
//   prose --family spouse  --a <id|slug> --b <id|slug>
//   prose --family show    --of <id|slug>
if (args.Contains("--family"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await FamilyCli.RunAsync(args, sp);
    return;
}

// CLI mode: scan beats for deprecated/renamed noun references.
//   prose --validate-nouns --slug <slug>
if (args.Contains("--validate-nouns"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ValidateNounsCli.RunAsync(args, sp);
    return;
}

// CLI mode: CRUD for DeprecatedEntityNames (list/add/remove).
//   prose --deprecated-names --list [--universe <slug>]
//   prose --deprecated-names --add --universe <slug> --name <deprecatedName> --canonical <canonicalName> [--notes <notes>]
//   prose --deprecated-names --remove --id <id>
if (args.Contains("--deprecated-names"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DeprecatedNameCli.RunAsync(args, sp);
    return;
}

// CLI mode: DataConsistencyService SSOT-drift audit (SQL-only, no LLM calls).
//   prose --audit-consistency [--json]
if (args.Contains("--audit-consistency"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DataConsistencyCli.RunAsync(args, sp);
    return;
}

// CLI mode: GraphHealthService — orphaned/weakly-connected/malformed world-graph node audit.
//   prose --graph-health --universe <slug> [--json]
if (args.Contains("--graph-health"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GraphHealthCli.RunAsync(args, sp);
    return;
}

// CLI mode: DataScanUtility family (fix-phi/fix-identity/tag-lethality/tag-normalize/
// assign-tiers/cross-reference) -- mass canon-entity maintenance tools. Defaults to a dry-run
// preview; pass --apply to actually write.
//   prose --data-scan --tool <name> [--apply] [--overwrite] --universe <slug>
if (args.Contains("--data-scan"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DataScanCli.RunAsync(args, sp);
    return;
}

// prose --repair-slugs [--apply] [--family entities|nodes|books|series|episodes] [--json]
// Regenerate every slug from its Name/Title metadata and update slug-carrying
// references (beat audio paths, publication paths, on-disk dirs, alt_slug).
// DRY-RUN by default; --apply writes. Slugs are loose keys — guid is the key.
if (args.Contains("--repair-slugs"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SlugRepairCli.RunAsync(args, sp);
    return;
}

// CLI mode: survey management.
//   prose --list-surveys [--status Open|Completed]
//   prose --get-survey --slug <slug>
if (args.Contains("--list-surveys") || args.Contains("--get-survey"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SurveyCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill BeatEntityMentions — index which entity names appear in
// each beat so entity-update staleness propagation works.
//   prose --scan-entity-mentions
if (args.Contains("--scan-entity-mentions"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ScanEntityMentionsCli.RunAsync(sp);
    return;
}

// CLI mode: backfill Entities.Status = 'stub' / 'canon' based on BeatEntityMentions.
//   prose --backfill-stubs
// Entities with no BeatEntityMentions row → Status='stub' (excluded from universe graph).
// Entities that ARE mentioned → Status='canon'. Re-run after --scan-entity-mentions.
if (args.Contains("--backfill-stubs"))
{
    var sp = BuildCoreServices(args);
    var db2 = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
    await using var ctx2 = await db2.CreateDbContextAsync();
    var promoted = await ctx2.Database.ExecuteSqlRawAsync(
        "UPDATE Entities SET Status = 'canon', ModifiedAt = SYSUTCDATETIME() WHERE IsActive = 1 AND Status != 'canon' AND Status != 'archived' AND Id IN (SELECT DISTINCT EntityId FROM BeatEntityMentions)");
    var demoted = await ctx2.Database.ExecuteSqlRawAsync(
        "UPDATE Entities SET Status = 'stub', ModifiedAt = SYSUTCDATETIME() WHERE IsActive = 1 AND Status != 'stub' AND Status != 'archived' AND Id NOT IN (SELECT DISTINCT EntityId FROM BeatEntityMentions)");
    Console.WriteLine($"[backfill-stubs] promoted={promoted} canon, demoted={demoted} stub.");
    return;
}

// CLI mode: dump canon JSON to the user's Downloads folder.
//   prose --export global                every repo, zipped + timestamped
//   prose --export <repoName>            one repo, zipped (e.g. "people", "weaponry")
//   prose --export <entityId>            one entity, plain .json
if (args.Contains("--export"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExportCli.RunAsync(args, sp);
    return;
}

// CLI mode: rebuild the entity-embedding cache via cloud OpenAI.
//   prose --reembed              drift-skipped corpus pass (only changed entities re-embed)
//   prose --reembed --force      clear the table first, re-embed everything
if (args.Contains("--reembed"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReembedCli.RunAsync(args, sp);
    return;
}

// CLI mode: query the Legion / LLMVoting cloud-LLM panel directly.
//   prose --legion ask "Q" --options "A,B,C"  → forced-choice Quorum decision (JSON on stdout)
//   prose --legion vote "Q" [--context "…"]    → open-ended vote with synthesized narrative
if (args.Contains("--legion"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await LegionCli.RunAsync(args, sp);
    return;
}

// --archive-json retired 2026-05-08 with JsonArchivalService — engine/data/*.json
// no longer exists, so legacy-file verification is moot.

// CLI mode: apply canonical SQL seeds via C# (replaces sqlcmd-by-hand workflow).
//   prose --seed                     list known seeds
//   prose --seed <name>              apply one
//   prose --seed --all [--force]     apply every known seed in order
// NOTE: --seed is also the prompt flag of --write-node / --write-story /
// --create-book — those commands must win the dispatch or their calls get
// hijacked by the SQL seeder.
if (args.Contains("--seed") && !args.Contains("--write-node")
    && !args.Contains("--write-story") && !args.Contains("--create-book")
    && !args.Contains("--run-corpus"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)generate the node bible for an existing node.
//   prose --book-bible --slug <slug> [--beats N] [--replace-beats]
if (args.Contains("--book-bible"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NodeBibleCli.RunAsync(args, sp);
    return;
}

// CLI mode: regenerate canon document .md files from DB (CanonDocuments + CanonDocumentSections).
// The disk files are generated read-only mirrors; source of truth is the DB.
//   prose --generate-canon-md --type <WorldBible|WorldMaster|Franchise|UniverseCanon>
//   prose --generate-canon-md --all
if (args.Contains("--generate-canon-md"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CanonDocumentCli.RunAsync(args, sp);
    return;
}

// CLI mode: assemble the unified Book Context Document for a node.
// Merges hand-authored NodeBible + Structural Blueprint + Beat Spine into one document,
// writes to Nodes.NodeBible (DB) and docs/nodes/{CODE}.md (read-only disk mirror).
//   prose --generate-node-doc --slug <slug>
//   prose --generate-node-doc --all
if (args.Contains("--generate-node-doc"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NodeDocCli.RunAsync(args, sp);
    return;
}

// CLI mode: regenerate a universe's Master Glossary (Glossary.htm/.json/.txt under
// docs/universes/{SLUG}/) from the GlossaryTerms table.
//   prose --generate-glossary --universe <slug>   (omit --universe for all)
if (args.Contains("--generate-glossary"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GlossaryCli.RunMasterAsync(args, sp);
    return;
}

// CLI mode: regenerate a book's Glossary (docs/nodes/{CODE}-Glossary.htm/.json/.txt) — the
// subset of its universe's Master Glossary whose terms appear in the book's live prose.
//   prose --generate-book-glossary --slug <slug>
//   prose --generate-book-glossary --all
if (args.Contains("--generate-book-glossary"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GlossaryCli.RunBookAsync(args, sp);
    return;
}

// CLI mode: generate Node.CoverPrompt (image-model cover description) from the book's
// own Title/Summary/Description/universe.
//   prose --generate-cover-prompt --slug <slug>
//   prose --generate-cover-prompt --all
if (args.Contains("--generate-cover-prompt"))
{
    var sp = BuildCoreServices(args);
    var (proceedCp, estCp) = await CostGateCli.ConfirmAsync("--generate-cover-prompt", args, sp);
    if (!proceedCp) return;
    var beforeCp = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await GenerateCoverPromptCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--generate-cover-prompt", estCp, beforeCp, sp);
    return;
}

// CLI mode: render Node.CoverPrompt through an image provider (openai/stability/google)
// and save the cover under the media dir. Costs real money — requires an API key.
//   prose --generate-cover-image --slug <slug> --provider openai|stability|google
if (args.Contains("--generate-cover-image"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GenerateCoverImageCli.RunAsync(args, sp);
    return;
}

// CLI mode: composite a book's cover onto a 3D mockup template, generate a short AI
// image-to-video clip (hand shows the cover, opens it, flips pages) via a chosen video
// provider (kling/runway/sora), and assemble a vertical 1080x1920 #booktok MP4. Costs
// real money per call unless --dry-run, which stops after the local ImageMagick mockup.
//   prose --booktok --slug <slug> --provider kling|runway|sora [--duration 8] [--dry-run] [--yes]
//   prose --booktok --standalone --cover-path <path> --title "<title>" --provider kling|runway|sora
if (args.Contains("--booktok"))
{
    var sp = BuildCoreServices(args);
    if (args.Contains("--dry-run"))
    {
        // No paid API call happens in dry-run — skip the cost gate entirely.
        Environment.ExitCode = await BookTokCli.RunAsync(args, sp);
        return;
    }
    var gateArgs = args.Contains("--yes") ? args.Append("--no-confirm").ToArray() : args;
    var (proceedBt, _) = await CostGateCli.ConfirmAsync("--booktok", gateArgs, sp);
    if (!proceedBt) return;
    Environment.ExitCode = await BookTokCli.RunAsync(args, sp);
    return;
}

// CLI mode: redraw the title onto an already-saved cover image without calling an
// image-generation API again.
//   prose --composite-cover-title --slug <slug>
if (args.Contains("--composite-cover-title"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CompositeCoverTitleCli.RunAsync(args, sp);
    return;
}

// CLI mode: generate a new node (bible-first: plan → planned beats → expand in UI).
// CLI mode: autonomous corpus loop — generate N nodes end-to-end and review them.
//   prose --run-corpus --count N [--seed "..."] [--kind episode] [--beats 12] [--ballots 20] [--resume] [--dry-run]
if (args.Contains("--run-corpus"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RunCorpusCli.RunAsync(args, sp);
    return;
}

// CLI mode: expand planned beats in a node to prose (headless ✨ for each beat).
//   prose --edit-beat --slug <slug> (--beat-number N | --insert-after N) --file <path>
if (args.Contains("--edit-beat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EditBeatCli.RunAsync(args, sp);
    return;
}

// CLI mode: re-slot a beat within its node's reading order (wraps NodeWorkbenchService
// .MoveBeatAsync, previously reachable only from the Blazor drag-and-drop UI).
//   prose --move-beat --slug <slug> --beat-number N --after M   (M=0 moves to the top)
if (args.Contains("--move-beat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MoveBeatCli.RunAsync(args, sp);
    return;
}

// CLI mode: enable/disable a beat's membership in a node's reading order without touching the
// Beat row itself (wraps NodeWorkbenchService.SetBeatMembershipEnabledAsync).
//   prose --set-beat-enabled --slug <slug> (--beat-number N | --beat-id <guid>) [--enable]
if (args.Contains("--set-beat-enabled"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SetBeatEnabledCli.RunAsync(args, sp);
    return;
}

// CLI mode: create a new empty root node (bible-first; no beats yet).
//   prose --create-book --title "..." [--code SRZR] [--kind book] [--description "..."] [--seed "..."] [--previous <slug|id>] [--parent <slug|id>]
if (args.Contains("--create-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CreateNodeCli.RunAsync(args, sp);
    return;
}

//   prose --expand-beat (--slug <slug> | --id <guid>) [--beat <beatId>] [--force]
if (args.Contains("--expand-beat"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExpandBeatCli.RunAsync(args, sp);
    return;
}

//   prose --auto-run (--slug <slug> | --id <guid>) [--effort draft|standard] [--dry-run] [--force]
if (args.Contains("--auto-run"))
{
    var sp = BuildCoreServices(args);
    var (proceedAr, estAr) = await CostGateCli.ConfirmAsync("--auto-run", args, sp);
    if (!proceedAr) return;
    var beforeAr = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await AutoRunCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--auto-run", estAr, beforeAr, sp);
    return;
}

//   prose --write-node --seed "..." [--title "..."] [--kind episode] [--beats 12] [--bible-only]
if (args.Contains("--write-node"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WriteNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: delete the 44 legacy book/chapter Entity+Records blobs whose
// content already lives in the Nodes/Beats model. Classifies each as JUNK,
// REDUNDANT, or ORPHAN (converts orphans to Nodes before deleting).
//   prose --migrate-legacy-book-chapter
if (args.Contains("--migrate-legacy-book-chapter"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateLegacyBookChapterCli.RunAsync(args, sp);
    return;
}

// Truth-First Architecture — Step A2: migrate hand-editable canon .md files
// (BIBLE.md, WORLD.md, FRANCHISE.md, universes/CAUL.md) into CanonDocument +
// CanonDocumentSection DB rows. Idempotent; skips already-migrated documents.
//   prose --migrate-canon-docs [--dry-run]
if (args.Contains("--migrate-canon-docs"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateCanonDocsCli.RunAsync(args, sp);
    return;
}

// Truth-First Architecture — Step A2 (NodeBible): migrate Nodes.NodeBible text
// blobs into NodeBibleSection rows. Creates a single "Full" section per node.
// Idempotent; skips nodes that already have sections.
//   prose --migrate-node-bibles [--slug <slug>] [--dry-run]
if (args.Contains("--migrate-node-bibles"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateNodeBiblesCli.RunAsync(args, sp);
    return;
}

// Truth-First Architecture — Step B2: decompose EscalationCurveJson /
// EventTypePaletteJson blobs and BeatTags into per-beat BeatBlueprintDecision rows.
// Idempotent; skips beats that already have a decision row.
//   prose --migrate-blueprint-rows [--slug <slug>] [--dry-run]
if (args.Contains("--migrate-blueprint-rows"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MigrateBlueprintRowsCli.RunAsync(args, sp);
    return;
}

//   prose --verify-beat --id <beatId> [--json]
//   prose --verify-book --slug <slug> [--json]
//   prose --verify-quote --id <beatId> --quote "<claimed text>" [--claimed-by <name>] [--json]
//   prose --verify-quotes-batch --json-file <path> [--json]
// Beat Verification Engine (Track C): checks prose against declared BeatBlueprintDecision
// contract. Results upserted to BeatVerification table. BLOCKER findings block --export-node.
// QuoteGrounding checks: confirm a logic-sweep audit agent's claimed quote actually appears
// in the beat it's attributed to, before that finding is trusted for triage/fix (SS-LOGIC-4a).
if (args.Contains("--verify-beat") || args.Contains("--verify-book")
    || args.Contains("--verify-quote") || args.Contains("--verify-quotes-batch"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await VerifyBeatCli.RunAsync(args, sp);
    return;
}

// CLI mode: migrate legacy Books/Chapters/ChapterBeats/Episodes/EpisodeBeats
// data into the unified Beat/Node schema. Idempotent — safe to re-run.
//   prose --migrate-nodes
if (args.Contains("--migrate-nodes"))
{
    var sp = BuildCoreServices(args);
    var svc = sp.GetRequiredService<NodeMigrationService>();
    var report = await svc.MigrateAllAsync();
    Console.WriteLine($"[migrate-nodes] Books={report.BooksAdded} Chapters={report.ChaptersAdded} Beats={report.BeatsAdded} Episodes={report.EpisodesAdded} Standalone={report.StandaloneBeatsAdded} Junctions={report.JunctionRowsAdded}");
    return;
}

// CLI mode: reconcile audio bytes between local disk and Azure Blob storage.
// Companion to DualWriteAudioStore — repairs drift from offline recordings
// and failed background uploads. Default (no --push/--pull args) is full
// bidirectional repair. See SyncAudioCli class doc for the full arg list.
//   prose --sync-audio [--push] [--pull] [--node SLUG] [--dry-run] [--verbose]
if (args.Contains("--sync-audio"))
{
    // Surface %APPDATA%\MindAttic\<bucket>\providers.json — AzureBlobAudioStore reads
    // AudioStore:ConnectionString straight from IConfiguration with no fallback.
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await SyncAudioCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)narrate an EXISTING node by id (full or prefix) or slug.
// Runs the same NarrateAsync path the Record button uses. Use to re-record a
// node whose beats failed (e.g. a TTS 400) without regenerating prose.
//   prose --narrate-book (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--narrate-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NarrateNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: create a fixed, named reviewer panel of N personas, disjoint from
// every existing focus group (no persona on two panels). No LLM calls.
//   prose --make-group --name "Group B" [--size 128]
if (args.Contains("--make-group"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MakeGroupCli.RunAsync(args, sp);
    return;
}

// CLI mode: run Legion persona quality voting across canon entity repos.
// Replaces the old LlmVoting (10 GLMZ residents) with the full 1000-persona library,
// 1-100 scale, and append-only EntityReview rows (same process as node reviews).
//   prose --review-entity [--type <type>] [--ballots N] [--prose N] [--unrated]
if (args.Contains("--review-entity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReviewEntityCli.RunAsync(args, sp);
    return;
}

//   prose --link-weapon-ammo [--local-url URL] [--local-key KEY] [--local-model TAG] [--dry-run]
if (args.Contains("--link-weapon-ammo"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await LinkWeaponAmmoCli.RunAsync(args, sp);
    return;
}

//   prose --populate-queue --entity-review|--story-review|--beat-write|--status [options]
if (args.Contains("--populate-queue"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await Prose.Cli.PopulateQueueCli.RunAsync(args, sp);
    return;
}

//   prose --worker-mode --queue-url URL --worker-key KEY --worker-id ID --local-url LLM_URL [options]
if (args.Contains("--worker-mode"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await Prose.Cli.WorkerModeCli.RunAsync(args, sp);
    return;
}

// CLI mode: have N Legion personas each read an EXISTING node and write an
// honest, scored reader review (saved to NodeReviews), then synthesize the
// Amazon-style aggregate summary. Round-robins reviewers across the trusted-4.
//   prose --review-node (--id <guid|prefix> | --slug <slug>) [--readers N]
//   prose --review-book / --run-panel  (legacy aliases)
if (args.Contains("--review-node") || args.Contains("--review-book") || args.Contains("--run-panel"))
{
    var sp = BuildServicesWithVault(args);
    var cmdRn = args.Contains("--review-node") ? "--review-node"
              : args.Contains("--review-book") ? "--review-book" : "--run-panel";
    var (proceedRn, estRn) = await CostGateCli.ConfirmAsync(cmdRn, args, sp);
    if (!proceedRn) return;
    var beforeRn = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await ReviewNodeCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync(cmdRn, estRn, beforeRn, sp);
    return;
}

// CLI mode: manage the rented vast.ai review box (key from the MindAttic vault, provider 'vast').
//   prose --gpu <status|stop|start|destroy> [--instance <id>]
if (args.Contains("--gpu"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await VastGpuCli.RunAsync(args, sp);
    return;
}

// CLI mode: manage the rented RunPod review pod (key from the MindAttic vault, provider 'runpod').
//   prose --runpod <status|stop|start|terminate> [--pod <id>]
if (args.Contains("--runpod"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RunPodGpuCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)generate the portable per-voter report (JSON + filterable HTM) from
// a node's most recent stored review batch, without re-running the panel.
//   prose --review-report (--slug <slug> | --id <guid> | --code <CODE>) [--provider local|cloud|all]
if (args.Contains("--review-report"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReviewReportCli.RunAsync(args, sp);
    return;
}

// CLI mode: add an author ruling to the prose-lessons memory store.
// Lessons are injected into review ballot prompts so reviewers don't penalise
// beats the author has already ruled are doing their job.
//   prose --lesson-add --scope <scope> --kind <kind> --text "<text>"
//   Scope: global | node:<slug> | beat:<guid>
//   Kind:  score-vs-function | delight | voice | pacing | continuity | other
if (args.Contains("--lesson-add"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseLessonCli.RunAddAsync(args, sp);
    return;
}

// CLI mode: list prose lessons (all scopes or filtered).
//   prose --lessons-list [--scope <scope>]
if (args.Contains("--lessons-list"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseLessonCli.RunListAsync(args, sp);
    return;
}

// CLI mode: review-driven auto-editor. Weight the latest reviews, target the
// lowest / most-flagged beats (raise the floor), and emit conservative
// before/after rewrite PROPOSALS (JSON) for an approval survey. Nothing is written.
//   prose --edit-book (--id <guid|prefix> | --slug <slug>) [--top N]
if (args.Contains("--edit-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EditNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: stitch an existing node's beats into one combined file (WAV →
// MP3), copy it to the publish output dir (Downloads by default), and record
// the publication run + process-event ledger. Headless Publish button.
//   prose --publish-book (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PublishNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: set Amazon KDP backend keywords for one node (no generic default).
//   prose --seed-keywords --slug <slug> --keywords "phrase one|phrase two|..."
if (args.Contains("--seed-keywords"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedKeywordsCli.RunAsync(args, sp);
    return;
}

// CLI mode: three-altitudes agreement audit (designed story vs told story).
//   prose --altitude-audit (--slug <slug> | --all) [--force-synopsis]
if (args.Contains("--altitude-audit"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AltitudeAuditCli.RunAsync(args, sp);
    return;
}

// CLI mode: chapter-by-chapter synopsis export (also runs inside --export-node).
//   prose --export-synopsis (--slug <slug> | --all) [--force]
if (args.Contains("--export-synopsis"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExportSynopsisCli.RunAsync(args, sp);
    return;
}

// CLI mode: render a node to .docx + .epub + .pdf + .txt + metadata artifacts
// (description.txt, story-synopsis.txt, <CODE>-dcm-viz.htm). Local file
// rendering only — no KDP API integration, hence "export" not "publish".
//   prose --export-node (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--export-node"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ExportNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: hard-delete all disabled (IsEnabled=false) beats from a book.
// Use ONLY when a book is export-ready and placeholder beats will never be used.
// Temporal history retains all deleted beats; data is recoverable by a DBA.
//   prose --prune-disabled --slug <slug> [--dry-run] [--yes]
if (args.Contains("--prune-disabled"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PruneDisabledCli.RunAsync(args, sp);
    return;
}

// CLI mode: build an Audible AI-narration hand-off package for a node.
// Produces a narration-clean manuscript, pronunciation guide, and README.
//   prose --prepare-audible (--slug <slug> | --id <guid|prefix>) [--no-phonetics]
if (args.Contains("--prepare-audible"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PrepareAudibleCli.RunAsync(args, sp);
    return;
}

// CLI mode: deterministic timeline-consistency check (RFC 0009 §5).
// Detects dead-character-acting and wound-regression violations. No LLM calls.
//   prose --timeline-check (--slug <slug> | --id <guid>)
if (args.Contains("--timeline-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await TimelineCheckCli.RunAsync(args, sp);
    return;
}

// CLI mode: set the ParentNodeId on an existing node (move it into a collection).
// X-Ray scene assembly (RFC 0002): print the entity roster + voice context block
// for a beat or raw prose. CLI twin of the MCP tool assemble_scene_context.
//   prose --assemble-scene (--beat <guid> | --text "<prose>") [--budget N]
if (args.Contains("--assemble-scene"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AssembleSceneCli.RunAsync(args, sp);
    return;
}

//   prose --reparent-node (--slug <slug> | --id <id>) (--parent-slug <slug> | --parent-id <id>)
//   prose --reparent-node --slug <slug> --clear   — detach from parent
if (args.Contains("--reparent-node"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReparentNodeCli.RunAsync(args, sp);
    return;
}


// CLI mode: render the WHOLE node as one continuous audiobook (one TTS pass,
// tiered to ElevenLabs limits — one request, else per-chapter, else split) and
// drop the MP3 in Downloads. The headless twin of the "Export Audio" button.
//   prose --record | --export-audio | --export-mp3 | --publish-audiobook
//      (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--publish-audiobook") || args.Contains("--record") || args.Contains("--export-audio") || args.Contains("--export-mp3"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PublishAudiobookCli.RunAsync(args, sp);
    return;
}

// CLI mode: codify the GLMZ house voice + world rules from the memory rubric into
// the DB stores the generator reads (literary_rules / tone_bible). De-fragilizes
// the rules so they no longer depend on an .md file being parsed. Idempotent.
//   prose --seed-voice-rules
if (args.Contains("--seed-voice-rules"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedVoiceRulesCli.RunAsync(args, sp);
    return;
}

// CLI mode: extract a time / elapsed-duration timeline from all beats in a node.
// Flags clock anchors, infers story-relative timestamps, and surfaces conflicts.
//   prose --timeline (--slug <slug> | --id <id>)
if (args.Contains("--timeline"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await TimelineCli.RunAsync(args, sp);
    return;
}

// CLI mode: print beat text WITH its authoritative POV character attached (sourced fresh
// from BeatEntityPresence every call, never inferred from prose content). Use this instead
// of raw sqlcmd/SELECT Text reads whenever a conclusion about character voice, attribution,
// or continuity will be drawn from what's read — see ReadBeatsCli's own doc comment for the
// live mistake (2026-08-10, VIGL multi-POV misattribution) this exists to make structurally
// harder to repeat.
//   prose --read-beats --slug <slug> (--from <N> --to <N> | --numbers <csv>)
if (args.Contains("--read-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReadBeatsCli.RunAsync(args, sp);
    return;
}

// CLI mode: per-entity-type reachability matrix (how much canon is embedded and
// thus pullable into prose). The standing gap-finder.
//   prose --coverage
if (args.Contains("--coverage"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CoverageCli.RunAsync(args, sp);
    return;
}

// CLI mode: (re)build the materialized character read-model projection from the
// relational source of truth. Run after a bulk import / relational migration,
// or whenever ReadModelVersion is bumped. Backfills missing/stale rows, prunes
// orphans. The steady-state path self-heals, so this is a one-time / maintenance op.
//   prose --rebuild-readmodel [--archived]
if (args.Contains("--rebuild-readmodel"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebuildReadModelCli.RunAsync(args, sp);
    return;
}

// CLI mode: create a runtime-defined repository (custom entity type).
//   prose --create-repository --name "Artifacts" [--category World] [--icon bi-box] [--description "..."]
if (args.Contains("--create-repository"))
{
    string ArgVal(string flag) { var i = Array.IndexOf(args, flag); return i >= 0 && i + 1 < args.Length ? args[i + 1] : ""; }
    var repoName = ArgVal("--name");
    if (string.IsNullOrWhiteSpace(repoName)) { Console.Error.WriteLine("[create-repository] --name is required."); Environment.ExitCode = 1; return; }
    var sp = BuildCoreServices(args);
    var svc = sp.GetRequiredService<Prose.Core.Services.RepositoryDefinitionService>();
    try
    {
        var def = svc.Create(repoName, ArgVal("--category"), ArgVal("--icon"), ArgVal("--description"));
        Console.WriteLine($"[create-repository] Created '{def.Name}' — slug '{def.Slug}', category {def.Category}, route {def.RoutePath}.");
    }
    catch (Exception ex) { Console.Error.WriteLine($"[create-repository] FAILED: {ex.Message}"); Environment.ExitCode = 1; }
    return;
}

// Table-driven: each --rebuild-*-relational flag maps to its CLI handler. ADDITIVE — Records.Json is never modified. (RFC 0007)
{
    var rebuildRelational = new Dictionary<string, Func<string[], IServiceProvider, Task<int>>>(StringComparer.Ordinal)
    {
        ["--rebuild-faction-relational"]        = RebuildFactionRelationalCli.RunAsync,
        ["--rebuild-quote-relational"]          = RebuildQuoteRelationalCli.RunAsync,
        ["--rebuild-news-relational"]           = RebuildNewsRelationalCli.RunAsync,
        ["--rebuild-contract-relational"]       = RebuildContractRelationalCli.RunAsync,
        ["--rebuild-vocabulary-relational"]     = RebuildVocabularyRelationalCli.RunAsync,
        ["--rebuild-archetype-relational"]      = RebuildArchetypeRelationalCli.RunAsync,
        ["--rebuild-genemod-relational"]        = RebuildGenemodRelationalCli.RunAsync,
        ["--rebuild-material-relational"]       = RebuildMaterialRelationalCli.RunAsync,
        ["--rebuild-psionic-relational"]        = RebuildPsionicRelationalCli.RunAsync,
        ["--rebuild-motif-relational"]          = RebuildMotifRelationalCli.RunAsync,
        ["--rebuild-lab-specimen-relational"]   = RebuildLabSpecimenRelationalCli.RunAsync,
        ["--rebuild-flyover-entity-relational"] = RebuildFlyoverEntityRelationalCli.RunAsync,
        ["--rebuild-automaton-relational"]      = RebuildAutomatonRelationalCli.RunAsync,
        ["--rebuild-ammunition-relational"]     = RebuildAmmunitionRelationalCli.RunAsync,
        ["--rebuild-transportation-relational"] = RebuildTransportationRelationalCli.RunAsync,
        ["--rebuild-corponation-relational"]    = RebuildCorponationRelationalCli.RunAsync,
        ["--rebuild-equipment-relational"]      = RebuildEquipmentRelationalCli.RunAsync,
        ["--rebuild-technology-relational"]     = RebuildTechnologyRelationalCli.RunAsync,
        ["--rebuild-pharmaceutical-relational"] = RebuildPharmaceuticalRelationalCli.RunAsync,
        ["--rebuild-cyberware-relational"]      = RebuildCyberwareRelationalCli.RunAsync,
        ["--rebuild-consumer-good-relational"]  = RebuildConsumerGoodRelationalCli.RunAsync,
        ["--rebuild-synthetic-relational"]      = RebuildSyntheticRelationalCli.RunAsync,
        ["--rebuild-place-relational"]          = RebuildPlaceRelationalCli.RunAsync,
        ["--rebuild-document-relational"]       = RebuildDocumentRelationalCli.RunAsync,
        ["--rebuild-entertainment-relational"]  = RebuildEntertainmentRelationalCli.RunAsync,
        ["--rebuild-weapon-relational"]         = RebuildWeaponRelationalCli.RunAsync,
        ["--rebuild-apparel-relational"]        = RebuildApparelRelationalCli.RunAsync,
        ["--rebuild-subsidiary-relational"]     = RebuildSubsidiaryRelationalCli.RunAsync,
    };
    if (Array.Find(args, a => rebuildRelational.ContainsKey(a)) is { } rebuildVerb)
    {
        var sp = BuildCoreServices(args);
        Environment.ExitCode = await rebuildRelational[rebuildVerb](args, sp);
        return;
    }
}

// CLI mode: materialize relational rows for active characters that are blob-only
// (no Characters row) — the no-data-loss gate before dropping the Character blob. (RFC 0007)
//   prose --backfill-missing-characters
if (args.Contains("--backfill-missing-characters"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BackfillMissingCharactersCli.RunAsync(args, sp);
    return;
}


// CLI mode: RFC 0007 unified blob-retirement gate — backfill all 29 relational types
// from Records.Json, validate, and delete the blobs in a single pass. (RFC 0007)
//   prose --retire-records-blobs [--rebuild] [--validate] [--apply]
if (args.Contains("--retire-records-blobs"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RetireRecordsBlobsCli.RunAsync(args, sp);
    return;
}

// CLI mode: split a monolithic node into a Collection (parent + chapter
// child nodes) at IsChapterStart boundaries. Backs up to markdown first.
//   prose --split-collection (--slug <s> | --id <guid>)
if (args.Contains("--split-collection"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SplitCollectionCli.RunAsync(args, sp);
    return;
}

// CLI mode: print the voice context the generator/re-beater receive — the
// verification that the canon-trained voice is wired into prompts.
//   prose --print-voice
if (args.Contains("--print-voice"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PrintVoiceCli.RunAsync(args, sp);
    return;
}

// CLI mode: print all beats of a node as continuous prose to stdout.
// No headers, no beat numbers, no metadata — just the prose, beats separated by blank lines.
//   prose --sanitize-beats [--slug <slug> | --all] [--dry-run]
if (args.Contains("--sanitize-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SanitizeBeatsCli.RunAsync(args, sp);
    return;
}

//   prose --print-book (--id <guid|prefix> | --slug <slug>)
if (args.Contains("--print-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PrintNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: rebuild a node's beats to the codified beat doctrine via LLM
// re-segmentation (story beats + dialogue/'?' mechanics + gaps). Dry-run by
// default; --apply backs up to markdown then replaces beats if the word-retention
// guard passes. --all targets every doctrine-violating node.
//   prose --rebeat-book (--slug <s> | --id <guid> | --all) [--apply]
if (args.Contains("--rebeat-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RebeatNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: sweep a node's prose against canon (all entity types) and queue
// contradictions as approval-gated findings — the self-correction pass.
//   prose --check-canon (--slug <s> | --id <guid> | --all)
if (args.Contains("--check-canon"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CheckCanonCli.RunAsync(args, sp);
    return;
}

// CLI mode: show what the universal canon reach pulls for a query, across ALL
// entity types — verifies the full-interconnect retrieval path.
//   prose --canon-retrieve "<query>" [--k N] [--types t1,t2]
if (args.Contains("--canon-retrieve"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CanonRetrieveCli.RunAsync(args, sp);
    return;
}

// CLI mode: author-only Canon trust gate — mark a node strong enough to draw
// conclusions about its characters/events (the voice-harvest learns from canon).
//   prose --mark-canon (--slug <s> | --id <guid>) [--off]
if (args.Contains("--mark-canon"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MarkCanonCli.RunAsync(args, sp);
    return;
}

// CLI mode: distill voice rules from winning (≥80%) nodes into the codified
// DB-backed rules the generator reads. Propose-then-approve.
//   prose --harvest-voice (--slug <s> | --id <id> | --all-80 | --pending | --apply <guid> | --reject <guid>) [--force]
if (args.Contains("--harvest-voice"))
{
    var sp = BuildCoreServices(args);
    var (proceedHv, estHv) = await CostGateCli.ConfirmAsync("--harvest-voice", args, sp);
    if (!proceedHv) return;
    var beforeHv = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await HarvestVoiceCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--harvest-voice", estHv, beforeHv, sp);
    return;
}

// CLI mode: list every node as a table (or JSON). Headless twin of /nodes.
//   prose --list-books [--status <s>] [--kind <k>] [--search <text>] [--limit <n>] [--json]
if (args.Contains("--list-books"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ListNodesCli.RunAsync(args, sp);
    return;
}

//   prose --kdp-status
//   Show KDP publication status: Published / Outdated / WorkInProgress for all tracked nodes.
//   Outdated = published but beats edited since last KDP push.
if (args.Contains("--kdp-status"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await KdpStatusCli.RunAsync(args, sp);
    return;
}

//   prose --kdp-manifest [--out <path>] [--userscript]
//   Reconciles DB + disk + tools/kdp/title-ids.json into tools/kdp/manifest.json (the ground
//   truth for what needs to go up on KDP). --userscript also regenerates
//   tools/kdp/kdp-panel.user.js from tools/kdp/kdp-panel.template.js.
if (args.Contains("--kdp-manifest"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await KdpManifestCli.RunAsync(args, sp);
    return;
}

//   prose --kdp-mark-published --slug <slug> [--url <amazonUrl>] [--title-id <id>]
//   Closes the loop after a republish actually completes on KDP.
if (args.Contains("--kdp-mark-published"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await KdpMarkPublishedCli.RunAsync(args, sp);
    return;
}

// CLI mode: render a node to Markdown or PDF in Downloads.
// Markdown output embeds <!-- beat:N:id7 --> markers for prose --import-md round-trip.
//   ss (--publish-md | --publish-pdf) (--id <guid|prefix> | --slug <slug>) [--author "Name"]
if (args.Contains("--publish-md") || args.Contains("--publish-pdf"))
{
    var sp = BuildCoreServices(args);
    var format = args.Contains("--publish-md") ? PublishManuscriptCli.Format.Markdown
               : PublishManuscriptCli.Format.Pdf;
    Environment.ExitCode = await PublishManuscriptCli.RunAsync(args, sp, format);
    return;
}

// CLI mode: reimport an edited --publish-md Markdown file back into the DB. Each
// <!-- beat:N:id7 --> marker identifies the beat; prose between markers updates Beat.Text.
//   prose --import-md --file path.md [--dry-run]
if (args.Contains("--import-md"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImportMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: bounded copy-edit of a node — proper paragraph/dialogue spacing, a
// "?" on questions that lack one, and "asks"/"asked" (not "says") on question
// dialogue. Dry-run by default; --apply commits. Beats edited beyond those bounds
// are rejected (word-token guard) and left untouched.
//   prose --reflow-book (--id <guid|prefix> | --slug <slug>) [--apply]
if (args.Contains("--reflow-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReflowNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: deep-duplicate a node (and its sub-node tree) into a fresh,
// independent copy — every beat cloned to a new row (prose + metadata kept;
// audio/score/stale reset). Editing the copy never touches the original.
//   prose --duplicate-book (--id <guid|prefix> | --slug <slug>) --title "New Title"
if (args.Contains("--duplicate-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DuplicateNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: import a hand-authored .node file (beat + gap + beat …) into a
// fresh node. The complement to --write-story (LLM-generated): this is for
// drafts written elsewhere (chat exports, transcripts, paper notes typed up).
// See ImportNodeCli class doc for the file format.
//   prose --import-book --file path.node [--title ...] [--kind ...] [--slug ...] [--parent ...] [--dry-run]
if (args.Contains("--import-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImportNodeCli.RunAsync(args, sp);
    return;
}

// CLI mode: import a local image file (png, jpg, webp) into the Media table.
// Optionally links to a node by --book-code and sets the media type.
//   prose --import-cover --file PATH [--book-code CODE] [--type TYPE] [--notes TEXT] [--dry-run]
if (args.Contains("--import-cover"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ImportCoverImageCli.RunAsync(args, sp);
    return;
}


// CLI mode: burst oversized beats (e.g. chapter-as-one-beat from old book
// imports) into paragraph-sized pieces. Idempotent — already-small beats
// are skipped on rerun.
//   prose --burst-beats [--min-chars 800] [--node slug] [--kind book] [--dry-run]
if (args.Contains("--burst-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BurstBeatsCli.RunAsync(args, sp);
    return;
}

// CLI mode: report flat-vs-bridge drift for a denormalised column.
//   prose --audit-denorm Entities.TagsJson
//   prose --audit-denorm Characters.Affiliation
if (args.Contains("--audit-denorm"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AuditDenormCli.RunAsync(args, sp);
    return;
}

// CLI mode: findings inbox — list / show / apply / dismiss / scan.
if (args.Contains("--findings"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await FindingsCli.RunAsync(args, sp);
    return;
}

// prose --entity-tree (--id <guid> | --slug <slug>) [--depth N] [--rel-types type1,type2] [--as-of date]
if (args.Contains("--entity-tree"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EntityTreeCli.RunAsync(args, sp);
    return;
}

// prose --prose-check (--slug <nodeSlug> | --id <beatId>) [--all] [--json]
if (args.Contains("--prose-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseCheckCli.RunAsync(args, sp);
    return;
}

// prose --compute-metrics [--slug <slug> | --all]
// CPU-only per-beat prose quality metrics: word count, sentence count, TTR,
// MTLD lexical diversity, Flesch-Kincaid readability, dialogue proportion.
// Upserts into BeatProseMetrics. Safe to re-run nightly. Exit 0 = success.
if (args.Contains("--compute-metrics"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BeatProseMetricsCli.RunAsync(args, sp);
    return;
}

// prose --beat-granularity [--slug <slug> | --code <code> | --all] [--beats]
// Analyses beat-size distribution against the 4,000–7,500 char optimal range.
// Labels each beat as OK / SPLIT / MERGE and prints per-story stats.
// CPU-only — no LLM calls. Exit 0 = success.
if (args.Contains("--beat-granularity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BeatGranularityCli.RunAsync(args, sp);
    return;
}

// prose --consistency-audit [--since <hours>]
// Surfaces factual contradictions that span multiple story nodes by querying
// the existing ContinuityClaims table. CPU-only — no LLM calls.
// Exit 0 = clean, 1 = conflicts found.
if (args.Contains("--consistency-audit"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CrossBookConsistencyAuditCli.RunAsync(args, sp);
    return;
}

// prose --morning-report [--since <hours>]
// Aggregates overnight findings: cross-story contradictions, new Findings,
// prose metrics outliers, near-duplicate alerts, score correlation, leaderboard.
// Writes HTML to PublishExportDirectory. Default window: 24h. Exit 0 always.
if (args.Contains("--morning-report"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await MorningReportCli.RunAsync(args, sp);
    return;
}

// prose --prose-health [--slug <nodeSlug>] [--json] [--out <dir>]
// Zero-cost overnight health scan: surface stats + kNN score prediction +
// semantic outlier detection using cached ProseEmbeddings. No API calls.
if (args.Contains("--prose-health"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ProseHealthCli.RunAsync(args, sp);
    return;
}

// prose --check-fidelity (--slug <nodeSlug> | --id <nodeId>) [--json]
// Detects the Semantic Fidelity Gap — beats scoring high but drifting from the
// story's original meaning (Goodhart's Law in prose). Two checks:
//   Bible alignment: prose vs Seed/Description (north-star drift)
//   Intent alignment: prose vs beat Description (purpose drift)
// Files SEMANTIC-DRIFT findings; also runs automatically after every review.
if (args.Contains("--check-fidelity"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CheckFidelityCli.RunAsync(args, sp);
    return;
}

// prose --world-state --beat <beatId> [--story-time "date"] [--json]
if (args.Contains("--world-state"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WorldStateCli.RunAsync(args, sp);
    return;
}

// prose --gear-check --slug <nodeSlug> --character <characterId> [--story-time date]
if (args.Contains("--gear-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await GearCheckCli.RunAsync(args, sp);
    return;
}

// prose --write-outline --slug <nodeSlug> [--json]
// Generates a beat-by-beat narrative outline (act-grouped, one sentence per beat).
// For a logic check, use --logic-sweep instead.
if (args.Contains("--write-outline"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WriteOutlineCli.RunAsync(args, sp);
    return;
}

// prose --logic-sweep --slug <nodeSlug> [--json]
// Codifies docs/LOGIC.md's six-dimension sweep (SS-A44) as one LLM call per dimension:
// causality chain, knowledge states, timeline, plant/payoff (two-way), orphan references,
// bible agreement. A single-pass approximation over the whole node's prose — for a large
// book or a thorough pass, prefer the /logic-sweep Claude Code skill (range-scoped
// subagents + quote verification + fix + re-verify). Findings persist to Findings and
// auto-heal on re-run. Exit 0 = clean, 1 = MODERATE/MINOR only, 2 = any BLOCKER.
if (args.Contains("--logic-sweep"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await LogicSweepCli.RunAsync(args, sp);
    return;
}

// prose --dcm-backfill --slug <slug> [--dry-run]
// Retroactive DCM footprint for books written OUTSIDE the engine (update_beat_text /
// --edit-beat / --import-md bypass ProseWriterRouter, so step-0 entity inference never
// ran — PURSUED shipped 127 beats with zero entity docs this way). Runs
// EntityDocService.InferFromTextAsync over every enabled beat's prose; hash-gated,
// no prose touched. Run after --generate-node-doc + --sync-markdown.
if (args.Contains("--dcm-backfill"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DcmBackfillCli.RunAsync(args, sp);
    return;
}

// prose --reader-qa (--slug <slug> | --all) [--force] [--json]
// Reader-Proxy QA (docs/READER-QA.md) — the default reader-facing quality instrument.
// Phase 1: comprehension probes — a cheap model reads each chapter cold, diffed against
// the Sonnet synopsis ground truth, Sonnet-arbitrated, filed as ComprehensionDefect
// findings. NO scores (measurement, not vote — SS-A44 exempt). Hash-cached per chapter.
// Exit 0 = clean, 1 = defects found, 2 = error.
if (args.Contains("--reader-qa"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReaderQaCli.RunAsync(args, sp);
    return;
}

// prose --craft-checklist --slug <slug> [--force] [--json]
// Reader-Proxy QA Instrument 2: binary craft/delight checklist per beat, hash-gated on
// Beat.TextHash + rule-set version (unchanged beats never re-bill). CRAFT §8 DON'Ts +
// "≥1 applicable DELIGHT move" + book-level move-monotony counters (DELIGHT §14).
// Findings persist as CraftChecklist. No scores. Exit 0 = clean, 1 = findings, 2 = error.
if (args.Contains("--craft-checklist"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BeatChecklistCli.RunAsync(args, sp);
    return;
}

// prose --diagnose-book --slug <nodeSlug> [--json]
// Pre-flight structural analysis before running the review panel.
// Runs 12 targeted checks (antagonist cost, protagonist behavior change,
// exposition density, etc.) and reports Pass/Warn/Fail with evidence + fixes.
// Exit 0 = ready, 1 = warnings, 2 = blocking failures.
if (args.Contains("--diagnose-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DiagnoseNodeCli.RunAsync(args, sp);
    return;
}

// prose --check-duplicate-beats --slug <nodeSlug> [--threshold 0.90] [--json]
// Corpus-wide near-duplicate-scene detector over prose embeddings (BeatDuplicateService).
// Candidate generator, not a verdict — verify by reading both beats before acting.
if (args.Contains("--check-duplicate-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CheckDuplicateBeatsCli.RunAsync(args, sp);
    return;
}

// prose --examine-emotion --slug <nodeSlug> [--effort draft|standard|deep] [--json]
// Emotional Intelligence Examination (SS-A15): 8-dimension 0–4 rubric, per-beat curve,
// character ledger (Want/Need/Wound/Flaw), register-adaptive anchors.
// Exit 0 = none blocking, 1 = advisory issues, 2 = blocking dimensions open.
if (args.Contains("--examine-emotion"))
{
    var sp = BuildCoreServices(args);
    var (proceedEe, estEe) = await CostGateCli.ConfirmAsync("--examine-emotion", args, sp);
    if (!proceedEe) return;
    var beforeEe = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await ExamineEmotionCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--examine-emotion", estEe, beforeEe, sp);
    return;
}

// Scene Collision engine manual test harness (2026-08-10): runs SceneCollisionService against
// one real beat without a full ProseWriterRouter pass. See SimulateCollisionCli for details.
if (args.Contains("--simulate-collision"))
{
    var sp = BuildCoreServices(args);
    var (proceedSc, estSc) = await CostGateCli.ConfirmAsync("--simulate-collision", args, sp);
    if (!proceedSc) return;
    var beforeSc = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await SimulateCollisionCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--simulate-collision", estSc, beforeSc, sp);
    return;
}

// prose --causality-check / --affect-check / --interpersonal-check --slug <slug> [--json]
// "Behave like people" beat lenses: cause-effect (kill "and then"), emotion→action,
// and verbal+non-verbal interpersonal dynamics (the 90+ relational lever).
if (args.Contains("--causality-check") || args.Contains("--affect-check") || args.Contains("--interpersonal-check"))
{
    var lens = args.Contains("--causality-check") ? "causality"
             : args.Contains("--affect-check") ? "affect" : "interpersonal";
    var cmdLens = $"--{lens}-check";
    var sp = BuildCoreServices(args);
    var (proceedLens, estLens) = await CostGateCli.ConfirmAsync(cmdLens, args, sp);
    if (!proceedLens) return;
    var beforeLens = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await BeatLensCli.RunAsync(args, sp, lens);
    await CostGateCli.RecordActualAsync(cmdLens, estLens, beforeLens, sp);
    return;
}

// prose --list-species — print the species taxonomy (canonical name, label, sentience).
if (args.Contains("--list-species"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = ListSpeciesCli.Run(sp);
    return;
}

// prose --behavior-check --slug <nodeSlug> --character <characterId>
if (args.Contains("--behavior-check"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BehaviorCheckCli.RunAsync(args, sp);
    return;
}

// prose --weapon-network (--id <weaponId> | --character <characterId> [--as-of date])
if (args.Contains("--weapon-network"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await WeaponNetworkCli.RunAsync(args, sp);
    return;
}

// prose --ambient-palette --character <characterId> [--as-of date]
if (args.Contains("--ambient-palette"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AmbientPaletteCli.RunAsync(args, sp);
    return;
}

// prose --seed-sensory-hints [--list] [--weapon "Name" --hints "hint1; hint2"] [--force]
if (args.Contains("--seed-sensory-hints"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SeedSensoryHintsCli.RunAsync(args, sp);
    return;
}

// prose --beat <subcommand> — fine-grained beat manipulation:
//   insert  --node <slug|id> [--after <beatId>] [--text "..."]
//   delete  --id <beatId> [--node <slug|id>]
//   update  --id <beatId> --text "..."  (use '-' for stdin)
//   meta    --id <beatId> [--title "..."] [--kind "..."] [--description "..."] [--tone "..."] ...
//   show    --id <beatId>
//   list    --node <slug|id>
if (args.Contains("--beat"))
{
    var sp = BuildCoreServices(args);
    var beatArgs = args.SkipWhile(a => a != "--beat").Skip(1).ToArray();
    Environment.ExitCode = await BeatCli.RunAsync(beatArgs, sp);
    return;
}

// prose --delete-node --id <guid>   Hard-delete a node and its BeatNode memberships.
// Beats that are exclusively owned by this node are also deleted.
// HARD RULE: never use raw sqlcmd DELETE on Nodes — use this command instead.
if (args.Contains("--delete-node"))
{
    var idStr = args.SkipWhile(a => a != "--id").Skip(1).FirstOrDefault();
    if (!Guid.TryParse(idStr, out var deleteNodeId))
    {
        Console.Error.WriteLine("Usage: prose --delete-node --id <guid>");
        Environment.ExitCode = 1;
        return;
    }
    var sp = BuildCoreServices(args);
    await using var scope = sp.CreateAsyncScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Prose.Core.Data.ProseDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    var target = await db.Nodes.FindAsync(deleteNodeId);
    if (target == null) { Console.Error.WriteLine($"Node {deleteNodeId} not found."); Environment.ExitCode = 1; return; }

    // 2026-08-09 bug fix: this used to cascade exactly one level deep ("nested chapters
    // are not supported", per the comment it replaces) — a child that was itself a split
    // Collection (chapter -> N sub-chapters -> beats, e.g. any book split via
    // --split-collection) left its own grandchildren untouched, so db.Nodes.Remove on that
    // mid-level chapter would hit FK_Nodes_ParentNode (grandchildren still reference it).
    // Deletion removes NODES themselves, not just beats, so the fix isn't the usual
    // GetLeafDescendantIdsAsync swap (that returns only leaves) — it needs a genuinely
    // recursive, depth-first, POST-order walk: fully delete every child's own subtree
    // before deleting the child, at any depth, then finally the target itself.
    async Task DeleteNodeSubtreeAsync(Guid id, int depth)
    {
        var childIds = await db.Nodes.Where(n => n.ParentNodeId == id).Select(n => n.Id).ToListAsync();
        foreach (var childId in childIds)
            await DeleteNodeSubtreeAsync(childId, depth + 1);

        var beatIds = await db.BeatNodes.Where(bn => bn.NodeId == id).Select(bn => bn.BeatId).ToListAsync();
        var sharedIds = await db.BeatNodes.Where(bn => beatIds.Contains(bn.BeatId) && bn.NodeId != id).Select(bn => bn.BeatId).Distinct().ToListAsync();
        var exclusiveIds = beatIds.Except(sharedIds).ToList();

        var blueprintIds = await db.NodeStructuralBlueprints.Where(bp => bp.NodeId == id).Select(bp => bp.Id).ToListAsync();
        if (blueprintIds.Count > 0)
        {
            db.NodeStructuralBlueprintBeatTags.RemoveRange(await db.NodeStructuralBlueprintBeatTags.Where(t => blueprintIds.Contains(t.BlueprintId)).ToListAsync());
            db.NodeStructuralBlueprints.RemoveRange(await db.NodeStructuralBlueprints.Where(bp => blueprintIds.Contains(bp.Id)).ToListAsync());
        }

        db.BeatNodes.RemoveRange(await db.BeatNodes.Where(bn => bn.NodeId == id).ToListAsync());
        if (exclusiveIds.Count > 0)
        {
            var beats = await db.Beats.Where(b => exclusiveIds.Contains(b.Id)).ToListAsync();
            db.Beats.RemoveRange(beats);
            Console.WriteLine($"  {new string(' ', depth * 2)}Deleting {beats.Count} exclusive beat(s) for {id}.");
        }

        var node = await db.Nodes.FindAsync(id);
        if (node != null)
        {
            db.Nodes.Remove(node);
            Console.WriteLine($"  {new string(' ', depth * 2)}→ {node.Title} ({id})");
        }
    }

    await DeleteNodeSubtreeAsync(deleteNodeId, 0);
    await db.SaveChangesAsync();
    Console.WriteLine($"[delete-node] Deleted: {target.Title} ({deleteNodeId})");
    return;
}

// prose --wound <subcommand> — character wound ledger:
//   list    --character <id|name> [--as-of "date"]
//   log     --character <id|name> --description "..." [--location "chest"] [--severity moderate] ...
//   status  --wound <id> --status active|healed|noted
if (args.Contains("--wound"))
{
    var sp = BuildCoreServices(args);
    var woundArgs = args.SkipWhile(a => a != "--wound").Skip(1).ToArray();
    Environment.ExitCode = await WoundCli.RunAsync(woundArgs, sp);
    return;
}

// CLI mode: harvest entities + edges from open text (design notes, canon briefs).
// Routed BEFORE the bare --universe command: --universe here is the scope flag, not a subcommand.
//   prose --harvest-entities --file <path> [--universe glmz] [--dry-run]
if (args.Contains("--harvest-entities"))
{
    var sp = BuildCoreServices(args);
    var (proceedHe, estHe) = await CostGateCli.ConfirmAsync("--harvest-entities", args, sp);
    if (!proceedHe) return;
    var beforeHe = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await HarvestEntitiesCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--harvest-entities", estHe, beforeHe, sp);
    return;
}

// prose --universe <subcommand> — universe management:
//   list      Print all universes
//   current   Print the active universe
//   use       --slug <slug> | --id <guid>
// Only hijacks dispatch when --universe is the PRIMARY command (args[0]) AND is followed by a
// real universe subcommand. Elsewhere in argv, --universe <slug> is the scoping flag other
// commands accept (parsed at line 28 into UniverseBootstrap.RequestedSlug) —
// args.Contains("--universe") would incorrectly steal dispatch from every command block defined
// after this one (e.g. --coordinate).
//
// The subcommand check matters because --universe is ALSO valid in first position as a scoping
// flag: `prose --universe source --export-node --slug x` is a legitimate export, not a malformed
// universe command. Matching on args[0] alone swallowed those silently — UniverseCli printed its
// usage text and the real command never ran, which looks like a no-op rather than an error.
// Bare `prose --universe` still lands here (args.Length == 1) so it prints usage instead of
// falling through the whole dispatch chain and exiting silently.
if (isUniverseManagementCommand)
{
    var sp = BuildCoreServices(args);
    var uniArgs = args.Skip(1).ToArray();
    Environment.ExitCode = await UniverseCli.RunAsync(uniArgs, sp);
    return;
}

// prose --review-settings [--set <key> <value>] — view or update review voting settings.
// Keys: ballots, prose, panel, readers, max-concurrency, judge-provider, allowed-providers
if (args.Contains("--review-settings"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ReviewSettingsCli.RunAsync(args, sp);
    return;
}

// prose --get <type> <name-or-id> — targeted entity lookup.
// Types: character | place | weapon | faction | corponation
if (args.Contains("--get"))
{
    var sp = BuildCoreServices(args);
    var getArgs = args.SkipWhile(a => a != "--get").Skip(1).ToArray();
    Environment.ExitCode = await GetEntityCli.RunAsync(getArgs, sp);
    return;
}

// CLI mode: sync project-rule, Codex, and Claude Code memory .md files to DB.
// Upserts by RelativePath; only changed files (hash diff) produce a history row.
//   prose --sync-markdown [--dry-run]
if (args.Contains("--sync-markdown"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SyncMarkdownCli.RunAsync(args, sp);
    return;
}


// CLI mode: restore .md files from DB back to disk. Supports point-in-time
// recovery from the MarkdownFiles_History temporal table.
//   prose --restore-markdown [--file <relativePath>] [--as-of <datetime-utc>] [--dry-run] [--list]
if (args.Contains("--restore-markdown"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RestoreMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: keyword recall — call up (print) or create (--to-disk) the select few
// tracked .md files relevant to a topic, straight from the DB.
//   prose --recall <keyword> [--content] [--to-disk] [--as-of <datetime-utc>]
if (args.Contains("--recall"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RecallMarkdownCli.RunAsync(args, sp);
    return;
}

// CLI mode: Doc Context Stack dry-run — print the rotating cast of .md docs that WOULD
// load for a node + optional scene text (tier, reason, score, budget). Read-only.
//   prose --doc-context --slug <node> [--goal "<text>"] [--budget <tokens>]
// CLI mode: manage user context overrides for the DocContextStack.
//   prose --context add     --doc <path|guid> [--node <slug>]   Pin doc into prompts
//   prose --context exclude --doc <path|guid> [--node <slug>]   Exclude doc
//   prose --context remove  --doc <path|guid> [--node <slug>]   Remove override
//   prose --context clear   [--node <slug>]                     Clear all overrides
//   prose --context status                                       Show active overrides
if (args.Contains("--context"))
{
    var sp = BuildCoreServices(args);
    var ctxArgs = args.SkipWhile(a => a != "--context").Skip(1).ToArray();
    Environment.ExitCode = await ContextCli.RunAsync(ctxArgs, sp);
    return;
}

// prose --liberty-report [--beat <guid> | --slug <slug>]
// Show liberty analysis + Rule of Cool findings for a beat or all beats in a story.
if (args.Contains("--liberty-report"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await LibertyReportCli.RunAsync(args, sp);
    return;
}

if (args.Contains("--doc-context-hook"))
{
    // UserPromptSubmit hook backend — stdout must contain ONLY the hook JSON, so kill logging.
    var sp = BuildCoreServicesNoLogging(args);
    Environment.ExitCode = await DocContextHookCli.RunAsync(args, sp);
    return;
}

// REMOVED 2026-06-24: `--refactor-telemetry` (bulk regenerate-beats-from-synopsis runner).
// It rebuilt finished beats from their one-line goals, discarding hand-crafted prose — proven
// to regress finished nodes (dual-read: surgical 80.8 > baseline 79.7 > regen 76.2). Doc/Entity
// context were validated separately and are KEPT; regen-from-synopsis is not a revision tool and
// is gone. New-beat generation lives in ProseWriterRouter.WriteAsync, untouched.

// CLI mode: dual-read comparative review — the SAME pinned panel grades both versions of a story;
// pairs scores per reader (within-reader delta cancels taste bias) → keep/revert/merge verdict.
//   prose --dual-read --old <slug|id> --new <slug|id> [--panel <name>] [--readers N]
if (args.Contains("--dual-read"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DualReadCli.RunAsync(args, sp);
    return;
}

if (args.Contains("--doc-context"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DocContextCli.RunAsync(args, sp);
    return;
}

// CLI mode: DCM lifecycle visualization — dry-run context pass + Gantt .htm export.
//   prose --dcm-viz --slug <slug> [--out <dir>]
if (args.Contains("--dcm-viz"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DcmVizCli.RunAsync(args, sp);
    return;
}

// CLI mode: backfill entity-doc MarkdownFiles rows for a book's characters.
//   prose --backfill-entity-docs --slug <slug> [--text]
// Replays EntityDocService.InferFromTextAsync over every beat goal (+ prose text with
// --text) so future prose generation and the DCM viz see per-character entity docs.
if (args.Contains("--backfill-entity-docs"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BackfillEntityDocsCli.RunAsync(args, sp);
    return;
}

// CLI mode: re-materialize the entity-doc row for EVERY active entity, in every universe.
//   prose --repair-entity-docs [--dry-run]
// Unlike --backfill-entity-docs (per-book, inference-driven, so it only reaches entities a
// given book mentions) this iterates the entity table itself — which is what stamping
// MarkdownFiles.UniverseId on all of them requires.
if (args.Contains("--repair-entity-docs"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await RepairEntityDocsCli.RunAsync(args, sp);
    return;
}

// prose --workflow-status [--slug <slug> | --all] [--json]
// Per-node or global prose service coverage matrix. Shows which services
// (Pacing, StoryMethodology, PlantPayoff, StoryAudit, Combat) were active
// when beats were written, and surfaces gaps where applicable services weren't used.
if (args.Contains("--workflow-status"))
{
    var sp = BuildCoreServices(args);
    await WorkflowMonitorCli.RunAsync(sp, args);
    return;
}

// prose --backfill-coverage --slug <book-or-chapter-slug>
// Populates BeatServiceLog + BeatModeLog for prose written before ProseWriterRouter
// existed, WITHOUT regenerating any beat. Runs the router's coverage-only path over
// each existing beat so --workflow-status has real logs to report.
if (args.Contains("--backfill-coverage"))
{
    var sp = BuildCoreServices(args);
    await BackfillCoverageCli.RunAsync(sp, args);
    return;
}

// prose --backfill-synopses --slug <s> [--model <id>] [--force]
// prose --backfill-structure-roles --slug <s> [--force]
// Fill missing beat metadata without touching prose. Synopses via LLM (BeatGoal proxy
// for mode detection); StructureRole deterministically by book-global Save-the-Cat arc.
if (args.Contains("--backfill-synopses") || args.Contains("--backfill-structure-roles"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BackfillBeatMetaCli.RunAsync(sp, args);
    return;
}

// prose --audit-book --slug <book-or-chapter-slug> [--deep] [--full] [--model <id>] [--out <path>] [--json]
// The "Player Piano" — one repeatable command running the full QA battery + the
// Structural Integrity Index (SII), a deterministic Findings rollup (BookHealthService).
// FREE (always): census/coverage/plant/prose/noun/timeline/verify/coordinate.
// DEEP (--deep): + examine-emotion/book-audit/diagnose/fidelity/logic-sweep/craft-checklist/
// check-canon/altitude-audit/reader-qa. FULL (--full, implies --deep): + storyscope/swain/chekhov.
// --model retargets the deep/full tier LLM calls (e.g. Haiku) for the run.
if (args.Contains("--audit-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await AuditNodeCli.RunAsync(sp, args);
    return;
}

// prose --book-audit --slug <nodeSlug> [--json]
// Audits a node against 7 commandments — gateway (PreviousNodeId=null) or
// sequel (PreviousNodeId set). Pass/warn/fail per commandment with fix hints.
// Exit 0 = all pass, 1 = advisory warnings, 2 = blocking failures.
if (args.Contains("--book-audit"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BookAuditCli.RunAsync(args, sp);
    return;
}

// prose --generate-blueprint --slug <nodeSlug> [--retrofit] [--json]
// Generates the StructuralBlueprint — pre-prose anti-tell commitments (subplot,
// temporal scheme, resolution mode, escalation curve, event palette, ending,
// intertextual anchors). StoryScope countermeasures; bible → blueprint → prose.
// --retrofit infers the blueprint from already-written prose.
if (args.Contains("--generate-blueprint"))
{
    var sp = BuildCoreServices(args);
    var (proceedGb, estGb) = await CostGateCli.ConfirmAsync("--generate-blueprint", args, sp);
    if (!proceedGb) return;
    var beforeGb = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await GenerateBlueprintCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--generate-blueprint", estGb, beforeGb, sp);
    return;
}

// prose --storyscope-audit --slug <nodeSlug> [--json]
// Verifies the book against measurable AI-fiction structural tells (StoryScope):
// flat escalation, event monoculture, moral gloss, emotion ratio, char-intro
// method, resolution mode, subplot execution, consensus clichés, TTCW originality.
// Findings triaged BLOCKER/MODERATE/MINOR; loop back into future beat prompts.
// Exit 0 = clean, 1 = moderate/minor, 2 = any blocker.
if (args.Contains("--storyscope-audit"))
{
    var sp = BuildCoreServices(args);
    var (proceedSsa, estSsa) = await CostGateCli.ConfirmAsync("--storyscope-audit", args, sp);
    if (!proceedSsa) return;
    var beforeSsa = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await StoryScopeAuditCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--storyscope-audit", estSsa, beforeSsa, sp);
    return;
}

// prose --chekhov-audit --slug <nodeSlug>
// Chekhov's Gun audit: extract all concrete props/anchors/traits and test whether
// each earns its place. ORPHANED = appears with no payoff; DECORATION = repeated
// without new function; EARNS_IT = each appearance serves a distinct narrative purpose.
// Run before trimming any prose detail.
if (args.Contains("--chekhov-audit"))
{
    var sp = BuildCoreServices(args);
    var (proceedCk, estCk) = await CostGateCli.ConfirmAsync("--chekhov-audit", args, sp);
    if (!proceedCk) return;
    var beforeCk = CostGateCli.SnapshotCost(sp);
    Environment.ExitCode = await ChekhovAuditCli.RunAsync(args, sp);
    await CostGateCli.RecordActualAsync("--chekhov-audit", estCk, beforeCk, sp);
    return;
}

// prose --duel --beat-id <guid> --candidate <file> [--goal "..."] [--apply] [--json]
// Blind A/B duel: beat's current prose vs a candidate revision. 3 voters
// (register/goal/reader lenses), three-way ballot; replace needs >=2 better
// with zero dissent; splits escalate to 7 voters with written rationales.
// Verdicts hash-cached by text pair. SS-A44: invoking this IS the explicit ask.
// Exit 0 = replace, 1 = keep, 2 = error.
if (args.Contains("--duel"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await BeatDuelCli.RunAsync(args, sp);
    return;
}


// prose --export-personas-json [--out <path>]
// Exports all 1024 Legion persona details + OCEAN psychometric profiles to JSON
// for consumption by the Python ML package (v3/ml/artifacts/personas.json).
if (args.Contains("--export-personas-json"))
{
    var outPath = args.SkipWhile(a => a != "--out").Skip(1).FirstOrDefault()
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "ml", "artifacts", "personas.json"));
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    var personas = MindAttic.Legion.PersonaLibrary.AllDetails
        .Select(p =>
        {
            var profile = MindAttic.Legion.PersonaLibrary.Profiles.TryGetValue(p.Id, out var pr) ? pr : null;
            var ocean   = profile?.Ocean;
            return new
            {
                p.Id, p.Archetype, p.Worldview, p.Background, p.Age, p.Quirk,
                Ocean = ocean == null ? null : new
                {
                    ocean.Openness, ocean.Conscientiousness, ocean.Extraversion,
                    ocean.Agreeableness, ocean.Neuroticism,
                },
            };
        });
    var json = System.Text.Json.JsonSerializer.Serialize(
        personas.ToList(),
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(outPath, json);
    Console.WriteLine($"Exported {MindAttic.Legion.PersonaLibrary.AllDetails.Count()} personas to {outPath}");
    return;
}

// prose --sanity-scan (--slug <slug|code> | --all) [--json]
// Deterministic prose checks — no LLM. Catches leaked internal node codes,
// undefined all-caps acronyms, encoding corruption, and heft-floor violations.
// Exit 0 = clean, 1 = warnings only, 2 = any blocks.
if (args.Contains("--sanity-scan"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SanityScanCli.RunAsync(args, sp);
    return;
}

// prose --duplicate-entity-scan --universe <slug> [--json]
// Deterministic scan for duplicate/near-duplicate character Entity names within a universe
// that aren't explained by legitimate cross-book OriginNodeId disambiguation. No LLM.
// Exit 0 = none found, 1 = candidates found (informational — read the prose before merging).
if (args.Contains("--duplicate-entity-scan"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await DuplicateEntityScanCli.RunAsync(args, sp);
    return;
}

// prose --plant-audit   --slug <node> [--json]   audit plant/payoff pairs
// prose --list-plants   --slug <node> [--json]   list all pairs
// prose --add-plant     --slug <node> --plant "..." --payoff "..." [--cat detail]
if (args.Contains("--plant-audit") || args.Contains("--list-plants") || args.Contains("--add-plant"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await PlantPayoffCli.RunAsync(args, sp);
    return;
}

// CLI mode: Will Storr narrative-science frameworks — sacred flaw, dramatic question,
// scene anatomy, five-act structure. Four subcommands:
//   prose --narrative-science sacred-flaw --character <slug|id> [--scaffold]
//   prose --narrative-science dramatic-question (--slug <s> | --id <beatId>) [--character <slug|id>]
//   prose --narrative-science scene-anatomy (--slug <s> | --id <beatId>)
//   prose --narrative-science five-act --slug <nodeSlug>
//   (add --json to any subcommand for raw JSON output)
if (args.Contains("--narrative-science"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await NarrativeScienceCli.RunAsync(args, sp);
    return;
}

// prose --clone-book (--id <guid> | --slug <slug>) [--title "New Title"] [--book-code SM1] [--draft] [--status ready]
if (args.Contains("--clone-book"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CloneNodeCli.RunAsync(args, sp);
    return;
}

// ── Edit Sessions ─────────────────────────────────────────────────────────────
// prose --start-session --slug <slug> --label "prose-pass-1" [--type prose-pass|gripes-cleanup|logic-sweep|custom]
if (args.Contains("--start-session"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await StartSessionCli.RunAsync(args, sp);
    return;
}

// prose --close-session (--slug <slug> | --session-id <guid>)
if (args.Contains("--close-session"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CloseSessionCli.RunAsync(args, sp);
    return;
}

// prose --list-sessions --slug <slug> [--limit N]
if (args.Contains("--list-sessions"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await ListSessionsCli.RunAsync(args, sp);
    return;
}

// prose --session-beats --session-id <guid>
if (args.Contains("--session-beats"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await SessionBeatsCli.RunAsync(args, sp);
    return;
}

// prose --sync-bible-from-session --session-id <guid> [--dry-run]
if (args.Contains("--sync-bible-from-session"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await SyncBibleFromSessionCli.RunAsync(args, sp);
    return;
}

// prose --sync-blueprint-from-session --session-id <guid>
if (args.Contains("--sync-blueprint-from-session"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await SyncBlueprintFromSessionCli.RunAsync(args, sp);
    return;
}

// prose --close-all-sessions
// Called by the /commit skill before every commit to flush open edit sessions,
// run bible + blueprint sync for each, and draw a clean 3B coordination boundary.
if (args.Contains("--close-all-sessions"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await CloseAllSessionsCli.RunAsync(args, sp);
    return;
}

// prose --coordinate --slug <slug> [--json <path>] [--no-stamp]
// Full-coverage bible↔blueprint↔beat coordination: correlate every beat's meaning,
// construction, and prose; emit JSON + stamp the "## Beat Coordination Index".
if (args.Contains("--coordinate"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CoordinateCli.RunAsync(args, sp);
    return;
}

// prose --ensure-chapter --slug <slug> | --all
// Enforce "every story has >= 1 chapter": wrap a flat story's direct beats into a
// single ChapterNode child (no-op if already chaptered). No LLM.
if (args.Contains("--ensure-chapter"))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await EnsureChapterCli.RunAsync(args, sp);
    return;
}

// prose --backfill-meaning --slug <slug> [--limit N] [--dry-run]
// Fill the MEANING coordinate (Beat.Description) for beats with prose but no meaning.
if (args.Contains("--backfill-meaning"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await BackfillMeaningCli.RunAsync(args, sp);
    return;
}

// prose --generate-event-list --slug <slug> [--force] [--limit N] [--dry-run] [--model <id>]
// Fill the per-beat plot-EVENT one-liner (Beat.EventSummary) — "what happened".
if (args.Contains("--generate-event-list"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await GenerateEventListCli.RunAsync(args, sp);
    return;
}

// prose --export-event-list --slug <slug>
// Write the current per-beat event list to {CODE}-Events.txt in the publish-export folder (no LLM call).
if (args.Contains("--export-event-list"))
{
    var sp = BuildServicesWithVault(args);
    Environment.ExitCode = await ExportEventListCli.RunAsync(args, sp);
    return;
}

// CLI mode: show running token cost tally for the current process.
//   prose --cost              print session cost table
//   prose --cost --json       emit summary as JSON
//   prose --cost --reset      clear the ledger
// When appended to another command (e.g. prose --write-node --slug foo --cost),
// the cost of that command's LLM calls is printed after the command finishes.
if (args.Contains("--cost") && (args.Length == 1 || (args.Length == 2 && (args.Contains("--json") || args.Contains("--reset")))))
{
    var sp = BuildCoreServices(args);
    Environment.ExitCode = await CostCli.RunAsync(args, sp);
    return;
}

// ────────────────────────────────────────────────────────────────────────────
// DI bootstrap helpers — replace WebApplication.CreateBuilder in every CLI block.
// Host.CreateDefaultBuilder loads appsettings.json + env vars but starts no
// HTTP server, no Kestrel, no Blazor middleware.
// ────────────────────────────────────────────────────────────────────────────

// Eagerly resolves IUniverseContext so its constructor sets UniverseScope.Current
// immediately (Prose.Core/Services/UniverseContext.cs line ~169), before any CLI
// dispatch block runs. This makes every command's universe scoping fully DB-driven off the
// live Universe table — the same path IUniverseContext already uses — instead of depending
// on whichever dispatch blocks happen to resolve IUniverseContext themselves. Adding a new
// universe (a new Universe row) needs no C# change anywhere after this: UniverseBootstrap.
// ResolveWellKnownId's hardcoded switch only still matters for a process that never calls
// this (there are none left, post-fix), so it's inert now rather than load-bearing.
// Cheap and safe pre-migration: the constructor doesn't touch the DB — catalog load is lazy
// and already has a try/catch fallback to an empty/no-op scope.
static IServiceProvider Finalize(IServiceProvider sp)
{
    var universe = sp.GetRequiredService<IUniverseContext>();

    // HARD RULE: an explicitly-requested --universe/PROSE_UNIVERSE slug that doesn't match any
    // registered universe must NEVER silently fall through to the persisted "current_universe"
    // default. Without this check, a typo (`--universe scyr`) or a slug that hasn't been
    // registered yet resolves to Guid.Empty in UniverseContext.EnsureLoaded's catalog lookup,
    // processOverride stays unset, and the command silently scopes to whatever the last human
    // left as default — the exact cross-universe bleed this rule exists to make impossible.
    // "Unacceptable" per user directive 2026-08-01: fail loud, never fail quiet.
    var requested = UniverseBootstrap.RequestedSlug ?? Environment.GetEnvironmentVariable("PROSE_UNIVERSE");
    if (!string.IsNullOrWhiteSpace(requested)
        && !string.Equals(universe.CurrentSlug, requested, StringComparison.OrdinalIgnoreCase))
    {
        var known = string.Join(", ", universe.ListUniverses().Select(u => u.Slug));
        Console.Error.WriteLine(
            $"[universe] Unknown universe slug '{requested}'. Registered universes: {known}. " +
            "Refusing to fall back to a default — pass a valid --universe slug.");
        Environment.Exit(2);
    }
    return sp;
}

static IServiceProvider BuildCoreServices(string[] args)
    => Finalize(Host.CreateDefaultBuilder(args)
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((_, svc) => svc.AddProseServices())
        .Build()
        .Services);

static IServiceProvider BuildCoreServicesNoLogging(string[] args)
    => Finalize(Host.CreateDefaultBuilder(args)
        .ConfigureLogging(lb => lb.ClearProviders())
        .ConfigureServices((_, svc) => svc.AddProseServices())
        .Build()
        .Services);

static IServiceProvider BuildServicesWithVault(string[] args)
    => Finalize(Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(cfg =>
            cfg.AddMindAtticVaultFiles())
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((ctx, svc) =>
        {
            SettingsService.VaultConfiguration = ctx.Configuration;
            svc.AddProseServices();
        })
        .Build()
        .Services);

static IServiceProvider BuildServicesWithVaultAndAuth(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(cfg =>
            cfg.AddMindAtticVaultFiles(o => o.Buckets = new[]
                { "LLM", "Brokers", "Tokens", "Subtitles", "Notifications", "AudioStore", "Security" }))
        .ConfigureLogging(lb => lb.AddConsole())
        .ConfigureServices((ctx, svc) =>
        {
            SettingsService.VaultConfiguration = ctx.Configuration;
            svc.AddProseServices();
            svc.AddMindAtticAuthentication<ProseAuthDbContext>(
                ctx.Configuration,
                o =>
                {
                    o.AppName = "Prose";
                    o.IsProduction = !string.Equals(
                        ctx.HostingEnvironment.EnvironmentName, "Development",
                        StringComparison.OrdinalIgnoreCase);
                });
        })
        .Build();
    return Finalize(host.Services);
}