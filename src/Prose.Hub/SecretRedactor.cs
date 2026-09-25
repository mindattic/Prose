using System.Text.Json;
using System.Text.Json.Nodes;

namespace Prose.Hub;

/// <summary>
/// Strips secret values from a command's arguments before they reach the Hub console, the
/// Serilog files or the Command Ledger. The ledger is a permanent table that any session can
/// read back with <c>command_log</c>; <c>add_operator_byo_key</c> and <c>--reset-password</c>
/// used to land there with the raw key/password in ArgsJson.
/// </summary>
public static class SecretRedactor
{
    private const string Mask = "***";

    private static readonly HashSet<string> SecretNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "key", "keys", "apikey", "api_key", "apiKey", "secret", "password", "token", "accesstoken",
    };

    private static readonly HashSet<string> SecretFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--key", "--keys", "--api-key", "--secret", "--password", "--token",
    };

    /// <summary>CLI argv: the value after a secret flag (or in <c>--flag=value</c>) becomes ***.</summary>
    public static string[] RedactArgs(string[] args)
    {
        var copy = (string[])args.Clone();
        for (int i = 0; i < copy.Length; i++)
        {
            var eq = copy[i].IndexOf('=');
            if (eq > 0 && SecretFlags.Contains(copy[i][..eq])) { copy[i] = copy[i][..(eq + 1)] + Mask; continue; }
            if (SecretFlags.Contains(copy[i]) && i + 1 < copy.Length) copy[++i] = Mask;
        }
        return copy;
    }

    /// <summary>MCP JSON args: any property named like a secret has its value(s) replaced.</summary>
    public static string RedactJson(JsonElement? args)
    {
        if (args is not { } el) return "{}";
        var raw = el.GetRawText();
        try
        {
            var node = JsonNode.Parse(raw);
            if (node == null) return raw;
            return Walk(node) ? node.ToJsonString() : raw;
        }
        catch (JsonException) { return raw; }
    }

    private static bool Walk(JsonNode node)
    {
        var changed = false;
        if (node is JsonObject obj)
        {
            foreach (var (name, value) in obj.ToList())
            {
                if (value == null) continue;
                if (SecretNames.Contains(name))
                {
                    obj[name] = value is JsonArray arr
                        ? new JsonArray(arr.Select(_ => (JsonNode?)JsonValue.Create(Mask)).ToArray())
                        : JsonValue.Create(Mask);
                    changed = true;
                }
                else changed |= Walk(value);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr) if (item != null) changed |= Walk(item);
        }
        return changed;
    }
}
