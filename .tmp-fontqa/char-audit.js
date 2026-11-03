const { chromium } = require('playwright');
const BASE = 'http://localhost:5124';
const PAGES = ['/', '/thuc-don', '/thuc-don/philu', '/tiec-cuoi/philu', '/tiec-cuoi/golden-phoenix', '/dat-tiec'];

const RANGES = {
  'latin':      'U+0000-00FF,U+0131,U+0152-0153,U+02BB-02BC,U+02C6,U+02DA,U+02DC,U+0304,U+0308,U+0329,U+2000-206F,U+20AC,U+2122,U+2191,U+2193,U+2212,U+2215,U+FEFF,U+FFFD',
  'latin-ext':  'U+0100-02BA,U+02BD-02C5,U+02C7-02CC,U+02CE-02D7,U+02DD-02FF,U+0304,U+0308,U+0329,U+1D00-1DBF,U+1E00-1E9F,U+1EF2-1EFF,U+2020,U+20A0-20AB,U+20AD-20C4,U+2113,U+2C60-2C7F,U+A720-A7FF',
  'vietnamese': 'U+0102-0103,U+0110-0111,U+0128-0129,U+0168-0169,U+01A0-01A1,U+01AF-01B0,U+0300-0301,U+0303-0304,U+0308-0309,U+0323,U+0329,U+1EA0-1EF9,U+20AB',
};
function parse(s){const set=new Set();s.split(',').forEach(p=>{const t=p.trim().replace(/^U\+/,'');if(t.includes('-')){const[a,b]=t.split('-').map(x=>parseInt(x,16));for(let i=a;i<=b;i++)set.add(i);}else set.add(parseInt(t,16));});return set;}
const R = Object.fromEntries(Object.entries(RANGES).map(([k,v])=>[k,parse(v)]));

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const onlyExt = new Map(), inVN = new Map(), other = new Map();
  for (const p of PAGES) {
    await page.goto(BASE + p, { waitUntil: 'domcontentloaded' });
    const text = await page.evaluate(() => document.body.innerText);
    for (const ch of text) {
      const cp = ch.codePointAt(0);
      const key = `${ch} U+${cp.toString(16).toUpperCase().padStart(4,'0')}`;
      if (R['latin'].has(cp)) continue;
      if (R['vietnamese'].has(cp)) { inVN.set(key,(inVN.get(key)||0)+1); continue; }
      if (R['latin-ext'].has(cp)) { onlyExt.set(key,(onlyExt.get(key)||0)+1); continue; }
      other.set(key,(other.get(key)||0)+1);
    }
  }
  console.log(`chars in vietnamese subset : ${inVN.size} distinct`);
  console.log(`chars ONLY in latin-ext    : ${onlyExt.size} distinct`);
  [...onlyExt.entries()].sort((a,b)=>b[1]-a[1]).forEach(([k,v])=>console.log(`   ${k} x${v}`));
  console.log(`chars in NO subset         : ${other.size} distinct`);
  [...other.entries()].sort((a,b)=>b[1]-a[1]).forEach(([k,v])=>console.log(`   ${k} x${v}`));
  await browser.close();
})();
