using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using MindAttic.Legion;
using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services.Operator;

/// <summary>
/// Drives one book through the KDP republish flow: find it on the bookshelf, upload the new
/// manuscript, save/continue, publish, confirm, record. Same Anthropic tool-use loop shape as
/// <see cref="WriterOperatorService"/> (same client, same event stream), scoped to ONE book per
/// call rather than an ongoing chat — each book gets a fresh conversation (no history carried
/// over from the previous book) so a stumble on book N can't drag book N+1's context down with
/// it. The caller (task #7) loops this over every checked book in sequence.
/// </summary>
public class KdpOperatorService
{
    private readonly AnthropicToolClient client;
    private readonly KdpToolRegistry tools;
    private readonly ILogger<KdpOperatorService> log;

    private const string Model = "claude-opus-4-7";
    private const int MaxTokens = 4096;
    private const int MaxToolIterations = 40;

    public KdpOperatorService(AnthropicToolClient client, KdpToolRegistry tools, ILogger<KdpOperatorService> log)
    {
        this.client = client;
        this.tools = tools;
        this.log = log;
    }

    public async IAsyncEnumerable<OperatorEvent> ProcessBookAsync(
        KdpManifestEntry book,
        KdpOperatorContext ctx,
        [EnumeratorCancellation] CancellationToken cancel = default)
    {
        var apiKey = MindAtticCredentialStore.GetKey("claude-api");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return new OperatorEvent.Error(
                "No Anthropic API key configured. Add a 'claude-api' provider key in Settings.");
            yield break;
        }

        var manuscriptPath = book.EpubPath ?? book.DocxPath;
        if (string.IsNullOrWhiteSpace(manuscriptPath))
        {
            yield return new OperatorEvent.Error($"{book.Code}: no manuscript file on disk — run ss --export-node.");
            yield break;
        }

