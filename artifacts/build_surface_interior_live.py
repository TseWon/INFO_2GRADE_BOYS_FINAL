"""Run through Blender MCP: roofless, walkable surface interior study."""
import bpy, math, json
from mathutils import Vector, Matrix
from pathlib import Path
ROOT=Path(r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL')
ref=bpy.data.scenes['Surface Research Station']
scene=bpy.data.scenes.new('Surface Interior | Roofless Review')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
cols={}
for name in ['01_Floors','02_Walls','03_Glazing','04_Frames','05_Central_Cage','06_Elevator','07_Wayfinding','90_Preview']:
 c=bpy.data.collections.new('SI_'+name);scene.collection.children.link(c);cols[name]=c
def mat(name,color,metal=0,rough=.6):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 return m
paint=bpy.data.materials['SR_IvoryPaint'].copy();paint.name='SI_Existing_Ivory_Wall'
metal=bpy.data.materials['SR_Graphite'].copy();metal.name='SI_Existing_Graphite_Frame'
floor=mat('SI_Concrete_Resin_Floor',(.24,.29,.30),.12,.78)
yellow=mat('SI_Safety_Ochre',(.7,.43,.09),.15,.55)
glass=mat('SI_Clear_Observation_Glass',(.75,.9,.94),0,.1)
p=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Transmission Weight'].default_value=1;p.inputs['IOR'].default_value=1.45
noise=floor.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=14;noise.inputs['Detail'].default_value=2
bump=floor.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.025
floor.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height'])
fp=next(n for n in floor.node_tree.nodes if n.type=='BSDF_PRINCIPLED');floor.node_tree.links.new(bump.outputs['Normal'],fp.inputs['Normal'])
def mesh(name,verts,faces,material,col):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 o=bpy.data.objects.new(name,me);cols[col].objects.link(o);o.data.materials.append(material)
 # Consistent metre-based UVs for the original exterior texture maps.
 uv=me.uv_layers.new(name='SurfaceMeters')
 for f in me.polygons:
  ax=max(range(3),key=lambda k:abs(f.normal[k]));axes=[k for k in range(3) if k!=ax]
  for li in f.loop_indices:
   v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]]/2,v[axes[1]]/2)
 return o
def box(name,loc,size,material,col='02_Walls',angle=0):
 x,y,z=[q/2 for q in size]
 o=mesh(name,[(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)],[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],material,col)
 o.location=loc;o.rotation_euler.z=angle
 return o
def slab(name,points):
 n=len(points);o=mesh(name,[(x,y,z) for z in [-.28,0] for x,y in points],[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],floor,'01_Floors');return o
H=4.2;T=.26
def wall(name,a,b,opening=None):
 a,b=Vector(a),Vector(b);d=b-a;L=d.length;d.normalize();angle=math.atan2(d.y,d.x)
 def piece(label,lo,hi,z,h,material=paint,depth=T,col='02_Walls'):
  if hi-lo<.005:return
  p=a+d*((lo+hi)/2);return box(name+' | '+label,(p.x,p.y,z),(hi-lo,depth,h),material,col,angle)
 if not opening:
  piece('solid',0,L,H/2,H)
 else:
  center,width,kind=opening;lo=center-width/2;hi=center+width/2
  assert lo>=-.01 and hi<=L+.01,(name,L,opening)
  piece('pier A',0,lo,H/2,H);piece('pier B',hi,L,H/2,H)
  bottom=0 if kind=='door' else .85;top=3.25 if kind=='door' else 3.55
  if bottom:piece('sill wall',lo,hi,bottom/2,bottom)
  piece('header',lo,hi,(top+H)/2,H-top)
  piece('head frame',lo,hi,top,.10,metal,.32,'04_Frames')
  for q in [lo,hi]:piece('jamb',q-.05,q+.05,(bottom+top)/2,top-bottom,metal,.32,'04_Frames')
  if kind!='door':
   piece('sill frame',lo,hi,bottom,.1,metal,.32,'04_Frames')
   count=max(1,math.ceil(width/2.2))
   for i in range(count):
    l=lo+width*i/count;r=lo+width*(i+1)/count
    piece('clear pane %02d'%i,l+.045,r-.045,(bottom+top)/2,top-bottom-.1,glass,.035,'03_Glazing')
    if i:piece('mullion %02d'%i,l-.035,l+.035,(bottom+top)/2,top-bottom,metal,.22,'04_Frames')
 # Low metal skirting follows solid portions only.
 for lo,hi in ([(0,L)] if not opening or opening[2]!='door' else [(0,opening[0]-opening[1]/2),(opening[0]+opening[1]/2,L)]):piece('skirting',lo,hi,.09,.18,metal,.29,'04_Frames')
