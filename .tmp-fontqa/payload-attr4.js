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

  console.log('--- browser CSS: family/style/weight declarations ---');
  const decls = [...css.matchAll(/font-family:\s*'([^']+)';\s*font-style:\s*(\w+);\s*font-weight:\s*(\d+);/g)]
    .map(m => `${m[1]} ${m[2]} ${m[3]}`);
  console.log([...new Set(decls)].join('\n'));

  console.log('\n--- downloaded files ---');
  const total = files.reduce((a,f)=>a+f.len,0);
  files.forEach(f => {
    const fam = (f.u.match(/\/s\/([a-z0-9]+)\//) || [])[1];
    const file = f.u.split('/').pop();
    console.log(`  ${String((f.len/1024).toFixed(1)).padStart(7)} KB  ${fam}  ${file}`);
  });
  console.log(`\nTOTAL ${(total/1024).toFixed(1)} KB  |  css ${(Buffer.byteLength(css)/1024).toFixed(1)} KB  |  files ${files.length}`);
  await browser.close();
})();
