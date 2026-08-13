using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Turns one review run's ballots into a portable, self-contained report: a
/// <c>.reviews.json</c> data file (every voter + per-beat aggregate + complaint
/// histogram + clusters + run metadata, incl. which BRAIN ran — cloud vs local)
/// and a <c>.reviews.htm</c> viewer that embeds that JSON and renders it with
/// vanilla JS (sortable/filterable voter table, per-beat heat strip, complaint
/// bars). The .htm opens straight off disk (file://) — the data is inlined, so
/// there's no fetch/CORS dance — while the .json stays available for tooling.
///
/// <para>Scope is THE RUN, not the content hash: a report is built from the exact
/// ballots a run produced (passed in by <see cref="NodeReviewService"/>), so
/// re-runs at the same node version don't pool into one another.</para>
///
/// <para>Files land in the SAME per-node publish folder as the manuscript exports —
/// <c>&lt;PublishExportDirectory&gt;/&lt;Series…&gt;/&lt;Title&gt;/</c> (Desktop fallback) — named by
/// title, beside the node's <c>.docx/.pdf/.epub</c>.</para>
/// </summary>
public sealed class ReviewReportExporter
{
    private readonly SettingsService settings;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public ReviewReportExporter(SettingsService settings, IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.settings = settings;
        this.dbFactory = dbFactory;
    }