def rect(name,x0,x1,y0,y1,openings=None,skip=()):
 slab(name+' | floor',[(x0,y0),(x1,y0),(x1,y1),(x0,y1)])
 for side,a,b in [('S',(x0,y0),(x1,y0)),('E',(x1,y0),(x1,y1)),('N',(x0,y1),(x1,y1)),('W',(x0,y0),(x0,y1))]:
  if side not in skip:wall(name+' | '+side,a,b,(openings or {}).get(side))

rect('ENTRY / reception',-10,10,-25,-14,{'S':(10,2.8,'door'),'N':(10,15,'window'),'E':(6.5,3.6,'door'),'W':(6.5,3.6,'door')})
# Rear T-shaped research block, open connections through the shared boundaries.
rect('DATA / rear spine',-8,8,9,27,{'S':(8,12,'window'),'E':(9,3.6,'door'),'W':(9,3.6,'door')})
for sign,side in [(-1,'WEST'),(1,'EAST')]:
 x0,x1=(-21,-8) if sign<0 else (8,21)
 rect(side+' / rear laboratory',x0,x1,13,23,{'S':((10.5 if sign<0 else 2.5),3.6,'door'),'N':(6.5,9,'window'),('W' if sign<0 else 'E'):(5,6,'window')},skip=('E' if sign<0 else 'W',))
 # Front round rooms keep the two domed-module footprints, with their roofs absent.
 cx=sign*14;cy=-18.5;r=5;N=32
 pts=[(cx+r*math.cos(i*math.tau/N),cy+r*math.sin(i*math.tau/N)) for i in range(N)]
 slab(side+' / circular lab floor',pts)
 for i in range(N):
  ang=(i+.5)*math.tau/N
  # Open the inward side to reception and the north side to the long passage.
  inward=math.pi if sign>0 else 0
  delta=lambda a,b:abs((a-b+math.pi)%math.tau-math.pi)
  if delta(ang,inward)<.40 or delta(ang,math.pi/2)<.40:continue
  a=pts[i];b=pts[(i+1)%N];length=(Vector(b)-Vector(a)).length
  wall(side+' / round lab bay %02d'%i,a,b,(length/2,length-.20,'window'))
 # Fill the doorway thresholds between rectangular and curved room footprints.
 slab(side+' / front door threshold',[(sign*9,-20.3),(sign*10.5,-20.3),(sign*10.5,-16.7),(sign*9,-16.7)] if sign>0 else [(-10.5,-20.3),(-9,-20.3),(-9,-16.7),(-10.5,-16.7)])
 # A continuous straight-and-angled passage with a mitered bend.
 center=[Vector((sign*14,-14.0)),Vector((sign*14,4)),Vector((sign*10.5,13))]
 offsets=[]
 for i,pnt in enumerate(center):
  ds=[]
  if i:ds.append((center[i]-center[i-1]).normalized())
  if i<len(center)-1:ds.append((center[i+1]-center[i]).normalized())
  norms=[Vector((-d.y,d.x)) for d in ds];v=sum(norms,Vector((0,0))).normalized();offsets.append(v*1.8/v.dot(norms[0]))
 left=[p+o for p,o in zip(center,offsets)];right=[p-o for p,o in zip(center,offsets)]
 for path in (left,right):
  d=(path[2]-path[1]).normalized()
  path[2]=Vector((path[2].x+(13-path[2].y)*d.x/d.y,13))
 slab(side+' / continuous connecting passage',[tuple(v) for v in left+list(reversed(right))])
 for edge,path in [('outer' if sign<0 else 'inner',left),('inner' if sign<0 else 'outer',right)]:
  for j in range(2):
   L=(path[j+1]-path[j]).length
   wall(side+' / passage '+edge+str(j),path[j],path[j+1],(L/2,L-1.1,'window') if edge=='inner' else None)

# An enclosed central observation court has its own continuous slab.
slab('CENTRAL / observation court',[(-12.1,-14),(12.1,-14),(12.1,4),(8.6,9),(-8.6,9),(-12.1,4)])
# Reuse the actual latest six-sided birdcage, including its curved crown.
keywords=['cage footing','specimen platform','cage personnel gate','restraint ring','experiment coil','experiment lamp','suspension cables','cage crossed','cage arched']
cage_names=[]
for old in ref.objects:
 if any(k in old.name.lower() for k in keywords):
  o=old.copy();o.data=old.data.copy();o.parent=None;o.matrix_world=Matrix.Translation((0,0,-.84)) @ Matrix.Diagonal((1.3,1.3,1,1)) @ old.matrix_world
  o.name='CAGE / '+old.name.replace('UNSAVED | ','');o.hide_render=False;o.hide_viewport=False;cols['05_Central_Cage'].objects.link(o);cage_names.append(o.name)
