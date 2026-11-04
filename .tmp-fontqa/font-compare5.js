const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1250, height: 1600 }, deviceScaleFactor: 2 });
  await page.setContent(`
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Playfair+Display:wght@400;500;600&family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap">
    <style>
      body { margin:0; padding:30px; background:#FFFDF9; color:#2C241C; }
      .lbl { font:11px monospace; color:#c00; margin:0 0 2px; }
      h2 { font-size:54px; line-height:1.16; margin:0 0 2px; letter-spacing:-0.02em; }
      .mix { font-family:'DM Serif Display', 'Playfair Display', Georgia, serif; font-weight:400; }
      .mix6 { font-family:'DM Serif Display', 'Playfair Display', Georgia, serif; font-weight:600; }
      .pure { font-family:'Playfair Display', Georgia, serif; font-weight:400; }
      .pure6 { font-family:'Playfair Display', Georgia, serif; font-weight:600; }
      hr { border:0; border-top:1px solid #ddd; margin:18px 0; }
    </style>
    <div class="lbl">1. MIX DMSD400</div>
    <h2 class="mix">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">2. MIX DMSD600 (faux bold check)</div>
    <h2 class="mix6">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <hr>
    <div class="lbl">3. PURE Playfair 400</div>
    <h2 class="pure">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <div class="lbl">4. PURE Playfair 600</div>
    <h2 class="pure6">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</h2>
    <hr>
    <div class="lbl">5. MIX DMSD400 (VN-heavy)</div>
    <h2 class="mix">Lựa chọn không gian phù hợp cho ngày trọng đại</h2>
    <div class="lbl">6. PURE Playfair 400 (VN-heavy)</div>
    <h2 class="pure">Lựa chọn không gian phù hợp cho ngày trọng đại</h2>
  `);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);
  await page.screenshot({ path: path.join(__dirname, 'font-compare5.png'), fullPage: true });
  console.log('ok');
  await browser.close();
})();
