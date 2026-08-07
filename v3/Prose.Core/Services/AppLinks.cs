namespace Prose.Core.Services;

/// <summary>
/// Cross-app URL resolver for the Writer/Codex split. Each host configures the
/// OTHER app's base URL under AppLinks:WriterBaseUrl / AppLinks:CodexBaseUrl;
/// its own stays empty so links to its own pages remain relative. When a base
/// URL is empty the target path is returned unchanged, which means a single
/// combined host (CLI tooling, tests, the retired monolith) keeps every link
/// local without any configuration.
/// </summary>
public class AppLinks
{
    /// <summary>Base URL of the Writer host (e.g. https://localhost:7200). Empty = links stay relative.</summary>
    public string WriterBaseUrl { get; init; } = "";

    /// <summary>Base URL of the Codex host (e.g. https://localhost:7201). Empty = links stay relative.</summary>
    public string CodexBaseUrl { get; init; } = "";

    /// <summary>Resolve a path that lives in the Writer app (workbench, beat editor, findings, listen).</summary>
    public string Writer(string path) => Combine(WriterBaseUrl, path);

    /// <summary>Resolve a path that lives in the Codex app (entity browser, world graph, coverage).</summary>
    public string Codex(string path) => Combine(CodexBaseUrl, path);

    /// <summary>True when Writer pages live in a different process than this host.</summary>
    public bool WriterIsRemote => !string.IsNullOrWhiteSpace(WriterBaseUrl);

    /// <summary>True when Codex pages live in a different process than this host.</summary>
    public bool CodexIsRemote => !string.IsNullOrWhiteSpace(CodexBaseUrl);

    private static string Combine(string baseUrl, string path)
        => string.IsNullOrWhiteSpace(baseUrl)
            ? path
            : $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
}
