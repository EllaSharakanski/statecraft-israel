// Every image and video the website shows, with the label it carries wherever it appears. The three kinds follow the
// honesty rules in reports/Strategy game websites.md: in-engine capture (with build and date), concept art
// (AI-generated) and cinematic promo (AI-generated, not gameplay). Provenance of the source files: ASSETS.yaml.
import type { ImageMetadata } from 'astro';
import fs from 'node:fs';
import path from 'node:path';
import YAML from 'yaml';
import m2Topdown from '../../../promo/final/10-m2-topdown.png';
import keyArt from '../../../promo/final/01-key-art.png';
import mapDiorama from '../../../promo/final/02-map-diorama.png';
import gameplay1936 from '../../../promo/final/03-gameplay-1936.png';
import cabinet1948 from '../../../promo/final/04-cabinet-1948.png';
import transitCamps from '../../../promo/final/05-event-transit-camps.png';
import diplomacy1958 from '../../../promo/final/06-diplomacy-1958.png';
import erasTelAviv from '../../../promo/final/07-eras-tel-aviv.png';
import defense1955 from '../../../promo/final/08-defense-1955.png';
import flythroughPoster from '../assets/media/flythrough-m2-poster.jpg';
import trailerPoster from '../assets/media/cinematic-trailer-1-poster.jpg';
import { type Lang, formatDate } from '../i18n/ui';
import { repoRoot } from './repo';

export type Kind = 'engine' | 'concept' | 'cinematic';
type Text = Record<Lang, string>;

export interface MediaImage {
  id: string;
  kind: Kind;
  src: ImageMetadata;
  /** Which build and when, for in-engine captures, in each language ("main 4abb64f, 13 October 2026"). */
  build?: Text;
  title: Text;
  alt: Text;
  /** An AI-generated part of a real screenshot (the start screen's painting), said beside the picture. */
  aiPart?: Text;
  /** AI likenesses of real people (docs/CONTENT_POLICY.md section 4). */
  likeness?: boolean;
  /** Path of the source file in the repository. */
  file: string;
}

// The README's screenshots are the site's in-engine images: docs/images/build-*.jpg, described by
// docs/images/captures.yaml. Retaking them updates the site; nothing is copied into site/.
const shotFiles = import.meta.glob<{ default: ImageMetadata }>('../../../docs/images/build-*.jpg', { eager: true });
const shotByName = new Map(Object.entries(shotFiles).map(([p, m]) => [p.split('/').pop()!, m.default]));
interface CaptureEntry { file: string; title: Text; alt: Text; ai_part?: Text }
const manifest = YAML.parse(fs.readFileSync(path.join(repoRoot, 'docs/images/captures.yaml'), 'utf8')) as {
  build: string; date: string | Date; images: CaptureEntry[];
};
const captureDate = manifest.date instanceof Date ? manifest.date.toISOString().slice(0, 10) : String(manifest.date);
export const currentBuild: Text = {
  en: `${manifest.build}, ${formatDate('en', captureDate)}`,
  he: `${manifest.build}, ${formatDate('he', captureDate)}`,
};
for (const name of shotByName.keys()) {
  if (!manifest.images.some((e) => e.file === name)) throw new Error(`media: docs/images/${name} has no entry in docs/images/captures.yaml`);
}

/** The current build's screenshots, in the manifest's order. The first is the home page's hero. */
export const captures: MediaImage[] = manifest.images.map((e) => {
  const src = shotByName.get(e.file);
  if (!src) throw new Error(`media: docs/images/captures.yaml names ${e.file}, which is not in docs/images/`);
  return { id: e.file.replace(/\.jpg$/, ''), kind: 'engine', src, build: currentBuild, title: e.title, alt: e.alt, aiPart: e.ai_part, file: `docs/images/${e.file}` };
});
export const capture = (file: string) => {
  const c = captures.find((x) => x.file === `docs/images/${file}`);
  if (!c) throw new Error(`media: no capture ${file} in docs/images/captures.yaml`);
  return c;
};

const m2Build: Text = { en: 'M2 map build, 10 October 2026', he: 'גרסת המפה של M2, 10 באוקטובר 2026' };

