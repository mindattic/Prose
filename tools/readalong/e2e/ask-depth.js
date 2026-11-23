// RA-F3.6 end-to-end: "Ask about this card" opens Ask with the question filled in; the answer can be
// pinned to the brainstorm board.
//   node tools/readalong/e2e/ask-depth.js [hubUrl] [bookSlug]
const { chromium } = require('playwright');
const [hub = 'http://localhost:5900', book] = process.argv.slice(2);
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/story?book=${book}&tab=continuity`);
  await page.waitForFunction(() => window.Blazor, null, { timeout: 20000 });
  await page.waitForTimeout(1500);
  await page.locator('.conflict-card button.ask-card').first().click();
  await page.waitForSelector('.ask-input textarea');
  const q = await page.inputValue('.ask-input textarea');
  if (!/^Which is right for /.test(q)) throw new Error('not prefilled: ' + q);
  await page.click('.ask-input button.send');
  await page.waitForSelector('.msg.assistant .to-board', { timeout: 30000 });
  const before = await page.evaluate(() => 0);
  await page.locator('.msg.assistant .to-board').last().click();
  await page.waitForTimeout(800);
  await page.click('.story-tab:has-text("Brainstorm")');
  await page.waitForSelector('.bs-card', { timeout: 10000 });
  const bodies = await page.$$eval('.bs-card .bs-body', els => els.map(e => e.value));
  if (!bodies.some(b => b.length > 20)) throw new Error('answer not on the board');
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log('ok: card → prefilled question → answer → pinned to the board');
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
