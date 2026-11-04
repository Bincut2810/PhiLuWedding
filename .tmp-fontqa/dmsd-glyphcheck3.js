const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('about:blank');
  await page.addStyleTag({ url: 'https://fonts.googleapis.com/css2?family=DM+Serif+Display&display=swap' });

  const res = await page.evaluate(async () => {
    await document.fonts.ready;
    await Promise.all([...document.fonts].map(f => f.load()));
    const cvs = document.createElement('canvas');
    const ctx = cvs.getContext('2d');
    const w = (font, ch) => { ctx.font = font; return ctx.measureText(ch).width; };
    const probe = ['a', 'A', 'ạ', 'ế', 'ộ', 'ữ', 'ơ', 'đ', 'à', 'á', 'è', 'ỳ', 'ầ', 'ậ'];
    const out = {};
    for (const ch of probe) {
      const dmsd = w('100px "DM Serif Display"', ch);
      const serif = w('100px serif', ch);
      const chain = w('100px "DM Serif Display", serif', ch);
      out[ch] = {
        code: 'U+' + ch.codePointAt(0).toString(16).toUpperCase(),
        dmsd: Math.round(dmsd * 10) / 10,
        serif: Math.round(serif * 10) / 10,
        chain: Math.round(chain * 10) / 10,
        // if DMSD lacks the glyph, the chain resolves to the fallback width
        servedByDMSD: Math.abs(chain - serif) > 0.05 && Math.abs(chain - dmsd) < 0.05
      };
    }
    return { out, fonts: [...document.fonts].map(f => f.family + ' ' + f.weight + ' ' + f.status) };
  });
  console.log(JSON.stringify(res, null, 1));
  await browser.close();
})();
