# Run assets through a TRELLIS HF Space anonymously (no token), waiting politely for the ZeroGPU quota.
# Usage: run_trellis.py <v2|v1> <rundir> <name>:<input.png> [...]
#   v2 = official microsoft/TRELLIS.2 (TRELLIS.2-4B): /preprocess_image, /image_to_3d (GPU 120 s), /extract_glb (GPU 120 s)
#   v1 = trellis-community/TRELLIS (microsoft/TRELLIS-image-large): /generate_and_extract_glb (one GPU 120 s call)
import sys, os, time, json, shutil, datetime, re
from gradio_client import Client, handle_file
mode, rundir, jobs = sys.argv[1], os.path.abspath(sys.argv[2]), [a.split(':', 1) for a in sys.argv[3:]]
SPACE = {'v2': 'microsoft/TRELLIS.2', 'v1': 'trellis-community/TRELLIS'}[mode]
MAX_WAIT = float(os.environ.get('MAX_WAIT_S', 6 * 3600))
def now(): return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec='seconds')
def say(*a): print(f'[{now()}]', *a, flush=True)
def quota_wait(msg):
    m = re.search(r'Try again in (\d+):(\d+):(\d+)', msg)
    left = re.search(r'\((\d+)s requested vs\. (\d+)s left\)', msg)
    return (int(m[1]) * 3600 + int(m[2]) * 60 + int(m[3]) + 90 if m else None), (left.groups() if left else None)
def run_one(name, inp, out):
    dl = os.path.join(out, 'dl'); os.makedirs(dl, exist_ok=True)
    c = Client(SPACE, verbose=False, download_files=dl); say(name, 'connected', SPACE)
    # The apps create TMP_DIR/<session_hash> in start_session, which only a browser page load fires (demo.load):
    # without it v1 writes its video/GLB into a missing dir and fails with FileNotFoundError after spending GPU quota.
    c.predict(api_name='/start_session')
    t = {}; t0 = time.time()
    if mode == 'v2':
        pre = c.predict(handle_file(inp), api_name='/preprocess_image'); t['preprocess'] = time.time() - t0; say(name, 'preprocessed', round(t['preprocess']))
        t1 = time.time()
        html = c.predict(handle_file(pre), 1, '1024', 7.5, 0.7, 12, 5.0, 7.5, 0.5, 12, 3.0, 1.0, 0.0, 12, 3.0, api_name='/image_to_3d')
        t['generate'] = time.time() - t1; say(name, 'generated', round(t['generate']))
        open(os.path.join(out, 'preview.html'), 'w').write(html)
        t2 = time.time(); glb, _ = c.predict(100000, 1024, api_name='/extract_glb'); t['extract'] = time.time() - t2
        params = {'seed': 1, 'resolution': '1024', 'steps': '12/12/12 (Space defaults)', 'decimation_target': 100000, 'texture_size': 1024}
    else:
        pre = c.predict(handle_file(inp), api_name='/preprocess_image'); t['preprocess'] = time.time() - t0; say(name, 'preprocessed', round(t['preprocess']))
        t1 = time.time()
        res = c.predict(handle_file(pre), [], 1, 7.5, 12, 3.0, 12, 'stochastic', 0.95, 1024, api_name='/generate_and_extract_glb')
        t['generate+extract'] = time.time() - t1
        vid, glb = res[0], res[1]
        if isinstance(vid, dict) and vid.get('video'): shutil.copy(vid['video'], os.path.join(out, 'preview.mp4'))
        params = {'seed': 1, 'steps': '12/12 (Space defaults)', 'ss_cfg': 7.5, 'slat_cfg': 3.0, 'mesh_simplify': 0.95, 'texture_size': 1024}
    shutil.copy(pre, os.path.join(out, 'preprocessed.png')); shutil.copy(glb, os.path.join(out, 'raw.glb'))
    return {k: round(v, 1) for k, v in t.items()}, params
waited = 0.0
for name, inp in jobs:
    out = os.path.join(rundir, name); os.makedirs(out, exist_ok=True)
    log = {'name': name, 'input': os.path.abspath(inp), 'space': f'https://huggingface.co/spaces/{SPACE}', 'mode': mode, 'attempts': []}
    while True:
        try:
            st = now(); secs, params = run_one(name, os.path.abspath(inp), out)
            log.update({'started': st, 'finished': now(), 'secs': secs, 'params': params, 'raw_glb_bytes': os.path.getsize(os.path.join(out, 'raw.glb'))})
            say(name, 'DONE', json.dumps(secs)); break
        except Exception as e:
            msg = str(e); say(name, 'FAILED', msg[:400]); log['attempts'].append({'at': now(), 'error': msg[:600]})
            json.dump(log, open(os.path.join(out, 'run.json'), 'w'), indent=1)
            low = msg.lower()
            if 'zerogpu quota' in low:
                w, left = quota_wait(msg)
                if left and int(left[1]) < 120 and w is None: w = 3600
                w = w or 1800
                if waited + w > MAX_WAIT: say('QUOTA: giving up, would wait', w, 's'); sys.exit(2)
                say(f'QUOTA left={left}, sleeping {w}s'); time.sleep(w); waited += w; continue
            if any(k in low for k in ('sign in', 'log in', 'login required', 'payment', 'subscribe')): say('BLOCKED: login/payment'); sys.exit(3)
            if len(log['attempts']) >= 4: say('giving up on', name); break
            time.sleep(120 * len(log['attempts']))
    json.dump(log, open(os.path.join(out, 'run.json'), 'w'), indent=1)
