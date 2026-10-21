from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from pypdf import PdfReader
root=Path(__file__).resolve().parent
out=Image.new('RGB',(1600,1100),'#f0f4f7')
dr=ImageDraw.Draw(out)
font=ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf',32)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',24)
ids=['J2','J3E','J3','J4']
labels=['2구 조인트','3구 (1) - 30mm 연장형','3구 (2) - 기본형','4구 조인트']
dims=['58.5 x 27 x 58.5 mm','88.5 x 58.5 x 58.5 mm','58.5 x 58.5 x 58.5 mm','90 x 58.5 x 58.5 mm']
for i,key in enumerate(ids):
    x=i%2*800;y=i//2*550
    dr.rounded_rectangle((x+18,y+18,x+782,y+532),radius=18,fill='white')
    dr.text((x+42,y+35),labels[i],font=font,fill='#12344c')
    dr.text((x+42,y+485),dims[i]+' / 외곽 치수 가정값',font=small,fill='#46617a')
    im=Image.open(root/(key+'_iso.png')).convert('RGBA');im.thumbnail((700,390))
    out.paste(im,(x+(800-im.width)//2,y+90),im)
out.save(root/'joints_preview.png')
pdf=PdfReader(root.parents[1]/'output/pdf/pipe_joint_drawings_review.pdf')
assert len(pdf.pages)==5
contents='\n'.join(p.extract_text() for p in pdf.pages)
assert '300mm' not in contents and '360mm' not in contents and '345' not in contents and '315' not in contents
assert '30mm 연장형' in contents and '전체 길이는 88.5mm' in contents
assert all('REV C' in p.extract_text() and '25.5' in p.extract_text() for p in pdf.pages)
print('Verified 5-page REV C PDF: 30mm extension, 88.5mm total, no stale 300mm dimensions.')

assert all('27' in p.extract_text() for p in pdf.pages)
assert 'Ø30' not in contents and '외경30' not in contents
