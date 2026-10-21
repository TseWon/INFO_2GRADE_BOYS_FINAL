from pathlib import Path
import json,math
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.units import mm
from reportlab.lib.colors import HexColor
ROOT=Path(__file__).resolve().parents[2]
SRC=Path(__file__).resolve().parent
OUT=ROOT/'output'/'pdf';OUT.mkdir(parents=True,exist_ok=True)
pdfmetrics.registerFont(TTFont('Malgun','C:/Windows/Fonts/malgun.ttf'))
pdfmetrics.registerFont(TTFont('MalgunBold','C:/Windows/Fonts/malgunbd.ttf'))
c=canvas.Canvas(str(OUT/'pipe_joint_drawings_review.pdf'),pagesize=(420*mm,297*mm))
c.setTitle('스틸 파이프 조인트 4종 - 정투상 및 300mm 보정 상세')
c.setAuthor('Booth fabrication review')
INK='#182B3A';BLUE='#1264A3';GRAY='#78858E';LIGHT='#EDF3F7';RED='#B44725'
def line(x1,y1,x2,y2,color=INK,width=.25,dash=None):
    c.setStrokeColor(HexColor(color));c.setLineWidth(width);c.setDash(dash or [])
    c.line(x1,y1,x2,y2);c.setDash([])
def text(x,y,s,size=3.1,color=INK,bold=False,align='left'):
    c.setFillColor(HexColor(color));c.setFont('MalgunBold' if bold else 'Malgun',size)
    getattr(c,{'left':'drawString','center':'drawCentredString','right':'drawRightString'}[align])(x,y,s)
def rect(x,y,w,h,fill=None,stroke=INK):
    c.setFillColor(HexColor(fill or '#FFFFFF'));c.setStrokeColor(HexColor(stroke));c.setLineWidth(.25)
    c.rect(x,y,w,h,stroke=1,fill=int(fill is not None))
def arrow(x,y,dx,dy):
    # Arrow tip at x,y, pointing in dx,dy.
    p=c.beginPath();p.moveTo(x,y);p.lineTo(x-dx*2-dy*.6,y-dy*2+dx*.6);p.lineTo(x-dx*2+dy*.6,y-dy*2-dx*.6);p.close()
    c.setFillColor(HexColor(BLUE));c.drawPath(p,fill=1,stroke=0)
def dimh(x1,x2,y,yref,label):
    for x in [x1,x2]:line(x,yref,x,y+2,BLUE,.18)
    line(x1,y,x2,y,BLUE,.2);arrow(x1,y,-1,0);arrow(x2,y,1,0)
    text((x1+x2)/2,y+2,label,3.2,BLUE,align='center')
def dimv(y1,y2,x,xref,label):
    for y in [y1,y2]:line(xref,y,x-2,y,BLUE,.18)
    line(x,y1,x,y2,BLUE,.2);arrow(x,y1,0,-1);arrow(x,y2,0,1)
    c.saveState();c.translate(x-2,(y1+y2)/2);c.rotate(90);text(0,0,label,3.2,BLUE,align='center');c.restoreState()
def header(title,page,code):
    c.saveState();c.scale(mm,mm)
    rect(9,9,402,279,stroke='#7D97AA')
    rect(9,266,402,22,fill='#12344C',stroke='#12344C')
    text(17,275,title,6,'#FFFFFF',True)
    text(401,276,'제작업체 검토용 / 가정값 포함',3.6,'#FFFFFF',align='right')
    text(17,258,'확정: 스틸 · 모든 소켓 내경 Ø25.5mm · 나사 구멍 없음',3.7,BLUE,True)
    text(401,258,'단위 mm | 투상: 제3각법 | 2026-10-22',3.1,align='right')
    line(9,28,411,28,'#7D97AA')
    text(17,17,'내경 외 허용공차 미정 · 표기 축척은 A3 원본 기준 · 도면 치수 우선',2.8,GRAY)
    text(401,17,f'{code}  |  REV A  |  {page} / 5',3.1,align='right')
def footer():c.restoreState();c.showPage()

