using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// In-process SPO fact extraction from all entity JSON files.
/// Supports pause/resume: Pause() suspends the batch loop without losing progress.
/// Resume() unblocks it. Auto-paused by LoreTriples.razor on Dispose (navigate-away).
/// </summary>
public class LoreTripleExtractionService
{
    public record ExtractionProgress(int Processed, int Total, string Phase, string Current = "");

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IPathProvider paths;
    private readonly LoreTripleService factDb;
    private readonly ILogger<LoreTripleExtractionService> log;

    private CancellationTokenSource? cts;
    private volatile TaskCompletionSource<bool>? pauseTcs;
    private volatile int resumeFromIndex;
    private List<EntityInfo> allEntities = [];

    public bool IsRunning { get; private set; }
    public bool IsPaused => pauseTcs != null;
    public ExtractionProgress Progress { get; private set; } = new(0, 0, "Idle");
    public event Action? StateChanged;

    private static readonly string[] EntityDirs =
    [
        "people", "synthetics", "automata", "creatures",
        "corponations", "subsidiaries", "factions",
        "places", "flyover_entities", "wasteland_entities",
        "weaponry", "ammunition", "cyberware", "equipment",
        "apparel", "genemods", "pharmaceuticals",
        "transportation", "materials", "technology",
        "lab_specimens", "psionics", "contracts",
        "archetypes", "consumer_goods"
    ];

    public LoreTripleExtractionService(
        IServiceScopeFactory scopeFactory,
        IPathProvider paths,
        LoreTripleService factDb,
        ILogger<LoreTripleExtractionService> log)
    {
        this.scopeFactory = scopeFactory;
        this.paths = paths;
        this.factDb = factDb;
        this.log = log;
    }

    // ── Lifecycle ─────────────────────────────────────────────

    public async Task RunAsync(bool resume = false)
    {
        if (IsRunning) return;
        IsRunning = true;
        cts = new CancellationTokenSource();
        if (!resume)
        {
            resumeFromIndex = 0;
            allEntities = [];
        }
        try
        {
            await RunInternalAsync(cts.Token, resume);
        }
        catch (OperationCanceledException)
        {
            Notify("Cancelled");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Fact extraction failed");
            Notify("Error: " + ex.Message);
        }
        finally
        {
            IsRunning = false;
            cts.Dispose();
            cts = null;
            StateChanged?.Invoke();
        }
    }

    public void Pause()
    {
        if (!IsRunning || IsPaused) return;
        pauseTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        var tcs = pauseTcs;
        pauseTcs = null;
        if (!IsRunning)
            _ = RunAsync(resume: true);   // task died while paused — restart from checkpoint
        else
            tcs?.TrySetResult(true);
        StateChanged?.Invoke();
    }

    public void Cancel()
    {
        // Hard stop — clears checkpoint so next Run is fresh
        pauseTcs = null;
        resumeFromIndex = 0;
        allEntities = [];
        cts?.Cancel();
    }

    // ── Core loop ─────────────────────────────────────────────

    private async Task RunInternalAsync(CancellationToken ct, bool resume)
    {
        Notify(resume ? "Resuming" : "Initializing", resumeFromIndex, 0);
        factDb.EnsureSchema();
        if (!resume)
            factDb.ClearExtractionData();

        if (allEntities.Count == 0)
        {
            foreach (var dir in EntityDirs)
            {
                var dirPath = Path.Combine(paths.EngineDataDir, dir);
                if (!Directory.Exists(dirPath)) continue;
                foreach (var file in Directory.GetFiles(dirPath, "*.json"))
                {
                    try
                    {
                        var info = ReadEntityInfo(file, dir);
                        if (info != null) allEntities.Add(info);
                    }
                    catch { }
                }
            }
            log.LogInformation("Fact extraction: {Count} entities queued", allEntities.Count);
        }

        const int batchSize = 12;
        var pending = new List<FactTriple>(500);

        using var scope = scopeFactory.CreateScope();
        var claude = scope.ServiceProvider.GetRequiredService<ClaudeService>();

        for (int i = resumeFromIndex; i < allEntities.Count; i += batchSize)
        {
            ct.ThrowIfCancellationRequested();

            // Pause gate — suspend until Resume() unblocks the TCS or Cancel() fires the CT
            if (pauseTcs != null)
            {
                resumeFromIndex = i;
                Notify("Paused", i, allEntities.Count);
                await pauseTcs.Task.WaitAsync(ct);
                Notify("Extracting", i, allEntities.Count);
            }

            var batch = allEntities.Skip(i).Take(batchSize).ToList();
            Notify("Extracting", i, allEntities.Count, batch[0].Name);

            var triples = await ExtractBatchAsync(claude, batch, ct);
            pending.AddRange(triples);

            resumeFromIndex = i + batchSize;

            if (pending.Count >= 500)
            {
                factDb.WriteTriples(pending);
                pending.Clear();
            }
        }

        if (pending.Count > 0)
            factDb.WriteTriples(pending);

        var total = allEntities.Count;
        Notify("Building consensus", total, total);
        factDb.BuildConsensus();

        resumeFromIndex = 0;
        allEntities = [];
        Notify("Done", total, total);
    }

