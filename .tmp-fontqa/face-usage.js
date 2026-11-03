const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';
const PAGES = [
  ['/', 'home'],
  ['/thuc-don', 'menus'],
  ['/thuc-don/combo-chuc-nha-cuoi', 'combo'],
  ['/thuc-don/philu', 'menus-philu'],
  ['/tiec-cuoi/philu', 'hall-philu'],
  ['/tiec-cuoi/golden-phoenix', 'hall-golden'],
  ['/dat-tiec', 'booking'],
];

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext();
  const page = await ctx.newPage();

  for (const [path, name] of PAGES) {
    const used = new Map();
    await page.goto(BASE + path, { waitUntil: 'load' });
    await page.evaluate(() => document.fonts.ready);
    await page.waitForTimeout(2500);
    // Which faces actually rendered text on this page?
    const faces = await page.evaluate(() => {
      const out = {};
      const walk = (el) => {
        if (el.nodeType === 1) {
          const txt = Array.from(el.childNodes).some(n => n.nodeType === 3 && n.textContent.trim());
          if (txt) {
            const cs = getComputedStyle(el);
            const fam = cs.fontFamily.split(',')[0].replace(/["']/g,'').trim();
            const key = fam + '|' + cs.fontStyle + '|' + cs.fontWeight;
            out[key] = (out[key] || 0) + 1;
          }
        }
        for (const c of el.children) walk(c);
      };
      walk(document.body);
      return out;
    });
    Object.entries(faces).forEach(([k,v]) => used.set(k, (used.get(k)||0)+v));
    console.log(`\n== ${name} (${path})`);
    [...used.entries()].sort().forEach(([k,v]) => console.log(`   ${k.padEnd(40)} ${v} node(s)`));
  }
  await browser.close();
})();
