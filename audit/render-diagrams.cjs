const { chromium } = require('../src/frontend/node_modules/@playwright/test');
const fs = require('node:fs');
const path = require('node:path');

const directory = path.join(__dirname, 'CORRECTED_DIAGRAMS');
const escapeHtml = value => value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  try {
    for (const name of fs.readdirSync(directory).filter(name => name.endsWith('.mmd'))) {
      const source = fs.readFileSync(path.join(directory, name), 'utf8');
      const page = await browser.newPage({ viewport: { width: 1800, height: 1200 }, deviceScaleFactor: 1 });
      await page.setContent(`<!doctype html><html><head><meta charset="utf-8"><style>body{margin:0;padding:24px;background:#fff;font-family:Arial,sans-serif}.mermaid{display:block;width:max-content;min-width:1300px}</style></head><body><pre class="mermaid">${escapeHtml(source)}</pre><script type="module">import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs'; mermaid.initialize({startOnLoad:false,theme:'neutral',securityLevel:'strict',flowchart:{useMaxWidth:false}}); try { await mermaid.run({querySelector:'.mermaid'}); window.renderResult='ok'; } catch(error) { window.renderResult=String(error); }</script></body></html>`);
      await page.waitForFunction(() => typeof window.renderResult === 'string', { timeout: 30000 });
      const result = await page.evaluate(() => window.renderResult);
      if (result !== 'ok') throw new Error(`${name}: ${result}`);
      await page.locator('.mermaid').screenshot({ path: path.join(directory, name.replace(/\.mmd$/, '.png')) });
      console.log(`Rendered ${name}`);
      await page.close();
    }
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error.message); process.exitCode = 1; });
