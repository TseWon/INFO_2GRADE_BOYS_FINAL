"""Build a six-view reference-inspired elevator asset and studio previews.
Run with Blender --background --factory-startup --python this_file.py.
"""
import bpy, math, os, json
from mathutils import Vector

BASE=r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(BASE,'Assets','Blender','industrial_elevator.blend')
REF=r'C:\Users\PLUS\AppData\Local\Temp\codex-clipboard-22196979-d17c-4768-82c5-e861a2ee6b99.png'
ART=os.path.join(BASE,'artifacts')
assert not os.path.exists(OUT), 'Do not overwrite an existing asset'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.name='Industrial_Elevator_Asset'
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
root=bpy.data.objects.new('IndustrialElevator',None)
scene.collection.objects.link(root)
root.empty_display_size=.12
root['reference']='Six-view elevator reference supplied by user'
root['dimensions_note']='Approximate image proportions; not an engineering assembly'
root['orientation']='Front -Y, up +Z, origin at floor center. Units: meters.'
root['door_setup']='Door_Left and Door_Right are separate mesh objects for later sliding animation.'
groups={}
for name in ['01_Structural_Frame','02_Cabin_Panels','03_Door_Assembly','04_Guide_Rails','05_Traction_Machine','06_Cables_Conduits','07_Fasteners','08_Controls']:
    c=bpy.data.collections.new(name);scene.collection.children.link(c);groups[name]=c

def material(name,color,metal=.8,rough=.34,brushed=False):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    n=m.node_tree.nodes;l=m.node_tree.links
    p=next(x for x in n if x.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if brushed:
        tc=n.new('ShaderNodeTexCoord');v=n.new('ShaderNodeVectorMath');v.operation='MULTIPLY'
        v.inputs[1].default_value=(150,150,1.2)
        l.new(tc.outputs['Generated'],v.inputs[0])
        noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=2.7;noise.inputs['Detail'].default_value=2
        l.new(v.outputs[0],noise.inputs['Vector'])
        ramp=n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].color=(*[v*.83 for v in color],1)
        ramp.color_ramp.elements[1].color=(*[min(v*1.1,1) for v in color],1)
        l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],p.inputs['Base Color'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.09;bump.inputs['Distance'].default_value=.00018
        l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
    return m

steel=material('Stainless | brushed door skin',(.43,.45,.47),.83,.32,True)
frame=material('Galvanized | frame members',(.28,.30,.31),.78,.40,True)
panel=material('Cabin | satin grey panels',(.29,.315,.34),.65,.43,True)
edge=material('Steel | polished guide face',(.48,.51,.53),.91,.23)
dark=material('Steel | blackened mechanism',(.055,.065,.074),.75,.36)
rubber=material('Rubber | seals and cable',(.009,.012,.014),.02,.7)
blue=material('Motor | midnight blue enamel',(.028,.058,.083),.58,.35)
blueedge=material('Motor | cooling fin highlights',(.044,.083,.11),.65,.32)
yellow=material('Sheave | safety ochre enamel',(.52,.33,.025),.58,.34)
brass=material('Hardware | warm zinc plating',(.37,.30,.15),.78,.36)
glass=material('Control | smoked display glass',(.004,.007,.009),.24,.18)
red=material('Control | red LED',(.5,.008,.008),.1,.32)
rp=next(n for n in red.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
rp.inputs['Emission Color'].default_value=(1,.005,.003,1);rp.inputs['Emission Strength'].default_value=2.5

def assign(o,name,mat,group):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    groups[group].objects.link(o);o.parent=root
    if mat:o.data.materials.append(mat)
    return o

def bevel(o,w=.003,segments=2):
    b=o.modifiers.new('Machined edge bevel','BEVEL');b.width=w;b.segments=segments
    return o

def box(name,loc,dim,mat=frame,group='01_Structural_Frame',bev=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=assign(bpy.context.object,name,mat,group)
    for v in o.data.vertices:v.co.x*=dim[0];v.co.y*=dim[1];v.co.z*=dim[2]
    if bev:bevel(o,bev)
    return o

def cyl(name,loc,r,depth,mat=frame,axis=(0,0,1),n=24,group='07_Fasteners',bev=.001):
    bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=depth,location=loc)
    o=assign(bpy.context.object,name,mat,group)
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    if bev:bevel(o,bev)
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o

def bolt(loc,axis=(0,-1,0),r=.009):
    p=Vector(loc);a=Vector(axis)
    cyl('Bolt washer',p,r*1.45,.0025,dark,axis,20,bev=.0004)
    cyl('Hex head M12',p+a*.004,r,.007,edge,axis,6,bev=.0007)

def tube(name,points,r=.008,mat=rubber,group='06_Cables_Conduits',smooth=False):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('BEZIER' if smooth else 'POLY')
    if smooth:
        sp.bezier_points.add(len(points)-1)
        for p,co in zip(sp.bezier_points,points):p.co=co;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    else:
        sp.points.add(len(points)-1)
        for p,co in zip(sp.points,points):p.co=(*co,1)
    o=bpy.data.objects.new(name,cu);groups[group].objects.link(o);o.parent=root;cu.materials.append(mat)
    return o

def beam(name,p1,p2,width,depth,mat=frame,group='01_Structural_Frame'):
    a=Vector(p1);b=Vector(p2)
    o=box(name,(a+b)/2,(width,depth,(b-a).length),mat,group)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o

def ring(name,center,outer,inner,depth,mat,axis=(0,1,0),group='05_Traction_Machine',n=64):
    verts=[];faces=[]
    for z,r in [(-depth/2,outer),(depth/2,outer),(-depth/2,inner),(depth/2,inner)]:
        for i in range(n):
            a=2*math.pi*i/n;verts.append((r*math.cos(a),r*math.sin(a),z))
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(2*n+j,2*n+i,3*n+i,3*n+j),(j,i,2*n+i,2*n+j),(n+i,n+j,3*n+j,3*n+i)])
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(name,me);groups[group].objects.link(o);o.parent=root;o.location=center
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();me.materials.append(mat)
    for p in me.polygons:p.use_smooth=True
    bevel(o,.0015)
    return o

