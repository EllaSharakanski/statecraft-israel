# Spike tool (branch promo-ai3d). Run from the working dir that holds crops/ and final/<name>/ (post.py output).
# One row per asset: promo 03 source region | our RGBA input | raw TRELLIS GLB (front) | decimated (4 yaws).  Usage: compare.py <out.png> name...
import sys, json, os
from PIL import Image, ImageDraw
sys.path.insert(0, os.path.dirname(__file__))
import render as R
inputs = json.load(open('crops/inputs.json')); promo = Image.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'promo', 'final', '03-gameplay-1936.png')).convert('RGB')
H = 240; rows = []
for name in sys.argv[2:]:
    d = f'final/{name}'
    if not os.path.exists(f'{d}/{name}.obj'): continue
    x0, y0, x1, y1 = inputs[name]['box_xyxy']
    src = promo.crop((x0, y0, x1, y1)); src = src.resize((int(src.width * H / src.height), H), Image.LANCZOS)
    inp = Image.open(f'crops/in_{name}.png'); bg = Image.new('RGB', inp.size, (210, 205, 195)); bg.paste(inp, (0, 0), inp); inp = bg.resize((int(inp.width * H / inp.height), H), Image.LANCZOS)
    V, T, F, tex = R.load(d, name)
    views = [R.render(V, T, F, tex, y, pitch=30, S=H) for y in (0, 60, 180, 270)]
    post = json.load(open(f'{d}/post.json'))
    tiles = [src, inp] + views
    if os.path.exists(f'{d}/raw_front.png'): tiles.insert(2, Image.open(f'{d}/raw_front.png').resize((H, H)))
    W = sum(t.width for t in tiles) + 8 * len(tiles); row = Image.new('RGB', (W, H + 20), 'white'); x = 0
    for t in tiles: row.paste(t, (x, 20)); x += t.width + 8
    ImageDraw.Draw(row).text((4, 4), f"{name}: promo 03 crop | input | {'raw TRELLIS ' + str(post['raw_tris']) + ' tris | ' if len(tiles) == 7 else ''}decimated {post['tris']} tris, {post['tex']}px tex (yaw 0/60/180/270)", fill=(0, 0, 0))
    rows.append(row)
W = max(r.width for r in rows); sheet = Image.new('RGB', (W, sum(r.height for r in rows)), 'white'); y = 0
for r in rows: sheet.paste(r, (0, y)); y += r.height
sheet.save(sys.argv[1]); print(sheet.size)