export const engine: MediaImage[] = [
  ...captures,
  {
    id: 'm2-topdown', kind: 'engine', src: m2Topdown, build: m2Build, file: 'promo/final/10-m2-topdown.png',
    title: { en: 'The whole map from above', he: 'כל המפה ממבט על' },
    alt: {
      en: 'An in-engine top-down view of the M2 map: the country’s terrain from the Galilee to the Negev, built from elevation data, with the hex grid.',
      he: 'מבט על מתוך המנוע של מפת M2: פני השטח של הארץ מהגליל ועד הנגב, שנבנו מנתוני גובה, עם רשת המשושים.',
    },
  },
];

export const concept: MediaImage[] = [
  {
    id: 'key-art', kind: 'concept', src: keyArt, file: 'promo/final/01-key-art.png',
    title: { en: 'Key art', he: 'תמונת מפתח' },
    alt: {
      en: 'AI-generated key art: the country as a tilt-shift diorama at sunset, from the coast to the Dead Sea, with the title Statecraft: Israel, 1882 — 2026.',
      he: 'תמונת מפתח שנוצרה בבינה מלאכותית: הארץ כדיורמה בשקיעה, מהחוף ועד ים המלח, עם הכותרת Statecraft: Israel ‏1882 — 2026.',
    },
  },
  {
    id: 'map-diorama', kind: 'concept', src: mapDiorama, file: 'promo/final/02-map-diorama.png',
    title: { en: 'The map as a table diorama', he: 'המפה כדיורמה על שולחן' },
    alt: {
      en: 'AI-generated concept art: a table-top relief map of the country with labelled cities from Haifa to Dimona.',
      he: 'אמנות קונספט שנוצרה בבינה מלאכותית: מפת תבליט של הארץ על שולחן, עם שמות ערים מחיפה ועד דימונה.',
    },
  },
  {
    id: 'gameplay-1936', kind: 'concept', src: gameplay1936, file: 'promo/final/03-gameplay-1936.png',
    title: { en: 'The look we are building toward (set in 1936)', he: 'המראה שאליו אנחנו שואפים (ב־1936)' },
    alt: {
      en: 'AI-generated concept art of the target look: a tilt-shift diorama of the coast around Tel Aviv and Jaffa in 1936, with a top resource bar and an event card. It shows things the game does not have.',
      he: 'אמנות קונספט שנוצרה בבינה מלאכותית של המראה המבוקש: דיורמה של החוף סביב תל אביב ויפו ב־1936, עם סרגל משאבים וכרטיס אירוע. היא מציגה דברים שאין במשחק.',
    },
  },
  {
    id: 'cabinet-1948', kind: 'concept', src: cabinet1948, likeness: true, file: 'promo/final/04-cabinet-1948.png',
    title: { en: 'A cabinet screen mock-up, May 1948', he: 'הדמיה של מסך הממשלה, מאי 1948' },
    alt: {
      en: 'AI-generated mock-up of a cabinet screen for the Provisional Government of May 1948, with AI likenesses of the ministers made from public-domain reference photographs. Not photographs.',
      he: 'הדמיה שנוצרה בבינה מלאכותית של מסך הממשלה הזמנית במאי 1948, עם דמויות השרים שנוצרו בבינה מלאכותית לפי צילומים בנחלת הכלל. אלה אינם צילומים.',
    },
  },
  {
    id: 'event-transit-camps', kind: 'concept', src: transitCamps, file: 'promo/final/05-event-transit-camps.png',
    title: { en: 'An event card mock-up: the transit camps', he: 'הדמיה של כרטיס אירוע: המעברות' },
    alt: {
      en: 'AI-generated mock-up of an event card about the winter in the transit camps, with a painted scene of tents, mud and new immigrants. Not a photograph.',
      he: 'הדמיה שנוצרה בבינה מלאכותית של כרטיס אירוע על החורף במעברות, עם ציור של אוהלים, בוץ ועולים חדשים. זה אינו צילום.',
    },
  },
  {
    id: 'diplomacy-1958', kind: 'concept', src: diplomacy1958, file: 'promo/final/06-diplomacy-1958.png',
    title: { en: 'A diplomacy screen mock-up, 1958', he: 'הדמיה של מסך יחסי חוץ, 1958' },
    alt: {
      en: 'AI-generated mock-up of a foreign-relations screen set in 1958. The game has no diplomacy yet.',
      he: 'הדמיה שנוצרה בבינה מלאכותית של מסך יחסי חוץ ב־1958. במשחק אין עדיין דיפלומטיה.',
    },
  },
  {
    id: 'eras-tel-aviv', kind: 'concept', src: erasTelAviv, file: 'promo/final/07-eras-tel-aviv.png',
    title: { en: 'Tel Aviv in 1909, 1948 and 2026', he: 'תל אביב ב־1909, ב־1948 וב־2026' },
    alt: {
      en: 'AI-generated concept art: three hex-shaped dioramas of the Tel Aviv shore, labelled 1909, 1948 and 2026.',
      he: 'אמנות קונספט שנוצרה בבינה מלאכותית: שלוש דיורמות בצורת משושה של חוף תל אביב, עם התוויות 1909, 1948 ו־2026.',
    },
  },
  {
    id: 'defense-1955', kind: 'concept', src: defense1955, likeness: true, file: 'promo/final/08-defense-1955.png',
    title: { en: 'A defence screen mock-up, October 1955', he: 'הדמיה של מסך הביטחון, אוקטובר 1955' },
    alt: {
      en: 'AI-generated mock-up of a defence screen set in October 1955, with AI likenesses of Moshe Sharett, David Ben-Gurion and Moshe Dayan made from public-domain reference photographs. Not photographs. Readiness bars and other values are illustrative.',
      he: 'הדמיה שנוצרה בבינה מלאכותית של מסך הביטחון באוקטובר 1955, עם דמויות של משה שרת, דוד בן־גוריון ומשה דיין שנוצרו בבינה מלאכותית לפי צילומים בנחלת הכלל. אלה אינם צילומים. המדדים והערכים להמחשה בלבד.',
    },
  },
];