# Open external skeleton, with four tall channels and bolted splice plates.
for x in [-.835,.835]:
    for y in [-.66,.66]:
        box('Channel web vertical',(x,y,1.77),(.11,.034,3.5))
        for dx in [-.05,.05]:box('Channel return flange',(x+dx,y+.025,1.77),(.014,.085,3.5),dark)
        box('Foot mounting pad',(x,y,.035),(.16,.20,.07),frame,bev=.004)
        for z in [.16,1.10,2.38,3.27]:
            yf=y+(-.035 if y<0 else .065)
            box('Upright splice plate',(x,yf,z),(.132,.018,.17),frame)
            for dx in [-.04,.04]:
                for dz in [-.055,.055]:bolt((x+dx,yf+(-.011 if y<0 else .011),z+dz),(0,-1 if y<0 else 1,0),.007)
        for z in [.06,3.47]:
            for dx in [-.033,.033]:bolt((x+dx,y-.026,z),r=.007)
for y in [-.65,.65]:
    for z in [.12,3.20]:
        box('Frame crosshead',(0,y,z),(1.74,.12,.11))
        box('Crosshead upper lip',(0,y-.065,z+.047),(1.73,.025,.025),edge)
for x in [-.79,.79]:
    for z in [.13,3.19]:box('Longitudinal carrier',(x,0,z),(.10,1.40,.10))

# Cab enclosure: separate wall skins, reinforcing hems, roof, and floor.
box('Cabin floor',(0,.025,.23),(1.48,1.34,.09),panel,'02_Cabin_Panels')
box('Cabin roof',(0,.025,3.075),(1.48,1.34,.055),panel,'02_Cabin_Panels')
for x in [-.721,.721]:
    box('Cabin side skin',(x,.025,1.66),(.028,1.34,2.79),panel,'02_Cabin_Panels',.002)
    for y in [-.60,.65]:box('Side folded edge',(x,y,1.66),(.047,.024,2.81),steel,'02_Cabin_Panels',.002)
    for z in [.28,3.025]:box('Side skin hem',(x,.025,z),(.05,1.32,.035),steel,'02_Cabin_Panels',.002)
    for y in [-.48,.51]:
        for z in [.30,3.01]:bolt((x+(.02 if x>0 else -.02),y,z),(1 if x>0 else -1,0,0),.005)
