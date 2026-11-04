// Minimal CDP driver using Node's built-in WebSocket (Node 21).
const fs = require('fs');
const path = require('path');

const PORT = process.env.CDP_PORT || 9222;
const OUT = process.env.OUT_DIR || 'd:/PhiLuWedding/.tmp-shots';

async function httpJson(url) {
  const res = await fetch(url);
  return res.json();
}

async function connect() {
  for (let i = 0; i < 40; i++) {
    try {
      const list = await httpJson(`http://127.0.0.1:${PORT}/json/list`);
      const page = list.find(t => t.type === 'page');
      if (page) return page.webSocketDebuggerUrl;
    } catch {}
    await new Promise(r => setTimeout(r, 500));
  }
  throw new Error('no CDP target');
}

class CDP {
  constructor(ws) {
    this.ws = ws;
    this.id = 0;
    this.pending = new Map();
    ws.addEventListener('message', ev => {
      const msg = JSON.parse(ev.data);
      if (msg.id && this.pending.has(msg.id)) {
        const { resolve, reject } = this.pending.get(msg.id);
        this.pending.delete(msg.id);
        msg.error ? reject(new Error(JSON.stringify(msg.error))) : resolve(msg.result);
      }
    });
  }
  send(method, params = {}) {
    const id = ++this.id;
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.ws.send(JSON.stringify({ id, method, params }));
      setTimeout(() => {
        if (this.pending.has(id)) {
          this.pending.delete(id);
          reject(new Error('timeout ' + method));
        }
      }, 60000);
    });
  }
}

const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const wsUrl = await connect();
  const ws = new WebSocket(wsUrl);
  await new Promise(r => ws.addEventListener('open', r, { once: true }));
  const cdp = new CDP(ws);

  await cdp.send('Page.enable');
  await cdp.send('Runtime.enable');
  await cdp.send('Log.enable');
  await cdp.send('Network.enable');

  const consoleErrors = [];
  const failedRequests = [];
  ws.addEventListener('message', ev => {
    const m = JSON.parse(ev.data);
    if (m.method === 'Log.entryAdded' && m.params.entry.level === 'error') {
      consoleErrors.push(m.params.entry.text + ' ' + (m.params.entry.url || ''));
    }
    if (m.method === 'Network.loadingFailed') {
      failedRequests.push(m.params.errorText + ' ' + (m.params.type || ''));
    }
  });

  const viewports = [
    { name: 'desktop', width: 1440, height: 900, mobile: false },
    { name: 'mobile', width: 390, height: 844, mobile: true },
  ];

  const results = {};

  for (const vp of viewports) {
    await cdp.send('Emulation.setDeviceMetricsOverride', {
      width: vp.width, height: vp.height, deviceScaleFactor: 1,
      mobile: vp.mobile,
    });
    await cdp.send('Page.navigate', { url: 'http://localhost:5199/lien-he' });
    await sleep(3500);

    const evalJs = async expr => {
      const r = await cdp.send('Runtime.evaluate', { expression: expr, returnByValue: true });
      return r.result.value;
    };

    const metrics = await evalJs(`(() => {
      const de = document.documentElement;
      const overflowing = [...document.querySelectorAll('body *')]
        .filter(el => {
          const r = el.getBoundingClientRect();
          return r.width > 0 && (r.right > de.clientWidth + 1 || r.left < -1);
        })
        .slice(0, 12)
        .map(el => el.tagName + '.' + (el.className && el.className.baseVal === undefined ? String(el.className) : '') + ' right=' + Math.round(el.getBoundingClientRect().right));
      return {
        scrollWidth: de.scrollWidth,
        clientWidth: de.clientWidth,
        innerWidth: window.innerWidth,
        docHeight: de.scrollHeight,
        overflowing,
      };
    })()`);

    const sel = await evalJs(`(() => {
      const pick = s => { const e = document.querySelector(s); if (!e) return null; const r = e.getBoundingClientRect(); return {top: Math.round(r.top + window.scrollY), h: Math.round(r.height), w: Math.round(r.width)}; };
      const cs = s => { const e = document.querySelector(s); if (!e) return null; const c = getComputedStyle(e); return {font: c.fontFamily.slice(0,30), size: c.fontSize, style: c.fontStyle, transform: c.textTransform, tracking: c.letterSpacing, color: c.color, bg: c.backgroundColor}; };
      return {
        heroTitle: cs('.contact-hero__title'),
        heroAccent: cs('.contact-hero__accent'),
        heroLede: cs('.contact-hero__lede'),
        eyebrow: cs('.contact-eyebrow'),
        venueCard: pick('.contact-venue-card'),
        venueCardBg: cs('.contact-venue-card'),
        venueMedia: pick('.contact-venue-card__media'),
        venuesBg: cs('.contact-venues'),
        venueTitle: cs('.contact-venue-card__title'),
        venueAddr: cs('.contact-venue-card__address-text'),
        venueBtn: cs('.contact-venue-card__actions .btn'),
        hotline: cs('.contact-channel__hotline'),
        channelLabel: cs('.contact-channel__label'),
        map: pick('.contact-map__frame'),
        bubble: pick('.contact-bubbles__link--hotline'),
      };
    })()`);

    const shot = await cdp.send('Page.captureScreenshot', {
      format: 'png', captureBeyondViewport: true,
    });
    fs.writeFileSync(path.join(OUT, `contact-${vp.name}.png`), Buffer.from(shot.data, 'base64'));

    results[vp.name] = { metrics, sel };
  }

  console.log(JSON.stringify(results, null, 1));
  console.log('CONSOLE_ERRORS', JSON.stringify(consoleErrors, null, 1));
  console.log('FAILED_REQUESTS', JSON.stringify(failedRequests, null, 1));
  ws.close();
  process.exit(0);
})().catch(e => { console.error('FAIL', e); process.exit(1); });
