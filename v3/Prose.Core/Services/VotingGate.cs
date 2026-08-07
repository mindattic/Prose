using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Prose.Core.Services;

/// <summary>
/// Thrown when a voting/ballot/score-soliciting flow runs while voting is
/// disabled by default (SS-A44) and no explicit per-invocation override was
/// supplied. The message is the canonical, actionable SS-A44 message.
/// </summary>
public sealed class VotingDisabledException : InvalidOperationException
{
    public VotingDisabledException(string message) : base(message) { }
}

/// <summary>
/// SS-A44 — the single central gate that every LLM ballot / score / vote
/// soliciting flow consults before spending API tokens on a panel. Score
/// panels (--review-node), Legion votes, census reviews, entity rating
/// ballots and book/story quality scoring are DISABLED BY DEFAULT engine-wide;
/// they run only when an explicit per-invocation override is supplied
/// (<c>--allow-votes</c> on the CLI, <c>allowVotes:true</c> on an MCP tool, or
/// the equivalent explicit user action in the UI).
///
/// PROSE generation / drafting / polish is NOT gated — the switch is on
/// scoring/balloting, never on generation. Single-LLM diagnostic analyzers
/// (Logic Sweep, structural diagnosis, continuity/contradiction scans,
/// emotional-depth examination, outline review) are likewise NOT gated: they
/// localize concrete failures for free, which is exactly what SS-A44 endorses
/// over blind panels.
///
/// The default is read from <c>legion.json</c> (<c>"votingEnabled"</c>).
/// Absence of the key = false (OFF).
/// </summary>
public sealed class VotingGate
{
    /// <summary>
    /// The canonical, actionable message shown when a gated flow is blocked.
    /// Kept verbatim per SS-A44 — tests assert on this exact string.
    /// </summary>
    public const string DisabledMessage =
        "Voting is disabled by default (SS-A44). Pass --allow-votes (CLI) / allowVotes:true (MCP) to run this explicitly.";

    private readonly ILogger<VotingGate>? log;

    public VotingGate(bool votingEnabledByDefault, ILogger<VotingGate>? log = null)
    {
        VotingEnabledByDefault = votingEnabledByDefault;
        this.log = log;
    }

    /// <summary>Whether voting is enabled by default (from legion.json). False = OFF.</summary>
    public bool VotingEnabledByDefault { get; }

    /// <summary>
    /// Non-throwing check — voting runs when it is enabled by default OR the
    /// caller supplied an explicit per-invocation override. Auto/background
    /// pipelines use this to SKIP the scoring step gracefully instead of failing.
    /// </summary>
    public bool IsAllowed(bool explicitOverride) => VotingEnabledByDefault || explicitOverride;

    /// <summary>
    /// Enforce the gate at the entry of a ballot/score-soliciting flow. When
    /// voting is disabled and no override was supplied, logs one warning line
    /// and throws <see cref="VotingDisabledException"/> with the canonical
    /// SS-A44 message.
    /// </summary>
    public void EnsureAllowed(string operation, bool explicitOverride)
    {
        if (IsAllowed(explicitOverride)) return;
        log?.LogWarning(
            "Voting blocked for '{Operation}' — disabled by default (SS-A44); no --allow-votes/allowVotes override supplied.",
            operation);
        throw new VotingDisabledException(DisabledMessage);
    }

    /// <summary>
    /// Resolve the default from <c>legion.json</c> by walking up from
    /// <paramref name="startDir"/>. The <c>"votingEnabled"</c> key gates every
    /// panel; absent, unreadable, or false → OFF (returns false). This reads the
    /// key directly rather than through MindAttic.Legion's LegionConfig, which
    /// intentionally ignores unknown keys.
    /// </summary>
    public static bool ReadVotingEnabledDefault(string? startDir = null)
    {
        try
        {
            var dir = string.IsNullOrWhiteSpace(startDir) ? Environment.CurrentDirectory : startDir;
            var current = new DirectoryInfo(dir);
            var opts = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            for (int depth = 0; depth < 12 && current != null; depth++)
            {
                var path = Path.Combine(current.FullName, "legion.json");
                if (File.Exists(path))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path), opts);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("votingEnabled", out var v))
                    {
                        return v.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String when bool.TryParse(v.GetString(), out var b) => b,
                            _ => false,
                        };
                    }
                    return false; // file present, key absent → OFF
                }
                current = current.Parent;
            }
        }
        catch { /* malformed path / permission / parse issue — fall through to OFF */ }
        return false; // absent / unreadable → OFF
    }
}
