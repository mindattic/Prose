using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Extensions;
using StreetSamurai.Core.Services;

const string ProjectId = "5a0959eb5619bf91f59ffb8632c80259";

Console.WriteLine($"=== RunRepair: {ProjectId} ===");
Console.WriteLine($"CWD: {Environment.CurrentDirectory}");

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "HH:mm:ss ";
        o.IncludeScopes = false;
    });
});
services.AddStreetSamuraiServices();

var sp = services.BuildServiceProvider();

var settings = sp.GetRequiredService<SettingsService>();
Console.WriteLine($"Active provider: {settings.ActiveLlmProvider}");
Console.WriteLine($"Canon root: {settings.CanonRootPath}");
Console.WriteLine($"API key present: {!string.IsNullOrWhiteSpace(settings.ApiKey)}");

var director = sp.GetRequiredService<StoryDirectorService>();

var checkpoint = director.LoadCheckpoint(ProjectId);
if (checkpoint == null)
{
    Console.Error.WriteLine($"FAIL: no checkpoint found for {ProjectId}");
    return 1;
}

Console.WriteLine($"Loaded checkpoint: title={checkpoint.Title}, beats={checkpoint.Beats.Count}, complete={checkpoint.Complete}");
Console.WriteLine($"Outline acts: {checkpoint.Outline?.Acts.Count ?? 0}");
Console.WriteLine($"Outline beats: {checkpoint.Outline?.Acts.Sum(a => a.Beats.Count) ?? 0}");
Console.WriteLine($"Damage state: {StoryDamage.Describe(checkpoint)}");
Console.WriteLine();

director.OnProgress += p =>
{
    var bar = $"[{p.CurrentBeat}/{p.TotalBeats}]";
    Console.WriteLine($"{bar} {p.Message}");
};

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Console.Error.WriteLine("Ctrl-C — cancelling…");
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine("Calling RepairStoryAsync…");
var sw = System.Diagnostics.Stopwatch.StartNew();
var result = await director.RepairStoryAsync(checkpoint, cts.Token);
sw.Stop();

Console.WriteLine();
Console.WriteLine($"=== Done in {sw.Elapsed:mm\\:ss} ===");
Console.WriteLine($"Complete: {result.Complete}");
Console.WriteLine($"FailureReason: {result.FailureReason ?? "(none)"}");
Console.WriteLine($"Beats written: {result.Beats.Count}");
Console.WriteLine($"FullText length: {result.FullText?.Length ?? 0} chars");

return result.Complete ? 0 : 2;
