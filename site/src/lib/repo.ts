// Build-time readers for the repository's own data. The website shows sourced content from content/ and the asset
// records from ASSETS.yaml as they are; it writes no historical claim of its own.
import fs from 'node:fs';
import path from 'node:path';
import YAML from 'yaml';

function findRoot(): string {
  for (const dir of [path.resolve(process.cwd(), '..'), process.cwd()]) {
    if (fs.existsSync(path.join(dir, 'ASSETS.yaml')) && fs.existsSync(path.join(dir, 'content'))) return dir;
  }
  throw new Error('site: run the build from site/ inside the statecraft-israel repository');
}

export const repoRoot = findRoot();
const read = (rel: string) => fs.readFileSync(path.join(repoRoot, rel), 'utf8');
const readYaml = (rel: string) => YAML.parse(read(rel));

export interface Source { url: string; checked?: string; note?: string }
export interface Option { id: string; text: string; historical?: boolean }
export interface GameEvent {
  id: string; title: string; text: string; from: string; to?: string; office?: string; tags: string[];
  status: string; options: Option[]; sources: Source[];
}

let officeTitles: Map<string, string> | undefined;
function offices(): Map<string, string> {
  if (!officeTitles) {
    const doc = readYaml('content/offices.yaml');
    officeTitles = new Map((doc.offices as { id: string; title: string }[]).map((o) => [o.id, o.title]));
  }
  return officeTitles;
}

/** Event text names offices as {prime_minister}; the game fills in the holder. Here it reads "the Prime Minister". */
export function fillOffices(text: string): string {
  return text.replace(/\{([a-z_]+)\}/g, (_m, id: string, offset: number, whole: string) => {
    const title = offices().get(id) ?? id.replace(/_/g, ' ');
    const before = whole.slice(0, offset).trimEnd();
    const startOfSentence = before === '' || /[.!?]$/.test(before);
    return `${startOfSentence ? 'The' : 'the'} ${title}`;
  });
}

const toIso = (v: unknown) => (v instanceof Date ? v.toISOString().slice(0, 10) : String(v ?? ''));

/** Every event in content/events/, by date. Only events that pass the release gate's rule: a sensitive tag needs
 * sensitivity-reviewed (docs/CONTENT_POLICY.md 2.1). Drafts are left out. */
export function events(): GameEvent[] {
  const dir = path.join(repoRoot, 'content/events');
  const sensitive = new Set(['holocaust', 'terror', 'massacre', 'war-casualties']);
  return fs.readdirSync(dir).filter((f) => f.endsWith('.yaml')).map((f) => {
    const d = YAML.parse(fs.readFileSync(path.join(dir, f), 'utf8'));
    return {
      id: d.id, title: d.title, text: fillOffices(d.text), from: toIso(d.from), to: d.to ? toIso(d.to) : undefined,
      office: d.office, tags: d.tags ?? [], status: d.status,
      options: (d.options ?? []).map((o: Option) => ({ id: o.id, text: fillOffices(o.text), historical: !!o.historical })),
      sources: (d.sources ?? []).map((s: Source & { checked?: unknown }) => ({ ...s, checked: s.checked ? toIso(s.checked) : undefined })),
    } as GameEvent;
  }).filter((e) => {
    if (e.status === 'draft') return false;
    const needsReview = e.tags.some((t) => sensitive.has(t));
    return !needsReview || e.status === 'sensitivity-reviewed';
  }).sort((a, b) => a.from.localeCompare(b.from) || a.id.localeCompare(b.id));
}

export interface EventPhoto { event: string; file: string; alt: string; credit: string; commons: string; licence: string }

/** The event-card photographs (archival, never AI) with their credit lines, from the game's own mapping. Only rows whose
 * sensitivity review is done when the photo is marked sensitive, as for a shareable build. */
export function eventPhotos(): Map<string, EventPhoto> {
  const doc = readYaml('unity/Assets/UI/EventImages/event-images.yaml');
  const rows = (doc.images as (EventPhoto & { sensitive?: boolean; sensitivity_reviewed?: boolean })[])
    .filter((r) => !r.sensitive || r.sensitivity_reviewed);
  return new Map(rows.map((r) => [r.event, r]));
}

export interface AssetRule { glob: string; licence: string; source: string; status: string; ai_generated?: boolean; note?: string }

export function assetRules(): AssetRule[] {
  return (readYaml('ASSETS.yaml').rules as AssetRule[]);
}

export const personCategories = ['team', 'historian', 'sensitivity-reviewer'] as const;
export type PersonCategory = (typeof personCategories)[number];

export interface Person {
  github: string;
  name: { en?: string; he?: string };
  title: { en: string; he: string };
  photo?: string | null;
  category: PersonCategory;
  since?: string;
  until?: string;
}

/** The file the contributors page reads. SITE_CONTRIBUTORS (a path from the repo root) swaps in a test fixture. */
export const contributorsFile = process.env.SITE_CONTRIBUTORS || 'site/src/data/contributors.yaml';

export function contributors(): Person[] {
  const doc = YAML.parse(fs.readFileSync(path.resolve(repoRoot, contributorsFile), 'utf8'));
  const people = (doc.people ?? []) as (Omit<Person, 'category' | 'since' | 'until'> & { category?: unknown; since?: unknown; until?: unknown })[];
  const year = /^\d{4}(-\d{2})?$/;
  return people.map((p) => {
    const bad = (msg: string) => { throw new Error(`contributors: ${p.github ?? '(no github)'}: ${msg}`); };
    if (!p.github) bad('no github handle');
    if (!p.title?.en || !p.title?.he) bad('title needs both en and he');
    const category = (p.category ?? 'team') as PersonCategory;
    if (!personCategories.includes(category)) bad(`category must be one of ${personCategories.join(', ')}`);
    // YAML reads a bare 2024 as a number; keep the text.
    const since = p.since == null || p.since === '' ? undefined : String(p.since);
    const until = p.until == null || p.until === '' ? undefined : String(p.until);
    for (const [k, v] of [['since', since], ['until', until]] as const) if (v && !year.test(v)) bad(`${k} must be YYYY or YYYY-MM, not ${v}`);
    if (since && until && until < since) bad('until is before since');
    return { ...p, category, since, until };
  });
}

/** The first `## Heading` section of a Markdown file, without the heading. */
export function markdownSection(rel: string, heading: string): string {
  const lines = read(rel).split('\n');
  const start = lines.findIndex((l) => l.trim() === `## ${heading}`);
  if (start < 0) return '';
  const end = lines.findIndex((l, i) => i > start && /^## /.test(l));
  return lines.slice(start + 1, end < 0 ? undefined : end).join('\n').trim();
}
