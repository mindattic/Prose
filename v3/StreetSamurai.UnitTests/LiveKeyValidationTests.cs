using System.Net.Http;
using MindAttic.Legion;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Live validation that StreetSamurai's trusted voting panel (the four providers
/// in <c>legion.json</c>: claude / openai / gemini / deepseek) actually
/// authenticate against their real endpoints, using the keys resolved through
/// the shared MindAttic Vault store.
///
/// <para>The <c>LiveKeysTrusted</c>-tagged gate is wired into the StreetSamurai
/// <c>pre-commit</c> hook (<c>.githooks/pre-commit</c>): no point committing when
/// a panel key is dead — <c>ss --legion</c> and the continuity auto-resolver both
/// depend on it. Kept <c>[Explicit]</c> so normal <c>dotnet test</c> stays
/// offline/deterministic.</para>
/// <code>
///   dotnet test --filter "Category=LiveKeysTrusted"   # the pre-commit gate
/// </code>
/// </summary>
[TestFixture]
[Category("LiveKeys")]
[Explicit("Hits real provider APIs with the live shared-store keys — costs money and depends on network. Run on demand / in the pre-commit gate.")]
public class LiveKeyValidationTests
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);

    [Test]
    [Category("LiveKeysTrusted")]
    public async Task TrustedPanel_EveryKeyAuthenticatesLive()
    {
        using var http = new HttpClient { Timeout = ProbeTimeout + TimeSpan.FromSeconds(5) };
        var client = new LegionClient(http, LegionClientOptions.NoResilience);
        var health = new LlmHealthCheck(client);

        // LlmProviderCatalog.DefaultIds == the trusted four, matching legion.json.
        var results = await health.CheckAsync(LlmProviderCatalog.DefaultIds, ProbeTimeout);

        // Quota/billing failures mean the key is valid — the account just needs a
        // top-up. These don't block a commit; only dead/invalid/missing keys do.
        static bool IsKeyDead(LlmHealthResult r) =>
            !r.IsHealthy &&
            r.Diagnosis is not LlmHealthDiagnosis.QuotaExhausted
                       and not LlmHealthDiagnosis.RateLimited;

        var broken = results
            .Where(IsKeyDead)
            .Select(r => $"{r.ProviderId}: {r.Diagnosis} " +
                         $"(HTTP {r.HttpStatusCode?.ToString() ?? "n/a"}) — {r.ActionableMessage}")
            .ToList();

        var billing = results
            .Where(r => !r.IsHealthy && !IsKeyDead(r))
            .Select(r => $"{r.ProviderId}: {r.Diagnosis} — {r.ActionableMessage}")
            .ToList();

        foreach (var r in results)
            TestContext.Out.WriteLine($"{r.ProviderId}: {(r.IsHealthy ? "OK" : "FAIL")} " +
                                      $"({r.Diagnosis}) in {r.ElapsedMilliseconds}ms");

        if (billing.Count > 0)
            TestContext.Out.WriteLine("WARN — quota/billing (key valid, needs credit):\n  - "
                + string.Join("\n  - ", billing));

        Assert.That(broken, Is.Empty,
            "trusted-panel keys that FAILED live validation — fix/rotate before committing:\n  - "
            + string.Join("\n  - ", broken));
    }
}
