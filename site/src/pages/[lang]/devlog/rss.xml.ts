// The devlog as an RSS 2.0 feed, one per language: /en/devlog/rss.xml and /he/devlog/rss.xml.
// Written by hand, so the site needs no feed dependency. Every URL is absolute, built from `site` and `base`.
import type { APIRoute } from 'astro';
import { type Lang, languages, href, t } from '../../../i18n/ui';
import { posts } from '../../../lib/devlog';

export const getStaticPaths = () => languages.map((lang) => ({ params: { lang } }));

const esc = (s: string) => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const rfc822 = (iso: string) => new Date(`${iso}T00:00:00Z`).toUTCString();

export const GET: APIRoute = ({ params, site }) => {
  const lang = params.lang as Lang;
  const he = lang === 'he';
  const abs = (p: string) => new URL(p, site).href;
  const page = abs(href(lang, 'devlog'));
  const list = posts(lang);
  // Feed readers that render the description as HTML get the text direction from it, not only from <language>.
  const body = (s: string) => esc(`<p dir="${he ? 'rtl' : 'ltr'}" lang="${lang}">${esc(s)}</p>`);
  const items = list.map((p) => `    <item>
      <title>${esc(p.title)}</title>
      <link>${page}#${p.slug}</link>
      <guid isPermaLink="true">${page}#${p.slug}</guid>
      <pubDate>${rfc822(p.date)}</pubDate>
      <description>${body(p.summary)}</description>
    </item>`);
  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
  <channel>
    <title>${esc(`${he ? 'יומן פיתוח' : 'Devlog'} — ${t(lang, 'site.name')}`)}</title>
    <link>${page}</link>
    <atom:link href="${page}rss.xml" rel="self" type="application/rss+xml" />
    <description>${esc(he ? 'מה השתנה במשחק, מתוך יומן השינויים של המאגר.' : 'What changed in the game, from the repository’s changelog.')}</description>
    <language>${lang}</language>
${list.length ? `    <lastBuildDate>${rfc822(list[0].date)}</lastBuildDate>\n` : ''}${items.join('\n')}
  </channel>
</rss>
`;
  return new Response(xml, { headers: { 'Content-Type': 'application/rss+xml; charset=utf-8' } });
};
