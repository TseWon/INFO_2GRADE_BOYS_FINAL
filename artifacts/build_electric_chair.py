import bpy, math, random, os
from mathutils import Vector
random.seed(41)
BASE = r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL'
OUT = os.path.join(BASE, 'Assets', 'Blender', 'electric_chair.blend')
# The previous unsaved modeling exercise was saved before starting this asset.
for ob in list(bpy.data.objects):
    bpy.data.objects.remove(ob, do_unlink=True)
scene = bpy.context.scene
scene.name = 'ElectricChair_Asset'
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
groups = {}
for name in ['01_Wood_Frame','02_Metal_Hardware','03_Leather_Restraints','04_Cables','05_Head_Assembly','06_Wear']:
    c = bpy.data.collections.new(name)
    scene.collection.children.link(c)
    groups[name] = c
root = bpy.data.objects.new('ElectricChair', None)
scene.collection.objects.link(root)
root['reference'] = 'User supplied front/back/left/right/top/bottom concept sheet'
root['units'] = 'Meters; dimensions estimated from image, not engineering measurements'
root['front_direction'] = '-Y; up +Z; pivot at floor'

def mat(name, color, metallic=0, rough=.6, grain=False):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    n = m.node_tree.nodes; l = m.node_tree.links
    p = next(x for x in n if x.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metallic
    p.inputs['Roughness'].default_value=rough
    coord=n.new('ShaderNodeTexCoord')
    mapping=n.new('ShaderNodeVectorMath'); mapping.operation='MULTIPLY'
    mapping.inputs[1].default_value=(26,26,2.0) if grain else (5,5,5)
    l.new(coord.outputs['Generated'],mapping.inputs[0])
    noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=3.7
    noise.inputs['Detail'].default_value=4.0; noise.inputs['Roughness'].default_value=.72
    l.new(mapping.outputs['Vector'],noise.inputs['Vector'])
    ramp=n.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.19
    ramp.color_ramp.elements[0].color=(*[x*.32 for x in color],1)
    ramp.color_ramp.elements[1].position=.83
    ramp.color_ramp.elements[1].color=(*[min(x*1.85,1) for x in color],1)
    l.new(noise.outputs['Fac'],ramp.inputs[0]); l.new(ramp.outputs[0],p.inputs['Base Color'])
    fine=n.new('ShaderNodeTexNoise'); fine.inputs['Scale'].default_value=150
    fine.inputs['Detail'].default_value=2
    l.new(mapping.outputs['Vector'],fine.inputs['Vector'])
    bump=n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.25 if grain else .18
    bump.inputs['Distance'].default_value=.0025 if grain else .001
    l.new(fine.outputs['Fac'],bump.inputs['Height']); l.new(bump.outputs[0],p.inputs['Normal'])
    return m

wood=mat('Oak | aged dark heartwood',(.19,.083,.032),grain=True)
wood2=mat('Oak | sun worn boards',(.26,.12,.052),grain=True)
edge=mat('Oak | exposed end grain',(.32,.17,.08),rough=.79)
steel=mat('Steel | oxidized warm grey',(.29,.28,.23),.78,.43)
iron=mat('Iron | dark patina',(.075,.067,.052),.72,.57)
rust=mat('Oxide | brown corrosion',(.19,.068,.025),.35,.82)
leather=mat('Leather | cracked saddle brown',(.11,.043,.016),0,.72)
leatheredge=mat('Leather | raw edge',(.24,.11,.045),0,.82)
rubber=mat('Cable | black aged insulation',(.018,.022,.018),.05,.65)
ceramic=mat('Insulator | ivory ceramic',(.48,.45,.34),.1,.28)

def place(o,name,material,group):
    o.name=name
    for c in list(o.users_collection): c.objects.unlink(o)
    groups[group].objects.link(o)
    o.parent=root
    if material: o.data.materials.append(material)
    return o

def box(name,loc,size,material=wood,group='01_Wood_Frame',bevel=.006,grain_axis=None):
    # Keep local Z aligned to the board length so procedural grain follows the timber.
    dims=tuple(size); rot=(0,0,0)
    if grain_axis=='X': dims=(size[2],size[1],size[0]);rot=(0,math.pi/2,0)
    if grain_axis=='Y': dims=(size[0],size[2],size[1]);rot=(math.pi/2,0,0)
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=place(bpy.context.object,name,material,group)
    for v in o.data.vertices:
        v.co.x*=dims[0];v.co.y*=dims[1];v.co.z*=dims[2]
    o.rotation_euler=rot
    if bevel:
        b=o.modifiers.new('Soft worn edges','BEVEL');b.width=bevel;b.segments=2
    return o

def cyl(name,loc,r,depth,material=steel,axis=(0,0,1),vertices=16,group='02_Metal_Hardware'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=depth,location=loc)
    o=place(bpy.context.object,name,material,group)
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    b=o.modifiers.new('Rounded machined edge','BEVEL');b.width=min(.0018,r*.15);b.segments=2
    for p in o.data.polygons: p.use_smooth=len(p.vertices)==4
    return o

def cord(name,points,r=.008,material=rubber,group='04_Cables',smooth=True):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('BEZIER' if smooth else 'POLY')
    if smooth:
        sp.bezier_points.add(len(points)-1)
        for p,co in zip(sp.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    else:
        sp.points.add(len(points)-1)
        for p,co in zip(sp.points,points):p.co=(*co,1)
    ob=bpy.data.objects.new(name,cu);groups[group].objects.link(ob);ob.parent=root;cu.materials.append(material)
    return ob

def strip(name,points,width,material=leather,group='03_Leather_Restraints',across=(1,0,0)):
    a=Vector(across)*width*.5;verts=[]
    for p in points:
        v=Vector(p);verts.extend([tuple(v-a),tuple(v+a)])
    faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)]
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);groups[group].objects.link(ob);ob.parent=root;me.materials.append(material)
    s=ob.modifiers.new('Leather thickness','SOLIDIFY');s.thickness=.006
    b=ob.modifiers.new('Soft leather edges','BEVEL');b.width=.0014;b.segments=2
    return ob

