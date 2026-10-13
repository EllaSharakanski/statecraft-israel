// Builds the Press page's two downloads into dist/downloads/ (issue #417). Nothing here is committed: the zips are
// made at every build from files already in the repository.
//  - statecraft-israel-screenshots.zip: the in-engine screenshots listed in docs/images/captures.yaml, never concept
//    art or the trailer;
//  - statecraft-israel-ai-concept-art.zip: the AI-generated images in promo/final/ (the ASSETS.yaml rows marked
//    ai_generated), each renamed to carry "AI concept art" in its file name.
// Each zip holds a README.txt with the label, licence and provenance of every file, taken from ASSETS.yaml. The zips
// are deterministic: sorted entries, a fixed timestamp, no extra fields. Images are stored as they are (they are
// already compressed); the README is deflated. Run after `astro build` (npm run build does it).
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';
import YAML from 'yaml';

const here = fileURLToPath(new URL('../../', import.meta.url));

export const SCREENSHOTS = 'statecraft-israel-screenshots';
export const CONCEPT = 'statecraft-israel-ai-concept-art';
export const CONCEPT_TAG = 'AI concept art';

// ASSETS.yaml globs use fnmatch: '*' also matches '/'.
function fnmatch(glob) {
  let re = '';
  for (let i = 0; i < glob.length; i++) {
    const c = glob[i];
    if (c === '*') re += '.*';
    else if (c === '?') re += '.';
    else if (c === '[') {
      const end = glob.indexOf(']', i + 1);
      re += end < 0 ? '\\[' : `[${glob.slice(i + 1, end).replace(/^!/, '^').replace(/\\/g, '\\\\')}]`;
      if (end >= 0) i = end;
    } else re += c.replace(/[.+^${}()|\\/]/g, '\\$&');
  }
  return new RegExp(`^${re}$`);
}

function assetRow(rules, file) {
  const row = rules.find((r) => fnmatch(r.glob).test(file));
  if (!row) throw new Error(`press-kit: ${file} has no ASSETS.yaml row`);
  return row;
}

const longDate = (d) => new Date(`${d}T00:00:00Z`).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });

const provenance = (rows) => [...rows.values()].map((r) =>
  [`Files matching ${r.glob}:`, `  Licence: ${r.licence}`, `  ${r.ai_generated ? 'Made with' : 'Source'}: ${r.source}`, r.note ? `  Note: ${r.note}` : null,
    r.status === 'owner-verify' ? '  The project owner has not yet confirmed the generator terms for this row (status owner-verify in ASSETS.yaml).' : null]
    .filter(Boolean).join('\n')).join('\n\n');

/**
 * The two zips: their names, entries (zip path and source file) and README text. Reads only, writes nothing.
 * `repo` is the repository root; pass it when this module is bundled (the Press page does), since import.meta.url
 * then no longer points into site/scripts/.
 */
