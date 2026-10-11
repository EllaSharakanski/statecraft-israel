# Spike tool (branch promo-ai3d). Run from the working dir that holds crops/ (see tools/ai3d/run_trellis.py); writes crops/in_<name>.png.
# Cut subjects out of promo 03 with hand-drawn polygons (sub-crop pixel coords), upscale, write RGBA inputs.
import json, os
from PIL import Image, ImageDraw, ImageFilter
SRC=os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'promo', 'final', '03-gameplay-1936.png')
im=Image.open(SRC).convert('RGB')
S={
 'bauhaus':([1100,511,1235,656],[(33,17),(90,17),(90,32),(120,42),(120,60),(116,136),(15,136),(12,45),(33,40)]),
 'moshava':([1189,1070,1349,1215],[(60,10),(76,10),(78,35),(80,55),(122,42),(150,68),(150,110),(125,120),(85,130),(40,125),(15,120),(12,72),(50,55),(57,55),(57,35)]),
 'jaffa':([302,674,715,1018],[(15,190),(60,160),(85,130),(90,40),(100,5),(115,40),(122,130),(175,128),(260,128),(345,140),(350,175),(345,300),(300,330),(200,340),(60,320),(15,290)]),
 'olive':([1998,582,2073,657],[(12,40),(18,22),(30,13),(45,15),(58,25),(62,40),(58,55),(45,60),(32,62),(30,66),(26,66),(25,60),(14,55)]),
 'citrus':([1048,844,1133,904],[(47,32),(52,25),(62,23),(72,28),(78,36),(74,45),(64,47),(63,55),(60,55),(59,47),(50,45)]),
 'palm':([720,497,805,617],[(12,78),(25,70),(42,72),(50,82),(46,95),(33,98),(32,120),(28,120),(27,98),(12,95)]),
 'figure':([2042,750,2102,855],[(19,4),(24,4),(26,12),(36,12),(38,24),(42,30),(42,52),(38,58),(36,95),(30,96),(29,70),(25,70),(22,88),(12,88),(18,60),(13,50),(14,30),(20,24),(20,12)]),
}
meta={}
for k,(box,poly) in S.items():
    cr=im.crop(box); w,h=cr.size
    up=max(1,round(1024/max(w,h)))
    m=Image.new('L',(w*up,h*up),0); ImageDraw.Draw(m).polygon([(x*up,y*up) for x,y in poly],fill=255)
    m=m.filter(ImageFilter.GaussianBlur(up*0.6))
    big=cr.resize((w*up,h*up),Image.LANCZOS).filter(ImageFilter.UnsharpMask(radius=2,percent=60,threshold=2))
    rgba=big.copy(); rgba.putalpha(m)
    bb=m.getbbox(); rgba=rgba.crop(bb)
    rgba.save(f'crops/in_{k}.png')
    chk=Image.new('RGB',rgba.size,(200,200,200)); d=ImageDraw.Draw(chk)
    for y in range(0,rgba.size[1],32):
        for x in range(0,rgba.size[0],32):
            if (x//32+y//32)%2: d.rectangle([x,y,x+31,y+31],fill=(150,150,150))
    chk.paste(rgba,(0,0),rgba); chk.thumbnail((400,400)); chk.save(f'crops/prev_{k}.png')
    meta[k]={'source':'promo/final/03-gameplay-1936.png','box_xyxy':box,'polygon_subcrop':poly,'upscale':f'{up}x Lanczos + UnsharpMask(r2,60%)','size':rgba.size}
    print(k,box,up,rgba.size)
json.dump(meta,open('crops/inputs.json','w'),indent=1)
