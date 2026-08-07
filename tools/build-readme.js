#!/usr/bin/env node
// tools/build-readme.js — Converts README.md to a standalone, distributable HTML file.
// Usage:  node tools/build-readme.js
//         npm run docs

'use strict';

const fs   = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const SRC  = path.join(ROOT, 'README.md');
const OUT  = path.join(ROOT, 'docs', 'README.htm');

// ─── Dependency check ─────────────────────────────────────────────────────────
let markedLib;
try {
  markedLib = require('marked');
} catch {
  console.error('[build-readme] ERROR: `marked` is not installed.\n  Run: npm install');
  process.exit(1);
}

// marked v5+ exports { marked }; older versions export the function directly.
const parse = (typeof markedLib.parse === 'function')
  ? (md) => markedLib.parse(md)
  : (md) => markedLib(md);

// ─── Inline CSS ───────────────────────────────────────────────────────────────
const CSS = `
:root{
  --bg:#0d1117;--bg2:#161b22;--bg3:#21262d;
  --border:#30363d;
  --text:#e6edf3;--muted:#8b949e;
  --h1:#58a6ff;--h2:#79c0ff;--h3:#d2a8ff;
  --link:#58a6ff;--code:#f0f6fc;
  --sidebar-w:272px;--content-max:900px;
}
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
html{scroll-behavior:smooth}
body{
  font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',system-ui,sans-serif;
  background:var(--bg);color:var(--text);font-size:15px;line-height:1.8
}
#layout{display:flex;min-height:100vh}

/* ── Sidebar ──────────────────────────────────── */
#sidebar{
  width:var(--sidebar-w);flex-shrink:0;
  position:sticky;top:0;height:100vh;overflow-y:auto;
  background:var(--bg2);border-right:1px solid var(--border);
  padding:1.5rem 0
}
#sidebar::-webkit-scrollbar{width:4px}
#sidebar::-webkit-scrollbar-track{background:transparent}
#sidebar::-webkit-scrollbar-thumb{background:var(--border);border-radius:2px}
#toc-inner{padding:0 .75rem}
.toc-title{
  display:block;font-size:.68rem;font-weight:700;color:var(--muted);
  text-transform:uppercase;letter-spacing:.1em;padding:.1rem .5rem .9rem
}
.toc-a{
  display:block;padding:.22rem .5rem;font-size:.79rem;color:var(--muted);
  border-radius:4px;text-decoration:none;line-height:1.35;margin-bottom:1px;
  transition:color .15s,background .15s
}
.toc-a:hover{color:var(--text);background:rgba(255,255,255,.05)}
.toc-a.active{color:var(--h1);background:rgba(88,166,255,.12);font-weight:500}
.toc-a.is-h3{padding-left:1.25rem;font-size:.75rem}
.toc-sep{
  border:none;border-top:1px solid var(--border);margin:.6rem .5rem
}

/* ── Main content ─────────────────────────────── */
#content{
  flex:1;min-width:0;
  max-width:calc(var(--sidebar-w) + var(--content-max));
  padding:2.5rem 3.5rem 5rem
}

h1{color:var(--h1);font-size:2rem;font-weight:700;
   border-bottom:1px solid var(--border);padding-bottom:.5rem;margin-bottom:1.25rem}
h2{color:var(--h2);font-size:1.35rem;font-weight:600;
   margin-top:2.75rem;margin-bottom:.75rem;
   border-bottom:1px solid var(--border);padding-bottom:.3rem}
h3{color:var(--h3);font-size:1.05rem;font-weight:600;
   margin-top:1.5rem;margin-bottom:.4rem}
h4{color:var(--text);font-size:.95rem;font-weight:600;margin-top:1rem}

a{color:var(--link);text-decoration:none}
a:hover{text-decoration:underline}

p{margin:.7rem 0}

ul,ol{padding-left:1.75rem;margin:.4rem 0}
li{margin:.2rem 0}
li>ul,li>ol{margin:.1rem 0}

hr{border:none;border-top:1px solid var(--border);margin:2.25rem 0}

blockquote{
  border-left:3px solid var(--h1);background:var(--bg2);
  color:var(--muted);padding:.65rem 1rem;margin:1rem 0;
  border-radius:0 4px 4px 0;font-style:italic
}
blockquote p{margin:0}

/* ── Code ─────────────────────────────────────── */
code{
  background:var(--bg2);color:var(--code);
  padding:.15em .45em;border-radius:4px;
  font-size:.84em;
  font-family:'Cascadia Code','Fira Code','Consolas','SF Mono',monospace
}
pre{
  background:var(--bg2);border:1px solid var(--border);
  border-radius:6px;padding:1rem 1.25rem;overflow-x:auto;
  margin:1rem 0;position:relative
}
pre code{
  background:none;padding:0;font-size:.84rem;
  line-height:1.65;color:var(--code)
}

/* ── Tables ───────────────────────────────────── */
table{border-collapse:collapse;width:100%;margin:1rem 0;font-size:.875rem}
thead th{
  background:var(--bg3);color:var(--h2);font-weight:600;
  text-align:left;padding:.5rem .75rem;border:1px solid var(--border)
}
tbody td{padding:.4rem .75rem;border:1px solid var(--border);vertical-align:top}
tbody tr:hover{background:rgba(255,255,255,.025)}

/* ── Copy button ──────────────────────────────── */
.cpbtn{
  position:absolute;top:.5rem;right:.5rem;
  background:var(--bg3);color:var(--muted);
  border:1px solid var(--border);border-radius:4px;
  padding:.15rem .55rem;font-size:.71rem;font-family:inherit;
  cursor:pointer;opacity:0;transition:opacity .15s,color .15s;line-height:1.5
}
pre:hover .cpbtn{opacity:1}
.cpbtn:hover{color:var(--text)}
.cpbtn.ok{color:#7ee787}

/* ── Responsive ───────────────────────────────── */
@media(max-width:820px){
  #sidebar{display:none}
  #content{padding:1.5rem 1.25rem 3.5rem}
}
`;