for x in [-.36,.36]:box('Rear split panel',(x,.706,1.66),(.713,.032,2.79),panel,'02_Cabin_Panels',.002)
for x in [-.68,0,.68]:box('Rear vertical hat stiffener',(x,.737,1.65),(.034,.045,2.80),frame,'02_Cabin_Panels')
for z in [.32,1.02,2.16,3.0]:
    box('Rear horizontal reinforcing rib',(0,.744,z),(1.42,.047,.042),frame,'02_Cabin_Panels',.002)
    for x in [-.64,-.34,.34,.64]:bolt((x,.773,z),(0,1,0),.006)

# Brushed front portal and independently named sliding leaves.
for x in [-.672,.672]:
    box('Portal vertical jamb',(x,-.730,1.65),(.19,.115,2.86),steel,'03_Door_Assembly',.005)
    box('Doorway inner reveal',(x*.855,-.716,1.49),(.030,.11,2.54),dark,'03_Door_Assembly',.001)
    for z in [.29,3.015]:bolt((x,-.790,z),r=.005)
box('Portal lintel',(0,-.729,2.93),(1.53,.116,.30),steel,'03_Door_Assembly',.005)
box('Door header lower reveal',(0,-.747,2.755),(1.14,.08,.027),dark,'03_Door_Assembly',.001)
box('Door threshold',(0,-.755,.232),(1.53,.19,.038),edge,'03_Door_Assembly',.003)
for y in [-.81,-.77,-.73]:box('Threshold guide groove',(0,y,.252),(1.14,.005,.002),dark,'03_Door_Assembly',0)
for side,name in [(-1,'Door_Left'),(1,'Door_Right')]:
    ob=box(name,(side*.282,-.719,1.50),(.556,.052,2.47),steel,'03_Door_Assembly',.002)
    ob['slide_axis']='local X';ob['open_offset_m']=side*.55
    strip=box(name+' inner seal',(side*.004,-.724,1.50),(.007,.054,2.462),rubber,'03_Door_Assembly',.0007)
    strip.parent=ob;strip.matrix_parent_inverse=ob.matrix_world.inverted()
    # Hanger brackets and rollers above the cab, partially exposed behind the lintel.
    for dx in [-.18,.18]:
        x=side*.282+dx
        box('Door hanger',(x,-.625,3.08),(.07,.035,.20),frame,'03_Door_Assembly')
        cyl('Door suspension roller',(x,-.652,3.14),.031,.022,dark,(0,1,0),32,'03_Door_Assembly')
        bolt((x,-.669,3.14),r=.008)
box('Door running track',(0,-.62,3.14),(1.56,.04,.04),edge,'03_Door_Assembly')

# Two rear T guide rails, central guide, guide shoes, and adjustment brackets.
for x in [-.76,.76]:
    box('Rear guide rail foot',(x,.828,1.76),(.11,.025,3.45),dark,'04_Guide_Rails')
    box('Rear guide rail web',(x,.859,1.76),(.018,.075,3.45),edge,'04_Guide_Rails',.001)
    for z in [.42,1.14,2.38,3.08]:
        box('Guide mounting cross plate',(x,.85,z),(.25,.035,.09),frame,'04_Guide_Rails')
        for dx in [-.095,.095]:bolt((x+dx,.873,z),(0,1,0),.008)
    for z in [.46,2.90]:
        box('Guide shoe housing',(x,.91,z),(.10,.11,.18),dark,'04_Guide_Rails',.005)
        box('Guide shoe cover',(x,.976,z),(.12,.025,.13),frame,'04_Guide_Rails')
        for dz in [-.041,.041]:bolt((x,.992,z+dz),(0,1,0),.007)
        cyl('Guide shoe roller',(x-.067,.899,z),.036,.027,brass,(1,0,0),24,'04_Guide_Rails')
box('Rear center rail',(0,.848,1.73),(.065,.07,3.27),dark,'04_Guide_Rails')
box('Rear center polished strip',(0,.89,1.73),(.017,.012,3.24),edge,'04_Guide_Rails',.001)
for z in [.35,1.23,2.3,3.12]:
    box('Center slider',(0,.91,z),(.11,.045,.11),frame,'04_Guide_Rails')
    for dx in [-.035,.035]:bolt((dx,.939,z),(0,1,0),.006)
