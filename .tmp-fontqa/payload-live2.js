const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const seen = [];
  page.on('response', async (res) => {
    const u = res.url();
    if (u.includes('fonts.gstatic.com')) {
      let len = Number(res.headers()['content-length'] || 0);
      if (!len) { try { len = (await res.body()).length; } catch {} }
      // resolve which family each file belongs to via the CSS
      seen.push({ u, len });
    }
  });
  const cssUrls = [];
  page.on('response', async (res) => {
    if (res.url().includes('fonts.googleapis.com/css2')) {
      cssUrls.push(res.url());
      const body = await res.text();
      // map each @font-face src url -> family + style + weight
      const map = {};
      const re = /font-family:\s*'([^']+)';\s*font-style:\s*(\w+);\s*font-weight:\s*(\d+);[\s\S]*?url\((https:\/\/fonts\.gstatic\.com\/[^)]+\.woff2)\)/g;
      let m;
      while ((m = re.exec(body))) {
        map[m[4]] = { fam: m[1], style: m[2], weight: m[3] };
      }
      page.__map = map;
    }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(4000);
  const map = page.__map || {};
  let total = 0;
  const byFam = {};
  for (const s of seen) {
    total += s.len;
    const info = map[s.u];
    const key = info ? `${info.fam} ${info.style} ${info.weight}` : 'UNKNOWN';
    byFam[key] = byFam[key] || { n: 0, bytes: 0 };
    byFam[key].n++; byFam[key].bytes += s.len;
  }
  console.log('css requests:', cssUrls.length);
  cssUrls.forEach(u => console.log('  ' + u));
  console.log(`\nTOTAL ${(total/1024).toFixed(1)} KB in ${seen.length} files\n`);
  Object.entries(byFam).sort((a,b)=>b[1].bytes-a[1].bytes).forEach(([k,v]) => {
    console.log(`  ${k.padEnd(34)} ${v.n} file(s)  ${(v.bytes/1024).toFixed(1)} KB`);
  });
  await browser.close();
})();
