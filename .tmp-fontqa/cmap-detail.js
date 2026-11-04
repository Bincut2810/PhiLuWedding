const fs = require('fs');
const buf = fs.readFileSync(process.argv[2]);
const numTables = buf.readUInt16BE(4);
let cmapOff = null;
for (let i = 0; i < numTables; i++) {
  const off = 12 + i * 16;
  if (buf.toString('ascii', off, off + 4) === 'cmap') cmapOff = buf.readUInt32BE(off + 8);
}
const n = buf.readUInt16BE(cmapOff + 2);
console.log('subtables:', n);
for (let i = 0; i < n; i++) {
  const p = cmapOff + 4 + i * 8;
  const plat = buf.readUInt16BE(p), enc = buf.readUInt16BE(p + 2), off = cmapOff + buf.readUInt32BE(p + 4);
  console.log('  platform', plat, 'encoding', enc, 'format', buf.readUInt16BE(off));
}
const codes = [];
for (let i = 0; i < n; i++) {
  const p = cmapOff + 4 + i * 8;
  const off = cmapOff + buf.readUInt32BE(p + 4);
  const fmt = buf.readUInt16BE(off);
  if (fmt === 4) {
    const segX2 = buf.readUInt16BE(off + 6), seg = segX2 / 2;
    const endBase = off + 14, startBase = endBase + segX2 + 2, deltaBase = startBase + segX2, rangeBase = deltaBase + segX2;
    for (let s = 0; s < seg; s++) {
      const end = buf.readUInt16BE(endBase + s * 2), start = buf.readUInt16BE(startBase + s * 2);
      if (start === 0xffff) continue;
      const delta = buf.readInt16BE(deltaBase + s * 2), rangeOff = buf.readUInt16BE(rangeBase + s * 2);
      for (let c = start; c <= end; c++) {
        let g;
        if (rangeOff === 0) g = (c + delta) & 0xffff;
        else {
          const gi = rangeBase + s * 2 + rangeOff + (c - start) * 2;
          if (gi + 1 >= buf.length) continue;
          g = buf.readUInt16BE(gi);
          if (g !== 0) g = (g + delta) & 0xffff;
        }
        if (g !== 0) codes.push(c);
      }
    }
  } else if (fmt === 12) {
    const ng = buf.readUInt32BE(off + 12);
    for (let i2 = 0; i2 < ng; i2++) {
      const q = off + 16 + i2 * 12;
      const start = buf.readUInt32BE(q), end = buf.readUInt32BE(q + 4);
      for (let c = start; c <= end; c++) codes.push(c);
    }
  }
}
const set = new Set(codes);
const arr = [...set].sort((a, b) => a - b);
console.log('unique codes:', arr.length);
console.log('max:', 'U+' + arr[arr.length - 1].toString(16).toUpperCase());
console.log('1E00-1EFF present:', arr.filter(c => c >= 0x1E00 && c <= 0x1EFF).map(c => 'U+' + c.toString(16).toUpperCase()).join(' '));
console.log('0100-01FF present:', arr.filter(c => c >= 0x100 && c <= 0x1FF).map(c => 'U+' + c.toString(16).toUpperCase()).join(' '));
console.log('0200-024F present:', arr.filter(c => c >= 0x200 && c <= 0x24F).map(c => 'U+' + c.toString(16).toUpperCase()).join(' '));
