import bpy, math, os, json, random
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets/Blender/SurfaceResearchStation');ART=os.path.join(ROOT,'artifacts/surface_station')
os.makedirs(OUT,exist_ok=True);os.makedirs(ART,exist_ok=True)
random.seed(29)
MODULE='Station';COUNT=0
def enum(obj,prop,value):
 choices=[v.identifier for v in obj.bl_rna.properties[prop].enum_items]
 assert value in choices,(prop,choices);setattr(obj,prop,value)
def material(name,color,metal=0,rough=.5,maps=None,emission=0):
 m=bpy.data.materials.get('SR_'+name)
 if m:return m
 m=bpy.data.materials.new('SR_'+name);m.use_nodes=True;m.diffuse_color=(*color,1)
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
 if maps:
  for suffix,socket in [('BaseColor','Base Color'),('Roughness','Roughness'),('Normal',None)]:
   path=os.path.join(OUT,'Textures',maps+'_'+suffix+'.png');im=bpy.data.images.load(path,check_existing=True);im.pack()
   if suffix!='BaseColor':im.colorspace_settings.name='Non-Color'
   t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=im
   if socket:m.node_tree.links.new(t.outputs['Color'],p.inputs[socket])
   else:
    n=m.node_tree.nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=.28;m.node_tree.links.new(t.outputs['Color'],n.inputs['Color']);m.node_tree.links.new(n.outputs['Normal'],p.inputs['Normal'])
 return m
def setup_materials():
 global PAINT,DARK,ROOF,GLASS,GASKET,RED,STEEL,GRIME,LIGHT,AMBER,WHITE
 PAINT=material('IvoryPaint',(.70,.69,.61),.12,.6,'IvoryPaint');DARK=material('Graphite',(.075,.105,.12),.65,.48,'Graphite');ROOF=material('RoofAlloy',(.47,.54,.56),.72,.43,'RoofAlloy')
 GLASS=material('SmokedGlazing',(.035,.10,.135),.48,.2);GASKET=material('Gaskets',(.016,.024,.028),0,.85);RED=material('FadedRed',(.38,.095,.075),.15,.65);STEEL=material('Fasteners',(.22,.27,.29),.85,.38);GRIME=material('OxidizedEdges',(.19,.135,.085),.15,.88);LIGHT=material('WarmLight',(1,.66,.31),0,.35,emission=3);AMBER=material('WarningYellow',(.58,.35,.055),.1,.65);WHITE=material('Lettering',(.65,.69,.67),.1,.6)
def link(ob,mat,collection='Shell'):
 global COUNT
 COUNT+=1;ob.name=MODULE+'__'+ob.name;COLS[collection].objects.link(ob);ob.parent=ROOTOBJ;ob['module']=MODULE
 if mat and hasattr(ob.data,'materials'):ob.data.materials.append(mat)
 return ob
def mesh(name,verts,faces,mat,collection='Shell',smooth=False):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();ob=link(bpy.data.objects.new(name,me),mat,collection)
 if smooth:
  for p in me.polygons:p.use_smooth=True
 return ob
def bevel(o,width=.025):
 if width:
  b=o.modifiers.new('Rounded manufactured edges','BEVEL');b.width=width;b.segments=2
 return o
