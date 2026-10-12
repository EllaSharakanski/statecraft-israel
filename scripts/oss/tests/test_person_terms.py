"""person_terms: terms come out oldest first, open terms say open, an unknown id exits 1."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('person_terms', ROOT / 'scripts/person_terms.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

PEOPLE = ('people:\n  - id: a\n    offices:\n      - { office: fm, from: 1950-01-01 }\n      - { office: pm, from: 1948-05-14, to: 1949-06-01 }\n'
          '  - id: b\n    offices: []\n')


def test_terms_sorted_by_start_with_open_marked(make_repo):
    r = make_repo({'content/people.yaml': PEOPLE})
    assert m.terms(r, 'a') == [('pm', '1948-05-14', '1949-06-01'), ('fm', '1950-01-01', 'open')]
    assert m.terms(r, 'b') == []
    assert m.terms(r, 'zz') is None


def test_exit_codes_and_output(make_repo, capsys):
    r = make_repo({'content/people.yaml': PEOPLE})
    assert m.main(['a', '--root', str(r)]) == 0
    assert capsys.readouterr().out.splitlines()[1].split() == ['1950-01-01', 'open', 'fm']
    assert m.main(['b', '--root', str(r)]) == 0
    assert 'no office terms' in capsys.readouterr().out
    assert m.main(['zz', '--root', str(r)]) == 1
    assert capsys.readouterr().out.strip() == 'unknown person'


def test_runs_on_this_repo():
    assert m.main(['ben_gurion']) == 0
