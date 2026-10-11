# Post-process a TRELLIS.2 GLB: decimate (geometry quadric), new UVs, rebake the texture, export GLB + OBJ/MTL/PNG.
# Usage: python -I post.py <raw.glb> <outdir> <target_tris> <tex_px> <name> [--zup]
#   --zup: the GLB is in TRELLIS's native Z-up frame (run_local.py's KDTree bake); rotate to Y-up as o_voxel.to_glb does.
import sys, os, json, io, math
import numpy as np, trimesh, pymeshlab
from PIL import Image
raw, out, target, texpx, name = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
ZUP = '--zup' in sys.argv[6:]
os.makedirs(out, exist_ok=True)
scene = trimesh.load(raw, force='scene')
geoms = list(scene.dump()) if hasattr(scene, 'dump') else [scene]
mesh = trimesh.util.concatenate(geoms) if len(geoms) > 1 else geoms[0]
mat = mesh.visual.material
base = getattr(mat, 'baseColorTexture', None) or getattr(mat, 'image', None)
info = {'name': name, 'raw_tris': int(len(mesh.faces)), 'raw_verts': int(len(mesh.vertices)), 'raw_tex': list(base.size) if base is not None else None,
        'raw_material': type(mat).__name__,
        'raw_has_normal_map': getattr(mat, 'normalTexture', None) is not None, 'raw_has_mr_map': getattr(mat, 'metallicRoughnessTexture', None) is not None}
uv = np.asarray(mesh.visual.uv, dtype=np.float64)
if ZUP:  # (x, y, z) -> (x, z, -y): Z-up to Y-up, a proper rotation, so winding is kept
    v = np.asarray(mesh.vertices, dtype=np.float64); mesh.vertices = np.stack([v[:, 0], v[:, 2], -v[:, 1]], 1)
# Geometry-only quadric decimation (a texture-aware collapse stalls on the many UV seams of an xatlas atlas: 200k -> 76k
# instead of 2k), then fresh xatlas UVs on the low mesh and a bake: each texel of the low mesh's atlas takes the
# inverse-distance-weighted colour of the 4 nearest high-mesh vertices (colour sampled from the original texture),
# then empty texels are filled from the nearest baked one so mip-mapping does not bleed the background into seams.
import xatlas
from scipy.spatial import cKDTree
Vh = np.asarray(mesh.vertices, dtype=np.float64); Fh = np.asarray(mesh.faces, dtype=np.int32)
src = np.asarray(base.convert('RGB'), dtype=np.float32) if base is not None else np.full((2, 2, 3), 255, np.float32)
sh_, sw_ = src.shape[:2]
Ch = src[np.clip(((1 - uv[:, 1] % 1.0) * sh_).astype(int), 0, sh_ - 1), np.clip(((uv[:, 0] % 1.0) * sw_).astype(int), 0, sw_ - 1)]
ms = pymeshlab.MeshSet()
ms.add_mesh(pymeshlab.Mesh(vertex_matrix=Vh, face_matrix=Fh))
ms.meshing_merge_close_vertices(threshold=pymeshlab.PercentageValue(0.01))
ms.meshing_decimation_quadric_edge_collapse(targetfacenum=target, qualitythr=0.5, preserveboundary=False, preservenormal=True,
                                            optimalplacement=True, planarquadric=True, autoclean=True)
