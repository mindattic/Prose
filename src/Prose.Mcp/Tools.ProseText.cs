using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// RFC 0017 conduit (docs/CONDUIT.md) — game texts, <c>prose-text/1</c>: conversations, crawls, descriptions,
/// quest text, books, barks and glossary terms authored against a universe's entities.
///
/// Agents may list, read and DRAFT. There is deliberately no approve, publish or retire tool: under RFC 0009
/// an agent drafts and only the author approves (CLI <c>prose --texts-approve … --by</c> or the HTTP call).
/// </summary>
[McpServerToolType]
public class ProseTextTools
{
    private readonly ProseTextService texts;
    private readonly HubInvoker hub;

    public ProseTextTools(ProseTextService texts, HubInvoker hub)
    {
        this.texts = texts;
        this.hub = hub;
    }

    [McpServerTool, Description("List a game universe's texts (prose-text/1, docs/CONDUIT.md) as whole documents. status: pullable (default: each text's live approved or published version), approved, published, draft (pending drafts), retired, all (newest version of every text). since: a version number, or an ISO date (approved/published after it). kind: conversation, crawl, description, quest, book, bark, glossary.")]
    public Task<string> ListTexts(
        [Description("Universe slug, e.g. 'gridgame' or 'spacegod'.")] string universe,
        [Description("Text kind filter.")] string? kind = null,
        [Description("pullable | approved | published | draft | retired | all.")] string? status = null,
        [Description("Version number or ISO date.")] string? since = null,
        [Description("Only the text with this game key.")] string? key = null) =>
        hub.InvokeAsync(nameof(ProseTextTools), nameof(ListTextsImpl), new { universe, kind, status, since, key }, universe);

    /// <summary>Runs inside the Hub via ToolDispatch reflection.</summary>
    public async Task<string> ListTextsImpl(string universe, string? kind = null, string? status = null, string? since = null, string? key = null)
    {
        try
        {
            var docs = await texts.ListAsync(universe, kind, status, since, key);
            return JsonSerializer.Serialize(new { count = docs.Count, texts = docs }, ProseTextService.JsonOptsPublic);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = "texts_list_failed", detail = ex.Message }, ProseTextService.JsonOptsPublic);
        }
    }

    [McpServerTool, Description("Get one text (prose-text/1) of a game universe: the given version, else its live version, else its newest. history=true returns every version.")]
    public Task<string> GetText(
        [Description("Universe slug.")] string universe,
        [Description("Text id, e.g. 'conversation-wanderer-ember'.")] string id,
        [Description("A specific version number.")] int? version = null,
        [Description("Return every version instead of one document.")] bool history = false) =>
        hub.InvokeAsync(nameof(ProseTextTools), nameof(GetTextImpl), new { universe, id, version, history }, universe);

    /// <summary>Runs inside the Hub via ToolDispatch reflection.</summary>
    public async Task<string> GetTextImpl(string universe, string id, int? version = null, bool history = false)
    {
        try
        {
            object? found = history ? await texts.GetRecordAsync(universe, id) : await texts.GetAsync(universe, id, version);
            return found == null
                ? JsonSerializer.Serialize(new { error = "not_found", id, version }, ProseTextService.JsonOptsPublic)
                : JsonSerializer.Serialize(found, ProseTextService.JsonOptsPublic);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = "texts_get_failed", detail = ex.Message }, ProseTextService.JsonOptsPublic);
        }
    }

    [McpServerTool, Description("Draft a game text (prose-text/1, docs/CONDUIT.md) in a universe. json is one document: {id, kind, key, about[], speakers[], payload}. Payload per kind: conversation {title?, start, nodes[{id, speaker, line, next?, choices?[{text, next, tags[]}]}]}; crawl {title, paragraphs[]}; description {short?, long?}; quest {title, giver?, brief, complete?, fail?}; book {title, author?, pages[]}; bark {speaker, context?, lines[]}; glossary {term, aliases?[], definition}. about/speakers are entity ids of the universe ($placeholders allowed as speakers); {entity:<id>} in a line must name an entity. New content becomes a new draft version; identical content changes nothing. Only the author approves (not available here).")]
    public Task<string> DraftText(
        [Description("Universe slug.")] string universe,
        [Description("The prose-text/1 document as JSON.")] string json,
        [Description("Who drafted it (an agent or session name).")] string? by = null) =>
        hub.InvokeAsync(nameof(ProseTextTools), nameof(DraftTextImpl), new { universe, json, by }, universe);

    /// <summary>Runs inside the Hub via ToolDispatch reflection.</summary>
    public async Task<string> DraftTextImpl(string universe, string json, string? by = null)
    {
        var result = await texts.DraftAsync(universe, json, by);
        return JsonSerializer.Serialize(result, ProseTextService.JsonOptsPublic);
    }
}
