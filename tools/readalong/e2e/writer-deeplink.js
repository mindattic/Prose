// RA-T2.2.1.2 / RA-T2.2.1.3 end-to-end: /writer?book=&beat= opens that beat, and a Story-workspace
// continuity card's "open in Writer" lands on the proposed beat.
//   node tools/readalong/e2e/writer-deeplink.js [hubUrl] [bookSlug]
// Needs a running Hub with a book that has been read along and has at least one open continuity card.
const { chromium } = require('playwright');
const hub = process.argv[2] || 'http://localhost:5900';
const book = process.argv[3];
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));

  await page.goto(`${hub}/story?book=${book}&tab=continuity`);
  const link = page.locator('.conflict-card .side.proposed a.open-writer').first();
  await link.waitFor({ timeout: 20000 });
  const quote = (await page.locator('.conflict-card .side.proposed .door-text i').first().innerText()).replace(/^“|”$/g, '');
  const href = await link.getAttribute('href');
  if (!/^\/writer\?book=[0-9a-f-]{36}&beat=[0-9a-f-]{36}$/.test(href)) throw new Error('bad href ' + href);

  await page.goto(hub + href);
  await page.waitForFunction(q => {
    const ed = document.querySelector('article.prose');
    return ed && ed.innerText.includes(q.slice(0, 30));
  }, quote, { timeout: 20000 });

  if (errors.length) throw new Error('page errors: ' + errors.join('; '));
  console.log('ok: card → Writer on the proposed beat:', href);
  await browser.close();
})().catch(e => { console.error('FAIL', e.message); process.exit(1); });
