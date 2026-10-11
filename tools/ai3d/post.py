# Post-process a TRELLIS.2 GLB: decimate (texture-aware quadric), downsize texture, export GLB + OBJ/MTL/PNG, render turntable.
# Usage: python -I post.py <raw.glb> <outdir> <target_tris> <tex_px> <name>
import sys, os, json, io, math
import numpy as np, trimesh, pymeshlab
from PIL import Image
raw, out, target, texpx, name = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
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
# pymeshlab: wedge-texcoord decimation needs per-wedge UVs; build from per-vertex UV.
ms = pymeshlab.MeshSet()
m = pymeshlab.Mesh(vertex_matrix=np.asarray(mesh.vertices, dtype=np.float64), face_matrix=np.asarray(mesh.faces, dtype=np.int32),
                   v_tex_coords_matrix=uv)
ms.add_mesh(m)
ms.compute_texcoord_transfer_vertex_to_wedge()
ms.meshing_merge_close_vertices(threshold=pymeshlab.PercentageValue(0.01))
ms.meshing_decimation_quadric_edge_collapse_with_texture(targetfacenum=target, qualitythr=0.5, extratcoordw=1.0,
                                                          preserveboundary=True, optimalplacement=True, preservenormal=True, planarquadric=True)
d = ms.current_mesh()
V = d.vertex_matrix(); F = d.face_matrix(); W = d.wedge_tex_coord_matrix().reshape(-1, 3, 2)
# split to per-wedge vertices so UV seams survive
V3 = V[F].reshape(-1, 3); UV3 = W.reshape(-1, 2); F3 = np.arange(len(V3)).reshape(-1, 3)
dm = trimesh.Trimesh(V3, F3, process=False); dm.merge_vertices(merge_tex=False, merge_norm=True) if False else None
# merge identical (pos,uv) pairs
key = np.round(np.hstack([V3, UV3]), 6); uniq, inv = np.unique(key, axis=0, return_inverse=True)
inv = inv.reshape(-1); Vd = uniq[:, :3]; UVd = uniq[:, 3:]; Fd = inv[F3]
tex = base.convert('RGB').resize((texpx, texpx), Image.LANCZOS) if base is not None else Image.new('RGB', (texpx, texpx), 'white')
# normalise: base on y=0, centred, height 1
Vd = Vd - [ (Vd[:,0].max()+Vd[:,0].min())/2, Vd[:,1].min(), (Vd[:,2].max()+Vd[:,2].min())/2 ]
h = Vd[:,1].max(); Vd = Vd / h
outm = trimesh.Trimesh(Vd, Fd, process=False, visual=trimesh.visual.TextureVisuals(uv=UVd, material=trimesh.visual.material.PBRMaterial(baseColorTexture=tex, metallicFactor=0.0, roughnessFactor=0.9)))
glb = outm.export(file_type='glb'); open(os.path.join(out, f'{name}.glb'), 'wb').write(glb)
tex.save(os.path.join(out, f'{name}.png'), optimize=True); tex.save(os.path.join(out, f'{name}.jpg'), quality=85)
with open(os.path.join(out, f'{name}.obj'), 'w') as f:
    f.write(f'# {name}: TRELLIS.2 (microsoft/TRELLIS.2 Space) output, decimated\nmtllib {name}.mtl\nusemtl m\n')
    for v in Vd: f.write(f'v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n')
    for t in UVd: f.write(f'vt {t[0]:.5f} {t[1]:.5f}\n')
    for a, b, c in Fd + 1: f.write(f'f {a}/{a} {b}/{b} {c}/{c}\n')
open(os.path.join(out, f'{name}.mtl'), 'w').write(f'newmtl m\nKd 1 1 1\nmap_Kd {name}.png\n')
ext = Vd.max(0) - Vd.min(0)
info.update({'tris': int(len(Fd)), 'verts': int(len(Vd)), 'tex': texpx, 'glb_bytes': len(glb), 'png_bytes': os.path.getsize(os.path.join(out, f'{name}.png')),
             'jpg_bytes': os.path.getsize(os.path.join(out, f'{name}.jpg')), 'extent_w_h_d_norm': [round(float(x), 3) for x in ext],
             'watertight': bool(outm.is_watertight)})
json.dump(info, open(os.path.join(out, 'post.json'), 'w'), indent=1); print(json.dumps(info))
