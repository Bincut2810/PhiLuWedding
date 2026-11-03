const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto(BASE + '/', { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(3000);

  const res = await page.evaluate(() => {
    const S = 'Phi Lu Wedding';
    const out = {};
    const probe = (fam, w, style) => {
      const d = document.createElement('span');
      d.textContent = S;
      d.style.cssText = `font-family:'${fam}';font-weight:${w};font-style:${style};font-size:48px;position:absolute;white-space:nowrap`;
      document.body.appendChild(d);
      const r = d.getBoundingClientRect().width;
      d.remove();
      return +r.toFixed(2);
    };
    for (const fam of ['Newsreader', 'Source Sans 3']) {
      for (const w of [400, 500, 600, 700]) out[`${fam} ${w} normal`] = probe(fam, w, 'normal');
      out[`${fam} 400 italic`] = probe(fam, 400, 'italic');
    }
    // is the loaded face actually variable? check what document.fonts reports
    const faces = Array.from(document.fonts).map(f => ({
      fam: f.family, style: f.style, weight: f.weight, status: f.status, variation: f.variationSettings
    }));
    return { out, faces };
  });

  console.log('=== measured widths on the LIVE site (48px) ===');
  Object.entries(res.out).forEach(([k,v]) => console.log(`  ${k.padEnd(26)} ${v}`));
  console.log('\n=== document.fonts faces (unique) ===');
  const seen = new Set();
  res.faces.forEach(f => {
    const k = `${f.fam} ${f.style} ${f.weight}`;
    if (seen.has(k)) return; seen.add(k);
    console.log(`  ${k.padEnd(30)} ${f.status}`);
  });
  await browser.close();
})();
