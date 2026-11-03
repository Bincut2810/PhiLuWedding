const { chromium } = require('playwright');
const FONT_CSS = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';
const CHROME_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36';

// fetch the CSS the browser sees and record each subset's unicode-range
async function getSubsets() {
  const res = await fetch(FONT_CSS, { headers: { 'User-Agent': CHROME_UA } });
  const css = await res.text();
  const out = {};
  const blocks = css.split('@font-face').slice(1);
  let lastComment = '?';
  blocks.forEach(b => {
    const body = b.split('}')[0];
    const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
    const url = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    const ur = (body.match(/unicode-range:\s*([^;]+);/) || [])[1] || '';
    if (!url) return;
    out[url] = { fam, ur, lastComment };
  });
  // recover subset names by scanning comments in order
  const order = [];
  const re = /\/\*\s*([\w-]+)\s*\*\/|@font-face/g;
  let m;
  while ((m = re.exec(css))) { if (m[1]) order.push(m[1]); }
  const urls = Object.values(out).map(o => o.url);
  return { out, order };
}

function parseRange(s) {
  const set = new Set();
  s.split(',').forEach(p => {
    const t = p.trim().replace(/^U\+/, '');
    if (t.includes('-')) { const [a,b] = t.split('-').map(x=>parseInt(x,16)); for (let i=a;i<=b;i++) set.add(i); }
    else set.add(parseInt(t,16));
  });
  return set;
}

(async () => {
  const { out } = await getSubsets();
  // For each family, map subset -> range, using the comment right before the block
  const res = await fetch(FONT_CSS, { headers: { 'User-Agent': CHROME_UA } });
  const css = await res.text();
  const re = /\/\*\s*([\w-]+)\s*\*\/|@font-face\s*\{([^}]*)\}/g;
  let m; let pending = null;
  const byFam = {};
  while ((m = re.exec(css))) {
    if (m[1]) { pending = m[1]; continue; }
    const body = m[2];
    const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
    const style = (body.match(/font-style:\s*(\w+)/) || [])[1];
    const weight = (body.match(/font-weight:\s*(\d+)/) || [])[1];
    const ur = (body.match(/unicode-range:\s*([^;]+);/) || [])[1] || '';
    const url = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
    if (!url) continue;
    (byFam[fam] = byFam[fam] || []).push({ style, weight, subset: pending, ur, url });
  }
  Object.entries(byFam).forEach(([fam, list]) => {
    console.log(`\n=== ${fam} ===`);
    const seen = new Set();
    list.forEach(l => {
      const key = `${l.style} ${l.weight} ${l.subset}`;
      if (seen.has(key)) return; seen.add(key);
      console.log(`  ${l.style.padEnd(6)} ${l.weight}  ${String(l.subset).padEnd(12)} ${l.ur.slice(0,110)}`);
    });
  });
})();
