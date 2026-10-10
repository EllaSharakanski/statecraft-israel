import json
import re
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[3]
CI = ROOT / ".github/workflows/ci.yml"
RULESET = ROOT / "scripts/oss/github/ruleset-main-review.json"
EXPECTED_JOBS = {"Sim (dotnet test)", "Tools (pytest)", "Repo policy", "Changelog"}


def ci() -> dict:
    return yaml.safe_load(CI.read_text(encoding="utf-8"))


def triggers(wf: dict) -> dict:
    return wf.get("on", wf.get(True))  # PyYAML reads the bare key `on` as True


def steps(wf: dict):
    return [s for j in wf["jobs"].values() for s in j["steps"]]


def test_job_names_are_the_required_checks():
    assert {j["name"] for j in ci()["jobs"].values()} == EXPECTED_JOBS
    if RULESET.exists():
        rules = json.loads(RULESET.read_text())["rules"]
        checks = next(r for r in rules if r["type"] == "required_status_checks")["parameters"]
        assert {c["context"] for c in checks["required_status_checks"]} == EXPECTED_JOBS


def test_default_token_is_read_only_and_nothing_targets_the_base_repo():
    wf = ci()
    assert wf["permissions"] == {"contents": "read"}
    t = triggers(wf)
    assert "pull_request_target" not in t and "schedule" not in t and "workflow_run" not in t


def test_checkouts_keep_no_credentials():
    for s in steps(ci()):
        if s.get("uses", "").startswith("actions/checkout@"):
            assert s.get("with", {}).get("persist-credentials") is False, s


def test_pull_request_types_rerun_on_label_changes():
    types = set(triggers(ci())["pull_request"]["types"])
    assert {"opened", "synchronize", "reopened", "labeled", "unlabeled"} <= types


def test_ci_uses_no_secret_but_the_read_only_token():
    text = CI.read_text(encoding="utf-8")
    assert "secrets." not in text
    assert set(re.findall(r"\$\{\{\s*github\.token\s*\}\}", text)) <= {"${{ github.token }}"}


def test_every_action_is_pinned_to_a_full_sha():
    for wf in (ROOT / ".github/workflows").glob("*.yml"):
        for ref in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@(\S+)", wf.read_text(encoding="utf-8")):
            assert re.fullmatch(r"[0-9a-f]{40}", ref), f"{wf.name}: {ref}"


def test_tools_job_installs_dotnet():
    # Review C2: tools/tests/test_package_release.py runs the sim's release gate through dotnet.
    tools = next(j for j in ci()["jobs"].values() if j["name"] == "Tools (pytest)")
    assert any(s.get("uses", "").startswith("actions/setup-dotnet@") for s in tools["steps"])
    assert "--deselect" not in CI.read_text(encoding="utf-8")


def test_changelog_diffs_the_pr_head_not_the_merge_ref():
    text = CI.read_text(encoding="utf-8")
    assert "github.event.pull_request.head.sha" in text and '--head "$HEAD_SHA"' in text


def test_no_cla_or_dco_machinery():
    # Decision D2 (owner ruling): inbound = outbound under the repository licences; no CLA, no DCO.
    text = CI.read_text(encoding="utf-8")
    assert "CLA" not in text.replace("no CLA or DCO", "").replace("There is no CLA", "") and "Signed-off-by" not in text
    assert not (ROOT / "CLA.md").exists() and not (ROOT / "contributors").exists()
    assert not (ROOT / ".github/workflows/cla-nudge.yml").exists()


def test_ci_commands_match_contributing():
    contributing = (ROOT / "CONTRIBUTING.md").read_text(encoding="utf-8")
    runs = [s["run"].strip() for s in steps(ci()) if "run" in s]
    for cmd in ("dotnet test sim/Statecraft.sln -c Release", "pytest tools/tests -q", "pytest scripts/oss/tests -q",
                "uv run scripts/oss/check_repo.py"):
        assert any(cmd in r for r in runs), cmd
        assert cmd.replace(" -c Release", "") in contributing, cmd


def test_codeowners_makes_the_maintainer_owner_of_everything():
    lines = [l for l in (ROOT / ".github/CODEOWNERS").read_text().splitlines() if l and not l.startswith("#")]
    assert lines == ["* @eyalgolan"]


# Review fix round 1 (I5, M1, M2, M4).
def test_pins_carry_exact_version_comments_and_dependabot_keeps_them_current():
    for ref_line in re.findall(r"uses:\s*[\w.-]+/[\w./-]+@[0-9a-f]{40}.*", CI.read_text(encoding="utf-8")):
        assert re.search(r"# v\d+\.\d+\.\d+$", ref_line), ref_line
    dep = yaml.safe_load((ROOT / ".github/dependabot.yml").read_text(encoding="utf-8"))
    assert dep["version"] == 2 and any(u["package-ecosystem"] == "github-actions" for u in dep["updates"])


def test_toolchain_is_pinned_and_main_pushes_are_not_cancelled():
    text = CI.read_text(encoding="utf-8")
    assert "ubuntu-latest" not in text and "ubuntu-24.04" in text
    assert 'dotnet-version: "10.0.401"' in text and "10.0.x" not in text
    assert 'UV_PYTHON: "3.12"' in text
    assert "cancel-in-progress: ${{ github.event_name == 'pull_request' }}" in text
    assert "ruleset-protect-main.json" not in text and "ruleset-main-review.json" in text
