# Spike tool (branch promo-ai3d). Run TRELLIS.2-4B locally on Apple Silicon (PyTorch MPS) through the trellis-mac port
# (github.com/shivampkumar/trellis-mac, cloned + patched in $TRELLIS_MAC, default ~/ai3d/trellis-mac), venv ~/ai3d/venv.
# No cloud, no token is ever read or printed by this script: huggingface_hub reads ~/.config/huggingface/token itself.
#
# Usage (always under the machine lock, so it never shares the GPU with Unity jobs or frame-time profiles):
#   uv run tools/machine_lock.py --exclusive --wait-unity -- ~/ai3d/venv/bin/python -I tools/ai3d/run_local.py \
#       <rundir> [--pipeline-type 512|1024|1024_cascade] [--seed N] [--texture-size 1024] <name>:<rgba.png> [...]
#   ~/ai3d/venv/bin/python -I tools/ai3d/run_local.py --check <name>:<rgba.png> ...   # preflight only, no model load
#   --smoke: replace DINOv3 with an all-zero condition (= the pipeline's own negative condition, i.e. unconditional) to
#            time/validate the whole local MPS path before the gated DINOv3 weights exist. Output is NOT an asset.
# Writes <rundir>/<name>/{raw.glb, raw_full.obj, preprocessed.png, run.json}; then post.py / render.py / compare.py as before.
#
# RMBG-2.0 (briaai, CC BY-NC 4.0) must never run: (1) inputs are rejected unless they are RGBA with real alpha, which is
# the only case where Trellis2ImageTo3DPipeline.preprocess_image skips self.rembg_model; (2) the pipeline's rembg class is
# replaced BEFORE from_pretrained with _NoRembg, so RMBG-2.0 is neither downloaded nor its trust_remote_code executed, and
# any call raises; (3) after loading we assert pipeline.rembg_model is _NoRembg.
import sys, os, json, time, datetime, threading, argparse

TRELLIS_MAC = os.path.expanduser(os.environ.get('TRELLIS_MAC', '~/ai3d/trellis-mac'))
TOKEN_FILE = os.path.expanduser('~/.config/huggingface/token')
DINO = 'facebook/dinov3-vitl16-pretrain-lvd1689m'
os.environ.setdefault('PYTORCH_ENABLE_MPS_FALLBACK', '1')
os.environ.setdefault('ATTN_BACKEND', 'sdpa'); os.environ.setdefault('SPARSE_ATTN_BACKEND', 'sdpa')
os.environ.setdefault('SPARSE_CONV_BACKEND', 'none')  # no Metal toolchain -> no mtlgemm/flex_gemm (see ai3d-local-report.md)
os.environ['HF_HUB_DISABLE_TELEMETRY'] = '1'
if os.path.exists(TOKEN_FILE): os.environ.setdefault('HF_TOKEN_PATH', TOKEN_FILE)  # path only; contents never touched here
sys.path.insert(0, os.path.join(TRELLIS_MAC, 'TRELLIS.2')); sys.path.insert(1, TRELLIS_MAC)
sys.path.append(os.path.join(TRELLIS_MAC, 'stubs'))
import numpy as np
from PIL import Image


def now(): return datetime.datetime.now(datetime.timezone.utc).isoformat(timespec='seconds')
def say(*a): print(f'[{now()}]', *a, flush=True)


def check_rgba(path):
    """Return None if the image is RGBA with real (not all-opaque) alpha, else the reason. Mirrors preprocess_image."""
    im = Image.open(path)
    if im.mode != 'RGBA': return f'mode {im.mode}, need RGBA'
    a = np.asarray(im)[:, :, 3]
    if np.all(a == 255): return 'alpha channel is fully opaque'
    if not (a > 0.8 * 255).any(): return 'no pixel with alpha > 0.8 (pipeline bbox would be empty)'
    return None


class _NoRembg:
    """Stand-in for trellis2.pipelines.rembg.BiRefNet (which would load briaai/RMBG-2.0, CC BY-NC, trust_remote_code)."""
    def __init__(self, *a, **k): pass
    def to(self, *a, **k): return self
    def cpu(self): return self
    def cuda(self): return self
    def __call__(self, *a, **k): raise RuntimeError('RMBG-2.0 is banned (CC BY-NC): input must be RGBA with real alpha')


