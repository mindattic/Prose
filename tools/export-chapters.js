// PROMOTION CANDIDATE (RFC 0002 / foundations doctrine): becomes `prose --export-chapters` + MCP tool.
// Interim home: tools/export-chapters.js. Usage: node tools/export-chapters.js <get_strand-dump.json>
// Re-export every chapter of bushido_coda as txt to Downloads after the sweep.
// Usage: node export_chapters.js <path-to-fresh-get_strand-dump.json>
const fs = require('fs');
const dump = process.argv[2];
const data = JSON.parse(fs.readFileSync(dump, 'utf8'));
const beats = data.beats;
const chapters = [];
let cur = null;
for (const b of beats) {
  if (b.beat_title) { cur = { title: b.beat_title, beats: [] }; chapters.push(cur); }
  if (cur) cur.beats.push(b);
}
const sanitize = s => s.replace(/[:\\/*?"<>|]/g, '').replace(/\s+/g, ' ').trim();
const dl = 'C:/Users/ryand/Downloads';
chapters.forEach((ch, i) => {
  const nn = String(i + 1).padStart(2, '0');
  const base = `Bushido Coda ${nn} ${sanitize(ch.title)}`;
  // next version number: scan existing files
  let v = 1;
  for (const f of fs.readdirSync(dl)) {
    const m = f.match(new RegExp('^' + base.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ' V(\\d+)\\.txt$'));
    if (m) v = Math.max(v, +m[1] + 1);
  }
  const text = ch.title + '\n\n' + ch.beats.map(b => b.text.replace(/\r\n/g, '\n').trim()).join('\n\n');
  fs.writeFileSync(`${dl}/${base} V${v}.txt`, text, 'utf8');
  console.log(`${base} V${v}.txt (${ch.beats.length} beats)`);
});