for z in [.27,3.10]:
    cyl('Rear transverse axle',(0,.82,z),.025,1.52,edge,(1,0,0),32,'04_Guide_Rails')
    for x in [-.61,.61]:cyl('Rear axle collar',(x,.82,z),.046,.08,dark,(1,0,0),24,'04_Guide_Rails')

# Upper machine bed, motor, gearbox, bearing blocks and spoke traction sheave.
for y in [-.28,.37]:
    box('Machine bed crossmember',(0,y,3.275),(1.57,.12,.10),dark,'05_Traction_Machine')
    box('Machine bed flange',(0,y,3.329),(1.57,.17,.018),frame,'05_Traction_Machine')
    for x in [-.64,.64]:bolt((x,y,3.344),(0,0,1),.012)
for x in [-.38,.27]:
    box('Motor mounting foot',(x,.08,3.36),(.18,.59,.045),blue,'05_Traction_Machine')
    for y in [-.16,.30]:bolt((x,y,3.389),(0,0,1),.010)
box('Gearbox cast housing',(-.31,.12,3.60),(.39,.43,.43),blue,'05_Traction_Machine',.025)
box('Gearbox inspection cover',(-.31,.13,3.825),(.32,.34,.025),blueedge,'05_Traction_Machine',.007)
for x in [-.43,-.19]:
    for y in [.0,.26]:bolt((x,y,3.845),(0,0,1),.008)
cyl('Motor main barrel',(.23,.10,3.575),.19,.53,blue,(0,1,0),64,'05_Traction_Machine',.008)
for y in [-.17,.37]:
    cyl('Motor end bell',(.23,y,3.575),.203,.045,blueedge,(0,1,0),64,'05_Traction_Machine',.006)
    for i in range(8):
        a=i*math.tau/8
        bolt((.23+.166*math.cos(a),y+(-.026 if y<0 else .026),3.575+.166*math.sin(a)),(0,-1 if y<0 else 1,0),.006)
# Longitudinal cooling fins follow the motor's cylindrical surface.
for i in range(28):
    a=i*math.tau/28
    o=box('Motor longitudinal cooling fin',(.23+.195*math.cos(a),.10,3.575+.195*math.sin(a)),(.023,.48,.012),blueedge,'05_Traction_Machine',.001)
    o.rotation_euler[1]=-a
box('Motor terminal box',(.23,.10,3.791),(.22,.20,.095),blue,'05_Traction_Machine',.008)
box('Motor terminal lid',(.23,.10,3.843),(.235,.215,.014),dark,'05_Traction_Machine')
for x in [.14,.32]:
    for y in [.025,.175]:bolt((x,y,3.855),(0,0,1),.005)
cyl('Fan grille cap',(.23,.414,3.575),.183,.019,dark,(0,1,0),64,'05_Traction_Machine')
for r in [.055,.09,.125,.16]:ring('Fan grille circular rib',(.23,.427,3.575),r,r-.006,.008,blueedge)
for i in range(12):
    a=i*math.tau/12
    beam('Fan grille radial rib',(.23+.025*math.cos(a),.43,3.575+.025*math.sin(a)),(.23+.173*math.cos(a),.43,3.575+.173*math.sin(a)),.006,.007,blueedge,'05_Traction_Machine')
cyl('Drive output shaft',(-.31,-.23,3.60),.042,.46,edge,(0,1,0),32,'05_Traction_Machine')
wx,wz=-.31,3.60
for y in [-.40,-.377,-.354,-.331]:
    ring('Traction groove yellow rim',(wx,y,wz),.242,.197,.012,yellow)
    ring('Traction groove dark channel',(wx,y+.009,wz),.230,.203,.006,dark)
ring('Front sheave face ring',(wx,-.414,wz),.242,.193,.024,yellow)
cyl('Sheave central hub',(wx,-.414,wz),.073,.055,yellow,(0,1,0),32,'05_Traction_Machine')
cyl('Sheave shaft cap',(wx,-.449,wz),.039,.018,dark,(0,1,0),16,'05_Traction_Machine')
for i in range(8):
    a=i*math.tau/8
    beam('Sheave spoke',(wx+.063*math.cos(a),-.414,wz+.063*math.sin(a)),(wx+.202*math.cos(a+.10),-.414,wz+.202*math.sin(a+.10)),.038,.026,yellow,'05_Traction_Machine')
    bolt((wx+.163*math.cos(a),-.433,wz+.163*math.sin(a)),r=.005)
