import numpy as np
from PIL import Image

import art_compare as ac


def solid(rgb, size=(64, 36)):
    return Image.new("RGB", size, rgb)


def noisy(size=(192, 108), seed=1):
    return Image.fromarray(np.random.default_rng(seed).integers(0, 255, (size[1], size[0], 3), dtype=np.uint8), "RGB")


def test_metrics_read_saturation_contrast_and_warmth():
    grey = ac.metrics(solid((128, 128, 128)))
    assert grey["saturation"] == 0 and grey["contrast"] == 0 and grey["warmth"] == 0
    red = ac.metrics(solid((255, 0, 0)))
    assert red["saturation"] > 0.99 and red["warmth"] > 0.99


def test_palette_distance_is_zero_for_the_same_image_and_one_for_disjoint_palettes():
    a = solid((200, 150, 60))
    assert ac.histogram_distance(a, a) == 0
    assert ac.histogram_distance(solid((0, 0, 0)), solid((255, 255, 255))) == 1


def test_compose_puts_both_images_at_the_same_height_under_a_caption():
    out = ac.compose(solid((10, 20, 30), (1920, 1080)), solid((200, 100, 50), (2752, 1536)), "t")
    assert out.height == ac.CAPTION_PX + ac.HEIGHT_PX
    assert out.width == round(1920 * ac.HEIGHT_PX / 1080) + ac.GAP_PX + round(2752 * ac.HEIGHT_PX / 1536)


def test_colour_blind_simulation_keeps_greys_and_merges_red_with_green():
    grey = np.asarray(ac.simulate_cvd(solid((128, 128, 128)), "deuteranopia"), dtype=int)
    assert np.abs(grey - 128).max() <= 1
    red = np.asarray(ac.simulate_cvd(solid((200, 30, 30)), "protanopia"), dtype=int)[0, 0]
    green = np.asarray(ac.simulate_cvd(solid((30, 160, 30)), "protanopia"), dtype=int)[0, 0]
    assert abs(int(red[0]) - int(green[0])) < 100      # 170 apart before simulation


def test_main_pairs_each_pose_with_its_promo_logs_a_row_and_writes_cvd_copies(tmp_path):
    promo = tmp_path / "promo.png"; noisy((275, 153), 2).save(promo)
    shots = tmp_path / "shots"; (shots / "editor").mkdir(parents=True)
    noisy().save(shots / "gameplay03.png")
    noisy(seed=3).save(shots / "editor" / "PC-diorama02.png")
    noisy(seed=4).save(shots / "unrelated.png")
    log = tmp_path / "elsewhere" / "art-log.tsv"
    code = ac.main(["art_compare.py", str(shots), "task-x", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo})
    assert code == 0
    for stem in ("gameplay03", "editor/PC-diorama02"):
        for suffix in ("-compare.png", "-compare-deuteranopia.png", "-compare-protanopia.png"):
            assert (shots / (stem + suffix)).exists(), stem + suffix
    rows = log.read_text().splitlines()
    assert len(rows) == 2 and all(r.startswith("task-x\t") for r in rows)
    assert ac.main(["art_compare.py", str(shots), "again", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    assert not (shots / "gameplay03-compare-compare.png").exists()


def test_a_uniform_screenshot_fails(tmp_path):
    promo = tmp_path / "promo.png"; noisy().save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    solid((0, 0, 0), (192, 108)).save(shots / "gameplay03.png")       # a black capture (the WebGL readback failed)
    assert ac.main(["art_compare.py", str(shots), "t"], root=tmp_path, promos={"gameplay03": promo, "diorama02": promo}) == 2
