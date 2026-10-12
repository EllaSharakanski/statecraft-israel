"""Event scaffold (#323): the file it writes has every required field and loads as YAML; bad input is refused."""
import importlib.util
import json
from pathlib import Path

import yaml

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location('new_event', ROOT / 'scripts/new_event.py')
m = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(m)


def test_writes_every_required_field(tmp_path):
    assert m.main(['my_event', '--from', '1949-01-01', '--to', '1949-02-01'], root=tmp_path) == 0
    doc = yaml.safe_load((tmp_path / 'content/events/my_event.yaml').read_text())
    required = json.loads((ROOT / 'content/schema/event.schema.json').read_text())['required']
    assert all(k in doc for k in required)
    assert doc['id'] == 'my_event' and doc['sources'] == [] and doc['status'] == 'draft'
    assert str(doc['to']) == '1949-02-01'


def test_no_to_means_no_to_key(tmp_path):
    m.main(['e1'], root=tmp_path)
    assert 'to' not in yaml.safe_load((tmp_path / 'content/events/e1.yaml').read_text())


def test_refuses_overwrite_bad_id_and_bad_dates(tmp_path):
    assert m.main(['e1'], root=tmp_path) == 0
    assert m.main(['e1'], root=tmp_path) == 1
    assert m.main(['Bad-Id'], root=tmp_path) == 2
    assert m.main(['e2', '--from', '1949-13-01'], root=tmp_path) == 2
    assert m.main(['e3', '--from', '1949-02-01', '--to', '1949-01-01'], root=tmp_path) == 2
    assert not (tmp_path / 'content/events/e2.yaml').exists()
