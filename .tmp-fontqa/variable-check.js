const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  let css = '';
  page.on('response', async (r) => {
    if (r.url().includes('fonts.googleapis.com/css2')) { try { css = await r.text(); } catch {} }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  await page.waitForTimeout(1500);

  const blocks = css.split('@font-face').slice(1);
  const urlFaces = {};
  blocks.forEach(b => {
    const body = b.split('}')[0];
    const url = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (body.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (body.match(/font-weight:\s*([d0-9 ]+)/) || [])[1];
    const ur = (body.match(/unicode-range:\s*([^;]+);/) || [])[1] || '';
    if (!url) return;
    (urlFaces[url] = urlFaces[url] || []).push({ fam, style, weight, ur });
  });
  console.log(`total @font-face blocks: ${blocks.length}`);
  console.log(`distinct woff2 URLs:      ${Object.keys(urlFaces).length}`);
  console.log('\nURLs used by more than one block (variable font):');
  Object.entries(urlFaces).filter(([u,f]) => f.length>1).forEach(([u,f]) => {
    console.log(`  ${u.slice(-34)}`);
    f.forEach(x => console.log(`      ${x.fam} ${x.style} ${x.weight}`));
  });
  await browser.close();
})();
