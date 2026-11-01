import bpy,os,math,random
from mathutils import Vector,Matrix
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/build_surface_station.py'),encoding='utf-8').read(),'station_helpers','exec'))
scene=bpy.data.scenes['Surface Research Station'];bpy.context.window.scene=scene;ROOTOBJ=bpy.data.objects['SurfaceResearchStation'];COLS={k:bpy.data.collections['SR_'+k] for k in ['Shell','Roofs','Decks','Details','Preview','References']};setup_materials()
assert not ROOTOBJ.get('detailed'),'Details already applied'
def prism(name,outline,bottom,top,mat,collection='Shell'):
 v=[(p[0],p[1],z) for z in [bottom,top] for p in outline];N=len(outline);f=[tuple(reversed(range(N))),tuple(range(N,2*N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)];return mesh(name,v,f,mat,collection)
def offset_path(path,d):
 path=[Vector(p) for p in path];out=[]
 for i,p in enumerate(path):
  a=(path[i]-path[i-1]).normalized() if i else (path[1]-path[0]).normalized();b=(path[i+1]-path[i]).normalized() if i<len(path)-1 else a
  n1=Vector((-a.y,a.x,0));n2=Vector((-b.y,b.x,0));n=(n1+n2).normalized();out.append(p+n*d/max(.3,n.dot(n1)))
 return out
def clean_link(name,path):
 global MODULE
 MODULE=name
 # Replace only this turn's generated corridor objects with continuous mitered surfaces.
 for ob in list(scene.objects):
  if ob.get('module')==name:bpy.data.objects.remove(ob,do_unlink=True)
 w=2.3;left=offset_path(path,w/2);right=offset_path(path,-w/2);outline=left+list(reversed(right))
 prism('Continuous corridor plinth',outline,0,.85,DARK,'Decks');prism('Continuous corridor floor',outline,.85,.93,ROOF);prism('Continuous corridor roof',outline,3.55,3.73,PAINT)
 for side in [-1,1]:
  outer=offset_path(path,side*w/2);inner=offset_path(path,side*(w/2-.18));prism('Mitered connector side wall',outer+list(reversed(inner)),.93,3.55,PAINT)
 for a,b in zip(path,path[1:]):
  a=Vector(a);b=Vector(b);d=b-a;n=Vector((-d.y,d.x,0)).normalized();N=max(2,math.ceil(d.length/.9))
  for i in range(N):
   p=a+d*(i/N);pipe('Connector structural rib',[p-n*1.185+Vector((0,0,1)),p-n*1.185+Vector((0,0,3.78)),p+n*1.185+Vector((0,0,3.78)),p+n*1.185+Vector((0,0,1))],.045,ROOF)
 for dy in [-.48,0,.48]:
  pts=offset_path(path,dy);pipe('Roof utility conduit',[p+Vector((0,0,3.87)) for p in pts],.06,ROOF)
def text_label(name,words,p,n,size=.16,mat=None):
 cu=bpy.data.curves.new(name,'FONT');cu.body=words;cu.size=size;cu.align_x='CENTER';cu.align_y='CENTER';cu.extrude=.001;cu.resolution_u=3
 o=link(bpy.data.objects.new(name,cu),mat or DARK,'Details');o.location=p;n=Vector(n);t=Vector((-n.y,n.x,0));up=Vector((0,0,1));o.rotation_mode='QUATERNION';o.rotation_quaternion=Matrix((t,up,n)).transposed().to_quaternion();return o
def grille(p,n,w=.8,h=.38):
 p=Vector(p);n=Vector(n);t=Vector((-n.y,n.x,0));wall_box('Vent recessed field',p,n,w,h,.08,GASKET,.025,'Details');rim('Vent mounting rim',p+n*.055,n,w+.12,h+.10,.045,.075,ROOF)
 for j in range(6):wall_box('Vent louver',p+n*.065+Vector((0,0,(j-2.5)*h/6)),n,w-.06,.024,.045,STEEL,.004,'Details')
def panel(p,n,w=.7,h=.55,label='SERVICE'):
 p=Vector(p);n=Vector(n);t=Vector((-n.y,n.x,0));wall_box('Maintenance access hatch',p,n,w,h,.065,PAINT,.04,'Details');rim('Panel perimeter gasket',p-n*.012,n,w+.035,h+.035,.022,.018,GASKET)
 for a in [-1,1]:
  for b in [-1,1]:cyl('Captive panel screw',p+n*.042+t*(a*(w/2-.07))+Vector((0,0,b*(h/2-.07))),.019,.018,STEEL,8,n)
 if label:text_label('Engraved service label',label,p+n*.045+Vector((0,0,.1)),n,.075)
def lamp(p,n):
 p=Vector(p);n=Vector(n);wall_box('Bulkhead light housing',p,n,.52,.18,.18,DARK,.035,'Details');wall_box('Sealed lamp diffuser',p+n*.103,n,.40,.10,.045,LIGHT,.02,'Details')
def circular_port(p,n,r=.16):
 p=Vector(p);n=Vector(n);cyl('Recessed pressure port',p,r,.055,DARK,32,n);cyl('Pressure port inner',p+n*.034,r*.70,.03,ROOF,32,n);cyl('Pressure port cap',p+n*.055,r*.40,.018,STEEL,20,n)
def mast(x,y,z,height=1.8):
 cyl('Mast vibration base',(x,y,z+.10),.16,.2,DARK,20)
 for k,(rr,hh) in enumerate([(.065,height*.5),(.038,height*.32),(.018,height*.18)]):
  zz=z+.2+sum([height*.5,height*.32][:k]);cyl('Whip antenna segment',(x,y,zz+hh/2),rr,hh,STEEL,12)
 for dz in [.27,.42,.57]:cyl('Mast insulator',(x,y,z+dz),.085,.055,PAINT,16)
def radome(x,y,z):
 cyl('Radome riser',(x,y,z+.28),.24,.56,PAINT,32,collection='Roofs');cyl('Radome collar',(x,y,z+.53),.38,.10,DARK,40,collection='Roofs')
 v=[];N=48;R=8
 for i in range(R+1):
  t=i/R*math.pi/2
  for j in range(N):a=j*math.tau/N;v.append((x+.60*math.cos(t)*math.cos(a),y+.60*math.cos(t)*math.sin(a),z+.60+.34*math.sin(t)))
 mesh('Faded red radar dome',v,[(i*N+j,i*N+(j+1)%N,(i+1)*N+(j+1)%N,(i+1)*N+j) for i in range(R) for j in range(N)],RED,'Roofs',True)
 rod('Radome lightning rod',(x,y,z+.93),(x,y,z+1.45),.018,STEEL,'Roofs')
def roof_vent(x,y,z):
 cyl('Vent curb',(x,y,z+.10),.42,.20,DARK,32,collection='Roofs');cyl('Vent neck',(x,y,z+.27),.30,.28,ROOF,32,collection='Roofs');cyl('Mushroom vent cap',(x,y,z+.45),.48,.15,PAINT,40,collection='Roofs')
 for a in range(8):
  th=a*math.tau/8;rod('Vent spacer',(x+.3*math.cos(th),y+.3*math.sin(th),z+.2),(x+.3*math.cos(th),y+.3*math.sin(th),z+.41),.018,DARK,'Roofs')
def hvac(x,y,z):
 box('Rooftop HVAC curb',(x,y,z+.10),(1.85,1.28,.20),DARK,.05,'Roofs');box('Rooftop packaged HVAC',(x,y,z+.53),(1.65,1.1,.7),PAINT,.07,'Roofs')
 grille((x,y-.57,z+.55),(0,-1,0),1.35,.43)
 for xx in [x-.46,x+.46]:
  cyl('Fan well',(xx,y,z+.90),.37,.09,GASKET,32,collection='Roofs');cyl('Fan axle',(xx,y,z+.96),.075,.10,STEEL,16,collection='Roofs')
  for j in range(5):
   a=j*math.tau/5;rod('Fan blade',(xx+.08*math.cos(a),y+.08*math.sin(a),z+.95),(xx+.31*math.cos(a+.3),y+.31*math.sin(a+.3),z+.95),.035,ROOF,'Roofs',6)
  for k in [-2,-1,0,1,2]:rod('Fan protective grille',(xx-.30,y+k*.10,z+1.0),(xx+.30,y+k*.10,z+1.0),.014,STEEL,'Roofs',6)
def valve(p,side):
 p=Vector(p);center=p+Vector((side*.23,0,0));cyl('Valve body',p,.13,.34,DARK,20,(0,1,0));rod('Valve stem',p,center,.04,STEEL)
 pipe('Red isolation valve handwheel',[center+Vector((0,.20*math.cos(i*math.tau/32),.20*math.sin(i*math.tau/32))) for i in range(33)],.022,RED)
 for j in range(3):a=j*math.tau/3;rod('Valve spoke',center,center+Vector((0,.19*math.cos(a),.19*math.sin(a))),.018,RED)
def dish(x,y,z):
 box('Radar pedestal',(x,y,z+.14),(1.1,1.1,.28),DARK,.05,'Roofs');cyl('Radar azimuth bearing',(x,y,z+.38),.42,.28,ROOF,32,collection='Roofs')
 for sx in [-.35,.35]:rod('Radar fork',(x+sx,y,z+.5),(x+sx,y,z+1.3),.09,PAINT,'Roofs',12)
 center=Vector((x,y,z+1.4));n=Vector((-.35,-.70,.62)).normalized();q=n.to_track_quat('Z','Y');v=[];N=64;R=12;radius=1.12
 for i in range(R+1):
  rr=radius*i/R
  for j in range(N):a=j*math.tau/N;v.append(center+q@Vector((rr*math.cos(a),rr*math.sin(a),rr*rr*.21)))
 mesh('Parabolic communication reflector',v,[(i*N+j,i*N+(j+1)%N,(i+1)*N+(j+1)%N,(i+1)*N+j) for i in range(R) for j in range(N)],PAINT,'Roofs',True)
 rimpts=[center+q@Vector((radius*math.cos(i*math.tau/N),radius*math.sin(i*math.tau/N),radius*radius*.21)) for i in range(N+1)];pipe('Dish rolled rim',rimpts,.035,ROOF,'Roofs')
 focus=center+n*.75
 for j in range(3):a=j*math.tau/3;rod('Dish feed strut',center+q@Vector((radius*.85*math.cos(a),radius*.85*math.sin(a),.22)),focus,.022,DARK,'Roofs')
 cyl('Receiver feedhorn',focus,.10,.19,DARK,20,n,'Roofs')
def safe_railing(path):
 # One upright per shared node avoids coincident duplicate meshes.
 keys=set()
 for a,b in zip(path,path[1:]):
  a=Vector(a);b=Vector(b);N=max(1,math.ceil((b-a).length/1.65))
  for k in range(N+1):
   p=a.lerp(b,k/N);key=tuple(round(c,4) for c in p)
   if key in keys:continue
   keys.add(key);box('Guardrail shoe',p+Vector((0,0,.035)),(.14,.14,.07),DARK,.01,'Details');rod('Guardrail post',p,p+Vector((0,0,1.02)),.032,DARK)
  for zz in [.52,1.04]:rod('Guardrail tube',a+Vector((0,0,zz)),b+Vector((0,0,zz)),.025 if zz<1 else .034,DARK)

for side in [-1,1]:
 clean_link(('West' if side<0 else 'East')+'_Front_Link',[(side*4.15,-7.4,0),(side*7.25,-7.4,0)])
 clean_link(('West' if side<0 else 'East')+'_Rear_Link',[(side*10.8,-3.65,0),(side*10.8,-.15,0),(side*7.7,1.9,0),(side*7.7,2.85,0)])
# Actual open junctions in the command building side walls are aligned with the links.
MODULE='Command_Access'
for ob in list(scene.objects):
 if ob.get('module')==MODULE and ('Side insulated wall' in ob.name or ('Lower wall flashing' in ob.name or 'Eave fascia' in ob.name) and abs(ob.location.x)>4):bpy.data.objects.remove(ob,do_unlink=True)
for side in [-1,1]:wall('Side insulated wall aligned',(side*4.2,-8.2,.85),(side,0,0),8.2,3.6,[(-8.2*.31,2.05,.95,1.15),(8.2*.31,2.05,.95,1.15),(.8*side,1.35,2.3,2.7)])
for side in [-1,1]:
 MODULE=('West' if side<0 else 'East')+'_Habitat';cx=side*10.8;cy=-7.4
 # Keep openings for the front stair, inward link and rear link.
 openangles=[-math.pi/2,math.pi/2,0 if side<0 else math.pi];arc=[]
 for i in range(49):
  a=i*math.tau/48
  if all(abs(math.atan2(math.sin(a-b),math.cos(a-b)))>.27 for b in openangles):arc.append((cx+4.40*math.cos(a),cy+4.40*math.sin(a),.89))
  elif arc:
   if len(arc)>1:safe_railing(arc)
   arc=[]
 if len(arc)>1:safe_railing(arc)
 for j in range(24):
  a=j*math.tau/24;n=Vector((math.cos(a),math.sin(a),0));cyl('Cornice locking bolt',Vector((cx,cy,4.25))+n*3.94,.032,.035,STEEL,8,n)
 for j in range(12):
  a=j*math.tau/12;n=Vector((math.cos(a),math.sin(a),0));p=Vector((cx,cy,.41))+n*4.43
  if j not in [3,9]:circular_port(p,n,.14)
  if j not in ([0,3,9] if side<0 else [3,6,9]):
   panel(Vector((cx,cy,3.62))+n*3.70,n,.43,.25,'');grille(Vector((cx,cy,1.27))+n*3.70,n,.62,.22)
 lamp((cx,cy-3.70,3.45),(0,-1,0));text_label('Habitat ID','HAB - 01' if side<0 else 'HAB - 02',(cx,cy-3.70,3.85),(0,-1,0),.19)
 MODULE=('West' if side<0 else 'East')+'_Laboratory'
 # External power/cooling conduit array, with rounded elbows and flanges.
 for k,z in enumerate([1.45,2.05,2.65]):
  xx=side*(11.95+.18*k);pipe('External coolant pipe',[(side*11.82,3.8,z),(xx,3.8,z),(xx,8.7-k*.33,z),(xx,8.7-k*.33,3.65),(side*11.82,8.7-k*.33,3.65)],.085,ROOF)
  for yy in [4.2,6.2,8.1]:
   cyl('Pipe coupling flange',(xx,yy,z),.14,.10,DARK,20,(0,1,0));cyl('Pipe flange collar',(xx,yy+.06,z),.112,.035,STEEL,20,(0,1,0))
  valve((xx,5.0+k*.65,z),side)
 for yy in [4.15,6.5,8.5]:box('Pipe mounting bracket',(side*11.95,yy,2.20),(.40,.11,1.7),DARK,.015,'Details')
 cyl('Expansion vessel',(side*12.3,8.8,1.85),.34,1.55,PAINT,32);cyl('Vessel top flange',(side*12.3,8.8,2.64),.39,.12,DARK,32)
 for zz in [1.35,2.25]:cyl('Vessel restraint band',(side*12.3,8.8,zz),.36,.075,STEEL,32)
 text_label('Wing designation','LABORATORY W' if side<0 else 'LABORATORY E',(side*7.7,2.72,3.82),(0,-1,0),.17)
 for xoff in [-2.7,2.7]:panel((side*7.7+xoff,2.72,1.35),(0,-1,0),.68,.40,'ELEC')
 hvac(side*7.7,7.8,5.52);roof_vent(side*9.1,4.5,5.12);mast(side*11.0,9.3,4.77,1.65)
 MODULE=('West' if side<0 else 'East')+'_Foundation_Walkway'
 safe_railing([(side*12.3,2.15,.89),(side*12.3,10.55,.89),(side*4.35,10.55,.89)])

MODULE='Command_Access'
for path in [[(-4.85,-13.0,.89),(-1.15,-13.0,.89)],[(1.15,-13.0,.89),(4.85,-13.0,.89)],[(-4.85,-13.0,.89),(-4.85,-8.95,.89)],[(4.85,-13.0,.89),(4.85,-8.95,.89)],[(-4.85,-5.95,.89),(-4.85,-3.45,.89),(4.85,-3.45,.89),(4.85,-5.95,.89)]]:safe_railing(path)
lamp((0,-12.38,3.55),(0,-1,0));text_label('Facility name','SURFACE ACCESS  /  07',(0,-12.39,3.95),(0,-1,0),.19)
panel((2.3,-12.37,1.65),(0,-1,0),.85,.92,'CONTROL');wall_box('Access panel display',(2.3,-12.43,1.82),(0,-1,0),.42,.20,.045,GLASS,.015,'Details')
for x in [2.17,2.30,2.43]:cyl('Panel push button',(x,-12.48,1.46),.035,.035,RED if x<2.2 else STEEL,12,(0,-1,0))
wall_box('Warning placard',(-1.25,-12.39,1.73),(0,-1,0),.58,.40,.025,DARK,.012,'Details');text_label('Caution text','RESTRICTED',(-1.25,-12.42,1.79),(0,-1,0),.074,WHITE);text_label('Caution subtext','SUBLEVEL ACCESS',(-1.25,-12.42,1.65),(0,-1,0),.049,WHITE)
for x in [-3.5,3.5]:grille((x,-12.35,1.45),(0,-1,0),.70,.35)
radome(1.45,-7.0,5.90);roof_vent(-1.8,-6.7,5.52);roof_vent(-1.8,-10.6,5.52)
MODULE='Research_Spine';radome(.8,10.4,6.10);roof_vent(-1.7,4.1,5.64);roof_vent(1.7,7.4,5.64)
text_label('Rear service title','LOGISTICS  /  B-02',(0,14.03,3.96),(0,1,0),.19);lamp((0,14.05,3.70),(0,1,0))
for sx in [-.35,.35]:rod('Roof ladder rail',(-2.4+sx,14.20,.85),(-2.4+sx,14.20,5.45),.036,DARK)
for i in range(15):rod('Roof ladder rung',(-2.75,14.20,1.05+i*.29),(-2.05,14.20,1.05+i*.29),.025,STEEL)
MODULE='West_Laboratory';dish(-7.7,4.7,5.55)
# Repeated service details reinforce a consistent construction kit.
for name,cx,cy,w,l,z in [('Command_Access',0,-8.2,8.4,8.2,4.45),('Research_Spine',0,7.1,7.2,13.8,4.65),('West_Laboratory',-7.7,6.4,8.2,7.3,4.35),('East_Laboratory',7.7,6.4,8.2,7.3,4.35)]:
 MODULE=name
 for side in [-1,1]:
  for k in range(1,5):
   xx=cx-w/2+k*w/5;p=(xx,cy+side*(l/2+.025),z-.35);panel(p,(0,side,0),.45,.23,'')
  for xx in [cx-w*.37,cx+w*.37]:circular_port((xx,cy+side*(l/2+.76),.41),(0,side,0),.13)
 for xx in [cx-w/2+.16,cx+w/2-.16]:
  for yy in [cy-l/2-.03,cy+l/2+.03]:
   side=-1 if yy<cy else 1
   for zz in [1.08,3.8]:cyl('Structural panel locking fastener',(xx,yy,zz),.025,.022,STEEL,8,(0,side,0))
# Sunlight is part of the preview setup, not exported building geometry.
data=bpy.data.lights.new('Cool morning sun','SUN');data.energy=2.2;data.angle=.15;o=bpy.data.objects.new('Cool morning sun',data);COLS['Preview'].objects.link(o);o.rotation_euler=(math.radians(28),math.radians(-24),math.radians(-25))
ROOTOBJ['detailed']=True
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ART,'SurfaceResearch_detailed.blend'))
scene.render.resolution_x=1300;scene.render.resolution_y=950
render_views('detailed_clay')
print('DETAILS_READY',len(scene.objects),'objects')
