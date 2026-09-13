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
        return services;
    }
}
