const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  const files = [];
  let css = '';
  page.on('response', async (r) => {
    const u = r.url();
    if (u.includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ u, len });
    }
    if (u.includes('fonts.googleapis.com/css2')) { try { css = await r.text(); } catch {} }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(4000);

  // walk the css sequentially, tracking the most recent subset comment
  const re = /\/\*\s*([\w-]+)\s*\*\/|@font-face\s*\{([^}]*)\}/g;
  let m, subset = '?', map = {};
  while ((m = re.exec(css))) {
    if (m[1]) { subset = m[1]; continue; }
    const b = m[2];
    const fam = (b.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (b.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (b.match(/font-weight:\s*(\d+)/) || [])[1];
    const url = (b.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    if (url) map[url] = { fam, style, weight, subset };
  }

  const groups = {};
  let total = 0;
  files.forEach(f => {
    total += f.len;
    const i = map[f.u] || { fam: '?', style: '?', weight: '?', subset: '?' };
    const key = `${i.fam} ${i.style} ${i.weight} [${i.subset}]`;
    groups[key] = groups[key] || { n: 0, bytes: 0 };
    groups[key].n++; groups[key].bytes += f.len;
  });
  console.log(`TOTAL ${(total/1024).toFixed(1)} KB in ${files.length} files\n`);
  Object.entries(groups).sort((a,b)=>b[1].bytes-a[1].bytes).forEach(([k,v]) =>
    console.log(`  ${(v.bytes/1024).toFixed(1).padStart(7)} KB  ${k}`));
  await browser.close();
})();
