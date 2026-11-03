const { chromium } = require('playwright');
const CHROME_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36';

const V2 = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap';
const V2_700 = 'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;0,700;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;0,700;1,400&display=swap';
// v1 API supports &subset= properly
const V1 = 'https://fonts.googleapis.com/css?family=Newsreader:400,500,600,400italic|Source+Sans+3:400,500,600,400italic&subset=vietnamese,latin&display=swap';

async function inspect(url, label) {
  const res = await fetch(url, { headers: { 'User-Agent': CHROME_UA } });
  const css = await res.text();
  const blocks = [...css.matchAll(/@font-face\s*\{([^}]*)\}/g)].map(m => {
    const b = m[1];
    return {
      fam: (b.match(/font-family:\s*'([^']+)'/) || [])[1],
      style: (b.match(/font-style:\s*(\w+)/) || [])[1],
      weight: (b.match(/font-weight:\s*(\d+)/) || [])[1],
      url: (b.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1],
    };
  });
  const subs = [...css.matchAll(/\/\*\s*([\w-]+)\s*\*\//g)].map(m => m[1]);
  console.log(`\n=== ${label} ===`);
  console.log(`  http ${res.status} | css ${(Buffer.byteLength(css)/1024).toFixed(1)} KB | ${blocks.length} blocks | ${subs.length} subset comments`);
  console.log(`  subsets: ${[...new Set(subs)].join(', ')}`);
  const byFam = {};
  blocks.forEach(b => { (byFam[`${b.fam} ${b.style} ${b.weight}`] = byFam[`${b.fam} ${b.style} ${b.weight}`] || new Set()).add(b.url); });
  Object.entries(byFam).forEach(([k, v]) => console.log(`    ${k.padEnd(28)} ${v.size} file(s)`));
  return { blocks, css };
}

(async () => {
  await inspect(V2, 'v2 current');
  await inspect(V2_700, 'v2 with 700');
  await inspect(V1, 'v1 API + subset=vietnamese,latin');
})();
