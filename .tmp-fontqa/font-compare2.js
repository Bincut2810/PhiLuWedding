const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1000, height: 1000 }, deviceScaleFactor: 2 });
  await page.setContent(`
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Playfair+Display:wght@400;500;600&family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap">
    <style>
      body { margin:0; padding:28px; background:#FFFDF9; color:#2C241C; }
      .lbl { font:11px monospace; color:#999; margin-bottom:4px; }
      h2 { font-size:32px; line-height:1.18; margin:0 0 30px; letter-spacing:-0.008em; }
      .A { font-family:'DM Serif Display', Georgia, serif; font-weight:400; }
      .B { font-family:'DM Serif Display', 'Playfair Display', Georgia, serif; font-weight:400; }
      .C { font-family:'Playfair Display', Georgia, serif; font-weight:400; }
      .D { font-family:'DM Serif Display', 'Be Vietnam Pro', Georgia, serif; font-weight:400; }
    </style>
    <div class="lbl">A. DM Serif Display + Georgia</div>
    <h2 class="A">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">B. DM Serif Display + Playfair Display</div>
    <h2 class="B">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">C. Playfair Display only</div>
    <h2 class="C">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">D. DM Serif Display + Be Vietnam Pro</div>
    <h2 class="D">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
  `);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);
  await page.screenshot({ path: path.join(__dirname, 'font-compare2.png'), fullPage: true });
  console.log('ok');
  await browser.close();
})();
