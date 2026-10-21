import bpy, math, os, json, shutil
import numpy as np
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
scene=bpy.context.scene
assert scene.name.startswith('Tactical Soldier')
rig=next(o for o in scene.objects if o.type=='ARMATURE')
path=os.path.join(ROOT,'Assets/Blender/TacticalSoldier/tactical_soldier.blend')
backup=os.path.join(ROOT,'artifacts/tactical_soldier_before_anatomy.blend')
if not os.path.exists(backup): shutil.copy2(path,backup)
materials={m.name:m for m in bpy.data.materials if m.name.startswith('Soldier_')}
parts=[]; bindings={}
camo=materials['Soldier_Camouflage']; leather=materials['Soldier_Leather']
# Reuse the existing manufactured-part helpers, retaining the approved material datablocks.
source=open(os.path.join(ROOT,'artifacts/build_tactical_soldier.py'),encoding='utf-8').read()
exec(source[source.index('def register('):source.index('# Tailored torso')])
bpy.ops.object.select_all(action='DESELECT')
for name in ['Mesh_Soldier_Camouflage','Mesh_Soldier_Leather']:
    ob=scene.objects.get(name)
    if ob: bpy.data.objects.remove(ob,do_unlink=True)
parts=[]

def active(ob):
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active=ob

def smooth_profile(rows,t):
    # Cubic Hermite interpolation gives continuous, anatomical curves between measurements.
    i=min(len(rows)-2,max(0,next((k-1 for k,r in enumerate(rows) if r[0]>t),len(rows)-2)))
    a=np.array(rows[i][1:]); b=np.array(rows[i+1][1:]); dt=rows[i+1][0]-rows[i][0]
    u=(t-rows[i][0])/dt
    prev=np.array(rows[max(0,i-1)][1:]); nex=np.array(rows[min(len(rows)-1,i+2)][1:])
    ma=(b-prev)/(rows[i+1][0]-rows[max(0,i-1)][0])*dt
    mb=(nex-a)/(rows[min(len(rows)-1,i+2)][0]-rows[i][0])*dt
    return (2*u**3-3*u*u+1)*a+(u**3-2*u*u+u)*ma+(-2*u**3+3*u*u)*b+(u**3-u*u)*mb

def organic(name,rows,mat,bone,folds=(),rings=70,segments=48,horizontal=False):
    # rows: t, center xyz, transverse radius, depth. Rings are horizontal for torso/legs,
    # tilted perpendicular to the arm axis for sleeves and fingers.
    verts=[]; faces=[]
    for j in range(rings):
        t=j/(rings-1); q=smooth_profile(rows,t); center=Vector(q[:3]); rx,ry=q[3:]
        qa=smooth_profile(rows,max(0,t-.002)); qb=smooth_profile(rows,min(1,t+.002))
        axis=Vector((0,0,1)) if horizontal else Vector(qb[:3]-qa[:3]).normalized(); u=Vector((1,0,0));u=(u-axis*u.dot(axis)).normalized();v=axis.cross(u)
        for k in range(segments):
            a=k*2*math.pi/segments
            # Localised compression folds at elbows, knees and cuffs; broad areas stay quiet.
            f=0
            for tc,width,amp,freq in folds:
                f+=amp*math.exp(-((t-tc)/width)**2)*math.sin((t-tc)*freq+a*1.7+.5*math.sin(a*3))
            p=center+u*((rx+f)*math.cos(a))+v*((ry+f*.8)*math.sin(a))
            verts.append(tuple(p))
    for j in range(rings-1):
        for k in range(segments):
            a=j*segments+k;b=j*segments+(k+1)%segments;faces.append((a,b,b+segments,a+segments))
    faces.extend([tuple(reversed(range(segments))),tuple((rings-1)*segments+k for k in range(segments))])
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);scene.collection.objects.link(ob);return register(ob,name,mat,bone)

def merge_surface(objects,name,voxel,mat,bone,smoothing=3):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();ob=bpy.context.object;ob.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    ob.data.remesh_voxel_size=voxel;ob.data.use_remesh_preserve_volume=True
    bpy.ops.object.voxel_remesh()
    sm=ob.modifiers.new('Relax garment intersections','SMOOTH');sm.factor=.7;sm.iterations=smoothing;bpy.ops.object.modifier_apply(modifier=sm.name)
    for p in ob.data.polygons:p.use_smooth=True
    ob.data.materials.clear();ob.data.materials.append(mat)
    return ob

def unwrap(ob):
    active(ob);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.012)
    bpy.ops.object.mode_set(mode='OBJECT')