class Peak(threading.Thread):
    """Sample process RSS and MPS driver memory (unified memory) every 0.5 s; keep peaks."""
    def __init__(self):
        super().__init__(daemon=True); self.rss = 0; self.mps = 0; self.stop = False
    def run(self):
        import psutil, torch
        p = psutil.Process()
        while not self.stop:
            self.rss = max(self.rss, p.memory_info().rss)
            try: self.mps = max(self.mps, torch.mps.driver_allocated_memory())
            except Exception: pass
            time.sleep(0.5)


def preflight(jobs):
    from huggingface_hub import try_to_load_from_cache
    rep = {'token_file_present': os.path.exists(TOKEN_FILE),
           'trellis_mac': TRELLIS_MAC, 'patched': os.path.exists(os.path.join(TRELLIS_MAC, 'TRELLIS.2/trellis2/modules/sparse/conv/conv_none.py')),
           'trellis2_4b_cached': isinstance(try_to_load_from_cache('microsoft/TRELLIS.2-4B', 'pipeline.json'), str),
           'dinov3_cached': isinstance(try_to_load_from_cache(DINO, 'config.json'), str),
           'rmbg_cached': isinstance(try_to_load_from_cache('briaai/RMBG-2.0', 'config.json'), str),
           'inputs': {n: (check_rgba(p) or 'ok') for n, p in jobs}}
    return rep


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true')
    ap.add_argument('--smoke', action='store_true')
    ap.add_argument('--pipeline-type', default='1024_cascade', choices=['512', '1024', '1024_cascade'])
    ap.add_argument('--seed', type=int, default=1)
    ap.add_argument('--texture-size', type=int, default=1024)
    ap.add_argument('rest', nargs='+')
    a = ap.parse_args()
    rundir = None if a.check else os.path.abspath(a.rest[0])
    jobs = [s.split(':', 1) for s in (a.rest if a.check else a.rest[1:])]
    rep = preflight(jobs); say('preflight', json.dumps(rep))
    bad = {n: r for n, r in rep['inputs'].items() if r != 'ok'}
    if bad: say('REFUSING non-RGBA inputs (would route through RMBG-2.0):', bad); sys.exit(4)
    if a.check: return
    if not a.smoke and not rep['dinov3_cached']:
        if not rep['token_file_present']:
            say(f'BLOCKED: {DINO} is gated and not cached, and {TOKEN_FILE} is missing. Accept the licence on HF, save a token there, rerun.')
            sys.exit(5)
        from huggingface_hub import hf_hub_download
        from huggingface_hub.errors import GatedRepoError
        try: hf_hub_download(DINO, 'config.json')
        except GatedRepoError:  # message never echoed (it can carry account details)
            say(f'BLOCKED: token is set but access to {DINO} is not granted. Accept the terms at https://huggingface.co/{DINO} and rerun.')
            sys.exit(5)

    import torch
    import trellis2.pipelines.rembg as rembg_mod
    rembg_mod.BiRefNet = _NoRembg                         # guard (2): must precede from_pretrained
    if a.smoke:
        import trellis2.modules.image_feature_extractor as ife
        class _ZeroCond:  # DINOv3 ViT-L/16: 1 cls + 4 register + (S/16)^2 patch tokens, dim 1024
            def __init__(self, *x, **k): self.image_size = 512
            def to(self, *x, **k): return self
            def cpu(self): return self
            def __call__(self, images): return torch.zeros(len(images), 5 + (self.image_size // 16) ** 2, 1024, device='mps')
        ife.DinoV3FeatureExtractor = _ZeroCond
    from trellis2.pipelines.trellis2_image_to_3d import Trellis2ImageTo3DPipeline
    peak = Peak(); peak.start()
    t0 = time.time(); say('loading pipeline')
    pipe = Trellis2ImageTo3DPipeline.from_pretrained('microsoft/TRELLIS.2-4B')
    assert isinstance(pipe.rembg_model, _NoRembg), 'rembg guard failed'  # guard (3)
    pipe.to(torch.device('mps')); t_load = time.time() - t0; say('loaded', round(t_load), 's')

    for name, inp in jobs:
        out = os.path.join(rundir, name); os.makedirs(out, exist_ok=True)
        img = Image.open(inp); img.load()
        log = {'smoke_unconditional_not_an_asset': a.smoke, 'name': name, 'input': os.path.abspath(inp), 'engine': 'trellis-mac (TRELLIS.2-4B, PyTorch MPS, local)',
               'backends': {k: os.environ[k] for k in ('ATTN_BACKEND', 'SPARSE_ATTN_BACKEND', 'SPARSE_CONV_BACKEND')},
               'torch': torch.__version__, 'params': {'seed': a.seed, 'pipeline_type': a.pipeline_type, 'steps': 'pipeline.json defaults',
               'texture_size': a.texture_size}, 'pipeline_load_s': round(t_load, 1), 'started': now()}
        peak.rss = peak.mps = 0
        t = {}; t1 = time.time()
        pre = pipe.preprocess_image(img); pre.save(os.path.join(out, 'preprocessed.png')); t['preprocess'] = time.time() - t1
        t1 = time.time()
        res = pipe.run(pre, seed=a.seed, pipeline_type=a.pipeline_type, preprocess_image=False)
        mesh = res[0] if isinstance(res, list) else res
        t['generate'] = time.time() - t1
        V = mesh.vertices.cpu().numpy(); F = mesh.faces.cpu().numpy()
        if len(V) == 0 or len(F) == 0: say(name, 'EMPTY MESH (macOS GPU watchdog? see trellis-mac generate.py)'); log['error'] = 'empty mesh'; json.dump(log, open(os.path.join(out, 'run.json'), 'w'), indent=1); continue
        with open(os.path.join(out, 'raw_full.obj'), 'w') as f:
            for v in V: f.write(f'v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n')
            for x in F + 1: f.write(f'f {x[0]} {x[1]} {x[2]}\n')
        log['raw_full'] = {'verts': int(len(V)), 'tris': int(len(F)), 'obj_bytes': os.path.getsize(os.path.join(out, 'raw_full.obj')),
                           'output_kind': 'voxel attribute volume (base_color/metallic/roughness/alpha) -> baked to UV texture; the raw mesh has no vertex colours'}
        # Texture bake: trellis-mac's KDTree baker (Metal baker needs the Xcode Metal Toolchain). 200k-face pre-simplify as upstream.
        t1 = time.time()
        from backends.texture_baker import uv_unwrap, bake_texture, export_glb_with_texture
        import fast_simplification
        bv, bf = V, F
        if len(F) > 200000: bv, bf = fast_simplification.simplify(V, F, 1.0 - 200000 / len(F))
        nv, nf, uvs, _ = uv_unwrap(bv, bf)
        base, mr, _ = bake_texture(nv, nf, uvs, mesh.coords.cpu().float().numpy(), mesh.attrs.cpu().float().numpy(),
                                   mesh.origin.cpu().float().numpy(), mesh.voxel_size, texture_size=a.texture_size)
        export_glb_with_texture(nv, nf, uvs, base, mr, os.path.join(out, 'raw.glb'))
        t['bake'] = time.time() - t1
        log.update({'finished': now(), 'secs': {k: round(v, 1) for k, v in t.items()}, 'wall_s': round(sum(t.values()), 1),
                    'peak_rss_gb': round(peak.rss / 2**30, 2), 'peak_mps_driver_gb': round(peak.mps / 2**30, 2),
                    'raw_glb': {'tris': int(len(nf)), 'verts': int(len(nv)), 'bytes': os.path.getsize(os.path.join(out, 'raw.glb'))}})
        json.dump(log, open(os.path.join(out, 'run.json'), 'w'), indent=1); say(name, 'DONE', json.dumps(log['secs']), log['peak_rss_gb'], 'GB rss')
        if torch.backends.mps.is_available(): torch.mps.empty_cache()
    peak.stop = True


if __name__ == '__main__':
    main()
