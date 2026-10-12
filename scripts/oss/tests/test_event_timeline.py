"""Event timeline (#324): sorting, era lookup, open windows, status filter and the real content."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('event_timeline', ROOT / 'scripts/event_timeline.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


def repo(tmp_path):
    (tmp_path / 'content/events').mkdir(parents=True)
    (tmp_path / 'content/eras.yaml').write_text(
        "eras:\n  - { id: a, from: 1948-05-15, to: 1950-01-01 }\n  - { id: b, from: 1950-01-01, to: 1957-01-01 }\n")
    (tmp_path / 'content/events/late.yaml').write_text("id: late\nfrom: 1951-01-01\nstatus: fact-checked\n")
    (tmp_path / 'content/events/early.yaml').write_text("id: early\nfrom: 1948-06-01\nto: 1948-07-01\ntags: [security]\nanchor: hard\n")
    return tmp_path


def test_sorted_with_era_open_window_and_default_status(tmp_path):
    rows = m.load(repo(tmp_path))
    assert [r['id'] for r in rows] == ['early', 'late']
    assert rows[0]['era'] == 'a' and rows[1]['era'] == 'b'
    assert rows[1]['to'] == 'open' and rows[0]['status'] == 'draft' and rows[0]['anchor'] == 'hard'
    out = m.render(rows)
    assert '[security]' in out and out.splitlines()[-1] == '2 events: 1 draft, 1 fact-checked'


def test_status_filter(tmp_path, capsys):
    assert m.main(['--root', str(repo(tmp_path)), '--status', 'draft']) == 0
    out = capsys.readouterr().out
    assert 'early' in out and 'late' not in out.split('\n')[0]


def test_real_content_every_event_lands_in_an_era():
    rows = m.load(ROOT)
    assert rows and all(r['era'] != '-' for r in rows)
