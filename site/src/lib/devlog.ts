// Devlog posts: src/content/devlog/<lang>/<date>-<slug>.md, one file per language with the same name.
import type { MarkdownInstance } from 'astro';
import type { Lang } from '../i18n/ui';

interface Frontmatter { title: string; date: string; summary: string }
export interface Post { slug: string; lang: Lang; date: string; title: string; summary: string; Content: MarkdownInstance<Frontmatter>['Content'] }

const files = import.meta.glob<MarkdownInstance<Frontmatter>>('../content/devlog/*/*.md', { eager: true });

export function posts(lang: Lang): Post[] {
  const list = Object.entries(files)
    .map(([path, mod]) => {
      const [, l, name] = path.match(/devlog\/(en|he)\/(.+)\.md$/)!;
      return { slug: name, lang: l as Lang, date: String(mod.frontmatter.date).slice(0, 10), title: mod.frontmatter.title, summary: mod.frontmatter.summary, Content: mod.Content };
    })
    .filter((p) => p.lang === lang)
    .sort((a, b) => b.date.localeCompare(a.date) || b.slug.localeCompare(a.slug));
  return list;
}

// Every post must exist in both languages.
const slugs = (l: Lang) => new Set(Object.keys(files).filter((p) => p.includes(`/devlog/${l}/`)).map((p) => p.split('/').pop()));
for (const s of slugs('en')) if (!slugs('he').has(s)) throw new Error(`devlog: ${s} has no Hebrew version`);
for (const s of slugs('he')) if (!slugs('en').has(s)) throw new Error(`devlog: ${s} has no English version`);
