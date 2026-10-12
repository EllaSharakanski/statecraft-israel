"""events_on: windows are [from, to); an open window runs on; nothing in play exits 1."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('events_on', ROOT / 'scripts/events_on.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

FILES = {'content/events/a.yaml': 'id: a\nfrom: 1948-06-01\nto: 1948-07-01\nanchor: hard\nstatus: fact-checked\n',
         'content/events/b.yaml': 'id: b\nfrom: 1948-06-15\n'}


def ids(root, d):
    return [r['id'] for r in m.events_on(root, d)]


def test_end_day_is_excluded_and_start_day_included(make_repo):
    r = make_repo(FILES)
    assert ids(r, '1948-05-31') == []
    assert ids(r, '1948-06-01') == ['a']
    assert ids(r, '1948-06-30') == ['a', 'b']
    assert ids(r, '1948-07-01') == ['b']


def test_defaults_and_open_window(make_repo):
    r = make_repo(FILES)
    b = m.events_on(r, '1950-01-01')[0]
    assert (b['to'], b['anchor'], b['status']) == ('open', 'soft', 'draft')


def test_exit_code_and_output(make_repo, capsys):
    r = make_repo(FILES)
    assert m.main(['1948-06-01', '--root', str(r)]) == 0
    assert capsys.readouterr().out.split()[-1] == 'a'
    assert m.main(['1948-01-01', '--root', str(r)]) == 1
    assert capsys.readouterr().out.strip() == 'no events'


def test_runs_on_this_repo():
    assert m.main(['1948-06-20']) == 0
