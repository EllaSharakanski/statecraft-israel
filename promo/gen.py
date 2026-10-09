# /// script
# requires-python = ">=3.11"
# dependencies = ["requests"]
# ///
"""Generate promo concept art via ElevenLabs /v1/flows/image (same endpoint and fields as
drowning-video/tools/image.py, which ran successfully on 2026-10-08).

Usage: uv run promo/gen.py SHOT_ID [MODEL]   (shot prompts live in promo/shots.json)
The API key is read from drowning-video/.env and never printed. Refuses to run if the shared
credit pool would drop below FLOOR."""
import json, sys, time
from pathlib import Path
import requests

BASE = "https://api.elevenlabs.io"
ENV = Path.home() / "drowning-video" / ".env"
HERE = Path(__file__).resolve().parent
FLOOR = 50_000          # credits left for the drowning-video project, never spent here
ESTIMATE = 2_000        # upper bound seen per 2K image on this account (Nano Banana Pro: 1,827)

def session() -> requests.Session:
    key = next((l.split("=", 1)[1].strip() for l in ENV.read_text().splitlines()
                if l.startswith("ELEVENLABS_API_KEY=")), None)
    if not key:
        raise SystemExit("ELEVENLABS_API_KEY missing")
    s = requests.Session(); s.headers["xi-api-key"] = key
    return s

def left(s) -> int:
    j = s.get(f"{BASE}/v1/user/subscription", timeout=30).json()
    return int(j["character_limit"]) - int(j["character_count"])

def main(shot_id: str, model: str = "gpt-image-2.5-sunburst"):
    shot = json.loads((HERE / "shots.json").read_text())[shot_id]
    s = session()
    before = left(s)
    if before - ESTIMATE < FLOOR:
        raise SystemExit(f"BLOCKED: {before} left, floor {FLOOR}")
    body = {"model_id": model, "prompt": shot["prompt"], "aspect_ratio": shot.get("aspect", "16:9"),
            "resolution": "2K"}
    if model.startswith("gpt-image"):
        body["quality"] = "high"
    r = s.post(f"{BASE}/v1/flows/image", json=body, timeout=120); r.raise_for_status()
    gid = r.json()["id"]
    out = HERE / "out"; out.mkdir(exist_ok=True)
    tag = f"{shot_id}-{model.split('-')[0]}"
    (out / f"{tag}.id").write_text(gid)  # paid: keep the id for recovery
    t0 = time.time()
    while True:
        time.sleep(5)
        try:
            j = s.get(f"{BASE}/v1/flows/image/{gid}", timeout=30).json()
        except (requests.RequestException, ValueError):
            continue
        if j.get("status") == "completed": break
        if j.get("status") == "failed": raise SystemExit(f"{tag} failed: {j}")
        if time.time() - t0 > 900: raise SystemExit(f"{tag} timeout; id saved")
    d = requests.get(j["content_url"], timeout=300); d.raise_for_status()  # signed CDN URL: no key sent
    ext = {"image/jpeg": ".jpg", "image/webp": ".webp"}.get(d.headers.get("content-type", ""), ".png")
    path = out / (tag + ext); path.write_bytes(d.content)
    print(path)  # cost is measured per batch (parallel runs share one counter): see `uv run promo/gen.py balance`

if __name__ == "__main__":
    if sys.argv[1:] == ["balance"]:
        print("left", left(session()))
    else:
        main(*sys.argv[1:])
