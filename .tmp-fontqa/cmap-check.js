const https = require('https');
const fs = require('fs');

function get(url, ua) {
  return new Promise((res, rej) => {
    https.get(url, { headers: { 'User-Agent': ua } }, r => {
      let d = [];
      r.on('data', c => d.push(c));
      r.on('end', () => res(Buffer.concat(d)));
    }).on('error', rej);
  });
}

// minimal TrueType cmap (format 4 and 12) parser
function parseCmap(buf) {
  const numTables = buf.readUInt16BE(4);
  let cmapOff = null;
  for (let i = 0; i < numTables; i++) {
    const off = 12 + i * 16;
    const tag = buf.toString('ascii', off, off + 4);
    if (tag === 'cmap') cmapOff = buf.readUInt32BE(off + 8);
  }
  if (cmapOff === null) throw new Error('no cmap');
  const n = buf.readUInt16BE(cmapOff + 2);
  const subs = [];
  for (let i = 0; i < n; i++) {
    const p = cmapOff + 4 + i * 8;
    subs.push({ platform: buf.readUInt16BE(p), encoding: buf.readUInt16BE(p + 2), offset: cmapOff + buf.readUInt32BE(p + 4) });
  }
  const codes = new Set();
  for (const s of subs) {
    const fmt = buf.readUInt16BE(s.offset);
    if (fmt === 4) {
      const segX2 = buf.readUInt16BE(s.offset + 6);
      const seg = segX2 / 2;
      const endBase = s.offset + 14;
      const startBase = endBase + segX2 + 2;
      const deltaBase = startBase + segX2;
      const rangeBase = deltaBase + segX2;
      for (let i = 0; i < seg; i++) {
        const end = buf.readUInt16BE(endBase + i * 2);
        const start = buf.readUInt16BE(startBase + i * 2);
        if (start === 0xffff) continue;
        const delta = buf.readInt16BE(deltaBase + i * 2);
        const rangeOff = buf.readUInt16BE(rangeBase + i * 2);
        for (let c = start; c <= end && c !== 0x10000; c++) {
          let g;
          if (rangeOff === 0) g = (c + delta) & 0xffff;
          else {
            const gi = rangeBase + i * 2 + rangeOff + (c - start) * 2;
            if (gi + 1 >= buf.length) continue;
            g = buf.readUInt16BE(gi);
            if (g !== 0) g = (g + delta) & 0xffff;
          }
          if (g !== 0) codes.add(c);
        }
      }
    } else if (fmt === 12) {
      const nGroups = buf.readUInt32BE(s.offset + 12);
      for (let i = 0; i < nGroups; i++) {
        const p = s.offset + 16 + i * 12;
        const start = buf.readUInt32BE(p), end = buf.readUInt32BE(p + 4);
        for (let c = start; c <= end; c++) codes.add(c);
      }
    }
  }
  return codes;
}

(async () => {
  const ua = 'Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)';
  const css = (await get('https://fonts.googleapis.com/css2?family=DM+Serif+Display&family=Be+Vietnam+Pro:wght@400;500;600;700&display=swap', ua)).toString('latin1');
  const urls = [...css.matchAll(/url\((https:[^)]+?\.ttf)\)/g)].map(m => m[1]);
  console.log('ttf urls:', urls);
  for (const u of urls) {
    const buf = await get(u, ua);
    const codes = parseCmap(buf);
    const vn = ['ơ', 'đ', 'á', 'ạ', 'ế', 'ộ', 'ữ', 'ề', 'ằ', 'ấ', 'ợ', 'ễ', 'Đ', 'Ơ', 'ỏ', 'ă', 'â', 'ê', 'ô', 'ề'];
    const res = {};
    for (const ch of vn) res[ch] = codes.has(ch.codePointAt(0));
    console.log(u.split('/').pop(), 'glyphs:', codes.size, JSON.stringify(res));
  }
})();