        var history = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = new JsonArray
                {
                    new JsonObject { ["type"] = "text", ["text"] = BuildUserMessage(book, manuscriptPath) },
                },
            },
        };

        var system = BuildSystemPrompt();
        var toolsArray = tools.BuildToolsArray();

        for (int iter = 0; iter < MaxToolIterations; iter++)
        {
            cancel.ThrowIfCancellationRequested();

            AnthropicTurnResponse? turn = null;
            string? callError = null;
            try
            {
                turn = await client.CreateAsync(apiKey, Model, system, CloneArray(history), toolsArray, MaxTokens, cancel);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Anthropic call failed for {Code}", book.Code);
                callError = ex.Message;
            }
            if (turn == null)
            {
                yield return new OperatorEvent.Error(callError ?? "Anthropic call returned null.");
                yield break;
            }

            history.Add(new JsonObject { ["role"] = "assistant", ["content"] = CloneArray(turn.Content) });

            var toolResults = new JsonArray();
            var sawToolUse = false;

            foreach (var block in turn.Content)
            {
                if (block is null) continue;
                var type = block["type"]?.GetValue<string>();
                if (type == "text")
                {
                    var text = block["text"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(text))
                        yield return new OperatorEvent.AssistantText(text);
                }
                else if (type == "tool_use")
                {
                    sawToolUse = true;
                    var id = block["id"]?.GetValue<string>() ?? "";
                    var name = block["name"]?.GetValue<string>() ?? "";
                    var input = block["input"];
                    var argsJson = input?.ToJsonString() ?? "{}";

                    yield return new OperatorEvent.ToolStarted(name, argsJson);

                    string resultJson;
                    bool isError = false;
                    try
                    {
                        var tool = tools.Get(name) ?? throw new InvalidOperationException($"Unknown tool: {name}");
                        using var argsDoc = JsonDocument.Parse(argsJson);
                        resultJson = await tool.InvokeAsync(argsDoc.RootElement, ctx, cancel);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.LogError(ex, "Tool {Tool} threw for {Code}", name, book.Code);
                        resultJson = JsonSerializer.Serialize(new { error = ex.Message });
                        isError = true;
                    }

                    yield return new OperatorEvent.ToolCompleted(name, resultJson, isError);

                    toolResults.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = id,
                        ["content"] = resultJson,
                        ["is_error"] = isError,
                    });
                }
            }

            if (!sawToolUse) yield break;

            history.Add(new JsonObject { ["role"] = "user", ["content"] = toolResults });
        }

        yield return new OperatorEvent.Error(
            $"{book.Code}: tool-use loop hit the {MaxToolIterations}-iteration safety cap without finishing.");
    }

    private static string BuildUserMessage(KdpManifestEntry book, string manuscriptPath) => $"""
        Process this book:
        - Code: {book.Code}
        - Title: {book.Title}
        - Slug: {book.Slug}
        - Target version: V{book.Version}
        - Manuscript to upload: {manuscriptPath}
        {(string.IsNullOrWhiteSpace(book.Subtitle) ? "- Subtitle: (none — this book has no subtitle set; skip step 1a, don't clear an existing KDP subtitle either)" : $"- Subtitle: {book.Subtitle}")}
        {(string.IsNullOrWhiteSpace(book.KdpTitleId) ? "" : $"- Known titleId from a previous run: {book.KdpTitleId} — pass this as find_and_open_book's known_title_id so it can skip straight there.")}
        {(string.IsNullOrWhiteSpace(book.Asin) ? "" : $"- Known ASIN: {book.Asin} — pass this as find_and_open_book's known_asin. This is the PRIMARY way to find this book: searching KDP's own bookshelf search box for this exact ASIN resolves to this one book with zero ambiguity, unlike the title text above (which commonly diverges from KDP's displayed title by a subtitle/series suffix).")}

        Work through the full republish flow for this one book and report what happened.
        """;

    private static string BuildSystemPrompt() => """
        You are operating the KDP (Kindle Direct Publishing) bookshelf in a live browser pane to
        republish a book's manuscript with a newer version. You have tools, not raw DOM access —
        use them in this order:

        1. find_and_open_book with the book's title. ALWAYS also pass known_title_id and
           known_asin too, whenever you were given either above — do not omit them. It tries,
           in order: known_title_id (a direct URL, fastest); known_asin (typed into KDP's own
           "Search by title" box — this is the PRIMARY, most reliable way to locate a specific
           book: an ASIN search resolves to that exact book with zero ambiguity, unlike title
           text, which commonly diverges from KDP's displayed title by a subtitle/series
           suffix); the title text itself, only as a last resort if neither of those was given
           or panned out. On success it returns a titleId — remember it, you will pass it to
           mark_published in step 10 so future runs on this same book can skip straight to it via
           known_title_id. If it returns found:false with likelyPublishing:true, the book's row
           exists but has no edit link — KDP hides that "..." options menu while an edition is
           already mid-publish. This is a normal, temporary, real state, not an error: call
           log_note saying so and STOP, don't retry or invent another cause. If it returns
           found:false with no likelyPublishing, the book genuinely isn't on the bookshelf under
           that title — call log_note explaining that and STOP; do not guess or invent a match.

        1a. If you were given a Subtitle above (not the "none" placeholder), call sync_subtitle
            with it now, before anything else. This detours to the Details step and back — that's
            expected, not an error. It reads the live Subtitle field and only writes to it if the
            value differs from what you gave it; changed:false means it already matched and
            nothing was touched. After calling it, call check_checkbox then click_button with
            ["save and continue"] to advance off the Details step (same as any other step-change
            in this flow — do this even if changed was false, to get back to Content), then call
            get_page_status once to confirm you're looking at the Edit eBook content page again
            before continuing to step 2. If sync_subtitle returns found:false, call log_note
            noting the Subtitle field wasn't found and continue anyway — the manuscript replace
            below matters more than this correction, don't let it block the rest of the flow. If
            you were given the "none" placeholder, skip this step entirely — do not navigate to
            Details, do not call sync_subtitle, do not clear an existing subtitle KDP already has.

        2. Once on the Edit eBook content page, call get_page_status BEFORE uploading anything.
           KDP shows the currently-attached manuscript's filename and version in a success
           banner, e.g. Manuscript "ICFI V24.epub" uploaded successfully! — extract the V<N>
           from that filename and compare it to your target version. If the page's version is
           already >= your target version, SKIP step 3 (upload_manuscript) only — the file
           itself doesn't need re-uploading. Do NOT treat a matching version as proof the book
           is published, and do NOT call mark_published at this point. This banner only reflects
           what's attached to the Content step, not whether Pricing/Publish were ever completed —
           confirmed live: a prior run can upload the correct version here, then stop before
           reaching Publish (e.g. an unticked confirmation checkbox silently blocked Save and
           Continue), leaving the manuscript sitting at the right version but the book still
           genuinely unpublished. So: whether you skip step 3 or not, always continue on to step
           4 (or step 5 directly if you skipped the upload) and complete the ENTIRE rest of the
           flow through step 11 — checkboxes, Save and Continue, Pricing, Publish, confirmation,
           mark_published. mark_published only ever happens after step 9's genuine
           publish-confirmation, never as a shortcut here.

        3. Call upload_manuscript with the given manuscript path. No file dialog will appear —
           the file attaches directly. The page is a client-rendered SPA, so the upload control
           retries internally for up to 10 seconds if it isn't in the DOM yet right after
           navigation — you do not need to add your own delay before calling this.

        4. If you called upload_manuscript in step 3, call get_page_status. Look for confirmation
           the manuscript was accepted (e.g. text containing "uploaded successfully") AND that
           the filename now shows your target version, not the old one. If nothing conclusive
           appears yet, call get_page_status again — KDP's processing can take up to a minute.
           Do not proceed until you see clear success text for the NEW version; do not assume
           success from silence or from an old version number still showing. If you skipped step
           3 because the version already matched, skip this check too and go straight to step 5.

           IMPORTANT — check isProcessing on every get_page_status result from here through step
           5: KDP shows a distinct "still preparing/converting/scanning" status for the uploaded
           file, separate from the eventual success banner. Checking the confirmation checkboxes
           while isProcessing is true does not reliably stick — confirmed live. If isProcessing
           is true (even if a success banner also happens to be showing), wait and call
           get_page_status again; do not call check_checkbox until isProcessing is false.

        5. Call check_checkbox with candidates like ["confirm that my answers are accurate"].
           KDP repeats this SAME confirmation checkbox once per section that had a new upload
           (manuscript, cover, etc.) — a single page commonly has TWO OR MORE identical
           checkboxes, not just one. check_checkbox ticks every unchecked match it finds in one
           call and returns {checkedCount, matches}. Do this BEFORE step 6, every time, even if
           you don't see any checkbox mentioned in get_page_status (get_page_status only reports
           banners, not form controls). checkedCount:0 is fine — it just means this page didn't
           need one.

        6. Call click_button with candidates like ["save and continue", "save and publish"] to
           advance past the manuscript step.

        7. Call get_page_status to confirm the page moved on (a new heading, a pricing or rights
           step, etc.). If the heading/URL looks unchanged, call get_page_status A SECOND TIME
           before concluding anything — click_button's own transition delay plus one re-check is
           usually enough for KDP's client-side navigation to finish; a single immediate read is
           not sufficient evidence of anything. Only after two checks still show no change should
           you treat it as stalled — and even then, if a "please fix the highlighted error" banner
           is present, go back to step 5 and call check_checkbox again (if checkedCount was ever
           >0 on this page before, there is almost certainly another duplicate checkbox further
           down the page). Self-diagnosing a missing control instead of re-checking has been WRONG
           every time it has happened so far — treat that instinct as a red flag, not a conclusion.

        8. The Pricing step (after Content) has its own Save and Continue / royalty settings —
           do not change any price, territory, or royalty field. Call check_checkbox (there can be
           confirmation checkboxes here too) then click_button with candidates like ["publish
           your kindle ebook", "save and publish", "save and continue"] — confirmed live, the
           real button text is "Publish Your Kindle eBook", not a generic "Publish Your Book".

        9. Call get_page_status and look for an explicit publish-confirmation (a modal or banner
           confirming the book is live/updated). This is the one check that matters most — do
           not call mark_published without clear confirmation text.

        10. Once confirmed, call mark_published with the book's slug so our tracking reflects
            reality. If you were given a titleId or Amazon URL you discovered along the way,
            include those too.

        11. Call click_button with candidates like ["done", "close"] to dismiss the publish
            confirmation modal. Leaving it open blocks the bookshelf underneath it — the next
            book's find_and_open_book call needs a clean bookshelf view, not a modal on top of it.

        RULES:
        - Before uploading or publishing, the title on the page should match the book you were
          asked to process — if something looks wrong (different book, different title), call
          log_note explaining the mismatch and STOP rather than proceeding.
        - A book that was JUST published (by this run or recently) shows "Live - Updates
          publishing" on the bookshelf for up to 72 hours (confirmed live in the page's own
          popover text: "Your recent changes are being published to the Kindle Store... the
          previous version of your title is live and available for purchase in the Kindle
          Store"), and its row temporarily loses the "..." options/edit-content link during that
          window. This is a real, normal, temporary KDP state — not an error and not a reason to
          invent an explanation. If find_and_open_book can't reach the
          edit page for a book in this state, call log_note saying it's likely in the post-publish
          review window and STOP; do not guess at some other cause.
        - Never call mark_published speculatively — only after get_page_status shows a genuine
          publish confirmation.
        - Do not state that any banner, error, or listing-state blocker exists unless you paste
          its exact text, character-for-character, from a tool_result you actually received this
          run — copy it directly out of the JSON, don't paraphrase or reconstruct it from memory.
          If you cannot paste an exact quoted string, you have not observed a blocker — say what
          the tool actually returned (including an empty banners list) instead of naming or
          describing any error, listing state, or account condition. A tool returning an error
          (e.g. upload_manuscript failing, or get_page_status returning banners:[]) means exactly
          what it says and nothing more.
        - Touch ONLY the manuscript-replace flow and the Subtitle field (step 1a) — do not change
          price, rights, categories, the title, the description, or anything else on the listing.
        - If a step fails or times out after a few retries, call log_note explaining what
          happened and stop — don't loop indefinitely on the same failing action.
        - When you are done (success or a clean stop with a reason logged), reply with a short
          final text summary — that ends this book's processing.
        """;

    private static JsonArray CloneArray(JsonArray src)
    {
        var dst = new JsonArray();
        foreach (var node in src) dst.Add(node?.DeepClone());
        return dst;
    }
}
