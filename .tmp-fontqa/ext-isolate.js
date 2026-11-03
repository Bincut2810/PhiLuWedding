const { chromium } = require('playwright');
const FONT_CSS = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';

async function t(browser, label, text, extra) {
  const page = await browser.newPage();
  const files = [];
  page.on('response', async (r) => {
    if (r.url().includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ u: r.url(), len });
    }
  });
  await page.setContent(`<link rel="stylesheet" href="${FONT_CSS}"><div style="font-family:'Newsreader',serif;font-weight:600">${text}</div>`);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(3000);
  const total = files.reduce((a,f)=>a+f.len,0);
  // which subset each file is, by unicode-range presence
  console.log(`\n${label}`);
  console.log(`  ${files.length} files, ${(total/1024).toFixed(1)} KB`);
  await page.close();
  return files.length;
}

(async () => {
  const browser = await chromium.launch();
  await t(browser, 'ASCII only', 'Wedding venue Da Nang');
  await t(browser, 'Vietnamese stacked marks', 'ệ ộ ữ ặ ằ ẳ');
  await t(browser, 'Vietnamese simple (đ ư ơ)', 'đ ư ơ Đ Ư Ơ');
  await t(browser, 'Vietnamese + em dash', 'Tiệc cưới — Đà Nẵng');
  await t(browser, 'full site-ish text', 'Nơi hành phúc — Nhà hàng tiệc cưới Đà Nẵng, ẩm thực tinh hoa, đồng hồ 1.850.000 đ');
  await browser.close();
})();