def bolt(loc,axis=(0,-1,0),r=.009):
    cyl('Iron washer',loc,r*1.42,.0025,iron,axis,20)
    pt=Vector(loc)+Vector(axis)*.003
    cyl('Forged hex bolt',pt,r,.006,steel,axis,6)

# Heavy timber frame; front is -Y.
for x in [-.405,.405]:
    box('Tall rear upright',(x,.32,.91),(.105,.115,1.78),wood)
    box('Front leg',(x,-.34,.49),(.115,.12,.94),wood2)
    box('Armrest',(x,-.035,1.015),(.16,.84,.075),wood2,bevel=.009,grain_axis='Y')
    box('Side seat bearer',(x,-.015,.60),(.10,.79,.11),wood,grain_axis='Y')
    box('Low side stretcher',(x,-.015,.205),(.065,.70,.067),wood,grain_axis='Y')
    for y in [-.34,.32]:
        box('Metal foot shoe',(x,y,.043),(.133,.145,.075),iron,'02_Metal_Hardware',.004)
        box('Foot sole',(x,y,.009),(.146,.155,.018),steel,'02_Metal_Hardware',.002)
        for dx in [-.044,.044]:bolt((x+dx,y-.076,.048),r=.006)
    for y,z in [(-.405,.57),(-.405,.91),(.258,.62),(.258,1.69)]:bolt((x,y,z))
    for z in [.22,.58,1.02,1.65]:bolt((x+(.059 if x>0 else -.059),.32,z),axis=(1 if x>0 else -1,0,0))
for y in [-.34,.32]:
    box('Seat apron',(0,y,.60),(.80,.095,.11),wood2,grain_axis='X')
    box('Lower cross brace',(0,y,.205),(.78,.065,.065),wood,grain_axis='X')
for i in range(7):
    x=-.315+i*.105
    box('Individual seat plank %02d'%i,(x,-.01,.679),(.100,.69,.052),wood2 if i%3 else wood,bevel=.003,grain_axis='Y')
    for y in [-.29,.265]:cyl('Seat nail',(x,y,.707),.004,.002,iron,vertices=10)
for x in [-.25,-.085,.085,.25]:
    box('Vertical back slat',(x,.326,1.23),(.105,.045,1.02),wood2 if x<0 else wood,bevel=.003)
for z in [.82,1.67]:
    box('Back horizontal rail',(0,.36,z),(.79,.085,.105),wood2,grain_axis='X')
    for x in [-.3,-.1,.1,.3]:bolt((x,.407,z),axis=(0,1,0),r=.007)
box('Top crown timber',(0,.315,1.755),(.79,.105,.08),wood,grain_axis='X')

