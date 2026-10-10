import fcntl
import os
import subprocess
import sys
import time
from pathlib import Path

import pytest

import machine_lock as ml


def test_the_command_follows_the_double_dash_and_a_mode_is_required():
    a = ml.parse(["--exclusive", "--wait-unity", "--", "uv", "run", "tools/web_check.py", "--query", "vprofile"])
    assert a.exclusive and a.wait_unity and a.command == ["uv", "run", "tools/web_check.py", "--query", "vprofile"]
    assert ml.parse(["--shared", "--", "echo", "x"]).command == ["echo", "x"]
    with pytest.raises(SystemExit):
        ml.parse(["--", "echo"])
    with pytest.raises(SystemExit):
        ml.parse(["--shared"])


def test_waiting_ends_when_no_unity_process_is_left():
    seen = iter([[101, 102], [102], []])
    clock = iter(range(100))
    assert ml.wait_for_unity(600, pids=lambda: next(seen), sleep=lambda s: None, now=lambda: next(clock)) is True


def test_waiting_gives_up_at_the_timeout():
    clock = iter(range(0, 1000, 100))
    assert ml.wait_for_unity(250, pids=lambda: [7], sleep=lambda s: None, now=lambda: next(clock)) is False


def test_a_shared_holder_really_blocks_an_exclusive_lock(tmp_path):
    lock = tmp_path / "machine.lock"
    env = {**os.environ, "STATECRAFT_MACHINE_LOCK": str(lock)}
    child = subprocess.Popen([sys.executable, ml.__file__, "--shared", "--", sys.executable, "-c", "import time; time.sleep(5)"],
                             env=env, stdout=subprocess.PIPE, text=True)
    try:
        assert "taking the machine lock (shared)" in child.stdout.readline()
        blocked, deadline = False, time.time() + 4
        while not blocked and time.time() < deadline:
            with open(lock, "a+") as f:
                try:
                    fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
                    fcntl.flock(f, fcntl.LOCK_UN)
                    time.sleep(0.05)
                except BlockingIOError:
                    blocked = True
        assert blocked, "an exclusive lock was granted while the shared holder ran"
    finally:
        child.terminate()
        child.wait(10)


def test_an_editor_holding_a_worktree_lockfile_counts_as_unity(tmp_path):
    temp = tmp_path / "a" / "unity" / "Temp"
    temp.mkdir(parents=True)
    (temp / "UnityLockfile").write_text("")
    (tmp_path / "b").mkdir()
    asked = []
    pids = ml.lockfile_holders([tmp_path / "a", tmp_path / "b"], lsof=lambda files: (asked.extend(files), "4242\n4242\n")[1])
    assert pids == [4242]
    assert asked == [str(temp / "UnityLockfile")]
    assert ml.lockfile_holders([tmp_path / "b"], lsof=lambda files: "1\n") == []
