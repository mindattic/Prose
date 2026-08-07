using System.Text.Json;

namespace Prose.Cli;

/// <summary>
/// Identity resolution for JSON seed imports (<c>ss --add-character</c>, <c>--add-place</c>, …).
///
/// The problem this solves: every canon model (<c>CharacterData</c>, <c>DistrictData</c>, …)
/// initializes its <c>Id</c> property inline —
/// <c>public string Id { get; set; } = Guid.CreateVersion7().ToString("N");</c>
/// — so deserializing a seed file that omits <c>"id"</c> yields an object carrying a brand-new,
/// never-before-seen id. The repositories upsert by id (see the EfRepository upsert-by-Id-only
/// behaviour), so that object INSERTS. Re-importing the same file therefore produces a second
/// entity with the same name rather than updating the first, and nothing warns you: the import
/// prints "saved" both times.
///
/// This bit for real. Re-seeding one corrected character file created a duplicate "Anne Devlin",
/// which only surfaced later when WorldValidationTests.NoSameTypeNameCollisions failed. Seed files
/// in this repo routinely omit "id" (they are hand-authored), so the trap is the default path, not
/// an edge case.
///
/// The fix: an id is "explicit" only if the RAW JSON actually carried one. When it did not, callers
/// resolve the existing row by name-slug and adopt its id, turning a re-import into an update.
/// </summary>
public static class SeedIdentity
{
    /// <summary>
    /// True when the raw seed JSON carried a usable <c>"id"</c>. Deliberately inspects the JSON
    /// text rather than the deserialized object, because the object always has an id by then.
    /// </summary>
    public static bool HasExplicitId(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return false;
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            // Match the deserializer's case-insensitive property matching.
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!string.Equals(prop.Name, "id", StringComparison.OrdinalIgnoreCase)) continue;
                return prop.Value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(prop.Value.GetString());
            }
            return false;
        }
        catch (JsonException)
        {
            // Unparseable JSON is the caller's problem to report; treat as "no explicit id".
            return false;
        }
    }

    /// <summary>
    /// Decide the id a seed import should save under.
    ///
    /// Returns the existing entity's id when the file omitted <c>"id"</c> and a row with the same
    /// name-slug already exists (→ update in place); otherwise returns <paramref name="currentId"/>
    /// unchanged (→ honour an explicit id, or insert genuinely new content).
    /// <paramref name="wasExisting"/> reports which happened so the CLI can print the truth.
    /// </summary>
    public static string ResolveId(
        string rawJson,
        string currentId,
        string name,
        Func<string, string?> findIdBySlug,
        Func<string, string> toSlug,
        out bool wasExisting)
    {
        wasExisting = false;
        if (HasExplicitId(rawJson)) return currentId;
        if (string.IsNullOrWhiteSpace(name)) return currentId;

        var existingId = findIdBySlug(toSlug(name));
        if (string.IsNullOrWhiteSpace(existingId)) return currentId;

        wasExisting = true;
        return existingId;
    }
}
