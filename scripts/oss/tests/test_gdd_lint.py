"""GDD feature-section linter (#285): each failure, and a passing example."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('gdd_lint', ROOT / 'scripts/oss/gdd_lint.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

GOOD = """# F

## Player story

As a player, I want a thing, so that it works.

## Rules

- A turn lasts 7 days.
- The budget is cut by 5% and costs IL 2m.

## Data touched

- `content/rules.yaml`

## Done when

- 3 tests pass.

## Out of scope

- Nothing else.

## Sources

- [S1] The spec, docs/superpowers/specs/x.md

## Open questions

- None.
"""


def kinds(tmp_path, text):
    f = tmp_path / 'f.md'
    f.write_text(text)
    return [p.split(': ')[1] if ':' in p else p for p in m.lint(f, 'f.md')]


def test_a_complete_section_passes(tmp_path):
    assert kinds(tmp_path, GOOD) == []


def test_no_done_when_bullet(tmp_path):
    out = kinds(tmp_path, GOOD.replace('- 3 tests pass.', 'Just prose.'))
    assert out == ['no-done-when']


def test_a_missing_section(tmp_path):
    out = kinds(tmp_path, GOOD.replace('## Out of scope\n\n- Nothing else.\n\n', ''))
    assert out == ['missing-section']


def test_a_leftover_tbd(tmp_path):
    for word in ('TBD', 'TODO', '???'):
        assert kinds(tmp_path, GOOD.replace('- None.', f'- {word} later')) == ['tbd']


def test_a_number_without_a_unit(tmp_path):
    out = kinds(tmp_path, GOOD.replace('lasts 7 days', 'lasts 7'))
    assert out == ['no-unit']


def test_unitless_marker_and_ranges_and_ids_are_not_flagged(tmp_path):
    ok = GOOD.replace('- A turn lasts 7 days.', '- A turn lasts between 7 and 21 days.\n- Rank 3 (unitless)\n- R-ECON-014 holds.')
    assert kinds(tmp_path, ok) == []


def test_a_year_without_a_source(tmp_path):
    out = kinds(tmp_path, GOOD.replace('- A turn lasts 7 days.', '- The war ended in 1949.'))
    assert out == ['no-source']


def test_a_dated_claim_with_a_defined_source_passes(tmp_path):
    ok = GOOD.replace('- A turn lasts 7 days.', '- It starts on 15 May 1948 [S1].')
    assert kinds(tmp_path, ok) == []


def test_citing_an_undefined_source(tmp_path):
    out = kinds(tmp_path, GOOD.replace('- A turn lasts 7 days.', '- It starts on 15 May 1948 [S2].'))
    assert out == ['no-source', 'no-source']


def test_a_source_with_no_link_or_path_does_not_count(tmp_path):
    bad = GOOD.replace('docs/superpowers/specs/x.md', 'a book').replace('- A turn lasts 7 days.', '- It starts in 1948 [S1].')
    assert 'no-source' in kinds(tmp_path, bad)


def test_this_repository_gdd_files_lint_clean():
    # Runs in CI with the other policy checker tests; includes the shipped example.
    assert m.check(ROOT) == []
    assert (ROOT / 'docs/gdd/example-turn-grid.md').exists()
