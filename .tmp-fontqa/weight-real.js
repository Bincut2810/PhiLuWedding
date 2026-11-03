const { chromium } = require('playwright');
const CSS_NOW = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';
const CSS_WITH_700 = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;0,700;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;0,700;1,400&display=swap';

const SAMPLE = 'Phi Lu Wedding';

async function widths(browser, label, css) {
  const page = await browser.newPage();
  const files = [];
  page.on('response', async (r) => {
    if (r.url().includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ len });
    }
  });
  await page.setContent(`<link rel="stylesheet" href="${css}">`);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(3000);
  const res = await page.evaluate((s) => {
    const out = {};
    for (const [fam, stack] of [['Newsreader','serif'], ['Source Sans 3','sans-serif']]) {
      for (const w of [400,500,600,700]) {
        const d = document.createElement('span');
        d.textContent = s;
        d.style.cssText = `font-family:'${fam}',${stack};font-weight:${w};font-size:48px;position:absolute;white-space:nowrap`;
        document.body.appendChild(d);
        out[`${fam} ${w}`] = +d.getBoundingClientRect().width.toFixed(2);
        d.remove();
      }
    }
    return out;
  }, SAMPLE);
  const total = files.reduce((a,f)=>a+f.len,0);
  console.log(`\n${label}`);
  console.log(`  fonts: ${files.length} files, ${(total/1024).toFixed(1)} KB`);
  Object.entries(res).forEach(([k,v]) => console.log(`    ${k.padEnd(22)} width ${v}`));
  await page.close();
  return res;
}

(async () => {
  const browser = await chromium.launch();
  const a = await widths(browser, 'CURRENT request (400/500/600 only)', CSS_NOW);
  const b = await widths(browser, 'WITH 700 added', CSS_WITH_700);
  console.log('\n--- width comparison (does 700 differ from 600?) ---');
  Object.keys(a).forEach(k => {
    const d = (b[k] - a[k]).toFixed(2);
    console.log(`  ${k.padEnd(22)} now ${String(a[k]).padStart(8)}  with700 ${String(b[k]).padStart(8)}  delta ${d}`);
  });
  await browser.close();
})();
