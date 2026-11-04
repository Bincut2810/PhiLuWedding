const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('about:blank');
  await page.addStyleTag({ url: 'https://fonts.googleapis.com/css2?family=DM+Serif+Display&display=swap' });

  const res = await page.evaluate(async () => {
    const faces = [
      new FontFace('DMSD-LatinExt', 'url(https://fonts.gstatic.com/s/dmserifdisplay/v17/-nFnOHM81r4j6k0gjAW3mujVU2B2G_5x0ujy.woff2)'),
      new FontFace('DMSD-Latin', 'url(https://fonts.gstatic.com/s/dmserifdisplay/v17/-nFnOHM81r4j6k0gjAW3mujVU2B2G_Bx0g.woff2)')
    ];
    for (const f of faces) { await f.load(); document.fonts.add(f); }
    const cvs = document.createElement('canvas');
    const ctx = cvs.getContext('2d');
    const w = (font, ch) => { ctx.font = font; return ctx.measureText(ch).width; };
    const probe = ['a', 'A', 'ạ', 'ế', 'ộ', 'ữ', 'ơ', 'đ', 'à', 'á', 'è', 'ỳ', 'ế'];
    const out = {};
    for (const ch of probe) {
      // monospace fallback => a missing glyph renders with the mono advance (55)
      const withMono = w('100px DMSD-LatinExt, monospace', ch);
      const pureMono = w('100px monospace', ch);
      out[ch] = {
        code: 'U+' + ch.codePointAt(0).toString(16).toUpperCase(),
        inDMSD: Math.abs(withMono - pureMono) > 0.05,
        width: Math.round(withMono * 10) / 10
      };
    }
    return out;
  });
  console.log(JSON.stringify(res, null, 1));
  await browser.close();
})();