# Riveted back electrode with leather mounting pad.
box('Back electrode leather backing',(0,.275,1.32),(.365,.035,.45),leather,'03_Leather_Restraints',.016)
box('Back electrode dark border',(0,.248,1.32),(.335,.019,.395),iron,'02_Metal_Hardware',.004)
box('Back electrode face',(0,.234,1.32),(.295,.012,.355),steel,'02_Metal_Hardware',.003)
for z in [1.16,1.225,1.29,1.355,1.42,1.48]:
    for x in [-.137,.137]:bolt((x,.225,z),r=.0048)
for x in [-.08,0,.08]:
    for z in [1.16,1.48]:bolt((x,.225,z),r=.0048)
box('Seat plate border',(0,-.05,.709),(.35,.27,.012),iron,'02_Metal_Hardware',.003)
box('Seat contact plate',(0,-.05,.718),(.30,.225,.008),steel,'02_Metal_Hardware',.003)
for x in [-.133,.133]:
    for y in [-.142,.042]:bolt((x,y,.724),axis=(0,0,1),r=.005)

def buckle(x,y,z,w=.10,d=.065):
    for dx in [-w/2,w/2]:box('Buckle side',(x+dx,y,z),(.008,d,.012),steel,'02_Metal_Hardware',.002)
    for dy in [-d/2,d/2]:box('Buckle bridge',(x,y+dy,z),(w,.009,.012),steel,'02_Metal_Hardware',.002)
    box('Buckle tongue',(x,y,z+.007),(.007,d*.85,.005),iron,'02_Metal_Hardware',.001)

for x in [-.405,.405]:
    # Band wraps visibly over each wrist rest, with a loose hanging leather tail.
    strip('Wrist restraint wrap',[(x-.084,-.22,.99),(x-.086,-.22,1.065),(x-.04,-.22,1.085),(x+.05,-.22,1.085),(x+.09,-.22,1.05),(x+.09,-.22,.92),(x+.075,-.22,.87)],.067,across=(0,1,0))
    buckle(x,-.22,1.096,.10,.073)
    box('Arm leather pad',(x,.03,1.058),(.115,.25,.018),leather,'03_Leather_Restraints',.007,grain_axis='Y')
    for y in [-.07,.13]:
        for dx in [-.045,.045]:cyl('Pad rivet',(x+dx,y,1.07),.004,.004,steel,vertices=12)
    strip('Long torso strap',[(x*.60,.28,1.54),(x*.67,.19,1.38),(x*.82,.11,1.08),(x*.78,.08,.86),(x*.65,.0,.73)],.05)
    for z in [.92,.96,1.0]:
        cyl('Strap punched hole',(x+.094,-.22,z),.003,.001,iron,axis=(1,0,0),vertices=10)
strip('Hanging ankle restraint',[(-.24,-.23,.72),(-.24,-.39,.71),(-.24,-.435,.59),(-.24,-.45,.34),(-.22,-.46,.10)],.053)
for z in [.16,.205,.25,.295,.34,.385]:cyl('Ankle strap holes',(-.229,-.465,z),.003,.002,iron,axis=(0,-1,0),vertices=10)
box('Ankle buckle top',(-.235,-.469,.43),(.069,.008,.008),steel,'02_Metal_Hardware',.001)
box('Ankle buckle bottom',(-.235,-.469,.385),(.069,.008,.008),steel,'02_Metal_Hardware',.001)
for x in [-.269,-.201]:box('Ankle buckle side',(x,-.469,.407),(.007,.009,.052),steel,'02_Metal_Hardware',.001)

# Shaped hollow helmet: revolved double surface, with an open lower rim.
profile=[(.177,1.755),(.172,1.782),(.157,1.803),(.144,1.84),(.121,1.878),(.083,1.91),(.046,1.928),(.033,1.934),(.033,1.944),(.052,1.940),(.089,1.923),(.129,1.89),(.154,1.851),(.167,1.811),(.185,1.788),(.190,1.755)]
verts=[];faces=[];N=64;hy=.015
for r,z in profile:
    for i in range(N):
        a=2*math.pi*i/N;verts.append((r*math.cos(a),hy+r*math.sin(a),z))
for j in range(len(profile)):
    j2=(j+1)%len(profile)
    for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,j2*N+(i+1)%N,j2*N+i))
