// RA-F3.7 end-to-end on an emulated phone: tie a card to a codex entry, filter by colour, drag a card by
// its handle with touch-style pointer events, and zoom the board.
//   node tools/readalong/e2e/brainstorm-depth.js [hubUrl] [bookSlug]
const { chromium, devices } = require('playwright');
const [hub = 'http://localhost:5900', book] = process.argv.slice(2);
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ ...devices['iPhone 13'] });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/story?book=${book}&tab=brainstorm`);
  await page.waitForFunction(() => window.Blazor, null, { timeout: 20000 });
  await page.waitForTimeout(1500);
  await page.click('button.bs-new.bs-plot');
  const card = page.locator('.bs-card.bs-plot').last();
  await card.waitFor();
  const opt = await card.locator('select.bs-tie option').nth(1).getAttribute('value');
  await card.locator('select.bs-tie').selectOption(opt);
  await page.waitForSelector('.bs-card.bs-plot .bs-entity .chip');

  const id = await card.getAttribute('data-id');
  const handle = page.locator(`.bs-card[data-id="${id}"] .bs-handle`);
  const b = await handle.boundingBox();
  // Touch-style pointer drag.
  const fire = (type, x, y) => handle.dispatchEvent(type, { pointerId: 7, pointerType: 'touch', clientX: x, clientY: y, isPrimary: true, bubbles: true });
  await fire('pointerdown', b.x + 8, b.y + 6);
  await page.locator('.bs-board').dispatchEvent('pointermove', { pointerId: 7, pointerType: 'touch', clientX: b.x + 88, clientY: b.y + 66, bubbles: true });
  await page.locator('.bs-board').dispatchEvent('pointerup', { pointerId: 7, pointerType: 'touch', clientX: b.x + 88, clientY: b.y + 66, bubbles: true });
  await page.waitForTimeout(800);
  const left = await page.locator(`.bs-card[data-id="${id}"]`).evaluate(e => parseFloat(e.style.left));
  if (!(left > 50)) throw new Error('touch drag did not move the card: left=' + left);

  await page.click('.bs-filter.bs-plot');
  await page.waitForTimeout(400);
  if (await page.locator(`.bs-card[data-id="${id}"]`).count()) throw new Error('filter did not hide plot cards');
  await page.click('.bs-filter.bs-plot');

  await page.click('.bs-tools button[aria-label="Zoom in"]');
  await page.waitForFunction(() => /scale\(1\.2/.test(document.querySelector('.bs-plane').style.transform), null, { timeout: 5000 }).catch(() => {});
  const t = await page.locator('.bs-plane').evaluate(e => e.style.transform);
  if (!/scale\(1\.2/.test(t)) throw new Error('zoom not applied: ' + t);
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log(`ok (phone): tied to an entry, touch-dragged to left=${left}px, filter hides, zoom ${t}`);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
