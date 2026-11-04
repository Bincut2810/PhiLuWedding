const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1000, height: 900 }, deviceScaleFactor: 2 });
  await page.setContent(`
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Playfair+Display:wght@400;600&family=Be+Vietnam+Pro:wght@400;600&display=swap">
    <style>
      body { margin:0; padding:28px; background:#FFFDF9; color:#2C241C; }
      .lbl { font:11px monospace; color:#999; margin-bottom:4px; }
      h2 { font-size:34px; line-height:1.15; margin:0 0 34px; letter-spacing:-0.01em; }
      .a { font-family:'DM Serif Display', Georgia, serif; font-weight:400; }
      .b { font-family:'Playfair Display', Georgia, serif; font-weight:400; }
      .c { font-family:'Playfair Display', Georgia, serif; font-weight:600; }
      .d { font-family:'DM Serif Display', 'Be Vietnam Pro', Georgia, serif; font-weight:400; }
    </style>
    <div class="lbl">A. DM Serif Display + Georgia fallback</div>
    <h2 class="a">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <div class="lbl">B. Playfair Display 400</div>
    <h2 class="b">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <div class="lbl">C. Playfair Display 600</div>
    <h2 class="c">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <div class="lbl">D. DM Serif Display + Be Vietnam Pro fallback</div>
    <h2 class="d">Tinh hoa ẩm thực cho ngày trọng đại</h2>
  `);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2000);
  await page.screenshot({ path: path.join(__dirname, 'font-compare.png'), fullPage: true });
  console.log('ok');
  await browser.close();
})();