me=bpy.data.meshes.new('Hollow spun helmet');me.from_pydata(verts,[],faces);me.update()
ob=bpy.data.objects.new('Head electrode | open metal dome',me);groups['05_Head_Assembly'].objects.link(ob);ob.parent=root;me.materials.append(steel)
for p in me.polygons:p.use_smooth=True
for i in range(16):
    a=2*math.pi*i/16
    cyl('Helmet rim rivet',(.19*math.cos(a),hy+.19*math.sin(a),1.772),.006,.006,iron,axis=(math.cos(a),math.sin(a),0),vertices=12,group='05_Head_Assembly')
for z,r in [(1.808,.163),(1.923,.082)]:
    cord('Helmet turned seam',[(r*math.cos(i*2*math.pi/64),hy+r*math.sin(i*2*math.pi/64),z) for i in range(65)],.0027,iron,'05_Head_Assembly',False)
cyl('Helmet ceramic terminal',(0,hy,1.958),.024,.042,ceramic,vertices=24,group='05_Head_Assembly')
cyl('Terminal contact',(0,hy,1.984),.014,.018,iron,vertices=16,group='05_Head_Assembly')
strip('Arched leather helmet suspension',[(0,.39,1.78),(0,.38,1.95),(0,.33,2.10),(0,.23,2.165),(0,.14,2.14),(0,.06,2.04),(0,.015,1.98)],.052,leather,'05_Head_Assembly')
box('Suspension bracket',(0,.38,1.85),(.097,.05,.13),iron,'05_Head_Assembly',.006)
for z in [1.81,1.88]:bolt((0,.346,z),r=.013)

# Coiled terminal lead and asymmetrical routed conductors.
cord('Helmet lead',[(0,.04,1.98),(.10,.09,1.99),(.23,.23,1.94),(.25,.34,1.85),(.32,.39,1.76)],.009)
coil=[]
for i in range(241):
    t=i/240;a=t*2*math.pi*24
    coil.append((.035+.26*t,.36+.013*math.cos(a),1.91+.013*math.sin(a)))
cord('Coiled protective spring',coil,.0028,steel,'04_Cables',False)
for side in [-1,1]:
    x=side*.405
    cord('Upright electrical cable',[(side*.29,.37,1.91),(x+.025,.39,1.79),(x+.063,.34,1.66),(x+.065,.29,1.2),(x+.065,.24,.76),(x+.062,-.34,.55),(x+.055,-.405,.21)],.008)
    for z in [.76,1.2,1.61]:
        box('Cable clamp',(x+.062,.285,z),(.024,.028,.018),steel,'02_Metal_Hardware',.002)
    cord('Arm cable',[(side*.16,.29,1.36),(side*.30,.20,1.10),(x,.11,1.08),(x,-.13,1.08)],.0055,leatheredge)
cord('Rear cross cable',[(-.46,.40,1.74),(-.21,.43,1.77),(.06,.42,1.72),(.30,.44,1.75),(.46,.40,1.72)],.006)
cord('Rear second cable',[(-.46,.415,1.71),(-.14,.45,1.74),(.12,.44,1.76),(.45,.41,1.70)],.004,leatheredge)

# Small worn streaks on exposed timber; color variation and grain come from shaders.
for side in [-1,1]:
    x=side*.405
    for k in range(30):
        z=random.uniform(.18,1.73);xx=x+random.uniform(-.04,.04)
        length=random.uniform(.012,.068)
        cord('Worn wood scratch',[(xx,.260,z),(xx+random.uniform(-.003,.003),.260,z+length)],random.uniform(.00035,.0008),edge,'06_Wear',False)
    for k in range(12):
        z=random.uniform(.14,.92);xx=x+random.uniform(-.046,.046)
        cord('Front leg wear',[(xx,-.401,z),(xx+.001,-.401,z+.025)],.00065,edge,'06_Wear',False)

ref = r'C:\Users\PLUS\AppData\Local\Temp\codex-clipboard-3b06b7bc-5848-4e5a-99ad-4ec44e554927.png'
im=bpy.data.images.load(ref,check_existing=True);im.name='REFERENCE | six-view chair concept';im.use_fake_user=True;im.pack()
scene.world.color=(.18,.18,.18)
for ob in bpy.context.selected_objects:ob.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=3.9
            area.spaces.active.region_3d.view_location=(0,0,1.05)
            area.spaces.active.region_3d.view_rotation=Vector((3,-5,2.5)).to_track_quat('Z','Y')
            area.spaces.active.shading.type='MATERIAL'
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=OUT)
print('SAVED',OUT)
print('Objects:',len(root.children),'Mesh polygons:',sum(len(o.data.polygons) for o in root.children if o.type=='MESH'))
