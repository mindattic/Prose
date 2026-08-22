using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --create-repository --name "Artifacts" [--category World] [--icon bi-box]
/// [--description "..."]</c> — create a runtime-defined repository (custom entity type).
/// </summary>
public static class CreateRepositoryCli
{
    public static int Run(string[] args, IServiceProvider services)
    {
        string ArgVal(string flag) { var i = Array.IndexOf(args, flag); return i >= 0 && i + 1 < args.Length ? args[i + 1] : ""; }
        var repoName = ArgVal("--name");
        if (string.IsNullOrWhiteSpace(repoName)) { Console.Error.WriteLine("[create-repository] --name is required."); return 1; }

        var svc = services.GetRequiredService<RepositoryDefinitionService>();
        try
        {
            var def = svc.Create(repoName, ArgVal("--category"), ArgVal("--icon"), ArgVal("--description"));
            Console.WriteLine($"[create-repository] Created '{def.Name}' — slug '{def.Slug}', category {def.Category}, route {def.RoutePath}.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[create-repository] FAILED: {ex.Message}");
            return 1;
        }
    }
}
