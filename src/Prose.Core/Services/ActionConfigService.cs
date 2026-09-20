using Microsoft.Extensions.Logging;
using Prose.Core.Models;

namespace Prose.Core.Services;

/// <summary>
/// Per-action voter-count / model-tier registry. Lets each LLM-driven action
/// in the app declare how big its panel is and at what model tier it runs,
/// editable from the settings page.
///
/// <para><b>Defaults are seeded on first read.</b> Add a new action via
/// <c>ActionIds</c> + an entry in <see cref="DefaultActions"/>; existing
/// installations pick it up the next time they read.</para>
///
/// <para><b>Writing actions are tier-locked.</b> Anything that produces prose
/// or beat blurbs the reader will see should set <c>LockTier=true</c> with
/// tier ≥ High, so the settings UI can't accidentally cost-cut quality.</para>
/// </summary>
public class ActionConfigService
{
    private const string KvKey = "action_configs";

    private readonly SettingsKvStore kv;
    private readonly ILogger<ActionConfigService> log;

    public ActionConfigService(SettingsKvStore kv, ILogger<ActionConfigService> log)
    {
        this.kv  = kv;
        this.log = log;
    }

    /// <summary>Canonical action ids — keep in sync with consumer references.</summary>
    public static class ActionIds
    {
        /// <summary>Beat blurb generation — the 10 expert personas that propose what happens next.</summary>
        public const string ChapterBeatWriter   = "ChapterBeatWriter";
        /// <summary>Beat blurb scoring — the 100 chaos↔order panel that ranks candidates.</summary>
        public const string ChapterBeatVoter    = "ChapterBeatVoter";
        /// <summary>Beat-prose expansion — the single LLM call that turns a blurb into prose.</summary>
        public const string ChapterBeatExpander = "ChapterBeatExpander";
        /// <summary>Picks which N personas from the table are pertinent to a scene.</summary>
        public const string PersonaSelector     = "PersonaSelector";
    }

    public List<ActionConfig> ListAll()
    {
        var doc = kv.Get<ActionConfigCollection>(KvKey);
        if (doc != null && doc.Actions.Count > 0)
        {
            // Backfill any newly-introduced actions that aren't in the saved doc yet
            // so adding a new action doesn't require re-seeding the whole table.
            var defaults = DefaultActions().ToList();
            var added = false;
            foreach (var d in defaults)
            {
                if (!doc.Actions.Any(a => string.Equals(a.Action, d.Action, StringComparison.OrdinalIgnoreCase)))
                {
                    doc.Actions.Add(d);
                    added = true;
                }
            }
            if (added) kv.Set(KvKey, doc);
            return doc.Actions;
        }

        var seeded = new ActionConfigCollection
        {
            Actions  = DefaultActions().ToList(),
            SeededAt = DateTime.UtcNow,
        };
        kv.Set(KvKey, seeded);
        log.LogInformation("ActionConfig: seeded {Count} default actions", seeded.Actions.Count);
        return seeded.Actions;
    }

    public ActionConfig Get(string action)
    {
        var all = ListAll();
        return all.FirstOrDefault(a => string.Equals(a.Action, action, StringComparison.OrdinalIgnoreCase))
            ?? DefaultFor(action);
    }

    public void Save(ActionConfig config)
    {
        var doc = kv.Get<ActionConfigCollection>(KvKey) ?? new ActionConfigCollection();
        if (doc.Actions.Count == 0)
            doc.Actions = DefaultActions().ToList();

        var existing = doc.Actions.FirstOrDefault(a => string.Equals(a.Action, config.Action, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            doc.Actions.Add(config);
        }
        else
        {
            existing.VoterCount = config.VoterCount;
            // LockTier prevents downgrading writing actions even from settings.
            if (!existing.LockTier || ParseTier(config.Tier) >= ParseTier(existing.Tier))
                existing.Tier = config.Tier;
            existing.Label       = string.IsNullOrWhiteSpace(config.Label) ? existing.Label : config.Label;
            existing.Description = string.IsNullOrWhiteSpace(config.Description) ? existing.Description : config.Description;
        }
        kv.Set(KvKey, doc);
    }

    /// <summary>Resolve the configured tier for an action, falling back through defaults.</summary>
    public ModelTierLite GetTier(string action) => ParseTier(Get(action).Tier);

    /// <summary>Resolve the configured voter count for an action, falling back through defaults.</summary>
    public int GetVoterCount(string action) => Get(action).VoterCount;

    private static ActionConfig DefaultFor(string action) =>
        DefaultActions().FirstOrDefault(a => string.Equals(a.Action, action, StringComparison.OrdinalIgnoreCase))
        ?? new ActionConfig { Action = action, VoterCount = 4, Tier = "Medium" };

    private static IEnumerable<ActionConfig> DefaultActions()
    {
        yield return new ActionConfig
        {
            Action      = ActionIds.ChapterBeatWriter,
            Label       = "Beat writer — proposes next-beat blurbs",
            Description = "10 expert personas, each pinned to a HIGH-tier model. Writing actions are tier-locked.",
            VoterCount  = 10,
            Tier        = "High",
            LockTier    = true,
        };
        yield return new ActionConfig
        {
            Action      = ActionIds.ChapterBeatExpander,
            Label       = "Beat expander — turns a blurb into prose",
            Description = "Single LLM call at HIGH tier — the prose readers actually see.",
            VoterCount  = 1,
            Tier        = "High",
            LockTier    = true,
        };
        yield return new ActionConfig
        {
            Action      = ActionIds.ChapterBeatVoter,
            Label       = "Beat voter — scores candidate beats 0-100",
            Description = "100 chaos↔order spectrum personas at LOW tier. Adjustable.",
            VoterCount  = 100,
            Tier        = "Low",
            LockTier    = false,
        };
        yield return new ActionConfig
        {
            Action      = ActionIds.PersonaSelector,
            Label       = "Persona selector — picks which experts to use per scene",
            Description = "Small Haiku-class panel that reads the persona table and the scene to pick top-N.",
            VoterCount  = 4,
            Tier        = "Low",
            LockTier    = false,
        };
    }

    /// <summary>
    /// Local enum mirroring Legion's ModelTier. Kept in Prose.Core to
    /// avoid a build-time dependency on the running app's locked Legion DLL —
    /// see HighTierModelFor / LowTierModelFor in BeatGeneratorService for the
    /// matching bridge. Will be replaced with the Legion enum once the lock
    /// clears.
    /// </summary>
    public enum ModelTierLite { Low, Medium, High, Higher, Highest }

    public static ModelTierLite ParseTier(string s) => s?.Trim().ToLowerInvariant() switch
    {
        "low"     => ModelTierLite.Low,
        "medium"  => ModelTierLite.Medium,
        "high"    => ModelTierLite.High,
        "higher"  => ModelTierLite.Higher,
        "highest" => ModelTierLite.Highest,
        _         => ModelTierLite.Medium,
    };
}
