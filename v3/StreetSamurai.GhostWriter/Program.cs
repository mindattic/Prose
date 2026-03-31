using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.GhostWriter;

/// <summary>
/// Background process that continuously scans story chapters for:
/// - Grammar and flow improvements
/// - Narrative alignment between chapters
/// - World rule consistency
/// - Character voice consistency
///
/// Writes suggestions to ghostwriter/ sidecar JSON files. Does NOT edit
/// the chapter the user is currently working on.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine("╔══════════════════════════════════════╗");
        Console.WriteLine("║  GHOSTWRITER — Narrative Watchdog    ║");
        Console.WriteLine("║  Street Samurai Canon Engine         ║");
        Console.WriteLine("╚══════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();

        var services = new ServiceCollection();
        services.AddStreetSamuraiServices();
        var provider = services.BuildServiceProvider();

        var multiLlm = provider.GetRequiredService<MultiLlmService>();
        var canonDb = provider.GetRequiredService<CanonDatabaseService>();
        var storyRepo = provider.GetRequiredService<IStoryBlockRepository>();
        var paths = provider.GetRequiredService<ICanonPathProvider>();

        var settings = provider.GetRequiredService<SettingsService>();

        if (!settings.GhostWriterEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("GhostWriter is disabled in Settings. Enable it to start scanning.");
            Console.ResetColor();
            return;
        }

        var configuredProviders = multiLlm.GetConfiguredProviders();
        if (configuredProviders.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: No LLM providers configured. Set API keys in Settings.");
            Console.ResetColor();
            return;
        }

        var voterIds = configuredProviders.Take(settings.GhostWriterMaxVoters).Select(p => p.Id).ToList();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Canon root: {paths.CanonRoot}");
        Console.WriteLine($"Voters ({voterIds.Count}): {string.Join(", ", configuredProviders.Take(settings.GhostWriterMaxVoters).Select(p => p.Name))}");
        Console.WriteLine($"Majority threshold: {settings.GhostWriterMajorityThreshold:P0}");
        Console.WriteLine($"Scan interval: {settings.GhostWriterScanIntervalSeconds}s");
        Console.WriteLine($"Rate limit delay: {settings.GhostWriterRateLimitDelayMs}ms");
        Console.WriteLine($"Rescan cooldown: {settings.GhostWriterRescanMinutes}min");
        Console.WriteLine($"Press Ctrl+C to stop.");
        Console.ResetColor();
        Console.WriteLine();

        var suggestionsDir = Path.Combine(paths.CanonRoot, "ghostwriter");
        Directory.CreateDirectory(suggestionsDir);

        // Track what we've already scanned (chapter text hash -> last scan time)
        var scannedHashes = new Dictionary<string, DateTime>();

        while (true)
        {
            try
            {
                var projects = storyRepo.ListProjects();
                foreach (var projectMeta in projects)
                {
                    var project = storyRepo.LoadProject(projectMeta.Id);
                    if (project == null || project.Chapters.Count == 0) continue;

                    project.EnsureChapters();

                    // The most recently modified chapter is the one the user is editing — skip it
                    var activeChapter = project.Chapters
                        .Where(c => !string.IsNullOrWhiteSpace(c.Text))
                        .OrderByDescending(c => c.Modified)
                        .FirstOrDefault();

                    var rulesContext = canonDb.GetLiteraryRulesPrompt();

                    var allSynopses = string.Join("\n\n",
                        project.Chapters
                            .Where(c => !string.IsNullOrWhiteSpace(c.Synopsis))
                            .OrderBy(c => c.Number)
                            .Select(c => $"Chapter {c.Number} ({c.Title}): {c.Synopsis}"));

                    foreach (var chapter in project.Chapters.Where(c => !string.IsNullOrWhiteSpace(c.Text)))
                    {
                        // Skip the active chapter
                        if (activeChapter != null && chapter.Number == activeChapter.Number)
                        {
                            Log($"[{project.Title}] Skipping Ch{chapter.Number} (active)", ConsoleColor.DarkGray);
                            continue;
                        }

                        // Skip if already scanned this exact version
                        var hash = $"{project.Id}_{chapter.Number}_{chapter.Text.Length}_{chapter.Modified.Ticks}";
                        if (scannedHashes.TryGetValue(hash, out var lastScan) && (DateTime.UtcNow - lastScan).TotalMinutes < settings.GhostWriterRescanMinutes)
                            continue;

                        Log($"[{project.Title}] Scanning Ch{chapter.Number}: {chapter.Title}...", ConsoleColor.Cyan);

                        var characterContext = string.Join("\n---\n",
                            project.Characters
                                .Select(name => canonDb.GetCharacterContext(name))
                                .Where(c => c.Length > 0));

                        var system = $"""
                            You are GhostWriter, a background narrative watchdog for cyberpunk fiction
                            set in Meridian 88. You scan chapters for issues and suggest fixes.

                            LITERARY RULES:
                            {rulesContext}

                            KNOWN CHARACTERS:
                            {characterContext}

                            OTHER CHAPTER SYNOPSES (for cross-chapter consistency):
                            {allSynopses}

                            Analyze this chapter and report issues in this format:

                            GRAMMAR: [line-level grammar/spelling fixes]
                            FLOW: [pacing issues, awkward transitions, paragraph structure]
                            CONTINUITY: [contradictions with other chapters or established canon]
                            VOICE: [character voice inconsistencies]
                            SUGGESTIONS: [optional improvements, not errors]

                            Be concise. One line per issue. If the chapter is clean, say "NO ISSUES FOUND."
                            Do NOT rewrite the chapter. Only list issues.
                            """;

                        try
                        {
                            Log($"  Calling {voterIds.Count} LLMs for majority vote...", ConsoleColor.DarkGray);
                            var (consensus, votes) = await multiLlm.MajorityVoteAsync(voterIds, system, chapter.Text);

                            var suggestion = new GhostWriterSuggestion
                            {
                                ProjectId = project.Id,
                                ProjectTitle = project.Title,
                                ChapterNumber = chapter.Number,
                                ChapterTitle = chapter.Title,
                                ScannedAt = DateTime.UtcNow,
                                Issues = consensus.Trim(),
                                VoterCount = votes.Count,
                                IndividualVotes = votes,
                            };

                            var filename = $"{project.Id}_ch{chapter.Number}.json";
                            var filepath = Path.Combine(suggestionsDir, filename);
                            var json = System.Text.Json.JsonSerializer.Serialize(suggestion,
                                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(filepath, json);

                            scannedHashes[hash] = DateTime.UtcNow;

                            if (consensus.Contains("NO CONSENSUS ISSUES", StringComparison.OrdinalIgnoreCase) ||
                                consensus.Contains("NO ISSUES FOUND", StringComparison.OrdinalIgnoreCase))
                                Log($"  Ch{chapter.Number}: Clean ({votes.Count} voters agreed)", ConsoleColor.Green);
                            else
                                Log($"  Ch{chapter.Number}: {CountIssues(consensus)} consensus issues ({votes.Count} voters) -> {filename}", ConsoleColor.Yellow);

                            foreach (var (voter, _) in votes)
                                Log($"    {voter}: responded", ConsoleColor.DarkGray);
                        }
                        catch (Exception ex)
                        {
                            Log($"  Ch{chapter.Number}: Error - {ex.Message}", ConsoleColor.Red);
                        }

                        // Rate limit
                        await Task.Delay(settings.GhostWriterRateLimitDelayMs);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Scan error: {ex.Message}", ConsoleColor.Red);
            }

            Log($"Scan complete. Sleeping {settings.GhostWriterScanIntervalSeconds}s...", ConsoleColor.DarkGray);
            await Task.Delay(settings.GhostWriterScanIntervalSeconds * 1000);
        }
    }

    static void Log(string msg, ConsoleColor color)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"[{ts}] ");
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    static int CountIssues(string text)
    {
        return text.Split('\n').Count(line =>
        {
            var trimmed = line.TrimStart();
            return trimmed.StartsWith('-') || trimmed.StartsWith('*') ||
                   trimmed.StartsWith("GRAMMAR") || trimmed.StartsWith("FLOW") ||
                   trimmed.StartsWith("CONTINUITY") || trimmed.StartsWith("VOICE");
        });
    }
}

record GhostWriterSuggestion
{
    public string ProjectId { get; init; } = "";
    public string ProjectTitle { get; init; } = "";
    public int ChapterNumber { get; init; }
    public string ChapterTitle { get; init; } = "";
    public DateTime ScannedAt { get; init; }
    public string Issues { get; init; } = "";
    public int VoterCount { get; init; }
    public Dictionary<string, string> IndividualVotes { get; init; } = new();
}
