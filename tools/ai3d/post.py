# Post-process a TRELLIS.2 output into a game LOD: geometry quadric decimation, fresh xatlas UVs, a texture baked onto
# the LOD, normalised (base on y=0, centred, height 1, Y-up), exported as GLB + OBJ/MTL/PNG/JPG + post.json.
# Usage: python -I post.py <raw.npz|raw.glb> <outdir> <target_tris> <tex_px> <name> [--zup]
#   raw.npz (run_local.py): the full decoder mesh + sparse PBR voxel volume, TRELLIS native Z-up. Each texel takes the
#     inverse-distance-weighted base colour of its 8 nearest voxel centres (no distance cut-off; values are sRGB already,
#     as o_voxel.postprocess.to_glb writes them).
#   raw.glb (HF Space trial, or run_local.py --bake trellis): colour from the 4 nearest high-mesh face centroids, sampled
#     from the GLB's texture at the centroid UV. --zup: the GLB is in the native Z-up frame (run_local.py --bake trellis).
# Why rebake: a texture-aware quadric collapse stalls on the seams of an xatlas atlas (200k -> 76k triangles, not 2k).
import sys, os, json
import numpy as np, trimesh, pymeshlab, xatlas
from PIL import Image
from scipy.spatial import cKDTree
raw, out, target, texpx, name = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
os.makedirs(out, exist_ok=True)
if raw.endswith('.npz'):
    z = np.load(raw)
    Vh = z['vertices'].astype(np.float64); Fh = z['faces'].astype(np.int32); ZUP = True
    vs = float(z['voxel_size']); lay = json.loads(str(z['layout'])); bc = slice(*lay['base_color'])
    centres = z['coords'].astype(np.float64) * vs + z['origin'].astype(np.float64) + 0.5 * vs
    vox_rgb = np.clip(z['attrs'][:, bc].astype(np.float32), 0, 1) * 255
    tree = cKDTree(centres)
    def colour(P):
        d, i = tree.query(P, k=8, workers=-1); w = 1.0 / (d + 0.1 * vs); w /= w.sum(1, keepdims=True)
        return (vox_rgb[i] * w[..., None]).sum(1)
    info = {'name': name, 'source': 'raw.npz (mesh + PBR voxel volume)', 'raw_tris': int(len(Fh)), 'raw_verts': int(len(Vh)),
            'raw_voxels': int(len(centres)), 'voxel_size': vs, 'bake': 'xatlas + 8-NN IDW from voxel centres'}
else:
    scene = trimesh.load(raw, force='scene')
    geoms = list(scene.dump()) if hasattr(scene, 'dump') else [scene]
    mesh = trimesh.util.concatenate(geoms) if len(geoms) > 1 else geoms[0]
    mat = mesh.visual.material; base = getattr(mat, 'baseColorTexture', None) or getattr(mat, 'image', None)
    Vh = np.asarray(mesh.vertices, dtype=np.float64); Fh = np.asarray(mesh.faces, dtype=np.int32); ZUP = '--zup' in sys.argv[6:]
    uv = np.asarray(mesh.visual.uv, dtype=np.float64)
    src = np.asarray(base.convert('RGB'), dtype=np.float32) if base is not None else np.full((2, 2, 3), 255, np.float32)
    sh_, sw_ = src.shape[:2]
    uvc = uv[Fh].mean(1)  # a vertex UV sits on its chart's outline, which some bakers leave dark: sample at centroids
    Ch = src[np.clip(((1 - uvc[:, 1] % 1.0) * sh_).astype(int), 0, sh_ - 1), np.clip(((uvc[:, 0] % 1.0) * sw_).astype(int), 0, sw_ - 1)]
    tree = cKDTree(Vh[Fh].mean(1))
    def colour(P):
        d, i = tree.query(P, k=4, workers=-1); w = 1.0 / np.maximum(d, 1e-9); w /= w.sum(1, keepdims=True)
        return (Ch[i] * w[..., None]).sum(1)
    info = {'name': name, 'source': 'GLB', 'raw_tris': int(len(Fh)), 'raw_verts': int(len(Vh)), 'raw_tex': list(base.size) if base is not None else None,
            'bake': 'xatlas + 4-NN IDW from high-mesh face centroids'}
ms = pymeshlab.MeshSet()
ms.add_mesh(pymeshlab.Mesh(vertex_matrix=Vh, face_matrix=Fh))
ms.meshing_merge_close_vertices(threshold=pymeshlab.PercentageValue(0.01))
ms.meshing_decimation_quadric_edge_collapse(targetfacenum=target, qualitythr=0.5, preserveboundary=False, preservenormal=True,
                                            optimalplacement=True, planarquadric=True, autoclean=True)