export interface MediaVideo {
  id: string;
  kind: Kind;
  poster: ImageMetadata;
  build?: Text;
  title: Text;
  description: Text;
  /** Files under public/, best first. Empty when the video is watched elsewhere (link). */
  sources: { src: string; type: string }[];
  captions?: Record<Lang, string>;
  link?: string;
  duration: string; // ISO 8601
  durationText: string;
  sound: boolean;
}

export const flythrough: MediaVideo = {
  id: 'flythrough-m2', kind: 'engine', poster: flythroughPoster, build: m2Build,
  title: { en: 'A flight over the M2 map', he: 'טיסה מעל מפת M2' },
  description: {
    en: 'Eight seconds recorded in the engine: the camera flies north over the terrain map, from Beersheba and the Negev past Jerusalem and Tel Aviv to Haifa. No interface and no sound.',
    he: 'שמונה שניות שהוקלטו במנוע המשחק: המצלמה טסה צפונה מעל מפת השטח, מבאר שבע והנגב, על פני ירושלים ותל אביב, ועד חיפה. בלי ממשק ובלי קול.',
  },
  sources: [
    { src: 'media/flythrough-m2-720p.webm', type: 'video/webm' },
    { src: 'media/flythrough-m2-720p.mp4', type: 'video/mp4' },
  ],
  captions: { en: 'media/flythrough-m2.en.vtt', he: 'media/flythrough-m2.he.vtt' },
  duration: 'PT8S', durationText: '0:08', sound: false,
};

/** The cinematic trailer published in the README (GitHub's player). The site does not host it; it links to it. */
export const trailer: MediaVideo = {
  id: 'cinematic-trailer-1', kind: 'cinematic', poster: trailerPoster,
  title: { en: 'The cinematic trailer', he: 'הטריילר הקולנועי' },
  description: {
    en: 'Made with AI video tools from our concept art. It is not in-game footage, and it shows scenes, places and years the game does not have. Music, with quotations of public figures on screen.',
    he: 'נוצר בכלי וידאו של בינה מלאכותית מאמנות הקונספט שלנו. זה אינו צילום מתוך המשחק, והוא מציג סצנות, מקומות ושנים שאין במשחק. מוזיקה, וציטוטים של אישי ציבור על המסך.',
  },
  sources: [],
  link: 'https://github.com/eyalgolan/statecraft-israel#readme',
  duration: 'PT1M23S', durationText: '1:23', sound: true,
};