export function plan(repo = here) {
  const read = (p) => fs.readFileSync(path.join(repo, p));
  const rules = YAML.parse(read('ASSETS.yaml').toString('utf8')).rules;
  const captures = YAML.parse(read('docs/images/captures.yaml').toString('utf8'));

  // 1. In-engine screenshots, as captures.yaml lists them.
  const label = `In-engine capture — ${captures.build}, ${longDate(String(captures.date))}`;
  const shots = captures.images.map((img) => ({ file: `docs/images/${img.file}`, img }))
    .sort((a, b) => a.file.localeCompare(b.file));
  const shotRows = new Map();
  const shotLines = shots.map(({ file, img }) => {
    const row = assetRow(rules, file);
    if (row.ai_generated) throw new Error(`press-kit: ${file} is marked ai_generated; it cannot go in the screenshots zip`);
    shotRows.set(row.glob, row);
    return [img.file, `  ${img.title.en}. ${label}. Licence: ${row.licence}.`, img.ai_part ? `  ${img.ai_part.en}` : null]
      .filter(Boolean).join('\n');
  });
  const shotsReadme = [
    'Statecraft: Israel: in-engine screenshots',
    '',
    `Real screenshots of the game's own WebGL build (${captures.build}, ${longDate(String(captures.date))}). They show the`,
    'current prototype as it is. No concept art and no trailer frames are in this zip.',
    '',
    'Label them "In-engine capture" with the build and date, and credit "Statecraft: Israel contributors"',
    '(CC BY 4.0, https://creativecommons.org/licenses/by/4.0/).',
    '',
    'צילומי מסך אמיתיים מתוך המשחק, ברישיון CC BY 4.0. הקרדיט: "Statecraft: Israel contributors".',
    '',
    'Files:',
    '',
    shotLines.join('\n'),
    '',
    'Licence and provenance (from ASSETS.yaml):',
    '',
    provenance(shotRows),
    '',
    'Source: https://github.com/eyalgolan/statecraft-israel (docs/images/, provenance in ASSETS.yaml)',
    '',
  ].join('\n');

  // 2. AI-generated concept art: every image in promo/final/ whose ASSETS.yaml row says ai_generated.
  const finals = fs.readdirSync(path.join(repo, 'promo/final')).sort()
    .map((n) => `promo/final/${n}`)
    .filter((f) => /\.(png|jpe?g|webp)$/i.test(f) && assetRow(rules, f).ai_generated);
  const conceptRows = new Map();
  const art = finals.map((file) => {
    const row = assetRow(rules, file);
    conceptRows.set(row.glob, row);
    const base = path.basename(file);
    return { file, name: `${CONCEPT_TAG} - ${base}`, row };
  });
  const artLines = art.map(({ name, row }) => `${name}\n  Concept art — AI-generated. Licence: ${row.licence} (no rights claimed).`);
  const conceptReadme = [
    'Statecraft: Israel: AI concept art',
    '',
    'Every image in this zip is AI-generated concept art. None of it is a screenshot of the game, and it shows things',
    'the current build does not have. Some images show AI likenesses of real people; they are not photographs.',
    '',
    'If you publish any of these images, label them as AI-generated concept art. For screenshots of the game,',
    'use the in-engine screenshots zip instead.',
    '',
    'כל התמונות בקובץ הזה הן אמנות קונספט שנוצרה בבינה מלאכותית, ולא צילומי מסך מהמשחק. אם אתם מפרסמים אותן,',
    'ציינו שזו אמנות קונספט שנוצרה בבינה מלאכותית.',
    '',
    'Files:',
    '',
    artLines.join('\n'),
    '',
    'Licence and provenance (from ASSETS.yaml):',
    '',
    provenance(conceptRows),
    '',
    'Source: https://github.com/eyalgolan/statecraft-israel (promo/final/)',
    '',
  ].join('\n');

  return [
    { zip: `${SCREENSHOTS}.zip`, dir: SCREENSHOTS, readme: shotsReadme,
      entries: shots.map(({ file }) => ({ name: path.basename(file), file: path.join(repo, file) })) },
    { zip: `${CONCEPT}.zip`, dir: CONCEPT, readme: conceptReadme,
      entries: art.map(({ name, file }) => ({ name, file: path.join(repo, file) })) },
  ];
}