def weight(ob,fn):
    ob.vertex_groups.clear()
    for b in rig.data.bones:ob.vertex_groups.new(name=b.name)
    for vert in ob.data.vertices:
        d=fn(ob.matrix_world@vert.co); d={k:v for k,v in d.items() if v>.005};total=sum(d.values())
        for k,w in d.items(): ob.vertex_groups[k].add([vert.index],w/total,'REPLACE')
    ob.parent=rig;mod=ob.modifiers.new('Character skeleton','ARMATURE');mod.object=rig

def blend(a,b,t):
    t=max(0,min(1,t));t=t*t*(3-2*t);return {a:1-t,b:t}

# Rib cage, trapezius, deltoid and sleeve form one sewn garment, with no exposed flat caps.
jacket=[]
jacket.append(organic('Anatomical jacket torso',[
 (0,0,0,1.005,.164,.106),(.18,0,.003,1.10,.166,.112),(.46,0,.004,1.27,.206,.125),
 (.74,0,.005,1.425,.225,.128),(.87,0,.005,1.48,.199,.105),(1,0,.008,1.525,.079,.078)
],camo,'Chest',[(.12,.1,.003,85)],rings=64))
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'
 jacket.append(sphere('Rounded deltoid '+s,(sgn*.212,.004,1.433),(.089,.098,.073),camo,'UpperArm_'+s))
 jacket.append(organic('Continuous shaped sleeve '+s,[
 (0,sgn*.213,.005,1.462,.066,.077),(.12,sgn*.249,.004,1.408,.082,.083),
 (.31,sgn*.282,.007,1.31,.078,.082),(.53,sgn*.312,.004,1.178,.061,.065),
 (.65,sgn*.332,-.002,1.117,.067,.065),(.84,sgn*.352,-.01,1.025,.049,.048),
 (1,sgn*.366,-.014,.953,.040,.039)
 ],camo,'UpperArm_'+s,[(.53,.12,.0045,94),(.92,.08,.0026,120)],rings=90))
jacket=merge_surface(jacket,'Jacket continuous shoulders and underarms',.0045,camo,'Chest',5)
unwrap(jacket)
for uv in jacket.data.uv_layers.active.data:uv.uv*=3.0
def jacket_weights(p):
 x,y,z=p;side='L' if x<0 else 'R';x=abs(x)
 # Overlapping shoulder weights bridge the arm to the torso rather than tearing a seam.
 if x>.205 and z<1.52:
  if z>1.32:
   return blend('Chest','UpperArm_'+side,(x-.195)/.09+(1.40-z)*1.5)
  return blend('UpperArm_'+side,'Forearm_'+side,(1.235-z)/.135)
 return blend('Spine','Chest',(z-1.13)/.23)
weight(jacket,jacket_weights)

# Pelvis with a real seat/crotch transition, full upper thighs and posterior calf volume.
pants=[]
pants.append(organic('Trouser pelvic saddle',[
 (0,0,.025,.855,.113,.085),(.28,0,.020,.905,.159,.126),(.7,0,.012,.988,.177,.125),(1,0,0,1.04,.165,.108)
],camo,'Hips',rings=38))
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'
 pants.append(organic('Continuous tailored trouser leg '+s,[
 (0,sgn*.092,.018,.98,.094,.108),(.12,sgn*.10,.019,.897,.108,.119),
 (.29,sgn*.109,.016,.773,.103,.112),(.43,sgn*.114,.002,.653,.088,.093),
 (.58,sgn*.115,-.002,.531,.071,.078),(.70,sgn*.117,.023,.435,.081,.086),
 (.83,sgn*.119,.019,.333,.066,.070),(1,sgn*.12,.013,.222,.057,.058)
 ],camo,'Thigh_'+s,[(.56,.12,.0053,105),(.93,.1,.005,120)],rings=110))
pants=merge_surface(pants,'Trousers continuous hip and leg anatomy',.0042,camo,'Hips',4);unwrap(pants)
for uv in pants.data.uv_layers.active.data:uv.uv*=3.0
def pants_weights(p):
 x,y,z=p;s='L' if x<0 else 'R'
 if z>.85:return blend('Thigh_'+s,'Hips',(z-.85)/.16)
 return blend('Shin_'+s,'Thigh_'+s,(z-.47)/.14)
weight(pants,pants_weights)

# Rebuild cloth pockets with less blocky profiles; existing flaps and velcro stay in place.
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'
 for ob in [cube('Soft cargo pocket '+s,(sgn*.192,.005,.755),(.04,.137,.173),camo,'Thigh_'+s,.017),
            cube('Sleeve patch pocket '+s,(sgn*.277,-.073,1.335),(.083,.025,.103),camo,'UpperArm_'+s,.013)]:
  unwrap(ob);weight(ob,lambda p,b=bindings[ob]:{b:1})

