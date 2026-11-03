const { chromium } = require('playwright');
const FONT_CSS = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';

const VIET = 'Nơi hành phúc được kiến tạo bởi từng khoảnh khắc — ẩm thực tinh hoa, Sảnh tiệc đẳng cấp, Đặt tiệc cưới tại Đà Nẵng';
const CASE_LOWER = 'nơi hành phúc được kiến tạo bởi từng khoảnh khắc ẩm thực tinh hoa sảnh tiệc đẳng cấp đặt tiệc cưới tại đà nẵng';
// remove ỳ (U+1EF3) and ỷ (U+1EF7) - the two chars that fall in latin-ext U+1EF2-1EFF
const NO_EXT = CASE_LOWER.replace(/[ỳỷ]/g, 'y');

async function measure(browser, label, text) {
  const page = await browser.newPage();
  const files = [];
  page.on('response', async (r) => {
    if (r.url().includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ u: r.url(), len });
    }
  });
  await page.setContent(`<link rel="stylesheet" href="${FONT_CSS}">
    <div style="font-family:'Newsreader',serif;font-weight:600">${text}</div>
    <div style="font-family:'Source Sans 3',sans-serif;font-weight:600">${text}</div>`);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(3500);
  const total = files.reduce((a,f)=>a+f.len,0);
  console.log(`\n${label}\n  ${files.length} files, ${(total/1024).toFixed(1)} KB`);
  files.forEach(f => console.log(`    ${String((f.len/1024).toFixed(1)).padStart(7)} KB  ${f.u.split('/').pop().slice(-30)}`));
  await page.close();
  return total;
}

(async () => {
  const browser = await chromium.launch();
  const a = await measure(browser, 'A. full Vietnamese sample (has ỳ, ỷ)', VIET);
  const b = await measure(browser, 'B. same text WITHOUT ỳ and ỷ', NO_EXT);
  console.log(`\nDELTA: ${((a-b)/1024).toFixed(1)} KB saved by removing ỳ/ỷ`);
  await browser.close();
})();
