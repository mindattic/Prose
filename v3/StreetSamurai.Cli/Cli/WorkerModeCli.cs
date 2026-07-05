using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MindAttic.Legion;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Worker mode: claims work from the coordinator REST API, runs it against a local LLM,
/// and POSTs results back. Stateless — no direct DB access required.
///
///   ss --worker-mode
///     --queue-url  https://streetsamurai.azurewebsites.net/api/worker
///     --worker-key SECRET_API_KEY           (same key configured on coordinator)
///     --worker-id  pod-1                    (opaque label for logging)
///     --local-url  https://pod-8000.proxy.runpod.net
///     --local-key  vllm_key_...
///     --local-model qwen2.5-72b-32k
///     [--work-type entity-review|node-review|beat-write]   (default: entity-review)
///     [--batch 20]
///     [--loop]     keep claiming until queue is empty
/// </summary>
public static class WorkerModeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var queueUrl  = ArgValue(args, "--queue-url")  ?? "https://streetsamurai.azurewebsites.net/api/worker";
        var workerKey = ArgValue(args, "--worker-key") ?? "";
        var workerId  = ArgValue(args, "--worker-id")  ?? Environment.MachineName;
        var localUrl  = ArgValue(args, "--local-url");
        var localKey  = ArgValue(args, "--local-key")  ?? "local";
        var localModel= ArgValue(args, "--local-model")?? "qwen2.5-72b-32k";
        var workType  = ArgValue(args, "--work-type")  ?? "entity-review";
        var batch     = int.TryParse(ArgValue(args, "--batch"), out var b) ? b : 20;
        var loop      = args.Contains("--loop");

        if (string.IsNullOrWhiteSpace(localUrl))
        {
            Console.Error.WriteLine("--local-url is required (URL of the local LLM endpoint)");
            return 1;
        }
        if (string.IsNullOrWhiteSpace(workerKey))
        {
            Console.Error.WriteLine("--worker-key is required (must match WorkerSettings:ApiKey on coordinator)");
            return 1;
        }

        // SS-A44: entity-review / node-review work types cast score ballots and
        // are disabled by default. beat-write is prose generation and is never
        // gated. Require --allow-votes to claim ballot work.
        if (workType is "entity-review" or "node-review")
        {
            var votingGate = sp.GetRequiredService<VotingGate>();
            try { votingGate.EnsureAllowed($"worker-mode {workType}", args.Contains("--allow-votes")); }
            catch (VotingDisabledException ex) { Console.Error.WriteLine($"[worker] {ex.Message}"); return 1; }
        }

        var legion = sp.GetRequiredService<LegionClient>();
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("X-Worker-Key", workerKey);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        Console.WriteLine($"[worker] id={workerId} type={workType} batch={batch} loop={loop}");
        Console.WriteLine($"[worker] coordinator={queueUrl}");
        Console.WriteLine($"[worker] llm={localUrl} model={localModel}");

        int totalDone = 0;
        do
        {
            // 1. Claim a batch.
            var claimResp = await http.GetAsync($"{queueUrl}/claim?workerId={Uri.EscapeDataString(workerId)}&workType={workType}&batch={batch}");
            if (!claimResp.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"[worker] claim failed: {claimResp.StatusCode}");
                return 1;
            }
            var claimJson = await claimResp.Content.ReadAsStringAsync();
            var items = JsonSerializer.Deserialize<List<WorkItemDto>>(claimJson, Json);

            if (items == null || items.Count == 0)
            {
                Console.WriteLine("[worker] queue empty — done.");
                break;
            }
            Console.WriteLine($"[worker] claimed {items.Count} items");

            // 2. Process each item.
            foreach (var item in items)
            {
                WorkerResult result;
                try
                {
                    result = workType switch
                    {
                        "entity-review" => await ProcessEntityReviewAsync(item, legion, localUrl, localKey, localModel),
                        "node-review" => await ProcessNodeReviewAsync(item, legion, localUrl, localKey, localModel),
                        "beat-write"    => await ProcessBeatWriteAsync(item, legion, localUrl, localKey, localModel),
                        _ => throw new InvalidOperationException($"unknown workType '{workType}'"),
                    };
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[worker] item {item.QueueId} FAILED: {ex.Message}");
                    result = new WorkerResult
                    {
                        WorkType = workType, WorkerId = workerId,
                        QueueId  = item.QueueId, Failed = true, ErrorMessage = ex.Message,
                    };
                }
                result.WorkerId = workerId;
                result.Model    = localModel;

                // 3. Submit results.
                var submitBody = JsonSerializer.Serialize(result, Json);
                var submitResp = await http.PostAsync($"{queueUrl}/submit",
                    new StringContent(submitBody, Encoding.UTF8, "application/json"));
                if (!submitResp.IsSuccessStatusCode)
                    Console.Error.WriteLine($"[worker] submit failed for {item.QueueId}: {submitResp.StatusCode}");
                else
                    Console.WriteLine($"[worker] submitted {item.TargetName}");

                totalDone++;
            }
        }
        while (loop);

        Console.WriteLine($"[worker] total processed: {totalDone}");
        return 0;
    }

    // ── Entity review ─────────────────────────────────────────────────────────

    private static async Task<WorkerResult> ProcessEntityReviewAsync(
        WorkItemDto item, LegionClient legion, string localUrl, string localKey, string localModel)
    {
        if (item.PayloadJson == null) throw new InvalidOperationException("missing payload");
        using var doc = JsonDocument.Parse(item.PayloadJson);
        var root = doc.RootElement;
        var entityId   = root.GetProperty("entityId").GetString()!;
        var entityType = root.GetProperty("entityType").GetString()!;
        var entityName = root.GetProperty("entityName").GetString()!;
        var desc       = root.GetProperty("description").GetString() ?? "";
        var ballots    = root.GetProperty("ballots").GetInt32();
        var proseCount = root.GetProperty("proseCount").GetInt32();

        // Sample personas from the in-process PersonaLibrary (not embedded in payload).
        var personaPool = PersonaLibrary.Enriched.ToList();
        var rng = Random.Shared;
        for (int pi = 0; pi < Math.Min(ballots, personaPool.Count); pi++)
        {
            int pj = rng.Next(pi, personaPool.Count);
            (personaPool[pi], personaPool[pj]) = (personaPool[pj], personaPool[pi]);
        }
        var personas = personaPool.Take(ballots)
            .Select(p => new PersonaInfo(p.Id, p.Name, FirstLine(p.PersonalityMarkdown)))
            .ToList();

        // Content hash = stable fingerprint for dedup.
        var contentHash = ComputeHash($"{entityId}:{desc[..Math.Min(400, desc.Length)]}");

        var ballotResults = new List<BallotResult>();
        var edges         = new List<EdgeResult>();

        // Run N ballots across the persona list.
        var sem = new SemaphoreSlim(4); // 4 concurrent LLM calls
        await Task.WhenAll(Enumerable.Range(0, ballots).Select(async i =>
        {
            await sem.WaitAsync();
            try
            {
                var persona = personas[i % personas.Count];
                var sysPrompt = BuildEntityBallotSystemPrompt(persona, entityType);
                var userMsg   = BuildEntityBallotUserMsg(entityName, entityType, desc, i < proseCount);

                var raw = await legion.CallAsync("local", localKey, localModel, sysPrompt, userMsg, localUrl, maxTokens: 400);
                if (TryParseEntityBallot(raw, out var score, out var review, out var improvements, out var contradictions))
                {
                    lock (ballotResults)
                        ballotResults.Add(new BallotResult
                        {
                            EntityId = entityId, EntityType = entityType, EntityName = entityName,
                            PersonaId = persona.Id, PersonaName = persona.Name, PersonaBlurb = persona.Blurb,
                            Score = score, ReviewText = review, Improvements = improvements,
                            Contradictions = contradictions, ContentHash = contentHash,
                        });
                }
            }
            catch (Exception ex) { Console.Error.WriteLine($"[worker] ballot error: {ex.Message}"); }
            finally { sem.Release(); }
        }));

        // One relationship-extraction call.
        try
        {
            var relSys = "You are a world-graph editor. Given an entity description, identify relationships to other named entities. Return JSON only.";
            var relMsg = $"Entity: {entityName} ({entityType})\n\nDescription:\n{desc[..Math.Min(1000, desc.Length)]}\n\n" +
                         "Return JSON:\n{\"relationships\":[{\"targetName\":\"...\",\"relationType\":\"...\",\"description\":\"...\",\"sentiment\":\"positive|neutral|negative\",\"confidence\":0.9}]}";
            var relRaw = await legion.CallAsync("local", localKey, localModel, relSys, relMsg, localUrl, maxTokens: 400);
            var parsed = ParseRelationships(relRaw);
            foreach (var r in parsed)
                edges.Add(new EdgeResult
                {
                    SourceEntityId = entityId, TargetEntityId = r.TargetId ?? "",
                    RelationType = r.RelationType, Description = r.Description,
                    Sentiment = r.Sentiment, Confidence = r.Confidence,
                });
        }
        catch { /* edge extraction is best-effort */ }

        return new WorkerResult
        {
            WorkType = "entity-review", QueueId = item.QueueId,
            EntityBallots = ballotResults, Edges = edges.Count > 0 ? edges : null,
        };
    }

    // ── Node review ─────────────────────────────────────────────────────────

    private static async Task<WorkerResult> ProcessNodeReviewAsync(
        WorkItemDto item, LegionClient legion, string localUrl, string localKey, string localModel)
    {
        if (item.PayloadJson == null) throw new InvalidOperationException("missing payload");
        using var doc = JsonDocument.Parse(item.PayloadJson);
        var root        = doc.RootElement;
        var nodeId    = root.GetProperty("nodeId").GetString()!;
        var nodeTitle = root.GetProperty("nodeTitle").GetString()!;
        var nodeText  = root.GetProperty("nodeText").GetString()!;
        var readers     = root.TryGetProperty("readers", out var rProp) ? rProp.GetInt32() : 5;
        // Sample reader personas locally.
        var allPersonas = PersonaLibrary.Enriched.ToList();
        for (int pi = 0; pi < Math.Min(readers, allPersonas.Count); pi++)
        {
            int pj = Random.Shared.Next(pi, allPersonas.Count);
            (allPersonas[pi], allPersonas[pj]) = (allPersonas[pj], allPersonas[pi]);
        }
        var personas = allPersonas.Take(readers)
            .Select(p => new PersonaInfo(p.Id, p.Name, FirstLine(p.PersonalityMarkdown)))
            .ToList();

        var contentHash = ComputeHash($"{nodeId}:{nodeText[..Math.Min(600, nodeText.Length)]}");
        var votes = new List<PersonaVoteResult>();

        await Task.WhenAll(personas.Select(async persona =>
        {
            var sysPrompt = $"You are {persona.Name}, a committed cyberpunk fiction reader. " +
                            $"{persona.Blurb ?? "You are passionate about gritty, authentic stories."} " +
                            "Read the following story and score it 1-100. Return ONLY JSON: " +
                            "{\"score\":NN,\"improvements\":\"...\",\"contradictions\":\"...or null\"}";
            var userMsg = $"TITLE: {nodeTitle}\n\n{nodeText[..Math.Min(8000, nodeText.Length)]}";

            try
            {
                var raw = await legion.CallAsync("local", localKey, localModel, sysPrompt, userMsg, localUrl, maxTokens: 400);
                if (TryParseNodeVote(raw, out var score, out var improvements, out var contradictions))
                {
                    lock (votes)
                        votes.Add(new PersonaVoteResult
                        {
                            PersonaId = persona.Id, PersonaName = persona.Name, PersonaBlurb = persona.Blurb,
                            Score = score, Improvements = improvements, Contradictions = contradictions,
                        });
                }
            }
            catch (Exception ex) { Console.Error.WriteLine($"[worker] node vote error: {ex.Message}"); }
        }));

        return new WorkerResult
        {
            WorkType = "node-review", QueueId = item.QueueId,
            NodeReview = new NodeReviewResult { NodeId = nodeId, ContentHash = contentHash, PersonaVotes = votes },
        };
    }

    // ── Beat write ────────────────────────────────────────────────────────────

    private static async Task<WorkerResult> ProcessBeatWriteAsync(
        WorkItemDto item, LegionClient legion, string localUrl, string localKey, string localModel)
    {
        if (item.PayloadJson == null) throw new InvalidOperationException("missing payload");
        using var doc = JsonDocument.Parse(item.PayloadJson);
        var root       = doc.RootElement;
        var beatId     = root.GetProperty("beatId").GetString()!;
        var nodeSlug = root.GetProperty("nodeSlug").GetString()!;
        var beatIndex  = root.GetProperty("beatIndex").GetInt32();
        var totalBeats = root.GetProperty("totalBeats").GetInt32();
        var beatGoal   = root.GetProperty("beatGoal").GetString()!;
        var beatSeed   = root.GetProperty("beatSeed").GetString() ?? "";

        // If the coordinator baked full prompts, use them; otherwise build a lightweight version.
        string sysPrompt, userMsg;
        if (root.TryGetProperty("systemPrompt", out var sp) && root.TryGetProperty("userPrompt", out var up))
        {
            sysPrompt = sp.GetString()!;
            userMsg   = up.GetString()!;
        }
        else
        {
            sysPrompt = "You are a professional cyberpunk fiction author (2225 GLMZ setting). " +
                        "Write vivid, grounded prose. Close-third POV. No purple prose. Voice is dry, precise, layered.";
            userMsg   = $"NODE: {nodeSlug}\n" +
                        $"Beat {beatIndex + 1} of {totalBeats}\n\n" +
                        $"GOAL: {beatGoal}\n\n" +
                        (string.IsNullOrWhiteSpace(beatSeed) ? "" : $"SEED NOTES:\n{beatSeed}\n\n") +
                        "Write the full scene for this beat. 600-900 words.";
        }

        var prose = await legion.CallAsync("local", localKey, localModel, sysPrompt, userMsg, localUrl, maxTokens: 1500);

        return new WorkerResult
        {
            WorkType = "beat-write", QueueId = item.QueueId,
            BeatWrite = new BeatWriteResult { BeatId = beatId, ProseText = prose ?? "" },
        };
    }

    // ── Prompt builders ───────────────────────────────────────────────────────

    private static string BuildEntityBallotSystemPrompt(PersonaInfo persona, string entityType)
        => $"You are {persona.Name}, a hardcore cyberpunk fan. {persona.Blurb ?? ""} " +
           $"Rate this {entityType} entity from the GLMZ 2225 world. " +
           "Score 1–100 and give brutally honest feedback. " +
           "Return ONLY JSON: {\"score\":NN,\"review\":\"...\",\"improvements\":\"...\",\"contradictions\":\"...\"}";

    private static string BuildEntityBallotUserMsg(string name, string type, string desc, bool includeProse)
        => $"Name: {name}\nType: {type}\n\nDescription:\n{desc[..Math.Min(1000, desc.Length)]}";

    // ── Parsers ───────────────────────────────────────────────────────────────

    private static bool TryParseEntityBallot(string? raw, out int score, out string? review,
        out string? improvements, out string? contradictions)
    {
        score = 50; review = null; improvements = null; contradictions = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var o = raw.IndexOf('{'); var c = raw.LastIndexOf('}');
        if (o < 0 || c <= o) return false;
        try
        {
            using var doc = JsonDocument.Parse(raw[o..(c + 1)]);
            var r = doc.RootElement;
            if (r.TryGetProperty("score", out var s)) score = s.GetInt32();
            if (r.TryGetProperty("review", out var rv)) review = rv.GetString();
            if (r.TryGetProperty("improvements", out var imp)) improvements = imp.GetString();
            if (r.TryGetProperty("contradictions", out var con)) contradictions = con.ValueKind == JsonValueKind.Null ? null : con.GetString();
            return true;
        }
        catch { return false; }
    }

    private static bool TryParseNodeVote(string? raw, out int score, out string? improvements, out string? contradictions)
    {
        score = 50; improvements = null; contradictions = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var o = raw.IndexOf('{'); var c = raw.LastIndexOf('}');
        if (o < 0 || c <= o) return false;
        try
        {
            using var doc = JsonDocument.Parse(raw[o..(c + 1)]);
            var r = doc.RootElement;
            if (r.TryGetProperty("score", out var s)) score = s.GetInt32();
            if (r.TryGetProperty("improvements", out var g)) improvements = g.GetString();
            if (r.TryGetProperty("contradictions", out var con)) contradictions = con.ValueKind == JsonValueKind.Null ? null : con.GetString();
            return true;
        }
        catch { return false; }
    }

    private static List<RelExtract> ParseRelationships(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var o = raw.IndexOf('{'); var c = raw.LastIndexOf('}');
        if (o < 0 || c <= o) return [];
        try
        {
            using var doc = JsonDocument.Parse(raw[o..(c + 1)]);
            if (!doc.RootElement.TryGetProperty("relationships", out var arr)) return [];
            return arr.EnumerateArray().Select(el =>
            {
                var targetName  = el.TryGetProperty("targetName", out var t) ? t.GetString() : null;
                var relType     = el.TryGetProperty("relationType", out var rt) ? rt.GetString() ?? "related_to" : "related_to";
                var description = el.TryGetProperty("description", out var d) ? d.GetString() : null;
                var sentiment   = el.TryGetProperty("sentiment", out var s) ? s.GetString() : "neutral";
                var confidence  = el.TryGetProperty("confidence", out var cf) ? cf.GetDouble() : 0.7;
                return new RelExtract(null, targetName, relType, description, sentiment, confidence);
            }).Where(r => r.TargetName != null && r.Confidence >= 0.6).ToList();
        }
        catch { return []; }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLower();
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    private static string? FirstLine(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var nl = s.IndexOfAny(['\r', '\n']);
        return nl < 0 ? s : s[..nl].Trim();
    }

    private record PersonaInfo(string Id, string Name, string? Blurb);
    private record RelExtract(string? TargetId, string? TargetName, string RelationType, string? Description, string? Sentiment, double Confidence);
}

// ── Minimal DTOs used by the worker (coordinator uses the full ones in Core) ──

internal class WorkItemDto
{
    public Guid    QueueId     { get; set; }
    public string  WorkType    { get; set; } = "";
    public string  TargetId    { get; set; } = "";
    public string  TargetType  { get; set; } = "";
    public string  TargetName  { get; set; } = "";
    public string? PayloadJson { get; set; }
}
