from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
def panel(path,size):
 im=Image.open(path).convert('RGBA');im.thumbnail(size,Image.Resampling.LANCZOS)
 return im
out=Image.new('RGB',(1080,650),(30,34,38));draw=ImageDraw.Draw(out)
for i,(view,label) in enumerate([('front','정면'),('back','후면'),('side','측면')]):
 im=panel(root/f'anatomy_final_textured_{view}.png',(350,590));out.paste(im,(i*360+(360-im.width)//2,50),im)
 draw.text((i*360+155,15),label,font=font,fill=(231,226,216))
out.save(root/'anatomy_final_three_views.png')
out=Image.new('RGB',(1080,1140),(30,34,38));draw=ImageDraw.Draw(out)
for row,(tag,label) in enumerate([('before_clay','수정 전'),('after_clay','수정 후')]):
 for col,(view,direction) in enumerate([('front','정면'),('back','후면'),('side','측면')]):
  im=panel(root/f'anatomy_{tag}_{view}.png',(340,500));x=col*360+(360-im.width)//2;y=row*570+55
  out.paste(im,(x,y),im);draw.text((col*360+100,row*570+18),label+' · '+direction,font=font,fill=(231,226,216))
out.save(root/'anatomy_before_after.png')
