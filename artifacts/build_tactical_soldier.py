import bpy, math, os, json
import numpy as np
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets','Blender','TacticalSoldier')
os.makedirs(os.path.join(OUT,'Textures'),exist_ok=True)
scene=bpy.data.scenes.new('Tactical Soldier - textile and equipment study')
bpy.context.window.scene=scene
scene.unit_settings.scale_length=1
parts=[]; bindings={}; materials={}; manifest=[]
def noise(n,g,seed):
    rng=np.random.default_rng(seed);a=rng.random((g+1,g+1));a[-1,:]=a[0,:];a[:,-1]=a[:,0]
    q=np.arange(n)*g/n;i=q.astype(int);f=q-i;f=f*f*(3-2*f)
    return (a[i[:,None],i[None,:]]*(1-f[None,:])+a[i[:,None],i[None,:]+1]*f[None,:])*(1-f[:,None])+(a[i[:,None]+1,i[None,:]]*(1-f[None,:])+a[i[:,None]+1,i[None,:]+1]*f[None,:])*f[:,None]
def save_image(name,a,color=True):
    n=a.shape[0];rgba=np.ones((n,n,4),np.float32)
    rgba[:,:,:3]=a[:,:,None] if a.ndim==2 else a
    im=bpy.data.images.new(name,width=n,height=n,alpha=True);im.pixels.foreach_set(rgba.ravel());im.colorspace_settings.name='sRGB' if color else 'Non-Color'
    im.filepath_raw=os.path.join(OUT,'Textures',name+'.png');im.file_format=next(e.identifier for e in im.bl_rna.properties['file_format'].enum_items if e.identifier=='PNG');im.save();return im
def material(name,col,rough,metal=0,textured=False,camo=False):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*col,1);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    entry={'name':name,'color':list(col),'roughness':rough,'metallic':metal}
    if textured:
        n=2048 if camo else 1024;yy,xx=np.mgrid[:n,:n];v=noise(n,26,11);f=noise(n,110,8);weave=(np.sin(xx*math.pi*.5)*np.sin(yy*math.pi*.5))*.5+.5
        rgb=np.ones((n,n,3))*np.array(col)
        if camo:
            broad=noise(n,10,2);detail=noise(n,23,19);mask=broad*.72+detail*.28
            palette=np.array([[.21,.23,.155],[.34,.36,.24],[.49,.47,.34],[.62,.59,.45]])
            rgb=palette[np.digitize(mask,[.37,.50,.65])];rgb=np.where((noise(n,35,42)>.73)[:,:,None],np.array([.15,.17,.115]),rgb)
        rgb*=((.93+.10*f+.025*weave)[:,:,None])
        height=weave*.035+(f-.5)*.045+(v-.5)*.04
        dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*2;dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*2
        nm=np.dstack((-dx,-dy,np.ones_like(dx)));nm/=np.linalg.norm(nm,axis=2)[:,:,None];nm=nm*.5+.5
        images={'albedo':save_image(name+'_BaseColor',np.clip(rgb,0,1)),'normal':save_image(name+'_Normal',nm,False),'roughness':save_image(name+'_Roughness',np.clip(rough+(f-.5)*.13,0,1),False)}
        for key,im in images.items():
            node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=im
            if key=='albedo':m.node_tree.links.new(node.outputs['Color'],p.inputs['Base Color'])
            elif key=='roughness':m.node_tree.links.new(node.outputs['Color'],p.inputs['Roughness'])
            else:
                normal=m.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.35;m.node_tree.links.new(node.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],p.inputs['Normal'])
        entry['maps']={k:'Assets/Blender/TacticalSoldier/Textures/'+im.name+'.png' for k,im in images.items()}
    materials[name]=m;manifest.append(entry);return m
