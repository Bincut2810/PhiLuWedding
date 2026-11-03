const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto(BASE + '/dat-tiec', { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);

  const r = await page.evaluate(() => {
    // 1. Is <strong> real 700, a clamped 600, or synthetic (faux) bold?
    const strong = document.querySelector('strong');
    const cs = getComputedStyle(strong);
    const strongW = strong.getBoundingClientRect().width;

    const mk = (w) => {
      const s = document.createElement('span');
      s.textContent = strong.textContent;
      s.style.cssText = `font-family:'Source Sans 3';font-weight:${w};font-size:${cs.fontSize};position:absolute;white-space:nowrap`;
      document.body.appendChild(s);
      const wpx = s.getBoundingClientRect().width;
      s.remove(); return +wpx.toFixed(3);
    };
    const w400 = mk(400), w500 = mk(500), w600 = mk(600), w700 = mk(700);

    // 2. Does the arrow glyph come from Source Sans 3 or a fallback?
    const arrow = [...document.querySelectorAll('.btn__arrow')][0];
    const arrowText = arrow ? arrow.textContent.trim() : '\u2192';
    const aCs = arrow ? getComputedStyle(arrow) : null;
    const probe = (fam) => {
      const s = document.createElement('span');
      s.textContent = arrowText;
      s.style.cssText = `font-family:${fam};font-weight:600;font-size:40px;position:absolute;white-space:nowrap`;
      document.body.appendChild(s);
      const wpx = s.getBoundingClientRect().width;
      s.remove(); return +wpx.toFixed(3);
    };
    const inSS3 = probe("'Source Sans 3'");
    const inArial = probe('Arial');
    const inSerif = probe('serif');

    return {
      strong: { tag: strong.tagName, text: strong.textContent.trim().slice(0,40),
                computedWeight: cs.fontWeight, fontFamily: cs.fontFamily.split(',')[0],
                width: +strongW.toFixed(3), w400, w500, w600, w700 },
      arrow: { text: arrowText, codePoint: 'U+' + arrowText.codePointAt(0).toString(16).toUpperCase(),
               computedFamily: aCs ? aCs.fontFamily.split(',')[0] : '?',
               widthInSS3: inSS3, widthInArial: inArial, widthInSerif: inSerif,
               ss3HasGlyph: document.fonts.check(`600 40px "Source Sans 3"`, arrowText) },
    };
  });

  console.log('=== <strong> on /dat-tiec ===');
  console.log(JSON.stringify(r.strong, null, 2));
  console.log('\n=== arrow glyph ===');
  console.log(JSON.stringify(r.arrow, null, 2));
  await browser.close();
})();
