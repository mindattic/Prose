// RA-T2.4.2.4 end-to-end: picking a whole folder in the Import tab previews one chapter per manuscript
// file (non-manuscript files skipped).
//   node tools/readalong/e2e/import-folder.js [hubUrl] <folder> <expectedChapters>
const { chromium } = require('playwright');
const [hub = 'http://localhost:5900', folder, expected] = process.argv.slice(2);
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/story?tab=import`);
  await page.waitForSelector('#import-folder', { state: 'attached' });
  await page.waitForTimeout(1500);
  await page.setInputFiles('#import-folder', folder);
  await page.waitForSelector('.import-preview', { timeout: 15000 });
  const items = await page.$$eval('.import-preview li:not(.vol)', els => els.map(e => e.innerText.split('·')[0].trim()));
  if (items.length !== +expected) throw new Error(`expected ${expected} chapters, got ${items.length}: ${items.join(' | ')}`);
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log('ok: folder previewed as', items.join(' | '));
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
