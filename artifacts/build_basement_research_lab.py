import bpy, math, os, random, json
from mathutils import Vector
random.seed(2910)
BASE=r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(BASE,'Assets','Blender','abandoned_basement_lab.blend')
REF=r'C:\Users\PLUS\AppData\Local\Temp\codex-clipboard-80770f26-c99b-4ed2-b377-95dd93c2485a.png'
ART=os.path.join(BASE,'artifacts')
assert not os.path.exists(OUT),'Do not overwrite existing assets'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.name='B1_Abandoned_Basement_Laboratory'
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
root=bpy.data.objects.new('B1_Basement_Laboratory',None);scene.collection.objects.link(root);root.empty_display_size=.2
root['source']='Six-view abandoned underground laboratory concept supplied by user'
root['layout']='Open cutaway diorama, elevated U catwalk, two stairs, central experiment bay and glazed observation booth'
root['scale']='Meters; dimensions inferred from concept proportions'
groups={}
for name in ['01_Structure','02_Floors_Gratings','03_Catwalks_Stairs','04_Railings','05_Pipes_Vents','06_Doors_Signs','07_Lab_Equipment','08_Furniture_Storage','09_Weathering','10_Light_Fixtures']:
    c=bpy.data.collections.new(name);scene.collection.children.link(c);groups[name]=c

