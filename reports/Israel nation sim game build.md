# Govern Israel's Century Through Systems, Dioramas, Restraint

A small team using AI coding agents can ship this game if it treats it as three separable problems. The first is a **data-driven simulation**, a Democracy-style graph of ministries with a Suzerain-style layer of authored decision scenes and Paradox-style history scripting. The second is a **diorama hex map**, built in Unity 6.3 LTS (URP) on top of an engine-independent, deterministic C# core. The third is a **content and legitimacy pipeline**, where AI drafts and human historians from several communities sign off. The research favors a stylized-realistic "readable realism" look over a true Civ VII clone. Firaxis's look rests on a AAA art team and a proprietary engine, but its techniques (splat-blended terrain, instanced scatter, modular city tiles, per-era lighting) are public and reproducible. Of the 140-plus years, **17 historical eras** group naturally into six chapters. The player starts as a non-sovereign, multi-headed proto-government (Zionist Organization, then Jewish Agency, Va'ad Leumi, Histadrut and Haganah) and becomes a coalition-bound prime minister after 1948. The Holocaust works only as a hard historical anchor that shapes immigration, politics and memory. It is never a system the player can optimize. Platform evidence shows that removals follow **glorification of violence against an identifiable side or a government request**, not the subject matter itself. A multi-perspective governance sim is therefore commercially viable on Steam. It still faces predictable risks: lost MENA markets, German rating rules, Israeli publicity and emblem law, two-sided review bombing, and a modest commercial ceiling. The 2023–2026 period is the hardest content problem. The Iran war and the 27 October 2026 election are still unresolved, so any "today" content will be out of date on the day it ships.

## Three proven systems stack into one ministry simulator

No single shipped game covers this brief. Four precedents each solve one layer, and they combine cleanly.

