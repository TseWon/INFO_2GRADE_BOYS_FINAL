from pathlib import Path
import numpy as np
from PIL import Image,ImageFilter
import shutil
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Blender/SurfaceResearchStation/Textures';OUT.mkdir(parents=True,exist_ok=True)
refs=ROOT/'artifacts/surface_station/references';refs.mkdir(parents=True,exist_ok=True)
for label,filename in [('01_exterior','codex-clipboard-09869f42-8718-46a6-ac9a-60b9e98cdba3.png'),('02_framework','codex-clipboard-dd28ddbf-af65-4c31-b925-5c4b416ee783.png'),('03_modules','codex-clipboard-56595410-8ed7-4261-9fac-97f49ddd016d.png')]:
 shutil.copy2(Path('C:/Users/user/AppData/Local/Temp')/filename,refs/(label+'.png'))
rng=np.random.default_rng(10429);N=1024
def noise(k):
 a=rng.integers(0,256,(k,k),dtype=np.uint8)
 return np.asarray(Image.fromarray(a).resize((N,N),Image.Resampling.BICUBIC),dtype=np.float32)/255
for name,color,metal,rough in [('IvoryPaint',(194,192,175),.12,.60),('Graphite',(53,65,70),.65,.48),('RoofAlloy',(152,164,166),.72,.43)]:
 cloud=noise(32);fine=noise(512);broad=noise(10)
 dirt=np.clip((.43-broad)*.32,0,.08);shade=(.94+.08*cloud+.025*fine-dirt)
 rgb=np.clip(np.array(color)[None,None,:]*shade[:,:,None],0,255).astype(np.uint8)
 Image.fromarray(rgb).save(OUT/f'{name}_BaseColor.png')
 r=np.clip(rough+.13*(cloud-.5)+dirt,0,1)
 Image.fromarray((r*255).astype(np.uint8)).save(OUT/f'{name}_Roughness.png')
 h=(fine-.5)*.009+(cloud-.5)*.012
 dy,dx=np.gradient(h);norm=np.stack((-dx*5,-dy*5,np.ones_like(h)),axis=2);norm/=np.linalg.norm(norm,axis=2,keepdims=True)
 Image.fromarray(np.uint8(np.clip((norm*.5+.5)*255,0,255))).save(OUT/f'{name}_Normal.png')
 ms=np.zeros((N,N,4),dtype=np.uint8);ms[:,:,:3]=int(metal*255);ms[:,:,3]=np.uint8((1-r)*255)
 Image.fromarray(ms).save(OUT/f'{name}_MetallicSmoothness.png')
print('Created 12 PBR maps and copied 3 references:',OUT)
