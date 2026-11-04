const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1100, height: 1400 }, deviceScaleFactor: 2 });
  await page.setContent(`
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Playfair+Display:wght@400;500;600;700&family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap">
    <style>
      body { margin:0; padding:30px; background:#FFFDF9; color:#2C241C; }
      .lbl { font:11px monospace; color:#a00; margin-bottom:2px; }
      h2 { font-size:52px; line-height:1.16; margin:0 0 6px; letter-spacing:-0.02em; font-weight:400; }
      .mix { font-family:'DM Serif Display', 'Playfair Display', Georgia, serif; }
      .pure { font-family:'Playfair Display', Georgia, serif; }
      .p600 { font-family:'Playfair Display', Georgia, serif; font-weight:600; }
      .dmsd { font-family:'DM Serif Display', Georgia, serif; }
    </style>
    <div class="lbl">1. MIXED: DM Serif Display first, Playfair Display fallback</div>
    <h2 class="mix">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">2. PURE Playfair Display 400</div>
    <h2 class="pure">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">3. PURE Playfair Display 600</div>
    <h2 class="p600">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <div class="lbl">4. MIXED other heading</div>
    <h2 class="mix">Tinh hoa ẩm thực cho ngày trọng đại</h2>
    <div class="lbl">5. PURE Playfair 400 other heading</div>
    <h2 class="pure">Lựa chọn không gian phù hợp cho ngày trọng đại của bạn</h2>
  `);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);
  await page.screenshot({ path: path.join(__dirname, 'font-compare3.png'), fullPage: true });
  console.log('ok');
  await browser.close();
})();
