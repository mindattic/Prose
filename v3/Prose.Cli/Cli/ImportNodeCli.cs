using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --import-book --file path.node</c> — materialize a node from a
/// human-authored "beat + gap + beat" text file. The complement to
/// <see cref="WriteNodeCli"/> (which generates nodes via the LLM) — this
/// is for hand-authored content (a draft pasted in from a chat, a transcript,
/// a rewrite from an external editor).
///
/// File format ("<c>.node</c>"):
/// <code>
/// # Title: Optional node title (overrides --title)
/// # Kind: episode | scene | chapter | book | …
/// # Voice: optional-elevenlabs-voice-id
/// # Synopsis: One-line description (optional)
///
/// %% beat
/// First beat prose. Multiple lines are allowed and become one Beat.Text.
///
/// %% beat tone:tense pace:clipped
/// Second beat. The key:value pairs are per-beat metadata that map onto
/// the Beat columns. Recognised keys: title, tone, pace, facet, kind,
/// scene-type, structure-role, act, gap, chapter, voice.
///
/// %% gap 800
/// A standalone gap line sets the PRECEDING beat's GapAfterMs override.
/// You can also write 'gap:800' on the next %% beat line — same effect.
///
/// %% beat chapter:"After the Fall" gap:1500
/// This beat starts a new chapter; the chapter heading is "After the Fall".
/// </code>
///
/// Args:
///   --file PATH        Required. Path to the .node file (or "-" for stdin).
///   --title "..."      Override the file's # Title header.
///   --kind KIND        Override the file's # Kind header. Default "episode".
///   --slug SLUG        Force a specific slug (else derived from title).
///   --parent SLUG      Attach the new node as a child of an existing node
///                      (e.g. a book node for a chapter import).
///   --dry-run          Parse only — don't write anything.
///
/// Output: the new Node's id + slug + URL, plus a beat count.
/// </summary>
public static class ImportNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? file = null, titleOverride = null, kindOverride = null, slugOverride = null, parentSlug = null;
        bool dryRun = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--file":    if (i + 1 < args.Length) file = args[++i]; break;
                case "--title":   if (i + 1 < args.Length) titleOverride = args[++i]; break;
                case "--kind":    if (i + 1 < args.Length) kindOverride = args[++i]; break;
                case "--slug":    if (i + 1 < args.Length) slugOverride = args[++i]; break;
                case "--parent":  if (i + 1 < args.Length) parentSlug = args[++i]; break;
                case "--dry-run": dryRun = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(file))
        {
            Console.Error.WriteLine("[import-book] --file is required (or '-' for stdin).");
            Console.Error.WriteLine("Usage: ss --import-book --file path.node [--title ...] [--kind ...] [--slug ...] [--parent ...] [--dry-run]");
            return 2;
        }

        string raw;
        if (file == "-")
        {
            raw = await Console.In.ReadToEndAsync();
        }
        else
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"[import-book] File not found: {file}");
                return 1;
            }
            raw = await File.ReadAllTextAsync(file);
        }

        ParsedNodeFile parsed;
        try { parsed = NodeFileParser.Parse(raw); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[import-book] Parse failed: {ex.Message}");
            return 1;
        }

        if (parsed.Beats.Count == 0)
        {
            Console.Error.WriteLine("[import-book] No beats found in the file.");
            return 1;
        }

        var title = !string.IsNullOrWhiteSpace(titleOverride) ? titleOverride : parsed.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.Error.WriteLine("[import-book] No # Title: header and no --title override.");
            return 1;
        }
        var kind = !string.IsNullOrWhiteSpace(kindOverride) ? kindOverride : (parsed.Kind ?? "episode");

        Console.WriteLine($"[import-book] file={file} title=\"{title}\" kind={kind} beats={parsed.Beats.Count} dry-run={dryRun}");
        for (int i = 0; i < parsed.Beats.Count; i++)
        {
            var b = parsed.Beats[i];
            var preview = b.Text.Length > 60 ? b.Text[..60].Replace('\n', ' ') + "…" : b.Text.Replace('\n', ' ');
            var meta = new List<string>();
            if (!string.IsNullOrEmpty(b.Title)) meta.Add($"title=\"{b.Title}\"");
            if (b.IsChapterStart) meta.Add("chapter-start");
            if (!string.IsNullOrEmpty(b.EmotionalTone)) meta.Add($"tone={b.EmotionalTone}");
            if (!string.IsNullOrEmpty(b.PaceHint)) meta.Add($"pace={b.PaceHint}");
            if (b.GapAfterMs.HasValue) meta.Add($"gap={b.GapAfterMs}ms");
            if (b.SceneType != "scene") meta.Add($"scene={b.SceneType}");
            var metaStr = meta.Count > 0 ? "  [" + string.Join(' ', meta) + "]" : "";
            Console.WriteLine($"  {i + 1,3}. {preview}{metaStr}");
        }

        if (dryRun) { Console.WriteLine("[import-book] dry-run — nothing written."); return 0; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        Guid? parentNodeId = null;
        if (!string.IsNullOrWhiteSpace(parentSlug))
        {
            var p = await db.Nodes.FirstOrDefaultAsync(s => s.Slug == parentSlug);
            if (p == null) { Console.Error.WriteLine($"[import-book] --parent slug not found: {parentSlug}"); return 1; }
            parentNodeId = p.Id;
        }

        var nodeId = Guid.CreateVersion7();
        var slug = !string.IsNullOrWhiteSpace(slugOverride)
            ? slugOverride!
            : $"{Slugify(title)}-{nodeId.ToString("N")[..8]}";

        // Serializable transaction around the sibling-max read + write so
        // two concurrent imports under the same parent can't both claim
        // `max + 100` and collide. SQL Server will deadlock-retry or block
        // the second reader until the first commits.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var siblingMaxSort = parentNodeId.HasValue
            ? await db.Nodes.Where(s => s.ParentNodeId == parentNodeId).Select(s => (double?)s.SortKey).MaxAsync() ?? 0
            : await db.Nodes.Where(s => s.ParentNodeId == null).Select(s => (double?)s.SortKey).MaxAsync() ?? 0;

        var node = NodeFactory.Create(kind);
        node.Id           = nodeId;
        node.Slug         = slug;
        node.Title        = title!;
        node.Status       = "draft";
        node.Description  = parsed.Description;
        node.VoiceId      = parsed.VoiceId;
        node.ParentNodeId = parentNodeId;
        node.SortKey      = siblingMaxSort + 100.0;
        db.Nodes.Add(node);

        // Pre-allocate a contiguous block of Beat.Number values in one round-trip
        // — matches the pattern in NodeWorkbenchService.SplitBeatByParagraphsAsync.
        var baseNumber = (await db.Beats.MaxAsync(b => (int?)b.Number) ?? 0) + 1;
        double sortKey = 100.0;
        for (int i = 0; i < parsed.Beats.Count; i++)
        {
            var pb = parsed.Beats[i];
            var beat = new Beat
            {
                Id             = Guid.CreateVersion7(),
                Number         = baseNumber + i,
                Text           = pb.Text,
                TextHash       = string.IsNullOrEmpty(pb.Text) ? null : NodeWorkbenchService.ComputeTextHash(pb.Text),
                Title          = pb.Title,
                IsChapterStart = pb.IsChapterStart,
                Kind           = string.IsNullOrEmpty(pb.Kind) ? "prose" : pb.Kind,
                Description    = pb.Description,
                StructureRole  = pb.StructureRole,
                Act            = pb.Act,
                SceneType      = string.IsNullOrEmpty(pb.SceneType) ? "scene" : pb.SceneType,
                EmotionalTone  = pb.EmotionalTone,
                PaceHint       = pb.PaceHint,
                GapAfterMs     = pb.GapAfterMs,
                VoiceId        = pb.VoiceId,
            };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey });
            sortKey += 100.0;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Console.WriteLine();
        Console.WriteLine($"[import-book] OK — {parsed.Beats.Count} beats written.");
        Console.WriteLine($"   Id:    {nodeId}");
        Console.WriteLine($"   Slug:  {slug}");
        Console.WriteLine($"   Title: {title}");
        Console.WriteLine($"   Kind:  {kind}");
        if (parentNodeId.HasValue) Console.WriteLine($"   Parent: {parentSlug} ({parentNodeId})");
        Console.WriteLine($"   URL:   https://localhost:7103/node/{slug}");
        Console.WriteLine($"   Next:  open the URL to edit, or run ss --narrate-book --slug {slug} to record.");
        return 0;
    }

    private static string Slugify(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "node";
        var lower = s.ToLowerInvariant();
        var ascii = System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(ascii) ? "node" : ascii;
    }
}

