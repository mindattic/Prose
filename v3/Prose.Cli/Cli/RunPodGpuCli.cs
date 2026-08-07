using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --runpod &lt;status|stop|start|terminate&gt; [--pod &lt;id&gt;]</c> — manage the rented RunPod
/// review pod via the RunPod REST API (v1, <c>https://rest.runpod.io/v1</c>). The API key is
/// resolved from the shared MindAttic credential vault (provider id <c>runpod</c>); no secret on
/// the command line.
///
/// <list type="bullet">
/// <item><c>status</c> — list pods (id, name, state, gpu, $/hr). Safe/read-only.</item>
/// <item><c>stop</c> — pause the pod (releases the GPU, keeps the disk; small storage charge).</item>
/// <item><c>start</c> — resume a stopped pod.</item>
/// <item><c>terminate</c> — permanently delete the pod (frees everything; billing stops). Data not
///   on a network volume is lost.</item>
/// </list>
/// When <c>--pod</c> is omitted and exactly one pod exists, that one is used; for the destructive
/// <c>terminate</c>/<c>stop</c>/<c>start</c> actions with multiple pods, an explicit id is required.
/// </summary>
public static class RunPodGpuCli
{
    private const string ApiBase = "https://rest.runpod.io/v1";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string action = "status"; string? podId = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--runpod": if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) action = args[++i].ToLowerInvariant(); break;
                case "--pod":    if (i + 1 < args.Length) podId = args[++i]; break;
            }
        }
        if (action is not ("status" or "stop" or "start" or "terminate"))
        {
            Console.Error.WriteLine($"[runpod] Unknown action '{action}'. Use: status | stop | start | terminate.");
            return 1;
        }

        var settings = services.GetRequiredService<SettingsService>();
        var key = settings.RunPodApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("[runpod] No RunPod API key in the vault (provider id 'runpod'). "
                + "Add %APPDATA%/MindAttic/LLM/runpod.json with an apiKey, or set RUNPOD_API_KEY.");
            return 1;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        try
        {
            // Always fetch the pod list first — drives status, and resolves a lone pod.
            var listJson = await http.GetStringAsync($"{ApiBase}/pods");
            using var doc = JsonDocument.Parse(listJson);
            // The list endpoint may return a bare array or an object with a "pods"/"data" array.
            var root = doc.RootElement;
            var pods = root.ValueKind == JsonValueKind.Array ? root.EnumerateArray().ToList()
                : root.TryGetProperty("pods", out var p1) && p1.ValueKind == JsonValueKind.Array ? p1.EnumerateArray().ToList()
                : root.TryGetProperty("data", out var p2) && p2.ValueKind == JsonValueKind.Array ? p2.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (action == "status")
            {
                if (pods.Count == 0) { Console.WriteLine("[runpod] No pods."); return 0; }
                Console.WriteLine($"[runpod] {pods.Count} pod(s):");
                foreach (var pod in pods)
                    Console.WriteLine($"   id {Get(pod, "id")} · {Get(pod, "name")} · {Get(pod, "desiredStatus")} · {Gpu(pod)} · ${Get(pod, "costPerHr")}/hr");
                return 0;
            }

            // Resolve the target pod for a control action.
            if (string.IsNullOrWhiteSpace(podId))
            {
                if (pods.Count == 1) podId = Get(pods[0], "id");
                else { Console.Error.WriteLine($"[runpod] {pods.Count} pods — pass --pod <id> for '{action}'."); return 1; }
            }

            HttpResponseMessage res = action switch
            {
                "terminate" => await http.DeleteAsync($"{ApiBase}/pods/{podId}"),
                "stop"      => await http.PostAsync($"{ApiBase}/pods/{podId}/stop", null),
                _           => await http.PostAsync($"{ApiBase}/pods/{podId}/start", null), // start
            };

            var body = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"[runpod] {action} pod {podId} → HTTP {(int)res.StatusCode}. {Trunc(body, 300)}");
            if (action == "terminate" && res.IsSuccessStatusCode)
                Console.WriteLine("[runpod] Pod terminated — billing stopped. (A 204 with empty body is success.)");
            return res.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[runpod] RunPod API call failed: {ex.Message}");
            return 1;
        }
    }

    private static string Get(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";

    /// <summary>Best-effort GPU label — the list shape nests it differently across API versions.</summary>
    private static string Gpu(JsonElement e)
    {
        foreach (var key in new[] { "gpuTypeId", "gpuType", "machineType" })
            if (e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
        if (e.TryGetProperty("machine", out var m) && m.ValueKind == JsonValueKind.Object)
            foreach (var key in new[] { "gpuTypeId", "gpuType", "gpuDisplayName" })
                if (m.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
        return "";
    }

    private static string Trunc(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s[..n] + "…");
}
