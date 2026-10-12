"""pr_scope: reports changelog, Unity and golden status from a list of changed paths."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('pr_scope', ROOT / 'scripts/pr_scope.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


def test_tooling_only_change_needs_nothing():
    out = m.scope(['scripts/x.py', 'docs/dev-setup.md'])
    assert out[0].startswith('User-visible paths: none')
    assert out[1] == 'Unity: nothing under unity/ changed.'
    assert out[2] == 'Goldens: unchanged.'


def test_user_visible_change_without_changelog_is_flagged():
    assert 'NOT updated' in m.scope(['content/model.yaml'])[0]
    assert 'is updated' in m.scope(['content/model.yaml', 'CHANGELOG.md'])[0]


def test_unity_tests_are_not_user_visible_but_still_unity():
    out = m.scope(['unity/Assets/Tests/A.cs'])
    assert out[0].startswith('User-visible paths: none')
    assert 'Unity: 1 file(s)' in out[1]


def test_golden_change_is_named():
    out = m.scope(['sim/Statecraft.Sim.Tests/golden/hash1950.txt'])
    assert out[2].startswith('Goldens: CHANGED (hash1950.txt)')


def test_runs_on_this_repo():
    assert m.main(['HEAD']) == 0
