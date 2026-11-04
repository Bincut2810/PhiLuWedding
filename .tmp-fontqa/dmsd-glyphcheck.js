const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  await page.goto('about:blank');
  await page.addStyleTag({ url: 'https://fonts.googleapis.com/css2?family=DM+Serif+Display&display=swap' });

  const res = await page.evaluate(async () => {
    // load the actual served woff2 files through the FontFace API
    const faces = [
      new FontFace('DMSD-LatinExt', 'url(https://fonts.gstatic.com/s/dmserifdisplay/v17/-nFnOHM81r4j6k0gjAW3mujVU2B2G_5x0ujy.woff2)'),
      new FontFace('DMSD-Latin', 'url(https://fonts.gstatic.com/s/dmserifdisplay/v17/-nFnOHM81r4j6k0gjAW3mujVU2B2G_Bx0g.woff2)')
    ];
    for (const f of faces) { await f.load(); document.fonts.add(f); }
    const cvs = document.createElement('canvas');
    const ctx = cvs.getContext('2d');
    const w = (font, ch) => { ctx.font = font; return ctx.measureText(ch).width; };
    const test = (label, font) => {
      const probe = ['a', 'A', 'ạ', 'ế', 'ộ', 'ữ', 'ơ', 'đ', 'à', 'á', 'è'];
      const mono = {};
      for (const ch of probe) mono[ch] = Math.round(w('100px monospace', ch) * 10) / 10;
      const out = {};
      for (const ch of probe) {
        const m = w('100px monospace', ch);
        const t = w(font, ch);
        out[ch] = { inFont: Math.abs(t - m) > 0.05, w: Math.round(t * 10) / 10 };
      }
      return { label, mono, out };
    };
    return [
      test('DMSD-LatinExt', '100px DMSD-LatinExt'),
      test('DMSD-Latin', '100px DMSD-Latin')
    ];
  });
  console.log(JSON.stringify(res, null, 1));
  await browser.close();
})();
