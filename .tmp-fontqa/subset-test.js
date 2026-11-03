const { chromium } = require('playwright');
const CHROME_UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36';

const VARIANTS = {
  'current (no subset param)':
    'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&display=swap',
  'subset=vietnamese,latin':
    'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&subset=vietnamese,latin&display=swap',
  'subset=vietnamese only':
    'https://fonts.googleapis.com/css2?family=Newsreader:ital,wght@0,400;0,500;0,600;1,400&family=Source+Sans+3:ital,wght@0,400;0,500;0,600;1,400&subset=vietnamese&display=swap',
};

(async () => {
  for (const [label, url] of Object.entries(VARIANTS)) {
    const res = await fetch(url, { headers: { 'User-Agent': CHROME_UA } });
    const css = await res.text();
    const re = /\/\*\s*([\w-]+)\s*\*\/|@font-face\s*\{([^}]*)\}/g;
    let m, pending = null;
    const order = [];
    const urls = new Set();
    while ((m = re.exec(css))) {
      if (m[1]) { pending = m[1]; continue; }
      const body = m[2];
      const fam = (body.match(/font-family:\s*'([^']+)'/) || [])[1];
      const style = (body.match(/font-style:\s*(\w+)/) || [])[1];
      const weight = (body.match(/font-weight:\s*(\d+)/) || [])[1];
      const u = (body.match(/url\((https:\/\/fonts\.gstatic\.com\/[^)]+)\)/) || [])[1];
      if (!u) continue;
      urls.add(u);
      order.push(`${fam} ${style} ${weight} [${pending}]`);
    }
    console.log(`\n=== ${label} ===`);
    console.log(`  css ${(Buffer.byteLength(css)/1024).toFixed(1)} KB | ${order.length} blocks | ${urls.size} distinct files`);
    // show subset order for one family to see if vietnamese comes AFTER latin-ext
    const news = order.filter(o => o.startsWith('Newsreader'));
    const seen = [];
    news.forEach(o => { const s = o.match(/\[(\w+)\]/); if (s && !seen.includes(s[1])) seen.push(s[1]); });
    console.log('  Newsreader block order:', seen.join(' -> '));
  }
})();
