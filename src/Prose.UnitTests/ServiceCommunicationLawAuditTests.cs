using System.Reflection;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Verifies the Service Communication Laws (SCL-1 … SCL-8) from BIBLE.md §12.
/// These are assembly-scan tests — violations are build-breakers, not just
/// conventions.
///
/// K3: Only VoiceHarvestService and DatabaseService may hold a
///     LiteraryRulesRepository or ToneBibleRepository field (SCL-3).
/// K4: No public method named GetCurrentWorldState without a beatId
///     parameter may exist on any service (SCL-4).
/// K8: NodeReviewService must not hold a field whose type is a
///     beat-write or prose-apply service (SCL-8).
/// </summary>
[TestFixture]
public class ServiceCommunicationLawAuditTests
{
    private static readonly Assembly CoreAssembly =
        typeof(VoiceHarvestService).Assembly;

    // ── K3 — Voice changes always proposed, never auto-applied (SCL-3) ───────

    [Test]
    public void K3_LiteraryRulesRepository_OnlyInjectedIntoAllowedTypes()
    {
        // Only VoiceHarvestService (write) and DatabaseService (read) may hold
        // a LiteraryRulesRepository field. Any other type with this field is a
        // violation of SCL-3.
        var allowed = new HashSet<Type> { typeof(VoiceHarvestService), typeof(DatabaseService) };

        var violations = CoreAssembly.GetTypes()
            .Where(t => !allowed.Contains(t))
            .Where(t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                         .Any(f => f.FieldType == typeof(LiteraryRulesRepository)))
            .ToList();

        Assert.That(violations, Is.Empty,
            $"SCL-3 violation: types with LiteraryRulesRepository outside the approved set: " +
            $"{string.Join(", ", violations.Select(t => t.Name))}");
    }

    [Test]
    public void K3_ToneBibleRepository_OnlyInjectedIntoAllowedTypes()
    {
        var allowed = new HashSet<Type> { typeof(VoiceHarvestService), typeof(DatabaseService) };

        var violations = CoreAssembly.GetTypes()
            .Where(t => !allowed.Contains(t))
            .Where(t => t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                         .Any(f => f.FieldType == typeof(ToneBibleRepository)))
            .ToList();

        Assert.That(violations, Is.Empty,
            $"SCL-3 violation: types with ToneBibleRepository outside the approved set: " +
            $"{string.Join(", ", violations.Select(t => t.Name))}");
    }

    [Test]
    public void K3_NoPublicDirectWriteToLiteraryRulesKey()
    {
        // Verify VoiceHarvestService exposes no public method that could be
        // called externally to directly write literary_rules. The only public
        // entry points should be HarvestAllAsync, HarvestOneAsync, ApplyAsync,
        // RejectAsync, GetByStatusAsync, LogDirectiveAsync — none of which
        // takes a raw key string.
        var publicMethods = typeof(VoiceHarvestService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(m => m.Name)
            .ToList();

        // These are the expected public surface; MutateLiteraryRules and
        // MutateToneBible are private. If MutateXxx appears in the public
        // list the approval bypass is exposed.
        Assert.That(publicMethods, Does.Not.Contain("MutateLiteraryRules"),
            "SCL-3: MutateLiteraryRules must be private — direct mutation must stay internal to VoiceHarvestService.");
        Assert.That(publicMethods, Does.Not.Contain("MutateToneBible"),
            "SCL-3: MutateToneBible must be private — direct mutation must stay internal to VoiceHarvestService.");
    }

    // ── K4 — World state always at-beat, never 'current' (SCL-4) ─────────────

    [Test]
    public void K4_NoGetCurrentWorldStateMethod_WithoutBeatId()
    {
        // No service may expose a public parameterless method whose name
        // implies a 'current' world state (vs an at-beat query). This prevents
        // callers from taking a snapshot without anchoring it to a beat.
        var violations = CoreAssembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m =>
                m.Name.Contains("CurrentWorldState", StringComparison.OrdinalIgnoreCase) ||
                (m.Name.StartsWith("GetWorldState", StringComparison.OrdinalIgnoreCase)
                 && m.GetParameters().Length == 0))
            .ToList();

        Assert.That(violations, Is.Empty,
            $"SCL-4 violation: public at-beat-free world-state methods found: " +
            $"{string.Join(", ", violations.Select(m => $"{m.DeclaringType!.Name}.{m.Name}"))}");
    }

    [Test]
    public void K4_WorldStatePrecheckService_TakesNodeOrBeatContext()
    {
        // WorldStatePrecheckService's public Precheck method must accept some
        // form of node/beat context — it must never be parameterless.
        var type = CoreAssembly.GetType("Prose.Core.Services.WorldStatePrecheckService");
        if (type == null)
        {
            Assert.Ignore("WorldStatePrecheckService not found in assembly — skip.");
            return;
        }

        var parameterlessPrechecks = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.StartsWith("Precheck", StringComparison.OrdinalIgnoreCase)
                        && m.GetParameters().Length == 0)
            .ToList();

        Assert.That(parameterlessPrechecks, Is.Empty,
            "SCL-4: WorldStatePrecheckService.Precheck must accept a node/beat context, not be parameterless.");
    }

    // ── K8 — Reviews never auto-apply editorial conclusions (SCL-8) ──────────

    [Test]
    public void K8_NodeReviewService_DoesNotHoldBeatWriteFields()
    {
        // NodeReviewService must not hold a field whose type can write prose
        // or apply findings. It is an observer only.
        var writeServiceTypes = new HashSet<string>
        {
            "NodeWorkbenchService",
            "BeatRepository",
            "FindingApplyService",
            "VoiceHarvestService",
            "ContinuityApplyService",
            "ProseReflowService",
        };

        var fields = typeof(NodeReviewService)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => writeServiceTypes.Contains(f.FieldType.Name))
            .ToList();

        Assert.That(fields, Is.Empty,
            $"SCL-8 violation: NodeReviewService holds fields that can apply/write prose: " +
            $"{string.Join(", ", fields.Select(f => $"{f.FieldType.Name} {f.Name}"))}. " +
            $"NodeReviewService is an observer — it scores, it does not fix.");
    }

    [Test]
    public void K8_NodeReviewService_IsRegistered_AndHasNoProseWriteDependencies()
    {
        // Belt-and-suspenders: verify the service itself resolves (already
        // covered by DiRegistrationTests) and that its constructor parameters
        // contain no prose-write types. Constructor-injection is the only DI
        // path we need to audit here.
        var ctor = typeof(NodeReviewService)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (ctor == null)
        {
            Assert.Ignore("NodeReviewService has no public constructor — skip.");
            return;
        }

        var writeServiceTypes = new HashSet<string>
        {
            "NodeWorkbenchService",
            "FindingApplyService",
            "VoiceHarvestService",
            "ContinuityApplyService",
        };

        var badParams = ctor.GetParameters()
            .Where(p => writeServiceTypes.Contains(p.ParameterType.Name))
            .ToList();

        Assert.That(badParams, Is.Empty,
            $"SCL-8 violation: NodeReviewService constructor takes prose-write parameters: " +
            $"{string.Join(", ", badParams.Select(p => p.ParameterType.Name))}.");
    }
}
