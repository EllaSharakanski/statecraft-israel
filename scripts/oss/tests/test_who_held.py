"""who_held: terms are [from, to); an open term runs on; no holder exits 1."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('who_held', ROOT / 'scripts/who_held.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

PEOPLE = ('people:\n'
          '  - id: a\n    offices:\n      - { office: pm, from: 1948-05-14, to: 1950-01-01 }\n'
          '  - id: b\n    offices:\n      - { office: pm, from: 1950-01-01 }\n      - { office: fm, from: 1949-01-01, to: 1949-06-01 }\n')


def repo(make_repo):
    return make_repo({'content/people.yaml': PEOPLE})


def test_the_end_day_is_not_covered_but_the_start_day_is(make_repo):
    r = repo(make_repo)
    assert m.holders(r, 'pm', '1949-12-31') == [('a', '1948-05-14', '1950-01-01')]
    assert m.holders(r, 'pm', '1950-01-01') == [('b', '1950-01-01', 'open')]
    assert m.holders(r, 'pm', '1948-05-14') == [('a', '1948-05-14', '1950-01-01')]


def test_open_term_runs_on_and_other_offices_are_ignored(make_repo):
    r = repo(make_repo)
    assert m.holders(r, 'pm', '1956-12-31') == [('b', '1950-01-01', 'open')]
    assert m.holders(r, 'fm', '1949-06-01') == []


def test_exit_code_and_output(make_repo, capsys):
    r = repo(make_repo)
    assert m.main(['pm', '1949-01-01', '--root', str(r)]) == 0
    assert capsys.readouterr().out.strip() == 'a  1948-05-14  1950-01-01'
    assert m.main(['fm', '1950-01-01', '--root', str(r)]) == 1
    assert capsys.readouterr().out.strip() == 'nobody'


def test_runs_on_this_repo():
    assert m.holders(ROOT, 'prime_minister', '1949-06-01')[0][0]