    /// <summary>The node's own publish folder: publish-root + its series/book ancestry
    /// (top-down) + its title — byte-for-byte the layout ManuscriptExportService uses, so
    /// the report sits in the same folder as the node's .docx/.pdf/.epub. The root is
    /// resolved per-universe (same as the exporters) so the report can't land in a
    /// different universe than the manuscript it accompanies.</summary>
    private async Task<string> NodePublishDirAsync(Guid nodeId, string title, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct);
        if (node is null) return Path.Combine(settings.GetExportDirectory(null), ExportPathResolver.SanitizeTitle(title));
        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        var (nodeDir, _) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct);
        return nodeDir;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Everything one report needs. <paramref name="Reviews"/> is the run's
    /// own ballots; the rest is the run's headline + the BRAIN that produced it.</summary>
    public sealed record ReportInput(
        Guid NodeId, string Slug, string Title, string ContentHash, int BeatCount,
        string Brain, string Model, double Mean, double Sd, double Ci95, double FlowMean,
        int Clusters, IReadOnlyList<NodeReview> Reviews);

    /// <summary>Build + write both files. Returns their absolute paths.
    /// Returns (null,null) if there are no reviews to report.</summary>
    public async Task<(string? JsonPath, string? HtmPath)> ExportAsync(ReportInput input, CancellationToken ct = default)
    {
        if (input.Reviews.Count == 0) return (null, null);

        var beatMeta = await FetchBeatMetaAsync(input.NodeId, input.BeatCount, ct);
        var json = BuildJson(input, beatMeta);

        var dir = await NodePublishDirAsync(input.NodeId, input.Title, ct);
        Directory.CreateDirectory(dir);
        // Named by title, brain-suffixed so a cloud run and a local run don't overwrite each
        // other; re-running the SAME brain overwrites (one current report per brain, like the
        // manuscripts keep one current version).
        var stem = $"{ExportPathResolver.SanitizeTitle(input.Title)} reviews ({input.Brain})";
        var jsonPath = Path.Combine(dir, stem + ".json");
        var htmPath = Path.Combine(dir, stem + ".htm");

        await File.WriteAllTextAsync(jsonPath, json, ct);
        await File.WriteAllTextAsync(htmPath, BuildHtm(input.Title, json), ct);
        return (jsonPath, htmPath);
    }

    // ── Beat metadata ─────────────────────────────────────────────────────────

    private sealed record BeatMeta(int Number, string? Title);

    /// <summary>Fetches the global beat number + optional title for every enabled beat
    /// in the node, keyed by 1-based positional index (the same index NodeMarkdownExporter
    /// uses when generating review prompts). Returns an empty dict on any error so the
    /// report still exports without beat titles.</summary>
    private async Task<Dictionary<int, BeatMeta>> FetchBeatMetaAsync(
        Guid nodeId, int beatCount, CancellationToken ct)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rows = await (
                from bn in db.BeatNodes
                join b in db.Beats on bn.BeatId equals b.Id
                where bn.NodeId == nodeId && true
                orderby bn.SortKey
                select new { b.Number, b.Title }
            ).ToListAsync(ct);

            var result = new Dictionary<int, BeatMeta>();
            for (int i = 0; i < rows.Count && i < beatCount; i++)
                result[i + 1] = new BeatMeta(rows[i].Number, rows[i].Title);
            return result;
        }
        catch { return new Dictionary<int, BeatMeta>(); }
    }

    // ── JSON model ────────────────────────────────────────────────────────────

    private static string BuildJson(ReportInput input, Dictionary<int, BeatMeta> beatMeta)
    {
        var reviews = input.Reviews;
        int beatCount = input.BeatCount;

        // Per-beat aggregate across THIS run's ballots (positional 1..beatCount).
        var beatVals = new List<int>[beatCount + 1];
        for (int p = 1; p <= beatCount; p++) beatVals[p] = new List<int>();
        foreach (var r in reviews)
            foreach (var bs in r.BeatScores)
                if (bs.BeatNumber >= 1 && bs.BeatNumber <= beatCount)
                    beatVals[bs.BeatNumber].Add(bs.Score);

        var beats = new List<object>();
        for (int p = 1; p <= beatCount; p++)
        {
            var v = beatVals[p];
            if (v.Count == 0) continue;
            // Contested = clusters disagree by >=1.2 on this beat.
            var byCluster = reviews
                .Where(r => r.ClusterId.HasValue && r.BeatScores.Any(b => b.BeatNumber == p))
                .GroupBy(r => r.ClusterId!.Value)
                .Select(g => g.Average(r => (double)r.BeatScores.First(b => b.BeatNumber == p).Score))
                .ToList();
            bool contested = byCluster.Count >= 2 && (byCluster.Max() - byCluster.Min()) >= 1.2;
            beatMeta.TryGetValue(p, out var meta);
            beats.Add(new
            {
                n = p,
                num = meta?.Number,       // global Beat.Number shown in the UI
                title = meta?.Title,      // optional short beat label
                mean = Math.Round(v.Average(), 2),
                min = v.Min(),
                max = v.Max(),
                count = v.Count,
                contested,
            });
        }

        // Complaint histogram: each ballot's single weakness gripe is one line; cluster
        // identical (case-insensitive) lines so recurring problems rise to the top.
        var complaints = reviews
            .Where(r => !string.IsNullOrWhiteSpace(r.Improvements))
            .SelectMany(r => r.Improvements!.Split('\n'))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0 && s != "-" && !s.Equals("none", StringComparison.OrdinalIgnoreCase))
            .GroupBy(s => s.ToLowerInvariant())
            .Select(g => new { text = g.First(), count = g.Count() })
            .OrderByDescending(c => c.count).ThenBy(c => c.text)
            .ToList();

        var clusters = reviews
            .Where(r => r.ClusterId.HasValue)
            .GroupBy(r => r.ClusterId!.Value)
            .Select(g => new
            {
                id = g.Key,
                label = g.Select(r => r.ClusterLabel).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? $"Cluster {g.Key}",
                size = g.Count(),
            })
            .OrderBy(c => c.id)
            .ToList();

        var voters = reviews
            .OrderByDescending(r => r.Score)
            .Select(r => new
            {
                persona = r.PersonaName,
                blurb = r.PersonaBlurb,
                provider = r.ProviderId,
                model = r.Model,
                isLocal = string.Equals(r.ProviderId, "local", StringComparison.OrdinalIgnoreCase),
                score = r.Score,
                flow = r.FlowScore,
                clusterId = r.ClusterId,
                clusterLabel = r.ClusterLabel,
                weakness = string.IsNullOrWhiteSpace(r.Improvements) ? null : r.Improvements!.Trim(),
                review = string.IsNullOrWhiteSpace(r.ReviewText) ? null : r.ReviewText.Trim(),
                beatScores = r.BeatScores
                    .OrderBy(b => b.BeatNumber)
                    .ToDictionary(b => b.BeatNumber.ToString(), b => b.Score),
            })
            .ToList();

        var scores = reviews.Select(r => (double)r.Score).ToList();
        var doc = new
        {
            node = new
            {
                id = input.NodeId,
                slug = input.Slug,
                title = input.Title,
                contentHash = input.ContentHash,
                beatCount = input.BeatCount,
            },
            run = new
            {
                brain = input.Brain,                       // "local" | "cloud"
                model = input.Model,
                ballots = reviews.Count,
                withProse = reviews.Count(r => !string.IsNullOrWhiteSpace(r.ReviewText)),
                mean = Math.Round(input.Mean, 1),
                sd = Math.Round(input.Sd, 1),
                ci95 = Math.Round(input.Ci95, 2),
                flowMean = Math.Round(input.FlowMean, 1),
                clusters = input.Clusters,
                min = scores.Count > 0 ? (int)scores.Min() : 0,
                max = scores.Count > 0 ? (int)scores.Max() : 0,
                reviewedAtUtc = reviews.Max(r => r.ReviewedAt),
            },
            beats,
            complaints,
            clusters,
            voters,
        };

        return JsonSerializer.Serialize(doc, JsonOpts);
    }

    // ── HTM viewer ──────────────────────────────────────────────────────────────
    // Self-contained: the JSON is inlined in a <script type="application/json"> block
    // (so file:// works with no fetch), and rendered by vanilla JS. No external deps.

    private static string BuildHtm(string title, string json)
    {
        // Guard the embedded JSON: a literal "</script>" in any review text would close
        // our script element early. Escaping the slash is invisible to JSON.parse.
        var safe = json.Replace("</", "<\\/");
        return HtmlTemplate
            .Replace("{{TITLE}}", System.Net.WebUtility.HtmlEncode(title))
            .Replace("{{DATA}}", safe);
    }

    private const string HtmlTemplate = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Reviews — {{TITLE}}</title>
