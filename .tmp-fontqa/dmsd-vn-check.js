const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const url = 'https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap';
  await page.goto('about:blank');
  await page.addStyleTag({ url });

  const probe = await page.evaluate(async () => {
    await Promise.all([...document.fonts].map(f => f.load()));
    await document.fonts.ready;
    const cvs = document.createElement('canvas');
    const ctx = cvs.getContext('2d');
    const w = (font, ch) => { ctx.font = font; return ctx.measureText(ch).width; };
    const D = '400 100px "DM Serif Display"';
    const B = '400 100px "Be Vietnam Pro"';
    // tofu reference: private use area char is in no font
    const tofuD = w(D, '\uE123');
    const tofuB = w(B, '\uE123');
    const chars = ['A', 'N', 'ơ', 'đ', 'á', 'ạ', 'ế', 'ộ', 'ữ', 'ề', 'ằ', 'ấ', 'ợ', 'ễ', 'Đ', 'Ơ', 'ỏ'];
    const out = chars.map(ch => {
      const d = w(D, ch);
      const b = w(B, ch);
      return {
        ch, code: 'U+' + ch.codePointAt(0).toString(16).toUpperCase(),
        dmsd: Math.round(d * 100) / 100,
        bvp: Math.round(b * 100) / 100,
        dmsdHasGlyph: Math.abs(d - tofuD) > 0.05,
        bvpHasGlyph: Math.abs(b - tofuB) > 0.05
      };
    });
    const head = 'Nơi hạnh phúc được kiến tạo bằng từng khoảnh khắc';
    return {
      tofuD: Math.round(tofuD * 100) / 100,
      tofuB: Math.round(tofuB * 100) / 100,
      out,
      dmsdHeadWidth: Math.round(w(D, head)),
      bvpHeadWidth: Math.round(w(B, head)),
      loaded: [...document.fonts].map(f => f.family + ' ' + f.weight + ' ' + f.status)
    };
  });
  console.log(JSON.stringify(probe, null, 1));
  await browser.close();
})();
