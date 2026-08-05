using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using MindAttic.Legion;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Services;

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
    // Was 40 — confirmed live (2026-08-03 full sweep) too tight: several books genuinely
    // completed the entire republish (manuscript check, checkboxes, Save and Continue,
    // Pricing, Publish, redirect confirmed) but exhausted the cap on capture_published_asin/
    // mark_published, the two steps closing out the loop — meaning the book was actually LIVE
    // on KDP but never recorded as such in the DB. Raised with real headroom rather than a
    // minimal bump, since a wasted iteration here just costs a little time, while running out
    // one step before mark_published silently desyncs the DB from reality.
    private const int MaxToolIterations = 60;
    private const int MaxToolIterationsNewListing = 80;

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

        // Hard gate, enforced here (not just in the UI's RunSelectedAsync) so it can never be
        // bypassed by any other caller — see KdpManifestEntry.MeetsHardPublishGate for the single
        // authoritative rule (never re-derive it inline): .publish marker + cover.jpg +
        // description.txt all present on disk, an actual .epub (not a docx-only fallback — KDP
        // auto-converts docx, but that conversion is unverified and this is the format we
        // actually want live), and the current .epub version strictly higher than whatever the
        // .publish marker recorded as last published. Each branch below gives a specific reason
        // rather than one opaque "gate failed" so a human reviewing the log knows exactly what's
        // missing.
        if (!book.ReadyToPublish)
        {
            yield return new OperatorEvent.Error($"{book.Code}: no .publish marker in {book.FolderPath} — not signed off for publish.");
            yield break;
        }
        if (string.IsNullOrWhiteSpace(book.EpubPath))
        {
            yield return new OperatorEvent.Error($"{book.Code}: no .epub found in {book.FolderPath} — run ss --export-node (a .docx alone is not enough).");
            yield break;
        }
        var manuscriptPath = book.EpubPath;
        var coverPath = Path.Combine(book.FolderPath, "cover.jpg");
        if (!book.HasCover)
        {
            yield return new OperatorEvent.Error($"{book.Code}: no cover.jpg found in {book.FolderPath}.");
            yield break;
        }
        if (!book.HasDescriptionFile)
        {
            yield return new OperatorEvent.Error($"{book.Code}: no description.txt found in {book.FolderPath} — run ss --export-node.");
            yield break;
        }
        if (!book.HasNewerVersionThanPublished)
        {
            yield return new OperatorEvent.Info($"{book.Code}: skipped — .epub version {book.Version} is not newer than what's already recorded as published.");
            yield break;
        }

        // A book with none of these is genuinely new on KDP — never published, never even
        // drafted there. Everything else (a stale ASIN-less draft, a republish) goes through the
        // existing find-and-open-book flow instead.
        var isNewListing = string.IsNullOrWhiteSpace(book.Asin)
            && string.IsNullOrWhiteSpace(book.KdpTitleId)
            && string.IsNullOrWhiteSpace(book.PublishUrl);

        if (isNewListing)
        {
            if (book.NewListingPlan == null)
            {
                yield return new OperatorEvent.Error(
                    $"{book.Code}: no first-time-publish plan configured — set kv.Set(\"kdp.newbook.{book.Code}\", ...) " +
                    "(price, categories, DRM, KDP Select, AI-generated-content disclosure) before running this book.");
                yield break;
            }
        }

        var history = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = isNewListing
                            ? BuildNewListingUserMessage(book, manuscriptPath, coverPath!)
                            : BuildUserMessage(book, manuscriptPath),
                    },
                },
            },
        };

        var system = isNewListing ? BuildNewListingSystemPrompt() : BuildSystemPrompt();
        var toolsArray = tools.BuildToolsArray();
        var maxIterations = isNewListing ? MaxToolIterationsNewListing : MaxToolIterations;

        for (int iter = 0; iter < maxIterations; iter++)
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

                    // Update the .publish marker's cache only after a REAL confirmed publish —
                    // mark_published itself already enforces "only after get_page_status shows a
                    // genuine publish confirmation" (see both system prompts), so hooking its
                    // success here can never write this speculatively. Records the manuscript
                    // filename this run actually uploaded so a future manifest build can skip
                    // re-processing this book entirely once the same version is still current.
                    if (!isError && name == "mark_published")
                        TryWritePublishMarker(book, manuscriptPath, resultJson);

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
            $"{book.Code}: tool-use loop hit the {maxIterations}-iteration safety cap without finishing.");
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

    private static string BuildNewListingUserMessage(KdpManifestEntry book, string manuscriptPath, string coverPath)
    {
        var plan = book.NewListingPlan!;
        var categoriesJson = JsonSerializer.Serialize(plan.CategoryPaths);
        var keywordsJson = JsonSerializer.Serialize(book.Keywords.Take(7).ToList());

        return $"""
        Publish this book on KDP for the FIRST TIME — it has never been listed before (no ASIN,
        no KDP titleId):
        - Code: {book.Code}
        - Slug: {book.Slug} (pass THIS exact string as mark_published's slug parameter — not
          the Code above, mark_published looks the book up by Slug)
        - Title: {book.Title}
        {(string.IsNullOrWhiteSpace(book.Subtitle) ? "- Subtitle: (none)" : $"- Subtitle: {book.Subtitle}")}
        - Author: put "{book.Author}" entirely in the author_last field via set_field; leave
          author_first blank. (Established convention for this imprint — confirmed live on an
          already-published MindAttic title.)
        - Manuscript to upload: {manuscriptPath}
        - Cover image to upload: {coverPath}
        - Description (set verbatim via set_description, plain text, no HTML):
          {book.Description}
        - Keywords (up to 7, via enter_keywords): {keywordsJson}
        - Category paths (up to 3, via select_categories): {categoriesJson}
        - List price: ${plan.PriceUsd} USD
        - Royalty plan: {(plan.PriceUsd >= 2.99m ? "70%" : "35% (price is below $2.99, the 70% tier's minimum)")}
        - DRM: {(plan.Drm ? "Yes, apply Digital Rights Management" : "No, do not apply Digital Rights Management")}
        - KDP Select enrollment: {(plan.KdpSelect ? "Yes, enroll" : "No, do not enroll")}
        - Territories: all territories (worldwide rights)
        - Adult content: No
        - AI-generated content disclosure: {(plan.AiTextOption != "None" || plan.AiImagesOption != "None" ? "Yes" : "No")}
          (pass these exact option texts to set_ai_disclosure)
          - Text option: "{plan.AiTextOption}"{(plan.AiTextTool is { Length: > 0 } tTool ? $", tool: \"{tTool}\"" : "")}
          - Images option: "{plan.AiImagesOption}"{(plan.AiImagesTool is { Length: > 0 } iTool ? $", tool: \"{iTool}\"" : "")}
          - Translations option: "{plan.AiTranslationsOption}"

        Work through the full new-listing flow for this one book and report what happened.
        """;
    }

    private static string BuildNewListingSystemPrompt() => """
        You are creating a BRAND-NEW Kindle eBook listing on KDP in a live browser pane — this
        book has never been published before, so there is no bookshelf row to find yet. Work
        through these steps in order:

        1. Call create_new_listing. This clicks through the bookshelf's "+ Create new title or
           series" -> "Create eBook" and lands on an empty Kindle eBook Details page.

        2. Call set_field with field="title" and the given Title. If a Subtitle was given, also
           call set_field with field="subtitle".

        3. Call set_field twice for the author: field="author_last" = the given author string,
           field="author_first" = "" (empty — leave it blank, do not split the name across the
           two fields).

        4. Call set_description with the given Description text, verbatim, no HTML tags.

        5. Call select_form_option with text_candidates=["I own the copyright"] to select the
           rights-ownership radio (every book in this catalog is originally authored, never
           public domain).

        6. Call select_form_option with text_candidates=["No"] for the adult-content question —
           unless you were explicitly told this book IS adult content, in which case stop and
           call log_note instead of guessing.

        7. Call select_categories with the given category paths. If any path fails partway (the
           result names the exact level and lists availableOptions), retry that ONE path using
           one of the options it actually gave you — do not guess a different wording blind, and
           do not abandon the whole call over one bad path; keep the paths that DID succeed.

        8. Call enter_keywords with the given keyword list.

        9. Call click_button with candidates=["Save and Continue"]. Then call get_page_status —
           its titleId field is now populated (KDP mints one the moment Details saves
           successfully). Remember this titleId; you'll pass it to mark_published in step 21. If
           get_page_status instead shows an error banner (e.g. "Add a category for your book."),
           go back and fix whatever it names, then retry this step — don't proceed with an
           incomplete Details page.

        10. You're now on Kindle eBook Content. Call select_form_option with
            text_candidates=["Yes. I have a file"] (confirms you're uploading a manuscript now,
            not deferring to a pre-order).

        11. Call select_form_option for DRM: text_candidates=["No, do not apply Digital Rights
            Management"] or text_candidates=["Yes, apply Digital Rights Management"], matching
            what you were told.

        12. Call upload_manuscript with the given manuscript path. Then call get_page_status
            repeatedly (KDP's conversion can take up to a minute) until isProcessing is false AND
            you see clear success text for the manuscript — do not proceed on silence or a stale
            isProcessing:true.

        13. Call click_button with text_candidates=["Upload a cover you already have"] — this is
            an accordion-style link that reveals the direct cover-upload control (KDP defaults to
            its own Cover Creator instead, which you do not want).

        14. Call upload_cover with the given cover image path. Then call get_page_status
            repeatedly until the cover is accepted (isProcessing false, success text shown).

        15. Call select_form_option with text_candidates=["I don't know if my informative
            images"] for the accessibility question (the safe default absent a real image-alt-
            text audit).

        16. Call select_form_option with text_candidates=["Yes"] or text_candidates=["No"] for
            the top-level AI-generated-content question, matching what you were told. If "Yes":
            three native <select> dropdowns are revealed (confirmed live, stable ids
            generative-ai-questionnaire-text/-images/-translations) — call set_ai_disclosure
            once with the three exact option texts you were given (e.g. "Entire work, with
            extensive editing" / "One or a few AI-generated images, with minimal or no editing" /
            "None") and the tool names for text/images (e.g. "Claude" / "ChatGPT") if their
            option isn't "None" — it fills the follow-up "Which tool(s) did you use" text field
            that appears next to each. If set_ai_disclosure reports an option not found, it also
            returns availableOptions actually present — retry with one of those rather than
            guessing a different wording blind.

        17. Call check_checkbox with text_candidates=["confirm that my answers are accurate"] —
            same confirmation checkbox as the republish flow, repeated after each new upload.

        18. Call click_button with text_candidates=["Save and Continue"] to advance to Pricing.
            Call get_page_status to confirm you're now on the Pricing step; if a "please fix the
            highlighted error(s)" banner shows instead, go back to whichever earlier step it
            names (re-check checkboxes first — see the republish flow's own lesson: a second,
            not-yet-ticked checkbox further down the page is the most common cause).

        19. Call select_form_option with text_candidates=["All territories", "Worldwide"] for the
            territories question.

        20. Call set_price with label_text="Amazon.com" and the given price, e.g. "0.99" — use
            set_price here, NOT set_field: KDP derives every other marketplace's price (UK, DE,
            FR, JP, ...) from whatever is typed into this one field, and that derivation may
            depend on genuine keystrokes rather than a value that merely appears in the input, so
            set_price types it via real keyboard dispatch instead of a value-setter. Then call
            select_form_option for the royalty plan: text_candidates=["35%"] if the price is
            below $2.99, or text_candidates=["70%"] otherwise — KDP will reject 70% below its
            minimum, so pick correctly from the start rather than discovering the rejection.
            Afterward, call get_page_status and verify the OTHER marketplace prices actually
            populated with sensible non-zero converted values (not still blank/zero) before
            moving on — if they didn't, log_note exactly what you saw rather than assuming they
            populated silently.

        21. Call select_form_option for KDP Select enrollment, matching what you were told
            (candidates like ["Enroll", "KDP Select"] to opt in, or leave it unselected /
            explicitly decline if you were told not to enroll).

        22. Call check_checkbox with text_candidates=["confirm that my answers are accurate"]
            once more (Pricing can carry its own confirmation checkbox).

        23. Call click_button with text_candidates=["Publish Your Kindle eBook", "Publish your
            book"]. Then call get_page_status and look for an explicit publish confirmation (a
            modal or banner confirming the book is live/submitted for review). This is the one
            check that matters most — do not call mark_published without clear confirmation text.

        24. Once confirmed, call capture_published_asin (the publish click typically redirects to
            the bookshelf with the URL carrying ?publishedId=<titleId>, which this tool uses to
            find the right book card and read its real Amazon ASIN off its "View on Amazon"
            link). If it returns found:true, call mark_published with the book's slug (from
            above, NOT the Code), the titleId from step 9, and url=
            "https://www.amazon.com/dp/<asin>" using the captured ASIN — this is what lets
            mark_published record the real ASIN and product URL on the very first pass. If
            capture_published_asin returns found:false (Amazon hasn't finished listing it yet —
            normal in the first few minutes), still call mark_published with just the slug and
            titleId; the ASIN can be filled in on a later run.

        25. Call click_button with candidates=["done", "close"] to dismiss the confirmation modal.

        RULES:
        - Do not state that any banner, error, or blocker exists unless you paste its exact text,
          character-for-character, from a tool_result you actually received this run — copy it
          directly out of the JSON, don't paraphrase or reconstruct it from memory.
        - Never call mark_published speculatively — only after get_page_status shows a genuine
          publish confirmation.
        - If a step fails or times out after a few retries, call log_note explaining exactly what
          you saw and stop — don't loop indefinitely on the same failing action, and don't
          silently skip a required field to "get past" a validation error.
        - Title cannot be changed after publication (KDP's own warning on the Details page) — if
          anything about the title/spelling looks off before step 9's save, call log_note and
          stop rather than saving something wrong.
        - When you are done (success or a clean stop with a reason logged), reply with a short
          final text summary — that ends this book's processing.
        """;

    /// <summary>Best-effort write of the .publish marker's JSON body (see
    /// <see cref="KdpManifestService.PublishMarker"/>) right after a confirmed publish. Never
    /// throws into the caller — a cache-write failure must not fail the actual publish run that
    /// already succeeded on KDP's side.</summary>
    private static void TryWritePublishMarker(KdpManifestEntry book, string manuscriptPath, string markPublishedResultJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(markPublishedResultJson);
            if (!doc.RootElement.TryGetProperty("Ok", out var okProp) || !okProp.GetBoolean()) return;

            var asin = doc.RootElement.TryGetProperty("Asin", out var asinProp) ? asinProp.GetString() : null;
            var marker = new PublishMarker(
                File: Path.GetFileName(manuscriptPath),
                Asin: asin,
                PublishedAtUtc: DateTime.UtcNow.ToString("o")
            );
            File.WriteAllText(Path.Combine(book.FolderPath, ".publish"), JsonSerializer.Serialize(marker));
        }
        catch { /* best-effort — see remarks */ }
    }

    private static JsonArray CloneArray(JsonArray src)
    {
        var dst = new JsonArray();
        foreach (var node in src) dst.Add(node?.DeepClone());
        return dst;
    }
}
