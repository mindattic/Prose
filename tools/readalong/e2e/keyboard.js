// RA-T4.2.1.1/2: reorder a chapter with Alt+↓ from the keyboard alone, then find the atlas's list view by role.
//   node tools/readalong/e2e/keyboard.js [hubUrl] [bookSlug]
const { chromium } = require('playwright');
const [hub = 'http://localhost:5900', book] = process.argv.slice(2);
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/story?book=${book}&tab=manuscript`);
  await page.waitForFunction(() => window.Blazor, null, { timeout: 20000 });
  await page.waitForTimeout(1500);
  const titles = () => page.$$eval('.ms-node.chapter .ms-title', els => els.map(e => e.innerText.trim()));
  const before = await titles();
  await page.locator('.ms-node.chapter .ms-open').first().focus();
  await page.keyboard.press('Alt+ArrowDown');
  await page.waitForFunction(b => JSON.stringify([...document.querySelectorAll('.ms-node.chapter .ms-title')].map(e => e.innerText.trim())) !== b, JSON.stringify(before), { timeout: 10000 });
  const after = await titles();
  if (after[1] !== before[0]) throw new Error(`expected "${before[0]}" second, got ${after.join(' | ')}`);
  const focused = await page.evaluate(() => document.activeElement?.querySelector?.('.ms-title')?.innerText.trim() ?? document.activeElement?.innerText);
  await page.keyboard.press('Alt+ArrowUp'); // put it back
  await page.waitForTimeout(1500);

  await page.click('.story-tab:has-text("Atlas")');
  await page.waitForTimeout(1500);
  await page.click('.atlas-list summary');
  const lists = await page.getByRole('list').count();
  if (lists < 1) throw new Error('no list view');
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log(`ok: Alt+↓ moved "${before[0]}" down (focus on "${String(focused).split('\n')[0]}"), atlas list view has ${lists} list(s)`);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
