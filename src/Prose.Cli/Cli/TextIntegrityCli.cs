using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --check-text-integrity [--fix] [--json]
///
/// Scans every Beats.Text and every book's Nodes.NodeOutline, corpus-wide across ALL universes in
/// one pass (TextIntegrityService.ScanAsync uses IgnoreQueryFilters — a data-integrity scan must
/// never be universe-scoped), for TWO known corruption signatures left by past non-UTF-8 write
/// paths mangling a multi-byte character: U+FFFD (the Unicode replacement character), and stray
/// low-range control characters (codepoints 1-31, excluding tab/LF/CR) standing in for a lost
/// em-dash or section symbol.
///
/// Added 2026-08-15 after finding 8 real instances of the U+FFFD case in Ballast's NodeOutline
/// during its sequential read. Root cause of why this went undetected: SQL Server's
/// REPLACE/CHARINDEX gave false negatives for U+FFFD under this DB's collation — only a raw
/// positional UNICODE() scan found it. This tool never uses those SQL functions for detection; it
/// pulls text into memory and does a plain C# char comparison, which cannot have that collation
/// bug. Extended the same day after finding a second, distinct corruption class (stray control
/// chars, 12 instances) in Between the Lines' bible — same failure family, different garbage
/// codepoint. Treat any future new garbage-codepoint discovery the same way: extend the service's
/// detector, don't just hand-fix the one instance and move on.
///
/// --fix applies only the HIGH-CONFIDENCE repairs (U+FFFD immediately followed by a digit → Φ;
/// a stray control char between two spaces → em-dash; a stray control char before a digit →
/// section symbol) via a direct positional single-character replace. Anything else is reported
/// for manual review, never guessed at.
/// </summary>
public static class TextIntegrityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var fix = args.Contains("--fix");
        var json = args.Contains("--json");

        var svc = services.GetRequiredService<TextIntegrityService>();
        var findings = await svc.ScanAsync();

        var highConfidence = findings.Where(f => f.SuggestedFix != null).ToList();
        var needsReview = findings.Where(f => f.SuggestedFix == null).ToList();

        if (fix)
        {
            foreach (var f in highConfidence)
                await svc.ApplyFixAsync(f, f.SuggestedFix!.Value);
        }

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                totalFindings = findings.Count,
                autoFixed = fix ? highConfidence.Count : 0,
                needsReview = needsReview.Count,
                findings = findings.Select(f => new
                {
                    f.Table, f.RowId, f.Column, f.RowLabel, f.Position, f.Context, f.FoundCodepoint,
                    suggestedFix = f.SuggestedFix?.ToString(), f.SuggestedFixReason,
                }),
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return findings.Count > 0 && !fix ? 1 : 0;
        }

        if (findings.Count == 0)
        {
            Console.WriteLine("[text-integrity] Clean — no U+FFFD or stray control characters found in any Beats.Text or Nodes.NodeOutline, corpus-wide.");
            return 0;
        }

        Console.WriteLine($"[text-integrity] {findings.Count} corruption instance(s) found ({highConfidence.Count} high-confidence, {needsReview.Count} need manual review):");
        Console.WriteLine();
        foreach (var f in findings)
        {
            var fixedTag = fix && f.SuggestedFix != null ? " [FIXED]" : f.SuggestedFix != null ? " [fixable with --fix]" : " [needs manual review]";
            Console.WriteLine($"  {f.Table}.{f.Column} — {f.RowLabel} ({f.RowId}) @ pos {f.Position}{fixedTag}");
            Console.WriteLine($"    ...{f.Context}...");
            if (f.SuggestedFixReason != null) Console.WriteLine($"    reason: {f.SuggestedFixReason}");
            Console.WriteLine();
        }

        if (!fix && highConfidence.Count > 0)
            Console.WriteLine($"Run again with --fix to auto-repair the {highConfidence.Count} high-confidence finding(s).");
        if (needsReview.Count > 0)
            Console.WriteLine($"{needsReview.Count} finding(s) need manual review — not auto-fixable (not immediately followed by a digit).");

        return findings.Count > 0 && (!fix || needsReview.Count > 0) ? 1 : 0;
    }
}
