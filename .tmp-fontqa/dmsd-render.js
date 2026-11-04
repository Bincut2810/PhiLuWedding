const { chromium } = require('playwright');
const path = require('path');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 900, height: 700 } });
  await page.setContent(`
    <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Be+Vietnam+Pro:wght@400;600&display=swap">
    <style>
      body { margin:0; padding:24px; background:#FFFDF9; color:#2C241C; }
      .row { margin-bottom: 28px; }
      .lbl { font: 11px monospace; color:#888; margin-bottom:6px; }
      .dmsd { font-family:'DM Serif Display', Georgia, serif; font-size:38px; line-height:1.2; }
      .dmsd-fallback { font-family: Georgia, serif; font-size:38px; line-height:1.2; }
      .bvp { font-family:'Be Vietnam Pro', sans-serif; font-size:38px; line-height:1.2; }
      .dmsd b { font-weight:700; }
    </style>
    <div class="row"><div class="lbl">DM Serif Display (requested)</div>
      <div class="dmsd">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</div></div>
    <div class="row"><div class="lbl">Georgia only</div>
      <div class="dmsd-fallback">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</div></div>
    <div class="row"><div class="lbl">Be Vietnam Pro</div>
      <div class="bvp">Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc</div></div>
    <div class="row"><div class="lbl">DM Serif Display bold test</div>
      <div class="dmsd"><b>Tinh hoa ẩm thực cho ngày trọng đại</b></div></div>
    <div class="row"><div class="lbl">DM Serif Display italic</div>
      <div class="dmsd"><i>bằng từng khoảnh khắc</i></div></div>
  `);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(1500);
  await page.screenshot({ path: path.join(__dirname, 'dmsd-render.png'), fullPage: true });
  console.log('done');
  await browser.close();
})();
