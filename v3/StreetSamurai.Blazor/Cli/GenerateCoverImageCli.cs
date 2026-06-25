using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --generate-cover</c> — AI cover-image generation via API.
///
/// Modes:
///
///   List prompts:
///     ss --generate-cover --list [--strand-code CODE]
///
///   Save a prompt (no generation):
///     ss --generate-cover --save --strand-code CODE --generator NAME
///                          --prompt "TEXT" [--negative "TEXT"]
///                          [--label "LABEL"] [--params "JSON"] [--notes "TEXT"]
///
///   Generate from a saved prompt:
///     ss --generate-cover --prompt-id GUID [--output PATH] [--dry-run]
///
///   Save + generate in one shot:
///     ss --generate-cover --strand-code CODE --generator NAME --prompt "TEXT"
///                          [--negative "TEXT"] [--label "LABEL"] [--params "JSON"]
///                          [--output PATH] [--dry-run]
///
/// Generators (those with [API] call the API; others are prompt-store-only):
///   chatgpt      [API]  gpt-image-1 via OpenAI          key: SS_OPENAI_API_KEY
///   gemini       [API]  Imagen 3 via Google AI           key: SS_GEMINI_API_KEY
///   ideogram     [API]  Ideogram v3                      key: SS_IDEOGRAM_API_KEY
///   flux         [API]  Flux Pro via FAL.ai              key: SS_FAL_API_KEY
///   midjourney         (no API — store prompt; paste manually)
///
/// Parameters JSON examples (passed as --params):
///   chatgpt:  {"size":"1024x1536","quality":"high","format":"png"}
///   gemini:   {"ar":"2:3","model":"imagen-3.0-generate-001"}
///   ideogram: {"ar":"ASPECT_2_3","speed":"BALANCED","style":"REALISTIC"}
///   flux:     {"model":"fal-ai/flux-pro/v1.1","width":1024,"height":1536}
///
/// --output PATH  Also write the image bytes to this file path (in addition to DB).
/// </summary>
public static class GenerateCoverImageCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var promptId   = Arg(args, "--prompt-id");
        var strandCode = Arg(args, "--strand-code");
        var generator  = Arg(args, "--generator");
        var prompt     = Arg(args, "--prompt");
        var negative   = Arg(args, "--negative");
        var label      = Arg(args, "--label");
        var parameters = Arg(args, "--params");
        var notes      = Arg(args, "--notes");
        var output     = Arg(args, "--output");
        var list       = args.Contains("--list");
        var save       = args.Contains("--save");
        var dryRun     = args.Contains("--dry-run");

        var factory  = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var genSvc   = sp.GetRequiredService<CoverImageGeneratorService>();

        // ── --list ────────────────────────────────────────────────────────────
        if (list)
        {
            await using var db = await factory.CreateDbContextAsync();
            var query = db.CoverImagePrompts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(strandCode))
            {
                var strand = await db.Strands
                    .Where(s => s.StrandCode == strandCode)
                    .Select(s => new { s.Id })
                    .FirstOrDefaultAsync();
                if (strand is null) { Console.Error.WriteLine($"No strand with StrandCode='{strandCode}'"); return 1; }
                query = query.Where(p => p.StrandId == strand.Id);
            }

            var rawRows = await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.Id, p.Generator, p.Label, p.StrandId, p.AssetId, p.CreatedAt, p.PromptText })
                .ToListAsync();
            var rows = rawRows.Select(p => new
            {
                p.Id, p.Generator, p.Label, p.StrandId, p.AssetId, p.CreatedAt,
                TextSnip = p.PromptText.Length > 80 ? p.PromptText[..80] + "…" : p.PromptText,
            }).ToList();

            var strandIds = rows.Where(r => r.StrandId.HasValue).Select(r => r.StrandId!.Value).Distinct().ToList();
            var codes = await db.Strands
                .Where(s => strandIds.Contains(s.Id))
                .Select(s => new { s.Id, s.StrandCode })
                .ToDictionaryAsync(s => s.Id, s => s.StrandCode ?? "?");

            Console.WriteLine($"{"ID",-36}  {"Gen",-16}  {"Strand",-8}  {"Asset?",-7}  {"Label / Prompt"}");
            Console.WriteLine(new string('-', 100));
            foreach (var r in rows)
            {
                var sc     = r.StrandId.HasValue && codes.TryGetValue(r.StrandId.Value, out var c) ? c : "(global)";
                var hasAsset = r.AssetId.HasValue ? "yes" : "no";
                var display  = r.Label ?? r.TextSnip;
                Console.WriteLine($"{r.Id,-36}  {r.Generator,-16}  {sc,-8}  {hasAsset,-7}  {display}");
            }
            Console.WriteLine($"\n{rows.Count} prompt(s)");
            return 0;
        }

        // ── --save (prompt-only, no generation) ───────────────────────────────
        if (save && string.IsNullOrWhiteSpace(promptId))
        {
            if (string.IsNullOrWhiteSpace(generator)) { Console.Error.WriteLine("--generator required"); return 1; }
            if (string.IsNullOrWhiteSpace(prompt))    { Console.Error.WriteLine("--prompt required");    return 1; }

            await using var db = await factory.CreateDbContextAsync();
            var row = await BuildPromptRowAsync(db, strandCode, generator, prompt, negative, label, parameters, notes);
            if (row is null) return 1;

            if (dryRun) { Console.WriteLine($"DRY-RUN: would save {generator} prompt for strand {strandCode ?? "(global)"}"); return 0; }
            db.CoverImagePrompts.Add(row);
            await db.SaveChangesAsync();
            Console.WriteLine($"Saved prompt: {row.Id}");
            return 0;
        }

        // ── --prompt-id (generate from existing prompt) ───────────────────────
        if (!string.IsNullOrWhiteSpace(promptId))
        {
            if (!Guid.TryParse(promptId, out var pid)) { Console.Error.WriteLine($"Invalid GUID: {promptId}"); return 1; }
            await using var db = await factory.CreateDbContextAsync();
            var row = await db.CoverImagePrompts.FindAsync(pid);
            if (row is null) { Console.Error.WriteLine($"No prompt found with id={promptId}"); return 1; }

            Console.WriteLine($"Generator: {row.Generator}");
            Console.WriteLine($"Label:     {row.Label ?? "(none)"}");
            Console.WriteLine($"Prompt:    {(row.PromptText.Length > 100 ? row.PromptText[..100] + "…" : row.PromptText)}");
            if (dryRun) { Console.WriteLine("DRY-RUN: no API call."); return 0; }

            return await GenerateAndSaveAsync(db, genSvc, row, output);
        }

        // ── Inline: save + generate ────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(generator)) { Console.Error.WriteLine("--generator required"); return 1; }
        if (string.IsNullOrWhiteSpace(prompt))    { Console.Error.WriteLine("--prompt required");    return 1; }

        {
            await using var db = await factory.CreateDbContextAsync();
            var row = await BuildPromptRowAsync(db, strandCode, generator, prompt, negative, label, parameters, notes);
            if (row is null) return 1;

            Console.WriteLine($"Generator: {row.Generator}");
            Console.WriteLine($"Strand:    {strandCode ?? "(global)"}");
            if (dryRun) { Console.WriteLine("DRY-RUN: prompt not saved, no API call."); return 0; }

            db.CoverImagePrompts.Add(row);
            await db.SaveChangesAsync();
            Console.WriteLine($"Prompt saved: {row.Id}");

            return await GenerateAndSaveAsync(db, genSvc, row, output);
        }
    }

    // ── Generation + DB write ─────────────────────────────────────────────────

    private static async Task<int> GenerateAndSaveAsync(
        StreetSamuraiDbContext db,
        CoverImageGeneratorService genSvc,
        CoverImagePrompt row,
        string? outputPath)
    {
        Console.Write($"Calling {row.Generator} API... ");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        byte[] data;
        string contentType;
        try
        {
            (data, contentType) = await genSvc.GenerateAsync(row);
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAILED");
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"done ({sw.Elapsed:m\\:ss}, {data.Length / 1024.0:F0} KB, {contentType})");

        var ext      = contentType == "image/jpeg" ? ".jpg" : ".png";
        var fileName = $"{row.Generator}-{(row.StrandId.HasValue ? row.StrandId.Value.ToString("N")[..8] : "global")}{ext}";

        var asset = new Asset
        {
            Type          = "cover_image",
            StrandId      = row.StrandId,
            UniverseId    = row.Strand?.UniverseId,
            FileName      = fileName,
            ContentType   = contentType,
            Data          = data,
            FileSizeBytes = data.Length,
            Notes         = $"Generated via {row.Generator} API from prompt {row.Id}",
        };
        db.Assets.Add(asset);

        row.AssetId     = asset.Id;
        row.GeneratedAt = DateTime.UtcNow;
        row.UpdatedAt   = DateTime.UtcNow;

        await db.SaveChangesAsync();
        Console.WriteLine($"Asset saved: {asset.Id}  ({fileName})");

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            await File.WriteAllBytesAsync(outputPath, data);
            Console.WriteLine($"Written to:  {outputPath}");
        }

        return 0;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static async Task<CoverImagePrompt?> BuildPromptRowAsync(
        StreetSamuraiDbContext db,
        string? strandCode,
        string generator,
        string promptText,
        string? negative,
        string? label,
        string? parameters,
        string? notes)
    {
        Guid?   strandId  = null;
        Guid?   universeId = null;

        if (!string.IsNullOrWhiteSpace(strandCode))
        {
            var strand = await db.Strands
                .Where(s => s.StrandCode == strandCode)
                .Select(s => new { s.Id, s.UniverseId, s.Title })
                .FirstOrDefaultAsync();

            if (strand is null)
            {
                Console.Error.WriteLine($"No strand found with StrandCode='{strandCode}'");
                return null;
            }
            strandId   = strand.Id;
            universeId = strand.UniverseId;
            Console.WriteLine($"Strand:    {strand.Title}");
        }

        return new CoverImagePrompt
        {
            StrandId       = strandId,
            Generator      = generator,
            Label          = label,
            PromptText     = promptText,
            NegativePrompt = negative,
            Parameters     = parameters,
            Notes          = notes,
        };
    }

    private static string? Arg(string[] args, string flag)
    {
        var idx = Array.IndexOf(args, flag);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
