import bpy, math, os, json
from mathutils import Vector

BASE=r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(BASE,'Assets','Blender','steel_storage_cabinet.blend')
REF=r'C:\Users\PLUS\AppData\Local\Temp\codex-clipboard-6ebb8b92-0ef8-47e4-b600-5fb25eb51a98.png'
ART=os.path.join(BASE,'artifacts')
assert not os.path.exists(OUT), 'Existing asset must not be overwritten'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.name='Steel_Storage_Cabinet'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
root=bpy.data.objects.new('SteelStorageCabinet',None);scene.collection.objects.link(root)
root.empty_display_size=.06
root['reference']='User-provided six-view grey steel cabinet'
root['scale_note']='Image proportions; approximate 1.08m wide, 0.65m deep, 1.90m high'
root['orientation']='Front -Y, up +Z, origin at floor center; meter units'
groups={}
for name in ['01_Cabinet_Shell','02_Left_Door','03_Right_Door','04_Hinges','05_Rear_Reinforcement','06_Base_Hardware']:
    c=bpy.data.collections.new(name);scene.collection.children.link(c);groups[name]=c

def material(name,color,metal=.4,rough=.5,coat=False):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links;p=next(n for n in n if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if coat:
        tc=n.new('ShaderNodeTexCoord');noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=230
        noise.inputs['Detail'].default_value=2;l.new(tc.outputs['Object'],noise.inputs['Vector'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.11;bump.inputs['Distance'].default_value=.00012
        l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
        ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*[x*.94 for x in color],1)
        ramp.color_ramp.elements[1].color=(*[x*1.04 for x in color],1)
        l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
    return m

paint=material('Cabinet | warm grey powder-coated steel',(.285,.281,.256),.38,.48,True)
folds=material('Cabinet | folded edging',(.258,.255,.235),.43,.43,True)
inside=material('Cabinet | interior grey',(.235,.235,.216),.3,.55,True)
handle=material('Handle | charcoal moulding',(.028,.031,.030),.25,.4)
black=material('Recess | matte black',(.006,.008,.008),.05,.7)
nickel=material('Lock | satin nickel',(.48,.49,.47),.87,.25)
fastener=material('Hardware | dark zinc',(.14,.15,.14),.78,.36)
rubber=material('Base | rubber pads',(.018,.019,.018),0,.85)

def assign(o,name,mat,group):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    groups[group].objects.link(o);o.parent=root
    if mat:o.data.materials.append(mat)
    return o

def bevel(o,w=.0015,segments=3):
    b=o.modifiers.new('Folded steel edge radius','BEVEL');b.width=w;b.segments=segments
    return o

def box(name,loc,dims,mat=paint,group='01_Cabinet_Shell',bev=.0015):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=assign(bpy.context.object,name,mat,group)
    for v in o.data.vertices:v.co.x*=dims[0];v.co.y*=dims[1];v.co.z*=dims[2]
    if bev:bevel(o,bev)
    return o

def cyl(name,loc,r,depth,mat=fastener,axis=(0,0,1),group='06_Base_Hardware',n=32,bev=.0005):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=depth,location=loc)
    o=assign(bpy.context.object,name,mat,group);o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    if bev:bevel(o,bev,2)
    return o

def screw(x,y,z,axis=(0,1,0),group='05_Rear_Reinforcement',r=.004):
    cyl('Recessed screw head',(x,y,z),r,.002,fastener,axis,group,n=20)
    if axis==(0,1,0):box('Screwdriver slot',(x,y+.0012,z),(.0055,.0006,.0008),black,group,.0001)
    elif axis==(0,0,-1):box('Screwdriver slot',(x,y,z-.0012),(.0055,.0008,.0006),black,group,.0001)

# A genuinely hollow steel carcass, with folded returns around the front opening.
for s in [-1,1]:
    x=s*.518
    box('Side wall sheet',(x,.003,.967),(.014,.60,1.793),paint)
    box('Front upright folded return',(s*.505,-.300,.967),(.035,.025,1.793),folds)
    box('Inside front return',(s*.484,-.278,.967),(.012,.045,1.77),inside)
    box('Rear inside folded return',(s*.50,.292,.967),(.036,.026,1.79),inside)
box('Floor pan',(0,0,.088),(1.02,.60,.021),inside)
box('Ceiling pan',(0,0,1.855),(1.02,.60,.016),inside)
box('Top overhanging cap',(0,0,1.881),(1.08,.65,.043),paint,bev=.002)
box('Top cap front fold',(0,-.317,1.865),(1.073,.016,.018),folds,bev=.001)
box('Bottom plinth',(0,0,.047),(1.075,.644,.061),paint,bev=.002)
box('Lower front sill',(0,-.308,.084),(1.016,.024,.026),folds,bev=.001)
box('Upper front lintel',(0,-.299,1.848),(1.016,.025,.026),folds,bev=.001)
for x in [-.254,.254]:box('Rear steel panel',(x,.300,.963),(.505,.012,1.781),paint)

# Flat closed doors, narrow central seam, genuine cut-outs for inset pull handles.
door_pivots=[]
for side,label,group in [(-1,'Left','02_Left_Door'),(1,'Right','03_Right_Door')]:
    cx=side*.2485
    door=box('Door_'+label,(cx,-.309,.963),(.492,.022,1.775),paint,group,0)
    hx=side*.057
    cutter=box('TEMP handle cutout',(hx,-.308,1.023),(.032,.09,.106),None,group,0)
    bpy.context.view_layer.objects.active=door
    boolean=door.modifiers.new('Actual recessed handle opening','BOOLEAN');boolean.operation='DIFFERENCE';boolean.object=cutter
    bpy.ops.object.modifier_apply(modifier=boolean.name)
    bpy.data.objects.remove(cutter,do_unlink=True)
    bevel(door,.0014)
    # Sheet-metal hems and inside stiffening, carried with each individual door.
    for dx in [-.238,.238]:box('Door '+label+' vertical hem',(cx+dx,-.287,.963),(.011,.025,1.765),folds,group,.0008)
    for z in [.085,1.842]:box('Door '+label+' horizontal hem',(cx,-.287,z),(.478,.025,.013),folds,group,.0008)
    box('Door '+label+' inside stiffener',(cx,-.277,.967),(.033,.018,1.64),inside,group,.001)
    # Small bezel, deep black cup and a slim recessed grip; no protruding bar handle.
    for dx in [-.021,.021]:box('Handle '+label+' bezel side',(hx+dx,-.324,1.023),(.008,.008,.125),handle,group,.0013)
    for dz in [-.0585,.0585]:box('Handle '+label+' bezel end',(hx,-.324,1.023+dz),(.041,.008,.008),handle,group,.0013)
    box('Handle '+label+' pocket back',(hx,-.281,1.023),(.033,.006,.107),black,group,.002)
    for dx in [-.015,.015]:box('Handle '+label+' cup wall',(hx+dx,-.301,1.023),(.003,.038,.105),handle,group,.0006)
    for dz in [-.051,.051]:box('Handle '+label+' cup end',(hx,-.301,1.023+dz),(.029,.038,.003),handle,group,.0006)
    box('Handle '+label+' recessed grip',(hx+side*.006,-.316,1.023),(.006,.008,.086),fastener,group,.0015)
    if side==1:
        cyl('Lock nickel bezel',(hx,-.324,.897),.014,.007,nickel,(0,-1,0),group)
        cyl('Lock cylinder face',(hx,-.329,.897),.0105,.003,fastener,(0,-1,0),group)
        cyl('Lock keyway round top',(hx,-.331,.899),.0024,.0007,black,(0,-1,0),group,n=16,bev=0)
        box('Lock keyway slot',(hx,-.3315,.894),(.0028,.0008,.008),black,group,.0003)
        box('Interior lock casing',(hx,-.273,.897),(.044,.034,.068),fastener,group,.002)
        box('Interior locking tongue',(.018,-.267,.897),(.073,.012,.018),nickel,group,.001)
    for z in [.282,1.00,1.69]:
        # Three hinges per door, separated barrel knuckles and cap pins.
        box('Door '+label+' hinge leaf',(side*.486,-.323,z),(.023,.007,.065),folds,group,.001)
        box('Fixed hinge leaf',(side*.510,-.322,z),(.018,.007,.065),folds,'04_Hinges',.001)
        for dz in [-.022,0,.022]:cyl('Hinge knuckle',(side*.5025,-.326,z+dz),.006,.021,folds,(0,0,1),'04_Hinges',n=24)
        for dz in [-.034,.034]:cyl('Hinge pin cap',(side*.5025,-.326,z+dz),.005,.002,nickel,(0,0,1),'04_Hinges',n=20)
    # Parent the complete leaf, grip, hems and lock to a side hinge pivot.
    pivot=bpy.data.objects.new('Door_'+label+'_Pivot',None);groups[group].objects.link(pivot)
    pivot.parent=root;pivot.location=(side*.5025,-.326,.963);pivot.empty_display_size=.025
    pivot['open_rotation_z_degrees']=-95 if side<0 else 95
    for o in list(groups[group].objects):
        if o==pivot:continue
        o.parent=pivot;o.location-=pivot.location
    door_pivots.append(pivot)

# Back face: narrow perimeter hems, broad center reinforcing strap, exposed screws.
for x in [-.498,.498]:box('Rear outer reinforcing upright',(x,.314,.961),(.037,.018,1.796),folds,'05_Rear_Reinforcement',.001)
box('Rear central reinforcing upright',(0,.315,.965),(.054,.019,1.766),folds,'05_Rear_Reinforcement',.0012)
for z in [.099,1.828]:box('Rear horizontal rail',(0,.316,z),(1.018,.019,.039),folds,'05_Rear_Reinforcement',.001)
for x in [-.479,-.034,.034,.479]:
    for z in [.165,.632,1.202,1.713]:screw(x,.327,z,r=.0035)
for x in [-.50,.50]:
    for z in [.035,1.881]:screw(x,.327,z,r=.0035)

# Underside frame and four square corner plates with recessed center fasteners.
for x in [-.467,.467]:box('Underside side rail',(x,0,.017),(.041,.555,.019),folds,'06_Base_Hardware',.001)
for y in [-.26,.26]:box('Underside transverse rail',(0,y,.017),(.952,.034,.019),folds,'06_Base_Hardware',.001)
box('Underside inset panel',(0,0,.030),(.903,.484,.016),inside,'06_Base_Hardware',.001)
for x in [-.478,.478]:
    for y in [-.268,.268]:
        box('Square corner foot plate',(x,y,.014),(.088,.085,.024),folds,'06_Base_Hardware',.004)
        box('Thin floor protection pad',(x,y,.0015),(.070,.068,.003),rubber,'06_Base_Hardware',.002)
        cyl('Recessed foot mounting hole',(x,y,.001),.007,.001,black,(0,0,-1),'06_Base_Hardware',n=24,bev=0)
        for dx,dy in [(-.028,-.026),(.028,.026)]:screw(x+dx,y+dy,.001,(0,0,-1),'06_Base_Hardware',r=.0026)
for x in [-.411,.411]:
    for y in [-.213,.213]:screw(x,y,.019,(0,0,-1),'06_Base_Hardware',r=.0045)

bpy.context.view_layer.update()
# UV layers for all components, including Boolean handle geometry.
for o in root.children_recursive:
    if o.type!='MESH':continue
    if o.name.startswith('Door_') or not o.data.uv_layers:
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
im=bpy.data.images.load(REF,check_existing=True);im.name='REFERENCE | six-view steel cabinet';im.use_fake_user=True;im.pack()
readme=bpy.data.texts.new('ASSET_README')
readme.write('Steel storage cabinet modeled from the user-provided six-view reference.\nUnits meters, front -Y, up +Z, root at floor center.\nApproximate reference proportions; dimensions are not supplied by the drawing.\nDoor_Left_Pivot and Door_Right_Pivot rotate around their local Z axes; their handles and lock follow them.\nClosed state is saved. Hinges, rear reinforcement and underside pads are modeled.\nInterior is a hollow shell; no undocumented shelf arrangement was added.\nReference is packed. Blender powder-coat material nodes require separate Unity material setup. No gameplay animation is included.\n')
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            s=a.spaces.active;s.region_3d.view_distance=3.5;s.region_3d.view_location=(0,0,.95)
            s.region_3d.view_rotation=Vector((3,-6,2)).to_track_quat('Z','Y');s.shading.type='MATERIAL';s.overlay.show_extras=False
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
# Save a clean asset before adding the temporary render rig.
bpy.ops.wm.save_as_mainfile(filepath=OUT)
meshes=[o for o in root.children_recursive if o.type=='MESH']
coords=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
report={'file':OUT,'mesh_objects':len(meshes),'base_polygons':sum(len(o.data.polygons) for o in meshes),'dimensions_m':[round(max(v[i] for v in coords)-min(v[i] for v in coords),3) for i in range(3)],'packed_reference':bool(im.packed_file)}
with open(os.path.join(ART,'steel_storage_cabinet_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print(json.dumps(report),flush=True)

# Render-only objects are added after saving and never written to the asset.
studio=bpy.data.collections.new('TEMP_Studio');scene.collection.children.link(studio)
def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',cd);studio.objects.link(cam);scene.camera=cam
cd.type='ORTHO';cd.ortho_scale=2.28
cam.location=(3,-5,2.8);aim(cam,(0,0,.95))
for name,pos,energy,size,color in [('Key',(-3,-4,4),430,3,(1,.96,.9)),('Fill',(3,-2,3),280,3,(.9,.94,1)),('Rim',(1,3,4),440,2,(1,1,1))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=pos;aim(o,(0,0,1))
floor=box('TEMP studio floor',(0,0,-.014),(200,200,.02),material('TEMP floor',(.16,.175,.19),0,.85))
floor.parent=None
for c in list(floor.users_collection):c.objects.unlink(floor)
studio.objects.link(floor)
scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.19,.21,.24,1);bg.inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=950;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(ART,'steel_storage_cabinet_preview.png')
print('Rendering cabinet front',flush=True);bpy.ops.render.render(write_still=True)
cam.location=(-3,5,2.7);aim(cam,(0,0,.95))
scene.render.filepath=os.path.join(ART,'steel_storage_cabinet_rear.png')
print('Rendering cabinet rear',flush=True);bpy.ops.render.render(write_still=True)
print('DONE',OUT,flush=True)