def projection(d,key,cx,cy,scale,label):
    data=d['views'][key];x0,y0,x1,y1=data['bounds'];mx=(x0+x1)/2;my=(y0+y1)/2
    tx=lambda x:cx+(x-mx)*scale
    ty=lambda y:cy+(y-my)*scale
    # All features are extracted from the same Boolean solid; hidden segments are ray-tested.
    for vis in [0,1]:
        c.setStrokeColor(HexColor(INK if vis else '#89949C'));c.setLineWidth(.28 if vis else .17)
        c.setDash([] if vis else [1,.8])
        p=c.beginPath()
        for a,b,e,f,v in data['lines']:
            if v==vis:p.moveTo(tx(a),ty(b));p.lineTo(tx(e),ty(f))
        c.drawPath(p)
    c.setDash([])
    eye={'front':(0,-1,0),'top':(0,0,1),'side':(1,0,0)}[key]
    if any(tuple(direction)==eye for direction in d['directions']):
        # Circular mouth rims face the viewer directly and cannot be hidden by the hub.
        # Reconstruct these verified model circles analytically to remove facet-ray artifacts.
        c.setStrokeColor(HexColor(INK));c.setLineWidth(.28)
        c.circle(tx(0),ty(0),d['OD_mm']*.5*scale,stroke=1,fill=0)
        c.circle(tx(0),ty(0),d['socket_ID_mm']*.5*scale,stroke=1,fill=0)
    line(tx(x0)-3,ty(0),tx(x1)+3,ty(0),'#6D94B2',.15,[3,1,.5,1])
    line(tx(0),ty(y0)-3,tx(0),ty(y1)+3,'#6D94B2',.15,[3,1,.5,1])
    dimh(tx(x0),tx(x1),ty(y1)+8,ty(y1),f'{x1-x0:g}*')
    dimv(ty(y0),ty(y1),tx(x0)-8,tx(x0),f'{y1-y0:g}*')
    text(cx,ty(y0)-11,label,3.8,bold=True,align='center')
    return tx,ty

def notes(lines):
    rect(17,35,386,35,fill=LIGHT,stroke=LIGHT)
    for i,s in enumerate(lines):text(22,61-i*7,s,3.0)

data=json.loads((SRC/'model_geometry.json').read_text(encoding='utf-8'))
for page,d in enumerate(data,1):
    ext=d['id']=='J3E'
    header(d['title'],page,d['id'])
    if ext:
        projection(d,'top',133,218,.55,'윗면도: +Z → -Z  |  가로 X / 세로 Y')
        projection(d,'front',133,125,.55,'정면도: -Y → +Y  |  가로 X / 세로 Z')
        projection(d,'side',338,125,1,'우측면도: +X → -X  |  가로 Y / 세로 Z')
        c.drawImage(str(SRC/(d['id']+'_iso.png')),278,177,120,76,preserveAspectRatio=True,anchor='c',mask='auto')
        text(338,177,'등각 참고도 / 축척 없음',3.1,GRAY,align='center')
        text(20,80,'정면·윗면 0.55:1 / 우측면 1:1  |  연장 방향 +X  |  300mm 보정 상세는 5쪽',3.2,BLUE)
        notes(['* 가정: 외경 Ø30, 기본 입구 위치 O+45, 삽입 깊이 30, 연장부 내부 Ø20.',
               '기본형 대비 +X쪽 입구 45 → 345, 멈춤면 15 → 315: 순증 300mm(30cm).',
               '전체 X 길이 360mm = 기본 60mm + 연장 300mm. 참고 이미지의 90mm/30mm 연장은 적용하지 않음.',
               '기본형과 동일한 3개 소켓. 추가 구멍·나사 없음. 멈춤면 상세와 가정값은 제작업체 확인 대상.'])
    else:
        projection(d,'top',100,213,1,'윗면도: +Z → -Z  |  가로 X / 세로 Y')
        projection(d,'front',100,127,1,'정면도: -Y → +Y  |  가로 X / 세로 Z')
        projection(d,'side',229,127,1,'우측면도: +X → -X  |  가로 Y / 세로 Z')
        c.drawImage(str(SRC/(d['id']+'_iso.png')),286,160,108,82,preserveAspectRatio=True,anchor='c',mask='auto')
        text(340,155,'등각 참고도 / 축척 없음',3.1,GRAY,align='center')
        text(340,141,'모든 입구 Ø25.5mm',4,BLUE,True,align='center')
        text(340,132,'외경 Ø30mm*',3.4,align='center')
        text(340,124,'삽입 깊이 30mm*',3.4,align='center')
        text(20,80,'정투상 1:1  |  실선: 보이는 모서리  /  파선: 가려진 모서리  /  일점쇄선: 축',3.0,BLUE)
        dirs={'J2':'+X, +Z (직각 2구)','J3':'+X, -Y, -Z (서로 직각인 입체 3구)','J4':'+X, -X, -Y, -Z (직선 2구 + 직각 2구)'}[d['id']]
        notes(['*는 가정 치수: 외경 Ø30, 소켓 삽입 깊이 30, 교차 중심 O에서 입구까지 45.',
               '소켓 방향: '+dirs+'. 모든 정투상은 동일한 블렌더 메시에서 산출.',
               '소켓 내경 Ø25.5는 확정. 내경/외경 허용공차, 스틸 세부 강종 및 표면처리는 미정.',
               '나사 구멍 없음. 바깥 접합부 곡면은 형상 가정이며 주조·가공·용접 등 제조 방식은 업체 협의.'])
    footer()

