from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def test_mit_licence_names_the_copyright_holder():
    text = (ROOT / "LICENSE").read_text(encoding="utf-8")
    assert "MIT License" in text and "Eyal Golan" in text


def test_licence_texts_are_the_real_ones():
    for name, marker in (("CC-BY-4.0", "Attribution 4.0 International"),
                         ("CC-BY-SA-4.0", "Attribution-ShareAlike 4.0 International"),
                         ("CC0-1.0", "CC0 1.0 Universal")):
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
