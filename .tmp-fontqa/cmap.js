const fs = require('fs');

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

const file = process.argv[2];
const buf = fs.readFileSync(file);
const codes = parseCmap(buf);
console.log(file, 'total glyph codes:', codes.size);
const vn = ['ơ', 'đ', 'á', 'ạ', 'ế', 'ộ', 'ữ', 'ề', 'ằ', 'ấ', 'ợ', 'ễ', 'Đ', 'Ơ', 'ỏ', 'ă', 'â', 'ê', 'ô', 'ề', 'ó', 'í'];
const res = {};
let missing = [];
for (const ch of vn) {
  const has = codes.has(ch.codePointAt(0));
  res[ch] = has;
  if (!has) missing.push(ch);
}
console.log(JSON.stringify(res));
console.log('MISSING:', missing.join(' '));
