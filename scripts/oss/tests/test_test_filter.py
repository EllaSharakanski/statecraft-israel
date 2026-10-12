"""test_filter: changed sim files map to test classes by name; unknown files fall back to the full suite."""
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('test_filter', ROOT / 'scripts/test_filter.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)

KNOWN = {'EconomySystemTests': 'sim/Statecraft.Sim.Tests/EconomySystemTests.cs',
         'ContentWindowsTests': 'sim/Statecraft.Sim.Tests/ContentWindowsTests.cs'}


def test_source_file_maps_to_its_test_class():
    assert m.classes_for(['sim/Statecraft.Sim/EconomySystem.cs'], KNOWN) == ['EconomySystemTests']


def test_changed_test_file_maps_to_its_classes():
    assert m.classes_for(['sim/Statecraft.Sim.Tests/ContentWindowsTests.cs'], KNOWN) == ['ContentWindowsTests']


def test_unmatched_and_non_sim_files_give_nothing():
    assert m.classes_for(['sim/Statecraft.Sim/Unknown.cs', 'docs/a.md'], KNOWN) == []


def test_command_filters_or_falls_back_to_full():
    assert m.command(['A', 'B']).endswith('--filter "FullyQualifiedName~A|FullyQualifiedName~B"')
    assert m.command([]) == m.FULL


def test_real_test_project_has_the_classes():
    assert 'ContentWindowsTests' in m.test_classes(ROOT)
