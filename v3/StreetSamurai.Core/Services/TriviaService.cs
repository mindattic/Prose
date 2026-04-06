using System.Text.Json;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Pre-generates 100 "Did You Know?" facts from canon data once per day.
/// Facts are cached to engine/trivia.json and only regenerated when the date changes.
/// </summary>
public class TriviaService(
    IPathProvider paths,
    CharacterRepository charRepo,
    CorponationRepository corpRepo,
    DistrictRepository districtRepo,
    WeaponryRepository weaponRepo,
    CyberwareRepository cyberRepo,
    VocabularyRepository vocabRepo,
    SyntheticLifeRepository synthRepo,
    TechnologyRepository techRepo,
    FactionRepository factionRepo,
    EquipmentRepository equipRepo,
    GenewareRepository geneRepo,
    TransportationRepository transportRepo)
{
    private const int TriviaSlots = 100;
    private string[] facts = [];
    private string cachedDate = "";

    private string FilePath => Path.Combine(paths.EngineDataDir, "trivia.json");

    /// <summary>Returns today's 100 pre-generated facts, regenerating if needed.</summary>
    public string[] GetFacts()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        if (facts.Length > 0 && cachedDate == today)
            return facts;

        // Try loading from disk
        if (File.Exists(FilePath))
        {
            try
            {
                var wrapper = JsonSerializer.Deserialize<TriviaFile>(File.ReadAllText(FilePath));
                if (wrapper?.Date == today && wrapper.Facts is { Length: > 0 })
                {
                    facts = wrapper.Facts;
                    cachedDate = today;
                    return facts;
                }
            }
            catch { /* regenerate on parse failure */ }
        }

        // Generate fresh
        facts = BuildAndShuffleTriviaPool();
        cachedDate = today;

        // Persist
        var json = JsonSerializer.Serialize(new TriviaFile { Date = today, Facts = facts },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);

        return facts;
    }

    private string[] BuildAndShuffleTriviaPool()
    {
        var pool = new HashSet<string>();

        foreach (var c in charRepo.GetAll().OrderBy(_ => Random.Shared.Next()))
        {
            if (pool.Count >= 500) break;
            var desc = FirstSentence(c.Description);
            if (desc.Length > 20)
                pool.Add($"{c.Name} — {desc}");
            if (!string.IsNullOrEmpty(c.Role))
                pool.Add($"{c.Name} is a {c.Role}{(!string.IsNullOrEmpty(c.Location) ? $" based in {c.Location}" : "")}.");
            if (c.Psychology.Secret.Length > 20)
                pool.Add($"Secret: {FirstSentence(c.Psychology.Secret)}");
        }

        foreach (var c in corpRepo.GetAll().OrderBy(_ => Random.Shared.Next()))
        {
            if (pool.Count >= 500) break;
            if (!string.IsNullOrEmpty(c.Sector))
                pool.Add($"{c.Name} operates in {c.Sector}.");
            if (!string.IsNullOrEmpty(c.KeyDetail))
                pool.Add(FirstSentence(c.KeyDetail));
            if (!string.IsNullOrEmpty(c.Valuation))
                pool.Add($"{c.Name} has a valuation of {c.Valuation}.");
        }

        foreach (var d in districtRepo.GetAll().OrderBy(_ => Random.Shared.Next()))
        {
            if (pool.Count >= 500) break;
            var desc = FirstSentence(d.Description);
            if (desc.Length > 20)
                pool.Add($"{d.Name} — {desc}");
        }

        foreach (var e in synthRepo.GetAll().OrderBy(_ => Random.Shared.Next()))
        {
            if (pool.Count >= 500) break;
            pool.Add($"An E.L.F. called '{e.Name}' ({e.Disposition}) inhabits {e.Location}. {FirstSentence(e.ObservedBehavior)}");
        }

        foreach (var v in vocabRepo.GetAll().OrderBy(_ => Random.Shared.Next()))
        {
            if (pool.Count >= 500) break;
            pool.Add($"'{v.Term}' — {FirstSentence(v.Definition)}");
        }

        foreach (var w in weaponRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(30))
        {
            var desc = FirstSentence(w.Description);
            if (desc.Length > 20) pool.Add($"The {w.Name} — {desc}");
        }

        foreach (var t in techRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(20))
        {
            var desc = FirstSentence(t.Description);
            if (desc.Length > 20) pool.Add($"{t.Name} — {desc}");
        }

        foreach (var f in factionRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(20))
        {
            var desc = FirstSentence(f.Description);
            if (desc.Length > 20) pool.Add($"The {f.Name} — {desc}");
        }

        foreach (var c in cyberRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(20))
        {
            var desc = FirstSentence(c.Description);
            if (desc.Length > 20) pool.Add($"{c.Name} — {desc}");
        }

        foreach (var e in equipRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(15))
        {
            var desc = FirstSentence(e.Description);
            if (desc.Length > 20) pool.Add($"{e.Name} — {desc}");
        }

        foreach (var g in geneRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(20))
        {
            var desc = FirstSentence(g.Description);
            if (desc.Length > 20) pool.Add($"Geneware: {g.Name} — {desc}");
        }

        foreach (var t in transportRepo.GetAll().OrderBy(_ => Random.Shared.Next()).Take(15))
        {
            var desc = FirstSentence(t.Description);
            if (desc.Length > 20) pool.Add($"{t.Name} — {desc}");
        }

        var arr = pool.ToArray();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr[..Math.Min(TriviaSlots, arr.Length)];
    }

    private static string FirstSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var clean = text.Replace("\\n", " ").Replace("\n", " ").Trim();
        var end = clean.IndexOfAny(['.', '!', '?']);
        if (end > 0 && end < 250) return clean[..(end + 1)];
        return clean.Length > 200 ? clean[..200] + "..." : clean;
    }

    private class TriviaFile
    {
        public string Date { get; set; } = "";
        public string[] Facts { get; set; } = [];
    }
}