d = ms.current_mesh(); Vl = d.vertex_matrix(); Fl = d.face_matrix()
# Winding: trellis-mac's pure-Python dual-grid extraction flips about 8% of its quads (110,782 conflicting edges over 1.4M
# faces on moshava), and the decimation spreads it (a third of the LOD's faces). Unity culls back faces, so the LOD drew
# as shattered debris. Make each connected piece coherent (trimesh BFS), then flip the pieces that face inward.
lm = trimesh.Trimesh(Vl, Fl, process=False)
bad0 = int((np.unique(lm.edges, axis=0, return_counts=True)[1] > 1).sum())
trimesh.repair.fix_winding(lm)
for comp in trimesh.graph.connected_components(lm.face_adjacency, nodes=np.arange(len(lm.faces)), min_len=1):
    c = np.asarray(comp); centre = lm.triangles_center[c].mean(0)
    if np.sum(lm.face_normals[c] * (lm.triangles_center[c] - centre) * lm.area_faces[c, None]) < 0:
        lm.faces[c] = lm.faces[c][:, ::-1]
Fl = np.asarray(lm.faces, dtype=np.int64)
info['winding'] = {'conflicting_edges_before': bad0, 'after': int((np.unique(trimesh.Trimesh(Vl, Fl, process=False).edges, axis=0, return_counts=True)[1] > 1).sum())}
vmap, Fd, UVd = xatlas.parametrize(Vl.astype(np.float32), Fl.astype(np.uint32))
Vd = Vl[vmap].astype(np.float64); Fd = Fd.astype(np.int64); UVd = UVd.astype(np.float64)
# rasterise the LOD's atlas: one 3D point per covered texel
T = texpx; P = []; Q = []
for f in Fd:
    t = UVd[f] * T; p3 = Vd[f]
    x0, x1 = int(max(0, np.floor(t[:, 0].min()))), int(min(T - 1, np.ceil(t[:, 0].max())))
    y0, y1 = int(max(0, np.floor(t[:, 1].min()))), int(min(T - 1, np.ceil(t[:, 1].max())))
    den = (t[1, 1] - t[2, 1]) * (t[0, 0] - t[2, 0]) + (t[2, 0] - t[1, 0]) * (t[0, 1] - t[2, 1])
    if abs(den) < 1e-12: continue
    gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
    w0 = ((t[1, 1] - t[2, 1]) * (gx - t[2, 0]) + (t[2, 0] - t[1, 0]) * (gy - t[2, 1])) / den
    w1 = ((t[2, 1] - t[0, 1]) * (gx - t[2, 0]) + (t[0, 0] - t[2, 0]) * (gy - t[2, 1])) / den
    w2 = 1 - w0 - w1; m = (w0 >= -1e-4) & (w1 >= -1e-4) & (w2 >= -1e-4)
    if not m.any(): continue
    P.append(w0[m, None] * p3[0] + w1[m, None] * p3[1] + w2[m, None] * p3[2])
    Q.append(np.stack([(gx[m] - 0.5).astype(int), (gy[m] - 0.5).astype(int)], 1))
P = np.concatenate(P); Q = np.concatenate(Q)
img = np.zeros((T, T, 3), np.float32); filled = np.zeros((T, T), bool)
rows = T - 1 - Q[:, 1]; img[rows, Q[:, 0]] = colour(P); filled[rows, Q[:, 0]] = True
# fill the gutters from the nearest baked texel so mip-mapping does not bleed black into the seams
ey, ex = np.nonzero(~filled); fy, fx = np.nonzero(filled)
_, nn = cKDTree(np.stack([fy, fx], 1)).query(np.stack([ey, ex], 1)); img[ey, ex] = img[fy[nn], fx[nn]]
tex = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
info['texel_coverage'] = round(float(len(fy)) / (T * T), 3)
if ZUP:  # (x, y, z) -> (x, z, -y): Z-up to Y-up as o_voxel.to_glb does; a proper rotation, so the winding is kept
    Vd = np.stack([Vd[:, 0], Vd[:, 2], -Vd[:, 1]], 1)
Vd = Vd - [(Vd[:, 0].max() + Vd[:, 0].min()) / 2, Vd[:, 1].min(), (Vd[:, 2].max() + Vd[:, 2].min()) / 2]
Vd = Vd / Vd[:, 1].max()
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