header('3구 (1) | 300mm 보정 원리와 소켓 단면',5,'J3E-DETAIL')
text(20,244,'입구뿐 아니라 파이프가 멈추는 면도 300mm 이동해야 길이가 실제로 보정됩니다.',4.4,bold=True)
text(20,234,'이 도면의 300mm는 추가 길이입니다. 늘어난 조인트의 전체 길이는 360mm입니다.',3.6,BLUE)

# Exact axial section of the +X branch, x=15..345, excluding the central hub and other branches.
s=.95;ox=42;oy=178
X=lambda x:ox+(x-15)*s
Y=lambda y:oy+y*s
for sign in [-1,1]:
    points=[(15,sign*10),(315,sign*10),(315,sign*12.75),(345,sign*12.75),(345,sign*15),(15,sign*15)]
    p=c.beginPath();p.moveTo(X(points[0][0]),Y(points[0][1]))
    for x,y in points[1:]:p.lineTo(X(x),Y(y))
    p.close();c.setFillColor(HexColor('#B9C9D5'));c.setStrokeColor(HexColor(INK));c.setLineWidth(.3);c.drawPath(p,fill=1,stroke=1)
line(X(15),Y(-15),X(15),Y(15))
line(X(12),oy,X(349),oy,BLUE,.15,[3,1,.5,1])
dimh(X(15),X(315),Y(15)+15,Y(15),'300 추가 길이')
dimh(X(315),X(345),Y(-15)-14,Y(-15),'삽입 30*')
dimv(Y(-15),Y(15),X(15)-10,X(15),'Ø30*')
text(X(140),oy+2,'내부 경량화 구멍 Ø20*',3.3,align='center')
text(X(330),oy+2,'Ø25.5',3.0,BLUE,align='center')
text(X(315),Y(-15)-26,'멈춤면 x=315*',3.2,BLUE,align='center')
text(22,132,'+X 연장축 종단면 (0.95:1): x=15~345 구간만 표시. 중앙 교차부와 다른 두 소켓은 생략.',3.1)
text(22,122,'* 연장부 내부 Ø20은 가정값. Ø25.5 소켓과의 단차를 파이프 끝의 멈춤면으로 사용합니다.',3.1)
text(22,113,'실제 파이프 외경·내경을 확인해 끼움 여유와 멈춤면 접촉 폭을 확정해야 합니다.',3.1)

rect(20,77,380,26,fill=LIGHT,stroke=LIGHT)
text(25,94,'기준 비교 (교차 중심 O=0)',3.5,bold=True)
text(25,84,'기본형: 입구 45 / 멈춤면 15     →     연장형: 입구 345 / 멈춤면 315     →     차이 +300mm',3.5,BLUE)
notes(['확정 조건: 스틸 / 모든 파이프 삽입구 내경 Ø25.5mm / 나사 구멍 없음.',
       '가정 조건: 외경 Ø30, 삽입 깊이 30, 내부 경량화 Ø20, 공차·표면처리·제조 공법 미정.',
       '단순 끼움 방식입니다. 조립 후 유지력과 설치 하중에 대한 검토는 이 도면에 포함하지 않습니다.',
       'Blender 원본: pipe_joints_review.blend  |  단위 mm (scene scale 0.001)  |  4개 독립 부품'])
footer();c.save()
print(OUT/'pipe_joint_drawings_review.pdf')
