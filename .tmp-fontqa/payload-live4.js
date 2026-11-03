const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';
const OLD_URL = 'https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:ital,wght@0,400;0,500;0,600;1,400&family=Playfair+Display:ital,wght@0,400;0,500;0,600;1,400;1,600&display=swap';
const CHROME_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36';

async function run(browser, label, useOld) {
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  const files = [];
  let cssBytes = 0;
  if (useOld) {
    const res = await fetch(OLD_URL, { headers: { 'User-Agent': CHROME_UA } });
    const css = await res.text();
    cssBytes = Buffer.byteLength(css);
    await page.route('**fonts.googleapis.com/css2**', route =>
      route.fulfill({ status: 200, contentType: 'text/css', body: css }));
  }
  page.on('response', async (r) => {
    const u = r.url();
    if (u.includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ u: u.split('/').pop().slice(-44), len });
    }
    if (!useOld && u.includes('fonts.googleapis.com/css2')) {
      try { cssBytes = (await r.body()).length; } catch {}
    }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(4500);
  const total = files.reduce((a, f) => a + f.len, 0);
  const loaded = await page.evaluate(() =>
    Array.from(document.fonts).map(f => `${f.family} ${f.style} ${f.weight} ${f.status}`));
  console.log(`\n${label}: ${files.length} files, ${(total/1024).toFixed(1)} KB fonts, ${(cssBytes/1024).toFixed(1)} KB css`);
  const fams = {};
  for (const f of files) { const k = f.u.slice(-30); fams[k] = (fams[k]||0)+f.len; }
  Object.entries(fams).sort((a,b)=>b[1]-a[1]).forEach(([k,v]) => console.log(`   ${String((v/1024).toFixed(1)).padStart(7)} KB  ${k}`));
  console.log('   loaded faces:', loaded.filter(l=>l.endsWith('loaded')).join(' | '));
  await ctx.close();
  return total;
}

(async () => {
  const browser = await chromium.launch();
  const cur = await run(browser, 'CURRENT Newsreader + Source Sans 3', false);
  const old = await run(browser, 'PREV Playfair Display + Be Vietnam Pro', true);
  console.log(`\nDELTA: ${((cur-old)/1024).toFixed(1)} KB (${(((cur-old)/old)*100).toFixed(0)}%)`);
  await browser.close();
})();
