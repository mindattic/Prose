using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --gpu &lt;status|stop|start|destroy&gt; [--instance &lt;id&gt;]</c> — manage the rented
/// vast.ai review box via the vast REST API (v1). The API key is resolved from the shared
/// MindAttic credential vault (provider id <c>vast</c>); no secret on the command line.
///
/// <list type="bullet">
/// <item><c>status</c> — list instances (id, state, ip, gpu). Safe/read-only.</item>
/// <item><c>stop</c> — pause the instance (keeps the disk/model; ~$0.21/day).</item>
/// <item><c>start</c> — resume a stopped instance (note: vast may assign a NEW ip/port).</item>
/// <item><c>destroy</c> — terminate the instance (frees everything; $0).</item>
/// </list>
/// When <c>--instance</c> is omitted and exactly one instance exists, that one is used.
/// </summary>
public static class VastGpuCli
{
    private const string ApiBase = "https://console.vast.ai/api/v1";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string action = "status"; string? instanceId = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--gpu":      if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) action = args[++i].ToLowerInvariant(); break;
                case "--instance": if (i + 1 < args.Length) instanceId = args[++i]; break;
            }
        }
        if (action is not ("status" or "stop" or "start" or "destroy"))
        {
            Console.Error.WriteLine($"[gpu] Unknown action '{action}'. Use: status | stop | start | destroy.");
            return 1;
        }

        var settings = services.GetRequiredService<SettingsService>();
        var key = settings.VastApiKey;
        if (string.IsNullOrWhiteSpace(key))
        {
            Console.Error.WriteLine("[gpu] No vast.ai API key in the vault (provider id 'vast'). "
                + "Add %APPDATA%/MindAttic/LLM/vast.json with an apiKey, or set VAST_API_KEY.");
            return 1;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        try
        {
            // Always fetch the instance list first — drives status, and resolves a lone instance.
            var listJson = await http.GetStringAsync($"{ApiBase}/instances/");
            using var doc = JsonDocument.Parse(listJson);
            var instances = doc.RootElement.TryGetProperty("instances", out var arr) && arr.ValueKind == JsonValueKind.Array
                ? arr.EnumerateArray().ToList()
                : new List<JsonElement>();

            if (action == "status")
            {
                if (instances.Count == 0) { Console.WriteLine("[gpu] No instances."); return 0; }
                Console.WriteLine($"[gpu] {instances.Count} instance(s):");
                foreach (var ins in instances)
                    Console.WriteLine($"   id {Get(ins, "id")} · {Get(ins, "actual_status")} · ip {Get(ins, "public_ipaddr")} · {Get(ins, "gpu_name")} · ${Get(ins, "dph_total")}/hr");
                return 0;
            }

            // Resolve the target instance for a control action.
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                if (instances.Count == 1) instanceId = Get(instances[0], "id");
                else { Console.Error.WriteLine($"[gpu] {instances.Count} instances — pass --instance <id> for '{action}'."); return 1; }
            }

            HttpResponseMessage res;
            if (action == "destroy")
            {
                res = await http.DeleteAsync($"{ApiBase}/instances/{instanceId}/");
            }
            else // stop | start
            {
                var state = action == "stop" ? "stopped" : "running";
                using var req = new HttpRequestMessage(HttpMethod.Put, $"{ApiBase}/instances/{instanceId}/")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { state }), Encoding.UTF8, "application/json"),
                };
                res = await http.SendAsync(req);
            }

            var body = await res.Content.ReadAsStringAsync();
            Console.WriteLine($"[gpu] {action} instance {instanceId} → HTTP {(int)res.StatusCode}. {Trunc(body, 300)}");
            return res.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[gpu] vast API call failed: {ex.Message}");
            return 1;
        }
    }

    private static string Get(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : v.ToString()) : "";

    private static string Trunc(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s[..n] + "…");
}
