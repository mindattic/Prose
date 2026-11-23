// RA-S2.2.2 end-to-end: kept story-bible names are highlighted in the Writer's editor (CSS Custom
// Highlight API, no DOM change), clicking one opens its entry beside the prose, and the beat's text is
// unchanged by the highlights.
//   node tools/readalong/e2e/writer-highlights.js [hubUrl] [bookSlug]
const { chromium } = require('playwright');
const hub = process.argv[2] || 'http://localhost:5900';
const book = process.argv[3];
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/writer?book=${book}`);
  await page.waitForSelector('article.prose[contenteditable="true"]', { timeout: 20000 });
  const before = await page.locator('article.prose').innerHTML();
  await page.waitForFunction(() => window.proseCodex && window.proseCodex.count() > 0, null, { timeout: 15000 });
  const count = await page.evaluate(() => window.proseCodex.count());
  const after = await page.locator('article.prose').innerHTML();
  if (before !== after) throw new Error('highlights changed the editor DOM');

  // Click the first highlighted range.
  const pt = await page.evaluate(() => {
    const r = [...CSS.highlights.get('prose-codex')][0].getBoundingClientRect();
    return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
  });
  await page.mouse.click(pt.x, pt.y);
  await page.waitForSelector('.writer .entry-panel h2', { timeout: 10000 });
  const name = await page.locator('.writer .entry-panel h2').innerText();
  await page.screenshot({ path: process.env.SHOT || '/tmp/writer-highlights.png' });
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log(`ok: ${count} highlighted name(s); clicking opened "${name}"; editor DOM unchanged`);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
