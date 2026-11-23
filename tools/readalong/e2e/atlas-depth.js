// RA-F3.5 end-to-end: category toggle hides stars, search focuses a star, a dragged star keeps its place
// after a reload, and a star opened with the scrubber back shows the "as of chapter N" entry.
//   node tools/readalong/e2e/atlas-depth.js [hubUrl] [bookSlug] <starName>
const { chromium } = require('playwright');
const [hub = 'http://localhost:5900', book, star] = process.argv.slice(2);
(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  const open = async () => {
    await page.goto(`${hub}/story?book=${book}&tab=atlas`);
    await page.waitForFunction(() => window.Blazor, null, { timeout: 20000 });
    await page.waitForTimeout(2000);
  };
  const count = async () => +(await page.locator('.atlas-head .muted').first().innerText()).match(/\d+/)[0];
  await open();
  const all = await count();
  await page.click('.legend-toggle:has-text("Characters")');
  await page.waitForTimeout(500);
  const fewer = await count();
  if (!(fewer < all)) throw new Error(`toggle did not hide characters: ${all} → ${fewer}`);
  await page.click('.legend-toggle:has-text("Characters")');

  await page.fill('.atlas-search', star);
  await page.waitForTimeout(800);
  // The focused star sits in the canvas centre: click it there, after dragging it 120px right to pin it.
  const box = await page.locator('canvas.atlas-canvas').boundingBox();
  const cx = box.x + box.width / 2, cy = box.y + box.height / 2;
  await page.mouse.move(cx, cy); await page.mouse.down(); await page.mouse.move(cx + 120, cy, { steps: 6 }); await page.mouse.up();
  const pins = await page.evaluate(() => Object.entries(localStorage).filter(([k]) => k.startsWith('prose.atlas.')).map(([, v]) => v).join(''));
  if (!pins.includes('[')) throw new Error('no pinned position saved');

  await open();
  const pinsAfter = await page.evaluate(() => Object.entries(localStorage).filter(([k]) => k.startsWith('prose.atlas.')).map(([, v]) => v).join(''));
  if (pinsAfter !== pins) throw new Error('pins changed across reload');

  // Scrub to chapter 1 and open the star from the search focus.
  await page.locator('.scrubber input[type=range]').fill('1');
  await page.fill('.atlas-search', star);
  await page.waitForTimeout(800);
  const box2 = await page.locator('canvas.atlas-canvas').boundingBox();
  await page.mouse.click(box2.x + box2.width / 2, box2.y + box2.height / 2);
  await page.waitForSelector('.entry-panel .banner', { timeout: 10000 });
  const banner = await page.locator('.entry-panel .banner').innerText();
  if (!/chapter 1/.test(banner)) throw new Error('no as-of banner: ' + banner);
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log(`ok: toggle ${all}→${fewer}, search focus, pin persisted, "${banner}"`);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
