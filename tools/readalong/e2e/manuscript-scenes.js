// RA-T2.3.1.2 end-to-end: add a scene to chapter 1 from the manuscript tree, drag it into chapter 2,
// and check the tree shows it under chapter 2.
//   node tools/readalong/e2e/manuscript-scenes.js [hubUrl] [bookSlug]
const { chromium } = require('playwright');
const hub = process.argv[2] || 'http://localhost:5900';
const book = process.argv[3];
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.goto(`${hub}/story?book=${book}&tab=manuscript`);
  const ch = page.locator('.ms-node.chapter');
  await ch.first().waitFor({ timeout: 20000 });
  await page.waitForFunction(() => window.Blazor && document.querySelectorAll('.ms-node').length > 0, null, { timeout: 20000 });
  await page.waitForTimeout(1500); // the prerendered tree is replaced once the circuit is interactive
  const scenesBefore = await page.locator('.ms-node.scene').count();
  await ch.nth(0).hover();
  await ch.nth(0).locator('.ms-add-scene').click();
  await page.waitForFunction(n => document.querySelectorAll('.ms-node.scene').length === n + 1, scenesBefore, { timeout: 10000 });
  await page.keyboard.press('Enter'); // commit the default title
  await page.waitForTimeout(500);
  const scene = page.locator('.ms-node.scene').last();
  const target = page.locator('.ms-node.chapter').nth(1);
  const targetTitle = (await target.locator('.ms-title').innerText()).trim();
  await scene.dragTo(target);
  await page.waitForTimeout(1500);
  // After the move the scene must sit directly after chapter 2's row (deeper than it).
  const rows = await page.$$eval('.ms-node', els => els.map(e => ({ kind: [...e.classList].find(c => ['volume','chapter','scene','sequence'].includes(c)), title: e.querySelector('.ms-title')?.innerText.trim(), depth: +getComputedStyle(e).getPropertyValue('--depth') })));
  const i = rows.findIndex(r => r.kind === 'chapter' && r.title === targetTitle);
  const under = rows.slice(i + 1).filter((r, k, a) => a.slice(0, k + 1).every(x => x.depth > rows[i].depth)).filter(r => r.kind === 'scene');
  if (!under.length) throw new Error('scene not under ' + targetTitle + ': ' + JSON.stringify(rows));
  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log(`ok: scene "${under[under.length - 1].title}" now under "${targetTitle}"`);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
