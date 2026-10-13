// Astro copies every imported original image into dist/_astro even when the pages only use its AVIF and WebP
// derivatives. This removes any file in dist/_astro that no built HTML, CSS or JS file names, so the deployed site
// carries only what it serves. Run after `astro build` and before scripts/check-site.mjs.
import fs from 'node:fs';
import path from 'node:path';

const dist = path.resolve(process.argv[2] ?? 'dist');
const assets = path.join(dist, '_astro');
if (!fs.existsSync(assets)) process.exit(0);

const texts = [];
(function walk(dir) {
  for (const d of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, d.name);
    if (d.isDirectory()) walk(p);
    else if (/\.(html|css|js|mjs|xml|json|txt|webmanifest)$/.test(d.name)) texts.push(fs.readFileSync(p, 'utf8'));
  }
})(dist);
const all = texts.join('\n');

let removed = 0, bytes = 0;
for (const name of fs.readdirSync(assets)) {
  if (/\.(js|mjs|css)$/.test(name)) continue; // code is always kept
  if (all.includes(name)) continue;
  const p = path.join(assets, name);
  bytes += fs.statSync(p).size;
  fs.rmSync(p);
  removed++;
}
console.log(`prune-dist: removed ${removed} unreferenced files (${(bytes / 1e6).toFixed(1)} MB) from dist/_astro`);
