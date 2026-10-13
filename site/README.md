# The Statecraft: Israel website

A static site built with [Astro](https://astro.build), in English (`/en/`) and Hebrew (`/he/`, right to left). It is meant for GitHub Pages and is not deployed yet (issue #393).

## Build and preview

You need Node 22.12 or later.

```
cd site
npm ci --ignore-scripts
npm run build      # astro build, then scripts/check-site.mjs
npm run preview    # http://localhost:4321/statecraft-israel/en/
```

`npm run build` fails when a page exists in one language only, when an image or video in a page has no media label, or when anything would load from another site.

## Where things are

- `src/pages/[lang]/`: one file per page; each renders both languages.
- `src/i18n/ui.ts`: interface strings in both languages. The build fails if a Hebrew string is missing.
- `src/content/<lang>/*.md`: longer page text. `src/content/devlog/<lang>/`: devlog posts, the same file name in both languages.
- `src/lib/media.ts`: every image and video the site shows, with its label. The images are read from `docs/images/` and `promo/final/` and converted to AVIF and WebP at build time; they are not copied into `site/`.
- `docs/images/captures.yaml`: the in-engine screenshots. The site's hero and in-engine gallery are the README's `docs/images/build-*.jpg`, described by this file (build, date, title and alt text in both languages; the first entry is the hero). The build fails if a screenshot has no entry or an entry has no file.
- `src/lib/repo.ts`: reads `content/events/`, `ASSETS.yaml` and the event photographs' mapping at build time. The History and Sources pages show that content as it is.
- `src/data/contributors.yaml`: the Contributors grid. Photos go in `src/assets/contributors/`, each with an `ASSETS.yaml` row and the person's consent. Never a GitHub avatar, never a generated image of a person.

## Refreshing the screenshots

The site always shows the README's screenshots. To refresh both, retake `docs/images/build-*.jpg` (same file names), then set `build` and `date` in `docs/images/captures.yaml`; the labels read "In-engine capture — <build>, <date>". A new screenshot needs an entry with its alt text in both languages.

Visual changes are about to land: new towns with a much better Tel Aviv (#339), warmer land (#392) and the tilt-shift change that keeps far towns readable (#390). Once they are merged, the README screenshots get retaken once and the site picks them up through `captures.yaml`, with no change under `site/`.

## Labels

Every image and video carries one of these labels where it appears, in both languages:

- In-engine capture — the build and date;
- Concept art — AI-generated;
- Cinematic promo — AI-generated, not gameplay;
- Archival photograph, with its credit.

The first image and the first video on the home page are in-engine captures. AI promo video appears only in the Media page's cinematic gallery.

## Deploying

`.github/workflows/site.yml` builds every pull request that touches the site or what it reads. Deploying from `main` needs two owner steps: enable GitHub Pages with "GitHub Actions" as the source, and set the repository variable `SITE_DEPLOY` to `true`.

The site lives at `https://eyalgolan.github.io/statecraft-israel/` until there is a domain. For a custom domain: add `public/CNAME` with the domain, set the repository variables `SITE_URL` to `https://<domain>` and `SITE_BASE` to `/`, and set the domain in the Pages settings. No page needs to change: every link and hreflang URL is built from those two values.

## Hosting the game here

GitHub Pages cannot set response headers, so the browser build must use Unity's Decompression Fallback and stay single-threaded. The Play page explains this; today `Build.Web` turns the fallback off.