camo=material('Soldier_Camouflage',(.46,.45,.33),.83,textured=True,camo=True)
cordura=material('Soldier_Cordura',(.35,.32,.22),.88,textured=True)
mask=material('Soldier_Mask',(.035,.042,.038),.91,textured=True)
leather=material('Soldier_Leather',(.27,.23,.16),.75,textured=True)
shell=material('Soldier_Helmet',(.34,.34,.26),.72,0.05)
rubber=material('Soldier_Rubber',(.025,.03,.027),.76)
metal=material('Soldier_Metal',(.11,.12,.105),.39,.65)
glass=material('Soldier_GoggleLens',(.012,.022,.025),.12,.35)
thread=material('Soldier_Stitching',(.49,.46,.34),.85)
def register(o,name,mat,bone):
    o.name=name;o.data.materials.append(mat);parts.append(o);bindings[o]=bone
    for p in o.data.polygons:p.use_smooth=True
    return o
def cube(name,loc,dim,mat,bone,bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft manufactured edges','BEVEL');mod.width=bevel;mod.segments=3;bpy.ops.object.modifier_apply(modifier=mod.name)
        mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=mod.name)
    return register(o,name,mat,bone)
def sphere(name,loc,scale,mat,bone):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,radius=1,location=loc);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return register(o,name,mat,bone)
def line(name,coords,r,mat,bone):
    cv=bpy.data.curves.new(name,'CURVE');cv.dimensions='3D';cv.resolution_u=1;cv.bevel_depth=r;cv.bevel_resolution=2;s=cv.splines.new('POLY');s.points.add(len(coords)-1)
    for p,c in zip(s.points,coords):p.co=(*c,1)
    ob=bpy.data.objects.new(name,cv);scene.collection.objects.link(ob);bpy.context.view_layer.objects.active=ob;ob.select_set(True);bpy.ops.object.convert(target='MESH');return register(bpy.context.object,name,mat,bone)