// ─── Inline JS ────────────────────────────────────────────────────────────────
const JS = `
(function(){
  'use strict';
  var inner   = document.getElementById('toc-inner');
  var content = document.getElementById('content');
  if(!inner||!content) return;

  // ── Build TOC from h2 / h3 ─────────────────
  var headings = Array.from(content.querySelectorAll('h2,h3'));
  if(headings.length){
    var title = document.createElement('span');
    title.className = 'toc-title';
    title.textContent = 'Contents';
    inner.appendChild(title);

    headings.forEach(function(h){
      var a = document.createElement('a');
      a.href      = '#'+h.id;
      a.textContent = h.textContent.replace(/[#¶]/g,'').trim();
      a.className = 'toc-a'+(h.tagName==='H3'?' is-h3':'');
      if(h.tagName==='H2' && inner.children.length>1){
        // light separator before each h2 group
        var sep=document.createElement('hr');
        sep.className='toc-sep';
        inner.appendChild(sep);
      }
      inner.appendChild(a);
    });
  }

  // ── Scroll spy via IntersectionObserver ────
  var tocLinks = inner.querySelectorAll('.toc-a');
  if(tocLinks.length && 'IntersectionObserver' in window){
    var idMap={};
    tocLinks.forEach(function(l){idMap[decodeURIComponent(l.getAttribute('href').slice(1))]=l;});
    var active=null;
    var obs=new IntersectionObserver(function(entries){
      entries.forEach(function(e){
        if(e.isIntersecting){
          if(active)active.classList.remove('active');
          active=idMap[e.target.id]||null;
          if(active){
            active.classList.add('active');
            // scroll toc item into view if out of sidebar bounds
            var sidebar=document.getElementById('sidebar');
            if(sidebar){
              var ar=active.getBoundingClientRect();
              var sr=sidebar.getBoundingClientRect();
              if(ar.top<sr.top||ar.bottom>sr.bottom)
                active.scrollIntoView({block:'nearest'});
            }
          }
        }
      });
    },{rootMargin:'-8% 0px -78% 0px',threshold:0});
    headings.forEach(function(h){obs.observe(h);});
  }

  // ── Copy-to-clipboard buttons on code blocks
  content.querySelectorAll('pre').forEach(function(pre){
    var btn=document.createElement('button');
    btn.className='cpbtn';
    btn.textContent='copy';
    btn.setAttribute('aria-label','Copy code');
    btn.addEventListener('click',function(){
      var text=(pre.querySelector('code')||pre).textContent;
      if(!navigator.clipboard){return;}
      navigator.clipboard.writeText(text).then(function(){
        btn.textContent='copied!';btn.classList.add('ok');
        setTimeout(function(){btn.textContent='copy';btn.classList.remove('ok');},1800);
      }).catch(function(){});
    });
    pre.appendChild(btn);
  });
}());
`;

// ─── Build ────────────────────────────────────────────────────────────────────
if (!fs.existsSync(SRC)) {
  console.error(`[build-readme] ERROR: source not found: ${SRC}`);
  process.exit(1);
}

const markdown = fs.readFileSync(SRC, 'utf-8');

// Parse markdown → HTML
const rawHtml = parse(markdown);

// Stamp id= attributes on headings so anchor links + scroll-spy work.
// Works on headings with or without inner inline HTML.
const body = rawHtml.replace(
  /<h([1-6])>([\s\S]*?)<\/h\1>/g,
  function (_, level, inner) {
    // Strip any HTML tags to get the plain-text slug source
    const text = inner.replace(/<[^>]+>/g, '').trim();
    const id   = text
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '')   // keep letters, digits, spaces, hyphens
      .trim()
      .replace(/[\s]+/g, '-')          // spaces → hyphens
      .replace(/-+/g, '-')             // collapse multiple
      .replace(/^-|-$/g, '');          // trim leading/trailing
    return `<h${level} id="${id}">${inner}</h${level}>`;
  }
);

const now    = new Date().toISOString().slice(0, 10);
const html   = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Prose — Engineering Reference</title>
<meta name="description" content="Full engineering reference for the Prose literary fiction engine.">
<meta name="generated" content="${now}">
<style>${CSS}</style>
</head>
<body>
<div id="layout">
  <nav id="sidebar" aria-label="Table of contents">
    <div id="toc-inner"></div>
  </nav>
  <main id="content">
${body}
  </main>
</div>
<script>${JS}</script>
</body>
</html>`;

fs.writeFileSync(OUT, html, 'utf-8');
const kb = Math.round(fs.statSync(OUT).size / 1024);
console.log(`[build-readme] Written → ${path.relative(ROOT, OUT)}  (${kb} KB)`);