# Left-side horizontal shaft and bearing blocks match the exposed top mechanism.
cyl('Cross drive shaft',(-.63,.10,3.575),.024,.60,edge,(1,0,0),32,'05_Traction_Machine')
for x in [-.84,-.51]:
    box('Pillow block base',(x,.10,3.40),(.12,.20,.03),dark,'05_Traction_Machine')
    box('Pillow block bearing',(x,.10,3.51),(.06,.13,.20),frame,'05_Traction_Machine',.008)
    cyl('Bearing outer race',(x,.10,3.575),.053,.083,dark,(1,0,0),32,'05_Traction_Machine')

# Four independent traction ropes over the upper semicircle of the grooved sheave.
for y in [-.395,-.372,-.349,-.326]:
    pts=[(wx+.231,y,3.17),(wx+.231,y,wz)]
    for i in range(33):
        a=math.pi*i/32;pts.append((wx+.231*math.cos(a),y,wz+.231*math.sin(a)))
    pts.extend([(wx-.231,y,3.17),(wx-.231,.57,3.13)])
    tube('Traction wire rope',pts,.0038,dark)
for x in [-.55,-.52,-.49,-.46]:tube('Rear descending rope',[(x,.57,3.13),(x,.795,3.10),(x,.795,.40)],.0038,dark)
tube('Motor power conduit',[(.32,.12,3.845),(.48,.21,3.80),(.61,.39,3.49),(.66,.51,3.16),(.67,.77,2.91),(.67,.78,.42)],.011,rubber,smooth=True)
tube('Door controller loom',[(-.40,.25,3.37),(-.61,.23,3.32),(-.67,-.52,3.23),(.52,-.57,3.20),(.65,-.61,2.85)],.009,rubber,smooth=True)
for z in [.48,1.32,2.25,2.88]:
    box('Conduit clip',(.67,.79,z),(.045,.02,.025),frame,'06_Cables_Conduits')
    bolt((.67,.808,z),(0,1,0),.004)
box('Roof electrical junction box',(.55,.36,3.16),(.22,.24,.115),frame,'06_Cables_Conduits',.006)
for x in [.47,.63]:
    for y in [.28,.44]:bolt((x,y,3.221),(0,0,1),.004)

# Front floor indicator: red upward arrow and numeral 1, constructed as meshes.
box('Floor display recess',(0,-.795,2.938),(.56,.019,.126),dark,'08_Controls',.012)
box('Floor display smoked glass',(0,-.808,2.938),(.53,.009,.102),glass,'08_Controls',.008)
beam('LED arrow stem',(-.075,-.815,2.912),(-.075,-.815,2.972),.006,.003,red,'08_Controls')
for x in [-.097,-.053]:beam('LED arrow head',(x,-.815,2.948),(-.075,-.815,2.972),.006,.003,red,'08_Controls')
box('LED floor digit 1',(.065,-.815,2.938),(.006,.003,.060),red,'08_Controls',.0005)
beam('LED digit upper serif',(.048,-.815,2.956),(.065,-.815,2.969),.006,.003,red,'08_Controls')
box('LED digit base',(.063,-.815,2.907),(.030,.003,.005),red,'08_Controls',.0004)
box('Call panel bezel',(.672,-.801,1.63),(.096,.025,.36),edge,'08_Controls',.008)
box('Call panel black upper',(.672,-.817,1.70),(.073,.009,.173),glass,'08_Controls',.004)
for z in [1.72,1.56,1.49]:
    cyl('Call button rim',(.672,-.828,z),.018,.009,edge,(0,-1,0),32,'08_Controls')
    cyl('Call button center',(.672,-.834,z),.0125,.004,dark if z>1.6 else red,(0,-1,0),24,'08_Controls')
for z in [1.465,1.795]:bolt((.672,-.817,z),r=.003)

