# AI-Assisted Asset Pipeline for an Indie Civ-VII-Style 3D Strategy Game (as of Oct 2026)

Scope note: research done 2026-10-09 with web search and fetch, about 15 tool calls. Many sources are vendor blogs or affiliate listicles, and these are flagged. Vendor pricing changes often, so re-check every price on the vendor page before budgeting.

## Q1. Image-to-3D / text-to-3D tools: quality for game-ready buildings and units, topology, PBR, LOD, licensing, price

### Takeaway
Hosted tools (Tripo, Meshy, Rodin) and open-weight models (Hunyuan3D 2.1, TRELLIS.2) produce usable static props, buildings and blockouts after decimation and cleanup. None reliably outputs final AAA-ready hero assets with clean topology, consistent style and LODs. Plan for human retopology/decimation, UV fixes, texture repainting and engine-side LOD generation. Commercial rights generally require a paid tier. CSM (Cube) is no longer an option: it shut down in January 2026.

### Cited Findings
- **Overall verdicts (conflicting).** One 2026 guide names Tripo the "best hosted for game pipelines" (v3.1 engine, auto-rigging, quad remesh, cheap credits), Meshy 6 the best all-round hosted tool (clean topology options, Low Poly Mode), and Rodin Gen-2.5 the pick for raw detail (10M+ polygon output) — [Cinevva guide](https://app.cinevva.com/guides/ai-3d-model-generators). Other listicles rank them differently — [Medium/Ideas with Wings](https://medium.com/ideas-with-wings/best-ai-3d-model-generators-in-2026-tripo-ai-vs-meshy-rodin-kaedim-and-more-7eea7b05eb11); [Scenario help center comparison](https://help.scenario.com/en/articles/comparing-generative-3d-models/).
- **Meshy.** Meshy 6 and 7 exist, with Meshy 6 Lite as the free option. It offers quad remesh, metallic-roughness PBR, and typically 20k–300k triangles by default. Exports: GLB, FBX, OBJ, STL, USDZ, BLEND. The free tier is 100 credits/month, and free output is **CC BY 4.0 (attribution required)**. Paid plans give private ownership — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **Tripo.** Offers quad remesh/retopology, auto-rigging, PBR, and engine/DCC bridges. Free tier: 200 credits/month, models public and **non-commercial**. Pro: 3,000 credits/month at about $20/mo billed annually ($240/yr), with private models and commercial use — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **Rodin Gen-2.5 (Hyper3D).** Very high polycounts (10M+), with quad options (high-poly quad on the Business plan). Creator plan is $30/mo ($24/mo annual) and Business is $120/mo ($96/mo annual). Downloads and commercial terms require a paid plan — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **Speed and mesh quality (vendor-competitor benchmark, treat with caution).** Rodin takes about 300 s per generation but gives watertight 100k+ poly output. Tripo takes about 15 s and gives uneven 30k–50k poly meshes. Meshy gives quad-dominant meshes but some holes — [Neural4D comparison](https://www.neural4d.com/features/neural4d-vs-tripo-vs-meshy-vs-rodin) (Neural4D is a competing vendor).
- **Hunyuan3D (Tencent).** The 2.1 open weights (June 2025) include a 3.3B shape model and a 2B PBR texture model, plus training code (fine-tunable). The license **allows commercial use but excludes the EU, UK and South Korea**. Hosted 3.0/3.1 are not open-sourced and give 20 free generations/day. Hosted "HY 3D Studio" covers retopology, UVs, texturing, rigging and animation, and an enterprise API is available via Tencent Cloud — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **TRELLIS.2 (Microsoft, Dec 2025).** MIT license for model and code, though "rendering dependencies have separate terms". Full PBR with GLB textures up to 4096². Outputs GLB/OBJ/PLY/Gaussian splats. Needs a capable NVIDIA GPU, and meshes are dense and need decimation — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **CSM (Common Sense Machines).** The Cube platform shut down on 5 Jan 2026 after Google acquired the company, so new projects should not start on it — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **General limits.** Static props, environment pieces and blockouts are production-usable after decimation. Hero characters need manual retopology for animation. Hard-surface models needing exact symmetry may come out "slightly melted". Gaussian splats are about 180 MB per million splats versus 5–20 MB for a comparable mesh, so use meshes for anything interactive — [Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators).
- **Sketchfab** (Epic-owned) has required a "CreatedWithAI" label since 11 Dec 2025 — [Tech-Insider](https://tech-insider.org/steam-ai-disclosure-2026).

### Inferences
- For a Civ-style game, where the camera is mostly top-down at mid distance, AI 3D is strongest for **districts, buildings, wonders as blockouts, props and terrain decorations**. Units and leaders (animated) are the weakest fit and need human retopo/rig, or AI auto-rig only for background units.
- Style coherence (the "Civ VII look") comes from a consistent concept-art front end (Q2), shared texture atlases/trim sheets and a human art-direction pass, not from the 3D generator.
- LODs: none of the sources show reliable LOD output from these tools. Generate LODs in-engine (Unreal Nanite or auto-LOD) or in Blender/Simplygon.
- Licensing: avoid free tiers for shipped assets (CC BY or non-commercial). Hunyuan3D's open-weight license excludes the EU/UK/South Korea, a problem for a worldwide Steam release. TRELLIS.2 (MIT) is the cleanest open option.

### Gaps
- No independent, side-by-side game-readiness benchmark (triangle budgets, UV quality, texel density) for 2026 versions was found. Most rankings are affiliate or vendor content.
- Sloyd and Meshy's current paid prices were not confirmed from the vendor pages.
- Whether Tripo or Meshy provide IP indemnification was not found.

## Q2. Concept art and 2D illustration: GPT Image, Nano Banana Pro, Midjourney, Flux, Seedream; ElevenLabs Image & Video; consistency techniques

### Takeaway
Leading image models are now widely available, including through aggregators such as ElevenLabs' Image & Video product, which exposes GPT Image 2.x, Nano Banana (Gemini 3 Pro image), Seedream 5, FLUX.2, Recraft, Krea and others. ElevenLabs API access requires the Pro plan or above. For consistent leader portraits, multi-reference models (Nano Banana Pro: up to 14 reference images and 5 people) are currently the most practical. Midjourney V8's character-reference features are reportedly not supported on V8.

### Cited Findings
- **ElevenLabs Image & Video models (official docs).**
  - Image: GPT Image 2.5 Sunburst/Flare, GPT Image 2/1.5/1; Nano Banana 2, 2 Lite, Pro and Nano Banana; Krea 2 Medium/Large; Recraft V4.1/V4; Seedream 5 Lite, 5 Pro, 4.5, 4; Kling O1 Image; FLUX.2 [Pro]; FLUX.1 Kontext [Pro]; Runway Gen-4 Image/Turbo; Wan 2.5 Image.
  - Video: Seedance 2.0/2.5, Kling 3.0/O3, Veo 3.1, Runway Gen-4.5/Aleph, LTX 2, Wan 3.0, MiniMax H3, among others.
  - Lip-sync: HeyGen Avatar 4, Sync 3, OmniHuman 1.5, among others.
  - Utility: Topaz Upscale, Background Removal.
  - Source: [ElevenLabs docs – Image & Video](https://elevenlabs.io/docs/overview/capabilities/image-video)
- **ElevenLabs API access.** Only a subset is callable via the API: `gpt-image-2.5-sunburst`, `gpt-image-2.5-flare`, `gpt-image-2`, `gpt-image-1.5`, `gpt-image-1`, `gemini-3.1-flash-image`, `gemini-3.1-flash-lite-image`, `gemini-3-pro-image`, `gemini-2.5-flash-image`, `bytedance-seedream-5-lite`, `bytedance-seedream-5-pro`, and for video Seedance v2/v2.5, Veo 3.1, Creatify Aurora. **API generation requires the Pro plan or above.** ByteDance models are disabled for API by default and need explicit approval. Free users get 3 image requests/day. Credit costs "vary based on selected settings and duration". Many models are "not available in the United States" — [ElevenLabs docs](https://elevenlabs.io/docs/overview/capabilities/image-video).
- **ElevenLabs commercial terms.** ElevenCreative advertises "unified commercial licensing across its native models" — [ElevenLabs Creative](https://www.elevenlabs.io/creative). The image/video docs page does not address commercial terms for third-party models — [ElevenLabs docs](https://elevenlabs.io/docs/overview/capabilities/image-video). Third-party sources say all paid plans include a commercial license, the Free plan does not, and upgrading later does not retroactively license Free-plan output — [diyai.io](https://diyai.io/ai-tools/audio-generation/elevenlabs-pricing/); [licenseorg](https://licenseorg.com/blog/ai-voice-licensing-elevenlabs).
- **ElevenLabs pricing (conflicting third-party figures).** Starter is about $5–6/mo with 30k credits. Creator is listed at $22/mo with either 121k or 100k credits. Scale is listed at $299 or $330/mo — [diyai.io](https://diyai.io/ai-tools/audio-generation/elevenlabs-pricing/). No official Pro-plan price was retrieved.
- **Nano Banana Pro.** Blends up to 14 images while keeping the consistency and resemblance of up to 5 people — [Google blog](https://blog.google/intl/en-mena/product-updates/explore-get-answers/nano-banana-pro-available-globally/). A third party lists 5 characters + 6 objects as references, plus an invisible SynthID watermark on all outputs — [Morphic](https://morphic.com/kr/resources/models/nano-banana-pro).
- **Midjourney.** The official Character Reference doc says to use Omni Reference on V7. A third-party summary of the feature chart says Character Reference is unsupported on V8.1/V8.2, while Style Reference, image prompts and seeds remain supported — [Midjourney docs](https://docs.midjourney.com/docs/character-reference); [blakecrosley guide](https://blakecrosley.com/en/guides/midjourney). Sources conflict on whether V8.1 or V8.2 is the default (one claims V8.2 became the default around 24 July) — [Medium](https://medium.com/@inchristiely/midjourney-v8-2-is-now-the-default-five-things-that-changed-30c8dffcc492).
- **Midjourney commercial terms.** Paid plans run Basic $10, Standard $30, Pro $60, Mega $120 per month, and all include commercial rights. Companies with more than $1M/yr gross revenue must use Pro or Mega. The free trial is CC BY-NC 4.0. Without Stealth Mode (Pro/Mega only), images are public. There is no IP indemnification — [terms.law](https://www.terms.law/2026/01/15/midjourney-commercial-use-rights-complete-2026-guide/index.html); [licenseorg](https://licenseorg.com/blog/midjourney-license-explained).
- **Copyright.** Copyright in purely AI-generated images is uncertain, and substantial human editing strengthens the position — [terms.law](https://www.terms.law/2026/01/15/midjourney-commercial-use-rights-complete-2026-guide/index.html).

### Inferences
- **Leader portraits.** Build a per-leader reference pack (front, 3/4 and profile images plus costume sheet, generated or painted) and feed it as multi-reference input to Nano Banana Pro or GPT Image edits. Lock style with a fixed style board. A human paint-over gives final consistency and strengthens the copyright position.
- **Real historical leaders (e.g., Ben-Gurion, Herzl).** Likeness generation of real people may hit model safety filters and raises publicity-rights questions. Painted or stylised treatment is safer. This was not researched in depth.
- ElevenLabs is a convenient single-bill hub (one subscription for voice, SFX, image and video), but the underlying model licenses (OpenAI, Google, ByteDance) also matter. Get written confirmation of commercial-use coverage for third-party models on the Pro plan.
- For bulk event-card illustrations (thousands), API pipelines (ElevenLabs Pro API, or the OpenAI/Google APIs directly) are needed. Midjourney has no general public API (not verified in this research).

### Gaps
- Official ElevenLabs Pro plan price and per-image credit costs for image models were not found.
- No official FLUX (Black Forest Labs) or Seedream commercial license terms were fetched. FLUX open-weight "dev" models have historically used non-commercial licenses (unverified for 2026).
- OpenAI GPT Image and Google Gemini API output-ownership terms were not fetched directly.

## Q3. Terrain from real elevation data (Israel/Levant)

### Takeaway
Copernicus GLO-30 DEM (30 m, free and open, attribution required) is the standard global source. The workflow is: DEM, then reproject/crop, then Gaea (or World Machine/Houdini) erosion and styling, then the Gaea Unreal node or a 16-bit heightmap, then the engine landscape. For a hex-based Civ-style map, real DEM data is better used to drive hex elevation/biome classification plus a stylised visual terrain than as a 1:1 heightfield.

### Cited Findings
- Copernicus DEM GLO-30 is distributed under a "Full, Free & Open" licence. Required attribution names DLR e.V. (2010–2014) and Airbus Defence and Space GmbH (2014–2018), provided under COPERNICUS by the EU and ESA. Modified data must carry the notice "produced using Copernicus WorldDEM-30 …", and redistributors must include a no-liability statement — [Copernicus License PDF](https://documentation.dataspace.copernicus.eu/APIs/SentinelHub/Data/DEM/resources/license/License-COPDEM-30.pdf); [Google Earth Engine catalog](https://developers.google.com/earth-engine/datasets/catalog/COPERNICUS_DEM_GLO30_2024_1).
- Gaea Unreal Tools: the Gaea "Unreal node" exports PNG or R16 heightmaps with metadata, and the UE5 Gaea Landscape Importer reads that metadata for scale conversion — [QuadSpinner docs](https://eadocs.quadspinner.com/gaea-unreal-tools/importing-terrains).
- Use 16/32-bit heights, because low bit depths cause artifacts on large terrains — [Unigine Gaea heightmap docs](https://developer.unigine.com/docs/future/objects/objects/terrain/landscape_terrain/heightmap_import/heightmap_gaea). A common UE rule of thumb is a 4033×4033 landscape at 1 px = 1 m (about 4 km) — [MotionForge](https://www.motionforgepictures.com/gaea-2-to-unreal-engine-2-ways-to-import-heightmaps-into-unreal-engine/).

### Inferences
- The Israel/Levant extent is about 450 km north–south. At strategy-game scale this is far beyond a 1 m/px landscape, so downsample heavily (e.g., 100–500 m/px) and exaggerate vertical scale. The Dead Sea Rift (about −430 m) and the Hermon (about 2,800 m) are the dramatic features to preserve.
- Copernicus GLO-30 is a surface model, so it includes buildings and vegetation. At strategy scale this is negligible, but smooth it.
- OpenStreetMap (ODbL) can supply rivers, coastlines, roads and city positions for hex classification. ODbL requires attribution and share-alike for derived databases, which needs a legal check if map data ships in-game. Not verified in this research.
- Houdini Heightfields or engine-native tools (UE Landscape, Godot Terrain3D) can do the same job, and Gaea is the lowest-friction option for small teams.

### Gaps
- No sources were fetched on SRTM licensing (generally public domain, NASA/USGS), OSM ODbL implications for games, or Gaea/World Machine current prices.
- No Civ-style hex-terrain-from-DEM case study was found.

## Q4. Audio: ElevenLabs voice, music (ElevenLabs Music, Suno), SFX; Hebrew/Arabic/English; commercial rights

### Takeaway
ElevenLabs voice is commercially usable on any paid plan and supports Hebrew and Arabic (Eleven v3, 74 languages; v4 is now the newest model). **Eleven Music is effectively not licensed for commercial video games on self-serve plans**: it needs an Enterprise agreement or written approval. Suno moved to Warner/BMG-licensed v6 models in September 2026 (single-source report). Downloads now require a paid account, and game-specific terms need checking.

### Cited Findings
- Eleven v3 supports 74 languages, including Arabic and Hebrew. Multilingual v2 (29 languages) includes Arabic but not Hebrew. The help center now names **Eleven v4** as the newest model, and its language list was not verified — [ElevenLabs languages](https://elevenlabs.io/docs/help-center/other/what-languages-do-you-support); [ElevenLabs models FAQ](https://elevenlabs.io/docs/help-center/technical/what-models-do-you-offer-and-what-is-the-difference-between-them).
- A third party says v3 is not intended for real-time use and suits long-form narration and expressive dialogue, with variable quality — [licenseorg](https://licenseorg.com/blog/ai-voice-licensing-elevenlabs) (dated; test Hebrew/Arabic output yourself).
- All paid plans include a commercial license for voice, and Free does not. Voice cloning of other people carries extra restrictions — [licenseorg](https://licenseorg.com/blog/ai-voice-licensing-elevenlabs); [licenseorg guide](https://licenseorg.com/blog/elevenlabs-licensing-guide-ai-voices).
- **Eleven Music restrictions.** Model-specific terms on self-serve plans allow commercial use "except film, TV, radio, & Studio Games". "Studio Games" means commercialised games available on more than one platform — [Eleven Music model-specific terms](https://elevenlabs.io/eleven-music-model-specific-terms). The Eleven Music v1 Terms (updated 4 Aug 2025) prohibit Starter/Creator/Pro/Scale use in commercial video games without prior written approval — [Eleven Music v1 Terms](https://elevenlabs.io/pt/eleven-music-v1-terms). Starter/Creator/Pro are "individual use only", Scale is limited to entities under 10 employees and Business to under 50 — [Eleven Music model-specific terms](https://elevenlabs.io/eleven-music-model-specific-terms). ElevenCreative also notes that music requires an additional license for games — [ElevenLabs Creative](https://www.elevenlabs.io/creative).
- **Suno.** WMG settled in Nov 2025. New licensed models arrive in 2026 and older ones are deprecated, and downloads require a paid account — [Music Business Worldwide](https://www.musicbusinessworldwide.com/warner-music-group-settles-with-suno/). Suno reportedly launched v6, v6-wild and v6-mini in Sept 2026, developed with WMG, BMG and Believe. v6/v6-wild are for Pro/Premier and v6-mini for all. The training catalogues were not disclosed — [ProPakistani](https://propakistani.pk/2026/09/10/suno-launches-licensed-ai-music-models-after-warner-copyright-settlement/) (single source). This conflicts with an August 2026 newsletter saying v6 had not shipped — [roo.beehiiv](https://roo.beehiiv.com/p/suno-v6-update-2026). The September report is newer.

### Inferences
- **Advisor voices and narration:** ElevenLabs (paid plan, designed voices rather than clones of real people) is viable for English, Hebrew and Arabic. Native-speaker QA is essential for nikud/stress errors in Hebrew and dialect (Levantine vs MSA) in Arabic.
- **Music:** do not ship Eleven Music output in a commercial Steam game without written approval or an Enterprise deal. A Steam-only release might arguably fall outside the "more than one platform" Studio Games definition, but the v1 terms are stricter, so get it in writing. Alternatives include Suno paid tiers (verify current game/sync terms), a human composer, or stock libraries. A human-composed main theme also reduces backlash risk.
- **SFX:** ElevenLabs SFX falls under the general paid-plan commercial license. Its terms were not specifically verified.

### Gaps
- Suno's current Terms of Service on video-game use under v6 were not fetched.
- ElevenLabs SFX-specific terms and the v4 language list were not verified.
- Udio, Stable Audio and other music options were not researched.

## Q5. AI for narrative content: thousands of historical event cards with fact-checking

### Takeaway
LLMs can draft thousands of event cards cheaply, but closed-book recall of obscure ("long-tail") history is the weak spot. The defensible approach is retrieval-grounded generation from a curated source corpus, claim-level automated verification, and human historian review of anything flagged or politically sensitive. This matters especially for Israel/Levant history, where errors become reputational and political incidents.

### Cited Findings
- Frontier models hallucinate on about 1.0–2.5% of grounded summaries (Vectara HHEM) and about 4–9% on RAGTruth. Closed-book long-tail facts (obscure historical/local information) reportedly hallucinate at **15–40%** — [Presenc AI, May 2026](https://presenc.ai/research/ai-hallucination-rate-benchmarks-2026) (not traced to a primary study).
- HALoGEN (2025) found error rates up to 86% across 10,923 prompts in 9 domains — [AIMultiple](https://research.aimultiple.com/ai-hallucination).
- An AIMultiple benchmark of 37 LLMs found >15% hallucination when analysing provided statements — [AIMultiple](https://research.aimultiple.com/ai-hallucination).
- A 2026 arXiv preprint describes a four-phase pipeline (intrinsic verification, adaptive search routing, context filtering, claim-level verification) evaluated on TimeQA, FreshQA, HaluEval, MMLU Global Facts and TruthfulQA. It consistently beat zero-shot baselines — [arXiv 2603.17872](https://arxiv.org/html/2603.17872).
- Retrieval-based fact-checkers struggle in languages with limited online coverage — [HalluSearch, arXiv 2504.10168](https://arxiv.org/pdf/2504.10168.pdf). This is relevant for Hebrew/Arabic sources.
- A survey stresses that current factuality metrics are limited and validated external evidence is important — [arXiv 2508.03860](https://arxiv.org/html/2508.03860v2).
- Vendor leaderboards (e.g., Suprmind) use model names and figures not independently confirmed, so do not rely on them — [Suprmind](https://suprmind.ai/hub/ai-hallucination-rates-benchmarks-in-2026/).

### Inferences
Suggested pipeline:
1. Human-curated event list (date, actors, sources).
2. LLM drafts the card text and mechanics only from attached source excerpts.
3. A separate LLM pass extracts atomic claims (dates, names, numbers) and verifies each against the corpus or the web, flagging any unsupported claim.
4. A human historian/editor reviews flagged cards and all cards about contested topics (1948, 1967, the Nakba, settlements, etc.).
5. Store citations per card in data so players and reviewers can see sources.

Under Steam's rules, AI-written narrative text that players read must be disclosed (Q7).

### Gaps
- No benchmark specific to historical facts, or to Middle East history, was found.
- No game case study of LLM-generated historical event content at scale was found.

## Q6. AI coding agents for game dev (Claude Code + engine MCP servers)

### Takeaway
Claude Code can drive Unreal, Godot and Blender through MCP servers (open source and commercial). The Unreal ecosystem is more mature, and Godot's is newer with smaller tool surfaces. Steam does not require disclosure of AI coding assistants.

### Cited Findings
- **UEMCP** (MIT, open source) controls a live UE5 editor via Unreal's built-in Python remote execution, with no C++ plugin to compile. It supports UE 5.0–5.6 — [PyPI uemcp](https://pypi.org/project/uemcp/).
- **runreal unreal-mcp** requires UE 5.4+ and the Python Editor Script Plugin with remote execution — [Playbooks](https://playbooks.com/mcp/runreal-unreal-engine).
- **StraySpark** sells commercial Unreal (UE 5.7/5.8), Godot and Blender MCP servers (vendor source) — [StraySpark guide](https://www.strayspark.studio/blog/mcp-server-game-development-complete-guide-2026).
- **godot-mcp** (open source, LeeSinLiang) supports Claude Code, Cursor and Codex, with project launch, scene editing and error capture — [Glama](https://glama.ai/mcp/servers/@LeeSinLiang/godot-mcp/blob/f2423ba8da17e2fc3bf8235c2ea02a6c3bac5762/README.md).
- StraySpark says Godot's MCP ecosystem is newer than Unreal's, and that GDScript's looser typing makes agent calls fail more often. It describes Godot MCP + Blender MCP in one Claude Code session, where the agent models a prop, exports it and places it (vendor claim) — [StraySpark Godot guide](https://www.strayspark.studio/blog/godot-mcp-setup-claude-code-2026).
- Steam's disclosure exempts AI coding assistants and efficiency tools — [Steamworks Content Survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey); [legalmoveslawfirm](https://legalmoveslawfirm.com/steam-ai-policy/).

### Inferences
- A Civ-style game is mostly systems code (hex grid, AI players, turn resolution, UI, data-driven content). This suits coding agents well, especially with a strongly typed codebase (C++/C# or typed GDScript) and good tests.
- Use MCP for editor automation (placing assets, batch-importing AI meshes, configuring LODs). Vet open-source MCP servers for maintenance and security before granting editor access.

### Gaps
- Unity MCP options and current Claude Code pricing/limits were not researched.
- No quantified productivity data for game projects was found.

## Q7. Steam AI disclosure policy (2026), community perception, how to disclose

### Takeaway
Steam allows AI content but requires disclosure in the Content Survey. Disclosure covers pre-generated AI content that players consume (art, sound, narrative, localization, and also marketing/store assets) and live-generated content (with a description of guardrails). The 16–17 January 2026 revision explicitly exempts efficiency tools such as coding assistants. Player backlash is real and is triggered mostly by visible, low-quality AI. Disclosure is mandatory, and visible quality is the main risk lever.

### Cited Findings
- **Official Steamworks wording.**
  - Pre-generated content is "created with the help of AI tools during development". Developers promise no illegal or infringing content, and Valve evaluates AI output "the same way we evaluate all non-AI content".
  - Live-generated content additionally requires telling Valve "what kind of guardrails you're putting on your AI".
  - Live-generated Adult Only sexual content is not allowed.
  - "Efficiency gains through the use of these tools is not the focus of this section."
  - Source: [Steamworks Content Survey docs](https://partner.steamgames.com/doc/gettingstarted/contentsurvey)
- **Timeline.**
  - Introduced January 2024.
  - Revised around 16–17 January 2026 to narrow the scope (dates differ by outlet).
  - Disclosure produces a public store label and carries no ranking penalty.
  - AI-made store/marketing material must be described in "About This Game".
  - Sources: [Tech-Insider](https://tech-insider.org/steam-ai-disclosure-2026); [Notebookcheck](https://www.notebookcheck.net/Steam-updates-AI-disclosure-form-requiring-developers-to-report-visible-and-in-game-AI-but-not-background-tools.1206103.0.html); [Gigazine](https://gigazine.net/gsc_news/en/20260120-steam-updates-ai-disclosure-guidelines)
- **Unconfirmed June 2026 change.** A single-source claim (Remio.ai, via Tech-Insider) says a June 2026 update requires AI metadata tags for art, code, music and text and blocks releases that skip tagging. **Unconfirmed**, and it conflicts with the coding exemption — [Tech-Insider](https://tech-insider.org/steam-ai-disclosure-2026).
- **Enforcement.** Steam has added an overlay tool for players to report problematic AI content. Developers remain liable — [Gigazine summary via search](https://gigazine.net/gsc_news/en/20260120-steam-updates-ai-disclosure-guidelines); [legalmoveslawfirm](https://legalmoveslawfirm.com/steam-ai-policy/).
- **Adoption.**
  - 7,818 games disclosed AI use by July 2025, about 7% of roughly 115,000 titles.
  - PCGuia reported that 40% of games launched in one June 2026 week disclosed AI use.
  - Epic Games Store has no disclosure requirement, and Tim Sweeney argues labels stigmatise small developers.
  - Source: [Tech-Insider](https://tech-insider.org/steam-ai-disclosure-2026)
- **Backlash cases.**
  - Vapor World: Over The Mind (Aug 2026) had about 100k wishlists, then replaced in-engine cutscenes with AI-generated ones with inconsistent character models. It launched at 36% positive on 64 reviews — [Everything Edinburgh](https://everythingedinburgh.com/games/news/vapor-world-steam-ai-cutscene-backlash/).
  - Shrine's Legacy devs were falsely accused of "AI slop" in Dec 2025 — [Outlook Respawn](https://respawn.outlookindia.com/gaming/gaming-news/shrines-legacy-devs-deny-ai-slop-chatgpt-rumors).
- **Review-score data.** A Game Oracle study of 16,554 Steam games from 2025 reportedly found 21% used generative AI, with 53% poorer first-month review scores. This is secondary reporting, so verify it — [SpriteCook blog, June 2026](https://www.spritecook.ai/blog/is-ai-art-ok-in-indie-games) (vendor blog).
- **What players object to.** Players object to visible shortcuts, not to backend tooling — [SpriteCook](https://www.spritecook.ai/blog/is-ai-art-ok-in-indie-games); [Everything Edinburgh](https://everythingedinburgh.com/games/news/vapor-world-steam-ai-cutscene-backlash/).

### Inferences
How to disclose:
- In the Content Survey, tick pre-generated content and list the categories specifically and honestly, for example: "3D building base meshes generated with AI then retopologised and textured by our artists; advisor voices synthesised with ElevenLabs from designed (not cloned) voices; event-card drafts written with AI assistance and fact-checked by historians."
- Mention any AI marketing art in "About This Game".
- If no runtime LLM is used, say so, since that avoids the live-generated guardrail requirement.

Risk management:
- Keep AI out of the most visible "hero" surfaces (key art, capsule, trailer, leader portraits, main theme), or human-finish them heavily.
- Under-disclosure that is later discovered is likely worse than honest disclosure, given player detective behaviour and the reporting tool.
- An Israel-themed game is already a magnet for review-bombing, so AI slop accusations would compound political backlash.

### Gaps
- The exact current Steam form fields and store-label text were not seen in a primary source. Steamworks docs did not mention how the store label displays.
- Valve's January 2026 announcement post was not fetched directly.
- The Game Oracle primary report was not found.
