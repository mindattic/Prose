using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;
using Prose.Hub;

namespace Prose.UnitTests;

/// <summary>
/// PRS-T920: <c>prose --universe-import &lt;file&gt; --universe &lt;new-slug&gt;</c> answered unknown_universe because the Hub
/// resolved the slug before the import could create it. The import creates its universe, as the HTTP import does;
/// every other command still needs an existing universe.
/// </summary>
[TestFixture]
public class CliDispatchUniverseTests
{
    private static readonly Guid GlmzId = Guid.NewGuid();
    private static readonly UniverseInfo[] Universes = [new(GlmzId, "glmz", "GLMZ", null, null, true, 100)];

    [Test]
    public void The_universe_import_runs_unscoped_for_a_universe_it_will_create()
    {
        Assert.That(CliDispatch.TryResolveUniverse("spacegod-new", "UniverseInterchangeCli", "RunImportAsync", Universes, out var id), Is.True);
        Assert.That(id, Is.Null);
    }

    [Test]
    public void The_universe_import_is_scoped_to_an_existing_universe()
    {
        Assert.That(CliDispatch.TryResolveUniverse("GLMZ", "UniverseInterchangeCli", "RunImportAsync", Universes, out var id), Is.True);
        Assert.That(id, Is.EqualTo(GlmzId));
    }

    [Test]
    public async Task The_unscoped_import_creates_the_universe_its_universe_flag_names()
    {
        var root = Path.Combine(Path.GetTempPath(), "ss-cli-import-new-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var paths = new TestPathProviderWithRoot(root);
        try
        {
            var dbFactory = Prose.Core.Data.TestDbFactory.For(paths, "cli-import-new");
            var services = new ServiceCollection()
                .AddSingleton(new UniverseInterchangeService(dbFactory, new OutboxService(dbFactory)))
                .BuildServiceProvider();
            var file = Path.Combine(root, "game.universe.json");
            await File.WriteAllTextAsync(file, """
                {"universe": {"id": "game", "name": "Game"},
                 "entities": [{"id": "a", "type": "character", "name": "A", "summary": "s", "relations": [], "tags": []}]}
                """);

            var exit = await Prose.Cli.UniverseInterchangeCli.RunImportAsync([file, "--universe", "brand-new"], services);

            Assert.That(exit, Is.EqualTo(0));
            await using var db = await dbFactory.CreateDbContextAsync();
            Assert.That(db.Universes.Any(u => u.Slug == "brand-new"), Is.True);
            Assert.That(db.Universes.Any(u => u.Slug == "game"), Is.False, "--universe wins over the file's id");
        }
        finally
        {
            Prose.Core.Data.TestDbFactory.Reset(paths);
            try { Directory.Delete(root, recursive: true); } catch { /* best-effort cleanup */ }
        }
    }

    [TestCase("UniverseInterchangeCli", "RunExportAsync")]
    [TestCase("UniverseInterchangeCli", "RunSyncAsync")]
    [TestCase("ReadBeatsCli", null)]
    public void Any_other_command_still_refuses_an_unknown_universe(string handler, string? method)
    {
        Assert.That(CliDispatch.TryResolveUniverse("spacegod-new", handler, method, Universes, out var id), Is.False);
        Assert.That(id, Is.Null);
    }
}
