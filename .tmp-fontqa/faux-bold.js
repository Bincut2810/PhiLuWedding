const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';
const WITH700 = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;0,700;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;0,700;1,400&display=swap';

function px(buf) { return Buffer.from(buf).toString('base64'); }

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await page.goto(BASE + '/dat-tiec', { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);

  // Render the SAME text at 600 and at 700 using the CURRENT request.
  // If Chrome synthesises (faux) bold, the 700 rendering differs from 600
  // even though the underlying variable file is identical.
  const shot = async (weight) => {
    await page.evaluate((w) => {
      let el = document.getElementById('__probe');
      if (!el) { el = document.createElement('div'); el.id = '__probe';
        el.style.cssText = 'position:fixed;left:0;top:0;z-index:9999;background:#fff;color:#000;padding:20px;font-size:32px;white-space:nowrap';
        document.body.appendChild(el); }
      el.style.fontFamily = "'Source Sans 3'";
      el.style.fontWeight = w;
      el.textContent = 'Phong bi truong Viet Nam';
    }, weight);
    return await page.locator('#__probe').screenshot();
  };
  const s600 = await shot(600);
  const s700 = await shot(700);
  console.log('=== faux-bold test (same variable file, weights 600 vs 700) ===');
  console.log(`  600 png bytes ${s600.length}`);
  console.log(`  700 png bytes ${s700.length}`);
  console.log(`  identical pixels: ${Buffer.compare(s600, s700) === 0}`);

  // Now add the 700 face and repeat
  await page.evaluate((css) => {
    const l = document.createElement('link'); l.rel='stylesheet'; l.href=css; document.head.appendChild(l);
  }, WITH700);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(3000);
  const s700b = await shot(700);
  console.log(`\n  with 700 requested:`);
  console.log(`  700 png bytes ${s700b.length}`);
  console.log(`  identical to 600 render: ${Buffer.compare(s600, s700b) === 0}`);

  // Arrow glyph: does -> come from Source Sans 3 or the fallback stack?
  const arrowTest = await page.evaluate(() => {
    const draw = (fam) => {
      const c = document.createElement('canvas'); c.width = 120; c.height = 120;
      const x = c.getContext('2d');
      x.fillStyle = '#fff'; x.fillRect(0,0,120,120);
      x.fillStyle = '#000'; x.font = '80px ' + fam;
      x.fillText('\u2192', 10, 90);
      return c.toDataURL();
    };
    return { ss3: draw("'Source Sans 3'"), seg: draw("'Segoe UI'"), arial: draw('Arial') };
  });
  console.log('\n=== arrow glyph rendering ===');
  console.log(`  Source Sans 3 == Segoe UI ? ${arrowTest.ss3 === arrowTest.seg}`);
  console.log(`  Source Sans 3 == Arial    ? ${arrowTest.ss3 === arrowTest.arial}`);
  console.log(`  Segoe UI == Arial          ? ${arrowTest.seg === arrowTest.arial}`);
  await browser.close();
})();
