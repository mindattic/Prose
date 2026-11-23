// RA-T4.2.1.3: WCAG contrast of every visible text element in the Story workspace, in both themes.
// Fails on anything under 4.5:1 (3:1 for large text: >= 24px, or >= 18.66px bold).
//   node tools/readalong/e2e/contrast.js [hubUrl] [bookSlug]
const { chromium } = require('playwright');
const [hub = 'http://localhost:5900', book] = process.argv.slice(2);
const TABS = ['manuscript', 'codex', 'continuity', 'ask', 'atlas', 'brainstorm', 'import'];
(async () => {
  const browser = await chromium.launch();
  const failures = [];
  for (const theme of ['dark', 'light']) {
    const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
    await ctx.addInitScript(t => { try { localStorage.setItem('prose.writer.theme', t); } catch (e) { } }, theme);
    const page = await ctx.newPage();
    for (const tab of TABS) {
      await page.goto(`${hub}/story?book=${book}&tab=${tab}`);
      await page.waitForFunction(() => window.Blazor, null, { timeout: 20000 });
      await page.waitForTimeout(1500);
      const bad = await page.evaluate(() => {
        const parse = c => { const m = c.match(/rgba?\(([^)]+)\)/); if (!m) return null; const p = m[1].split(',').map(x => parseFloat(x)); return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 }; };
        const lum = ({ r, g, b }) => { const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }; return 0.2126 * f(r) + 0.7152 * f(g) + 0.0722 * f(b); };
        const blend = (top, under) => ({ r: top.r * top.a + under.r * (1 - top.a), g: top.g * top.a + under.g * (1 - top.a), b: top.b * top.a + under.b * (1 - top.a), a: 1 });
        const bgOf = el => {
          const layers = [];
          for (let n = el; n; n = n.parentElement) { const c = parse(getComputedStyle(n).backgroundColor); if (c && c.a > 0) { layers.push(c); if (c.a >= 1) break; } }
          let bg = { r: 255, g: 255, b: 255, a: 1 };
          for (let i = layers.length - 1; i >= 0; i--) bg = blend(layers[i], bg);
          return bg;
        };
        const out = [];
        const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
        const seen = new Set();
        for (let t = walker.nextNode(); t; t = walker.nextNode()) {
          const el = t.parentElement;
          if (!el || seen.has(el) || !t.data.trim()) continue;
          seen.add(el);
          const cs = getComputedStyle(el);
          const rect = el.getBoundingClientRect();
          if (cs.visibility === 'hidden' || cs.display === 'none' || rect.width === 0 || parseFloat(cs.opacity) < 0.5) continue;
          if (el.closest('[hidden], option, datalist, script, style, .visually-hidden, .ms-end, .conflict-card.ruled, .legend-toggle.off, .bs-filter.off')) continue;
          const fg = parse(cs.color); if (!fg) continue;
          const bg = bgOf(el);
          const c = blend(fg, bg);
          const L1 = lum(c), L2 = lum(bg);
          const ratio = (Math.max(L1, L2) + 0.05) / (Math.min(L1, L2) + 0.05);
          const size = parseFloat(cs.fontSize), bold = parseInt(cs.fontWeight) >= 700;
          const need = size >= 24 || (bold && size >= 18.66) ? 3 : 4.5;
          if (ratio < need) out.push(`${ratio.toFixed(2)} < ${need} "${t.data.trim().slice(0, 40)}" (${el.tagName.toLowerCase()}.${[...el.classList].join('.')}; ${cs.color} on rgb(${Math.round(bg.r)},${Math.round(bg.g)},${Math.round(bg.b)}))`);
        }
        return out;
      });
      bad.forEach(b => failures.push(`[${theme}/${tab}] ${b}`));
    }
    await ctx.close();
  }
  await browser.close();
  const unique = [...new Set(failures)];
  if (unique.length) { console.error(unique.join('\n')); console.error(`FAIL ${unique.length} contrast failure(s)`); process.exit(1); }
  console.log('ok: every visible text element meets WCAG AA in both themes');
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
