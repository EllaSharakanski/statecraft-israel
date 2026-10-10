from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def test_decisions_record_d2_decided_and_d10_d11_open_for_the_owner():
    text = read("docs/oss/DECISIONS.md")
    assert "- [x] D2" in text and "inbound = outbound" in text
    assert "- [ ] D10" in text and "d42bd6b" in text and "- [ ] D11" in text and "Published before checked" in text


def test_security_policy_uses_private_reporting_only():
    text = read("SECURITY.md")
    assert "security/advisories/new" in text and "@" not in text.replace("@eyalgolan", "")
    assert "revoked at its provider; history is not rewritten" in text


def test_contributing_links_security_and_untrusted_review():
    text = read("CONTRIBUTING.md")
    assert "(SECURITY.md)" in text and "docs/UNTRUSTED_PR_REVIEW.md" in text


def test_contributing_has_no_cla_and_names_every_sensitive_tag():
    text = read("CONTRIBUTING.md")
    assert "There is no CLA and no sign-off" in text
    for tag in ("holocaust", "terror", "massacre", "war-casualties"):
        assert f"`{tag}`" in text, tag


def test_untrusted_pr_review_forbids_running_pr_code_and_warns_about_github_diffs():
    text = read("docs/UNTRUSTED_PR_REVIEW.md")
    assert "--network none" in text and "Assets/Editor" in text and "Directory.Build.props" in text
    assert "Any diff under `.github/` can make every required check green" in text


def test_contributing_names_every_ci_command_and_the_unity_gate():
    text = read("CONTRIBUTING.md")
    for cmd in ("dotnet test sim/Statecraft.sln",
                "uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 "
                "--with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q",
                "uv run --with pytest==9.1.1 --with pyyaml==6.0.3 --with requests==2.32.5 pytest scripts/oss/tests -q",
                "uv run scripts/oss/check_repo.py",
                "-buildTarget OSXUniversal -runTests -testPlatform EditMode", "## What CI runs", "CHANGELOG.md"):
        assert cmd in text, cmd


def test_changelog_has_unreleased_on_top():
    text = read("CHANGELOG.md")
    assert text.index("## [Unreleased]") == text.index("## [")


def test_historical_accuracy_form_requires_a_source():
    import yaml
    form = yaml.safe_load(read(".github/ISSUE_TEMPLATE/historical_accuracy.yml"))
    body = {b.get("id"): b for b in form["body"]}
    assert body["sources"]["validations"]["required"] is True
    assert {"where", "wrong", "sources"} <= set(body)
    cfg = yaml.safe_load(read(".github/ISSUE_TEMPLATE/config.yml"))
    assert cfg["blank_issues_enabled"] is False
    assert any("security/advisories/new" in c["url"] for c in cfg["contact_links"])
