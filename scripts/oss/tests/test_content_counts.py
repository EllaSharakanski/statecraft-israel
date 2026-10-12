"""content_counts: counts records per kind and how many carry a window, from a small fixture repo."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('content_counts', ROOT / 'scripts/content_counts.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

FILES = {
    'content/model.yaml': 'indicators:\n  - { id: a }\n  - { id: b, from: 1949-01-01 }\n  - { id: c, to: 1950-01-01 }\npolicies:\n  - { id: p }\n',
    'content/events/e1.yaml': 'id: e1\n',
    'content/events/e2.yaml': 'id: e2\n',
    'content/offices.yaml': 'offices:\n  - { id: o1 }\n',
    'content/people.yaml': 'people:\n  - { id: x }\n  - { id: y }\n',
}


def test_counts_records_windows_and_files(make_repo):
    root = make_repo(FILES)
    got = {k: (t, w) for k, t, w in m.counts(root)}
    assert got['indicators'] == (3, 2)
    assert got['policies'] == (1, 0)
    assert got['effects'] == (0, 0)
    assert got['events'] == (2, None)
    assert got['offices'] == (1, None) and got['people'] == (2, None) and got['parties'] == (0, None)


def test_prints_one_line_per_kind(make_repo, capsys):
    assert m.main(['--root', str(make_repo(FILES))]) == 0
    out = capsys.readouterr().out.splitlines()
    assert out[0] == 'indicators: 3 (2 with a window)'
    assert 'events: 2' in out and len(out) == 10


def test_runs_on_this_repo():
    assert m.main([]) == 0