// A minimal zip writer (PKWARE APPNOTE 6.3): no new dependency. Fixed DOS time 1980-01-01 00:00, UTF-8 names.
const DOS_TIME = 0, DOS_DATE = (0 << 9) | (1 << 5) | 1;
export function zip(files) {
  const locals = [], centrals = [];
  let offset = 0;
  for (const { name, data } of [...files].sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0))) {
    const nameBuf = Buffer.from(name, 'utf8');
    const deflated = /\.(txt|md)$/i.test(name) ? zlib.deflateRawSync(data, { level: 9 }) : null;
    const method = deflated && deflated.length < data.length ? 8 : 0;
    const body = method === 8 ? deflated : data;
    const crc = zlib.crc32(data);
    const head = Buffer.alloc(30);
    head.writeUInt32LE(0x04034b50, 0); head.writeUInt16LE(20, 4); head.writeUInt16LE(0x0800, 6);
    head.writeUInt16LE(method, 8); head.writeUInt16LE(DOS_TIME, 10); head.writeUInt16LE(DOS_DATE, 12);
    head.writeUInt32LE(crc, 14); head.writeUInt32LE(body.length, 18); head.writeUInt32LE(data.length, 22);
    head.writeUInt16LE(nameBuf.length, 26); head.writeUInt16LE(0, 28);
    const cen = Buffer.alloc(46);
    cen.writeUInt32LE(0x02014b50, 0); cen.writeUInt16LE((3 << 8) | 20, 4); cen.writeUInt16LE(20, 6);
    cen.writeUInt16LE(0x0800, 8); cen.writeUInt16LE(method, 10); cen.writeUInt16LE(DOS_TIME, 12);
    cen.writeUInt16LE(DOS_DATE, 14); cen.writeUInt32LE(crc, 16); cen.writeUInt32LE(body.length, 20);
    cen.writeUInt32LE(data.length, 24); cen.writeUInt16LE(nameBuf.length, 28);
    cen.writeUInt32LE((0o100644 << 16) >>> 0, 38); cen.writeUInt32LE(offset, 42);
    locals.push(head, nameBuf, body);
    centrals.push(cen, nameBuf);
    offset += head.length + nameBuf.length + body.length;
  }
  const cd = Buffer.concat(centrals);
  if (offset + cd.length > 0xffffffff) throw new Error('press-kit: zip over 4 GB needs Zip64');
  const end = Buffer.alloc(22);
  end.writeUInt32LE(0x06054b50, 0); end.writeUInt16LE(files.length, 8); end.writeUInt16LE(files.length, 10);
  end.writeUInt32LE(cd.length, 12); end.writeUInt32LE(offset, 16);
  return Buffer.concat([...locals, cd, end]);
}

/** One entry's bytes, uncompressed. */
export function zipRead(buf, e) {
  const start = e.offset + 30 + buf.readUInt16LE(e.offset + 26) + buf.readUInt16LE(e.offset + 28);
  const body = buf.subarray(start, start + e.csize);
  return e.method === 8 ? zlib.inflateRawSync(body) : body;
}

/** The entry names of a zip, read from its central directory (used by scripts/check-site.mjs). */
export function zipEntries(buf) {
  const eocd = buf.lastIndexOf(Buffer.from([0x50, 0x4b, 0x05, 0x06]));
  if (eocd < 0) throw new Error('no end of central directory');
  const count = buf.readUInt16LE(eocd + 10);
  let p = buf.readUInt32LE(eocd + 16);
  const out = [];
  for (let i = 0; i < count; i++) {
    if (buf.readUInt32LE(p) !== 0x02014b50) throw new Error('bad central directory');
    const n = buf.readUInt16LE(p + 28), x = buf.readUInt16LE(p + 30), c = buf.readUInt16LE(p + 32);
    out.push({ name: buf.toString('utf8', p + 46, p + 46 + n), csize: buf.readUInt32LE(p + 20), size: buf.readUInt32LE(p + 24), offset: buf.readUInt32LE(p + 42), method: buf.readUInt16LE(p + 10) });
    p += 46 + n + x + c;
  }
  return out;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const dist = path.resolve(process.argv[2] ?? 'dist');
  const out = path.join(dist, 'downloads');
  fs.mkdirSync(out, { recursive: true });
  for (const z of plan()) {
    const files = [
      { name: `${z.dir}/README.txt`, data: Buffer.from(z.readme, 'utf8') },
      ...z.entries.map((e) => ({ name: `${z.dir}/${e.name}`, data: fs.readFileSync(e.file) })),
    ];
    const buf = zip(files);
    fs.writeFileSync(path.join(out, z.zip), buf);
    console.log(`press-kit: downloads/${z.zip} ${files.length} files, ${(buf.length / 1e6).toFixed(1)} MB`);
  }
}