/// <summary>Parsed representation of a .node file. Public on purpose so unit
/// tests can drive the parser directly without going through the CLI.</summary>
public sealed class ParsedNodeFile
{
    public string? Title { get; set; }
    public string? Kind { get; set; }
    public string? Description { get; set; }
    public string? VoiceId { get; set; }
    public List<ParsedBeat> Beats { get; } = new();
}

public sealed class ParsedBeat
{
    public string Text { get; set; } = "";
    public string? Title { get; set; }
    public bool IsChapterStart { get; set; }
    public string Kind { get; set; } = "prose";
    public string? Description { get; set; }
    public string? StructureRole { get; set; }
    public int Act { get; set; }
    public string SceneType { get; set; } = "scene";
    public string? EmotionalTone { get; set; }
    public string? PaceHint { get; set; }
    public int? GapAfterMs { get; set; }
    public string? VoiceId { get; set; }
}

/// <summary>
/// Parser for the .node text format. See <see cref="ImportNodeCli"/> for
/// the format spec. Designed to be liberal about whitespace and forgiving on
/// unknown keys (logs and skips, doesn't throw — so writers can paste rough
/// drafts and iterate).
/// </summary>
public static class NodeFileParser
{
    private const string BeatMarker = "%% beat";
    private const string GapMarker  = "%% gap";

