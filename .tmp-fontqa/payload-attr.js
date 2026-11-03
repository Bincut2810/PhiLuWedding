const { chromium } = require('playwright');
const LIVE = 'http://localhost:5124/';

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  const files = [];
  let css = '';
  page.on('response', async (r) => {
    const u = r.url();
    if (u.includes('fonts.gstatic.com')) {
      let len = Number(r.headers()['content-length'] || 0);
      if (!len) { try { len = (await r.body()).length; } catch {} }
      files.push({ u, len });
    }
    if (u.includes('fonts.googleapis.com/css2')) {
      try { css = await r.text(); } catch {}
    }
  });
  await page.goto(LIVE, { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(4000);

  // parse @font-face blocks properly
  const blocks = css.split('@font-face').slice(1);
  const map = {};
  blocks.forEach(b => {
    const fam = (b.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (b.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (b.match(/font-weight:\s*(\d+)/) || [])[1];
    const url = (b.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    const subset = (css.slice(0, css.indexOf(b)).match(/\/\* ([\w-]+) \*\/\s*$/) || [])[1];
    if (url) map[url] = { fam, style, weight, subset };
  });

  const groups = {};
  let total = 0;
  files.forEach(f => {
    total += f.len;
    const m = map[f.u] || { fam: '?', style: '?', weight: '?', subset: '?' };
    const key = `${m.fam} | ${m.style} ${m.weight} | ${m.subset}`;
    groups[key] = groups[key] || { n: 0, bytes: 0 };
    groups[key].n++; groups[key].bytes += f.len;
  });
  console.log(`TOTAL ${(total/1024).toFixed(1)} KB in ${files.length} files\n`);
  Object.entries(groups).sort((a,b)=>b[1].bytes-a[1].bytes).forEach(([k,v]) =>
    console.log(`  ${(v.bytes/1024).toFixed(1).padStart(7)} KB  ${k}`));

  // which faces the page actually rendered with
  const used = await page.evaluate(() => {
    const set = new Set();
    const walk = el => {
      if (el.nodeType === 1) {
        const t = Array.from(el.childNodes).some(n => n.nodeType===3 && n.textContent.trim());
        if (t) { const cs = getComputedStyle(el);
          set.add(`${cs.fontFamily.split(',')[0].replace(/["']/g,'').trim()} | ${cs.fontStyle} ${cs.fontWeight}`); }
      }
      for (const c of el.children) walk(c);
    };
    walk(document.body); return [...set];
  });
  console.log('\nRendered faces on homepage:'); used.forEach(u => console.log('   ' + u));
  console.log('\nDeclared-but-unused faces:');
  const declared = await page.evaluate(() => Array.from(document.fonts).map(f=>`${f.family} | ${f.style} ${f.weight}`).filter((v,i,a)=>a.indexOf(v)===i));
  declared.filter(d => !used.includes(d)).forEach(d => console.log('   ' + d));
  await browser.close();
})();