def loft(name,a,b,profile,mat,bone,wrinkle=.006,segments=40,rings=28):
    a=Vector(a);b=Vector(b);axis=(b-a).normalized();u=Vector((1,0,0));u=(u-axis*u.dot(axis)).normalized();v=axis.cross(u);verts=[];faces=[]
    for j in range(rings):
        t=j/(rings-1);r1=np.interp(t,[p[0] for p in profile],[p[1] for p in profile]);r2=np.interp(t,[p[0] for p in profile],[p[2] for p in profile]);center=a.lerp(b,t)
        for i in range(segments):
            ang=2*math.pi*i/segments;fold=wrinkle*math.sin(math.pi*t)**.5*(math.sin(t*43+ang*3)+.45*math.sin(t*77-ang*5));q=center+u*((r1+fold)*math.cos(ang))+v*((r2+fold)*math.sin(ang));verts.append(tuple(q))
    for j in range(rings-1):
        for i in range(segments):
            k=j*segments+i;kn=j*segments+(i+1)%segments;faces.append((k,kn,kn+segments,k+segments))
    faces.append(tuple(reversed(range(segments))));faces.append(tuple((rings-1)*segments+i for i in range(segments)))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o)
    uv=me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        for li in poly.loop_indices:
            vi=me.loops[li].vertex_index;uv.data[li].uv=(vi%segments/segments,vi//segments/(rings-1))
    return register(o,name,mat,bone)
# Tailored torso, collar, belt, soft vest panels.
loft('Combat jacket tailored torso',(0,0,.94),(0,0,1.47),[(0,.16,.10),(.2,.17,.11),(.6,.21,.115),(.88,.225,.11),(1,.18,.09)],camo,'Spine',.004)
loft('Trouser seat',(0,0,.87),(0,0,1.04),[(0,.145,.095),(.5,.175,.11),(1,.16,.105)],camo,'Hips',.004)
loft('Stand collar',(0,0,1.43),(0,0,1.55),[(0,.10,.09),(.8,.08,.076),(1,.078,.071)],camo,'Chest',.003)
cube('Front plate carrier',(0,-.123,1.285),(.365,.085,.36),cordura,'Chest',.035)
cube('Back plate carrier',(0,.125,1.29),(.35,.09,.36),cordura,'Chest',.035)
loft('Belt',(0,0,.993),(0,0,1.04),[(0,.181,.118),(1,.181,.118)],cordura,'Hips',0,rings=4)
cube('Belt locking buckle',(0,-.131,1.015),(.062,.018,.04),metal,'Hips',.005)
cube('Buckle insert',(0,-.143,1.015),(.038,.006,.023),rubber,'Hips',.003)
for side in [-1,1]:
    s='L' if side<0 else 'R'
    line('Padded shoulder strap '+s,[(side*.14,-.17,1.39),(side*.15,-.09,1.48),(side*.15,.04,1.49),(side*.14,.16,1.39)],.024,cordura,'Chest')
    cube('Shoulder adjustment buckle '+s,(side*.14,-.158,1.425),(.052,.021,.048),metal,'Chest',.004)
    cube('Buckle webbing '+s,(side*.14,-.173,1.425),(.032,.008,.04),cordura,'Chest',.002)
    cube('Belt utility pouch '+s,(side*.187,-.035,.987),(.065,.09,.12),cordura,'Hips',.012)
for back in [False,True]:
    y=.179 if back else -.17
    for row in range(6):
        z=1.12+row*.048
        for col in range(6):
            x=(col-2.5)*.052
            cube('MOLLE woven loop', (x,y,z),(.046,.008,.016),cordura,'Chest',.002)
            for end in [-1,1]:line('MOLLE bar tack',[(x+end*.02,y+(.005 if back else -.005),z-.006),(x+end*.02,y+(.005 if back else -.005),z+.006)],.0008,thread,'Chest')
for i in [-1,0,1]:
    x=i*.098;cube('Front magazine pouch',(x,-.203,1.16),(.087,.065,.15),cordura,'Chest',.012)
    cube('Pouch closing flap',(x,-.24,1.22),(.083,.012,.043),cordura,'Chest',.007)
    line('Pouch seam',[(x-.035,-.239,1.1),(x-.035,-.239,1.205),(x+.035,-.239,1.205),(x+.035,-.239,1.1)],.0012,thread,'Chest')
    cube('Pouch pull tab',(x,-.251,1.2),(.018,.006,.029),rubber,'Chest',.002)
cube('Back hydration pack',(0,.203,1.31),(.25,.10,.29),cordura,'Chest',.03)
line('Hydration hose',[(.10,.24,1.42),(.18,.13,1.49),(.19,-.04,1.45),(.17,-.19,1.32)],.006,rubber,'Chest')
# Limbs, boots, sewn cargo pockets and knee pads.
bone_specs=[('Hips',(0,0,.88),(0,0,1.04),None),('Spine',(0,0,1.04),(0,0,1.27),'Hips'),('Chest',(0,0,1.27),(0,0,1.48),'Spine'),('Neck',(0,0,1.48),(0,0,1.58),'Chest'),('Head',(0,0,1.58),(0,0,1.82),'Neck')]
for sign in [-1,1]:
    s='L' if sign<0 else 'R';hip=(sign*.10,0,.94);knee=(sign*.115,-.004,.53);ankle=(sign*.12,.012,.16);shoulder=(sign*.215,0,1.43);elbow=(sign*.31,0,1.17);wrist=(sign*.365,-.014,.955)
    bone_specs += [('Thigh_'+s,hip,knee,'Hips'),('Shin_'+s,knee,ankle,'Thigh_'+s),('Foot_'+s,ankle,(sign*.12,-.16,.07),'Shin_'+s),('UpperArm_'+s,shoulder,elbow,'Chest'),('Forearm_'+s,elbow,wrist,'UpperArm_'+s),('Hand_'+s,wrist,(sign*.38,-.02,.855),'Forearm_'+s)]
    loft('Trouser thigh '+s,hip,knee,[(0,.09,.095),(.2,.103,.11),(.65,.087,.092),(1,.074,.078)],camo,'Thigh_'+s,.006)
    loft('Trouser calf '+s,knee,ankle,[(0,.077,.079),(.25,.083,.085),(.65,.067,.065),(1,.055,.055)],camo,'Shin_'+s,.007)
    cube('Cargo thigh pocket '+s,(sign*.19,-.005,.745),(.045,.135,.17),camo,'Thigh_'+s,.014)
    cube('Cargo pocket flap '+s,(sign*.214,-.005,.805),(.013,.14,.045),cordura,'Thigh_'+s,.006)
    sphere('Knee pad padded backing '+s,(sign*.114,-.071,.542),(.071,.026,.098),cordura,'Shin_'+s)
    cube('Knee pad molded shell '+s,(sign*.114,-.097,.542),(.10,.029,.135),shell,'Shin_'+s,.021)
    for k in [-1,1]:line('Knee articulation groove',[(sign*.114-.033,-.114,.542+k*.035),(sign*.114+.033,-.114,.542+k*.035)],.0025,rubber,'Shin_'+s)
    x=sign*.12
    cube('Boot lugged sole '+s,(x,-.065,.026),(.129,.285,.052),rubber,'Foot_'+s,.014)
    cube('Boot leather foot '+s,(x,-.066,.091),(.124,.264,.11),leather,'Foot_'+s,.033)
    loft('Boot ankle upper '+s,(x,.013,.08),(x,.013,.255),[(0,.062,.079),(.4,.057,.061),(1,.057,.057)],leather,'Foot_'+s,.002,rings=16)
    cube('Boot toe cap '+s,(x,-.164,.089),(.12,.075,.064),leather,'Foot_'+s,.022)
    for j in range(8):
        z=.11+j*.017;y=-.057+(z-.11)*.10
        line('Crossed boot lace',[(x-.036,y,z),(x+.036,y-.008,z+.012)],.002,cordura,'Foot_'+s)
        line('Crossed boot lace',[(x+.036,y,z),(x-.036,y-.008,z+.012)],.002,cordura,'Foot_'+s)
    for j in range(7):
        for side in [-1,1]:cube('Boot tread block',(x+side*.045,-.175+j*.037,.012),(.035,.023,.022),rubber,'Foot_'+s,.002)
    loft('Jacket upper sleeve '+s,shoulder,elbow,[(0,.081,.083),(.3,.089,.083),(.7,.072,.070),(1,.059,.061)],camo,'UpperArm_'+s,.005)
    loft('Jacket forearm '+s,elbow,wrist,[(0,.062,.064),(.3,.065,.065),(.8,.05,.048),(1,.041,.04)],camo,'Forearm_'+s,.005)
    sphere('Sleeve elbow reinforcement '+s,(sign*.317,.039,1.17),(.057,.026,.063),cordura,'Forearm_'+s)
    cube('Sleeve pocket '+s,(sign*.282,-.051,1.32),(.082,.025,.10),camo,'UpperArm_'+s,.012)
    cube('Shoulder velcro patch '+s,(sign*.282,-.067,1.34),(.062,.009,.045),cordura,'UpperArm_'+s,.005)
    loft('Cuff '+s,(sign*.359,-.013,.98),(sign*.369,-.014,.943),[(0,.045,.041),(1,.043,.04)],cordura,'Forearm_'+s,.001,rings=5)
    sphere('Glove palm '+s,(sign*.378,-.014,.909),(.038,.026,.058),leather,'Hand_'+s)
    sphere('Glove knuckle protection '+s,(sign*.378,-.037,.923),(.034,.009,.024),rubber,'Hand_'+s)
    for finger in range(4):
        x=sign*(.351+finger*.018);length=[.056,.068,.064,.048][finger]
        loft('Glove articulated finger '+s,(x,-.018,.882),(x,-.034,.882-length),[(0,.009,.010),(.55,.008,.009),(1,.006,.007)],leather,'Hand_'+s,.0006,segments=12,rings=8)
    loft('Glove thumb '+s,(sign*.345,-.016,.932),(sign*.327,-.038,.884),[(0,.012,.013),(1,.009,.009)],leather,'Hand_'+s,.001,segments=16,rings=8)
# Fully covered head: fabric, goggles and helmet hardware.
loft('Balaclava neck',(0,0,1.47),(0,0,1.63),[(0,.067,.069),(.5,.071,.073),(1,.087,.084)],mask,'Neck',.002)
sphere('Balaclava covered head',(0,.005,1.675),(.101,.105,.131),mask,'Head')
sphere('Contoured fabric face mask',(0,-.077,1.602),(.085,.049,.078),mask,'Head')
for x in [-.055,0,.055]:line('Mask tailored seam',[(x,-.115,1.55),(x*.95,-.123,1.59),(x*.83,-.118,1.64)],.001,mask,'Head')
for side in [-1,1]:
    line('Mask retention strap',[(side*.075,-.082,1.59),(side*.107,-.01,1.64),(side*.084,.065,1.60)],.005,rubber,'Head')
    sphere('Ear protection cup',(side*.113,.008,1.665),(.024,.052,.064),rubber,'Head')
    cube('Ear cup mounting plate',(side*.136,.008,1.675),(.012,.06,.065),shell,'Head',.012)
    line('Helmet headset arm',[(side*.137,.01,1.68),(side*.148,.01,1.73),(side*.125,.012,1.76)],.005,metal,'Head')
# High-cut helmet dome with a real thickness.
nv=64;nr=20;verts=[];faces=[]
for j in range(nr):
    for i in range(nv):
        phi=2*math.pi*i/nv;limit=1.68-.30*abs(math.cos(phi))**6;theta=.025+(limit-.025)*j/(nr-1)
        verts.append((.133*math.sin(theta)*math.cos(phi),.144*math.sin(theta)*math.sin(phi)+.008,1.713+.148*math.cos(theta)))
for j in range(nr-1):
    for i in range(nv):a=j*nv+i;b=j*nv+(i+1)%nv;faces.append((a,b,b+nv,a+nv))
faces.append(tuple(reversed(range(nv))))
me=bpy.data.meshes.new('Helmet dome');me.from_pydata(verts,[],faces);me.update();ob=bpy.data.objects.new('FAST high cut helmet shell',me);scene.collection.objects.link(ob);register(ob,ob.name,shell,'Head')
bpy.context.view_layer.objects.active=ob;mod=ob.modifiers.new('Helmet rim thickness','SOLIDIFY');mod.thickness=.006;bpy.ops.object.modifier_apply(modifier=mod.name)
line('Helmet rubber edge',verts[-nv:]+[verts[-nv]],.004,rubber,'Head')
for side in [-1,1]:
    rail=cube('Accessory side rail',(side*.132,.007,1.739),(.019,.145,.029),shell,'Head',.007)
    for k in range(6):sphere('Rail recessed slot',(side*.143,-.047+k*.022,1.739),(.002,.007,.006),rubber,'Head')
    for y in [-.064,.077]:sphere('Rail screw',(side*.145,y,1.741),(.003,.005,.005),metal,'Head')
    cube('Helmet side velcro',(side*.123,.025,1.79),(.012,.066,.041),cordura,'Head',.006)
    line('Helmet bungee',[(side*.055,-.127,1.75),(side*.083,-.103,1.80),(side*.092,-.06,1.83),(side*.075,.035,1.84)],.003,rubber,'Head')
cube('Rear helmet webbing patch',(0,.147,1.733),(.10,.01,.036),cordura,'Head',.006)
cube('NVG metal mount',(0,-.141,1.768),(.049,.018,.063),metal,'Head',.006)
cube('NVG socket',(0,-.153,1.768),(.027,.008,.036),rubber,'Head',.003)
for x in [-.018,.018]:
    for z in [1.745,1.791]:sphere('NVG screw',(x,-.154,z),(.003,.002,.003),metal,'Head')
cube('Top helmet velcro',(0,.008,1.859),(.055,.075,.006),cordura,'Head',.004)
# Wrapped goggle lens with nose bridge cutout.
outline=[(-.105,1.69),(-.082,1.705),(-.035,1.708),(0,1.705),(.035,1.708),(.082,1.705),(.105,1.69),(.104,1.653),(.085,1.641),(.035,1.639),(.021,1.653),(0,1.666),(-.021,1.653),(-.035,1.639),(-.085,1.641),(-.104,1.653)]
coords=[(x,-.126+.31*x*x,z) for x,z in outline]
line('Goggle rubber frame',coords+[coords[0]],.007,rubber,'Head')
me=bpy.data.meshes.new('Wraparound smoked lens');me.from_pydata(coords,[],[tuple(reversed(range(len(coords))))]);me.update();o=bpy.data.objects.new('Smoked panoramic goggles',me);scene.collection.objects.link(o);register(o,o.name,glass,'Head')
for side in [-1,1]:
    line('Goggle elastic band',[(side*.105,-.105,1.673),(side*.126,.0,1.68),(side*.105,.09,1.68),(side*.04,.139,1.68)],.012,mask,'Head')
    cube('Goggle adjuster',(side*.13,.05,1.683),(.012,.03,.027),metal,'Head',.004)
    line('Chin strap',[(side*.095,.03,1.68),(side*.092,-.013,1.55),(side*.04,-.067,1.522)],.007,cordura,'Head')
    cube('Chin strap buckle',(side*.085,-.025,1.557),(.019,.014,.026),metal,'Head',.003)
# UV every manufactured part, then bind each anatomical assembly to a reusable skeleton.
for o in parts:
    if not o.data.uv_layers:
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
arm=bpy.data.armatures.new('Tactical Soldier skeleton');rig=bpy.data.objects.new('TacticalSoldier_Rig',arm);scene.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,h,t,parent in bone_specs:
    bone=arm.edit_bones.new(name);bone.head=h;bone.tail=t
    if parent:bone.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
for o in parts:
    group=o.vertex_groups.new(name=bindings[o]);group.add(list(range(len(o.data.vertices))),1,'REPLACE');mod=o.modifiers.new('Character skeleton','ARMATURE');mod.object=rig;o.parent=rig
# Consolidate by material for modest draw-call cost while preserving bone weights.
for mat in materials.values():
    group=[o for o in scene.objects if o.type=='MESH' and o.data.materials[0]==mat]
    if not group:continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in group:o.select_set(True)
    bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();group[0].name='Mesh_'+mat.name
scene['reference']='Helmet, goggles, face mask and full tactical uniform reference supplied by user'
scene['height_metres']=1.864;rig['equipment']='FAST helmet, goggles, mask, headset, plate carrier, MOLLE, pouches, gloves, knee pads, boots'
for im in bpy.data.images:
    if im.filepath.startswith(OUT):im.filepath='//Textures/'+os.path.basename(im.filepath)
path=os.path.join(OUT,'tactical_soldier.blend');assert not os.path.exists(path),'Target exists'
bpy.data.libraries.write(path,{scene},fake_user=True)
with open(os.path.join(ROOT,'artifacts','soldier_material_manifest.json'),'w') as f:json.dump(manifest,f,indent=2)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_location=Vector((0,0,.95));area.spaces.active.region_3d.view_distance=3.4;area.spaces.active.region_3d.view_rotation=Vector((1,-3,.8)).to_track_quat('Z','Y')
print('Saved',path,'meshes',sum(o.type=='MESH' for o in scene.objects),'bones',len(arm.bones),'vertices',sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'))

