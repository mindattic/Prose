using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

[McpServerToolType]
public class WorldModellingTools(
    EntityRelationshipService entityRelSvc,
    WorldStateAtBeatService worldStateSvc,
    GearCarryEnforcer gearEnforcer,
    BehavioralInvariantEnforcer behaviorEnforcer,
    ProsePatternGuard proseGuard,
    WeaponAmmoCompatibilityService weaponAmmoSvc,
    AmbientDetailInjector ambientSvc,
    EntityRamificationService ramificationSvc,
    PostBeatValidationService postBeatValidator,
    ProseLessonStore proseLessonStore,
    TimelineConsistencyService timelineSvc,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    [McpServerTool, Description(
        "Returns a hierarchical relationship tree rooted at an entity, " +
        "traversing the Edge graph up to maxDepth hops. " +
        "Formatted as a prompt-injectable context block. " +
        "Use before generation to understand who/what an entity is connected to.")]
    public Task<string> GetEntityTree(
        [Description("Entity GUID")] string entityId,
        [Description("Maximum hop depth (default 3)")] int maxDepth = 3,
        [Description("Comma-separated relation types to follow, e.g. 'carries,wields,member_of'. Omit for all.")] string? relTypes = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(GetEntityTreeImpl), new { entityId, maxDepth, relTypes });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetEntityTreeImpl(string entityId, int maxDepth = 3, string? relTypes = null)
    {
        if (!Guid.TryParse(entityId, out var id))
            return JsonSerializer.Serialize(new { error = "invalid_guid", entityId }, CanonTools.JsonOpts);

        var types = relTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var tree = await entityRelSvc.GetTreeAsync(id, maxDepth, types?.Length > 0 ? types : null);
        return entityRelSvc.FormatTreeAsContextBlock(tree);
    }

    [McpServerTool, Description(
        "Returns the world-state snapshot at a given beat: " +
        "all entity aspect states (wounds, location, status…) + active relationships. " +
        "Use to inject consistent 'what is true right now' context before writing a beat.")]
    public Task<string> GetWorldStateAtBeat(
        [Description("Beat GUID")] string beatId,
        [Description("Story-world timestamp override, ISO 8601. Inferred from beat events when omitted.")] string? storyTime = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(GetWorldStateAtBeatImpl), new { beatId, storyTime });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetWorldStateAtBeatImpl(string beatId, string? storyTime = null)
    {
        if (!Guid.TryParse(beatId, out var bid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        DateTime? st = null;
        if (storyTime != null && DateTime.TryParse(storyTime, out var dt)) st = dt;

        var snapshot = await worldStateSvc.SnapshotAsync(bid, st);
        return snapshot.FormatAsContextBlock();
    }

    [McpServerTool, Description(
        "Returns the sensory detail palette for a character's carried gear. " +
        "Inject the result into a beat prompt to ground sensory texture in what the character actually carries.")]
    public Task<string> GetAmbientPalette(
        [Description("Character entity GUID")] string characterId,
        [Description("Story-date filter (ISO 8601). Omit for current carry edges.")] string? asOfDate = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(GetAmbientPaletteImpl), new { characterId, asOfDate });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetAmbientPaletteImpl(string characterId, string? asOfDate = null)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        DateTime? asOf = null;
        if (asOfDate != null && DateTime.TryParse(asOfDate, out var dt)) asOf = dt;

        var palette = await ambientSvc.GetPaletteAsync(cid, asOf);
        return ambientSvc.FormatPaletteAsPromptBlock(palette)
            ?? JsonSerializer.Serialize(new { message = "No carry edges or sensory_hints found" }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Scans prose text for gear usage verbs (drew, fired, aimed…) and checks " +
        "whether the subject character has a carry/wield edge for each named prop. " +
        "Returns a JSON array of violations — empty array means clean.")]
    public Task<string> CheckGearCarry(
        [Description("Beat prose text to scan")] string beatText,
        [Description("Character entity GUID (the POV/subject character)")] string characterId,
        [Description("Story-date for edge validation (ISO 8601). Legacy — confirmed dead in the " +
            "live pipeline (2026-09-02); prefer beatId. Omit to use all-time carry edges.")] string? storyTime = null,
        [Description("Beat GUID this text belongs to — the live mechanism. Filters carry edges by " +
            "beat-scoped validity (Edge.ValidFromBeatId/ValidUntilBeatId) via reading-order position. " +
            "Omit for ad hoc text with no real beat.")] string? beatId = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(CheckGearCarryImpl), new { beatText, characterId, storyTime, beatId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> CheckGearCarryImpl(string beatText, string characterId, string? storyTime = null, string? beatId = null)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        DateTime? st = null;
        if (storyTime != null && DateTime.TryParse(storyTime, out var dt)) st = dt;

        Guid? asOfBeatId = null;
        if (beatId != null && Guid.TryParse(beatId, out var bid)) asOfBeatId = bid;

        var violations = await gearEnforcer.EnforceAsync(beatText, cid, st, asOfBeatId);
        return JsonSerializer.Serialize(violations.Select(v => new
        {
            gear = v.GearName,
            verb = v.VerbUsed,
            character = v.CharacterName,
            issue = v.Issue,
            offset = v.CharOffset,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "LLM-checks prose text against a character's behavioral rules (decision_rules, " +
        "escalation_ladder, contradictions, habits, breaking_points). " +
        "Returns a JSON array of violations — empty array means the prose is consistent.")]
    public Task<string> CheckBehavior(
        [Description("Beat prose text to check")] string beatText,
        [Description("Character entity GUID")] string characterId) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(CheckBehaviorImpl), new { beatText, characterId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> CheckBehaviorImpl(string beatText, string characterId)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        var violations = await behaviorEnforcer.EnforceAsync(beatText, cid);
        return JsonSerializer.Serialize(violations.Select(v => new
        {
            bucket = v.RuleBucket,
            rule = v.RuleText,
            explanation = v.Explanation,
            character = v.CharacterName,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Runs the deterministic prose pattern linter on text. " +
        "Detects: clichés (chrome gleam, heart hammered…), pseudo-profound constructs " +
        "(in that moment, it hit him that…), on-the-nose interiority, and italicised dialogue. " +
        "Returns a JSON array of violations.")]
    public Task<string> CheckProse(
        [Description("Prose text to lint")] string text) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(CheckProseImpl), new { text });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string CheckProseImpl(string text)
    {
        var violations = proseGuard.Check(text);
        return JsonSerializer.Serialize(violations.Select(v => new
        {
            category = v.Category.ToString(),
            match = v.Match,
            offset = v.CharOffset,
            rule = v.Rule,
            suggestion = v.Suggestion,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns the ammo network for a weapon: its ammunition types + sibling weapons " +
        "that share at least one chambering. Use for continuity (scavenging compatible rounds, " +
        "borrowing ammo between characters) and world enrichment.")]
    public Task<string> GetWeaponNetwork(
        [Description("Weapon entity GUID")] string weaponId) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(GetWeaponNetworkImpl), new { weaponId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetWeaponNetworkImpl(string weaponId)
    {
        if (!Guid.TryParse(weaponId, out var wid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", weaponId }, CanonTools.JsonOpts);

        var network = await weaponAmmoSvc.GetSharedAmmoNetworkAsync(wid);
        return JsonSerializer.Serialize(new
        {
            weapon = network.WeaponName,
            ammunition = network.Ammunition.Select(a => new { name = a.AmmunitionName, alias = a.Alias }),
            siblings = network.SiblingWeapons.Select(s => new { name = s.WeaponName, sharedAmmo = s.SharedAmmoAlias }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns a character's weapon loadout from their signature_gear list, " +
        "with ammo types for each weapon. Use for scene continuity and logistics.")]
    public Task<string> GetCharacterLoadout(
        [Description("Character entity GUID")] string characterId,
        [Description("Story-date filter (ISO 8601). Omit for all-time loadout.")] string? asOfDate = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(GetCharacterLoadoutImpl), new { characterId, asOfDate });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetCharacterLoadoutImpl(string characterId, string? asOfDate = null)
    {
        if (!Guid.TryParse(characterId, out var cid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", characterId }, CanonTools.JsonOpts);

        DateTime? asOf = null;
        if (asOfDate != null && DateTime.TryParse(asOfDate, out var dt)) asOf = dt;

        var loadout = await weaponAmmoSvc.GetCharacterLoadoutAsync(cid, asOf);
        return JsonSerializer.Serialize(new
        {
            character = loadout.CharacterName,
            asOfDate = loadout.AsOfDate,
            weapons = loadout.Weapons.Select(w => new
            {
                name = w.GearName,
                ammunition = w.Ammunition.Select(a => new { name = a.AmmunitionName, alias = a.Alias }),
            }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns a character's full equipment across all slots: primary/secondary/ranged weapons, " +
        "armor, tool, signature gear, pharmaceuticals, and carried loot. " +
        "Use for scene continuity, loot tracking, and loadout management.")]
    public Task<string> GetCharacterEquipment(
        [Description("Character entity slug (e.g. 'kyle_ellen_corbin', 'sasha_vo').")] string characterSlug) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(GetCharacterEquipmentImpl), new { characterSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetCharacterEquipmentImpl(string characterSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var entity = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character" && e.Slug == characterSlug)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync();

        if (entity == null)
            return JsonSerializer.Serialize(new { error = "not_found", characterSlug }, CanonTools.JsonOpts);

        var gear = await db.CharacterBelongingsGear.AsNoTracking()
            .Where(g => g.CharacterId == entity.Id)
            .OrderBy(g => g.Bucket).ThenBy(g => g.Position)
            .Select(g => new { g.Bucket, g.Position, g.GearName })
            .ToListAsync();

        var extras = await db.CharacterBelongingsExtras.AsNoTracking()
            .Where(x => x.CharacterId == entity.Id)
            .Select(x => new { x.KeyName, x.Value })
            .ToListAsync();

        string Primary(string bucket)
            => gear.Where(g => g.Bucket == bucket).OrderBy(g => g.Position).Select(g => g.GearName).FirstOrDefault() ?? "";

        List<string> List(string bucket)
            => gear.Where(g => g.Bucket == bucket).OrderBy(g => g.Position).Select(g => g.GearName).ToList();

        return JsonSerializer.Serialize(new
        {
            character        = entity.Name,
            characterSlug,
            primary_weapon   = Primary("primary_weapon"),
            secondary_weapon = Primary("secondary_weapon"),
            ranged_weapon    = Primary("ranged_weapon"),
            armor            = Primary("armor"),
            tool_slot        = Primary("tool_slot"),
            signature_gear   = List("signature_gear"),
            pharmaceuticals  = List("pharmaceuticals"),
            carried_loot     = List("carried_loot"),
            other            = extras.ToDictionary(x => x.KeyName, x => x.Value),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Returns every beat flagged EntityStale — i.e. a canon entity mentioned in " +
        "the beat was updated after the beat was written. Grouped by node. " +
        "Review each beat and call clear_entity_stale when satisfied.")]
    public Task<string> ListEntityStaleBeats() =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(ListEntityStaleBeatsImpl), new { });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ListEntityStaleBeatsImpl()
    {
        var beats = await ramificationSvc.GetEntityStaleBeatsAsync();
        if (beats.Count == 0)
            return JsonSerializer.Serialize(new { message = "No entity-stale beats." }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(beats.Select(b => new
        {
            beatId      = b.BeatId,
            beatNumber  = b.BeatNumber,
            nodeId    = b.NodeId,
            node      = b.NodeTitle,
            textPreview = b.TextPreview,
            entities    = b.Entities,
        }), CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Clears the EntityStale flag on a beat after the author has reviewed it " +
        "and confirmed the prose is still consistent with current entity canon.")]
    public Task<string> ClearEntityStale(
        [Description("Beat GUID")] string beatId) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(ClearEntityStaleImpl), new { beatId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ClearEntityStaleImpl(string beatId)
    {
        if (!Guid.TryParse(beatId, out var bid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        await ramificationSvc.ClearEntityStaleAsync(bid);
        return JsonSerializer.Serialize(new { ok = true, beatId }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Run the full post-beat validation battery on a saved beat: " +
        "prose pattern guard (clichés, pseudo-profound, on-the-nose, italicised dialogue) + " +
        "gear carry check (character uses gear without a carry edge) + " +
        "optional behavior invariant check (LLM — one call per character). " +
        "All violations are filed as Findings and returned. " +
        "Accepts an optional comma-separated list of character GUIDs; when omitted, " +
        "characters are derived from the beat's indexed entity mentions.")]
    public Task<string> ValidateBeat(
        [Description("Beat GUID.")] string beatId,
        [Description("Comma-separated character GUIDs to check gear/behavior for. Omit to auto-detect from entity mentions.")] string? characterIds = null,
        [Description("Run the LLM-based behavior invariant check (one LLM call per character). Default false.")] bool checkBehavior = false,
        [Description("Story-date for gear edge validation (ISO 8601). Omit for all-time carry edges.")] string? storyTime = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(ValidateBeatImpl), new { beatId, characterIds, checkBehavior, storyTime });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ValidateBeatImpl(string beatId, string? characterIds = null, bool checkBehavior = false, string? storyTime = null)
    {
        if (!Guid.TryParse(beatId, out var bid))
            return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, CanonTools.JsonOpts);

        List<Guid>? charIds = null;
        if (!string.IsNullOrWhiteSpace(characterIds))
        {
            charIds = characterIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => Guid.TryParse(s, out _))
                .Select(Guid.Parse)
                .ToList();
        }

        DateTime? st = null;
        if (storyTime != null && DateTime.TryParse(storyTime, out var dt)) st = dt;

        var result = await postBeatValidator.FullValidateAsync(bid, charIds, checkBehavior, st);
        return JsonSerializer.Serialize(new
        {
            beat_id            = beatId,
            prose_violations   = result.ProseViolations,
            gear_violations    = result.GearViolations,
            behavior_violations = result.BehaviorViolations,
            total_findings     = result.Total,
            note               = result.Total > 0
                ? "Findings filed — use list_findings to review them."
                : "No violations found.",
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Run the prose pattern guard over every beat in a node and file violations " +
        "as Findings. This is the node-wide sweep equivalent of check_prose — " +
        "use it after importing or rewriting a node to catch all clichés, " +
        "pseudo-profound constructs, on-the-nose interiority, and italicised dialogue " +
        "in one pass. Returns a per-beat summary of violations found.")]
    public Task<string> ScanBookViolations(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(ScanBookViolationsImpl), new { nodeIdOrSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ScanBookViolationsImpl(string nodeIdOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        Guid nodeId;
        if (Guid.TryParse(nodeIdOrSlug, out var g))
            nodeId = g;
        else
        {
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            var s = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Slug == nodeIdOrSlug || x.NodeCode == nodeIdOrSlug);
            if (s == null)
                return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
            nodeId = s.Id;
        }

        var slug = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => s.Slug)
            .FirstOrDefaultAsync() ?? nodeId.ToString();

        // SS-A43: expand to chapter children for book-mode nodes.
        // Recurses past any nested Collection (2026-08-09 fix).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId);
        var beats = await db.BeatNodes.AsNoTracking()
            .Where(sb => searchIds.Contains(sb.NodeId) && true)
            .Join(db.Beats, sb => sb.BeatId, b => b.Id, (sb, b) => new { b.Id, b.Number, sb.SortKey, b.Text })
            .OrderBy(b => b.SortKey)
            .ToListAsync();

        int totalViolations = 0;
        var beatSummaries = new List<object>();
        foreach (var beat in beats)
        {
            if (string.IsNullOrWhiteSpace(beat.Text)) continue;
            var violations = proseGuard.Check(beat.Text);
            if (violations.Count == 0) continue;

            // File all violations for this beat as Findings.
            await postBeatValidator.QuickValidateAsync(slug, beat.Text, beat.Id);

            beatSummaries.Add(new
            {
                beat_number = beat.Number,
                beat_id     = beat.Id,
                violations  = violations.Select(v => new { category = v.Category.ToString(), rule = v.Rule }),
            });
            totalViolations += violations.Count;
        }

        return JsonSerializer.Serialize(new
        {
            node_id       = nodeId,
            slug,
            beats_scanned   = beats.Count,
            total_violations = totalViolations,
            beats_with_issues = beatSummaries.Count,
            note = totalViolations > 0
                ? "Violations filed as Findings — use list_findings to review."
                : "No prose violations found.",
            beats            = beatSummaries,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Add an editorial prose lesson — an author ruling that reviewers must respect. " +
        "Lessons are injected into every future review ballot prompt so the panel does not " +
        "penalise beats the author has already decided are doing their job in the sequence. " +
        "scope: 'global' applies to all nodes; 'node:<slug>' to one node; 'beat:<guid>' to one beat. " +
        "kind: score-vs-function | delight | voice | pacing | continuity | other.")]
    public Task<string> AddProseLesson(
        [Description("Scope: 'global', 'node:<slug>', or 'beat:<guid>'")] string scope,
        [Description("Kind: score-vs-function | delight | voice | pacing | continuity | other")] string kind,
        [Description("The ruling text — what reviewers must respect.")] string text) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(AddProseLessonImpl), new { scope, kind, text });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string AddProseLessonImpl(string scope, string kind, string text)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return JsonSerializer.Serialize(new { error = "scope_required" }, CanonTools.JsonOpts);
        if (string.IsNullOrWhiteSpace(kind))
            return JsonSerializer.Serialize(new { error = "kind_required" }, CanonTools.JsonOpts);
        if (string.IsNullOrWhiteSpace(text))
            return JsonSerializer.Serialize(new { error = "text_required" }, CanonTools.JsonOpts);

        proseLessonStore.Add(scope, kind, text);
        return JsonSerializer.Serialize(new { ok = true, scope, kind, text }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "List prose lessons from the editorial memory store. " +
        "When scope is omitted, returns all lessons across all scopes. " +
        "When scope is provided, returns only lessons whose scope starts with that prefix " +
        "(e.g. 'global' for all global lessons, 'node:my-slug' for a specific node).")]
    public Task<string> ListProseLessons(
        [Description("Optional scope filter prefix (e.g. 'global', 'node:my-slug'). Omit for all.")] string? scope = null) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(ListProseLessonsImpl), new { scope });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string ListProseLessonsImpl(string? scope = null)
    {
        var all = proseLessonStore.ListAll();
        if (!string.IsNullOrWhiteSpace(scope))
            all = all.Where(l => l.Scope.StartsWith(scope, StringComparison.OrdinalIgnoreCase)).ToList();

        return JsonSerializer.Serialize(new
        {
            total = all.Count,
            lessons = all.OrderBy(l => l.AddedAt).Select(l => new
            {
                id       = l.Id,
                scope    = l.Scope,
                kind     = l.Kind,
                text     = l.Text,
                added_at = l.AddedAt,
            }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Deterministic timeline-consistency check for a node (RFC 0009 §5). " +
        "Zero LLM calls. " +
        "Detects two violation classes: " +
        "(1) dead-character-acting — an entity whose status is 'dead'/'deceased' appears " +
        "in a later beat; " +
        "(2) wound-regression — a healed/none event precedes the injury-onset event for " +
        "the same condition. " +
        "Returns a list of findings with kind, entityId, entityName, beatNumber, detail, severity. " +
        "Returns an empty array when no events are in the ledger for this node — never throws.")]
    public Task<string> CheckTimeline(
        [Description("Node slug or GUID")] string slugOrId) =>
        hub.InvokeAsync(nameof(WorldModellingTools), nameof(CheckTimelineImpl), new { slugOrId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> CheckTimelineImpl(string slugOrId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        Guid nodeId;
        if (Guid.TryParse(slugOrId, out nodeId) || Guid.TryParseExact(slugOrId, "N", out nodeId))
        {
            // already have the GUID
        }
        else
        {
            var node = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Slug == slugOrId || s.NodeCode == slugOrId);
            if (node == null)
                return JsonSerializer.Serialize(new { error = "node_not_found", slugOrId }, CanonTools.JsonOpts);
            nodeId = node.Id;
        }

        var findings = await timelineSvc.CheckNodeAsync(nodeId);

        return JsonSerializer.Serialize(new
        {
            node_id = nodeId,
            count     = findings.Count,
            findings  = findings.Select(f => new
            {
                kind        = f.Kind,
                entity_id   = f.EntityId,
                entity_name = f.EntityName,
                beat_number = f.BeatNumber,
                detail      = f.Detail,
                severity    = f.Severity,
            }),
        }, CanonTools.JsonOpts);
    }
}