d = ms.current_mesh(); Vl = d.vertex_matrix(); Fl = d.face_matrix()
vmap, Fd, UVd = xatlas.parametrize(Vl.astype(np.float32), Fl.astype(np.uint32))
Vd = Vl[vmap].astype(np.float64); Fd = Fd.astype(np.int64); UVd = UVd.astype(np.float64)
T = texpx; P = []; Q = []
for f in Fd:
    t = UVd[f] * T; p3 = Vd[f]
    x0, x1 = int(max(0, np.floor(t[:, 0].min()))), int(min(T - 1, np.ceil(t[:, 0].max())))
    y0, y1 = int(max(0, np.floor(t[:, 1].min()))), int(min(T - 1, np.ceil(t[:, 1].max())))
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
    den = (t[1, 1] - t[2, 1]) * (t[0, 0] - t[2, 0]) + (t[2, 0] - t[1, 0]) * (t[0, 1] - t[2, 1])
    if abs(den) < 1e-12: continue
    w0 = ((t[1, 1] - t[2, 1]) * (gx - t[2, 0]) + (t[2, 0] - t[1, 0]) * (gy - t[2, 1])) / den
    w1 = ((t[2, 1] - t[0, 1]) * (gx - t[2, 0]) + (t[0, 0] - t[2, 0]) * (gy - t[2, 1])) / den
    w2 = 1 - w0 - w1; m = (w0 >= -1e-4) & (w1 >= -1e-4) & (w2 >= -1e-4)
    if not m.any(): continue
    P.append(w0[m, None] * p3[0] + w1[m, None] * p3[1] + w2[m, None] * p3[2])
    Q.append(np.stack([(gx[m] - 0.5).astype(int), (gy[m] - 0.5).astype(int)], 1))
P = np.concatenate(P); Q = np.concatenate(Q)
dist, idx = cKDTree(Vh).query(P, k=4); w = 1.0 / np.maximum(dist, 1e-9); w /= w.sum(1, keepdims=True)
img = np.zeros((T, T, 3), np.float32); filled = np.zeros((T, T), bool)
rows = T - 1 - Q[:, 1]; img[rows, Q[:, 0]] = (Ch[idx] * w[..., None]).sum(1); filled[rows, Q[:, 0]] = True
ey, ex = np.nonzero(~filled); fy, fx = np.nonzero(filled)
_, nn = cKDTree(np.stack([fy, fx], 1)).query(np.stack([ey, ex], 1)); img[ey, ex] = img[fy[nn], fx[nn]]
tex = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
info.update({'bake': 'geometry quadric + xatlas + 4-NN IDW vertex-colour bake', 'texel_coverage': round(float(len(fy)) / (T * T), 3)})
# normalise: base on y=0, centred, height 1
Vd = Vd - [ (Vd[:,0].max()+Vd[:,0].min())/2, Vd[:,1].min(), (Vd[:,2].max()+Vd[:,2].min())/2 ]
h = Vd[:,1].max(); Vd = Vd / h
outm = trimesh.Trimesh(Vd, Fd, process=False, visual=trimesh.visual.TextureVisuals(uv=UVd, material=trimesh.visual.material.PBRMaterial(baseColorTexture=tex, metallicFactor=0.0, roughnessFactor=0.9)))
glb = outm.export(file_type='glb'); open(os.path.join(out, f'{name}.glb'), 'wb').write(glb)
tex.save(os.path.join(out, f'{name}.png'), optimize=True); tex.save(os.path.join(out, f'{name}.jpg'), quality=85)
with open(os.path.join(out, f'{name}.obj'), 'w') as f:
    f.write(f'# {name}: TRELLIS.2-4B output, decimated\nmtllib {name}.mtl\nusemtl m\n')
    for v in Vd: f.write(f'v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n')
    for t in UVd: f.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
    for a, b, c in Fd + 1: f.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
open(os.path.join(out, f'{name}.mtl'), 'w').write(f'newmtl m\nKd 1 1 1\nmap_Kd {name}.png\n')
ext = Vd.max(0) - Vd.min(0)
info.update({'tris': int(len(Fd)), 'verts': int(len(Vd)), 'tex': texpx, 'glb_bytes': len(glb), 'png_bytes': os.path.getsize(os.path.join(out, f'{name}.png')),
             'jpg_bytes': os.path.getsize(os.path.join(out, f'{name}.jpg')), 'extent_w_h_d_norm': [round(float(x), 3) for x in ext],
             'watertight': bool(outm.is_watertight)})
json.dump(info, open(os.path.join(out, 'post.json'), 'w'), indent=1); print(json.dumps(info))
