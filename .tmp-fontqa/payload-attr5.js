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

  // index every @font-face block by the url it references
  const faces = [];
  const re = /@font-face\s*\{([^}]*)\}/g;
  let m;
  while ((m = re.exec(css))) {
    const b = m[1];
    const url = (b.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    if (!url) continue;
    const fam = (b.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (b.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (b.match(/font-weight:\s*(\d+)/) || [])[1];
    // subset comment: text right before this block
    const before = css.slice(0, m.index);
    const cm = before.match(/\/\*\s*([\w-]+)\s*\*\/\s*$/);
    faces.push({ url, fam, style, weight, subset: cm ? cm[1] : '?' });
  }
  const byUrl = Object.fromEntries(faces.map(f => [f.url, f]));

  console.log('--- browser CSS blocks ---');
  faces.forEach(f => console.log(`  ${f.fam} | ${f.style} ${f.weight} | ${f.subset}`));

  console.log('\n--- downloaded ---');
  let total = 0;
  files.forEach(f => {
    total += f.len;
    const i = byUrl[f.u];
    const label = i ? `${i.fam} ${i.style} ${i.weight} [${i.subset}]` : 'UNMATCHED';
    console.log(`  ${String((f.len/1024).toFixed(1)).padStart(7)} KB  ${label}`);
  });
  console.log(`\nTOTAL ${(total/1024).toFixed(1)} KB | css ${(Buffer.byteLength(css)/1024).toFixed(1)} KB | ${files.length} files`);
  await browser.close();
})();
