import bpy,math,os,json
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets/Environment/IronPerimeter')
os.makedirs(OUT,exist_ok=True)
class Batch:
 def __init__(self):self.v=[];self.f=[]
 def ring(self,pts,axis,rad,sides=8):
  d=Vector(axis).normalized();u=d.cross(Vector((0,0,1)))
  if u.length<.01:u=d.cross(Vector((0,1,0)))
  u.normalize();w=d.cross(u).normalized();start=len(self.v)
  for p in pts:
   p=Vector(p)
   for k in range(sides):self.v.append(tuple(p+rad*(u*math.cos(math.tau*k/sides)+w*math.sin(math.tau*k/sides))))
  for i in range(len(pts)-1):
   for k in range(sides):self.f.append((start+i*sides+k,start+i*sides+(k+1)%sides,start+(i+1)*sides+(k+1)%sides,start+(i+1)*sides+k))
 def rod(self,a,b,r,sides=8):
  a,b=Vector(a),Vector(b)
  if (b-a).length>1e-5:self.ring([a,b],b-a,r,sides)
 def box(self,loc,dim):
  x,y,z=[d/2 for d in dim];cx,cy,cz=loc;off=len(self.v)
  self.v.extend([(cx+dx,cy+dy,cz+dz) for dx,dy,dz in [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]])
  self.f.extend([tuple(off+i for i in face) for face in [(3,2,1,0),(4,5,6,7),(0,1,5,4),(2,3,7,6),(3,0,4,7),(1,2,6,5)]])
 def done(self,name,mat,parent=None):
  me=bpy.data.meshes.new(name);me.from_pydata(self.v,[],self.f);me.update();ob=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(ob);ob.data.materials.append(mat)
  if parent:ob.parent=parent
  return ob
def material(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
def root(name):return bpy.data.objects.new(name,None)
def export(path):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
def fence():
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 steel=material('IP_GalvanizedSteel',(.38,.43,.45),.78,.43);dark=material('IP_WornIron',(.12,.15,.17),.72,.58);cement=material('IP_Concrete',(.34,.36,.35),.02,.89)
 r=root('FencePanel_3m');bpy.context.collection.objects.link(r)
 frame=Batch();wire=Batch();base=Batch();barb=Batch()
 for x in [-1.5,1.5]:
  frame.rod((x,0,.15),(x,0,2.02),.048,12);base.box((x,0,.07),(.27,.28,.14));base.box((x,0,.16),(.16,.17,.025))
 frame.rod((-1.5,0,.25),(1.5,0,.25),.032);frame.rod((-1.5,0,1.96),(1.5,0,1.96),.035)
 for x in [-1.5,1.5]:
  for s in [-1,1]:frame.rod((x,0,1.92),(x+s*.26,0,2.23),.023)
 # Diagonal diamond mesh, physical tubes in 3D rather than a texture plane.
 for offset in [i*.19 for i in range(-20,21)]:
  for slope in [-1,1]:
   pts=[]
   for z in [.25+j*(1.68/18) for j in range(19)]:
    x=offset+slope*(z-.25)
    if -1.49<=x<=1.49:pts.append((x,0,z))
   for a,b in zip(pts,pts[1:]):wire.rod(a,b,.006,5)
 # Double helical concertina wire about 0.5m tall. Tiny barbs are actual geometry.
 turns=5;steps=150
 for phase in [0,math.pi]:
  pts=[(-1.5+3*i/steps,.27*math.cos(2*math.pi*turns*i/steps+phase),2.23+.23*math.sin(2*math.pi*turns*i/steps+phase)) for i in range(steps+1)]
  for a,b in zip(pts,pts[1:]):barb.rod(a,b,.009,6)
  for i in range(8,steps,14):
   p=Vector(pts[i]);barb.rod(p,p+Vector((.055,.035,.055)),.006,5)
 for z in [2.1,2.24]:barb.rod((-1.5,0,z),(1.5,0,z),.008,6)
 for batch,name,mat in [(frame,'Fence_Frame',steel),(wire,'Fence_ChainLink',dark),(barb,'Fence_Concertina',steel),(base,'Fence_Footings',cement)]:batch.done(name,mat,r)
 bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'FencePanel.blend'));export(os.path.join(OUT,'FencePanel.fbx'))
def gate():
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 iron=material('IP_GateIron',(.17,.20,.21),.8,.49);trim=material('IP_GateTrim',(.36,.37,.34),.7,.47);cement=material('IP_Concrete',(.34,.36,.35),.02,.89)
 r=root('GateDouble_5m');bpy.context.collection.objects.link(r)
 posts=Batch();stone=Batch()
 for x in [-2.55,2.55]:
  posts.box((x,0,1.26),(.14,.19,2.52));posts.box((x,0,2.51),(.27,.25,.08));stone.box((x,0,.08),(.38,.40,.16));posts.rod((x,0,2.55),(x,0,2.68),.055,8)
 posts.done('Gate_FixedPosts',iron,r);stone.done('Gate_PostFoundations',cement,r)
 for side,label in [(1,'Left'),(-1,'Right')]:
  hinge=-2.5 if side==1 else 2.5
  pivot=root('Gate_'+label+'Leaf_Pivot');pivot.location=(hinge,0,0);bpy.context.collection.objects.link(pivot);pivot.parent=r
  shape=Batch();ornament=Batch();edge=2.47
  for z in [.16,2.14]:shape.rod((0,0,z),(side*edge,0,z),.039,10)
  for x in [0,side*edge]:shape.rod((x,0,.14),(x,0,2.17),.042,10)
  for i in range(17):
   x=side*(.10+i*.14)
   if abs(x)>edge-.02:continue
   shape.rod((x,0,.18),(x,0,2.30),.016,8)
   ornament.rod((x,0,2.30),(x,0,2.36),.025,8)
  for z in [.43,.54,1.35]:shape.rod((0,0,z),(side*edge,0,z),.015,8)
  # Forged scrolls and central crest: kept below fence height.
  for center in [.72,1.65]:
   pts=[(side*(center+.30*math.cos(t)),.027,.94+.29*math.sin(t)) for t in [j*math.pi/22 for j in range(32)]]
   for a,b in zip(pts,pts[1:]):ornament.rod(a,b,.012,7)
  for center in [.70,1.80]:
   pts=[(side*(center+.25*math.cos(t)),.027,1.69+.24*math.sin(t)) for t in [j*math.pi/22 for j in range(36)]]
   for a,b in zip(pts,pts[1:]):ornament.rod(a,b,.012,7)
  ornament.rod((side*1.24,.03,1.18),(side*1.24,.03,2.11),.018,8)
  for z in [.42,1.34]:ornament.rod((side*.02,.02,z),(side*edge,.02,z),.013,8)
  shape.done('Gate_'+label+'Leaf_Frame',iron,pivot);ornament.done('Gate_'+label+'Leaf_Ornaments',trim,pivot)
  for z in [.4,1.9]:
   h=Batch();h.box((0,0,z),(.13,.23,.16));h.done('Gate_'+label+'Leaf_Hinge',trim,pivot)
 bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'DoubleGate.blend'));export(os.path.join(OUT,'DoubleGate.fbx'))
fence();gate();print('BUILT',OUT)
