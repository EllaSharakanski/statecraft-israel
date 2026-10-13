// The Statecraft: Israel website. Static output for GitHub Pages.
// SITE_URL and SITE_BASE set where the site lives. The defaults are the GitHub Pages project URL. With a custom domain,
// build with SITE_URL=https://<domain> SITE_BASE=/ and add public/CNAME (site/README.md).
import { defineConfig } from 'astro/config';
import { fileURLToPath } from 'node:url';

const repoRoot = fileURLToPath(new URL('..', import.meta.url));
const site = process.env.SITE_URL || 'https://eyalgolan.github.io';
const base = process.env.SITE_BASE || '/statecraft-israel';

export default defineConfig({
  site,
  base,
  trailingSlash: 'always',
  output: 'static',
  build: { format: 'directory', inlineStylesheets: 'always' },
  devToolbar: { enabled: false },
  image: { responsiveStyles: false },
  vite: {
    resolve: { alias: { '@repo': repoRoot } },
    server: { fs: { allow: [repoRoot] } },
  },
});