**Democracy 4 supplies the ministry engine.** Positech's game treats every policy, situation, voter group and event as a node in a "neural network" of effects defined in CSV files. Each effect has a formula and an **inertia** value, which averages a change over recent turns. Inertia 4, for example, means a change takes up to four turns to fully apply ([Positech: Modding](https://www.positech.co.uk/democracy4/modding.html)). Policy effects are reduced while the policy is only partly implemented and are **scaled by the relevant minister's competence**. "Situations" such as crises and booms use separate start and stop thresholds (hysteresis), so they do not flicker on and off ([Positech: Situations](https://www.positech.co.uk/democracy4/mod_situations.html); [Positech: Policies](https://www.positech.co.uk/democracy4/mod_policies.html)). This maps almost one-to-one onto the brief's ten ministries:

- **Education, health, infrastructure, legal, security, tourism, foreign relations, economy and aliyah** each become a "department" cluster of sliders and situations on one shared variable graph.
- **"Politics" is the coalition layer** that sits on top of that graph.
- **Inertia expresses delayed consequences.** Education spending can take 20 or more turns to reach GDP, while a security operation takes effect in one or two.
- **The minister-competence multiplier becomes coalition politics.** Partners demand ministries, and a weak minister drags that ministry down.

Cliff Harris's own warning sets the main cost: one equation change can "affect dozens, maybe hundreds" of other nodes ([Positech](https://www.positech.co.uk/democracy4/mod_policies.html)). The simulation therefore needs automated balance tests from day one, which is work AI agents do well.

**Voters must be Israeli blends, and before 1948 there are no voters.** Democracy 3 models each voter as a mix drawn from 21 overlapping groups ([Steam: Democracy 3](https://store.steampowered.com/app/245470)). For Israel the useful groups are overlapping identities such as Ashkenazi or Mizrahi, Haredi, national-religious, secular, Arab citizens, FSU and Ethiopian olim, kibbutzniks, settlers, reservists and periphery residents. Blending them lets a "Mizrahi, traditional, periphery, reservist" voter feel cross-pressure. The pre-state era needs a different legitimacy model. The Yishuv's self-government was "multi-headed": an elected Assembly of Representatives, the Va'ad Leumi, the Jewish Agency, a Histadrut that also ran health care (Kupat Holim), a bank and construction (Solel Boneh), and a Haganah under only nominal civilian control ([Wikipedia: Mandatory Palestine](https://en.wikipedia.org/wiki/Mandatory_Palestine); [Wikipedia: Histadrut](https://en.wikipedia.org/wiki/Histadrut)). The design implication is a shift from command to persuasion. Before 1948 the player negotiates with the Histadrut, Diaspora donors, the Ottoman or British sovereign, and the dissident militias. The pre-state resources are immigration certificates, donor money, land dunams and Haganah strength. After 1948 the model switches to coalition arithmetic: no Knesset party has ever won 61 seats ([JVL electoral history](https://www.jewishvirtuallibrary.org/israeli-electoral-history)).

**Paradox supplies the history layer, and Civ VII supplies a warning.** Victoria 3 moved scripted content from country-specific popups to condition-gated **Journal Entries** that "present the situation both narratively and mechanically" ([Paradox DD31](https://admin-forum.paradoxplaza.com/forum/developer-diary/victoria-3-dev-diary-31-journal-entries.1508059)). Players complained that this lost iconic history: "you didn't get the US civil war, you got a random radicalization" ([Steam discussion](https://steamcommunity.com/app/529340/discussions/0/5675129542132329045)). Hearts of Iron IV's "historical focus" toggle keeps the AI on history while leaving the player free ([Steam discussion](https://steamcommunity.com/app/394360/discussions/0/1698293703771116061)). A game about a single nation can afford HoI4-depth content for that one country, which removes most of Vic3's "thin history" problem.

Civ VII's Ages are the cautionary case. The game split play into three Ages with forced resets and civ swaps. It sat at roughly **48% positive on Steam** months after launch, with most complaints aimed at the abrupt transitions, and it fell below 5k concurrent players by May 2025 ([Wikipedia: Civilization VII](https://en.wikipedia.org/wiki/Civilization_VII)). Firaxis's May 2026 "Test of Time" update let players "start and stay as any civ in any Age" and replaced Legacy Paths with flexible "Triumphs" ([Firaxis patch notes](https://support.civilization.com/hc/en-us/articles/51685242291219-Civilization-VII-Patch-Notes-May-19-2026)). Daily reviews then averaged about 55% positive ([Forbes, via search summary](https://www.forbes.com/sites/barrycollins/2026/05/31/civilization-vii-finally-passes-test-of-time-as-review-scores-rise/)). The lesson: **eras should be chapters that carry the polity forward.** Institutions, demographics, debts and grievances persist. Era-ending crises can still borrow Civ VII's "Crisis Policies," the forced negative slots, for things like Tzena rationing, emergency regulations or reserve mobilization. They must depend on how well the player prepared, or they will read as "boring and meaningless," as players said of Civ VII's crises ([CivFanatics forum](https://forums.civfanatics.com/threads/civ-7-in-2026.701129/)).

**Suzerain, Frostpunk and PeaceMaker shape the decision feel.** Suzerain tracks consequences across a massive branching text with articy:draft ([articy case study](https://www.articy.com/en/?p=4725)). It is the model for the one to three pivotal authored scenes per era, such as the Altalena, the reparations vote or the 1967 waiting period. Its known cost is long blocks of text ([GamingTrend](https://gamingtrend.com/feature/reviews/youve-got-my-vote-suzerain-review)).

Frostpunk's permanent laws carry moral weight. Critics showed how its two meters reduce morality to optimization: "how much more discontent he can take" ([indienova](https://lab.indienova.com/indie-game-review/critical-play-report-frostpunk/); [Unwinnable](https://unwinnable.com/2021/12/10/authoritarianism_in_frostpunk/)). Israel's charged legal choices therefore need qualitative human-cost presentation as well as numbers. Examples are military government over Arab citizens (1948–66), administrative detention, the Haredi draft and judicial reform.

PeaceMaker (2007) is the closest direct precedent. It runs Israeli and Palestinian opinion meters from −100 to +100, with sub-polls and scenarios seeded from real historical event sequences. Its documented failure is that players "find an effective policy set and repeat it" ([Wikipedia: PeaceMaker](https://en.wikipedia.org/wiki/PeaceMaker); [Mechanics of Magic](https://mechanicsofmagic.com/2024/09/30/introducing-serious-games-peacemaker/)). That argues for diminishing returns, adaptive adversaries in the style of Rebel Inc. ([Ndemic](https://www.ndemiccreations.com/en/54-rebel-inc-escalation)), and era shifts that make old strategies obsolete.

**Readability is a first-class system.** Vic3 adopted CK3's nested tooltips so players don't need "an econ degree" ([PCGamesN](https://www.pcgamesn.com/victoria-3/nested-tooltip-system)). It was still criticized for notifications that piled up and for blocking actions without explaining why ([GameCritics](https://gamecritics.com/mitch-zehe/victoria-3-review/)). Civ VII's launch UI was "almost universally" criticized on Steam ([TheSixthAxis](https://www.thesixthaxis.com/2025/02/07/firaxis-highlights-three-areas-theyll-improve-civilization-7-after-mixed-launch-reviews/)). A ten-ministry game needs four things:

1. Every computed number decomposes into its graph terms, for example "+3 from Ulpan funding, −2 from housing shortage."
2. Notifications are triaged instead of stacked.
3. Every blocked action states why it is blocked.
4. In-character ministers voice warnings.

### Time structure: annual turns with crisis zoom

A span of 1882–2026 at quarterly turns runs to about 580 turns, which is too long. Annual turns, plus "crisis zoom" episodes that drop to weeks or days for 1929, 1948, 1967, 1973, 1991 and 2023, keep a campaign to about 145 turns plus episodes. No shipped game is documented to vary turn length by era, so this is a design bet, not a proven pattern. History should come in three grades:

- **Hard anchors** always happen, but their outcomes depend on the game state: WWI and the Ottoman collapse, the Holocaust, the UN partition vote, and wars started by neighbors.
- **Soft anchors** are Journal Entries with preconditions: Balfour, Sèvres, Camp David.
- **Counterfactual branches** are focus-tree alternatives: the Uganda Scheme, accepting Peel, a different 1977 election.

A visible "Historical Track" indicator should tell players when they have diverged and why.

## Seventeen eras collapse into six chapters, each with a decision spine

The event catalog divides the period into 17 playable eras. They fall into two phases. Before 1948 the player runs a voluntary proto-government under an imperial sovereign. From May 1948 the player is a prime minister constrained by a coalition, the courts, the Attorney General and, from 1967–73 on, US patronage. The table below groups them into the six campaign chapters the research proposes.

| Chapter | Eras | Who the player is | Anchor events and crises |
|---|---|---|---|
| **Building** (1882–1948) | 1 Ottoman First/Second Aliyah (1882–1914); 2 WWI & Balfour (1914–20); 3 Early Mandate (1920–29); 4 Fifth Aliyah, Arab Revolt, White Paper (1929–39); 5 WWII, Holocaust, revolt (1939–47) | ZO Executive / Palestine Office, then the Jewish Agency with the Va'ad Leumi, Histadrut and Haganah | Uganda Scheme; Ottoman expulsions; Balfour; 1920/1921/1929 riots; Peel; 1939 White Paper; Struma and Patria; Biltmore; Saison; King David Hotel; Aliyah Bet; UNGA 181 |
| **Founding** (1947–67) | 6 1947–49 War / Nakba; 7 Mass immigration & austerity (1949–56); 8 Suez & state-building (1956–67) | Provisional government, then Mapai-led coalitions | Declaration vote (6–4); Altalena; armistice; ma'abarot; Law of Return; reparations (61–50); Qibya; Lavon; Kasztner; Sèvres; Kafr Qasim; Dimona; Eichmann; Water Carrier |
| **Holding** (1967–85) | 9 Six-Day War & aftermath; 10 Meir, Munich, Yom Kippur (1970–77); 11 Begin era (1977–84) | PM of national-unity or Likud governments | Waiting period; territories decision; War of Attrition; Agranat; Entebbe; Land Day; Camp David; Yamit; Osirak; Lebanon 1982; Kahan; bank-shares crisis |
| **Opening** (1984–2000) | 12 Unity governments, stabilization, First Intifada; 13 Oslo decade | Rotation and Labor/Likud PMs | 1985 stabilization; Pollard; London Agreement; Gulf War restraint; FSU aliyah; Operation Solomon; Oslo; Rabin assassination; Versailles collapse; Lebanon pullout |
| **Managing** (2000–22) | 14 Second Intifada, Disengagement, Lebanon 2006; 15 Netanyahu era, Abraham Accords (2009–21) | Sharon/Olmert/Netanyahu/Bennett-Lapid | October 2000; Defensive Shield; disengagement; Winograd; Carmel fire; 2011 protests; Shalit deal; COVID; Abraham Accords; Meron; May 2021 |
| **Reckoning** (2022–26) | 16 Judicial overhaul crisis; 17 October 7 and the multi-front war | Coalition PM, then a caretaker PM before an election | Reasonableness law; Gallant firing; Oct 7; hostages; Hezbollah; Twelve-Day War; 2026 Iran war; Board of Peace; Haredi draft; pardon; 27 Oct 2026 election |

Sources for the table: era entries in [Wikipedia: Mandatory Palestine](https://en.wikipedia.org/wiki/Mandatory_Palestine), [1948 Palestine war](https://en.wikipedia.org/wiki/1948_Palestine_war), [Six-Day War](https://en.wikipedia.org/wiki/Six-Day_War), [Yom Kippur War](https://en.wikipedia.org/wiki/Yom_Kippur_War), [Oslo Accords](https://en.wikipedia.org/wiki/Oslo_Accords), [2023 judicial reform](https://en.wikipedia.org/wiki/2023_Israeli_judicial_reform) and [2026 Israeli legislative election](https://en.wikipedia.org/wiki/2026_Israeli_legislative_election).

**The decision spine already exists in the historical record.** The catalog lists about 35 dilemmas that had real alternatives at the time, each suited to an authored scene with branches. A few show the range:

- **1933, the Haavara Agreement.** Sign the deal with Nazi Germany, which brought in about 60,000 people and their capital, or join the boycott ([Haavara](https://en.wikipedia.org/wiki/Haavara_Agreement)).
- **1937, Peel partition.** Accept it in principle, as Ben-Gurion and Weizmann did, or reject it ([Peel Commission](https://en.wikipedia.org/wiki/Peel_Commission)).
- **1948, the declaration.** Declare the state or accept the US-urged trusteeship; the vote was 6–4 to declare ([Declaration](https://en.wikipedia.org/wiki/Israeli_Declaration_of_Independence)).
- **1948, the Altalena.** Shell the ship and get a single army, or risk civil war ([Altalena](https://en.wikipedia.org/wiki/Altalena_Affair)).
- **1952, German reparations.** DM 3 billion, approved 61–50 amid Begin-led riots ([Reparations](https://en.wikipedia.org/wiki/Reparations_Agreement_between_Israel_and_the_Federal_Republic_of_Germany)).
- **6 October 1973.** Elazar urged a pre-emptive strike and Dayan opposed it ([Yom Kippur War](https://en.wikipedia.org/wiki/Yom_Kippur_War)).
- **1985, stabilization.** Shock stabilization or dollarization ([Stabilization plan](https://en.wikipedia.org/wiki/1985_Israel_Economic_Stabilization_Plan)).
- **1991, Iraqi Scuds.** Restraint or a strike on western Iraq ([Gulf War](https://en.wikipedia.org/wiki/Gulf_War)).
- **2005, Gaza.** Unilateral disengagement despite the Likud referendum ([Disengagement](https://en.wikipedia.org/wiki/Israeli_disengagement_from_Gaza)).
- **2011, the Shalit deal.** 1,027 prisoners released, including Yahya Sinwar ([Shalit exchange](https://en.wikipedia.org/wiki/Gilad_Shalit_prisoner_exchange)).
- **2020, annexation or normalization.** Normalization won ([Abraham Accords](https://en.wikipedia.org/wiki/Abraham_Accords)).

Each dilemma should move several meters at once. The Shalit deal, for example, gives an immediate morale gain and carries a deferred security cost that the simulation remembers. Two rows are **historiographically contested**: Sadat's 1971 overture as a "missed peace," and Lausanne's "100,000 offer." They should be presented as contested, not as clean branches.

**Disasters and scandals point to a reusable "commission of inquiry" mechanic.** Many of them ended in a state commission:

- The **Versailles wedding-hall collapse** (1999, 23 killed; the Pal-Kal building method was banned) ([Wikipedia](https://en.wikipedia.org/wiki/Versailles_wedding_hall_disaster)).
- The **Carmel fire** (2010, 44 killed; the fire service was reformed) ([Wikipedia](https://en.wikipedia.org/wiki/Mount_Carmel_forest_fire_(2010))).
- The **Meron crush** (2021, 45 killed; personal responsibility assigned to the PM and others) ([Wikipedia](https://en.wikipedia.org/wiki/2021_Meron_crowd_crush)).
- The Agranat and Kahan commissions, and the Or Commission after October 2000.

The disaster card offers a recurring choice. An **independent state commission** gives high legitimacy and high political risk. A **government probe** stays under the player's control but carries low legitimacy. The contemporary version is the dispute over a commission of inquiry into October 7 ([2026 election page](https://en.wikipedia.org/wiki/2026_Israeli_legislative_election)). The cards also spread the stress across ministries:

- **Infrastructure and Interior** take the collapses and fires.
- **Health** takes the pandemic: three COVID lockdowns and the vaccine-for-data deal ([Wikipedia](https://en.wikipedia.org/wiki/COVID-19_pandemic_in_Israel)).
- **Finance** takes the 1983 bank-shares collapse ([Wikipedia](https://en.wikipedia.org/wiki/1983_Israel_bank_stock_crisis)).
- **Justice** takes Kav 300 ([Wikipedia](https://en.wikipedia.org/wiki/Kav_300_affair)).

**The Cold War is a patron meter, not a backdrop.** Israel's patrons changed in stages ([Israel–Soviet relations](https://en.wikipedia.org/wiki/Israel%E2%80%93Soviet_Union_relations); [France–Israel](https://en.wikipedia.org/wiki/France%E2%80%93Israel_relations); [Israel–US](https://en.wikipedia.org/wiki/Israel%E2%80%93United_States_relations); [MASHAV](https://en.wikipedia.org/wiki/MASHAV)):

1. **Soviet and Czechoslovak support and arms in 1948.** The USSR recognized Israel de jure on 17 May 1948.
2. **A Soviet tilt to the Arabs** after 1953–55, ending in a break in relations in 1967.
3. **France as main arms supplier, 1954–67,** including Dimona, followed by de Gaulle's embargo.
4. **US Hawk missiles in 1962,** Phantoms in 1968, and the 1973 airlift.
5. **African outreach through MASHAV,** which collapsed after 1973.

Victoria 3's lesson about "Interests" applies here. Paradox made them a requirement for all diplomacy only after playtests showed they felt insignificant ([Paradox DD58](https://www.paradoxinteractive.com/games/victoria-3/news/dev-diary-58-interest-revisions)). A per-power score for arms reliability and UN diplomatic cover must therefore gate real things: arms deals, immigration agreements such as Soviet exit visas and Jackson–Vanik, loan guarantees, and votes.

**Multi-voice means distinct actors with their own meters.** The catalog pairs every pivotal Israeli event with its Arab or Palestinian framing. For Palestinians, Balfour was a promise made without the consent of a roughly 90% Arab majority. 1936–39 was the formative national revolt. 1948 is the **Nakba**: about 700,000 people displaced (estimates run from 520,000 to over 750,000) and more than 400 villages depopulated. 1967 is the **Naksa** ([Wikipedia: Nakba](https://en.wikipedia.org/wiki/Nakba); [Naksa](https://en.wikipedia.org/wiki/Naksa)). The causes of 1948 should be presented as contested. Morris describes mixed causes with no master plan. Khalidi and Pappé argue for systematic expulsion. Older Israeli narratives stressed calls from Arab leaders to flee ([1948 expulsion and flight](https://en.wikipedia.org/wiki/1948_Palestinian_expulsion_and_flight)). The research recommends modeling the PA/Fatah, Hamas, PIJ and Arab citizens of Israel as **separate actors**, each with its own legitimacy. Merging them would erase dynamics such as Ra'am joining the 2021 coalition and the rivalry between the PA and Hamas. Egypt, Jordan, Syria, Iran, Hezbollah, the Gulf states, Turkey, the Ottomans and Britain each need AI agency of their own.

**"Today" is a moving target.** As of 9 October 2026:

- **Gaza:** the Trump-brokered ceasefire took effect on 10 October 2025, and the last living hostages were freed on 13 October ([NBC News](https://www.nbcnews.com/world/middle-east/know-abpagreed-first-phase-gaza-truce-plan-rcna236598?rand=26684)). Phase Two, under a Board of Peace, has stalled over Hamas disarmament ([FDD](https://www.fdd.org/analysis/2026/01/16/phase-two-of-the-gaza-ceasefire-announced-despite-core-obstacles-still-to-overcome/)).
- **Iran:** a US–Israel war that began on 28 February 2026 killed Khamenei. It went through ceasefires and collapses, and hostilities were renewed in September ([Wikipedia: 2026 Iran war](https://en.wikipedia.org/wiki/2026_Iran_war)).
- **Election:** set for **27 October 2026**. The Supreme Court reversed the disqualification of the Arab parties on 2 October ([2026 election](https://en.wikipedia.org/wiki/2026_Israeli_legislative_election)).

The catalog suggests a strong "present day" scenario: a caretaker PM weeks before an election, facing shipping shocks, reservist burnout, the Haredi draft and international legal exposure. Any such scenario will be out of date within weeks of launch.

## Unity URP and a pure C# core give agents the most leverage

The engine survey ranks four options for this use case: a tiny team, agent-written code, a stylized hex map, and Steam on Windows, Mac and Deck.

| Rank | Stack | Why | Costs and risks |
|---|---|---|---|
| 1 | **Unity 6.3 LTS, URP, C#** | Genre precedent: Humankind, Endless Legend and Old World are on Unity, and Old World moved to 6.3 in Feb 2026. Catlike Coding's 27-part Hex Map series is a direct blueprint. Official MCP server (open beta May 2026). Batchmode CLI builds and tests. UI Toolkit uses CSS-like USS. | Free under $200k revenue; Pro $2,310/seat/yr from Jan 2026. MCP needs Unity Cloud and a subscription or trial. Scene YAML is noisy for agents. |
| 2 | **Godot 4.6** | Text-native scenes and shaders, a tiny headless binary, MIT license, the Terrain3D plugin, and community MCP servers including GUT test runners. | No shipped high-fidelity 3D 4X. Less mature post-processing. LLMs confuse Godot 3 and Godot 4 syntax. |
| 3 | **Three.js/Babylon + TypeScript in Electron** | Highest agent leverage, with free HTML/CSS UI, which is how Civ VII's UI is built. | No high-visual 3D 4X precedent. WebGPU maturity is disputed. Deck overhead. |
| 4 | **Unreal 5.7/5.8** | Best raw rendering. | Binary Blueprints and materials. Official MCP is "minimal." Weak Mac and Deck performance reports. 5% royalty above $1M. |

Sources: [Wikipedia: Humankind](https://en.wikipedia.org/wiki/Humankind_(video_game)); [Wikipedia: Endless Legend](https://en.wikipedia.org/wiki/Endless_Legend); [rutab.net on Old World](https://rutab.net/b/video-games/2026/02/19/razrabotchik-civilization-4-vypustil-krupnoe-obnovlenie-dlya-svoey-strategii-old-world.html); [Catlike Coding](https://catlikecoding.com/unity/tutorials/hex-map/); [Unity MCP blog](https://unity.com/blog/unity-ai-mcp-how-to-get-started); [Unity pricing](https://unity.com/products/pricing-updates); [Godot 4.6](https://gamefromscratch.com/godot-4-6-released/); [godot-mcp](https://dev.co/ai/mcp/godot-mcp); [StraySpark on UE MCP](https://www.strayspark.studio/blog/epic-official-mcp-plugin-ue5-8-vs-third-party); [Unreal FAQ](https://www.unrealengine.com/en-US/faq?active).

**Unreal's main advantages do not apply to this map.** A Civ-style map of Israel and the region is bounded, about 100×60 to 200×120 hexes, and viewed by a tilted camera. Nanite and Lumen solve open-world problems. Here the look comes from art direction, terrain shaders and post-processing, which every engine on the list can do. Firaxis itself uses a proprietary engine ([Game Developer](https://www.gamedeveloper.com/design/firaxis-big-swing-with-civilization-vii-convincing-players-to-actually-finish-their-games)), which is not an option for a small team. Unity is the off-the-shelf choice studios in this genre actually make. Old World's 2026 migration suggests Unity 6 LTS is safe for a long-lived 4X.

**The architecture choice matters more than the engine choice.** The simulation should be a **pure, deterministic .NET library**: integer or fixed-point math, a seeded RNG, and a replayable command log. Content is JSON or YAML, mirroring Democracy's CSVs. The engine only renders state snapshots and sends commands. Four benefits follow:

- Agents can run unit and balance tests headlessly in seconds.
- Save/load and replays come almost free.
- Historians and modders can edit the data.
- The same library runs under Godot's .NET build if the team ever switches engines.

Civ VII shows the same boundary. Its UI talks to the native game through `engine.trigger` and `engine.createJSModel` bindings ([CivFanatics analysis](https://forums.civfanatics.com/threads/scripting-runtime-information.695101/)).

**UI is where the volume lives.** Civ VII's interface runs on Coherent Gameface (HTML/CSS/JS), chosen for one consistent layout across platforms ([Coherent Labs](https://coherent-labs.com/blog/casestudies/firaxis-case-study/)). Firaxis later admitted the launch UI "did not meet expectations" and rebuilt its framework in patch 1.2.4, which broke many mods ([2K](https://civilization.2k.com/de-DE/civ-vii/game-guide/gameplay/loading-screen/); [PC Games](https://www.pcgames.de/Civilization-7-Spiel-74716/News/Neues-Update-Mods-1480362/)). The failure was in UX design, not in the web technology. For an agent-built game with dozens of ministry panels and charts, Unity's UI Toolkit (UXML/USS) offers the most CSS-like text format that agents edit well. An embedded HTML layer is possible but adds integration cost. It is worth it only if UI volume dominates and a licensing quote for Gameface, whose price is not public, makes sense.

## Readable realism is a recipe a small team can reproduce

Firaxis calls the Civ VII look **"readable realism."** Art Director Jason Johnson drew on **dioramas, model railroads and elaborate tabletop-gaming tables** ([Gamereactor, via search snippet](https://www.gamereactor.eu/video/698313/Changing+History+One+Age+at+a+Time+-+Civilization+VII+Interview/)). Senior Lead Environment Artist Matthew Quickel set the governing test: "Can we make it so you never have to look at resource icons?" To tell cocoa from jungle, the team changed bean size, tree spacing and hue ([Player2](https://www.player2.net.au/2024/10/interview-john-fitzgerald-matthew-quickel-and-ryan-andrade-of-firaxis-civilization-7/)). Other confirmed features:

- Cities sprawl across adjacent hexes, and their architecture changes with each Age ([Kowatek](https://blog.kowatek.com/?p=92609)).
- Rivers come from simulated hydrology, with elevation and rainfall per tile ([Intel Gaming](https://game.intel.com/stories/sid-meiers-civilization-vii-next-level-empire-building/)).
- Early ages are lit "very dark, imposing."

The CG teaser frames the world as something "carved by human hands" on a tabletop ([Halon](https://halon.com/work/civilization)). "Tilt-shift" is the community's word, not Firaxis's. Realism also has a documented cost: without Civ VI's color-coded rooftops, "there is *no* identifying buildings from a glance" ([Erindromeda](https://erindromeda.pika.page/posts/a-civ-fans-thoughts-on-civ-vii-its-pretty-ok)). Some players also found the palette muddy ([Steam](https://steamcommunity.com/app/1295660/discussions/2/591762729907275087)).

**The reproduction recipe uses public techniques.** None of these are confirmed Firaxis internals.

- **Terrain:** one continuous heightmap mesh, with the hex grid living in data. A per-hex biome-ID texture with noise-warped blending gives organic borders, as in Catlike Coding's splat-map approach ([part 14](https://catlikecoding.com/unity/tutorials/hex-map/part-14/)) and the neighbor-blending shader on [Godot Shaders](https://godotshaders.com/shader/hexagonal-tilemap-with-blending/). Use 8–12 PBR layers: sand, loess, terra rossa, limestone, wadi gravel, irrigated field, marsh, Hermon snow. Use triplanar mapping on slopes and a toggleable faint hex overlay.
- **Decals, scatter and water:** projected decals for roads, field patterns and ruins. GPU-instanced olive, citrus, date and wheat scatter, tuned per resource the way Quickel describes. A depth-tinted sea shader with a shoreline foam band and flow-mapped rivers. Salt-flat shading for the Dead Sea.
- **Fog of war:** two levels, parchment-style for unexplored areas and desaturated for explored ones. Civ VII's own "outlined" fog was criticized as hard to parse ([Steam](https://steamcommunity.com/app/1295660/discussions/2/591762729907275087)), so biome color should stay visible.
- **Camera and grade:** a pitch of about 45–60°, a narrow FOV, mild depth of field only when zoomed in, ACES tonemapping, and **per-era color grading (LUTs)**. That means sepia-dark Ottoman evenings, bright Bauhaus noon for the 1930s, and crisp, cool modern light.

**Scale the asset list down to the minimum that works.** The research estimates a minimum set: about 8–12 terrain layers, 40–60 scatter props, 6–10 modular urban tile variants per era, 10–15 hero landmarks, and units as squads of 3–5 figures. Urban tiles need roads that line up at their edges, with variants chosen by neighbor count, so cities fuse across hexes the way Civ VII's do. The era architecture palette is distinctive and works as both prompt and kitbash brief:

- **Ottoman:** domed stone houses, khans, minarets, and red-tiled Templer roofs in Haifa.
- **Mandate:** Bauhaus "White City" Tel Aviv, with ribbon balconies and thermometer stairwells.
- **Kibbutz:** red-roofed blocks, a water tower, cypress windbreaks.
- **Jerusalem:** golden limestone.
- **Modern:** glass towers in Tel Aviv and Ramat Gan.

No Synty pack is Middle-East-specific. POLYGON Ancient Egypt (870+ prefabs) and Ancient Empire (700+) are the nearest ([Synty](https://syntystore.com/products/polygon-ancient-egypt); [Unity Asset Store](https://assetstore-fallback.unity.com/packages/3d/environments/historic/ancient-empire-synty-polygon-stylized-low-poly-art-assets-224020)). Their flat-shaded low-poly look also conflicts with a readable-realism target. The research's verdict is to pick one lane, **stylized-realistic PBR terrain with heavy scatter and simple modular buildings**, and not mix it with low-poly packs. Civ VII-level bespoke micro-detail for every era requires an art team on the scale of Firaxis plus 2K Valencia's outsourced buildings ([2K Valencia](https://valencia.2k.com/civ-vii-environment-art/)).

## AI accelerates blockouts, voices and drafts but not hero surfaces

The 2026 tools are good enough to change the budget but not to replace art direction. Licensing traps matter more than quality differences.

| Need | Best current option | Licensing trap |
|---|---|---|
| Building/prop blockouts | **Tripo** Pro (~$20/mo annual; quad remesh, auto-rig), **Meshy 6/7**, **Rodin Gen-2.5** (10M+ polys) | Tripo's free tier is non-commercial. Meshy's free output is CC BY. Rodin needs a paid plan to download |
| Open-weight 3D | **TRELLIS.2** (MIT, PBR up to 4096²) | Hunyuan3D 2.1 open weights **exclude the EU, UK and South Korea**, which disqualifies them for a worldwide Steam release |
| Discontinued | CSM Cube | **Shut down 5 Jan 2026** after Google acquired the company |
| Concept, event art, portraits | **Nano Banana Pro** (up to 14 reference images, 5 people); GPT Image 2.x; Midjourney V8 | Midjourney: Pro/Mega required above $1M revenue, and no IP indemnity. Character Reference is reportedly unsupported on V8 |
| Voice (EN/HE/AR) | **ElevenLabs v3** (74 languages incl. Hebrew and Arabic) | Free plan output is never licensed, even after upgrading. Don't clone real people |
| Music | Human composer, or verified Suno paid terms | **Eleven Music excludes "Studio Games"** on self-serve plans without written approval |

Sources: [Cinevva guide](https://app.cinevva.com/guides/ai-3d-model-generators); [Google: Nano Banana Pro](https://blog.google/intl/en-mena/product-updates/explore-get-answers/nano-banana-pro-available-globally/); [terms.law on Midjourney](https://www.terms.law/2026/01/15/midjourney-commercial-use-rights-complete-2026-guide/index.html); [Midjourney docs](https://docs.midjourney.com/docs/character-reference); [ElevenLabs languages](https://elevenlabs.io/docs/help-center/other/what-languages-do-you-support); [Eleven Music terms](https://elevenlabs.io/eleven-music-model-specific-terms); [Eleven Music v1 terms](https://elevenlabs.io/pt/eleven-music-v1-terms); [licenseorg](https://licenseorg.com/blog/ai-voice-licensing-elevenlabs).

Most 3D-tool rankings come from affiliate or vendor sources, and no independent game-readiness benchmark exists. The consensus is still clear. Static props, buildings and blockouts are usable after decimation, UV fixes and repainting. Hero characters still need manual retopology. **No tool reliably generates LODs**, so the engine or Blender has to produce them ([Cinevva](https://app.cinevva.com/guides/ai-3d-model-generators)). Style coherence comes from a fixed concept-art front end and shared trim sheets, not from the 3D generator. Portraits of real leaders such as Herzl and Ben-Gurion should be painted or heavily painted over. That improves consistency, strengthens copyright claims (copyright in purely AI-generated images is uncertain), and reduces the publicity-rights risk discussed below.

**Terrain comes from real data, stylized.** Copernicus GLO-30 (30 m) is free and open but requires a specific attribution line and a no-liability notice ([Copernicus license](https://documentation.dataspace.copernicus.eu/APIs/SentinelHub/Data/DEM/resources/license/License-COPDEM-30.pdf)). Process it offline with Python/GDAL. Downsample to 100–500 m per pixel, exaggerate the vertical scale so the roughly −430 m Dead Sea Rift and the Hermon read clearly, classify hexes by elevation and biome, then style the result in Gaea or an equivalent tool. The pipeline is engine-agnostic and well suited to agents. OpenStreetMap can supply coastlines and rivers, but its share-alike ODbL license needs legal review before map data ships.

**Thousands of event cards need retrieval-grounded generation with human sign-off.** Hallucination on grounded summaries runs to about 1–2.5%. One aggregator reports **15–40% on closed-book long-tail facts**, though that figure was not traced to a primary study ([Presenc AI](https://presenc.ai/research/ai-hallucination-rate-benchmarks-2026)). Retrieval-based fact-checkers also do worse in languages with thin online coverage, which matters for Hebrew and Arabic sources ([HalluSearch](https://arxiv.org/pdf/2504.10168.pdf)). The defensible pipeline has five steps:

1. Historians curate the event list (date, actors, sources).
2. An LLM drafts card text and mechanics **only from attached source excerpts**.
3. A second pass extracts atomic claims and verifies each one ([arXiv 2603.17872](https://arxiv.org/html/2603.17872)).
4. Historians review every flagged card and every card on a contested topic.
5. Citations are stored per card, so players can see the sources in game.

**Disclose AI use honestly on Steam, and keep it off the most visible surfaces.** Steam requires disclosure of pre-generated AI content that players consume, including store assets. The January 2026 revision exempts coding assistants and efficiency tools ([Steamworks Content Survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey); [Notebookcheck](https://www.notebookcheck.net/Steam-updates-AI-disclosure-form-requiring-developers-to-report-visible-and-in-game-AI-but-not-background-tools.1206103.0.html)). Using no runtime LLM avoids the separate guardrails disclosure. Backlash follows visible shortcuts. Vapor World had about 100k wishlists, swapped in inconsistent AI cutscenes, and launched at **36% positive** ([Everything Edinburgh](https://everythingedinburgh.com/games/news/vapor-world-steam-ai-cutscene-backlash/)). A vendor blog cites a study of 16,554 games in which AI users had poorer first-month reviews; that study is unverified ([SpriteCook](https://www.spritecook.ai/blog/is-ai-art-ok-in-indie-games)). An Israel-themed game already attracts hostile scrutiny, so a credible "AI slop" charge would compound a political attack. Key art, capsule, trailer, leader portraits and the main theme should therefore be human-made or heavily human-finished.

## Open statistics calibrate the simulation within contested ranges

Israeli official statistics are unusually usable. The **CBS Open License** grants worldwide, royalty-free commercial use with a prescribed attribution line. It excludes logos, personal data and photos, and it forbids implying endorsement ([CBS Open License](https://www.cbs.gov.il/en/Pages/Enduser-license.aspx)). The CBS also runs an SDMX API ([CBS API](https://www.cbs.gov.il/en/Pages/API-interface.aspx)). Maddison 2023 (GDP per capita to 2022) is CC BY 4.0 ([B2FIND](https://b2find.eudat.eu/dataset/5af947f7-57b4-5a20-a87b-76c0d9ee9300)), and CRS reports are public domain. Three sources still need their terms checked before shipping: SIPRI defense data (possibly non-commercial only), World Bank/OWID, and the Bank of Israel series. Treat the JVL as a cross-check rather than a source to redistribute. Its casualty-table total does not match the sum of its rows ([JVL](https://www.jewishvirtuallibrary.org/total-casualties-arab-israeli-conflict)).

**The calibration anchors are clear enough to validate the model.**

- **Jewish population:** about 24k in 1882, 84k in 1922, 175k in 1931, 554k in 1945, and about 630k in 1947. The 1914 figure is contested at 59k–94k. Israel reached 10.178M at the end of 2025, with **net migration of about −20k** ([Wikipedia: Demographic history](https://en.wikipedia.org/wiki/Demographic_history_of_Palestine_(region)); [JPost](https://www.jpost.com/israel-news/article-908103)).
- **Aliyah:** the CBS annual series runs from 101,828 in 1948 to 22,522 in 2025 and totals 3.52M. Its spikes are 1949 (239,954, though Wikipedia gives 249,954), 1990 (199,516), 1991 (176,100) and 2022 (74,400) ([JVL/CBS](https://www.jewishvirtuallibrary.org/total-immigration-to-israel-by-year)). An aliyah ministry model can be fitted directly to this series. It also needs an emigration term that responds to war and politics: 69,300 Israelis left in 2025 ([Ynet](https://www.ynetnews.com/article/1655f65xp)).
- **Economy:** inflation of about 450% in 1984, broken by the plan of 1 July 1985 ([Wikipedia](https://en.wikipedia.org/wiki/1985_Israel_Economic_Stabilization_Plan)). Defense spending ran about 8.5% of GDP in 1950 and roughly 21–30% in the 1970s; the 35% peak claim is contested. It fell to 4.4% in 2022 and rose to **8.8% in 2024** ([Breaking Defense/SIPRI](https://breakingdefense.com/2025/04/israeli-defense-spending-jumped-in-2024-part-of-overall-rise-in-middle-east-sipri/); [Rivlin](https://www.cambridge.org/core/product/14ED6D804849AB551B053EFB8A418B1D)). US aid totals $158B nominal ([CRS RL33222](https://www.congress.gov/crs_external_products/RL/PDF/RL33222/RL33222.39.pdf)). Defense burden belongs at the center of the economic model, and the 1970s should be a range, not a point.
- **Security:** Israeli war deaths are consistent per war: about 6,400 in 1948, 776+20 in 1967, 2,656–2,688 in 1973, 1,063 in the Second Intifada, and about 1,200 on October 7 ([Wikipedia: Israeli casualties](https://en.wikipedia.org/wiki/Israeli_casualties_of_war); [JVL](https://www.jewishvirtuallibrary.org/jsource/History/casualty_table.html)). Palestinian figures for Gaza are contested. The Gaza Health Ministry reported 73,001 by June 2026, and the UN treats its figures as reliable while Israel disputes them. The Lancet published estimates that demographers have challenged ([AP via WPRI](https://www.wpri.com/news/us-and-world/ap-palestinian-death-toll-in-gaza-tops-73000-officials-say-as-israel-strikes-despite-ceasefire/amp/); [Ground News](https://ground.news/article/lancet-gaza-death-toll-study-faces-bias-claims_c32b10)). The game should show such figures **as attributed ranges, never as single numbers**.
- **Elections:** no open machine-readable dataset covers every Knesset result. The team will build a CSV from JVL's per-election pages and check it against the Central Elections Committee ([gov.il](https://www.gov.il/apps/elections/eng/list/Results_eng.html)).
- **Tourism:** a record 4.55M arrivals in 2019, collapsing to 961k in 2024 ([ITN](https://itn.co.il/news/updates/israel-sees-all-time-record-in-tourist-arrivals-in-2019); [JPost](https://jpost.com/israel-news/article-881915)).

Several reference points are still unverified: university founding dates, the Water Carrier date, the dates of the Egypt and Jordan treaties as a diplomatic-relations table, and a CBS breakdown of origin by year. Wikidata (CC0) and the CBS Statistical Abstract are the follow-up sources.

## The sensitive-content line is glorification, not subject matter

**The precedents draw a consistent line.** The well-received titles share four traits: credited historians, the perspective of an ordinary person or a decision-maker, no "winnable" atrocity, and plural or testimony-based viewpoints.

- **Attentat 1942** was written by professional historians from Charles University and the Czech Academy of Sciences, with eight survivor interviews ([attentat1942.com](https://attentat1942.com/press)).
- **The Light in the Darkness**: "there's no winning the Holocaust" ([CJN](https://thecjn.ca/arts/newest-take-on-holocaust-education-comes-through-a-video-game/)).
- **Through the Darkest of Times**: Steam "Very Positive" ([Wikipedia](https://en.wikipedia.org/wiki/Through_the_Darkest_of_Times)).
- **PeaceMaker**, with dual Israeli and Palestinian leadership roles, was used with Israeli and Palestinian youth through the Peres Center ([CMU](https://www.cmu.edu/homepage/innovation/2006/fall/teaching-the-complexities-of-conflict.shtml)).

The removals targeted glorified violence. The 2014 "Bomb Gaza" apps were pulled within days. Steam blocked **Fursan al-Aqsa**'s October 7 update in the UK at the request of the UK Counter-Terrorism Internet Referral Unit. Valve said it complies with "any authority for a region" ([404 Media](https://www.404media.co/steam-removes-oct-7-game-at-request-of-uk-counter-terrorism-unit/); [Wikipedia: I-P conflict in video games](https://en.wikipedia.org/wiki/Israeli%E2%80%93Palestinian_conflict_in_video_games)). **Six Days in Fallujah** shows the "too soon" problem and the cost of a missing perspective. Konami dropped it in 2009, and when it was revived it was criticized for profiting from recent trauma and for offering no Iraqi viewpoint ([Military Times](https://www.militarytimes.com/off-duty/movies-video-games/2023/06/22/how-soon-is-too-soon-six-days-in-fallujah-out-18-years-after-battle/); [Middle East Eye](https://www.middleeasteye.net/news/six-days-fallujah-game-sparks-social-media-backlash)). Big strategy franchises avoid these subjects. HoI4 has no death camps and no direct targeting of civilians ([Paradox forum rules](https://devtrackers.gg/heartsofiron/p/b2bba598-hoi-iv-forum-rules-read-before-you-post)), and Israel has never been a Civ civ ([Steam](https://steamcommunity.com/app/289070/discussions/0/359543951717713852)). This game would be breaking new ground.

**The Holocaust belongs in the game as the chair's helplessness, not as a mechanic.** The leader's-chair framing fits the history. The Yishuv leadership's real levers were narrow, contested and painful:

- the Haavara dilemma;
- fighting the 1939 White Paper while fighting alongside Britain;
- Aliyah Bet, with more than 50,000 people interned on Cyprus;
- the Struma (about 768 dead) and the Patria;
- the rescue debates over Joel Brand and Hannah Szenes, which historians still dispute ([Wikipedia: White Paper of 1939](https://en.wikipedia.org/wiki/White_Paper_of_1939); [Struma](https://en.wikipedia.org/wiki/Struma_disaster); [Joel Brand](https://en.wikipedia.org/wiki/Joel_Brand); [Aliyah Bet](https://en.wikipedia.org/wiki/Aliyah_Bet)).

Those choices should affect the Yishuv's internal politics, its standing with Britain and its postwar legitimacy. They should never be "optimized" against the scale of the genocide, which stays a fixed historical fact presented through testimony. The aftermath carries forward as real mechanics: survivors arriving, the reparations vote, the Kasztner trial and the Eichmann trial.

**Hold the other contested events to the same discipline.** Wars and terror attacks are events with human cost and policy dilemmas. They are never tactical missions against civilians. Victims should appear as aggregated or composite figures, because naming them is risky. Occupation, settlement and collective-punishment choices carry modeled international, demographic and moral costs. Each pivotal event gets a "voices" panel: 1948 as both Independence and Nakba, with sources.

**Platform and rating exposure is manageable but concrete.**

- **Germany:** Steam requires an age rating for German sales, either from the survey or the USK ([Steamworks: Germany](https://partner.steamgames.com/doc/gettingstarted/contentsurvey/germany)). Nazi symbols are allowed only case by case under the USK's social-adequacy clause, for critical or educational works ([PCGamesN](https://www.pcgamesn.com/wolfenstein-youngblood/censorship)). Avoid swastikas, or plan a USK submission.
- **PEGI:** the "Discrimination" descriptor is always PEGI 18 and "likely to infringe national criminal laws" ([Wikipedia: PEGI](https://en.wikipedia.org/wiki/PEGI)). Stereotyped depictions of any group are therefore a hard line.
- **Gulf markets:** Saudi GCAM refused classification for Final Fantasy XVI, and the UAE banned Spec Ops: The Line across the GCC, Lebanon and Jordan ([Push Square](https://pushsquare.com/news/2023/05/final-fantasy-16-refused-classification-in-saudia-arabia); [Fast Company ME](https://fastcompanyme.com/impact/this-is-how-the-gaming-industry-is-tackling-censorship-in-the-middle-east/)). Treat MENA revenue as upside, not as part of the plan.
- **Symbols of banned organizations:** symbols of armed groups such as Hamas or Hezbollah may raise similar German legal issues. That was not verified, so use generic or text-only representation.

**Legal exposure concentrates in Israeli law and marketing.** In the US, depicting real historical figures in an expressive game is well protected: *Noriega v. Activision* was dismissed on First Amendment grounds ([Fenwick](https://www.fenwick.com/insights/publications/litigation-alert-california-superior-court-finds-use-of-likeness-of-former-panamanian-dictator-manuel-noriega-in-video-game-transformative)). Israel is riskier on three counts:

- **Publicity rights.** The Privacy Protection Law §2(6) bars commercial use of a person's name, picture or voice without consent. *Aloniel v. McDonald's* recognized a publicity right. The Dali estate case extended it for at least 25 years after death. Bar Rafaeli won about $113k over unauthorized advertising use ([FBC Lawyers](https://www.fbclawyers.com/news/legal-update-right-publicity-israeli-law-september-2016); [Jewish Press](https://www.jewishpress.com/news/israels-supermodel-bar-rafaeli-wins-113000-in-samsung-lawsuit/2013/10/17/)).
- **State symbols.** The **1949 Flag and Emblem Law** requires a permit for commercial use of the flag and emblem ([OICP](https://www.oicp-protocolo.com/israel/)).

In practice: keep living politicians and recently deceased figures out of key art and store capsules, paint portraits originally, use stylized party and IDF icons instead of logos, attribute any statement about a living person, and get Israeli counsel before launch. Israel's defamation law was not researched. Events that attribute crimes to named living politicians are the highest legal risk.

**Expect review bombing from both directions, and plan for a niche audience.** Pro-Palestinian audiences may read "playing Israel" as normalization. Pro-Israel audiences may object to any depiction of the Nakba, the occupation or civilian harm. The documented post-2023 pressure campaigns targeted pro-Palestinian games. Fursan al-Aqsa drew a Congressman's letter, yet it kept an "overwhelmingly positive" user rating ([Jewish Insider](https://jewishinsider.com/2024/12/steam-oct-7-roleplay-video-game-valve-violence-incitement/)). That shows a polarized audience can also rally behind a game. No 2023–26 cases of attacks on Israel-perspective games were found, which does not mean none happened. The commercial ceiling is sobering. Through the Darkest of Times, despite acclaim, has single- or low-double-digit concurrent players and is often discounted 80% ([Steambase](https://steambase.io/apps/1003090)). Even Civ VII fell below 5k concurrent players. The 1979 Revolution team was branded as spies in Iran ([GameStar](https://www.gamestar.de/artikel/1979-revolution-iranische-entwickler-werden-wegen-propaganda-als-feindliche-spione-gebrandmarkt,3030645.html)), so team-member safety needs a plan.

## Recommendations: de-risk the simulation and the legitimacy before the art

The recommendations below put the hardest unknowns first. The first is whether the ministry graph is fun. The second is whether the framing survives scrutiny from both sides.

**Team and governance.** The core team should be a lead designer with a strong history background, one engineer who directs coding agents, one technical artist who owns terrain shaders and the look, and one 3D artist who acts as art director. Contract the rest: a human composer, Hebrew- and Arabic-speaking voice QA, and Israeli media and IP counsel. Before content production starts, form a **named advisory board**. It should include historians of the Yishuv and Zionism, the Holocaust, Palestinian history including the Nakba, and Mizrahi and Arab-Israeli history, with at least one Israeli and one Palestinian or Arab scholar. Publish the board on the Steam page. Add sensitivity readers from Ashkenazi and Mizrahi Israeli Jewish, Palestinian, Arab-Israeli, Druze and Haredi communities, plus descendants of survivors. Yad Vashem, the USHMM and Palestinian oral-history archives could be approached as partners, but none has a documented record of partnering with commercial games.

**Phased build.**

| Phase | Months (indicative) | Deliverable | Exit test |
|---|---|---|---|
| 0. Paper and data | 0–3 | Ministry graph spec in CSV/JSON; calibration datasets (CBS, Maddison, JVL aliyah, casualties) with license log; era/decision catalog with advisory sign-off on framing rules | Advisors approve the Holocaust, Nakba and terror representation rules |
| 1. Headless sim | 3–8 | Deterministic C# library; one chapter (Founding 1947–67) playable in a debug UI; automated balance runs that reproduce 1948–67 population/aliyah curves | Playtesters find the trade-offs interesting with grey boxes only |
| 2. Vertical slice | 8–14 | Unity URP hex map of the Levant from Copernicus DEM; terrain shader, scatter, two era LUTs, 6 urban tile variants; UI Toolkit cabinet and ministry screens; 3 authored scenes | Wishlist page, external playtest, the "would you buy this" check |
| 3. Content scale-up | 14–30 | Building, Holding, Opening and Managing chapters; retrieval-grounded event pipeline with per-card citations; AI-blockout buildings human-finished; ElevenLabs advisor voices | Historian review clears every contested card |
| 4. Launch | ~30–36 | Steam Win/Mac/Deck; AI disclosure; German rating; content warnings; in-game sources page | Steam Deck verified; legal sign-off on likeness and emblems |
| 5. Reckoning | Post-launch | 2022–26 chapter as strategic-level event cards only | Events have settled enough to write with attribution |

The months are planning estimates, not figures from the research.

**Specific design calls.**

- **Annual turns with crisis zoom.** Run 17 eras as six continuous chapters. Replace Civ VII-style legacy paths with optional, era-specific Triumph goals.
- **Two political models.** Use a pre-state legitimacy and influence model, then a coalition model after 1948. Model the PA, Hamas, PIJ, Arab citizens and each neighbor as separate actors with AI agency.
- **Data as the tuning surface.** Ship every equation as moddable data. Expose nested tooltips for every number from the first prototype.
- **Ship the October 7 era late.** Ship the 2023–26 era after launch rather than at release, with no tactical October 7 gameplay and with attributed casualty ranges. Today's facts may change: the Iran war, the Lebanon framework, the Gaza Phase Two process and the election result.

## Conclusion

The research changes where the difficulty lies. Graphics, once the obvious bottleneck for a small team chasing Civ VII, are now the most tractable layer. The terrain techniques are public, Unity has genre precedent, and AI blockouts and concept art cut asset costs, provided the team avoids the license traps. The real constraints are elsewhere. One is a simulation graph whose interactions multiply with every equation. Another is the factual reliability of thousands of event cards. The third is the legitimacy of the framing in front of audiences who will attack from both sides. All three call for the same discipline: a data-first, test-heavy simulation, plus human historians who are named in public.

The most distinctive opportunity is one that large studios have avoided. The leader's-chair perspective turns Israel's history into a series of constrained, consequential choices taken under pressure from other actors with their own voices. Those choices include Peel, the Altalena, reparations, 1973, Oslo and disengagement. That is exactly what the Democracy, Suzerain and PeaceMaker lineage does well. The game succeeds if players finish a campaign understanding why each decision was hard and how the other side saw it. It fails if the game reduces Israel's history to a strategy for winning.
