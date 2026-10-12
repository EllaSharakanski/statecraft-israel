"""era_of: eras are [from, to); the day an era ends belongs to the next; outside every era exits 1."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('era_of', ROOT / 'scripts/era_of.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

ERAS = 'eras:\n  - { id: one, from: 1948-05-15, to: 1950-01-01 }\n  - { id: two, from: 1950-01-01, to: 1957-01-01 }\n'


def test_boundary_day_belongs_to_the_next_era(make_repo):
    r = make_repo({'content/eras.yaml': ERAS})
    assert m.era_of(r, '1948-05-15') == ('one', '1948-05-15', '1950-01-01')
    assert m.era_of(r, '1949-12-31')[0] == 'one'
    assert m.era_of(r, '1950-01-01')[0] == 'two'
    assert m.era_of(r, '1956-12-31')[0] == 'two'


def test_outside_every_era(make_repo, capsys):
    r = make_repo({'content/eras.yaml': ERAS})
    assert m.era_of(r, '1948-05-14') is None and m.era_of(r, '1957-01-01') is None
    assert m.main(['1957-01-01', '--root', str(r)]) == 1
    assert capsys.readouterr().out.strip() == 'no era'


def test_runs_on_this_repo(capsys):
    assert m.main(['1950-06-01']) == 0
    assert capsys.readouterr().out.split()[0] == 'founding'
