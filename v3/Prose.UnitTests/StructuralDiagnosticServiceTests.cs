using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix: StructuralDiagnosticService.RunCheckAsync's catch
/// used to default a failed check to StructuralCheckResult.Warn — a real, LLM-producible
/// non-blocking value, so a BLOCKING check's own outage was silently indistinguishable from that
/// check genuinely passing (only Fail ever trips `IsBlocking && Result == Fail`). An LLM outage
/// during --diagnose-book could read as "Ready to review" when in truth zero checks ever ran.
/// Fixed by adding a distinct StructuralCheckResult.Error and gating blocking/purge/filing on
/// "no check errored this run," matching the pattern already used for the `truncated` case.
/// </summary>
[TestFixture]
public class StructuralDiagnosticServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private FindingsService findings = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-structuraldiag-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        findings = new FindingsService(TestDbFactory.For(paths, "structural"), paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private StructuralDiagnosticService Make(ILlmService llm) =>
        new(llm, findings, TestDbFactory.For(paths, "structural"), NullLogger<StructuralDiagnosticService>.Instance);

    private sealed class ThrowingLlm : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            throw new InvalidOperationException("Circuit breaker open for provider 'claude-api'.");
    }

    private sealed class FixedResponseLlm(string response) : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user,
            double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default) =>
            Task.FromResult(response);
    }

    [Test]
    public async Task DiagnoseTextAsync_AllChecksThrow_ReportsErrorNotClean()
    {
        var svc = Make(new ThrowingLlm());
        var result = await svc.DiagnoseTextAsync(Guid.NewGuid(), "test-slug", "Test", "Some prose to diagnose.");

        Assert.That(result.ErrorCount, Is.EqualTo(result.Checks.Count),
            "every check should report Error when the LLM throws for all of them");
        Assert.That(result.Checks, Has.None.Matches<StructuralCheck>(c => c.Result == StructuralCheckResult.Warn),
            "a failed check must never be reported as a real Warn verdict");
        Assert.That(result.HasBlockingFailures, Is.False,
            "an outage must never read as a genuine blocking failure — we have no evidence either way");
        Assert.That(result.Recommendation, Does.Contain("could not run").Or.Contain("INCOMPLETE"),
            "the recommendation must say the diagnosis is incomplete, not silently recommend proceeding");
    }

    [Test]
    public async Task DiagnoseTextAsync_AllChecksThrow_DoesNotClaimReadyToReview()
    {
        var svc = Make(new ThrowingLlm());
        var result = await svc.DiagnoseTextAsync(Guid.NewGuid(), "test-slug", "Test", "Some prose to diagnose.");

        Assert.That(result.Recommendation, Is.Not.EqualTo("Ready to review."),
            "this is the exact bug: an outage used to be indistinguishable from a clean pass");
    }

    [Test]
    public async Task DiagnoseTextAsync_AllChecksPass_NoErrorsAndReadyToReview()
    {
        var svc = Make(new FixedResponseLlm("""{"result":"pass","evidence":"","fix":""}"""));
        var result = await svc.DiagnoseTextAsync(Guid.NewGuid(), "test-slug", "Test", "Some prose to diagnose.");

        Assert.That(result.ErrorCount, Is.EqualTo(0));
        Assert.That(result.HasBlockingFailures, Is.False);
        Assert.That(result.Recommendation, Is.EqualTo("Ready to review."));
    }

    [Test]
    public async Task DiagnoseTextAsync_AllChecksFail_BlocksReview()
    {
        var svc = Make(new FixedResponseLlm("""{"result":"fail","evidence":"missing","fix":"add it"}"""));
        var result = await svc.DiagnoseTextAsync(Guid.NewGuid(), "test-slug", "Test", "Some prose to diagnose.");

        Assert.That(result.ErrorCount, Is.EqualTo(0));
        Assert.That(result.HasBlockingFailures, Is.True,
            "a real, successfully-evaluated fail on a blocking check must still block");
    }
}
