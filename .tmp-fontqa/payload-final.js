const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';
const CHROME_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36';

(async () => {
  // Fetch the CSS exactly as the browser sees it, via the page's own request
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

  // robust parse: split on "@font-face {" boundaries
  const blocks = css.split('@font-face').slice(1);
  const map = {};
  blocks.forEach((b, i) => {
    const body = b.split('}')[0];
    const url = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (body.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (body.match(/font-weight:\s*(\d+)/) || [])[1];
    const ur = (body.match(/unicode-range:\s*([^;]+);/) || [])[1] || '';
    if (url) map[url] = { fam, style, weight, ur, idx: i };
  });

  // subset name = comment preceding the block, found by scanning css
  const commentAt = {};
  const cre = /\/\*\s*([\w-]+)\s*\*\//g; let cm;
  while ((cm = cre.exec(css))) commentAt[cm.index] = cm[1];

  console.log('=== downloaded files ===');
  let total = 0;
  const byFace = {};
  files.forEach(f => {
    total += f.len;
    const m = map[f.u];
    if (!m) { console.log(`  UNMAPPED ${f.u}`); return; }
    const key = `${m.fam} ${m.style} ${m.weight}`;
    byFace[key] = byFace[key] || { n: 0, b: 0, subsets: [] };
    byFace[key].n++; byFace[key].b += f.len;
  });
  Object.entries(byFace).sort((a,b)=>b[1].b-a[1].b).forEach(([k,v]) =>
    console.log(`  ${(v.b/1024).toFixed(1).padStart(7)} KB  ${v.n} file(s)  ${k}`));

  // Do any two different weight blocks share one URL? (variable font detection)
  const urlToFaces = {};
  Object.entries(map).forEach(([u,m]) => { (urlToFaces[u] = urlToFaces[u] || []).push(`${m.fam} ${m.style} ${m.weight}`); });
  const shared = Object.entries(urlToFaces).filter(([u,f]) => f.length > 1);
  console.log(`\nURLs shared by multiple weight blocks: ${shared.length}`);
  shared.slice(0,5).forEach(([u,f]) => console.log('   ', f.join(' + '), '->', u.slice(-30)));

  console.log(`\nTOTAL ${(total/1024).toFixed(1)} KB | css ${(Buffer.byteLength(css)/1024).toFixed(1)} KB | ${files.length} files`);
  await browser.close();
})();
