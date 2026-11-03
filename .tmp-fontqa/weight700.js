const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto(BASE + '/dat-tiec', { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(2500);
  const hits = await page.evaluate(() => {
    const out = [];
    const walk = (el) => {
      if (el.nodeType === 1) {
        const txt = Array.from(el.childNodes).some(n => n.nodeType === 3 && n.textContent.trim());
        if (txt) {
          const cs = getComputedStyle(el);
          const fam = cs.fontFamily.split(',')[0].replace(/["']/g,'').trim();
          if (cs.fontWeight === '700' || cs.fontWeight === 'bold') {
            out.push({
              tag: el.tagName.toLowerCase(),
              cls: el.className && el.className.toString().slice(0,60),
              fam, weight: cs.fontWeight, style: cs.fontStyle,
              text: (el.textContent||'').trim().slice(0,50),
              width: Math.round(el.getBoundingClientRect().width)
            });
          }
        }
      }
      for (const c of el.children) walk(c);
    };
    walk(document.body);
    return out;
  });
  console.log('weight 700 / bold nodes on /dat-tiec:');
  hits.forEach(h => console.log('  ', JSON.stringify(h)));

  // Does a real 700 exist for Source Sans 3 in the loaded font set?
  const faces = await page.evaluate(() =>
    Array.from(document.fonts).map(f => `${f.family} ${f.style} ${f.weight}`).filter((v,i,a)=>a.indexOf(v)===i));
  console.log('\ndeclared faces in document.fonts:', faces.join('\n  '));
  await browser.close();
})();
