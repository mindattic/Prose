using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --print-voice</c> — print the exact voice context the generator and
/// re-beater receive: <see cref="DatabaseService.GetLiteraryRulesPrompt"/> (leads
/// with the beat doctrine + prohibitions + paragraph requirements) and
/// <see cref="DatabaseService.GetToneBiblePrompt"/> (tone + dialogue rules), plus
/// Kyle's <c>NarrationVoice</c>. The verification that the canon-trained voice is
/// actually wired into prompts, not just stored.
/// </summary>
public static class PrintVoiceCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var db = services.GetRequiredService<DatabaseService>();

        Console.WriteLine("══════════ LITERARY RULES (GetLiteraryRulesPrompt) ══════════");
        Console.WriteLine(db.GetLiteraryRulesPrompt());
        Console.WriteLine();
        Console.WriteLine("══════════ TONE BIBLE (GetToneBiblePrompt) ══════════");
        Console.WriteLine(db.GetToneBiblePrompt());

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var ctx = await dbFactory.CreateDbContextAsync();
        var kyleNv = await ctx.Characters.AsNoTracking()
            .Join(ctx.Entities.AsNoTracking().Where(e => e.Name == "Kyle Ellen Corbin"),
                  c => c.Id, e => e.Id, (c, e) => c.NarrationVoice)
            .FirstOrDefaultAsync();
        Console.WriteLine();
        Console.WriteLine("══════════ KYLE NarrationVoice ══════════");
        Console.WriteLine(string.IsNullOrWhiteSpace(kyleNv) ? "(empty)" : kyleNv);
        return 0;
    }
}
