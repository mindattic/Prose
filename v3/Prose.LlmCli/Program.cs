using System.Text.Json;
using MindAttic.Legion;

namespace Prose.LlmCli;

/// <summary>
/// prose-llm — a generic, Prose-independent CLI front-end over MindAttic.Legion's
/// LegionClient. One binary, one calling convention, for every provider Legion knows how
/// to speak (Claude, OpenAI, Gemini, DeepSeek, Mistral, Kimi, Perplexity, Cohere, Grok,
/// Groq, Together, OpenRouter, Fireworks) — the "generic CLI that works for all LLMs"
/// piece of the Multi-LLM Master Switch-Over plan. Serves two purposes: (1) the
/// last-resort tier in Prose.Core's LlmRouter fallback chain for providers with no
/// native CLI of their own, and (2) a manual escape hatch usable straight from a
/// terminal if Prose.Core itself won't build or run.
///
/// Usage:
///   prose-llm --provider &lt;id&gt; [--model &lt;id&gt;] --prompt &lt;text|@file|-&gt; [--system &lt;text|@file&gt;]
///             [--temperature &lt;0.0-2.0&gt;] [--max-tokens &lt;n&gt;] [--json]
///
/// Credentials resolve through the same shared MindAttic credential store Prose.Core
/// uses (%APPDATA%/MindAttic/LLM/) — a key already configured for Prose just works here,
/// no extra setup. claude-team additionally rides the Claude Code CLI's own OAuth
/// session automatically (LegionClient.ResolveKey handles this internally).
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        string? provider = null, model = null, promptArg = null, systemArg = null;
        var temperature = 0.7;
        var maxTokens = 2048;
        var asJson = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--provider": provider = ArgAt(args, ++i, "--provider"); break;
                case "--model": model = ArgAt(args, ++i, "--model"); break;
                case "--prompt": promptArg = ArgAt(args, ++i, "--prompt"); break;
                case "--system": systemArg = ArgAt(args, ++i, "--system"); break;
                case "--temperature": temperature = double.Parse(ArgAt(args, ++i, "--temperature")); break;
                case "--max-tokens": maxTokens = int.Parse(ArgAt(args, ++i, "--max-tokens")); break;
                case "--json": asJson = true; break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    PrintUsage();
                    return 1;
            }
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            Console.Error.WriteLine("--provider is required.");
            PrintUsage();
            return 1;
        }
        if (string.IsNullOrWhiteSpace(promptArg))
        {
            Console.Error.WriteLine("--prompt is required.");
            PrintUsage();
            return 1;
        }

        string userMessage, systemPrompt;
        try
        {
            userMessage = await ResolveTextAsync(promptArg);
            systemPrompt = systemArg is null ? "" : await ResolveTextAsync(systemArg);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to read --prompt/--system: {ex.Message}");
            return 1;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var legion = new LegionClient(http);

        try
        {
            var response = await legion.CallAsync(
                providerId: provider,
                systemPrompt: systemPrompt,
                userMessage: userMessage,
                maxTokens: maxTokens,
                temperature: temperature,
                modelOverride: model);

            if (asJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    provider,
                    model,
                    response,
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine(response);
            }
            return 0;
        }
        catch (Exception ex)
        {
            if (asJson)
                Console.WriteLine(JsonSerializer.Serialize(new { provider, model, error = ex.Message }));
            else
                Console.Error.WriteLine($"[{provider}] error: {ex.Message}");
            return 1;
        }
    }

    private static string ArgAt(string[] args, int i, string flag)
    {
        if (i >= args.Length)
            throw new ArgumentException($"{flag} requires a value.");
        return args[i];
    }

    /// <summary>
    /// "-" reads the full prompt from stdin (avoids command-line length limits for long
    /// prose-generation prompts). "@path" reads from a file. Anything else is the literal
    /// prompt text.
    /// </summary>
    private static async Task<string> ResolveTextAsync(string arg)
    {
        if (arg == "-")
            return await Console.In.ReadToEndAsync();
        if (arg.StartsWith('@'))
            return await File.ReadAllTextAsync(arg[1..]);
        return arg;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("""
            prose-llm — generic multi-provider LLM CLI (MindAttic.Legion front-end)

            Usage:
              prose-llm --provider <id> --prompt <text|@file|-> [options]

            Options:
              --provider <id>       claude-api | claude-team | openai | gemini | deepseek |
                                    mistral | kimi | perplexity | cohere | xai | groq |
                                    together | openrouter | fireworks
              --prompt <value>      literal text, "@path/to/file", or "-" for stdin
              --system <value>      same forms as --prompt; optional system prompt
              --model <id>          override the provider's default model
              --temperature <n>     default 0.7
              --max-tokens <n>      default 2048
              --json                emit a JSON envelope instead of plain text
              --help, -h            show this message

            Credentials resolve from the same shared store Prose.Core uses
            (%APPDATA%/MindAttic/LLM/) — no extra setup if Prose is already configured.

            Examples:
              prose-llm --provider gemini --prompt "Say ok"
              prose-llm --provider openai --system "You are terse." --prompt @beat-goal.txt --json
              cat prompt.txt | prose-llm --provider deepseek --prompt -
            """);
    }
}
