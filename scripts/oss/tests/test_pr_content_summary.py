"""PR content summary (#325): added, removed and re-dated records against a base, and the no-change line."""
import importlib.util
from pathlib import Path

from conftest import commit_all, git

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('pr_content_summary', ROOT / 'scripts/pr_content_summary.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

MODEL = "indicators:\n  - { id: morale }\n  - { id: food }\npolicies:\n  - { id: rationing }\n"


def test_added_removed_redated(make_repo, capsys):
    root = make_repo({"content/model.yaml": MODEL, "content/events/a.yaml": "id: a\nfrom: 1948-06-01\n",
                      "content/events/b.yaml": "id: b\nfrom: 1949-01-01\nto: 1949-02-01\n"})
    base = commit_all(root, "base")
    (root / "content/events/a.yaml").write_text("id: a\nfrom: 1948-06-01\nto: 1948-07-01\n")
    (root / "content/events/b.yaml").unlink()
    (root / "content/events/c.yaml").write_text("id: c\nfrom: 1950-01-01\n")
    (root / "content/model.yaml").write_text(MODEL.replace("{ id: food }", "{ id: food, from: 1949-01-01 }")
                                             + "  - { id: maabarot, from: 1948-08-01 }\n")
    commit_all(root, "change")
    assert m.main([base, "--root", str(root)]) == 0
    out = capsys.readouterr().out
    assert "**Added** (2)" in out and "- `event:c` (1950-01-01 to open)" in out and "- `policy:maabarot` (1948-08-01 to open)" in out
    assert "**Removed** (1)" in out and "- `event:b` (1949-01-01 to 1949-02-01)" in out
    assert "`event:a`: 1948-06-01 to open -> 1948-06-01 to 1948-07-01" in out
    assert "`indicator:food`: - to open -> 1949-01-01 to open" in out


def test_no_change_and_bad_base(make_repo, capsys):
    root = make_repo({"content/model.yaml": MODEL, "content/events/a.yaml": "id: a\nfrom: 1948-06-01\n"})
    base = commit_all(root, "base")
    assert m.main([base, "--root", str(root)]) == 0
    assert capsys.readouterr().out.strip() == "No content records changed."
    assert m.main(["nope", "--root", str(root)]) == 2
