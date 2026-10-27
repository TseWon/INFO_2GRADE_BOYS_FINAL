from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
root=Path(__file__).parent
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
out=Image.new('RGB',(1380,930),(30,34,38));draw=ImageDraw.Draw(out)
for row,(tag,label) in enumerate([('backthorax_before','수정 전'),('backthorax_final_body','수정 후')]):
 for col,(view,text) in enumerate([('front','정면'),('back','후면'),('side','측면')]):
  im=Image.open(root/f'anatomy_{tag}_{view}.png').convert('RGBA').crop((50,30,510,430))
  out.paste(im,(460*col,55+450*row),im);draw.text((460*col+140,15+450*row),label+' · '+text,font=font,fill=(232,226,216))
draw.text((350,900),'군장만 숨긴 상체 비교 · 동일 축척의 직교 투영',font=font,fill=(204,209,214))
out.save(root/'back_thorax_before_after.png')
out=Image.new('RGB',(1080,650),(30,34,38));draw=ImageDraw.Draw(out)
for col,(view,label) in enumerate([('front','정면'),('back','후면'),('side','측면')]):
 im=Image.open(root/f'anatomy_backthorax_release_{view}.png').convert('RGBA');im.thumbnail((350,590),Image.Resampling.LANCZOS)
 out.paste(im,(col*360+(360-im.width)//2,50),im);draw.text((col*360+155,15),label,font=font,fill=(232,226,216))
out.save(root/'back_thorax_final_three_views.png')
