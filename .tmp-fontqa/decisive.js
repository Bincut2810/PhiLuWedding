const { chromium } = require('playwright');
const FONT_CSS = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';

// ---------- A. Is the arrow glyph really coming from Source Sans 3? ----------
async function arrowOrigin(browser) {
  const render = async (withFont) => {
    const page = await browser.newPage();
    if (withFont) {
      await page.setContent(`<link rel="stylesheet" href="${FONT_CSS}">
        <div style="font-family:'Source Sans 3';font-weight:600;font-size:64px">&#8594;</div>`);
      await page.evaluate(() => document.fonts.ready);
      await page.waitForTimeout(3000);
    } else {
      await page.setContent(`<div style="font-family:'Source Sans 3';font-weight:600;font-size:64px">&#8594;</div>`);
    }
    const png = await page.locator('div').screenshot();
    await page.close();
    return png;
  };
  const withFont = await render(true);
  const without = await render(false);
  console.log('=== A. arrow glyph origin ===');
  console.log(`  render WITH webfont : ${withFont.length} bytes`);
  console.log(`  render WITHOUT webfont: ${without.length} bytes`);
  console.log(`  pixel-identical (=> arrow comes from the system fallback, not Source Sans 3): ${Buffer.compare(withFont, without) === 0}`);
  return Buffer.compare(withFont, without) === 0;
}

// ---------- B. Can a later @font-face steer Vietnamese chars to the small
//              vietnamese file and stop the 94 KB latin-ext download? ----------
async function latinExtFix(browser) {
  const page = await browser.newPage();
  const css = await (await fetch(FONT_CSS, { headers: { 'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36' } })).text();
  const blocks = css.split('@font-face').slice(1);
  const faces = [];
  blocks.forEach(b => {
    const body = b.split('}')[0];
    const url = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    const ur = (body.match(/unicode-range:\s*([^;]+);/) || [])[1] || '';
    const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (body.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (body.match(/font-weight:\s*(\d+)/) || [])[1];
    if (url) faces.push({ url, ur, fam, style, weight });
  });
  const vn = faces.filter(f => f.ur.includes('U+0102-0103'));
  const override = vn.map(f =>
    `@font-face{font-family:'${f.fam}';font-style:${f.style};font-weight:${f.weight};src:url(${f.url}) format('woff2');unicode-range:${f.ur};}`).join('\n');

  const measure = async (label, extra) => {
    const p = await browser.newPage();
    const files = [];
    p.on('response', async (r) => {
      if (r.url().includes('fonts.gstatic.com')) {
        let len = Number(r.headers()['content-length'] || 0);
        if (!len) { try { len = (await r.body()).length; } catch {} }
        files.push({ u: r.url(), len });
      }
    });
    await p.setContent(`<link rel="stylesheet" href="${FONT_CSS}"><style>${extra || ''}</style>
      <div style="font-family:'Source Sans 3';font-weight:600">Nhà hàng tiệc cưới đường phố Ưu Việt ạ ệ</div>
      <div style="font-family:'Newsreader';font-weight:600">Nhà hàng tiệc cưới đường phố Ưu Việt ạ ệ</div>`);
    await p.evaluate(() => document.fonts.ready);
    await p.waitForTimeout(3500);
    const total = files.reduce((a, f) => a + f.len, 0);
    console.log(`\n  ${label}: ${files.length} files, ${(total/1024).toFixed(1)} KB`);
    files.forEach(f => console.log(`      ${String((f.len/1024).toFixed(1)).padStart(7)} KB  ${f.u.split('/').pop().slice(-34)}`));
    await p.close();
    return total;
  };

  console.log('\n=== B. latin-ext suppression ===');
  const before = await measure('current', '');
  const after = await measure('with vietnamese override', override);
  console.log(`\n  DELTA: ${((before-after)/1024).toFixed(1)} KB`);
  await page.close();
  return { before, after };
}

(async () => {
  const browser = await chromium.launch();
  await arrowOrigin(browser);
  await latinExtFix(browser);
  await browser.close();
})();