def mat(name,col,metal=0,rough=.65,weather=0,emit=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
    ns=m.node_tree.nodes;ln=m.node_tree.links;p=next(n for n in ns if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*col,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if emit:p.inputs['Emission Color'].default_value=(*col,1);p.inputs['Emission Strength'].default_value=emit
    if weather:
        tc=ns.new('ShaderNodeTexCoord');noise=ns.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1.9;noise.inputs['Detail'].default_value=5
        ln.new(tc.outputs['Object'],noise.inputs['Vector']);r=ns.new('ShaderNodeValToRGB')
        r.color_ramp.elements[0].position=.2;r.color_ramp.elements[0].color=(*[x*.32 for x in col],1)
        r.color_ramp.elements[1].position=.82;r.color_ramp.elements[1].color=(*[min(x*1.3,.8) for x in col],1)
        ln.new(noise.outputs['Fac'],r.inputs[0]);ln.new(r.outputs[0],p.inputs['Base Color'])
        fine=ns.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=42;fine.inputs['Detail'].default_value=3;ln.new(tc.outputs['Object'],fine.inputs['Vector'])
        b=ns.new('ShaderNodeBump');b.inputs['Strength'].default_value=.32;b.inputs['Distance'].default_value=.022 if weather==2 else .004
        ln.new(fine.outputs['Fac'],b.inputs['Height']);ln.new(b.outputs[0],p.inputs['Normal'])
    return m
concrete=mat('Concrete | aged cold grey',(.19,.215,.224),rough=.91,weather=2)
slab=mat('Concrete | broken slab edges',(.105,.12,.125),rough=.95,weather=2)
floor=mat('Floor | worn industrial tiles',(.22,.25,.26),.1,.79,2)
floor2=mat('Floor | varied dark tile',(.17,.20,.21),.1,.83,2)
steel=mat('Steel | oxidized blue grey',(.11,.145,.16),.75,.53,1)
dark=mat('Steel | blackened framing',(.035,.047,.051),.78,.52,1)
silver=mat('Steel | exposed machined edges',(.28,.33,.35),.85,.33,1)
yellow=mat('Paint | faded safety yellow',(.48,.32,.045),.48,.63,1)
rust=mat('Rust | brown oxide',(.18,.075,.029),.3,.92,1)
black=mat('Recess | charcoal black',(.006,.011,.012),0,.8)
blue=mat('Paint | old blue drums',(.035,.115,.175),.6,.55,1)
ivory=mat('Paint | laboratory equipment',(.38,.43,.42),.5,.51,1)
white=mat('Stencil | faded white',(.63,.68,.65),0,.85)
paper=mat('Paper | aged labels',(.38,.35,.24),0,.95)
cardboard=mat('Storage | cardboard',(.23,.16,.084),0,.9,1)
rubber=mat('Cable | dirty rubber',(.013,.018,.017),0,.8)
glass=mat('Glass | dirty observation pane',(.09,.24,.29),.12,.18)
p=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Transmission Weight'].default_value=.55;p.inputs['IOR'].default_value=1.45
cyan=mat('Emission | fluorescent cyan',(.34,.75,1),.1,.3,emit=4)
screen=mat('Emission | muted CRT display',(.028,.18,.23),.1,.35,emit=1.1)
red=mat('Emission | warning red',(.8,.025,.008),.1,.35,emit=3)
green=mat('Emission | status green',(.025,.42,.16),0,.3,emit=2)
stain=mat('Grime | oily seepage',(.045,.056,.049),0,.96,1)

def link(name,mesh,material,group):
    o=bpy.data.objects.new(name,mesh);groups[group].objects.link(o);o.parent=root
    if material and not mesh.materials:mesh.materials.append(material)
    return o
def uv(me):
    layer=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        for j,li in enumerate(p.loop_indices):layer.data[li].uv=[(0,0),(1,0),(1,1),(0,1)][j%4]
def bevel(o,w=.01):
    b=o.modifiers.new('Worn bevel','BEVEL');b.width=w;b.segments=2
def box(name,loc,size,m=steel,g='01_Structure',bev=.006):
    sx,sy,sz=[s/2 for s in size]
    v=[(-sx,-sy,-sz),(sx,-sy,-sz),(sx,sy,-sz),(-sx,sy,-sz),(-sx,-sy,sz),(sx,-sy,sz),(sx,sy,sz),(-sx,sy,sz)]
    f=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();uv(me)
    o=link(name,me,m,g);o.location=loc
    if bev:bevel(o,min(bev,min(size)*.3))
    return o
cache={}
def cyl(name,loc,r,depth,m=steel,axis=(0,0,1),g='05_Pipes_Vents',n=20):
    key=(round(r,5),round(depth,5),m.name,n)
    if key not in cache:
        vs=[(r*math.cos(i*math.tau/n),r*math.sin(i*math.tau/n),z) for z in [-depth/2,depth/2] for i in range(n)]
        fs=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();uv(me)
        for p in list(me.polygons)[2:]:p.use_smooth=True
        me.materials.append(m);cache[key]=me
    o=link(name,cache[key],None,g);o.location=loc;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();return o
def rod(name,a,b,r=.025,m=steel,g='04_Railings'):
    a,b=Vector(a),Vector(b);return cyl(name,(a+b)/2,r,(b-a).length,m,b-a,g)
def beam(name,a,b,w=.1,d=.1,m=dark,g='03_Catwalks_Stairs'):
    a,b=Vector(a),Vector(b);o=box(name,(a+b)/2,(w,d,(b-a).length),m,g);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def path(name,points,r=.035,m=steel,g='05_Pipes_Vents',rounding=.15):
    # Rounded sampled bends preserve straight pipe runs between corners.
    p=[Vector(v) for v in points];q=[p[0]]
    for i in range(1,len(p)-1):
        a=(p[i-1]-p[i]).normalized();b=(p[i+1]-p[i]).normalized();d=min(rounding,(p[i-1]-p[i]).length*.25,(p[i+1]-p[i]).length*.25)
        s=p[i]+a*d;e=p[i]+b*d;q.append(s)
        for j in range(1,9):
            t=j/8;q.append((1-t)**2*s+2*(1-t)*t*p[i]+t*t*e)
    q.append(p[-1]);cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2
    sp=cu.splines.new('POLY');sp.points.add(len(q)-1)
    for v,co in zip(sp.points,q):v.co=(*co,1)
    o=link(name,cu,m,g);return o
def text(name,body,loc,size=.2,m=white,rot=(math.pi/2,0,0),g='06_Doors_Signs'):
    cu=bpy.data.curves.new(name,'FONT');cu.body=body;cu.size=size;cu.extrude=.0008;cu.align_x='CENTER'
    o=link(name,cu,m,g);o.location=loc;o.rotation_euler=rot;return o
def bolt(loc,axis=(0,-1,0),r=.022):return cyl('Exposed hex fastener',loc,r,.012,silver,axis,'09_Weathering',6)
def rail(a,b,z):
    a=Vector((a[0],a[1],z));b=Vector((b[0],b[1],z));n=max(1,math.ceil((b-a).length/1.35))
    for i in range(n+1):
        p=a.lerp(b,i/n);box('Railing base plate',p+Vector((0,0,.025)),(.15,.15,.035),dark,'04_Railings')
        rod('Yellow guard post',p,p+Vector((0,0,1.03)),.025,yellow)
    for h in [.50,1.03]:rod('Yellow handrail',a+Vector((0,0,h)),b+Vector((0,0,h)),.026,yellow)
    beam('Catwalk kick plate',a+Vector((0,0,.10)),b+Vector((0,0,.10)),.08,.025,dark,'04_Railings')
def panelwall(x0,x1,z0,z1,y=4.65):
    nx=max(1,math.ceil((x1-x0)/1.5));nz=max(1,math.ceil((z1-z0)/1.3))
    for i in range(nx):
        for j in range(nz):
            w=(x1-x0)/nx;h=(z1-z0)/nz;x=x0+(i+.5)*w;z=z0+(j+.5)*h
            box('Rear concrete panel',(x,y,z),(w-.016,.24,h-.016),concrete,bev=.012)
            for dx in [-w/2+.11,w/2-.11]:bolt((x+dx,y-.127,z+h/2-.10),r=.018)

# Footprint: central lab slab, narrow wings, and exposed underside support beams.
box('Main foundation',(0,0,-.28),(12,9.6,.55),slab,bev=.05)
box('Left entry wing',(-5.05,-6.05,-.28),(1.9,2.6,.55),slab,bev=.04)
box('Right service wing',(5.05,-6.35,-.28),(1.9,3.2,.55),slab,bev=.04)
for ix in range(12):
    for iy in range(9):box('Jointed floor tile',(-5.5+ix,-4+iy,.009),(.984,.984,.025),floor if random.random()>.27 else floor2,'02_Floors_Gratings',.004)
for x in [-5.05,5.05]:
    for y in [-5.3,-6.3]:box('Wing floor tile',(x,y,.01),(1.86,.985,.03),floor,'02_Floors_Gratings')
for x in [-5,-2.5,0,2.5,5]:box('Underside steel joist',(x,0,-.60),(.15,9.3,.18),dark)
for y in [-4,0,4]:box('Underside cross beam',(0,y,-.64),(11.7,.17,.22),dark)
for x in [-5.5,5.5]:path('Underslab supply pipe',[(x,-6,-.7),(x,3.7,-.7),(0,3.7,-.7)],.055,dark)

# Full back and left walls; the foreground and right side remain cut away for visibility.
panelwall(-6,1.4,0,5.5);panelwall(5.5,6,0,5.5);panelwall(1.4,5.5,0,2.35);panelwall(1.4,5.5,3.95,5.5)
for iy in range(7):
    for iz in range(4):
        y=-4.25+iy*1.35;z=.69+iz*1.375
        box('Left concrete panel',(-6,y,z),(.24,1.334,1.359),concrete,bev=.012)
        for dy in [-.55,.55]:bolt((-5.872,y+dy,z+.55),(1,0,0),.018)
for x in [-6,-2,1.4,5.6]:box('Back pilaster',(x,4.43,2.75),(.20,.20,5.5),slab)
for y in [-4.8,-1.35,2.4,4.5]:box('Left wall pilaster',(-5.80,y,2.75),(.20,.20,5.5),slab)
box('Back wall coping',(0,4.65,5.52),(12.25,.38,.15),slab)
box('Left wall coping',(-6,0,5.52),(.38,9.6,.15),slab)
box('Right cutaway curb',(6,-.4,.55),(.22,8.7,1.1),concrete)
box('Front cutaway partition',(0,-4.65,.60),(5.7,.22,1.2),concrete)

# Elevated U-shaped walkway with a gap at the lower end of the left stair.
Z=1.65
for name,loc,size in [('Rear catwalk',(0,3.8,Z-.10),(11.8,1.45,.20)),('Left catwalk',(-5.16,1.25,Z-.10),(1.45,5.15,.20)),('Right catwalk',(5.16,-1.05,Z-.10),(1.45,10.9,.20))]:box(name,loc,size,steel,'03_Catwalks_Stairs',.02)
for x in [-5.16,5.16]:
    for y in [-1,1.2,3.6]:
        box('Catwalk support column',(x,y,.76),(.14,.14,1.52),dark,'03_Catwalks_Stairs')
        beam('Catwalk diagonal bracket',(x,y,.7),(x+(.5 if x<0 else -.5),y,1.5),.09,.09)
rail((-4.41,-1.25),(-4.41,3.05),Z);rail((-4.41,3.05),(4.41,3.05),Z)
rail((4.41,-2.95),(4.41,3.05),Z);rail((4.41,-6.4),(4.41,-4.3),Z)
rail((5.88,-6.4),(5.88,3.0),Z)
# Stair 1 follows the left wall; stair 2 connects the foreground lab to the right wing.
def stairs(name,start,end,width=1.15,n=11):
    a,b=Vector(start),Vector(end);run=Vector((b.x-a.x,b.y-a.y,0));u=run.normalized();across=Vector((-u.y,u.x,0));length=run.length
    for i in range(n):
        pos=a+u*((i+.5)*length/n);pos.z=a.z+(i+1)*(b.z-a.z)/n
        o=box(name+' steel tread',pos,(width,length/n+.022,.045),steel,'03_Catwalks_Stairs',.004)
        o.rotation_euler.z=math.atan2(-u.x,u.y)
        lip=box('Yellow stair nosing',pos-u*(length/n*.43)+Vector((0,0,.025)),(width,.026,.012),yellow,'03_Catwalks_Stairs',.002);lip.rotation_euler.z=o.rotation_euler.z
    for sign in [-1,1]:
        aa=a+across*(width/2+.02);bb=b+across*(width/2+.02)
        beam('Stair stringer',aa-Vector((0,0,.12)),bb-Vector((0,0,.12)),.14,.07)
        for t in [0,.5,1]:
            p=aa.lerp(bb,t);rod('Stair railing post',p,p+Vector((0,0,1.0)),.026,yellow)
        for h in [.5,1.0]:rod('Sloping yellow handrail',aa+Vector((0,0,h)),bb+Vector((0,0,h)),.026,yellow)
stairs('Left access stair',(-5.16,-4.65,.03),(-5.16,-1.3,Z),1.15)
stairs('Right access stair',(1.7,-3.64,.03),(4.41,-3.64,Z),1.10)
# Large inset floor drains on either side of the experiment station.
for x in [-2.95,2.95]:
    box('Drain dark channel',(x,-.25,.04),(.74,5.4,.055),black,'02_Floors_Gratings')
    for y in [i*.12-2.88 for i in range(45)]:box('Grating crossbar',(x,y,.076),(.71,.024,.035),dark,'02_Floors_Gratings',.002)
    for dx in [-.355,0,.355]:box('Grating longitudinal rib',(x+dx,-.25,.077),(.028,5.4,.035),silver,'02_Floors_Gratings',.002)
for x in [-3.68,3.68]:box('Bay yellow boundary',(x,-.2,.03),(.045,5.8,.006),yellow,'02_Floors_Gratings',0)
for y in [-3.1,2.7]:box('Bay yellow boundary',(0,y,.03),(7.4,.045,.006),yellow,'02_Floors_Gratings',0)
print('Architecture, catwalks and stairs complete',flush=True)

def fixture(x,y,z,length=1.1,wall='back'):
    loc=(x,y,z);g='10_Light_Fixtures'
    if wall=='back':
        box('Fluorescent protective housing',loc,(length+.15,.12,.12),dark,g)
        box('Frosted luminous tube',(x,y-.07,z),(length,.026,.044),cyan,g,.01)
        for dx in [-length/2,length/2]:box('Tube end clip',(x+dx,y-.07,z),(.04,.04,.09),silver,g)
        pos=(x,y-.20,z-.08);target=(x,y-1.5,z-1)
    else:
        box('Fluorescent protective housing',loc,(.12,length+.15,.12),dark,g)
        box('Frosted luminous tube',(x+.07,y,z),(.026,length,.044),cyan,g,.01)
        pos=(x+.2,y,z-.08);target=(x+1.5,y,z-1)
    d=bpy.data.lights.new('Installed fluorescent light','AREA');d.energy=65;d.color=(.55,.76,1);d.shape='RECTANGLE';d.size=length;d.size_y=.18
    o=bpy.data.objects.new(d.name,d);groups[g].objects.link(o);o.parent=root;o.location=pos;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
def hazard(name,center,width,height=.12,front=True):
    x,y,z=center
    if front:
        box(name+' yellow field',center,(width,.014,height),yellow,'06_Doors_Signs',0)
        # Narrow diagonal bars remain inside the rectangular boundary.
        for i in range(max(1,int(width/.14))):
            xx=x-width/2+.06+i*.14
            a=(xx,y-.009,z-height*.45);b=(xx+.075,y-.009,z+height*.45)
            beam(name+' diagonal black stripe',a,b,.041,.006,black,'06_Doors_Signs')
    else:
        box(name+' yellow field',center,(width,height,.007),yellow,'02_Floors_Gratings',0)
        for i in range(max(1,int(width/.15))):
            o=box(name+' diagonal black stripe',(x-width/2+.07+i*.15,y,z+.005),(.055,height*.95,.004),black,'02_Floors_Gratings',0);o.rotation_euler.z=-.45
def door(name,x,y,z,scale=1):
    # All door components face -Y; closed separate leaf is preserved for later editing.
    g='06_Doors_Signs';w=1.18*scale;h=2.2*scale
    box(name+' recessed opening',(x,y+.025,z+h/2),(w+.28,.12,h+.23),black,g,.04)
    for sx in [-1,1]:
        box(name+' heavy jamb',(x+sx*(w/2+.08),y-.03,z+h/2),(.16,.16,h+.2),steel,g,.035)
        for zz in [.18,.68,1.25,1.83]:bolt((x+sx*(w/2+.08),y-.119,z+zz*scale),r=.024)
    box(name+' top crossmember',(x,y-.03,z+h+.04),(w+.31,.16,.17),steel,g,.035)
    leaf=box(name+' sealed door leaf',(x,y-.055,z+h/2),(w,.12,h),steel,g,.045)
    leaf['opening']='Closed independent door mesh'
    box(name+' lower inset',(x,y-.123,z+.61*scale),(w-.25,.018,.88*scale),dark,g,.025)
    box(name+' lower inset face',(x,y-.136,z+.61*scale),(w-.29,.01,.84*scale),steel,g,.02)
    box(name+' viewport seal',(x,y-.126,z+1.65*scale),(.46*scale,.035,.38*scale),black,g,.035)
    box(name+' viewport rim',(x,y-.147,z+1.65*scale),(.395*scale,.028,.32*scale),silver,g,.025)
    box(name+' viewport dark glass',(x,y-.164,z+1.65*scale),(.32*scale,.016,.25*scale),glass,g,.018)
    for zz in [.28,1.03,1.83]:cyl('Door hinge barrel',(x-w/2+.035,y-.14,z+zz*scale),.035,.16,steel,(0,0,1),g)
    rod('Door lever standoff',(x+w*.32,y-.15,z+1.04*scale),(x+w*.32,y-.24,z+1.04*scale),.023,silver,g)
    rod('Door lever grip',(x+w*.32,y-.24,z+.91*scale),(x+w*.32,y-.24,z+1.17*scale),.023,silver,g)
    hazard('Door kick plate',(x,y-.127,z+.11),w-.1,.17)
    text('Door identification','B1 / '+name.upper(),(x,y-.13,z+1.30*scale),.065,white)
    # Badge reader and alarm beacon beside the door.
    box('Access reader',(x-w/2-.32,y-.075,z+1.26),(.18,.15,.32),dark,g,.015)
    box('Reader screen',(x-w/2-.32,y-.158,z+1.31),(.10,.012,.10),screen,g,.006)
    cyl('Alarm beacon base',(x-w/2-.32,y-.08,z+1.76),.07,.07,dark,(0,0,1),g)
    cyl('Red warning beacon',(x-w/2-.32,y-.08,z+1.84),.054,.13,red,(0,0,1),g)
    fixture(x,y-.04,z+h+.25,.65)
    return leaf
door('Containment',-1.75,4.36,Z)
text('B1 wall stencil','B1',(-.10,4.386,3.75),.51)
text('Research lab wall stencil','RESEARCH LAB',(-.10,4.386,3.53),.105)
box('Service door partition',(5.08,3.00,Z+1.22),(1.65,.18,2.44),concrete)
door('Service',5.02,2.87,Z,.82)
# A second lower-level door in a short foreground partition.
box('Lower service wall',(-3.95,-4.25,1.29),(1.8,.22,2.58),concrete)
door('Auxiliary',-3.95,-4.40,.02,.88)
text('Lower B1 marker','B1',(-5.865,-.05,1.00),.48,white,(math.pi/2,0,math.pi/2))

# Observation booth, with an actual opening through the main back wall.
box('Observation room floor',(3.45,5.47,Z-.12),(4.2,1.65,.22),slab)
box('Observation room rear wall',(3.45,6.35,3.35),(4.25,.18,3.4),concrete)
for x in [1.35,5.55]:box('Observation room side wall',(x,5.50,3.35),(.15,1.72,3.4),concrete)
for x in [1.44,5.46]:box('Observation window upright',(x,4.47,3.15),(.09,.16,1.65),dark,'07_Lab_Equipment')
for z in [2.35,3.95]:box('Observation window sill',(3.45,4.47,z),(4.12,.20,.10),dark,'07_Lab_Equipment')
for x in [2.8,4.12]:box('Observation glazing mullion',(x,4.49,3.15),(.045,.09,1.53),steel,'07_Lab_Equipment')
for x in [2.12,3.46,4.80]:box('Observation blue glazing',(x,4.51,3.15),(1.28,.015,1.5),glass,'07_Lab_Equipment',0)
text('Observation window header','OBSERVATION  /  AUTHORIZED PERSONNEL',(3.45,4.39,4.13),.12)
fixture(3.45,6.19,4.65,1.3)

# Main pipe trunks wrap the back wall and turn along the left wall.
for z,r,offset in [(5.10,.11,0),(4.84,.07,.18)]:
    path('Continuous overhead utility main',[(-5.65,-4.5,z),(-5.65,4.25-offset,z),(5.7,4.25-offset,z),(5.7,4.25-offset,1.72)],r,dark,rounding=.30)
    for x in [-5,-3,-1,1,3,5]:
        cyl('Pipe bolted flange',(x,4.25-offset,z),r*1.45,.065,steel,(1,0,0))
        for j in range(6):
            a=j*math.tau/6;bolt((x-.039,4.25-offset+r*1.2*math.cos(a),z+r*1.2*math.sin(a)),(-1,0,0),.014)
        box('Pipe wall bracket',(x,4.48,z),(.07,.28,.24),dark,'05_Pipes_Vents')
    for y in [-3,-1,1,3]:cyl('Left pipe coupling',(-5.65,y,z),r*1.32,.075,steel,(0,1,0))
path('Large extraction duct',[(4.75,4.2,4.58),(4.75,3.98,4.58),(2.2,3.98,4.58),(2.2,4.58,4.58)],.19,steel,rounding=.30)
for x in [2.35,3.10,3.85,4.6]:cyl('Duct sleeve seam',(x,3.98,4.58),.202,.055,silver,(1,0,0))
path('Left ventilation elbow',[(-5.65,2.35,5.42),(-5.65,2.35,4.55),(-5.3,2.35,4.25),(-5.3,1.1,4.25)],.17,steel,rounding=.3)
for x in [-5.35,5.48]:
    path('Yellow small-bore supply',[(x,4.33,5.3),(x,4.33,1.78),(x-.28,4.33,1.78)],.025,yellow)
    for z in [2.2,3.1,4.0]:box('Small pipe retaining clip',(x,4.35,z),(.10,.06,.035),steel,'05_Pipes_Vents')
for x in [-3.5,.7]:path('Surface electrical conduit',[(x,4.42,4.9),(x,4.42,4.35),(x+.25,4.42,4.35),(x+.25,4.42,2.7)],.018,dark)
# Left wall pipe drops, valves and bends.
for y in [-3.9,-.9,3.3]:
    path('Left vertical riser',[(-5.72,y,.20),(-5.72,y,4.7),(-5.72,y+.32,4.98)],.065,steel)
    cyl('Valve body',(-5.72,y,1.25),.12,.20,dark,(0,0,1))
    cyl('Valve wheel hub',(-5.50,y,1.25),.05,.14,rust,(1,0,0))
    # Open wheel, made from a curved loop and radial spokes.
    points=[(-5.40,y+.15*math.cos(i*math.tau/32),1.25+.15*math.sin(i*math.tau/32)) for i in range(33)]
    path('Red handwheel',points,.018,rust,rounding=0)
    for a in [0,math.pi/2,math.pi,3*math.pi/2]:rod('Valve wheel spoke',(-5.4,y,1.25),(-5.4,y+.14*math.cos(a),1.25+.14*math.sin(a)),.012,rust,'05_Pipes_Vents')
def vent(x,y,z,w=1,h=.42,left=False):
    g='05_Pipes_Vents'
    if not left:
        box('Vent metal rim',(x,y,z),(w+.08,.09,h+.08),steel,g)
        box('Vent black opening',(x,y-.05,z),(w,.02,h),black,g)
        for i in range(7):box('Horizontal vent louver',(x,y-.07,z-h/2+(i+.5)*h/7),(w,.035,.016),dark,g,.001)
        for i in range(9):box('Vent vertical grid',(x-w/2+(i+.5)*w/9,y-.09,z),(.009,.01,h),steel,g,.001)
    else:
        box('Left vent rim',(x,y,z),(.09,w+.08,h+.08),steel,g)
        box('Left vent opening',(x+.05,y,z),(.02,w,h),black,g)
        for i in range(7):box('Left vent louver',(x+.075,y,z-h/2+(i+.5)*h/7),(.035,w,.016),dark,g,.001)
vent(-4.35,4.40,4.22,1.18,.45);vent(-5.84,-.4,.68,.7,.45,True)
vent(-5.84,1.2,4.5,1.0,.4,True)
for x in [-4.7,-.2,3.1]:fixture(x,4.38,4.61,1.15)
for y in [-3.2,.3,3.2]:fixture(-5.78,y,4.45,1.1,'left')

def cabinet(name,x,y,z,w=.8,h=1.5):
    g='08_Furniture_Storage'
    box(name+' case',(x,y,z+h/2),(w,.46,h),steel,g,.018)
    for s in [-1,1]:
        xx=x+s*w*.247
        box(name+' door',(xx,y-.241,z+h/2),(w*.477,.025,h-.06),steel,g,.01)
        box('Recessed cabinet handle',(x+s*.055,y-.259,z+h*.56),(.025,.018,.13),black,g,.003)
        for zz in [.15,.21,.27]:box('Locker ventilation slot',(xx,y-.257,z+zz),(w*.29,.006,.013),black,g,0)
        box('Cabinet label',(xx,y-.260,z+h-.21),(.12,.005,.08),paper,g,.001)
    for dx in [-w*.36,w*.36]:box('Cabinet foot',(x+dx,y,z+.025),(.09,.35,.05),dark,g)
    return
cabinet('B1 equipment locker',.18,4.02,Z,.85,1.48)
cabinet('Left wing cabinet',-5.14,2.4,Z,1.0,1.7)
cabinet('Control room locker',4.95,5.98,Z,.7,1.65)
cabinet('Foreground service cabinet',-.6,-4.23,.02,.85,1.15)
def rack(name,x,y,z,w=1.1,h=1.55,d=.45):
    g='08_Furniture_Storage'
    for dx in [-w/2,w/2]:
        for dy in [-d/2,d/2]:box(name+' upright',(x+dx,y+dy,z+h/2),(.045,.045,h),steel,g,.004)
    for level in range(4):
        zz=z+.06+level*(h-.08)/3;box(name+' shelf',(x,y,zz),(w,d,.035),steel,g,.004)
        if level<3:
            for k in range(3):
                ww=random.uniform(.20,.29);xx=x-w*.32+k*w*.32
                box('Stored archive bin',(xx,y,zz+.15),(ww,d*.79,.26),cardboard if k%2 else dark,g,.007)
                box('Faded inventory label',(xx,y-d*.40-.004,zz+.15),(.10,.004,.05),paper,g,.001)
rack('Observation supply rack',2.15,5.98,Z,1.05,1.45)
rack('Back supply rack',.78,3.98,Z,.55,1.35,.40)
rack('Front equipment rack',1.01,-4.20,.02,1.2,1.25,.44)
rack('Left lower storage',-4.97,1.2,.04,1.1,1.30,.5)
def barrel(x,y,z):
    g='08_Furniture_Storage'
    cyl('Blue chemical drum',(x,y,z+.43),.29,.86,blue,(0,0,1),g,40)
    for zz in [.035,.19,.66,.845]:cyl('Drum rolled steel hoop',(x,y,z+zz),.30,.026,dark,(0,0,1),g,40)
    cyl('Drum lid',(x,y,z+.868),.276,.015,blue,(0,0,1),g,40)
    cyl('Drum bung',(x+.13,y,z+.881),.032,.015,silver,(0,0,1),g)
    box('Chemical warning label',(x,y-.292,z+.46),(.14,.006,.18),paper,g,.002)
for x,y,z in [(3.6,3.86,Z),(4.3,3.86,Z),(-3.5,-3.8,.03),(-3.03,-3.75,.03)]:barrel(x,y,z)

# Gas cylinder storage cage on the right wing.
for x in [4.74,5.11,5.48]:
    cyl('Compressed gas cylinder',(x,1.95,Z+.64),.135,1.18,blue,(0,0,1),'07_Lab_Equipment',32)
    cyl('Gas bottle shoulder',(x,1.95,Z+1.26),.10,.12,steel,(0,0,1),'07_Lab_Equipment',24)
    cyl('Cylinder brass valve',(x,1.95,Z+1.37),.03,.10,yellow,(0,0,1),'07_Lab_Equipment')
    rod('Bottle valve handle',(x-.07,1.95,Z+1.42),(x+.07,1.95,Z+1.42),.015,silver,'07_Lab_Equipment')
    path('Gas feed hose',[(x,1.95,Z+1.39),(x,2.20,Z+1.5),(5.78,2.25,Z+1.5),(5.78,2.25,Z+.2)],.014,rubber)
for x in [4.53,5.7]:
    for y in [1.67,2.24]:box('Gas rack corner post',(x,y,Z+.77),(.04,.04,1.54),dark,'07_Lab_Equipment')
for z in [Z+.1,Z+.65,Z+1.50]:
    for y in [1.66,2.25]:box('Gas bottle rack bar',(5.115,y,z),(1.22,.045,.04),steel,'07_Lab_Equipment')
for x in [4.65,4.9,5.15,5.4,5.65]:box('Gas cage grille',(x,1.65,Z+.8),(.012,.015,1.35),steel,'07_Lab_Equipment',.001)
text('Gas warning','CAUTION / GAS',(5.12,1.621,Z+1.57),.085,yellow)

def monitor(x,y,z,w=.52):
    g='07_Lab_Equipment'
    box('Monitor foot',(x,y,z+.025),(.24,.19,.035),dark,g)
    box('Monitor stand',(x,y+.03,z+.15),(.045,.04,.26),steel,g)
    box('Monitor casing',(x,y,z+.38),(w,.11,.36),dark,g,.018)
    box('Monitor screen',(x,y-.059,z+.38),(w-.045,.007,.31),screen,g,.004)
    # Graphic UI is geometry, so it remains visible without external textures.
    for j in range(6):
        ww=(w-.10)*random.uniform(.30,.95)
        box('Screen data row',(x-w*.35+ww/2,y-.064,z+.48-j*.035),(ww,.002,.004),cyan,g,0)
    for j in range(3):box('Screen graph bar',(x+w*.30+j*.025,y-.065,z+.30+j*.014),(.010,.002,.045+j*.01),cyan,g,0)
def desk(x,y,z,w=1.6):
    g='08_Furniture_Storage'
    box('Operator desk top',(x,y,z+.78),(w,.62,.065),steel,g,.012)
    for dx in [-w*.4,w*.4]:box('Desk pedestal',(x+dx,y,z+.38),(.32,.52,.74),dark,g,.012)
    for zz in [.18,.39,.60]:
        box('Desk drawer face',(x-w*.4,y-.27,z+zz),(.29,.025,.18),steel,g,.003)
        box('Drawer pull',(x-w*.4,y-.294,z+zz+.02),(.09,.025,.018),silver,g,.004)
    monitor(x-.36,y+.04,z+.82,.53);monitor(x+.29,y+.04,z+.82,.60)
    box('Keyboard',(x,y-.19,z+.825),(.47,.16,.025),black,'07_Lab_Equipment',.006)
    for i in range(12):box('Keyboard key row',(x-.20+i*.035,y-.20,z+.841),(.024,.085,.005),steel,'07_Lab_Equipment',.001)
    # An empty operator chair emphasizes the abandoned setting.
    cyl('Chair central pedestal',(x,y-.72,z+.25),.035,.40,dark,(0,0,1),g)
    box('Office chair cushion',(x,y-.72,z+.48),(.43,.42,.09),black,g,.035)
    box('Office chair back',(x,y-.94,z+.77),(.43,.08,.45),black,g,.035)
    for i in range(5):
        a=i*math.tau/5;end=(x+.31*math.cos(a),y-.72+.31*math.sin(a),z+.07)
        rod('Chair star base',(x,y-.72,z+.12),end,.020,dark,g);cyl('Chair caster',end,.043,.04,black,(1,0,0),g)
desk(3.5,5.95,Z,1.7)
desk(-2.4,2.23,.02,1.7)

# Central experiment station, surrounded by a marked foundation pad.
box('Experiment foundation',(0,-.05,.09),(2.30,2.80,.16),dark,'07_Lab_Equipment',.035)
for x in [-1.10,1.10]:
    box('Experiment pad yellow edge',(x,-.05,.179),(.09,2.65,.008),yellow,'02_Floors_Gratings',0)
    for j in range(13):box('Experiment pad black stripe',(x,-1.24+j*.20,.184),(.092,.08,.004),black,'02_Floors_Gratings',0)
box('Experiment main pedestal',(0,-.05,.57),(1.24,1.72,.84),steel,'07_Lab_Equipment',.045)
for x in [-.59,.59]:
    for y in [-.75,.65]:box('Experiment corner brace',(x,y,.61),(.095,.095,.91),silver,'07_Lab_Equipment',.012)
box('Experiment instrument bench',(0,-.05,1.05),(1.70,2.10,.16),ivory,'07_Lab_Equipment',.055)
box('Containment gasket',(0,.08,1.145),(.85,1.27,.045),black,'07_Lab_Equipment',.045)
box('Containment glass',(0,.08,1.24),(.72,1.14,.16),glass,'07_Lab_Equipment',.045)
box('Blue specimen core',(0,.08,1.23),(.23,.63,.12),cyan,'07_Lab_Equipment',.045)
for x in [-.49,.49]:
    for y in [-.61,.73]:
        cyl('Instrument mount',(x,y,1.18),.10,.11,dark,(0,0,1),'07_Lab_Equipment')
        cyl('Instrument screw cap',(x,y,1.247),.064,.026,silver,(0,0,1),'07_Lab_Equipment',12)
for x in [-.69,.69]:
    for y in [-.47,.14,.59]:
        cyl('Sample canister',(x,y,1.23),.063,.22,steel,(0,0,1),'07_Lab_Equipment')
        cyl('Sample cap',(x,y,1.35),.068,.025,dark,(0,0,1),'07_Lab_Equipment')
box('Experiment control face',(0,-1.119,1.065),(.66,.035,.13),dark,'07_Lab_Equipment',.012)
for x in [-.22,-.13,-.04,.05,.14,.23]:cyl('Experiment status lamp',(x,-1.14,1.08),.017,.01,green if x<.1 else red,(0,-1,0),'07_Lab_Equipment',16)
text('Experiment front stencil','PROJECT 07  /  CONTAINMENT',(0,-.919,.74),.075,white)
for sx in [-1,1]:path('Experiment power umbilical',[(sx*.54,.65,.45),(sx*.95,.83,.20),(sx*1.38,1.22,.09),(sx*1.38,2.70,.09)],.035,rubber)
monitor(.88,-1.43,.36,.48)
box('Remote console pedestal',(.88,-1.43,.20),(.32,.30,.4),steel,'07_Lab_Equipment')

# Fixed maintenance ladder, instrument panels, labels, cable trays and scuffed details.
for x in [5.45,5.84]:rod('Maintenance ladder stile',(x,4.24,Z),(x,4.24,5.40),.027,steel,'05_Pipes_Vents')
for z in [Z+.16+i*.27 for i in range(14)]:rod('Maintenance ladder rung',(5.45,4.24,z),(5.84,4.24,z),.024,steel,'05_Pipes_Vents')
for x in [-3.6,.76]:
    box('Wall electrical box',(x,4.35,2.65),(.31,.15,.61),steel,'07_Lab_Equipment',.02)
    box('Electrical box front',(x,4.265,2.65),(.26,.025,.55),dark,'07_Lab_Equipment',.01)
    for i in range(6):cyl('Control indicator',(x-.055+(i%2)*.11,4.243,2.81-(i//2)*.12),.017,.008,red if i==1 else green,(0,-1,0),'07_Lab_Equipment',12)
for x,y in [(-1.9,4.43),(.9,4.43),(5.2,4.43)]:
    box('Warning placard',(x,y,4.28),(.22,.012,.25),yellow,'06_Doors_Signs',.004)
    text('Warning symbol','!',(x,y-.013,4.235),.19,black)
for y in [-3.4,-.6,2.1]:
    box('Left identification panel',(-5.858,y,3.35),(.015,.32,.22),steel,'06_Doors_Signs')
    text('Panel code','B1',(-5.846,y,3.29),.13,white,(math.pi/2,0,math.pi/2))
# Wall cracks are thin irregular geometry; streaks are shallow stained surfaces.
for k in range(24):
    x=random.uniform(-5.7,1.1);z=random.uniform(2,5.2)
    pts=[(x,4.519,z)]
    for j in range(random.randint(3,6)):
        x+=random.uniform(-.10,.10);z-=random.uniform(.08,.21);pts.append((x,4.519,z))
    path('Concrete hairline fracture',pts,.006,black,'09_Weathering',.006)
for k in range(30):
    x=random.uniform(-5.6,5.6);z=random.uniform(4.3,5.1);w=random.uniform(.012,.045);h=random.uniform(.12,.65)
    box('Vertical water stain',(x,4.517,z-h/2),(w,.003,h),stain,'09_Weathering',0)
for k in range(30):
    x=random.uniform(-3.7,3.8);y=random.uniform(-3.5,2.6)
    if abs(x)<1.3 and abs(y)<1.5:continue
    o=box('Scattered concrete chip',(x,y,.055),(random.uniform(.05,.16),random.uniform(.04,.12),random.uniform(.02,.06)),slab,'09_Weathering',.008);o.rotation_euler.z=random.random()*math.tau
for x,y in [(-1.1,2.1),(1.8,1.2),(-3.3,-2.7),(.5,-2.3)]:
    o=box('Discarded research sheet',(x,y,.043),(.21,.28,.003),paper,'09_Weathering',0);o.rotation_euler.z=random.uniform(-1,1)
for y in [-5.4,-6.4]:hazard('Entry wing marking',(-5.05,y,.033),1.75,.13,False)
print('Pipes, equipment and weathering complete',flush=True)

# Mesh conversion keeps lettering, pipes, valves and cracks portable as model geometry.
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
convert=[o for o in root.children_recursive if o.type in {'CURVE','FONT'}]
for o in convert:o.select_set(True)
if convert:
    bpy.context.view_layer.objects.active=convert[0];bpy.ops.object.convert(target='MESH')
meshes=[o for o in root.children_recursive if o.type=='MESH']
for o in meshes:
    if not o.data.uv_layers:uv(o.data)
reference=bpy.data.images.load(REF,check_existing=True);reference.name='REFERENCE | abandoned basement laboratory';reference.use_fake_user=True;reference.pack()
readme=bpy.data.texts.new('ASSET_README')
readme.write('B1 abandoned underground laboratory.\nModeled from the supplied six-view concept, with image-based approximate dimensions.\nOpen cutaway scene with central experiment floor, elevated U catwalk, two stairs, safety railing, doors, pipe networks, observation booth, operator desks, shelving, gas storage and underside beams.\nCollections separate architecture, walkway, piping, fixtures, props and weathering. All visible geometry is mesh; installed fluorescent lights remain as scene lights.\nThe original reference is packed. Blender procedural materials require a separate Unity material conversion or texture baking step.\nNo colliders, navigation or gameplay scripts are included.\n')
for scr in bpy.data.screens:
    for a in scr.areas:
        if a.type=='VIEW_3D':
            s=a.spaces.active;s.region_3d.view_distance=24;s.region_3d.view_location=(0,0,1.7)
            s.region_3d.view_rotation=Vector((14,-18,15)).to_track_quat('Z','Y');s.shading.type='MATERIAL';s.overlay.show_extras=False
scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.11,.145,.18,1);bg.inputs[1].default_value=.38
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=OUT)
coords=[o.matrix_world@Vector(c) for o in meshes for c in o.bound_box]
report={'file':OUT,'mesh_objects':len(meshes),'base_polygons':sum(len(o.data.polygons) for o in meshes),'dimensions_m':[round(max(v[i] for v in coords)-min(v[i] for v in coords),3) for i in range(3)],'packed_reference':bool(reference.packed_file),'installed_lights':sum(o.type=='LIGHT' for o in root.children_recursive)}
with open(os.path.join(ART,'basement_lab_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
print(json.dumps(report),flush=True)

# Presentation rig is added only after saving; it is not part of the Unity asset.
studio=bpy.data.collections.new('TEMP_Presentation');scene.collection.children.link(studio)
def aim(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',cd);studio.objects.link(cam);scene.camera=cam
cd.type='ORTHO';cd.ortho_scale=21.7
cam.location=(16,-21,18);aim(cam,(0,-.35,1.65))
for name,loc,power,size,color in [('Large cool key',(-4,-7,13),4600,9,(.76,.87,1)),('Front fill',(9,-8,8),3600,8,(.78,.86,1)),('Overhead rim',(1,7,12),5600,7,(.74,.86,1)),('Warm spill',(-8,-4,6),1100,5,(1,.74,.45))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=loc;aim(o,(0,0,1))
groundmat=mat('TEMP studio ground',(.024,.033,.043),rough=.93)
ground=box('TEMP ground',(0,0,-.88),(200,200,.10),groundmat);ground.parent=None
for c in list(ground.users_collection):c.objects.unlink(ground)
studio.objects.link(ground)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1500;scene.render.resolution_y=1250;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(ART,'basement_lab_preview.png')
print('Rendering overall cutaway',flush=True);bpy.ops.render.render(write_still=True)
cam.location=(7,-11,8);aim(cam,(0,1.5,2.0));cd.ortho_scale=13.2
scene.render.resolution_x=1400;scene.render.resolution_y=1100
scene.render.filepath=os.path.join(ART,'basement_lab_detail.png')
print('Rendering equipment detail',flush=True);bpy.ops.render.render(write_still=True)
print('DONE',OUT,flush=True)
