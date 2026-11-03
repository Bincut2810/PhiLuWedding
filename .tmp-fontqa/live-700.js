const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';
const CUR = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';
const WITH700 = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;0,700;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;0,700;1,400&display=swap';

async function run(browser, label, extraCss) {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  const files = [];
  page.on('response', async (r) => {
    if (r.url().includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ u: r.url(), len });
    }
  });
  await page.goto(BASE + '/dat-tiec', { waitUntil: 'load' });
  if (extraCss) {
    await page.evaluate((css) => {
      const l = document.createElement('link');
      l.rel = 'stylesheet'; l.href = css;
      document.head.appendChild(l);
    }, extraCss);
  }
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(4000);

  const total = files.reduce((a, f) => a + f.len, 0);
  const widths = await page.evaluate(() => {
    const out = {};
    const probe = (fam, w) => {
      const s = document.createElement('span');
      s.textContent = 'Phi Lu Wedding';
      s.style.cssText = `font-family:'${fam}';font-weight:${w};font-size:48px;position:absolute;white-space:nowrap`;
      document.body.appendChild(s);
      const px = s.getBoundingClientRect().width; s.remove();
      return +px.toFixed(2);
    };
    for (const f of ['Newsreader', 'Source Sans 3']) {
      for (const w of [400, 500, 600, 700]) out[`${f} ${w}`] = probe(f, w);
    }
    // how many distinct woff2 files total, and is latin-ext among them?
    return out;
  });
  const urlSet = new Set(files.map(f => f.u));
  console.log(`\n=== ${label} ===`);
  console.log(`  files ${files.length} (${urlSet.size} unique) | ${(total/1024).toFixed(1)} KB`);
  console.log('  widths:', Object.entries(widths).map(([k,v]) => `${k}=${v}`).join('  '));
  await ctx.close();
  return { total, n: urlSet.size, widths };
}

(async () => {
  const browser = await chromium.launch();
  await run(browser, 'CURRENT (400/500/600)', null);
  await run(browser, 'WITH 700 ADDED', WITH700);
  await browser.close();
})();