# Anatomical gloves: tapered palm, separate metacarpals, naturally flexed fingers and opposing thumb.
new_leather=[]
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'; hand=[]
 hand.append(organic('Gloved palm '+s,[(0,sgn*.365,-.014,.965,.027,.025),(.3,sgn*.374,-.014,.928,.036,.025),(.7,sgn*.379,-.011,.895,.037,.024),(1,sgn*.38,-.009,.877,.029,.02)],leather,'Hand_'+s,rings=30))
 # The hand hangs with a slight natural cup. Fingers shorten towards the little-finger side.
 for f in range(4):
  x=sgn*(.352+f*.018);ln=[.069,.078,.073,.056][f];z=.888-[.002,0,.002,.008][f]
  hand.append(organic('Curled finger '+s+str(f),[
   (0,x,-.012,z,.0095,.010),(.35,x+sgn*.001,-.015,z-ln*.36,.009,.0095),
   (.67,x+sgn*.002,-.025,z-ln*.68,.008,.008),(1,x+sgn*.001,-.037,z-ln*.89,.0045,.005)
  ],leather,'Hand_'+s,rings=24,segments=16))
  hand.append(sphere('Finger rounded tip',(x+sgn*.001,-.037,z-ln*.89),(.005,.005,.005),leather,'Hand_'+s))
 hand.append(organic('Opposing thumb '+s,[(0,sgn*.352,-.009,.928,.015,.014),(.4,sgn*.333,-.016,.908,.012,.012),(.7,sgn*.331,-.032,.888,.010,.010),(1,sgn*.342,-.044,.88,.006,.007)],leather,'Hand_'+s,rings=28,segments=20))
 ob=merge_surface(hand,'Sculpted glove '+s,.0018,leather,'Hand_'+s,3);unwrap(ob);weight(ob,lambda p,b='Hand_'+s:{b:1});new_leather.append(ob)
 # Boot lasts follow an instep, ankle, round toe and heel, not an overlapping box and tube.
 boot=[];x=sgn*.12
 boot.append(organic('Boot instep '+s,[(0,x,-.058,.049,.058,.13),(.22,x,-.059,.075,.060,.131),(.50,x,-.032,.108,.055,.103),(.78,x,.008,.146,.052,.062),(1,x,.013,.24,.055,.055)],leather,'Foot_'+s,rings=42,horizontal=True))
 ob=merge_surface(boot,'Shaped leather boot '+s,.0025,leather,'Foot_'+s,3);unwrap(ob);weight(ob,lambda p,b='Foot_'+s:{b:1});new_leather.append(ob)

# Consolidate to preserve existing Unity material remaps and modest draw-call count.
for mat in [camo,leather]:
 group=[o for o in scene.objects if o.type=='MESH' and len(o.data.materials) and o.data.materials[0]==mat]
 bpy.ops.object.select_all(action='DESELECT')
 for ob in group:ob.select_set(True)
 bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();ob=bpy.context.object;ob.name='Mesh_'+mat.name
 # Retain silhouette and skin weights while reducing the dense sculpt working surface.
 dec=ob.modifiers.new('Game mesh reduction','DECIMATE');dec.ratio=.34
 bpy.ops.object.modifier_apply(modifier=dec.name)

# Existing rigid plates, webbing and head equipment are untouched. Fill the neck with a
# more human tapered sternocleidomastoid silhouette while retaining its approved fabric.
mask_ob=scene.objects.get('Mesh_Soldier_Mask')
neck=mask_ob.vertex_groups.get('Neck')
for v in mask_ob.data.vertices:
 if neck and any(g.group==neck.index and g.weight>.5 for g in v.groups):
  p=mask_ob.matrix_world@v.co
  if p.z<1.59 and not scene.get('anatomy_revision'):
   factor=1+.18*max(0,1-abs(p.z-1.51)/.1);p.x*=factor;p.y*=factor;v.co=mask_ob.matrix_world.inverted()@p

scene['anatomy_revision']='Continuous shoulder/underarm and pelvis, shaped limbs, localised garment folds, organic gloves/boots, blended joints'
for ob in scene.objects:
 if ob.type=='MESH':
  for p in ob.data.polygons:p.use_smooth=True
active(scene.objects['Mesh_Soldier_Camouflage'])
print(json.dumps({'meshes':sum(o.type=='MESH' for o in scene.objects),'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),'backup':backup}))
