namespace Prose.Hub.Components;

/// <summary>
/// The cache-busting stamp appended to every static asset a Hub-served page links.
///
/// <para>This is not cosmetic. The WebView2 profile deliberately survives a redeploy so the
/// author's session does, which means it happily keeps serving the JS and CSS it cached the first
/// time the window opened. The editor is wired through JS interop, so a stale <c>writer.js</c>
/// means <c>proseEditor is undefined</c> — that throws, takes the Blazor circuit down with it, and
/// the whole page stops responding. Diagnosed 2026-09-12 after the window froze twice.</para>
///
/// <para>Shared rather than copied into each root document: two pages that drift on this would
/// reintroduce the same freeze on whichever one was forgotten.</para>
/// </summary>
public static class HubAssets
{
    /// <summary>Changes on every deploy — the executable's own timestamp, which the publish
    /// rewrites. Cheap enough to compute once per process and more reliable than an assembly
    /// version nobody remembers to bump.</summary>
    public static readonly string Version = Compute();

    private static string Compute()
    {
        try
        {
            var entry = System.Reflection.Assembly.GetEntryAssembly()?.Location;
            var path = !string.IsNullOrEmpty(entry) && File.Exists(entry)
                ? entry
                : Path.Combine(AppContext.BaseDirectory, "Hub.exe");
            return File.GetLastWriteTimeUtc(path).Ticks.ToString();
        }
        catch (IOException)
        {
            // A version that changes every start is still better than one that never changes.
            return DateTime.UtcNow.Ticks.ToString();
        }
    }
}
