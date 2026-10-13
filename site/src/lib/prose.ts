// Long page text: src/content/<lang>/<name>.md. Both languages must exist.
import type { MarkdownInstance } from 'astro';
import type { Lang } from '../i18n/ui';

const files = import.meta.glob<MarkdownInstance<Record<string, unknown>>>('../content/{en,he}/*.md', { eager: true });

export function prose(lang: Lang, name: string) {
  const mod = files[`../content/${lang}/${name}.md`];
  if (!mod) throw new Error(`prose: missing src/content/${lang}/${name}.md`);
  const other = files[`../content/${lang === 'en' ? 'he' : 'en'}/${name}.md`];
  if (!other) throw new Error(`prose: ${name}.md exists in one language only`);
  return mod;
}
