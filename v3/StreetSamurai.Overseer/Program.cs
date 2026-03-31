using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Models.Graph;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Overseer;

/// <summary>
/// Background service that continuously backfills story metadata:
/// - Chapter synopses (for chapters that have text but no synopsis)
/// - Story-level synopsis and plot arc
/// - Story outline
/// - Entity extraction (new characters, tech, locations → repositories)
///
/// Runs outside the story writing thread so there are no pauses.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║  OVERSEER — Metadata Backfill        ║");
        Console.WriteLine("║  Street Samurai Canon Engine          ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        var services = new ServiceCollection();
        services.AddStreetSamuraiServices();
        var provider = services.BuildServiceProvider();

        var llm = provider.GetRequiredService<ILlmService>();
        var storyRepo = provider.GetRequiredService<IStoryBlockRepository>();
        var paths = provider.GetRequiredService<ICanonPathProvider>();
        var settings = provider.GetRequiredService<SettingsService>();
        var charRepo = provider.GetRequiredService<CharacterRepository>();
        var districtRepo = provider.GetRequiredService<DistrictRepository>();
        var weaponRepo = provider.GetRequiredService<WeaponryRepository>();
        var docRepo = provider.GetRequiredService<WorldbuildingDocRepository>();
        var entityExtract = provider.GetRequiredService<EntityExtractionService>();
        var graph = provider.GetRequiredService<WorldGraphService>();

        var configured = await llm.IsConfiguredAsync();
        if (!configured)
        {
            Log("ERROR: LLM not configured.", ConsoleColor.Red);
            return;
        }

        Log($"Canon root: {paths.CanonRoot}", ConsoleColor.DarkGray);
        Log("Press Ctrl+C to stop.\n", ConsoleColor.DarkGray);

        // Track what we've processed
        var processedSynopses = new HashSet<string>();
        var processedEntities = new HashSet<string>();

        while (true)
        {
            try
            {
                var projects = storyRepo.ListProjects();
                foreach (var meta in projects)
                {
                    var project = storyRepo.LoadProject(meta.Id);
                    if (project == null) continue;
                    project.EnsureChapters();

                    bool changed = false;

                    // ── 1. Backfill chapter synopses ──
                    foreach (var ch in project.Chapters.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
                    {
                        var key = $"{project.Id}_ch{ch.Number}_{ch.Text.Length}";
                        if (!string.IsNullOrWhiteSpace(ch.Synopsis) || processedSynopses.Contains(key))
                            continue;

                        Log($"[{project.Title}] Synopsizing Ch{ch.Number}...", ConsoleColor.Cyan);
                        try
                        {
                            var priorCtx = project.GetPriorContext(ch.Number);
                            var synUser = string.IsNullOrWhiteSpace(priorCtx) ? ch.Text
                                : $"PREVIOUS CHAPTERS:\n{priorCtx}\n\nCURRENT CHAPTER:\n{ch.Text}";

                            ch.Synopsis = (await llm.GenerateAsync(
                                "Create a tight, factual synopsis. Capture ALL key events, characters, locations, unresolved threads. 1-2 dense paragraphs.",
                                synUser, 0.2, 2048)).Trim();

                            await Task.Delay(settings.GhostWriterRateLimitDelayMs);

                            ch.Beats = (await llm.GenerateAsync(
                                "List 3-6 major plot beats as bullet points. Each: \"- [what happened]\". Focus on decisions, revelations, conflicts.",
                                ch.Text, 0.2, 1024)).Trim();

                            processedSynopses.Add(key);
                            changed = true;
                            Log($"  Ch{ch.Number}: Synopsis + beats generated", ConsoleColor.Green);

                            await Task.Delay(settings.GhostWriterRateLimitDelayMs);
                        }
                        catch (Exception ex) { Log($"  Ch{ch.Number}: {ex.Message}", ConsoleColor.Red); }
                    }

                    // ── 2. Update story-level synopsis + plot arc ──
                    var chaptersWithSynopsis = project.Chapters.Count(c => !string.IsNullOrWhiteSpace(c.Synopsis));
                    if (chaptersWithSynopsis >= 2 && changed)
                    {
                        Log($"[{project.Title}] Updating story overview...", ConsoleColor.Cyan);
                        try
                        {
                            var allSyn = string.Join("\n\n", project.Chapters.OrderBy(c => c.Number)
                                .Where(c => !string.IsNullOrWhiteSpace(c.Synopsis))
                                .Select(c => $"Chapter {c.Number} ({c.Title}): {c.Synopsis}"));

                            project.StorySynopsis = (await llm.GenerateAsync(
                                "Synthesize into ONE story overview. 2-3 paragraphs: premise, major events, character states, unresolved threads.",
                                allSyn, 0.2, 2048)).Trim();

                            await Task.Delay(settings.GhostWriterRateLimitDelayMs);

                            project.PlotArc = (await llm.GenerateAsync(
                                "Describe the plot arc in 3-5 bullet points. Format: \"- [turning point]\". Focus on trajectory.",
                                allSyn, 0.2, 1024)).Trim();

                            Log("  Story overview + plot arc updated", ConsoleColor.Green);
                            await Task.Delay(settings.GhostWriterRateLimitDelayMs);
                        }
                        catch (Exception ex) { Log($"  Overview: {ex.Message}", ConsoleColor.Red); }
                    }

                    // ── 3. Update outline ──
                    if (changed && chaptersWithSynopsis >= 2)
                    {
                        Log($"[{project.Title}] Updating outline...", ConsoleColor.Cyan);
                        try
                        {
                            var allText = string.Join("\n\n", project.Chapters.OrderBy(c => c.Number)
                                .Where(c => !string.IsNullOrWhiteSpace(c.Text))
                                .Select(c => $"=== CHAPTER {c.Number}: {c.Title} ===\n{c.Text}"));

                            project.Outline = (await llm.GenerateAsync(
                                "Create a structured chapter-by-chapter outline. Per chapter: key events, characters, locations, unresolved threads. Then: CONTINUITY CHECK (contradictions, dropped threads, voice issues). Then: STORY ARC STATUS (where it is, what's next).",
                                allText, 0.2, 8192)).Trim();

                            Log("  Outline updated", ConsoleColor.Green);
                            await Task.Delay(settings.GhostWriterRateLimitDelayMs);
                        }
                        catch (Exception ex) { Log($"  Outline: {ex.Message}", ConsoleColor.Red); }
                    }

                    // ── 4. Entity extraction → repositories ──
                    foreach (var ch in project.Chapters.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
                    {
                        var entKey = $"{project.Id}_ch{ch.Number}_ent_{ch.Text.Length}";
                        if (processedEntities.Contains(entKey)) continue;

                        Log($"[{project.Title}] Extracting entities from Ch{ch.Number}...", ConsoleColor.Cyan);
                        try
                        {
                            var (entities, relationships) = await entityExtract.ExtractAndMergeAsync(ch.Text, project.Id);
                            processedEntities.Add(entKey);

                            if (entities > 0 || relationships > 0)
                                Log($"  +{entities} entities, +{relationships} relationships → graph", ConsoleColor.Green);
                            else
                                Log("  No new entities", ConsoleColor.DarkGray);

                            await Task.Delay(settings.GhostWriterRateLimitDelayMs);
                        }
                        catch (Exception ex) { Log($"  Extract: {ex.Message}", ConsoleColor.Red); }
                    }

                    // Save if anything changed
                    if (changed)
                    {
                        project.SyncBlocksFromChapters();
                        storyRepo.SaveProject(project);
                        Log($"[{project.Title}] Saved", ConsoleColor.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Scan error: {ex.Message}", ConsoleColor.Red);
            }

            Log($"Cycle complete. Sleeping {settings.GhostWriterScanIntervalSeconds}s...\n", ConsoleColor.DarkGray);
            await Task.Delay(settings.GhostWriterScanIntervalSeconds * 1000);
        }
    }

    static void Log(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
    }
}
