const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';
const OLD = 'https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:ital,wght@0,400;0,500;0,600;1,400&family=Playfair+Display:ital,wght@0,400;0,500;0,600;1,400;1,600&display=swap';

async function run(browser, label, mode) {
  const ctx = await browser.newContext();      // isolated cache
  const page = await ctx.newPage();
  const files = [];
  let cssBytes = 0;
  if (mode === 'old') {
    await page.route('**fonts.googleapis.com/css2**', route => {
      const u = route.request().url();
      route.continue({ url: u.startsWith('https://fonts.googleapis.com/css2') ? OLD : u });
    });
  }
  page.on('response', async (res) => {
    const u = res.url();
    if (u.includes('fonts.gstatic.com')) {
      let len = Number(res.headers()['content-length'] || 0);
      if (!len) { try { len = (await res.body()).length; } catch {} }
      files.push({ u: u.split('/').pop().slice(-44), len });
    }
    if (u.includes('fonts.googleapis.com/css2')) {
      try { cssBytes = (await res.body()).length; } catch {}
    }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(4500);
  const total = files.reduce((a, f) => a + f.len, 0);
  console.log(`${label.padEnd(24)} ${String(files.length).padStart(2)} files  ${(total/1024).toFixed(1)} KB fonts  + ${(cssBytes/1024).toFixed(1)} KB css`);
  await ctx.close();
  return total;
}

(async () => {
  const browser = await chromium.launch();
  const cur = await run(browser, 'CURRENT News+SS3', 'new');
  const old = await run(browser, 'PREV Playfair+BeVN', 'old');
  console.log(`\ndelta: ${((cur - old)/1024).toFixed(1)} KB (${(((cur-old)/old)*100).toFixed(0)}%)`);
  await browser.close();
})();
