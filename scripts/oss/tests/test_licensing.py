import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
import yaml12  # noqa: E402  (tools/yaml12.py: YAML 1.2, no 1.1 coercions)


def test_mit_licence_names_the_copyright_holder():
    text = (ROOT / "LICENSE").read_text(encoding="utf-8")
    assert "MIT License" in text and "Eyal Golan" in text


def test_licence_texts_are_the_real_ones():
    for name, marker in (("CC-BY-4.0", "Attribution 4.0 International"),
                         ("CC-BY-SA-4.0", "Attribution-ShareAlike 4.0 International"),
                         ("CC0-1.0", "CC0 1.0 Universal"),
                         ("CC-BY-NC-ND-4.0", "Attribution-NonCommercial-NoDerivatives 4.0 International")):
        assert marker in (ROOT / f"LICENSES/{name}.txt").read_text(encoding="utf-8"), name
    yaml_dot_net = (ROOT / "LICENSES/YamlDotNet-MIT.txt").read_text(encoding="utf-8")
    assert "Antoine Aubry" in yaml_dot_net and "Permission is hereby granted" in yaml_dot_net


def test_licensing_table_covers_every_top_level_area():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    for d in ("sim/", "tools/", "scripts/", ".github/", "unity/Assets/", "content/", "docs/", "reports/",
              "research_notes/", "promo/", "spikes/", "ASSETS.yaml", "unity/Assets/Settings/", "unity/Packages/"):
        assert f"`{d}" in text, d


def test_licensing_puts_unity_template_files_under_the_companion_licence():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    row = next(l for l in text.splitlines() if l.startswith("| Unity template-derived files"))
    assert "Unity Companion License" in row and "spikes/m0/unity/Assets/Settings/" in row and "not MIT" in row


def test_licensing_points_at_decisions_and_assets():
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    assert "docs/oss/DECISIONS.md" in text and "ASSETS.yaml" in text and "published before the checks" in text


def test_notice_carries_the_required_attributions():
    text = (ROOT / "NOTICE").read_text(encoding="utf-8")
    assert "produced using Copernicus WorldDEM-30 (c) DLR e.V. 2010-2014" in text
    for s in ("CHELSA V2.1", "Israel Meteorological Service", "SIL Open Font License", "Wikipedia", "YamlDotNet"):
        assert s in text, s


def test_every_repo_path_licensing_names_exists():
    """Every backticked repo path in LICENSING.md names something tracked, so the table never licenses a file that is
    gone (the decision clips' old label, tools/event_videos/ai-bug.png, was deleted in #551).
    What counts as a repo path: a backticked token with a "/" whose first segment is a tracked top-level entry
    (`unity/...`, `promo/...`, `.github/`). Skipped as not repo paths: URLs, placeholders such as `<name>`, and tokens
    with no "/" or a relative first segment (`*.md`, `.meta`, `link.xml`, `FrankRuhlLibre/`, `08`), which name a kind of
    file or a file inside a folder the row names. A token with * or ? must match at least one tracked file; any other
    token must be a tracked file or the start of one's path (a folder, or `promo/final/01` for the file 01-key-art.png)."""
    import fnmatch
    import re
    import subprocess
    tracked = subprocess.run(["git", "ls-files"], cwd=ROOT, capture_output=True, text=True, check=True).stdout.splitlines()
    top = {p.split("/", 1)[0] for p in tracked}
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    missing = []
    for token in sorted(set(re.findall(r"`([^`\n]+)`", text))):
        if "://" in token or "<" in token or "/" not in token or token.split("/", 1)[0] not in top:
            continue
        if any(c in token for c in "*?"):
            ok = any(fnmatch.fnmatchcase(p, token) for p in tracked)
        else:
            ok = any(p == token or p.startswith(token) for p in tracked)
        if not ok:
            missing.append(token)
    assert missing == [], f"LICENSING.md names paths that are not in the repository: {missing}"


def test_all_of_tal_heubergers_models_are_cc_by_nc_nd_from_20_october_2026():
    """Owner ruling, 20 October 2026: all of Tal Heuberger's models, the 73 released as CC0 included, are distributed
    under CC BY-NC-ND 4.0 from that date. CC0 cannot be withdrawn, so LICENSING.md and NOTICE say that copies obtained
    before that date keep CC0."""
    rows = [r for r in yaml12.load_path(ROOT / "ASSETS.yaml")["rules"]
            if r.get("artist") == "Tal Heuberger"]
    globs = {r["glob"] for r in rows}
    assert len(rows) == 12  # the 23 single-building models (one row) and the 50 in The 1948 Collection (eleven rows)
    assert {"unity/Assets/Art/Settlements/Prototype/Models/b*", "unity/Assets/Art/Collection/*"} <= globs
    assert [r["glob"] for r in rows if r["licence"] != "CC-BY-NC-ND-4.0"] == []
    assert all("CC0 1.0 until 2026-10-20" in r["note"] for r in rows)
    text = (ROOT / "LICENSING.md").read_text(encoding="utf-8")
    tal = [l for l in text.splitlines() if l.startswith("|") and "Tal Heuberger" in l]
    assert len(tal) == 1, "one LICENSING.md row covers all of Tal Heuberger's models"
    row = tal[0]
    for path in ("`unity/Assets/Art/Settlements/Prototype/Models/`", "`unity/Assets/Art/Collection/`"):
        assert path in row
    assert "[CC BY-NC-ND 4.0](LICENSES/CC-BY-NC-ND-4.0.txt)" in row and 'credit "Tal Heuberger"' in row
    assert "before 20 October 2026" in row and "keeps them under CC0 1.0" in row
    notice = (ROOT / "NOTICE").read_text(encoding="utf-8")
    credit = next(p for p in notice.split("\n\n") if "Tal Heuberger" in p)
    assert "CC BY-NC-ND 4.0" in credit and "https://creativecommons.org/licenses/by-nc-nd/4.0/" in credit
    assert "copies obtained before then keep CC0 1.0" in credit
    assert "Tal Heuberger, CC0" not in notice