    public static ParsedNodeFile Parse(string content)
    {
        var result = new ParsedNodeFile();
        if (string.IsNullOrEmpty(content)) return result;

        // Normalise newlines so the line-by-line loop is platform-independent.
        var lines = content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        ParsedBeat? current = null;
        var buf = new System.Text.StringBuilder();
        bool inFrontMatter = true;

        void Flush()
        {
            if (current == null) return;
            current.Text = buf.ToString().Trim();
            // Discard beats that have no prose AND no metadata worth keeping.
            // (An empty %% beat followed immediately by another %% beat with
            // no text between them is a writer oversight, not a real beat.)
            if (current.Text.Length > 0 || current.IsChapterStart || !string.IsNullOrEmpty(current.Title))
                result.Beats.Add(current);
            current = null;
            buf.Clear();
        }

        for (int li = 0; li < lines.Length; li++)
        {
            var line = lines[li];
            var trimmed = line.Trim();

            // Front matter: leading lines starting with "# Key: value". Stops
            // at the first non-empty non-# line (which is typically the first
            // %% beat marker).
            if (inFrontMatter)
            {
                if (trimmed.Length == 0) continue;
                if (trimmed.StartsWith('#'))
                {
                    var headerBody = trimmed.TrimStart('#').Trim();
                    var colon = headerBody.IndexOf(':');
                    if (colon > 0)
                    {
                        var key = headerBody[..colon].Trim().ToLowerInvariant();
                        var val = headerBody[(colon + 1)..].Trim();
                        switch (key)
                        {
                            case "title":    result.Title = val; break;
                            case "kind":     result.Kind = val; break;
                            case "synopsis": result.Description = val; break;
                            case "voice":    result.VoiceId = val; break;
                        }
                    }
                    continue;
                }
                inFrontMatter = false;
                // fall through to body processing
            }

            // %% gap N : sets the preceding beat's GapAfterMs override.
            if (trimmed.StartsWith(GapMarker, StringComparison.OrdinalIgnoreCase))
            {
                var rest = trimmed[GapMarker.Length..].Trim();
                if (int.TryParse(rest.TrimEnd('m', 's', 'M', 'S'), out var ms) && result.Beats.Count > 0)
                {
                    result.Beats[^1].GapAfterMs = Math.Clamp(ms, 0, 6000);
                }
                continue;
            }

            // %% beat [key:value …] : starts a new beat. Any text after the
            // marker is parsed as per-beat metadata (the prose starts on the
            // following line).
            if (trimmed.StartsWith(BeatMarker, StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                current = new ParsedBeat();
                var metaLine = trimmed[BeatMarker.Length..].Trim();
                if (metaLine.Length > 0) ApplyMetaTokens(current, metaLine, result);
                continue;
            }

            if (current == null)
            {
                // Body text before any %% beat marker — implicit first beat.
                current = new ParsedBeat();
            }
            buf.Append(line).Append('\n');
        }
        Flush();
        return result;
    }

    /// <summary>Parse a "key:value foo:\"quoted bar\"" metadata string from a
    /// %% beat line. Liberal: unknown keys are silently ignored so future
    /// schemas don't break older imports.</summary>
    private static void ApplyMetaTokens(ParsedBeat beat, string metaLine, ParsedNodeFile file)
    {
        foreach (var (key, val) in TokeniseMeta(metaLine))
        {
            switch (key.ToLowerInvariant())
            {
                case "title":         beat.Title = val; break;
                case "synopsis":      beat.Description = val; break;
                case "tone":          beat.EmotionalTone = val.ToLowerInvariant(); break;
                case "pace":          beat.PaceHint = val.ToLowerInvariant(); break;
                case "kind":          beat.Kind = val.ToLowerInvariant(); break;
                case "scene-type":
                case "scene":         beat.SceneType = val.ToLowerInvariant(); break;
                case "structure":
                case "structure-role":beat.StructureRole = val; break;
                case "act":           if (int.TryParse(val, out var act)) beat.Act = act; break;
                case "gap":           if (int.TryParse(val.TrimEnd('m','s'), out var ms)) beat.GapAfterMs = Math.Clamp(ms, 0, 6000); break;
                case "chapter":       beat.IsChapterStart = true; beat.Title = val; break;
                case "voice":         beat.VoiceId = val; break;
            }
        }
    }

    /// <summary>Yield key/value pairs from a meta string, handling double-
    /// quoted values that may contain spaces and backslash-escaped quotes
    /// (so a chapter title like <c>chapter:"Chapter \"Two\""</c> parses as
    /// <c>chapter</c> = <c>Chapter "Two"</c>). Forgiving on malformed input —
    /// partial pairs are dropped rather than throwing.</summary>
    private static IEnumerable<(string Key, string Value)> TokeniseMeta(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length) yield break;

            int keyStart = i;
            while (i < s.Length && s[i] != ':' && !char.IsWhiteSpace(s[i])) i++;
            if (i >= s.Length || s[i] != ':') yield break;
            var key = s[keyStart..i];
            i++; // skip ':'

            string val;
            if (i < s.Length && s[i] == '"')
            {
                i++; // skip opening quote
                var sb = new System.Text.StringBuilder();
                while (i < s.Length && s[i] != '"')
                {
                    // Backslash escape: \" yields a literal ", \\ yields a
                    // literal backslash. Any other \x sequence is left as
                    // two characters so the writer's own backslashes (e.g.
                    // path samples) round-trip unchanged.
                    if (s[i] == '\\' && i + 1 < s.Length && (s[i + 1] == '"' || s[i + 1] == '\\'))
                    {
                        sb.Append(s[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        sb.Append(s[i]);
                        i++;
                    }
                }
                val = sb.ToString();
                if (i < s.Length) i++; // closing quote
            }
            else
            {
                int valStart = i;
                while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;
                val = s[valStart..i];
            }
            if (!string.IsNullOrEmpty(key)) yield return (key, val);
        }
    }
}
