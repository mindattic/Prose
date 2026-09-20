using Microsoft.Extensions.DependencyInjection;
using Prose.WriterUi.Services;

namespace Prose.WriterUi;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the editor. Mirrors <c>AddProseServices()</c> / <c>AddProseObserverUi()</c> —
    /// one call, and the host owns nothing else.
    ///
    /// <para>Takes no base URL, unlike <c>AddProseObserverUi</c>: this UI has no HTTP client
    /// because it does not leave the process. It is hosted only by Prose.Hub, and reaches the
    /// database through the same Core services every CLI command and MCP tool uses.</para>
    /// </summary>
    public static IServiceCollection AddProseWriterUi(this IServiceCollection services)
    {
        // Scoped: one per browser circuit, matching TabShellState's reasoning. WriterService holds
        // the flow-universe pin for the book the circuit has open, which must not be shared
        // between two windows editing two different books.
        services.AddScoped<WriterService>();
        // Same reasoning: the wiki pins the flow universe to whichever entity the page is showing,
        // so two windows browsing two universes must not share one instance.
        services.AddScoped<WikiService>();
        // And again: the spine view pins the flow universe to the book whose order of events it is
        // showing. Two windows reading two books must not share the pin.
        services.AddScoped<SpineService>();
        // Same reasoning once more: the Discuss panel reads and writes conversations scoped to the
        // book the circuit has open.
        services.AddScoped<DiscussionUiService>();
        // Credentials are machine-wide rather than per-book, but this stays Scoped so every
        // service this library exposes has one lifetime and no one has to remember which.
        services.AddScoped<SettingsUiService>();
        // Reading a passage aloud. Stateless — it synthesizes, hands the bytes to the browser,
        // and keeps nothing.
        services.AddScoped<ReadAloudUiService>();
        // Hearing a spoken turn. Scoped for the flow-universe pin it needs to read the book's own
        // proper nouns — the priming that makes invented names transcribe at all.
        services.AddScoped<SpeechUiService>();
        // Read Mode. Scoped for the flow-universe pin, like the rest — and deliberately a separate
        // service from WriterService, because the read must not be able to reach an editing verb.
        services.AddScoped<ReadService>();
        // The findings inbox and the obligation ledger. Both read data the rest of the system has
        // been writing for months with nothing in the editor reading it back.
        services.AddScoped<FindingsUiService>();
        services.AddScoped<LedgerUiService>();
        return services;
    }
}
