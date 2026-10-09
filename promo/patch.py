# /// script
# requires-python = ">=3.11"
# dependencies = ["opencv-python-headless", "numpy", "pillow"]
# ///
"""Deterministic fixes for typos/counts that image-edit models get wrong.
Boxes are given in 1400-px-wide preview coordinates and scaled to each image."""
import math
import cv2, numpy as np
from PIL import Image, ImageDraw

def load(p): return cv2.imread(p, cv2.IMREAD_COLOR)
def sc(img): return img.shape[1] / 1400

def copy_word(src, dst, box, search=60):
    """Copy a correctly spelled region from src into the matching spot in dst (template-matched)."""
    s, d = sc(src), sc(dst)
    x0, y0, x1, y1 = [int(v * s) for v in box]
    crop = cv2.resize(src[y0:y1, x0:x1], None, fx=d / s, fy=d / s, interpolation=cv2.INTER_CUBIC)
    gx, gy = int(box[0] * d), int(box[1] * d)
    wx0, wy0 = max(gx - search, 0), max(gy - search, 0)
    win = dst[wy0:gy + crop.shape[0] + search, wx0:gx + crop.shape[1] + search]
    res = cv2.matchTemplate(cv2.cvtColor(win, cv2.COLOR_BGR2GRAY), cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY), cv2.TM_CCOEFF_NORMED)
    _, score, _, (mx, my) = cv2.minMaxLoc(res)
    if score < 0.6:
        raise SystemExit(f"alignment too weak ({score:.2f}) for box {box}")
    dst[wy0 + my:wy0 + my + crop.shape[0], wx0 + mx:wx0 + mx + crop.shape[1]] = crop
    print(f"copied box {box} score={score:.3f}")

def erase(dst, box):
    """Inpaint away the light glyphs inside box (e.g. stray quotation marks)."""
    d = sc(dst); x0, y0, x1, y1 = [int(v * d) for v in box]
    roi = dst[y0:y1, x0:x1]
    mask = (cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY) > 90).astype(np.uint8) * 255
    dst[y0:y1, x0:x1] = cv2.inpaint(roi, cv2.dilate(mask, np.ones((5, 5), np.uint8)), 5, cv2.INPAINT_TELEA)
    print(f"erased {int(mask.sum() / 255)} px in {box}")

def redraw_seats(dst, box, rows, n_left, left_is="blue"):
    """Erase every blue/grey seat dot inside box and draw sum(rows) dots, first n_left (by angle, from the left) blue."""
    d = sc(dst); x0, y0, x1, y1 = [int(v * d) for v in box]
    roi = dst[y0:y1, x0:x1].astype(int); b, g, r = roi[..., 0], roi[..., 1], roi[..., 2]
    blue = (b > 140) & (b - r > 60)
    grey = (roi.min(axis=2) > 120) & (roi.max(axis=2) - roi.min(axis=2) < 25)
    n, lab, stats, cents = cv2.connectedComponentsWithStats((blue | grey).astype(np.uint8))
    keep = [i for i in range(1, n) if stats[i][4] > 200]  # seat-sized blobs only; letters are < 150 px
    dots = [(cents[i], stats[i]) for i in keep]
    seat = np.isin(lab, keep); blue &= seat; grey &= seat
    mask = (seat * 255).astype(np.uint8)
    rad = np.median([max(s[2], s[3]) for _, s in dots]) / 2
    cxs = [c[0] for c, _ in dots]; cys = [c[1] for c, _ in dots]
    cx = (min(cxs) + max(cxs)) / 2; cy = max(cys)
    radii = sorted(math.hypot(c[0] - cx, c[1] - cy) for c, _ in dots)
    r_in, r_out = radii[0], radii[-1]
    bcol = tuple(int(v) for v in roi[blue].mean(axis=0)[::-1]); gcol = tuple(int(v) for v in roi[grey].mean(axis=0)[::-1])
    print(f"found {len(dots)} dots, r={rad:.1f}, arc r {r_in:.0f}-{r_out:.0f}")
    clean = cv2.inpaint(dst[y0:y1, x0:x1], cv2.dilate(mask, np.ones((7, 7), np.uint8)), 7, cv2.INPAINT_TELEA)
    dst[y0:y1, x0:x1] = clean
    seats = []
    for i, k in enumerate(rows):
        R = r_in + (r_out - r_in) * i / (len(rows) - 1)
        for j in range(k):
            a = math.pi - math.pi * j / (k - 1)
            seats.append((a, x0 + cx + R * math.cos(a), y0 + cy - R * math.sin(a)))
    seats.sort(key=lambda s: -s[0])
    S = 4; H, W = dst.shape[:2]
    over = Image.new("RGBA", (W * S, H * S)); dr = ImageDraw.Draw(over)
    for i, (_, x, y) in enumerate(seats):
        col = bcol if i < n_left else gcol
        dr.ellipse([(x - rad) * S, (y - rad) * S, (x + rad) * S, (y + rad) * S], fill=col + (255,))
    over = over.resize((W, H), Image.LANCZOS)
    base = Image.fromarray(cv2.cvtColor(dst, cv2.COLOR_BGR2RGB)).convert("RGBA")
    dst[:] = cv2.cvtColor(np.array(Image.alpha_composite(base, over).convert("RGB")), cv2.COLOR_RGB2BGR)
    print(f"drew {len(seats)} seats: {n_left} {left_is}, {len(seats) - n_left} other")

if __name__ == "__main__":
    cab, cab0 = load("out/cabinet_1948_fix-gemini.png"), load("out/cabinet_1948-gpt.png")
    copy_word(cab0, cab, (683, 656, 818, 676))           # "Ychuda" -> "Yehuda"
    erase(cab, (97, 497, 105, 509)); erase(cab, (202, 497, 212, 509))  # quote marks around Party: Mapai
    redraw_seats(cab, (548, 118, 815, 252), rows=[10, 12, 15], n_left=30)  # 30 coalition + 7 opposition
    cv2.imwrite("final/04-cabinet-1948.png", cab)
    dfn, dfn0 = load("out/defense_1955_fix-gemini.png"), load("out/defense_1955-gpt.png")
    copy_word(dfn0, dfn, (1022, 244, 1340, 263))         # "Sovict" -> "Soviet"
    copy_word(dfn0, dfn, (695, 659, 800, 679))           # "Pewer" -> "Fewer"
    cv2.imwrite("final/08-defense-1955.png", dfn)