    // ── Extraction helpers ────────────────────────────────────

    private static EntityInfo? ReadEntityInfo(string file, string dir)
    {
        using var stream = File.OpenRead(file);
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("name", out var nameProp)) return null;
        var name = nameProp.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(name)) return null;

        return new EntityInfo(
            SourceFile: file,
            SourceRepo: dir,
            Name: name,
            Type: root.TryGetProperty("type", out var t) ? t.GetString() ?? dir : dir,
            Description: Truncate(root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "", 250),
            Zone: root.TryGetProperty("zone", out var z) ? z.GetString() ?? "" : "",
            Tags: root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array
                ? string.Join(", ", tags.EnumerateArray().Take(6).Select(x => x.GetString() ?? "").Where(s => s != ""))
                : ""
        );
    }

    private async Task<List<FactTriple>> ExtractBatchAsync(ClaudeService claude, List<EntityInfo> batch, CancellationToken ct)
    {
        var payload = batch.Select(e => new
        {
            name = e.Name,
            type = e.Type,
            zone = e.Zone.Length > 0 ? e.Zone : (string?)null,
            tags = e.Tags.Length > 0 ? e.Tags : (string?)null,
            description = e.Description.Length > 0 ? e.Description : (string?)null
        });

        var batchJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var user = $$"""
            Extract 5-10 factual claims per entity as subject-predicate-object triples.
            Subject must be the entity name exactly. Predicates in snake_case (e.g. type, located_in_zone, affiliated_with, manufactured_by, member_of).
            Return a JSON object keyed by entity name: {"Name": [{"predicate":"...","object":"..."}]}

            Entities:
            {{batchJson}}
            """;

        try
        {
            var response = await claude.GenerateAsync(
                system: "You extract factual claims from worldbuilding entity data as SPO triples. Return only valid JSON with no markdown code blocks.",
                user: user,
                temperature: 0,
                maxTokens: 2048,
                model: "claude-haiku-4-5-20251001",
                ct: ct);

            return ParseTriples(response, batch);
        }
        catch (Exception ex)
        {
            log.LogWarning("Batch extraction failed ({Names}): {Msg}", string.Join(", ", batch.Select(b => b.Name)), ex.Message);
            return [];
        }
    }

    private static List<FactTriple> ParseTriples(string json, List<EntityInfo> batch)
    {
        var result = new List<FactTriple>();
        json = json.Trim();

        if (json.StartsWith("```"))
        {
            var lines = json.Split('\n');
            json = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var byName = batch.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!byName.TryGetValue(prop.Name, out var entity)) continue;
                if (prop.Value.ValueKind != JsonValueKind.Array) continue;

                foreach (var item in prop.Value.EnumerateArray())
                {
                    if (!item.TryGetProperty("predicate", out var pred) ||
                        !item.TryGetProperty("object", out var obj)) continue;

                    var predStr = pred.GetString()?.Trim() ?? "";
                    var objStr  = obj.GetString()?.Trim()  ?? "";
                    if (string.IsNullOrEmpty(predStr) || string.IsNullOrEmpty(objStr)) continue;

                    result.Add(new FactTriple
                    {
                        SourceFile = entity.SourceFile,
                        SourceRepo = entity.SourceRepo,
                        EntityName = entity.Name,
                        Subject    = entity.Name,
                        Predicate  = predStr,
                        Object     = objStr
                    });
                }
            }
        }
        catch { }

        return result;
    }

    private void Notify(string phase, int processed = 0, int total = 0, string current = "")
    {
        Progress = new(processed, total, phase, current);
        StateChanged?.Invoke();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}

public record EntityInfo(string SourceFile, string SourceRepo, string Name, string Type, string Description, string Zone, string Tags);

public class FactTriple
{
    public string SourceFile { get; set; } = "";
    public string SourceRepo { get; set; } = "";
    public string EntityName  { get; set; } = "";
    public string Subject     { get; set; } = "";
    public string Predicate   { get; set; } = "";
    public string Object      { get; set; } = "";
}
