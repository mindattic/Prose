namespace Prose.Cli;

/// <summary>
/// <c>prose --estimate-cost --beats &lt;N&gt; [--pov-characters &lt;M&gt;] [--tier free|deep|full]</c>
///
/// Prints the LLM call count implied by <c>BookHealthService</c>'s CURRENT wiring for a
/// book of N beats, so a future per-beat service addition's cost is visible before it ships
/// instead of discovered by totaling a bill months later (RFC 0009 §9.5, added 2026-08-13
/// after a cost-reduction pass found the original "$400 for 8-11 books" diagnosis had
/// over-attributed the bill to redundant post-hoc audits — most of it lives in
/// <c>ProseWriterRouter.WriteAsync</c>'s unconditional per-beat calls. RFC 0009 §9.4 recorded
/// item 1 — collapsing five of those calls into <c>BeatExtractionService</c> — as implemented
/// the same day once this very estimator showed generation, not the audit battery, was the
/// dominant cost; the number below already reflects that collapse.
///
/// Figures below are read directly from BookHealthService.cs/NarrativeScienceService.cs as of
/// 2026-08-13 — update both the code comment there and the table here together if either
/// service's per-beat/per-book/per-character shape changes, or this estimate silently rots.
/// </summary>
public static class EstimateCostCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        int beats = 500, povCharacters = 4;
        var tier = "full";
        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--beats":         if (int.TryParse(args[i + 1], out var b)) beats = b; i++; break;
                case "--pov-characters": if (int.TryParse(args[i + 1], out var p)) povCharacters = p; i++; break;
                case "--tier":          tier = args[i + 1].Trim().ToLowerInvariant(); i++; break;
            }
        }

        Console.WriteLine($"[estimate-cost] {beats} beats, {povCharacters} POV characters, tier={tier}");
        Console.WriteLine();

        // ── generation (always paid, regardless of any audit tier) ─────────────
        Console.WriteLine("GENERATION (ProseWriterRouter.WriteAsync — fires on every beat, no settings gate; item 1, implemented 2026-08-13):");
        const int perBeatGenerationCalls = 6; // BeatGenerator + EntityContextService.ReconcileAsync + SceneCollisionService (conditional) +
                                                // LibertyReport + SemanticFidelity + BeatExtractionService (1 consolidated call, was 5)
        var generationTotal = beats * perBeatGenerationCalls;
        Console.WriteLine($"  ~{perBeatGenerationCalls} calls/beat x {beats} beats = {generationTotal} calls (was ~10/beat before BeatExtractionService consolidated 5 calls into 1)");
        Console.WriteLine();

        // ── BookHealthService FREE tier ─────────────────────────────────────────
        Console.WriteLine("FREE tier (BookHealthService — deterministic/near-zero, run as often as wanted):");
        Console.WriteLine("  ~0 LLM calls (timeline/nouns/sanity-scan/voice-drift/plant-audit/coordinate/prose-check/verify-book)");
        if (tier == "free") return Task.FromResult(0);
        Console.WriteLine();

        // ── DEEP tier adds ───────────────────────────────────────────────────────
        const int deepPerBookCalls = 10; // examine-emotion, book-audit, diagnose-book, check-fidelity,
                                          // logic-sweep, craft-checklist, check-canon, altitude-audit,
                                          // reader-qa, behavior-check, theme-coherence (one call/book each,
                                          // except craft-checklist/logic-sweep which are closer to O(chapters) —
                                          // treated here as 1 "unit" each; this is a first-pass estimate, not
                                          // a verified per-service call count for every one of these).
        Console.WriteLine("DEEP tier adds (one call per book, roughly, per service):");
        Console.WriteLine($"  ~{deepPerBookCalls} calls/book (examine-emotion, book-audit, diagnose-book, check-fidelity,");
        Console.WriteLine("   logic-sweep, craft-checklist, check-canon, altitude-audit, reader-qa, behavior-check, theme-coherence)");
        if (tier == "deep") return Task.FromResult(0);
        Console.WriteLine();

        // ── FULL tier adds — the two genuinely per-beat/per-character FULL checks ───
        Console.WriteLine("FULL tier adds (confirmed per-beat / per-character, 2026-08-13 code review):");
        var swainCalls = beats;
        Console.WriteLine($"  swain-audit:        1 Haiku call/beat  x {beats} beats = {swainCalls} calls");
        var dramaticQCalls = beats;
        Console.WriteLine($"  dramatic-question:  1 call/beat        x {beats} beats = {dramaticQCalls} calls (deliberately per-beat — see RFC 0009 §9.2)");
        var sacredFlawCalls = povCharacters;
        Console.WriteLine($"  sacred-flaw:        1 call/POV character x {povCharacters} = {sacredFlawCalls} calls");
        Console.WriteLine("  storyscope-audit / chekhov-audit / five-act-map: ~1 call/book each = 3 calls");
        var fullTotal = swainCalls + dramaticQCalls + sacredFlawCalls + 3;
        Console.WriteLine($"  FULL tier subtotal: {fullTotal} calls");
        Console.WriteLine();

        var grandTotal = generationTotal + deepPerBookCalls + fullTotal;
        Console.WriteLine($"ESTIMATED TOTAL (generation + DEEP + FULL, one full pass): ~{grandTotal} calls for a {beats}-beat book.");
        Console.WriteLine($"  ({(double)grandTotal / beats:F1}x overhead over the theoretical minimum of {beats} — one call per beat.)");

        return Task.FromResult(0);
    }
}
