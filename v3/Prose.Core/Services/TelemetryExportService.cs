using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prose.Core.Services;

/// <summary>
/// Renders a <see cref="ContextTelemetryService.Run"/> into three artifacts for every instrumented
/// refactor/export:
///   • <c>.json</c> — the machine-readable record + computed summary. This is the self-feedback
///     format: re-readable to learn whether the Doc Context Stack is actually improving output
///     (score/flow deltas correlated with what loaded).
///   • <c>.log</c>  — a once-per-second timeline of what was loaded during the run.
///   • <c>.html</c> — a self-contained, dependency-free interactive visualization.
/// </summary>
public sealed class TelemetryExportService
{
    private static readonly JsonSerializerOptions JsonOut = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public sealed record ExportPaths(string Json, string Log, string Html);

    public ExportPaths Export(ContextTelemetryService.Run run, string outDir, string stem)
    {
        Directory.CreateDirectory(outDir);
        var summary = BuildSummary(run);

        var jsonPath = Path.Combine(outDir, stem + ".json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(summary, JsonOut), new UTF8Encoding(false));

        var logPath = Path.Combine(outDir, stem + ".log");
        File.WriteAllText(logPath, BuildLog(run, summary), new UTF8Encoding(false));

        var htmlPath = Path.Combine(outDir, stem + ".html");
        File.WriteAllText(htmlPath, BuildHtml(summary), new UTF8Encoding(false));

        return new ExportPaths(jsonPath, logPath, htmlPath);
    }

    // ── summary DTO (this is the JSON shape the feedback loop reads) ───────────

    private sealed record DocLoadDto(string path, string tier, string reason, double score, int chars);
    private sealed record EntityLoadDto(string name, string type, string matchSource, double score, int depth);
    private sealed record BeatDto(int beatIndex, string beatId, string beatTitle, double offsetSec,
        double durationMs, int proseChars, List<DocLoadDto> docs, List<EntityLoadDto> entities);
    private sealed record TickDto(int second, int beatIndex, string beatTitle, int docsLoaded, int entitiesLoaded);
    private sealed record FreqDto(string path, string tier, int count);
    private sealed record SummaryDto(
        string runId, string nodeSlug, string label, bool docContextEnabled,
        string startedAt, string endedAt, double durationSec,
        double baselineScore, double finalScore, double scoreDelta,
        double baselineFlow, double finalFlow, double flowDelta,
        int beatCount, double avgDocsPerBeat, double avgEntitiesPerBeat,
        Dictionary<string, int> tierDistribution,
        List<FreqDto> docFrequency, List<FreqDto> entityFrequency,
        List<BeatDto> beats, List<TickDto> timeline);

    private static SummaryDto BuildSummary(ContextTelemetryService.Run run)
    {
        var start = run.StartedAt;
        var end = run.EndedAt ?? (run.Beats.Count > 0
            ? run.Beats[^1].StartedAt.AddMilliseconds(run.Beats[^1].DurationMs)
            : start);

        var beats = run.Beats.Select(b => new BeatDto(
            b.BeatIndex, b.BeatId, b.BeatTitle,
            Math.Round((b.StartedAt - start).TotalSeconds, 1),
            Math.Round(b.DurationMs, 0), b.ProseChars,
            b.Docs.Select(d => new DocLoadDto(d.Path, d.Tier, d.Reason, Math.Round(d.Score, 3), d.Chars)).ToList(),
            b.Entities.Select(e => new EntityLoadDto(e.Name, e.Type, e.MatchSource, Math.Round(e.Score, 3), e.Depth)).ToList()
        )).ToList();

        var tierDist = new Dictionary<string, int>();
        foreach (var b in run.Beats)
            foreach (var d in b.Docs)
                tierDist[d.Tier] = tierDist.GetValueOrDefault(d.Tier) + 1;

        var docFreq = run.Beats.SelectMany(b => b.Docs)
            .GroupBy(d => d.Path)
            .Select(g => new FreqDto(g.Key, g.First().Tier, g.Count()))
            .OrderByDescending(f => f.count).ToList();

        var entFreq = run.Beats.SelectMany(b => b.Entities)
            .GroupBy(e => e.Name)
            .Select(g => new FreqDto(g.Key, g.First().Type, g.Count()))
            .OrderByDescending(f => f.count).Take(40).ToList();

        // Per-second timeline: for each second of the run, which beat was being written + load counts.
        var totalSec = Math.Max(1, (int)Math.Ceiling((end - start).TotalSeconds));
        var ticks = new List<TickDto>(totalSec);
        for (int s = 0; s < totalSec; s++)
        {
            var at = start.AddSeconds(s);
            var b = run.Beats.LastOrDefault(x => x.StartedAt <= at) ?? (run.Beats.Count > 0 ? run.Beats[0] : null);
            if (b == null) { ticks.Add(new TickDto(s, -1, "", 0, 0)); continue; }
            ticks.Add(new TickDto(s, b.BeatIndex, b.BeatTitle, b.Docs.Count, b.Entities.Count));
        }

        var beatCount = run.Beats.Count;
        return new SummaryDto(
            run.RunId.ToString("N"), run.NodeSlug, run.Label, run.DocContextEnabled,
            start.ToString("u"), end.ToString("u"), Math.Round((end - start).TotalSeconds, 1),
            run.BaselineScore, run.FinalScore, Math.Round(run.FinalScore - run.BaselineScore, 2),
            run.BaselineFlow, run.FinalFlow, Math.Round(run.FinalFlow - run.BaselineFlow, 2),
            beatCount,
            beatCount == 0 ? 0 : Math.Round(run.Beats.Average(b => b.Docs.Count), 2),
            beatCount == 0 ? 0 : Math.Round(run.Beats.Average(b => b.Entities.Count), 2),
            tierDist, docFreq, entFreq, beats, ticks);
    }

    // ── .log (per-second timeline) ────────────────────────────────────────────

    private static string BuildLog(ContextTelemetryService.Run run, SummaryDto s)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Doc Context Telemetry — {s.nodeSlug} ({s.label}) ===");
        sb.AppendLine($"runId={s.runId}  DocContext={(s.docContextEnabled ? "ON" : "OFF")}");
        sb.AppendLine($"started={s.startedAt}  ended={s.endedAt}  duration={s.durationSec}s  beats={s.beatCount}");
        sb.AppendLine($"score {s.baselineScore:0.00} -> {s.finalScore:0.00}  (delta {s.scoreDelta:+0.00;-0.00;0.00})");
        sb.AppendLine($"flow  {s.baselineFlow:0.00} -> {s.finalFlow:0.00}  (delta {s.flowDelta:+0.00;-0.00;0.00})");
        sb.AppendLine($"avg docs/beat={s.avgDocsPerBeat}  avg entities/beat={s.avgEntitiesPerBeat}");
        sb.AppendLine();
        sb.AppendLine("--- per-second timeline (t+SSSs) ---");
        foreach (var t in s.timeline)
        {
            var title = t.beatTitle.Length > 50 ? t.beatTitle[..50] : t.beatTitle;
            sb.AppendLine($"t+{t.second,4}s  beat#{(t.beatIndex < 0 ? "-" : t.beatIndex.ToString()),-4} docs={t.docsLoaded,-3} ents={t.entitiesLoaded,-3} {title}");
        }
        sb.AppendLine();
        sb.AppendLine("--- per-beat detail ---");
        foreach (var b in s.beats)
        {
            sb.AppendLine($"beat#{b.beatIndex} (+{b.offsetSec}s, {b.durationMs}ms, {b.proseChars}c) {b.beatTitle}");
            foreach (var d in b.docs) sb.AppendLine($"    DOC  [{d.tier,-6}] {d.reason,-22} {d.path}");
            foreach (var e in b.entities) sb.AppendLine($"    ENT  {e.type,-12} {e.name}");
        }
        return sb.ToString();
    }

