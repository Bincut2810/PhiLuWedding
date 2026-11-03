const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';
const PAGES = ['/', '/thuc-don', '/thuc-don/philu', '/tiec-cuoi/philu', '/tiec-cuoi/golden-phoenix', '/dat-tiec'];

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  for (const p of PAGES) {
    await page.goto(BASE + p, { waitUntil: 'load' });
    await page.evaluate(() => document.fonts.ready);
    await page.waitForTimeout(1500);
    const hits = await page.evaluate(() => {
      const out = [];
      const walk = el => {
        if (el.nodeType === 1) {
          for (const n of el.childNodes) {
            if (n.nodeType === 3 && /[\u2190\u2192]/.test(n.textContent)) {
              const cs = getComputedStyle(el);
              out.push({
                tag: el.tagName.toLowerCase(),
                cls: (el.className || '').toString().slice(0, 50),
                text: n.textContent.trim().slice(0, 60),
                fam: cs.fontFamily.split(',')[0].replace(/["']/g,'').trim(),
                size: cs.fontSize,
                weight: cs.fontWeight,
              });
            }
          }
        }
        for (const c of el.children) walk(c);
      };
      walk(document.body);
      return out;
    });
    if (hits.length) {
      console.log(`\n== ${p}`);
      hits.forEach(h => console.log(`  <${h.tag} class="${h.cls}"> ${h.fam} ${h.weight} ${h.size}  "${h.text}"`));
    }
  }
  await browser.close();
})();
