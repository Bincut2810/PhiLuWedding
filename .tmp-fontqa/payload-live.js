const { chromium } = require('playwright');

const LIVE = 'http://localhost:5124/';
const OLD = 'https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:ital,wght@0,400;0,500;0,600;1,400&family=Playfair+Display:ital,wght@0,400;0,500;0,600;1,400;1,600&display=swap';

async function measure(browser, label, swap) {
  const page = await browser.newPage();
  const files = [];
  page.on('response', async (res) => {
    const u = res.url();
    if (u.includes('fonts.gstatic.com')) {
      let len = Number(res.headers()['content-length'] || 0);
      if (!len) { try { len = (await res.body()).length; } catch {} }
      files.push({ u: u.split('/').pop().slice(-40), len });
    }
  });
  let cssBytes = 0;
  page.on('response', async (res) => {
    if (res.url().includes('fonts.googleapis.com/css2')) {
      try { cssBytes = (await res.body()).length; } catch {}
    }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  if (swap) {
    await page.evaluate((old) => {
      document.querySelectorAll('link[href*="fonts.googleapis.com/css2"]').forEach(l => {
        const n = document.createElement('link');
        n.rel = 'stylesheet'; n.href = old;
        l.parentNode.insertBefore(n, l); l.remove();
      });
    }, OLD);
    await page.evaluate(() => document.fonts.ready);
  } else {
    await page.evaluate(() => document.fonts.ready);
  }
  await page.waitForTimeout(4000);
  const total = files.reduce((a, f) => a + f.len, 0);
  const lines = files.map(f => `      ${String(f.len).padStart(6)}  ${f.u}`);
  console.log(`${label.padEnd(26)} ${String(files.length).padStart(2)} files  ${(total/1024).toFixed(1)} KB  (css ${(cssBytes/1024).toFixed(1)} KB)`);
  lines.forEach(l => console.log(l));
  await page.close();
  return total;
}

(async () => {
  const browser = await chromium.launch();
  const cur = await measure(browser, 'CURRENT Newsreader+SS3', false);
  const old = await measure(browser, 'PREV Playfair+BeVN', true);
  console.log(`\ndelta: ${((cur - old) / 1024).toFixed(1)} KB (${(((cur - old) / old) * 100).toFixed(0)}%)`);
  await browser.close();
})();
