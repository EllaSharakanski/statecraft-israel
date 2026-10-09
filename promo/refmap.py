# /// script
# requires-python = ">=3.11"
# dependencies = ["pillow"]
# ///
"""Schematic geography reference (north up) for image generation. Coordinates (lon, lat) from
standard gazetteer values; accuracy ~0.05 deg, enough to fix relative placement."""
from PIL import Image, ImageDraw, ImageFont
W, H = 1340, 2000  # 1.9 deg lon x 2.4 deg lat = 179 x 266 km at 31.8N
LON0, LON1, LAT0, LAT1 = 34.15, 36.05, 30.95, 33.35
def xy(lon, lat): return ((lon - LON0) / (LON1 - LON0) * W, (LAT1 - lat) / (LAT1 - LAT0) * H)
COAST = [(35.10,33.35),(35.10,33.09),(35.08,32.92),(35.05,32.85),(34.96,32.83),(34.95,32.72),
         (34.90,32.50),(34.84,32.30),(34.78,32.10),(34.75,32.04),(34.65,31.80),(34.55,31.67),
         (34.45,31.52),(34.27,31.32),(34.15,31.25)]
GALILEE = [(35.53,32.88),(35.60,32.89),(35.65,32.82),(35.64,32.72),(35.57,32.70),(35.52,32.78)]
DEAD = [(35.50,31.77),(35.57,31.75),(35.58,31.45),(35.55,31.20),(35.45,31.05),(35.39,31.10),(35.40,31.45),(35.45,31.70)]
JORDAN = [(35.58,32.70),(35.57,32.45),(35.55,32.20),(35.53,31.95),(35.52,31.77)]
CITIES = {"TEL AVIV (skyscrapers)":(34.78,32.08),"Jaffa":(34.75,32.03),"Haifa + Mt Carmel":(34.99,32.80),
          "Jerusalem (hills, Old City)":(35.22,31.78),"Beersheba":(34.79,31.25),"Ashdod port":(34.65,31.80),
          "Ashkelon":(34.57,31.67),"Netanya":(34.86,32.33),"Tiberias":(35.53,32.79),"Nazareth":(35.30,32.70),
          "Ben Gurion Airport":(34.88,32.01),"Dimona":(35.03,31.07)}
img = Image.new("RGB", (W, H), (222, 200, 150)); d = ImageDraw.Draw(img)
d.polygon([xy(*p) for p in COAST] + [xy(LON0, LAT0), xy(LON0, LAT1)], fill=(60, 130, 170))
for poly in (GALILEE, DEAD): d.polygon([xy(*p) for p in poly], fill=(60, 140, 180))
d.line([xy(*p) for p in JORDAN], fill=(60, 140, 180), width=8)
f = ImageFont.load_default(size=34)
d.text(xy(34.30, 32.6), "MEDITERRANEAN SEA", fill="white", font=f)
d.text(xy(35.60, 31.30), "DEAD SEA", fill=(20, 40, 60), font=f)
d.text(xy(35.45, 33.00), "SEA OF GALILEE", fill=(20, 40, 60), font=f)
d.text(xy(35.60, 32.2), "JORDAN RIVER", fill=(20, 40, 60), font=f)
d.text(xy(34.95, 31.45), "NEGEV DESERT", fill=(120, 80, 30), font=f)
d.text(xy(34.98, 31.62), "JUDEAN HILLS", fill=(120, 80, 30), font=f)
for name, p in CITIES.items():
    x, y = xy(*p); d.ellipse([x-12, y-12, x+12, y+12], fill=(170, 30, 30)); d.text((x+16, y-18), name, fill="black", font=f)
d.text((20, 20), "N ^  (north is up)", fill="black", font=f)
img.save("promo/ref/geo-reference.png"); print("ok")
