// Checks the built site (dist/) for the rules a page template could break without anyone noticing:
//  1. every page exists in both languages (/en/x/ and /he/x/);
//  2. each page declares its language and direction on <html>, and lists itself, its counterpart and x-default;
//  3. every image and video in the page body sits in a <figure> that carries a visible media label
//     (In-engine capture, Concept art — AI-generated, Cinematic promo — AI-generated, not gameplay, or Archival
//     photograph), except contributor photos, which are portraits supplied by the owner;
//  4. nothing loads from another origin: no external script, stylesheet, font, image, video or frame.
// Run after `astro build` (npm run build does both). Exit 1 with one line per problem.
import fs from 'node:fs';
import path from 'node:path';

const dist = path.resolve(process.argv[2] ?? 'dist');
const problems = [];
const fail = (file, msg) => problems.push(`${path.relative(dist, file)}: ${msg}`);

function walk(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap((d) => {
    const p = path.join(dir, d.name);
    return d.isDirectory() ? walk(p) : p.endsWith('.html') ? [p] : [];
  });
}

const pages = walk(dist);
const rel = (f) => path.relative(dist, f).split(path.sep).join('/');
const byLang = { en: new Set(), he: new Set() };
for (const f of pages) {
  const r = rel(f);
  const m = r.match(/^(en|he)\/(.*)$/);
  if (m) byLang[m[1]].add(m[2]);
}
for (const [a, b] of [['en', 'he'], ['he', 'en']]) {
  for (const p of byLang[a]) if (!byLang[b].has(p)) problems.push(`${a}/${p}: no ${b}/${p}`);
}
if (byLang.en.size === 0) problems.push('no /en/ pages were built');

const external = /^(?:https?:)?\/\//i;
for (const f of pages) {
  const html = fs.readFileSync(f, 'utf8');
  const r = rel(f);
  const lang = r.match(/^(en|he)\//)?.[1];
  if (lang) {
    const tag = html.match(/<html[^>]*>/)?.[0] ?? '';
    if (!tag.includes(`lang="${lang}"`)) fail(f, `<html> lacks lang="${lang}"`);
    if (!tag.includes(`dir="${lang === 'he' ? 'rtl' : 'ltr'}"`)) fail(f, '<html> has the wrong dir');
    for (const h of ['en', 'he', 'x-default']) {
      if (!new RegExp(`<link rel="alternate" hreflang="${h}" href="https?://`).test(html)) fail(f, `missing absolute hreflang ${h}`);
    }
  }

  // 4. Resources from other origins. Links (<a href>) are fine: they load nothing until followed.
  const resource = [
    ...html.matchAll(/<script[^>]*\ssrc="([^"]+)"/g), ...html.matchAll(/<link[^>]*rel="(?:stylesheet|preload|icon|modulepreload|manifest)"[^>]*href="([^"]+)"/g),
    ...html.matchAll(/<(?:img|source|video|audio|track|iframe|embed)[^>]*\ssrc="([^"]+)"/g),
    ...html.matchAll(/\ssrcset="([^"]+)"/g), ...html.matchAll(/\simagesrcset="([^"]+)"/g), ...html.matchAll(/url\(['"]?([^'")]+)/g),
    ...html.matchAll(/<iframe/g),
  ];
  for (const m of resource) {
    if (m[0] === '<iframe') { fail(f, 'contains an <iframe>'); continue; }
    for (const u of m[1].split(',').map((s) => s.trim().split(/\s+/)[0])) {
      if (external.test(u)) fail(f, `loads from another origin: ${u}`);
    }
  }

  // 3. Every image and video in <main> carries a media label in its <figure>.
  const main = html.match(/<main[\s\S]*<\/main>/)?.[0] ?? '';
  for (const m of main.matchAll(/<(img|video)\b[^>]*>/g)) {
    if (/class="[^"]*\bportrait\b/.test(m[0])) continue; // contributor photo
    const before = main.lastIndexOf('<figure', m.index);
    const closeBefore = main.lastIndexOf('</figure>', m.index);
    const after = main.indexOf('</figure>', m.index);
    if (before < 0 || closeBefore > before || after < 0) { fail(f, `<${m[1]}> outside a <figure>: ${m[0].slice(0, 90)}`); continue; }
    const figure = main.slice(before, after);
    if (!/data-media-label="(engine|concept|cinematic|archival)"/.test(figure)) fail(f, `<${m[1]}> without a media label: ${m[0].slice(0, 90)}`);
  }
  // The facade's video, made on click, takes the place of a button inside a labelled figure; check the button too.
  for (const m of main.matchAll(/<button[^>]*class="facade"[^>]*>/g)) {
    const before = main.lastIndexOf('<figure', m.index);
    const figure = main.slice(before, main.indexOf('</figure>', m.index));
    if (!/data-media-label=/.test(figure)) fail(f, 'video facade without a media label');
  }
}

// The hero and the first video on the home page are real gameplay.
for (const lang of ['en', 'he']) {
  const f = path.join(dist, lang, 'index.html');
  if (!fs.existsSync(f)) continue;
  const main = fs.readFileSync(f, 'utf8').match(/<main[\s\S]*<\/main>/)?.[0] ?? '';
  const firstLabel = main.match(/data-media-label="(\w+)"/)?.[1];
  if (firstLabel !== 'engine') fail(f, `the first image is labelled ${firstLabel}, not an in-engine capture`);
  const firstVideo = main.match(/<figure class="media" data-kind="(\w+)"><div class="media-frame">[\s\S]*?class="facade"/)?.[1];
  if (firstVideo && firstVideo !== 'engine') fail(f, `the first video is ${firstVideo}, not in-engine`);
}

if (problems.length) {
  console.error(problems.join('\n'));
  console.error(`check-site: ${problems.length} problem(s)`);
  process.exit(1);
}
console.log(`check-site: ${pages.length} pages OK (both languages, labels on every image and video, nothing from other origins)`);
