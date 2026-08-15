using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --provider-status [--live] [--json]
///
/// RFC 0011 Brick 3, item 3 — a single, visible "degraded services" status the CLI can report on
/// demand. See docs/PROVIDERS.md for which service depends on which provider.
///
/// Without <c>--live</c>: reports whether each provider has credentials configured at all — free,
/// no API call, but this is exactly the check that would have said "configured" throughout this
/// session's entire Anthropic credit-exhaustion window, since a present API key and a USABLE one
/// are different questions (<see cref="ILlmService.IsConfiguredAsync"/> only ever answers the
/// first). Pass <c>--live</c> for the real answer: a minimal, real call to each provider,
/// distinguishing "not configured," "configured but failing," and "configured and reachable."
/// This costs a trivial amount of real quota/money per provider — deliberately opt-in, not run on
/// every casual invocation of this command.
/// </summary>
public static class ProviderStatusCli
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed record ProviderCheck(string Provider, string Status, string? Detail);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var isJson = args.Contains("--json");
        var isLive = args.Contains("--live");

        var llm = services.GetRequiredService<ILlmService>();
        var router = services.GetService<LlmRouter>();
        var embeddings = services.GetService<EmbeddingService>();

        var results = new List<ProviderCheck>();

        var llmConfigured = await llm.IsConfiguredAsync();
        if (!llmConfigured)
        {
            results.Add(new ProviderCheck("Anthropic (ILlmService)", "not configured", "no active provider key on file"));
        }
        else if (!isLive)
        {
            results.Add(new ProviderCheck("Anthropic (ILlmService)", "configured", "key present — pass --live to verify it's actually usable, not just present"));
        }
        else
        {
            try
            {
                await llm.GenerateAsync("Reply with one word.", "Say: ok", temperature: 0, maxTokens: 5);
                results.Add(new ProviderCheck("Anthropic (ILlmService)", "reachable", null));
            }
            catch (Exception ex)
            {
                results.Add(new ProviderCheck("Anthropic (ILlmService)", "configured but failing", ex.Message));
            }
        }

        if (embeddings == null)
        {
            results.Add(new ProviderCheck("OpenAI (EmbeddingService)", "not registered", "EmbeddingService not wired into this build"));
        }
        else if (!isLive)
        {
            results.Add(new ProviderCheck("OpenAI (EmbeddingService)", "unknown", "pass --live to verify reachability"));
        }
        else
        {
            try
            {
                await embeddings.ComputeSimilarityAsync("ok", "ok");
                results.Add(new ProviderCheck("OpenAI (EmbeddingService)", "reachable", null));
            }
            catch (Exception ex)
            {
                results.Add(new ProviderCheck("OpenAI (EmbeddingService)", "configured but failing", ex.Message));
            }
        }

        // Full fallback-chain roster (RFC: Multi-LLM master switch-over). Config-only by
        // default — GetProvidersAsync() only checks credential presence per provider; pass
        // --live to also fire one real generation through the whole chain (primary + every
        // configured fallback), most expensive check in this command, so it's opt-in only.
        if (router != null)
        {
            foreach (var p in await router.GetProvidersAsync())
            {
                var label = $"{p.Name} [{p.Id}]" + (p.IsActive ? " (primary)" : "");
                if (!p.IsConfigured)
                {
                    results.Add(new ProviderCheck(label, "not configured", null));
                }
                else if (!isLive)
                {
                    results.Add(new ProviderCheck(label, "configured", null));
                }
                else
                {
                    try
                    {
                        // GenerateViaAsync (not GenerateAsync) — calls exactly this provider,
                        // no fallback, so a failing provider can't be masked by the chain
                        // quietly succeeding through a different one.
                        await router.GenerateViaAsync(p.Id, "Reply with one word.", "Say: ok", temperature: 0, maxTokens: 5);
                        results.Add(new ProviderCheck(label, "reachable", null));
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ProviderCheck(label, "configured but failing", ex.Message));
                    }
                }
            }
        }

        var degraded = results.Any(r => r.Status is "not configured" or "configured but failing" or "not registered");

        if (isJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { live = isLive, results }, JsonOpts));
            return degraded ? 1 : 0;
        }

        Console.WriteLine($"[provider-status]{(isLive ? " (live)" : " (config-only — pass --live for a real check)")}");
        foreach (var r in results)
        {
            Console.WriteLine($"  {r.Provider,-28} {r.Status}");
            if (r.Detail != null) Console.WriteLine($"    {r.Detail}");
        }
        Console.WriteLine();
        Console.WriteLine("See docs/PROVIDERS.md for which service depends on which provider.");
        return degraded ? 1 : 0;
    }
}