    // ── .html (self-contained interactive viz) ────────────────────────────────

    private static string BuildHtml(SummaryDto s)
    {
        var json = JsonSerializer.Serialize(s, JsonOut);
        // Embed the JSON and render with vanilla JS — no external deps, opens offline.
        return """
<!DOCTYPE html><html lang="en"><head><meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Doc Context Telemetry</title>
<style>
  :root{--bg:#0e1116;--panel:#171b22;--line:#262c36;--ink:#e6edf3;--mut:#8b949e;
        --always:#f0a868;--node:#6cb6ff;--topic:#7ee787;--ent:#d2a8ff;--up:#3fb950;--down:#f85149}
  *{box-sizing:border-box} body{margin:0;background:var(--bg);color:var(--ink);
    font:14px/1.5 ui-monospace,SFMono-Regular,Menlo,monospace}
  header{padding:20px 24px;border-bottom:1px solid var(--line)}
  h1{margin:0 0 4px;font-size:18px} .sub{color:var(--mut);font-size:12px}
  .cards{display:flex;flex-wrap:wrap;gap:12px;padding:18px 24px}
  .card{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:12px 16px;min-width:120px}
  .card .k{color:var(--mut);font-size:11px;text-transform:uppercase;letter-spacing:.5px}
  .card .v{font-size:22px;margin-top:2px} .up{color:var(--up)} .down{color:var(--down)}
  section{padding:8px 24px 24px} h2{font-size:13px;color:var(--mut);text-transform:uppercase;letter-spacing:.5px;margin:18px 0 10px}
  .legend{display:flex;gap:14px;font-size:12px;color:var(--mut);margin-bottom:8px}
  .dot{display:inline-block;width:10px;height:10px;border-radius:2px;margin-right:5px;vertical-align:middle}
  .timeline{display:flex;align-items:flex-end;gap:2px;height:160px;border-bottom:1px solid var(--line);overflow-x:auto;padding-bottom:2px}
  .bar{min-width:7px;display:flex;flex-direction:column-reverse;cursor:pointer}
  .bar:hover{outline:1px solid var(--ink)}
  .seg{width:100%}
  table{width:100%;border-collapse:collapse;font-size:12px} td,th{text-align:left;padding:6px 8px;border-bottom:1px solid var(--line);vertical-align:top}
  th{color:var(--mut);font-weight:400} .chip{display:inline-block;padding:1px 6px;border-radius:4px;margin:1px 3px 1px 0;font-size:11px}
  .freqrow{display:flex;align-items:center;gap:8px;margin:3px 0} .freqbar{height:10px;border-radius:3px}
  .tip{position:fixed;pointer-events:none;background:#000;border:1px solid var(--line);padding:8px 10px;border-radius:6px;font-size:11px;max-width:320px;display:none;z-index:9}
</style></head><body>
<header><h1 id="title"></h1><div class="sub" id="meta"></div></header>
<div class="cards" id="cards"></div>
<section>
  <h2>Per-second timeline — bar height = docs+entities loaded; segments by tier</h2>
  <div class="legend">
    <span><span class="dot" style="background:var(--always)"></span>always</span>
    <span><span class="dot" style="background:var(--node)"></span>node</span>
    <span><span class="dot" style="background:var(--topic)"></span>topic</span>
    <span><span class="dot" style="background:var(--ent)"></span>entities</span>
  </div>
  <div class="timeline" id="timeline"></div>
</section>
<section><h2>Most-loaded docs</h2><div id="docfreq"></div></section>
<section><h2>Per-beat detail</h2><table id="beats"><thead><tr><th>#</th><th>+s</th><th>ms</th><th>chars</th><th>docs</th><th>entities</th></tr></thead><tbody></tbody></table></section>
<div class="tip" id="tip"></div>
<script id="data" type="application/json">__DATA__</script>
<script>
const D=JSON.parse(document.getElementById('data').textContent);
const tierColor={always:'var(--always)',node:'var(--node)',topic:'var(--topic)'};
document.getElementById('title').textContent=`Doc Context Telemetry — ${D.nodeSlug}`;
document.getElementById('meta').textContent=`${D.label} · DocContext ${D.docContextEnabled?'ON':'OFF'} · ${D.beatCount} beats · ${D.durationSec}s · run ${D.runId.slice(0,8)}`;
const sd=D.scoreDelta, fd=D.flowDelta;
const card=(k,v,cls='')=>`<div class="card"><div class="k">${k}</div><div class="v ${cls}">${v}</div></div>`;
document.getElementById('cards').innerHTML=
  card('Score',`${D.finalScore} <span class="${sd>=0?'up':'down'}">(${sd>=0?'+':''}${sd})</span>`)+
  card('Flow',`${D.finalFlow} <span class="${fd>=0?'up':'down'}">(${fd>=0?'+':''}${fd})</span>`)+
  card('Baseline',`${D.baselineScore} / ${D.baselineFlow}`)+
  card('Avg docs/beat',D.avgDocsPerBeat)+
  card('Avg entities/beat',D.avgEntitiesPerBeat)+
  card('Duration',D.durationSec+'s');
// timeline: one bar per beat (compact), height by docs+entities, stacked tier segments
const tl=document.getElementById('timeline'); const tip=document.getElementById('tip');
const maxLoad=Math.max(1,...D.beats.map(b=>b.docs.length+b.entities.length));
D.beats.forEach(b=>{
  const total=b.docs.length+b.entities.length;
  const bar=document.createElement('div'); bar.className='bar'; bar.style.height='100%';
  const tiers={always:0,node:0,topic:0}; b.docs.forEach(d=>tiers[d.tier]=(tiers[d.tier]||0)+1);
  const parts=[['always',tiers.always],['node',tiers.node],['topic',tiers.topic],['ent',b.entities.length]];
  parts.forEach(([t,n])=>{ if(!n)return; const seg=document.createElement('div'); seg.className='seg';
    seg.style.height=(n/maxLoad*100)+'%'; seg.style.background=t==='ent'?'var(--ent)':tierColor[t]; bar.appendChild(seg);});
  bar.onmousemove=e=>{tip.style.display='block';tip.style.left=(e.clientX+12)+'px';tip.style.top=(e.clientY+12)+'px';
    tip.innerHTML=`<b>beat #${b.beatIndex}</b> +${b.offsetSec}s · ${b.proseChars}c<br>${b.beatTitle||''}<br><br>`+
      b.docs.map(d=>`<span class="chip" style="background:${tierColor[d.tier]};color:#000">${d.reason}</span>`).join('')+
      `<br>`+b.entities.slice(0,12).map(en=>`<span class="chip" style="background:var(--ent);color:#000">${en.name}</span>`).join('');};
  bar.onmouseleave=()=>tip.style.display='none';
  tl.appendChild(bar);
});
// doc frequency
const maxF=Math.max(1,...D.docFrequency.map(f=>f.count));
document.getElementById('docfreq').innerHTML=D.docFrequency.slice(0,25).map(f=>
  `<div class="freqrow"><div class="freqbar" style="width:${f.count/maxF*240}px;background:${tierColor[f.tier]||'#888'}"></div>
   <span style="color:var(--mut)">${f.count}×</span> ${f.path}</div>`).join('');
// beats table
document.querySelector('#beats tbody').innerHTML=D.beats.map(b=>
  `<tr><td>${b.beatIndex}</td><td>${b.offsetSec}</td><td>${b.durationMs}</td><td>${b.proseChars}</td>
   <td>${b.docs.map(d=>`<span class="chip" style="background:${tierColor[d.tier]};color:#000">${d.path.split('/').pop()}</span>`).join('')}</td>
   <td>${b.entities.slice(0,10).map(e=>`<span class="chip" style="background:var(--ent);color:#000">${e.name}</span>`).join('')}</td></tr>`).join('');
</script></body></html>
""".Replace("__DATA__", json.Replace("</", "<\\/"));
    }
}
