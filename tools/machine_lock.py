# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""One Mac, several worktrees: frame-time runs must not share the machine with Unity imports, builds, test runs or an open
Unity editor.

Usage: uv run tools/machine_lock.py (--shared | --exclusive) [--wait-unity] [--timeout SECONDS] -- <command> [args...]
--shared takes the machine lock shared (Unity builds, captures and edit-mode runs); --exclusive takes it alone (frame-time
profiles and boot budgets) and, with --wait-unity, then waits until no Unity is left running: no batch-mode Unity process,
and no process (an interactive editor included) holding any worktree's unity/Temp/UnityLockfile. The lock file is
~/.cache/statecraft/machine.lock (fcntl.flock; STATECRAFT_MACHINE_LOCK overrides it), shared by every worktree of this
user. Exit code: the command's, or 3 when --wait-unity times out."""
import argparse
import fcntl
import os
import subprocess
import sys
import time
from pathlib import Path

LOCK = Path(os.environ.get("STATECRAFT_MACHINE_LOCK", str(Path.home() / ".cache" / "statecraft" / "machine.lock")))
UNITY_BATCH = "Unity.app/Contents/MacOS/Unity .*-batchmode"


def parse(argv: list[str]) -> argparse.Namespace:
    ap = argparse.ArgumentParser()
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--shared", action="store_true")
    mode.add_argument("--exclusive", action="store_true")
    ap.add_argument("--wait-unity", action="store_true")
    ap.add_argument("--timeout", type=float, default=3600)
    ap.add_argument("command", nargs=argparse.REMAINDER)
    a = ap.parse_args(argv)
    if a.command and a.command[0] == "--":
        a.command = a.command[1:]
    if not a.command:
        ap.error("no command after --")
    return a


def unity_batch_pids() -> list[int]:
    r = subprocess.run(["pgrep", "-f", UNITY_BATCH], capture_output=True, text=True)
    return [int(p) for p in r.stdout.split() if p.isdigit() and int(p) != os.getpid()]


def worktrees() -> list[Path]:
    """Every worktree of this repository, so an editor open in any of them is seen."""
    r = subprocess.run(["git", "worktree", "list", "--porcelain"], capture_output=True, text=True)
    return [Path(line[len("worktree "):]) for line in r.stdout.splitlines() if line.startswith("worktree ")]


def lockfile_holders(roots: list[Path], lsof=None) -> list[int]:
    """Pids holding any root's unity/Temp/UnityLockfile: every Unity editor with that project open, interactive or batch."""
    files = [str(p / "unity" / "Temp" / "UnityLockfile") for p in roots if (p / "unity" / "Temp" / "UnityLockfile").exists()]
    if not files:
        return []
    run = lsof or (lambda fs: subprocess.run(["lsof", "-t", *fs], capture_output=True, text=True).stdout)
    return sorted({int(p) for p in run(files).split() if p.isdigit()})


def unity_pids() -> list[int]:
    return sorted(set(unity_batch_pids()) | set(lockfile_holders(worktrees())))


def wait_for_unity(timeout: float, pids=unity_pids, sleep=time.sleep, now=time.monotonic) -> bool:
    deadline = now() + timeout
    while True:
        busy = pids()
        if not busy:
            return True
        if now() >= deadline:
            return False
        print(f"machine_lock: waiting for Unity processes {busy} (batch runs, or an editor with a worktree open)", flush=True)
        sleep(10)


def main(argv: list[str] | None = None) -> int:
    a = parse(sys.argv[1:] if argv is None else argv)
    LOCK.parent.mkdir(parents=True, exist_ok=True)
    with open(LOCK, "a+") as f:
        print(f"machine_lock: taking the machine lock ({'exclusive' if a.exclusive else 'shared'})", flush=True)
        fcntl.flock(f, fcntl.LOCK_EX if a.exclusive else fcntl.LOCK_SH)
        if a.wait_unity and not wait_for_unity(a.timeout):
            print("machine_lock: timed out waiting for Unity")
            return 3
        return subprocess.call(a.command)


if __name__ == "__main__":
    sys.exit(main())
