const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1200, height: 1500 }, deviceScaleFactor: 2 });
  await page.setContent(`
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Playfair+Display:wght@400;600&family=Noto+Serif+Display:wght@400;600&family=Be+Vietnam+Pro:wght@400;600&display=swap">
    <style>
      body { margin:0; padding:30px; background:#FFFDF9; color:#2C241C; }
      .lbl { font:11px monospace; color:#c00; margin-bottom:2px; }
      h2 { font-size:56px; line-height:1.15; margin:0 0 4px; letter-spacing:-0.02em; font-weight:400; }
      .mixPF { font-family:'DM Serif Display', 'Playfair Display', Georgia, serif; }
      .mixNS { font-family:'DM Serif Display', 'Noto Serif Display', Georgia, serif; }
      .purePF { font-family:'Playfair Display', Georgia, serif; }
    </style>
    <div class="lbl">MIXED DM Serif Display + Playfair Display</div>
    <h2 class="mixPF">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <h2 class="mixPF">Nơi hạnh phúc được kiến tạo</h2>
    <div class="lbl">MIXED DM Serif Display + Noto Serif Display</div>
    <h2 class="mixNS">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <h2 class="mixNS">Nơi hạnh phúc được kiến tạo</h2>
    <div class="lbl">PURE Playfair Display</div>
    <h2 class="purePF">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <h2 class="purePF">Nơi hạnh phúc được kiến tạo</h2>
  `);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);
  await page.screenshot({ path: path.join(__dirname, 'font-compare4.png'), fullPage: true });
  console.log('ok');
  await browser.close();
})();
