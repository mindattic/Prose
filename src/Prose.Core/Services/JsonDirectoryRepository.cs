using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// Legacy class — file-based directory repository has been retired in favor of
/// <see cref="Prose.Core.Data.EfRepository{T}"/>. The slug helpers below
/// remain because the EF repo and the rest of the codebase reference them; if
/// you're looking for "the repository," it now lives in the SQL Server
/// Prose database via EfRepository.
/// </summary>
public static partial class JsonDirectoryRepository<T> where T : class
{
    public static string Slugify(string name) =>
        SlugRegex().Replace(StripDiacritics(name.ToLowerInvariant().Trim()), "_").Trim('_');

    public static string ToSlug(string name) =>
        SlugRegex().Replace(StripDiacritics(name.ToLowerInvariant().Trim()), "-").Trim('-');

    private static readonly Dictionary<char, string> DiacriticMap = new()
    {
        ['ø'] = "o", ['Ø'] = "o", ['ð'] = "d", ['Ð'] = "d", ['þ'] = "th", ['Þ'] = "th",
        ['æ'] = "ae", ['Æ'] = "ae", ['œ'] = "oe", ['Œ'] = "oe", ['ß'] = "ss",
        ['ł'] = "l", ['Ł'] = "l", ['ı'] = "i", ['ĸ'] = "k", ['ŉ'] = "n",
    };

    private static string StripDiacritics(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length + 4);
        foreach (var c in text)
        {
            if (DiacriticMap.TryGetValue(c, out var mapped)) { sb.Append(mapped); continue; }
            sb.Append(c);
        }
        var normalized = sb.ToString().Normalize(System.Text.NormalizationForm.FormD);
        sb.Clear();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