# Reinforcement and mechanical pads visible from below.
for x in [-.57,.57]:box('Underfloor longitudinal bearer',(x,0,.125),(.09,1.27,.10),dark,'01_Structural_Frame')
for y in [-.47,.47]:
    box('Underfloor crossbrace',(0,y,.105),(1.37,.08,.08),frame)
    for x in [-.57,.57]:
        box('Underfloor corner plate',(x,y,.057),(.15,.16,.016),frame)
        for dx in [-.043,.043]:bolt((x+dx,y,.044),(0,0,-1),.008)

print('Geometry constructed',len(root.children),flush=True)
# All geometry uses meshes in the saved asset, including ropes and controls.
bpy.ops.object.select_all(action='DESELECT')
curves=[o for o in root.children_recursive if o.type=='CURVE']
for o in curves:o.select_set(True)
if curves:
    bpy.context.view_layer.objects.active=curves[0];bpy.ops.object.convert(target='MESH')
# UVs for explicitly generated topology, preserving primitives' UV maps.
for o in root.children_recursive:
    if o.type!='MESH' or o.data.uv_layers:continue
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
im=bpy.data.images.load(REF,check_existing=True);im.name='REFERENCE | elevator six-view sheet';im.use_fake_user=True;im.pack()
note=bpy.data.texts.new('ASSET_README')
note.write('Industrial elevator, modeled from the supplied six-view concept.\nOrigin at floor center. Blender +Z up, front -Y, meter units.\nDoor_Left and Door_Right are independent sliding leaves.\nFrame, cabin, rails, traction equipment, cables, fasteners and controls are organized into named collections.\nReference image is packed in the blend file. Procedural brushed materials are Blender materials; Unity shader setup and motion scripting are not included.\nThis is a visual game prop, not a construction or safety engineering model.\n')

# Temporary studio is removed before saving the asset.
studio=bpy.data.collections.new('TEMP_Render_Studio');scene.collection.children.link(studio)
def aim(o,point):o.rotation_euler=(Vector(point)-o.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('PreviewCamera');cam=bpy.data.objects.new('PreviewCamera',camdata);studio.objects.link(cam)
scene.camera=cam;camdata.type='ORTHO';camdata.ortho_scale=4.65
cam.location=(5,-8,5.0);aim(cam,(0,0,1.94))
for name,pos,power,size,color in [('Key',(-4,-5,6),1250,4,(1,.95,.87)),('Fill',(4,-2,4),900,3,(.82,.91,1)),('Rim',(2,4,6),1500,3,(1,1,1))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=pos;aim(o,(0,0,1.8))
floor=box('TEMP floor',(0,0,-.025),(200,200,.025),material('TEMP floor mat',(.12,.135,.15),0,.8))
floor.parent=None
for c in list(floor.users_collection):c.objects.unlink(floor)
studio.objects.link(floor)
scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.18,.21,.25,1);bg.inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1050;scene.render.resolution_y=1250;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(ART,'industrial_elevator_preview.png')
print('Rendering front three-quarter preview',flush=True)
bpy.ops.render.render(write_still=True)
cam.location=(-5,8,5.3);aim(cam,(0,0,1.94))
scene.render.filepath=os.path.join(ART,'industrial_elevator_rear.png')
print('Rendering rear preview',flush=True)
bpy.ops.render.render(write_still=True)
scene.camera=None
for o in list(studio.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(studio)
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            s=area.spaces.active;s.region_3d.view_distance=7;s.region_3d.view_location=(0,0,1.9)
            s.region_3d.view_rotation=Vector((4,-7,2.5)).to_track_quat('Z','Y');s.shading.type='MATERIAL'
            s.overlay.show_extras=False
bpy.ops.wm.save_as_mainfile(filepath=OUT)
mesh=[o for o in root.children_recursive if o.type=='MESH']
coords=[o.matrix_world@Vector(c) for o in mesh for c in o.bound_box]
report={'file':OUT,'mesh_objects':len(mesh),'base_polygons':sum(len(o.data.polygons) for o in mesh),'dimensions_m':[round(max(v[i] for v in coords)-min(v[i] for v in coords),3) for i in range(3)],'packed_reference':bool(im.packed_file),'separate_doors':all(n in bpy.data.objects for n in ['Door_Left','Door_Right'])}
with open(os.path.join(ART,'industrial_elevator_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print(json.dumps(report),flush=True)
