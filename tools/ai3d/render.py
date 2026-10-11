# Tiny numpy rasteriser: textured, lambert + ambient, orthographic-ish perspective turntable. Usage: render.py <obj-dir> <name> <out.png> [raw.glb]
import sys, math, numpy as np, trimesh
from PIL import Image, ImageDraw
def load(d, name):
    V=[];T=[];F=[]
    for line in open(f'{d}/{name}.obj'):
        p=line.split()
        if not p: continue
        if p[0]=='v': V.append([float(x) for x in p[1:4]])
        elif p[0]=='vt': T.append([float(x) for x in p[1:3]])
        elif p[0]=='f': F.append([int(x.split('/')[0])-1 for x in p[1:4]])
    return np.array(V),np.array(T),np.array(F),np.asarray(Image.open(f'{d}/{name}.png').convert('RGB'),dtype=np.float32)/255
def render(V,T,F,tex,yaw,pitch=25,S=320,flat=False):
    c=V.mean(0)*[1,0,1]+[0,0.5,0]; R=np.linalg.norm(V-c,axis=1).max()
    a=math.radians(yaw); b=math.radians(pitch)
    Ry=np.array([[math.cos(a),0,math.sin(a)],[0,1,0],[-math.sin(a),0,math.cos(a)]]); Rx=np.array([[1,0,0],[0,math.cos(b),-math.sin(b)],[0,math.sin(b),math.cos(b)]])
    P=(V-c)@Ry.T@Rx.T; xy=P[:,:2]/R*S*0.45+S/2; xy[:,1]=S-xy[:,1]; z=P[:,2]
    img=np.ones((S,S,3),np.float32)*np.array([0.82,0.80,0.76]); zb=np.full((S,S),-1e9)
    L=np.array([-0.4,0.8,0.45]); L/=np.linalg.norm(L)
    n=np.cross(P[F[:,1]]-P[F[:,0]],P[F[:,2]]-P[F[:,0]]); n/=np.linalg.norm(n,axis=1,keepdims=True)+1e-12
    Lv=Rx@Ry@L; sh=0.45+0.6*np.abs(n@Lv) if not flat else np.ones(len(F))
    th,tw=tex.shape[:2]
    for i,(i0,i1,i2) in enumerate(F):
        p0,p1,p2=xy[i0],xy[i1],xy[i2]
        x0=int(max(0,math.floor(min(p0[0],p1[0],p2[0])))); x1=int(min(S-1,math.ceil(max(p0[0],p1[0],p2[0]))))
        y0=int(max(0,math.floor(min(p0[1],p1[1],p2[1])))); y1=int(min(S-1,math.ceil(max(p0[1],p1[1],p2[1]))))
        if x1<x0 or y1<y0: continue
        den=(p1[1]-p2[1])*(p0[0]-p2[0])+(p2[0]-p1[0])*(p0[1]-p2[1])
        if abs(den)<1e-9: continue
        gx,gy=np.meshgrid(np.arange(x0,x1+1)+0.5,np.arange(y0,y1+1)+0.5)
        w0=((p1[1]-p2[1])*(gx-p2[0])+(p2[0]-p1[0])*(gy-p2[1]))/den; w1=((p2[1]-p0[1])*(gx-p2[0])+(p0[0]-p2[0])*(gy-p2[1]))/den; w2=1-w0-w1
        m=(w0>=0)&(w1>=0)&(w2>=0)
        if not m.any(): continue
        zz=w0*z[i0]+w1*z[i1]+w2*z[i2]
        ys,xs=np.nonzero(m); ys2=ys+y0; xs2=xs+x0; zv=zz[ys,xs]
        ok=zv>zb[ys2,xs2]
        if not ok.any(): continue
        ys2,xs2,zv=ys2[ok],xs2[ok],zv[ok]; a0,a1,a2=w0[ys,xs][ok],w1[ys,xs][ok],w2[ys,xs][ok]
        uv=a0[:,None]*T[i0]+a1[:,None]*T[i1]+a2[:,None]*T[i2]
        tx=np.clip((uv[:,0]*tw).astype(int),0,tw-1); ty=np.clip(((1-uv[:,1])*th).astype(int),0,th-1)
        img[ys2,xs2]=tex[ty,tx]*sh[i]; zb[ys2,xs2]=zv
    return Image.fromarray((np.clip(img,0,1)*255).astype(np.uint8))
if __name__=='__main__':
    d,name,out=sys.argv[1:4]
    V,T,F,tex=load(d,name)
    views=[render(V,T,F,tex,y) for y in (0,90,180,270)]+[render(V,T,F,tex,45,pitch=55)]
    S=views[0].width; sheet=Image.new('RGB',(S*len(views),S+22),'white')
    for i,v in enumerate(views): sheet.paste(v,(i*S,22))
    ImageDraw.Draw(sheet).text((4,4),f'{name}: {len(F)} tris, tex {tex.shape[0]}px  (yaw 0/90/180/270, top-ish 45)',fill=(0,0,0))
    sheet.save(out); print(out)