def box(name,loc,dims,mat,bev=0,collection='Shell'):
 x,y,z=[d*.5 for d in dims];v=[(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
 f=[(2,6,4,0),(5,7,3,1),(4,5,1,0),(3,7,6,2),(1,3,2,0),(6,7,5,4)]
 o=mesh(name,v,f,mat,collection);o.location=loc;return bevel(o,bev)
def cyl(name,loc,r,depth,mat,segments=24,axis=(0,0,1),collection='Details'):
 v=[(r*math.cos(i*math.tau/segments),r*math.sin(i*math.tau/segments),z) for z in [-depth*.5,depth*.5] for i in range(segments)]
 f=[tuple(reversed(range(segments))),tuple(range(segments,2*segments))]+[(i,(i+1)%segments,(i+1)%segments+segments,i+segments) for i in range(segments)]
 o=mesh(name,v,f,mat,collection,True);o.location=loc;o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector(axis).to_track_quat('Z','Y');return o
def rod(name,a,b,r,mat,collection='Details',segments=10):
 a=Vector(a);b=Vector(b);return cyl(name,(a+b)*.5,r,(b-a).length,mat,segments,b-a,collection)
def pipe(name,points,r,mat,collection='Details'):
 # Rounded elbows, sampled directly into a polygonal sweep.
 points=[Vector(p) for p in points];path=[points[0]]
 for i in range(1,len(points)-1):
  prev,p,nxt=points[i-1:i+2];d=min(.30,(p-prev).length*.22,(nxt-p).length*.22)
  a=p+(prev-p).normalized()*d;b=p+(nxt-p).normalized()*d
  path.append(a)
  for j in range(1,7):
   t=j/6;path.append((1-t)**2*a+2*(1-t)*t*p+t*t*b)
 path.append(points[-1]);v=[];N=10
 for i,p in enumerate(path):
  tangent=(path[min(i+1,len(path)-1)]-path[max(0,i-1)]).normalized();u=tangent.cross(Vector((0,0,1)))
  if u.length<.01:u=tangent.cross(Vector((0,1,0)))
  u.normalize();w=tangent.cross(u).normalized()
  for j in range(N):v.append(p+r*(u*math.cos(j*math.tau/N)+w*math.sin(j*math.tau/N)))
 f=[tuple(reversed(range(N))),tuple((len(path)-1)*N+j for j in range(N))]
 for i in range(len(path)-1):
  for j in range(N):f.append((i*N+j,i*N+(j+1)%N,(i+1)*N+(j+1)%N,(i+1)*N+j))
 return mesh(name,v,f,mat,collection,True)
def orient(o,n):
 n=Vector(n);t=Vector((-n.y,n.x,0));o.rotation_euler[2]=math.atan2(t.y,t.x);return o
def wall_box(name,p,n,w,h,d,mat,bev=0,collection='Shell'):
 return orient(box(name,p,(w,d,h),mat,bev,collection),n)
def rim(name,p,n,w,h,border,depth,mat,collection='Details'):
 n=Vector(n);t=Vector((-n.y,n.x,0));up=Vector((0,0,1));p=Vector(p);v=[]
 for dep in [-depth*.5,depth*.5]:
  for W,H in [(w,h),(w-2*border,h-2*border)]:
   rr=min(.10,W*.15,H*.15)
   for cx,cz,start in [(W/2-rr,H/2-rr,0),(-W/2+rr,H/2-rr,90),(-W/2+rr,-H/2+rr,180),(W/2-rr,-H/2+rr,270)]:
    for j in range(5):
     a=math.radians(start+j*90/4);v.append(p+t*(cx+rr*math.cos(a))+up*(cz+rr*math.sin(a))+n*dep)
 N=20;f=[]
 for i in range(N):
  k=(i+1)%N
  f.extend([(i,k,N+k,N+i),(2*N+i,3*N+i,3*N+k,2*N+k),(i,2*N+i,2*N+k,k),(N+i,N+k,3*N+k,3*N+i)])
 return mesh(name,v,f,mat,collection,True)
def window(p,n,w=1.02,h=1.12,detail=True):
 p=Vector(p);n=Vector(n);t=Vector((-n.y,n.x,0))
 wall_box('Inset glass',p-n*.055,n,w,h,.028,GLASS,.025)
 rim('Port window frame',p+n*.025,n,w+.24,h+.24,.13,.12,PAINT)
 rim('Glazing gasket',p+n*.034,n,w+.015,h+.015,.027,.025,GASKET)
 if detail:
  for a in [-1,1]:
   for b in [-1,1]:cyl('Window fastener',p+n*.094+t*(a*(w/2+.055))+Vector((0,0,b*(h/2+.055))),.025,.02,STEEL,8,n)
def wall(name,p,n,width,height,holes=None):
 holes=holes or [];n=Vector(n);t=Vector((-n.y,n.x,0));p=Vector(p);xs=sorted(set([-width/2,width/2]+[q for x,z,w,h in holes for q in [x-w/2,x+w/2]]));zs=sorted(set([0,height]+[q for x,z,w,h in holes for q in [z-h/2,z+h/2]]))
 for a,b in zip(xs,xs[1:]):
  for c,d in zip(zs,zs[1:]):
   x=(a+b)/2;z=(c+d)/2
   if not (-width/2<=x<=width/2 and 0<=z<=height):continue
   if any(abs(x-X)<W/2-.0001 and abs(z-Z)<H/2-.0001 for X,Z,W,H in holes):continue
   wall_box(name,p+t*x+Vector((0,0,z))-n*.12,n,b-a,d-c,.24,PAINT,.012)
 wall_box('Lower wall flashing',p+Vector((0,0,.13))+n*.025,n,width,.16,.06,DARK,.012)
 wall_box('Eave fascia',p+Vector((0,0,height-.08))+n*.03,n,width,.18,.08,ROOF,.015)
def roof_gable(cx,cy,w,l,z,rise,mat= None):
 mat=mat or PAINT
 for side in [-1,1]:
  v=[(cx,cy-l/2,z+rise),(cx+side*w/2,cy-l/2,z),(cx+side*w/2,cy+l/2,z),(cx,cy+l/2,z+rise)]
  v+= [(x,y,Z-.16) for x,y,Z in v];f=[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
  bevel(mesh('Standing seam pitched roof',v,f,mat,'Roofs'),.025)
 rod('Roof ridge cap',(cx,cy-l/2-.04,z+rise+.05),(cx,cy+l/2+.04,z+rise+.05),.10,ROOF,'Roofs',16)
 for yy in [cy-l/2,cy+l/2]:
  pipe('Gable edge flashing',[(cx-w/2,yy,z),(cx,yy,z+rise),(cx+w/2,yy,z)],.085,ROOF,'Roofs')
 for side in [-1,1]:
  for yy in [cy-l/2+i*.85 for i in range(1,int(l/.85))]:rod('Roof seam',(cx+side*.09,yy,z+rise+.028),(cx+side*(w/2-.08),yy,z+.07),.018,ROOF,'Roofs',6)
def gable_infill(cx,yy,w,z,rise,n,glazed=False):
 v=[(cx-w/2,yy,z),(cx+w/2,yy,z),(cx,yy,z+rise)];mesh('Gable infill',v,[(0,1,2)],PAINT)
 if glazed:
  # Raised trapezoidal observation window, with roof outline remaining visible.
  sign=n[1];verts=[(cx-w*.35,yy+sign*.035,z+.15),(cx+w*.35,yy+sign*.035,z+.15),(cx+w*.12,yy+sign*.035,z+rise*.72),(cx-w*.12,yy+sign*.035,z+rise*.72)]
  mesh('Observation glazing',verts,[(0,1,2,3)],GLASS,'Details')
  pipe('Observation window surround',verts+[verts[0]],.065,PAINT)
  for x in [-w*.23,0,w*.23]:
   top=z+rise*.72 if abs(x)<w*.12 else z+rise*.72-(abs(x)-w*.12)*rise*.57/(w*.23)
   rod('Observation mullion',(cx+x,yy+sign*.075,z+.15),(cx+x,yy+sign*.075,top),.026,ROOF)
def door(p,n,w=1.5,h=2.35,service=False):
 p=Vector(p);n=Vector(n);t=Vector((-n.y,n.x,0))
 wall_box('Recessed airlock door',p-n*.08,n,w-.06,h-.03,.14,DARK,.06)
 rim('Airlock surround',p+n*.06,n,w+.25,h+.20,.12,.15,ROOF)
 if service:
  for s in [-1,1]:
   wall_box('Pressure door leaf',p+t*(s*w*.235),n,w*.44,h*.91,.045,PAINT,.035)
   a=p+t*(s*w*.43)+Vector((0,0,-h*.36));b=p+t*(s*.09)+Vector((0,0,h*.31));rod('Door diagonal reinforcement',a+n*.075,b+n*.075,.035,ROOF)
 else:
  window(p+Vector((0,0,h*.20))+n*.025,n,w*.49,.43,False)
  rod('Pull handle',p+t*(w*.31)+n*.13+Vector((0,0,-.25)),p+t*(w*.31)+n*.13+Vector((0,0,.1)),.025,STEEL)
def foundation(cx,cy,w,l):
 box('Raised foundation',(cx,cy,.39),(w,l,.78),DARK,.12,'Decks');box('Deck rim',(cx,cy,.795),(w+.08,l+.08,.13),ROOF,.035,'Decks')
 for side in [-1,1]:
  for i in range(max(1,int(w/2.5))):
   xx=cx-w/2+(i+.5)*w/max(1,int(w/2.5));wall_box('Foundation service panel',(xx,cy+side*(l/2+.015),.4),(0,side,0),1.65,.42,.045,STEEL,.035,'Decks')
def rectangular_module(name,cx,cy,w,l,eave=4.5,rise=1.65,front_entry=True,back_entry=False,side_passages=False):
 global MODULE
 MODULE=name;foundation(cx,cy,w+1.4,l+1.5);box('Module floor',(cx,cy,.86),(w-.12,l-.12,.12),DARK,0)
 front=[(0,1.2,1.55,2.4)] if front_entry else []
 for x in [-w*.32,w*.32]:front.append((x,2.03,.90,1.12))
 wall('Front insulated wall',(cx,cy-l/2,.85),(0,-1,0),w,eave-.85,front)
 for x,z,W,H in front:
  if H>2:door((cx-x,cy-l/2,.85+z),(0,-1,0),W,H)
  else:window((cx-x,cy-l/2,.85+z),(0,-1,0),W,H)
 back=[(0,1.35,2.35,2.7)] if back_entry else [(-w*.28,2.1,.9,1.0),(w*.28,2.1,.9,1.0)]
 wall('Back insulated wall',(cx,cy+l/2,.85),(0,1,0),w,eave-.85,back)
 for x,z,W,H in back:
  if H>2:door((cx-x,cy+l/2,.85+z),(0,1,0),W,H,True)
  else:window((cx-x,cy+l/2,.85+z),(0,1,0),W,H)
 for side in [-1,1]:
  holes=[(-l*.31,2.05,.95,1.15),(l*.31,2.05,.95,1.15)]
  if side_passages:holes.append((0,1.35,2.3,2.7))
  wall('Side insulated wall',(cx+side*w/2,cy,.85),(side,0,0),l,eave-.85,holes)
  for x,z,W,H in holes:
   if H<2:window((cx+side*w/2,cy+side*x,.85+z),(side,0,0),W,H)
 for x in [-1,1]:
  for y in [-1,1]:box('Corner structural post',(cx+x*(w/2-.06),cy+y*(l/2-.06),(.85+eave)/2),(.16,.16,eave-.85),ROOF,.018)
 roof_gable(cx,cy,w+.44,l+.42,eave,rise)
 gable_infill(cx,cy-l/2,w,eave,rise,(0,-1,0),True);gable_infill(cx,cy+l/2,w,eave,rise,(0,1,0),not back_entry)
def dome(name,cx,cy):
 global MODULE
 MODULE=name;R=3.8;N=12;apothem=R*math.cos(math.pi/N);width=2*R*math.sin(math.pi/N)
 cyl('Dodecagonal foundation',(cx,cy,.39),4.55,.78,DARK,12,collection='Decks');cyl('Deck edge ring',(cx,cy,.81),4.58,.13,ROOF,12,collection='Decks');cyl('Interior floor',(cx,cy,.86),3.74,.12,DARK,48,collection='Shell')
 for j in range(N):
  a=j*math.tau/N;n=Vector((math.cos(a),math.sin(a),0));p=Vector((cx,cy,.85))+n*apothem
  connection=(j==3 or (cx<0 and j==0) or (cx>0 and j==6));entry=j==9
  holes=[(0,1.32,1.75,2.64)] if connection else ([(0,1.18,1.24,2.36)] if entry else [(0,1.80,1.02,1.16)])
  wall('Faceted habitat wall',p,n,width,3.36,holes)
  if entry:door(p+Vector((0,0,1.18)),n,1.24,2.36)
  elif not connection:window(p+Vector((0,0,1.80)),n)
  post=Vector((cx,cy,2.53))+Vector((math.cos(a+math.pi/N),math.sin(a+math.pi/N),0))*R
  box('Habitat vertical stiffener',post,(.13,.13,3.36),ROOF,.018)
 for z,r,h in [(4.20,3.93,.20),(4.36,3.86,.12)]:cyl('Dome cornice',(cx,cy,z),r,h,PAINT,96,collection='Roofs')
 v=[];rings=14;segs=96
 for i in range(rings+1):
  t=(i/rings)*math.pi/2;rr=3.81*math.cos(t);z=4.42+1.46*math.sin(t)
  for j in range(segs):a=j*math.tau/segs;v.append((cx+rr*math.cos(a),cy+rr*math.sin(a),z))
 faces=[(i*segs+j,i*segs+(j+1)%segs,(i+1)*segs+(j+1)%segs,(i+1)*segs+j) for i in range(rings) for j in range(segs)]
 mesh('Oblate pressure dome',v,faces,PAINT,'Roofs',True)
 for j in range(12):
  a=j*math.tau/12;pts=[(cx+3.82*math.cos(t)*math.cos(a),cy+3.82*math.cos(t)*math.sin(a),4.43+1.47*math.sin(t)) for t in [i*math.pi/2/18 for i in range(19)]];pipe('Dome radial rib',pts,.036,ROOF,'Roofs')
 for z,r in [(5.03,3.46),(5.53,2.52)]:pipe('Dome circumferential seam',[(cx+r*math.cos(i*math.tau/96),cy+r*math.sin(i*math.tau/96),z) for i in range(97)],.022,ROOF,'Roofs')
 cyl('Dome service collar',(cx,cy,5.92),.72,.15,DARK,48,collection='Roofs');cyl('Dome top vent',(cx,cy,6.06),.62,.18,ROOF,48,collection='Roofs')
def corridor(name,path,width=2.3):
 global MODULE
 MODULE=name
 for a,b in zip(path,path[1:]):
  a=Vector(a);b=Vector(b);d=b-a;L=d.length;theta=math.atan2(d.y,d.x);center=(a+b)/2;n=Vector((-d.y,d.x,0)).normalized()
  for label,z,h,mat in [('Walkway base',.40,.8,DARK),('Walkway floor',.88,.12,ROOF),('Corridor roof',3.63,.18,PAINT)]:
   o=box(label,(center.x,center.y,z),(L+.15,width+.12,h),mat,.04,'Shell');o.rotation_euler[2]=theta
  for side in [-1,1]:
   o=box('Connector wall',(center.x+n.x*side*(width/2-.10),center.y+n.y*side*(width/2-.10),2.22),(L+.12,.20,2.6),PAINT,.025);o.rotation_euler[2]=theta
  count=max(2,int(L/.8))
  for i in range(count+1):
   p=a+d*(i/count);pts=[p-n*(width/2+.035)+Vector((0,0,.98)),p-n*(width/2+.035)+Vector((0,0,3.72)),p+n*(width/2+.035)+Vector((0,0,3.72)),p+n*(width/2+.035)+Vector((0,0,.98))];pipe('Connector structural hoop',pts,.045,ROOF)
def railing(path):
 for a,b in zip(path,path[1:]):
  a=Vector(a);b=Vector(b);N=max(1,math.ceil((b-a).length/1.65))
  for k in range(N+1):
   p=a.lerp(b,k/N);box('Rail base shoe',p+Vector((0,0,.035)),(.15,.15,.07),DARK,.012,'Details');rod('Guardrail upright',p,p+Vector((0,0,1.02)),.032,DARK)
  for z in [.51,1.04]:rod('Continuous guardrail',a+Vector((0,0,z)),b+Vector((0,0,z)),.025 if z<1 else .035,DARK)
def stairs(name,cx,y_top,width=1.9):
 global MODULE
 MODULE=name
 for i in range(5):
  h=(5-i)*.17;box('Grated access step',(cx,y_top-.15-i*.30,h/2),(width,.32,h),DARK,.025,'Decks');box('Step nosing',(cx,y_top-.29-i*.30,h+.008),(width,.035,.028),ROOF,.006,'Decks')
 for side in [-1,1]:railing([(cx+side*(width/2+.08),y_top,.85),(cx+side*(width/2+.08),y_top-1.5,.0)])
def preview_setup():
 global MODULE
 MODULE='Preview';ground=box('Presentation ground',(0,0,-.18),(200,200,.18),material('PreviewGround',(.21,.235,.25),0,.93),0,'Preview');ground.parent=None
 scene.world=bpy.data.worlds.new('Station studio world');scene.world.use_nodes=True
 bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.33,.40,.49,1);bg.inputs['Strength'].default_value=.45
 for name,loc,power,size in [('Large key',(8,-15,26),7000,16),('Sky fill',(-20,-4,17),4500,14),('Rear rim',(12,20,22),6500,12)]:
  data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);COLS['Preview'].objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,2))-o.location).to_track_quat('-Z','Y').to_euler()
 data=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',data);COLS['Preview'].objects.link(cam);cam.location=(35,-45,29);cam.rotation_euler=(Vector((0,0,2))-cam.location).to_track_quat('-Z','Y').to_euler();enum(data,'type','ORTHO');data.ortho_scale=44;scene.camera=cam
 scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
 try:scene.render.engine='BLENDER_WORKBENCH'
 except TypeError as e:print(e)
 enum(scene.display.shading,'color_type','MATERIAL');enum(scene.display.shading,'light','STUDIO');scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
