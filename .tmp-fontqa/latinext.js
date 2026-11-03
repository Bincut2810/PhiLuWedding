const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';
const PAGES = ['/', '/thuc-don', '/thuc-don/philu', '/tiec-cuoi/philu', '/dat-tiec'];

// Latin-ext range from Google Fonts CSS
const LATIN_EXT = 'U+0100-02BA,U+02BD-02C5,U+02C7-02CC,U+02CE-02D7,U+02DD-02FF,U+0304,U+0308,U+0329,U+1D00-1DBF,U+1E00-1E9F,U+1EF2-1EFF,U+2020,U+20A0-20AB,U+20AD-20C4,U+2113,U+2C60-2C7F,U+A720-A7FF';
const LATIN = 'U+0000-00FF,U+0131,U+0152-0153,U+02BB-02BC,U+02C6,U+02DA,U+02DC,U+0304,U+0308,U+0329,U+2000-206F,U+20AC,U+2122,U+2191,U+2193,U+2212,U+2215,U+FEFF,U+FFFD';

function parse(range) {
  const set = new Set();
  range.split(',').forEach(p => {
    const m = p.trim().replace(/^U\+/, '');
    if (m.includes('-')) { const [a,b]=m.split('-').map(x=>parseInt(x,16)); for(let i=a;i<=b;i++) set.add(i); }
    else set.add(parseInt(m,16));
  });
  return set;
}
const ext = parse(LATIN_EXT), lat = parse(LATIN);

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const hits = new Map();
  for (const p of PAGES) {
    await page.goto(BASE + p, { waitUntil: 'domcontentloaded' });
    const text = await page.evaluate(() => document.body.innerText);
    for (const ch of text) {
      const cp = ch.codePointAt(0);
      if (ext.has(cp) && !lat.has(cp)) {
        const key = `${ch} U+${cp.toString(16).toUpperCase().padStart(4,'0')}`;
        hits.set(key, (hits.get(key)||0)+1);
      }
    }
  }
  console.log('Characters that force the latin-ext subset:');
  [...hits.entries()].sort((a,b)=>b[1]-a[1]).forEach(([k,v]) => console.log(`  ${k}  x${v}`));
  if (!hits.size) console.log('  (none found in innerText)');
  await browser.close();
})();
