using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services.Spelling;

namespace Prose.Mcp;

// ── The author's spelling dictionary ─────────────────────────────────────────
// The one place new words live (the SpellingWords table). The Writer's squiggles, its
// "Add to dictionary" and its Settings read and write the same rows.
//   list_dictionary_words / add_dictionary_word / remove_dictionary_word
// CLI twin: prose --dictionary list|add|remove.

[McpServerToolType]
public class SpellingTools(SpellingService spelling, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    [McpServerTool, Description("List the author's spelling dictionary: every word the Writer's spellcheck accepts beyond English and the universe's entity names. One entry covers its plural and possessive forms.")]
    public Task<string> list_dictionary_words() =>
        hub.InvokeAsync(nameof(SpellingTools), nameof(ListDictionaryWordsImpl), new { });

    public async Task<string> ListDictionaryWordsImpl()
    {
        var rows = await spelling.ListAsync();
        return JsonSerializer.Serialize(new { count = rows.Count, words = rows.Select(r => new { r.Word, r.AddedBy, r.AddedAt }) }, JsonOpts);
    }

    [McpServerTool, Description("Add a word to the author's spelling dictionary. One entry covers its inflections: adding 'CorpoNation' also accepts CorpoNations, CorpoNation's and CorpoNations'. A word with capitals must be written with them; an all-lowercase word matches any capitalisation. A possessive is stored as its bare word. Returns the stored row.")]
    public Task<string> add_dictionary_word(
        [Description("One word (letters and apostrophes; a hyphenated word is two words).")] string word,
        [Description("Who added it: author (default), or session:<id>.")] string addedBy = "author") =>
        hub.InvokeAsync(nameof(SpellingTools), nameof(AddDictionaryWordImpl), new { word, addedBy });

    public async Task<string> AddDictionaryWordImpl(string word, string addedBy = "author")
    {
        try
        {
            var r = await spelling.AddAsync(word, addedBy);
            return JsonSerializer.Serialize(new { ok = true, added = r.Added, r.Row.Word, r.Row.AddedBy, r.Row.AddedAt, note = r.Note }, JsonOpts);
        }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("Remove a word from the author's spelling dictionary (matched without regard to case).")]
    public Task<string> remove_dictionary_word([Description("The word.")] string word) =>
        hub.InvokeAsync(nameof(SpellingTools), nameof(RemoveDictionaryWordImpl), new { word });

    public async Task<string> RemoveDictionaryWordImpl(string word) =>
        JsonSerializer.Serialize(new { ok = true, removed = await spelling.RemoveAsync(word), word }, JsonOpts);
}
