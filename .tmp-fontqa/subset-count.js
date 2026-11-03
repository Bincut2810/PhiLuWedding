const { chromium } = require('playwright');
const CHROME_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36';

const VARIANTS = {
  'A current':
    'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap',
  'B +subset=vietnamese':
    'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&subset=vietnamese&display=swap',
  'C +subset=vietnamese,latin':
    'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&subset=vietnamese,latin&display=swap',
  'D split: vietnamese link then latin link':
    null,
};

(async () => {
  for (const [label, url] of Object.entries(VARIANTS)) {
    let cssUrl = url;
    if (!cssUrl) {
      cssUrl = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';
    }
    const res = await fetch(cssUrl, { headers: { 'User-Agent': CHROME_UA } });
    const css = await res.text();
    const subsets = [...css.matchAll(/\/\*\s*([\w-]+)\s*\*\//g)].map(m => m[1]);
    const counts = {};
    subsets.forEach(s => counts[s] = (counts[s]||0)+1);
    const files = new Set([...css.matchAll(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/g)].map(m => m[1]));
    console.log(`\n=== ${label} ===`);
    console.log(`  css ${(Buffer.byteLength(css)/1024).toFixed(1)} KB | blocks ${subsets.length} | files ${files.size}`);
    console.log(`  subsets: ${Object.entries(counts).map(([k,v])=>`${k}x${v}`).join(', ')}`);
  }
})();