# Elevator landing shares the rear spine floor. Seven metre clear footprint.
for name,a,b,opening in [('rear',(-3.5,26.5),(3.5,26.5),None),('left',(-3.5,19.5),(-3.5,26.5),None),('right',(3.5,19.5),(3.5,26.5),None),('portal',(-3.5,19.5),(3.5,19.5),(3.5,3.2,'door'))]:wall('LIFT / '+name,a,b,opening)
box('LIFT / cabin deck',(0,23,.012),(6.8,6.8,.07),metal,'06_Elevator')
for x in [-3.25,3.25]:box('LIFT / guide rail',(x,25.9,2),( .12,.15,4),metal,'06_Elevator')
for x in [-1.7,1.7]:box('LIFT / door recess',(x,19.52,1.55),(.12,.4,3.1),metal,'06_Elevator')
box('LIFT / caution threshold',(0,19.3,.012),(3.2,.2,.024),yellow,'06_Elevator')
def textlabel(name,label,loc,size=.45,rotation=(math.pi/2,0,0)):
 cu=bpy.data.curves.new(name,'FONT');cu.body=label;cu.align_x='CENTER';cu.size=size;cu.extrude=.001
 ob=bpy.data.objects.new(name,cu);cols['07_Wayfinding'].objects.link(ob);ob.location=loc;ob.rotation_euler=rotation;cu.materials.append(yellow)
textlabel('LIFT / sign','B1 / B2   -   LIFT',(0,19.32,3.55),.38)
for side,x in [('WEST',-14.5),('EAST',14.5)]:textlabel(side+' / floor title',side+' LAB',(x,17,.015),.6,(0,0,0))
textlabel('ENTRY / floor title','RECEPTION',(0,-22,.015),.7,(0,0,0))
textlabel('DATA / floor title','DATA / CONTROL',(0,14,.015),.65,(0,0,0))

parts=list(cols['01_Floors'].objects);base=parts[0]
temp=bpy.data.collections.new('SI_TEMP_floor_union');scene.collection.children.link(temp)
for ob in parts[1:]:temp.objects.link(ob)
bpy.context.view_layer.objects.active=base
mod=base.modifiers.new('Continuous floor without coplanar overlap','BOOLEAN');mod.operation='UNION';mod.operand_type='COLLECTION';mod.collection=temp;mod.solver='EXACT'
bpy.ops.object.modifier_apply(modifier=mod.name)
for ob in parts[1:]:bpy.data.objects.remove(ob,do_unlink=True)
bpy.data.collections.remove(temp);base.name='GROUND / continuous walkable floor'
p=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Roughness'].default_value=.012;p.inputs['Base Color'].default_value=(.94,.98,1,1)

# Review setup is isolated from the architectural collections.
scene.world=bpy.data.worlds.new('SI_ReviewWorld');scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.55,.65,.75,1);bg.inputs['Strength'].default_value=.6
ld=bpy.data.lights.new('SI soft sun','SUN');lo=bpy.data.objects.new('SI soft sun',ld);cols['90_Preview'].objects.link(lo);lo.rotation_euler=(.35,-.5,-.4);ld.energy=2
ld=bpy.data.lights.new('SI softbox','AREA');lo=bpy.data.objects.new('SI softbox',ld);cols['90_Preview'].objects.link(lo);lo.location=(0,-5,32);ld.energy=7000;ld.shape='DISK';ld.size=30
ca=bpy.data.cameras.new('SI Review Camera');cam=bpy.data.objects.new('SI Review Camera',ca);cols['90_Preview'].objects.link(cam);scene.camera=cam
cam.location=(55,-72,67);cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();ca.type='ORTHO';ca.ortho_scale=72
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1500;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();area.spaces.active.region_3d.view_location=(0,0,1);area.spaces.active.region_3d.view_distance=70
  area.spaces.active.clip_end=1000;area.spaces.active.shading.type='MATERIAL'
scene['design_notes']='Roofless enlarged ground floor. Front reception, round labs, side viewing corridors, rear T labs, 7m lift. Original central cage reused. No elevator animation.'
print(json.dumps({'scene':scene.name,'objects':len(scene.objects),'cage_reused':cage_names,'footprint_m':[42,52],'wall_height':H,'lift_m':[7,7]},indent=2))
