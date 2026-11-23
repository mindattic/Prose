// RA-F3.4 end-to-end: rename an entry inline (old name becomes an alias), then merge another entry of
// the same category into it from the "Merge into…" picker.
//   node tools/readalong/e2e/codex-editing.js [hubUrl] [bookSlug] <entryToRename> <newName> <entryToMergeAway>
const { chromium } = require('playwright');
const [hub = 'http://localhost:5900', book, entry, newName, mergeAway] = process.argv.slice(2);
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/story?book=${book}&tab=codex`);
  await page.waitForFunction(() => window.Blazor, null, { timeout: 20000 });
  await page.waitForTimeout(1500);
  await page.click(`.codex-list button:has-text("${entry}")`);
  await page.waitForSelector('.entry-panel h2');
  await page.dblclick('.entry-panel h2');
  await page.fill('.entry-rename', newName);
  await page.keyboard.press('Enter');
  await page.waitForFunction(n => document.querySelector('.entry-panel h2')?.innerText === n, newName, { timeout: 10000 });
  const aliases = await page.locator('.entry-fields dd').first().innerText();
  if (!aliases.includes(entry)) throw new Error('old name not an alias: ' + aliases);

  // Merge another entry into the renamed one.
  await page.click(`.codex-list button:has-text("${mergeAway}")`);
  await page.waitForFunction(n => document.querySelector('.entry-panel h2')?.innerText === n, mergeAway, { timeout: 10000 });
  await page.click('button:has-text("Merge into…")');
  const value = await page.locator('.entry-foot select option', { hasText: newName }).getAttribute('value');
  await page.selectOption('.entry-foot select', value);
  await page.click('.entry-foot button:has-text("Merge")');
  await page.waitForFunction(n => document.querySelector('.entry-panel h2')?.innerText === n, newName, { timeout: 10000 });
  const after = await page.locator('.entry-fields dd').first().innerText();
  if (!after.includes(mergeAway)) throw new Error('merged name not an alias: ' + after);
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log(`ok: renamed "${entry}" → "${newName}", merged "${mergeAway}" into it; aliases: ${after.replace(/\n/g, ' ')}`);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
