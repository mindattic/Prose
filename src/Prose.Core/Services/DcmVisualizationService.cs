using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prose.Core.Services;

/// <summary>
/// Generates a self-contained, dependency-free HTML visualization of the Dynamic Context Memory (DCM)
/// lifecycle across the beats of a story:
///
///   Chart 1 (area) — X = beat index, Y = count of active .md files. Shows how working-set
///   size fluctuates as docs load and evict.
///
///   Chart 2 (Gantt) — One row per unique .md file, horizontal colored bars spanning each
///   active range. Gaps = evicted. Color = tier (always / node / topic). Hover tooltip shows
///   path, tier, active range, and total beats active.
///
/// No LLM calls — the CLI (<c>prose --dcm-viz</c>) feeds it per-beat snapshots from a dry-run
/// context pass, and ProseWriterRouter feeds it live via FullActiveSet when DcmLoggingEnabled.
/// </summary>
public sealed class DcmVisualizationService
{
    public sealed record BeatSnapshot(int BeatIndex, string BeatTitle, IReadOnlyList<DocEntry> ActiveDocs);
    public sealed record DocEntry(string Path, string Tier, string Reason, double Score);

    public void Generate(string slug, IReadOnlyList<BeatSnapshot> beats, string outputPath)
    {
        var html = BuildHtml(slug, beats);
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(outputPath, html, new System.Text.UTF8Encoding(false));
    }

    // ── JSON payload ─────────────────────────────────────────────────────────────
    // Public (observability plan, 2026-08-20, Phase 7): reused by the legacy .htm export
    // below AND by Prose.Hub's live/history DCM-Viz paths (ObservabilityBridge pushes a
    // freshly-rebuilt payload over SignalR after every beat; a history GET endpoint rebuilds
    // one from persisted DcmBeatSnapshots rows) - one JSON shape, one JS renderer, three
    // producers.

    public sealed class VizPayload
    {
        public string slug { get; set; } = "";
        public int totalBeats { get; set; }
        public int maxActive { get; set; }
        public List<DocRow> docs { get; set; } = new();
        public List<BeatCount> counts { get; set; } = new();
    }

    public sealed class DocRow
    {
        public string path { get; set; } = "";
        public string label { get; set; } = "";
        public string tier { get; set; } = "";
        public int first { get; set; }
        public int last { get; set; }
        public int total { get; set; }
        public List<int[]> segs { get; set; } = new();
    }

    public sealed record BeatCount(int i, string t, int n);

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Serialized <see cref="BuildPayload"/> — what both the live SignalR push and
    /// the history HTTP endpoint actually send; the JS renderer only ever sees this shape.</summary>
    public static string BuildPayloadJson(string slug, IReadOnlyList<BeatSnapshot> beats) =>
        JsonSerializer.Serialize(BuildPayload(slug, beats), JsonOpts);

    public static VizPayload BuildPayload(string slug, IReadOnlyList<BeatSnapshot> beats)
    {
        // Map every unique doc path to all beat indices where it was active.
        var docBeats = new Dictionary<string, (string Tier, List<int> Beats)>(StringComparer.Ordinal);
        foreach (var b in beats)
            foreach (var d in b.ActiveDocs)
            {
                if (!docBeats.TryGetValue(d.Path, out var entry))
                {
                    entry = (d.Tier, new List<int>());
                    docBeats[d.Path] = entry;
                }
                if (entry.Beats.Count == 0 || entry.Beats[^1] != b.BeatIndex)
                    entry.Beats.Add(b.BeatIndex);
            }

        // Build doc rows sorted: always → node → topic, then by first beat.
        var rows = new List<DocRow>();
        foreach (var (path, (tier, beatList)) in docBeats)
        {
            var sorted = beatList.OrderBy(x => x).ToList();
            var segs = MakeSegments(sorted);
            rows.Add(new DocRow
            {
                path  = path,
                label = ShortLabel(path),
                tier  = tier,
                first = sorted[0],
                last  = sorted[^1],
                total = sorted.Count,
                segs  = segs,
            });
        }
        rows.Sort((a, b) =>
        {
            var tr = TierRank(a.tier).CompareTo(TierRank(b.tier));
            return tr != 0 ? tr : a.first.CompareTo(b.first);
        });

        var counts = beats.Select(b => new BeatCount(b.BeatIndex, Clip(b.BeatTitle, 60), b.ActiveDocs.Count)).ToList();
        var maxActive = counts.Count > 0 ? counts.Max(c => c.n) : 0;

        return new VizPayload
        {
            slug       = slug,
            totalBeats = beats.Count,
            maxActive  = maxActive,
            docs       = rows,
            counts     = counts,
        };
    }