def render_views(prefix,quality=False):
 cam=scene.camera;views=[('overview',(35,-45,30),(0,0,2),44),('plan',(0,0,60),(0,0,0),36),('front',(0,-60,4),(0,0,3.5),35)]
 if quality:views=[('overview',(37,-48,29),(0,-.2,2.1),43),('front',(0,-60,4),(0,0,3.5),35),('rear',(-34,40,24),(0,1,2),43),('plan',(0,0,60),(0,0,0),36)]
 for label,loc,target,scale in views:
  cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
  scene.render.resolution_x=1300 if label!='plan' else 1100;scene.render.resolution_y=950 if label!='plan' else 1100;scene.render.filepath=os.path.join(ART,prefix+'_'+label+'.png');bpy.ops.render.render(write_still=True)
def frame():
 global scene,COLS,ROOTOBJ,MODULE
 assert bpy.data.scenes.get('Surface Research Station') is None,'Scene already exists; inspect before rebuilding'
 scene=bpy.data.scenes.new('Surface Research Station');bpy.context.window.scene=scene
 enum(scene.unit_settings,'system','METRIC');scene.unit_settings.scale_length=1
 COLS={}
 for k in ['Shell','Roofs','Decks','Details','Preview','References']:
  c=bpy.data.collections.new('SR_'+k);scene.collection.children.link(c);COLS[k]=c
 ROOTOBJ=bpy.data.objects.new('SurfaceResearchStation',None);scene.collection.objects.link(ROOTOBJ);ROOTOBJ['scale_note']='Meters. Compact surface facility, exterior-focused. Dimensions inferred from 2.2m doors.'
 setup_materials()
 rectangular_module('Command_Access',0,-8.2,8.4,8.2,4.45,1.70,True,True,True)
 dome('West_Habitat',-10.8,-7.4);dome('East_Habitat',10.8,-7.4)
 rectangular_module('Research_Spine',0,7.1,7.2,13.8,4.65,1.75,True,True,True)
 rectangular_module('West_Laboratory',-7.7,6.4,8.2,7.3,4.35,1.10,True,False,False)
 rectangular_module('East_Laboratory',7.7,6.4,8.2,7.3,4.35,1.10,True,False,False)
 for side in [-1,1]:
  corridor(('West' if side<0 else 'East')+'_Front_Link',[(side*4.15,-7.4,0),(side*7.25,-7.4,0)])
  corridor(('West' if side<0 else 'East')+'_Rear_Link',[(side*10.8,-3.65,0),(side*10.8,-.15,0),(side*8.5,1.9,0),(side*8.5,2.85,0)])
 stairs('Command_stairs',0,-13.0,2.0)
 for side in [-1,1]:stairs(('West' if side<0 else 'East')+'_hab_stairs',side*10.8,-11.99,1.7)
 preview_setup();bpy.context.view_layer.update()
 # Store references as packed hidden image empties, outside export collections.
 MODULE='References'
 for i,file in enumerate(['01_exterior.png','02_framework.png','03_modules.png']):
  im=bpy.data.images.load(os.path.join(ART,'references',file),check_existing=True);im.pack();o=bpy.data.objects.new(file,None);o.empty_display_type='IMAGE';o.data=im;COLS['References'].objects.link(o);o.hide_render=True
 COLS['References'].hide_render=True;scene.view_layers[0].layer_collection.children[COLS['References'].name].exclude=True
 bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ART,'SurfaceResearch_frame.blend'))
 render_views('frame')
 print('FRAME_READY',len(scene.objects),'objects',os.path.join(ART,'SurfaceResearch_frame.blend'))
if globals().get('BUILD_FRAME',False):frame()