<style>
  :root { --bg:#0d0f12; --panel:#15181d; --line:#262b33; --txt:#e6edf3; --muted:#8b949e;
          --good:#2ea043; --ok:#1f6feb; --warn:#d29922; --bad:#da3633; --local:#d29922; --cloud:#1f6feb; }
  * { box-sizing:border-box; }
  body { margin:0; background:var(--bg); color:var(--txt); font:14px/1.5 ui-sans-serif,system-ui,Segoe UI,Roboto,sans-serif; }
  header { padding:1rem 1.25rem; border-bottom:1px solid var(--line); position:sticky; top:0; background:var(--bg); z-index:5; }
  h1 { margin:0 0 .35rem; font-size:1.2rem; }
  .badges { display:flex; flex-wrap:wrap; gap:.5rem; align-items:center; }
  .badge { padding:.15rem .55rem; border-radius:999px; font-size:.78rem; font-weight:600; }
  .b-score { background:#1b2129; border:1px solid var(--line); font-size:1rem; }
  .b-local { background:var(--local); color:#1a1300; }
  .b-cloud { background:var(--cloud); color:#fff; }
  .muted { color:var(--muted); }
  main { padding:1rem 1.25rem; display:grid; gap:1.25rem; max-width:1600px; }
  .panel { background:var(--panel); border:1px solid var(--line); border-radius:10px; padding:1rem; }
  .panel h2 { margin:0 0 .75rem; font-size:.95rem; color:var(--muted); text-transform:uppercase; letter-spacing:.04em; }
  .controls { display:flex; flex-wrap:wrap; gap:.6rem; align-items:center; }
  input, select { background:#0d1117; color:var(--txt); border:1px solid var(--line); border-radius:6px; padding:.4rem .55rem; font-size:.85rem; }
  input[type=search] { min-width:240px; }
  label.chk { display:flex; gap:.3rem; align-items:center; color:var(--muted); font-size:.85rem; }
  table { width:100%; border-collapse:collapse; }
  th, td { text-align:left; padding:.5rem .6rem; border-bottom:1px solid var(--line); vertical-align:top; }
  th { color:var(--muted); font-size:.78rem; text-transform:uppercase; letter-spacing:.03em; cursor:pointer; user-select:none; white-space:nowrap; }
  th.sorted::after { content:" \25BE"; }
  th.asc.sorted::after { content:" \25B4"; }
  tr.voter { cursor:pointer; }
  tr.voter:hover { background:#1b2129; }
  .pill { display:inline-block; min-width:2.4rem; text-align:center; padding:.1rem .4rem; border-radius:6px; font-variant-numeric:tabular-nums; font-weight:600; }
  .s-good{background:rgba(46,160,67,.18);color:#56d364;} .s-ok{background:rgba(31,111,235,.18);color:#6cb6ff;}
  .s-warn{background:rgba(210,153,34,.18);color:#e3b341;} .s-bad{background:rgba(218,54,51,.18);color:#ff7b72;}
  .prov-local{color:var(--local);font-weight:600;} .prov-cloud{color:var(--muted);}
  .detail td { background:#0d1117; }
  .detail .rev { white-space:pre-wrap; color:#c9d1d9; margin:.25rem 0 .5rem; }
  .beatchips { display:flex; flex-wrap:wrap; gap:3px; margin-top:.35rem; }
  .chip { font-size:.62rem; min-width:1.5rem; text-align:center; padding:1px 3px; border-radius:3px; color:#0d1117; font-weight:700; }
  .heat { display:flex; flex-wrap:wrap; gap:3px; }
  .heat .cell { width:34px; height:34px; border-radius:4px; display:flex; align-items:center; justify-content:center; font-size:.62rem; color:#0d1117; font-weight:700; position:relative; }
  .heat .cell.contested { outline:2px solid #ff7b72; outline-offset:-2px; }
  .bars { display:grid; gap:.3rem; }
  .bar { display:grid; grid-template-columns:2.2rem 1fr; gap:.5rem; align-items:center; }
  .bar .n { text-align:right; color:var(--muted); font-variant-numeric:tabular-nums; }
  .bar .t { background:#0d1117; border-radius:5px; overflow:hidden; position:relative; height:1.5rem; }
  .bar .f { background:var(--bad); height:100%; }
  .bar .lbl { position:absolute; left:.5rem; top:0; line-height:1.5rem; font-size:.8rem; color:var(--txt); white-space:nowrap; }
  .count { color:var(--muted); font-size:.85rem; }
</style>
</head>
<body>
<header>
  <h1>Reader panel — {{TITLE}}</h1>
  <div class="badges" id="badges"></div>
</header>
<main>
  <section class="panel">
    <h2>Per-beat heat <span class="muted" style="text-transform:none">(mean score 1–5 · color = quality · red outline = readers split, not a bad beat · hover for detail)</span></h2>
    <div class="heat" id="heat"></div>
  </section>
  <section class="panel">
    <h2>Recurring complaints</h2>
    <div class="bars" id="complaints"></div>
  </section>
  <section class="panel">
    <h2>Voters</h2>
    <div class="controls">
      <input type="search" id="q" placeholder="search persona / weakness / review…">
      <select id="prov"></select>
      <select id="clust"></select>
      <label class="chk"><input type="checkbox" id="proseOnly"> with prose only</label>
      <span class="count" id="count"></span>
    </div>
    <table id="tbl">
      <thead><tr>
        <th data-k="persona">Persona</th>
        <th data-k="provider">Brain</th>
        <th data-k="score" class="sorted">Score</th>
        <th data-k="flow">Flow</th>
        <th data-k="clusterLabel">Cluster</th>
        <th data-k="weakness">Top gripe</th>
      </tr></thead>
      <tbody id="rows"></tbody>
    </table>
  </section>
</main>

<script id="report-data" type="application/json">{{DATA}}</script>
<script>
const DATA = JSON.parse(document.getElementById('report-data').textContent);
const $ = s => document.querySelector(s);
const sCls = (v,max=100)=>{const p=v/max*100; return p>=85?'s-good':p>=70?'s-ok':p>=55?'s-warn':'s-bad';};
const heatColor = m => { const t=Math.max(0,Math.min(1,(m-1)/4)); const r=Math.round(218+(46-218)*t), g=Math.round(54+(160-54)*t), b=Math.round(51+(67-51)*t); return `rgb(${r},${g},${b})`; };

// Badges
(()=>{const r=DATA.run, s=DATA.node; const brainCls=r.brain==='local'?'b-local':'b-cloud';
  $('#badges').innerHTML =
    `<span class="badge b-score ${sCls(r.mean)}">${r.mean}/100</span>`+
    `<span class="badge ${brainCls}">${r.brain.toUpperCase()} — ${r.model}</span>`+
    `<span class="muted">flow ${r.flowMean||'—'}/100 · SD ${r.sd} · 95% CI ±${r.ci95} · ${r.ballots} ballots (${r.withProse} prose) · ${r.clusters} clusters · ${s.beatCount} beats</span>`;
})();

// Per-beat heat strip
const beatByPos={};
DATA.beats.forEach(b=>{beatByPos[b.n]=b;});
$('#heat').innerHTML = DATA.beats.map(b=>{
  const c=heatColor(b.mean);
  const label=b.num??b.n;
  const titleLine=b.title?` — ${b.title}`:'';
  return `<div class="cell ${b.contested?'contested':''}" style="background:${c}" title="Beat ${label}${titleLine}: mean ${b.mean} (min ${b.min}, max ${b.max}, n=${b.count})${b.contested?' · readers split (clusters diverge ≥1.2)':''}">${label}</div>`;
}).join('') || '<span class="muted">No per-beat scores in this run.</span>';

// Complaint bars
(()=>{const c=DATA.complaints; const max=c.length?c[0].count:1;
  $('#complaints').innerHTML = c.length ? c.map(x=>
    `<div class="bar"><div class="n">${x.count}×</div><div class="t"><div class="f" style="width:${x.count/max*100}%"></div><div class="lbl">${esc(x.text)}</div></div></div>`
  ).join('') : '<span class="muted">No gripes recorded.</span>';
})();

// Provider + cluster filters
(()=>{const provs=[...new Set(DATA.voters.map(v=>v.provider))].sort();
  $('#prov').innerHTML = `<option value="">all brains</option>`+provs.map(p=>`<option value="${p}">${p}</option>`).join('');
  const cls=DATA.clusters; $('#clust').innerHTML = `<option value="">all clusters</option>`+cls.map(c=>`<option value="${c.id}">${esc(c.label)} (${c.size})</option>`).join('');
})();

let sortK='score', sortAsc=false;
function esc(s){return (s??'').replace(/[&<>]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;'}[c]));}

function render(){
  const q=$('#q').value.toLowerCase(), prov=$('#prov').value, clust=$('#clust').value, proseOnly=$('#proseOnly').checked;
  let rows=DATA.voters.filter(v=>{
    if(prov && v.provider!==prov) return false;
    if(clust!=='' && String(v.clusterId)!==clust) return false;
    if(proseOnly && !v.review) return false;
    if(q){const hay=`${v.persona} ${v.weakness||''} ${v.review||''} ${v.clusterLabel||''}`.toLowerCase(); if(!hay.includes(q)) return false;}
    return true;
  });
  rows.sort((a,b)=>{let x=a[sortK],y=b[sortK]; x=(x==null)?'':x; y=(y==null)?'':y;
    if(typeof x==='number'||typeof y==='number'){x=+x||0;y=+y||0;} else {x=(''+x).toLowerCase();y=(''+y).toLowerCase();}
    return (x<y?-1:x>y?1:0)*(sortAsc?1:-1);});
  $('#count').textContent = `${rows.length} of ${DATA.voters.length} voters`;
  $('#rows').innerHTML = rows.map((v,i)=>{
    const beats=Object.entries(v.beatScores||{});
    const chips=beats.map(([n,s])=>{const bm=beatByPos[+n];const label=bm?.num??n;const tl=bm?.title?` — ${bm.title}`:'';return `<span class="chip" style="background:${heatColor(s)}" title="Beat ${label}${tl}: ${s}/5">${label}</span>`;}).join('');
    const detail=`<tr class="detail" id="d${i}" style="display:none"><td colspan="6">`+
      (v.blurb?`<div class="muted" style="font-style:italic;margin-bottom:.35rem">${esc(v.blurb)}</div>`:'')+
      (v.review?`<div class="rev">${esc(v.review)}</div>`:'')+
      (v.weakness?`<div class="muted"><b>Gripe:</b> ${esc(v.weakness)}</div>`:'')+
      (chips?`<div class="beatchips">${chips}</div>`:'')+`</td></tr>`;
    const provCls=v.isLocal?'prov-local':'prov-cloud';
    return `<tr class="voter" onclick="(()=>{const d=document.getElementById('d${i}');d.style.display=d.style.display==='none'?'':'none';})()">`+
      `<td>${esc(v.persona)}</td>`+
      `<td class="${provCls}">${v.isLocal?'LOCAL':esc(v.provider)}</td>`+
      `<td><span class="pill ${sCls(v.score)}">${v.score}</span></td>`+
      `<td>${v.flow!=null?`<span class="pill ${sCls(v.flow)}">${v.flow}</span>`:'<span class="muted">—</span>'}</td>`+
      `<td class="muted">${esc(v.clusterLabel||'')}</td>`+
      `<td>${esc((v.weakness||'').split('\n')[0])}</td></tr>`+detail;
  }).join('');
}
document.querySelectorAll('th[data-k]').forEach(th=>th.onclick=()=>{
  const k=th.dataset.k; if(sortK===k) sortAsc=!sortAsc; else {sortK=k; sortAsc=false;}
  document.querySelectorAll('th').forEach(t=>t.classList.remove('sorted','asc'));
  th.classList.add('sorted'); if(sortAsc) th.classList.add('asc'); render();
});
['#q','#prov','#clust','#proseOnly'].forEach(s=>$(s).addEventListener('input',render));
render();
</script>
</body>
</html>
""";
}
