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
/// RFC 0018 (PRS-T922) adds <c>verify_texts</c> and <c>text_findings</c>: the verifiers write findings only, never a
/// text, so an agent may run them and read what they found; acting on a finding stays the author's decision.
/// </summary>
[McpServerToolType]
public class ProseTextTools
{
    private readonly ProseTextService texts;
    private readonly ProseTextVerifier verifier;
    private readonly HubInvoker hub;

    public ProseTextTools(ProseTextService texts, ProseTextVerifier verifier, HubInvoker hub)
    {
        this.texts = texts;
        this.verifier = verifier;
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

    [McpServerTool, Description("Draft a game text (prose-text/1, docs/CONDUIT.md) in a universe. json is one document: {id, kind, key, about[], speakers[], payload}. Payload per kind: conversation {title?, start, nodes[{id, speaker, line, next?, choices?[{text, next, tags[]}]}]}; crawl {title, paragraphs[]}; description {short?, long?}; quest {title, giver?, brief, complete?, fail?}; book {title, author?, pages[]}; bark {speaker, context?, lines[]}; glossary {term, aliases?[], definition}. Optional context (RFC 0018), where and when the text appears: {place?: entity id, chapter?: label, order?: number (earliest story point the player can see it), trigger?: the game's handle, after?: [text ids seen first]}. about/speakers are entity ids of the universe ($placeholders allowed as speakers); {entity:<id>} in a line must name an entity. New content becomes a new draft version; identical content changes nothing. batch tags the new version with its drafting pass (e.g. 'v4'), so the author can bulk-approve that pass alone. The result lists what the verifiers found on the new version (findings describe; they never refuse a draft). Only the author approves (not available here).")]
    public Task<string> DraftText(
        [Description("Universe slug.")] string universe,
        [Description("The prose-text/1 document as JSON.")] string json,
        [Description("Who drafted it (an agent or session name).")] string? by = null,
        [Description("Optional drafting-pass tag (letters, digits, _ . : / -; up to 64), e.g. 'v4'. Use the same tag for every text of one pass.")] string? batch = null) =>
        hub.InvokeAsync(nameof(ProseTextTools), nameof(DraftTextImpl), new { universe, json, by, batch }, universe);

    /// <summary>Runs inside the Hub via ToolDispatch reflection.</summary>
    public async Task<string> DraftTextImpl(string universe, string json, string? by = null, string? batch = null)
    {
        var result = await texts.DraftAsync(universe, json, by, batch);
        return JsonSerializer.Serialize(result, ProseTextService.JsonOptsPublic);
    }

    [McpServerTool, Description("Run the game-mode verifiers (RFC 0018) over a game universe's texts and file their findings: canon (retired or missing entities, glossary conflicts), continuity (a name the player meets before its introduction, judged by each text's context.order/after; bad after links; a context.place that is not a place), obligations (a quest with no complete text, entities with no description), voice drift per entity speaker, conversation branches (unreachable nodes, no ending, duplicate choices) and bark pools (duplicate, repeated, long lines). Writes findings only, never a text. ids: the texts to check (default: every text); kind narrows to one kind.")]
    public Task<string> VerifyTexts(
        [Description("Universe slug.")] string universe,
        [Description("Text ids to check; omit for every text.")] string[]? ids = null,
        [Description("Only texts of this kind.")] string? kind = null) =>
        hub.InvokeAsync(nameof(ProseTextTools), nameof(VerifyTextsImpl), new { universe, ids, kind }, universe);

    /// <summary>Runs inside the Hub via ToolDispatch reflection.</summary>
    public async Task<string> VerifyTextsImpl(string universe, string[]? ids = null, string? kind = null)
    {
        var result = await verifier.VerifyAsync(universe, ids, kind);
        return JsonSerializer.Serialize(result, ProseTextService.JsonOptsPublic);
    }

    [McpServerTool, Description("The findings the game-mode verifiers (RFC 0018) filed on a universe's texts, or on one text (every version). status: open (new and triaged), new, triaged, applied, dismissed, all (default: all). Findings describe; the author decides.")]
    public Task<string> TextFindings(
        [Description("Universe slug.")] string universe,
        [Description("One text id; omit for every text of the universe.")] string? id = null,
        [Description("open | new | triaged | applied | dismissed | all.")] string? status = null) =>
        hub.InvokeAsync(nameof(ProseTextTools), nameof(TextFindingsImpl), new { universe, id, status }, universe);

    /// <summary>Runs inside the Hub via ToolDispatch reflection.</summary>
    public Task<string> TextFindingsImpl(string universe, string? id = null, string? status = null)
    {
        if (!ProseTextVerifier.TryParseStatusFilter(status, out var statuses, out var error))
            return Task.FromResult(JsonSerializer.Serialize(new { error = "unknown_status", status, detail = error }, ProseTextService.JsonOptsPublic));
        var rows = verifier.ListFindingRows(universe, id, statuses);
        return Task.FromResult(JsonSerializer.Serialize(new { count = rows.Count, findings = rows }, ProseTextService.JsonOptsPublic));
    }
}
