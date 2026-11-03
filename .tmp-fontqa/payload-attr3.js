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

  // Simple: split on "@font-face", and find the subset comment that precedes each block
  const parts = css.split('@font-face');
  const map = {};
  for (let i = 1; i < parts.length; i++) {
    const body = parts[i];
    // subset comment sits at the end of the PREVIOUS part
    const prev = parts[i - 1];
    const cm = prev.match(/\/\*\s*([\w-]+)\s*\*\/\s*$/);
    const subset = cm ? cm[1] : '?';
    const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (body.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (body.match(/font-weight:\s*(\d+)/) || [])[1];
    const url = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    if (url) map[url] = `${fam} ${style} ${weight} [${subset}]`;
  }
  console.log(`parsed ${Object.keys(map).length} faces from CSS`);
  const groups = {};
  let total = 0;
  files.forEach(f => {
    total += f.len;
    const k = map[f.u] || 'UNMATCHED ' + f.u.slice(-24);
    groups[k] = groups[k] || { n: 0, b: 0 };
    groups[k].n++; groups[k].b += f.len;
  });
  console.log(`\nTOTAL ${(total/1024).toFixed(1)} KB in ${files.length} files\n`);
  Object.entries(groups).sort((a,b)=>b[1].b-a[1].b).forEach(([k,v]) =>
    console.log(`  ${(v.b/1024).toFixed(1).padStart(7)} KB  ${v.n} file(s)  ${k}`));
  await browser.close();
})();