    private static List<int[]> MakeSegments(List<int> sorted)
    {
        var segs = new List<int[]>();
        if (sorted.Count == 0) return segs;
        var start = sorted[0];
        var end   = sorted[0];
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == end + 1) { end = sorted[i]; }
            else { segs.Add([start, end]); start = sorted[i]; end = sorted[i]; }
        }
        segs.Add([start, end]);
        return segs;
    }

    private static int TierRank(string t) => t switch { "always" => 0, "node" => 1, _ => 2 };

    private static string ShortLabel(string path)
    {
        var parts = path.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? string.Join("/", parts[^2..]) : path;
    }

    private static string Clip(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    // ── HTML ──────────────────────────────────────────────────────────────────────

    private static string BuildHtml(string slug, IReadOnlyList<BeatSnapshot> beats)
    {
        var payload = BuildPayload(slug, beats);
        var json    = JsonSerializer.Serialize(payload, JsonOpts)
            .Replace("</", "<\\/");

        return """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>DCM Lifecycle</title>
<style>
:root{--bg:#f8fafc;--surf:#ffffff;--line:#e2e8f0;--ink:#0f172a;--mut:#64748b;
     --always:#b45309;--node:#1d4ed8;--topic:#15803d;
     --always-tr:#b4530922;--node-tr:#1d4ed822;--topic-tr:#15803d22}
@media(prefers-color-scheme:dark){:root{
  --bg:#0e1116;--surf:#171b22;--line:#262c36;--ink:#e6edf3;--mut:#8b949e;
  --always:#f0a868;--node:#6cb6ff;--topic:#7ee787;
  --always-tr:#f0a86818;--node-tr:#6cb6ff18;--topic-tr:#7ee78718}}
:root[data-theme="light"]{--bg:#f8fafc;--surf:#ffffff;--line:#e2e8f0;--ink:#0f172a;--mut:#64748b;
  --always:#b45309;--node:#1d4ed8;--topic:#15803d}
:root[data-theme="dark"]{--bg:#0e1116;--surf:#171b22;--line:#262c36;--ink:#e6edf3;--mut:#8b949e;
  --always:#f0a868;--node:#6cb6ff;--topic:#7ee787}
*{box-sizing:border-box;margin:0;padding:0}
body{background:var(--bg);color:var(--ink);font:13px/1.5 ui-monospace,SFMono-Regular,Menlo,monospace}
header{padding:14px 20px 10px;border-bottom:1px solid var(--line);display:flex;align-items:center;gap:12px;flex-wrap:wrap}
h1{font-size:16px;font-weight:600}
.sub{color:var(--mut);font-size:12px}
.tbtn{margin-left:auto;background:var(--surf);border:1px solid var(--line);color:var(--mut);
  border-radius:6px;padding:3px 10px;cursor:pointer;font-size:11px;font-family:inherit}
.cards{display:flex;flex-wrap:wrap;gap:10px;padding:14px 20px;border-bottom:1px solid var(--line)}
.card{background:var(--surf);border:1px solid var(--line);border-radius:8px;padding:10px 14px;min-width:100px}
.card .k{color:var(--mut);font-size:10px;text-transform:uppercase;letter-spacing:.5px}
.card .v{font-size:20px;margin-top:2px;font-weight:600}
.legend{display:flex;gap:14px;padding:12px 20px 2px;font-size:12px;color:var(--mut);flex-wrap:wrap}
.dot{display:inline-block;width:10px;height:10px;border-radius:2px;margin-right:4px;vertical-align:middle}
section{padding:4px 20px 16px}
h2{font-size:11px;color:var(--mut);text-transform:uppercase;letter-spacing:.5px;margin:14px 0 6px}
.scroll{overflow-x:auto;border:1px solid var(--line);border-radius:8px;background:var(--surf)}
svg{display:block}
.tip{position:fixed;pointer-events:none;background:var(--surf);border:1px solid var(--line);
  padding:8px 10px;border-radius:6px;font-size:11px;max-width:380px;display:none;z-index:99;
  box-shadow:0 4px 16px #0003;line-height:1.6}
.tip b{color:var(--ink);display:block;margin-bottom:4px}
</style>
</head>
<body>
<header>
  <h1>DCM Lifecycle &mdash; <span id="hs"></span></h1>
  <span class="sub" id="hm"></span>
  <button class="tbtn" onclick="toggleTheme()">toggle theme</button>
</header>
<div class="cards" id="cards"></div>
<div class="legend">
  <span><span class="dot" style="background:var(--always)"></span>always</span>
  <span><span class="dot" style="background:var(--node)"></span>node</span>
  <span><span class="dot" style="background:var(--topic)"></span>topic</span>
</div>
<section>
  <h2>Active .md count per beat &mdash; area shows working-set size over time</h2>
  <div class="scroll"><svg id="csvg"></svg></div>
</section>
<section>
  <h2>DCM Gantt &mdash; each row = one .md file; bar = active range; gap = evicted</h2>
  <div class="scroll"><svg id="gsvg"></svg></div>
</section>
<div class="tip" id="tip"></div>
<script type="application/json" id="data">__DATA__</script>
<script>
'use strict';
const D=JSON.parse(document.getElementById('data').textContent);
const tip=document.getElementById('tip');
const SVG_NS='http://www.w3.org/2000/svg';

function toggleTheme(){
  const r=document.documentElement;
  const cur=r.getAttribute('data-theme')||(window.matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');
  r.setAttribute('data-theme',cur==='dark'?'light':'dark');
}

function esc(s){return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');}

function showTip(e,html){
  tip.style.display='block';tip.innerHTML=html;
  const pad=14,tw=tip.offsetWidth,th=tip.offsetHeight;
  let x=e.clientX+pad,y=e.clientY+pad;
  if(x+tw>window.innerWidth-pad)x=e.clientX-tw-pad;
  if(y+th>window.innerHeight-pad)y=e.clientY-th-pad;
  tip.style.left=x+'px';tip.style.top=y+'px';
}
function hideTip(){tip.style.display='none';}

// Header
document.getElementById('hs').textContent=D.slug;
const avg=D.counts.length?(D.counts.reduce((s,b)=>s+b.n,0)/D.counts.length).toFixed(1):'0';
document.getElementById('hm').textContent=
  D.totalBeats+' beats · '+D.docs.length+' unique .md files · peak '+D.maxActive+' active · avg '+avg+'/beat';

// Cards
[['Total beats',D.totalBeats],['Unique docs',D.docs.length],['Avg active',avg],['Peak active',D.maxActive],
 ['always',D.docs.filter(d=>d.tier==='always').length],
 ['node',D.docs.filter(d=>d.tier==='node').length],
 ['topic',D.docs.filter(d=>d.tier==='topic').length],
].forEach(([k,v])=>{
  const c=document.createElement('div');c.className='card';
  c.innerHTML='<div class="k">'+esc(k)+'</div><div class="v">'+esc(v)+'</div>';
  document.getElementById('cards').appendChild(c);
});

// Shared X geometry
const LABEL_W=220,MARG_R=20,MARG_B=28;
const BEAT_W=Math.max(4,Math.min(16,Math.floor(860/D.totalBeats)));
const CHART_W=LABEL_W+D.totalBeats*BEAT_W+MARG_R;
function bx(i){return LABEL_W+i*BEAT_W;}

// X-axis tick spacing (aim for ~10 ticks)
const TICK=Math.max(1,Math.round(D.totalBeats/10));

function svgEl(tag,attrs){
  const el=document.createElementNS(SVG_NS,tag);
  for(const[k,v]of Object.entries(attrs))el.setAttribute(k,String(v));
  return el;
}
function svgText(x,y,txt,attrs){
  const el=svgEl('text',{x,y,'font-family':'ui-monospace,SFMono-Regular,Menlo,monospace','font-size':'11',...attrs});
  el.textContent=txt;return el;
}

// ── Chart 1: Count area ───────────────────────────────────────────────────────
(function(){
  const H=80,MARG_T=16;
  const totalH=H+MARG_T+MARG_B;
  const svg=document.getElementById('csvg');
  svg.setAttribute('width',CHART_W);svg.setAttribute('height',totalH);
  svg.setAttribute('viewBox','0 0 '+CHART_W+' '+totalH);

  const mx=D.maxActive||1;
  function cy(n){return MARG_T+H-Math.round(n/mx*H);}

  // Background
  const bg=svgEl('rect',{x:LABEL_W,y:MARG_T,width:D.totalBeats*BEAT_W,height:H,fill:'var(--surf)',stroke:'var(--line)','stroke-width':'1'});
  svg.appendChild(bg);

  // Gridlines + Y labels
  [0,Math.round(mx/2),mx].forEach(v=>{
    const y=cy(v);
    const g=svgEl('line',{x1:LABEL_W,y1:y,x2:LABEL_W+D.totalBeats*BEAT_W,y2:y,stroke:'var(--line)','stroke-width':'1'});
    svg.appendChild(g);
    const t=svgText(LABEL_W-5,y+4,String(v),{'text-anchor':'end',fill:'var(--mut)'});
    svg.appendChild(t);
  });

  // Area path
  if(D.counts.length>0){
    const pts=D.counts.map(b=>[bx(b.i)+BEAT_W/2,cy(b.n)]);
    const first=pts[0],last=pts[pts.length-1];
    const areaD='M'+first[0]+','+(MARG_T+H)+' '+pts.map(([x,y])=>'L'+x+','+y).join(' ')+' L'+last[0]+','+(MARG_T+H)+' Z';
    const lineD='M'+pts.map(([x,y])=>x+','+y).join(' L');
    const clip=svgEl('clipPath',{id:'cc'});
    clip.appendChild(svgEl('rect',{x:LABEL_W,y:MARG_T,width:D.totalBeats*BEAT_W,height:H}));
    svg.appendChild(clip);
    const area=svgEl('path',{d:areaD,fill:'var(--node)',opacity:'0.2','clip-path':'url(#cc)'});
    svg.appendChild(area);
    const line=svgEl('path',{d:lineD,fill:'none',stroke:'var(--node)','stroke-width':'1.5','clip-path':'url(#cc)'});
    svg.appendChild(line);
  }

  // X axis ticks + labels
  for(let i=0;i<D.totalBeats;i+=TICK){
    const x=bx(i)+BEAT_W/2;
    svg.appendChild(svgEl('line',{x1:x,y1:MARG_T+H,x2:x,y2:MARG_T+H+4,stroke:'var(--mut)','stroke-width':'1'}));
    svg.appendChild(svgText(x,totalH-6,String(i),{'text-anchor':'middle',fill:'var(--mut)'}));
  }

  // Y axis label
  const yl=svgText(10,MARG_T+H/2+4,'active docs',{fill:'var(--mut)',transform:'rotate(-90 10 '+(MARG_T+H/2+4)+')'});
  svg.appendChild(yl);

  // Hover rects (transparent, full-height)
  D.counts.forEach(b=>{
    const r=svgEl('rect',{x:bx(b.i),y:MARG_T,width:BEAT_W,height:H,fill:'transparent',style:'cursor:crosshair'});
    r.addEventListener('mouseenter',e=>{
      showTip(e,'<b>Beat #'+b.i+'</b>'+esc(b.t?'\n'+b.t:'')+'<br>Active: <b>'+b.n+'</b> docs');
    });
    r.addEventListener('mouseleave',hideTip);
    svg.appendChild(r);
  });
  // Highlight line on hover
  const vline=svgEl('line',{x1:0,y1:MARG_T,x2:0,y2:MARG_T+H,stroke:'var(--ink)',opacity:'0.3','stroke-width':'1','pointer-events':'none',display:'none'});
  svg.appendChild(vline);
  svg.addEventListener('mousemove',e=>{
    const rect=svg.getBoundingClientRect();
    const relX=e.clientX-rect.left;
    if(relX>=LABEL_W&&relX<=LABEL_W+D.totalBeats*BEAT_W){
      vline.setAttribute('x1',relX);vline.setAttribute('x2',relX);vline.setAttribute('display','');
    }else{vline.setAttribute('display','none');}
  });
  svg.addEventListener('mouseleave',()=>{vline.setAttribute('display','none');hideTip();});
})();

// ── Chart 2: Gantt ────────────────────────────────────────────────────────────
(function(){
  const ROW_H=24,BAR_H=14,MARG_T=20;
  const nRows=D.docs.length;
  const totalH=MARG_T+nRows*ROW_H+MARG_B;
  const svg=document.getElementById('gsvg');
  svg.setAttribute('width',CHART_W);svg.setAttribute('height',totalH);
  svg.setAttribute('viewBox','0 0 '+CHART_W+' '+totalH);

  // Clip path for bars
  const clip=svgEl('clipPath',{id:'gc'});
  clip.appendChild(svgEl('rect',{x:LABEL_W,y:MARG_T,width:D.totalBeats*BEAT_W,height:nRows*ROW_H}));
  svg.appendChild(clip);

  // Beat-column gridlines (light, every TICK beats)
  for(let i=0;i<D.totalBeats;i+=TICK){
    const x=bx(i);
    svg.appendChild(svgEl('line',{x1:x,y1:MARG_T,x2:x,y2:MARG_T+nRows*ROW_H,
      stroke:'var(--line)','stroke-width':'1','stroke-dasharray':'2,3'}));
  }

  D.docs.forEach((doc,ri)=>{
    const rowY=MARG_T+ri*ROW_H;
    const barY=rowY+(ROW_H-BAR_H)/2;
    const color='var(--'+doc.tier+')';
    const isEven=ri%2===0;

    // Row stripe
    svg.appendChild(svgEl('rect',{x:LABEL_W,y:rowY,width:D.totalBeats*BEAT_W,height:ROW_H,
      fill:isEven?'var(--surf)':'var(--bg)'}));

    // Tier indicator strip
    svg.appendChild(svgEl('rect',{x:LABEL_W-4,y:rowY+5,width:3,height:ROW_H-10,rx:'1',fill:color}));

    // Label (truncated to fit column)
    const lbl=doc.label.length>26?'…'+doc.label.slice(-24):doc.label;
    const lt=svgText(LABEL_W-8,rowY+ROW_H/2+4,lbl,{'text-anchor':'end',fill:'var(--ink)'});
    svg.appendChild(lt);

    // Active bars (one rect per consecutive segment)
    doc.segs.forEach((seg,si)=>{
      const [s,e]=seg;
      const rx_=bx(s),rw=(e-s+1)*BEAT_W;
      const bar=svgEl('rect',{x:rx_,y:barY,width:rw,height:BAR_H,rx:'2',fill:color,
        style:'cursor:pointer','clip-path':'url(#gc)'});
      bar.addEventListener('mouseenter',ev=>{
        const beatRange=s===e?'beat #'+s:'beats '+s+'–'+e+' ('+((e-s+1))+' consecutive)';
        showTip(ev,'<b>'+esc(doc.path)+'</b>'
          +'tier: '+doc.tier+'<br>'
          +beatRange+'<br>'
          +'total active: '+doc.total+' / '+D.totalBeats+' beats ('+(Math.round(doc.total/D.totalBeats*100))+'%)');
      });
      bar.addEventListener('mouseleave',hideTip);
      svg.appendChild(bar);
    });
  });

  // Border rect over bars area
  svg.appendChild(svgEl('rect',{x:LABEL_W,y:MARG_T,width:D.totalBeats*BEAT_W,height:nRows*ROW_H,
    fill:'none',stroke:'var(--line)','stroke-width':'1'}));

  // X axis ticks + labels
  for(let i=0;i<D.totalBeats;i+=TICK){
    const x=bx(i)+BEAT_W/2;
    svg.appendChild(svgEl('line',{x1:x,y1:MARG_T+nRows*ROW_H,x2:x,y2:MARG_T+nRows*ROW_H+4,
      stroke:'var(--mut)','stroke-width':'1'}));
    svg.appendChild(svgText(x,totalH-6,String(i),{'text-anchor':'middle',fill:'var(--mut)'}));
  }
  // X axis label
  svg.appendChild(svgText(LABEL_W+D.totalBeats*BEAT_W/2,totalH-2,'beat index',
    {'text-anchor':'middle',fill:'var(--mut)'}));
})();
</script>
</body>
</html>
""".Replace("__DATA__", json);
    }
}
