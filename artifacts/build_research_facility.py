import bpy, math, random, os
from mathutils import Vector
random.seed(1983)
scene = bpy.data.scenes.new('Abandoned Research Facility')
bpy.context.window.scene = scene
collection = bpy.data.collections.new('Research Facility - building only')
scene.collection.children.link(collection)
root = bpy.data.objects.new('Research_Facility_220m', None)
collection.objects.link(root)
root['description'] = 'Reference-inspired abandoned research facility; exterior architectural model'
root['scale_note'] = '1 Blender unit = 1 metre; five main storeys'
materials = []

def material(name, color, rough=.85, metallic=0, weather=False):
    m = bpy.data.materials.new('Facility | ' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    n = m.node_tree.nodes; links = m.node_tree.links
    p = next(x for x in n if x.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metallic
    if weather:
        coord = n.new('ShaderNodeTexCoord')
        noise = n.new('ShaderNodeTexNoise')
        noise.inputs['Scale'].default_value = .75
        noise.inputs['Detail'].default_value = 5
        links.new(coord.outputs['Object'], noise.inputs['Vector'])
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = .2
        ramp.color_ramp.elements[0].color = (*(v*.30 for v in color), 1)
        ramp.color_ramp.elements[1].position = .8
        ramp.color_ramp.elements[1].color = (*(min(v*1.4, .9) for v in color), 1)
        links.new(noise.outputs['Fac'], ramp.inputs[0])
        links.new(ramp.outputs[0], p.inputs['Base Color'])
        fine = n.new('ShaderNodeTexNoise'); fine.inputs['Scale'].default_value = 32
        links.new(coord.outputs['Object'], fine.inputs['Vector'])
        bump = n.new('ShaderNodeBump'); bump.inputs['Strength'].default_value = .35
        bump.inputs['Distance'].default_value = .045
        links.new(fine.outputs['Fac'], bump.inputs['Height'])
        links.new(bump.outputs[0], p.inputs['Normal'])
    materials.append(m)
    return len(materials)-1

concrete = material('weathered limestone concrete', (.43,.405,.34), weather=True)
edge = material('spalled dark concrete', (.22,.225,.20), weather=True)
roofmat = material('old asphalt roof membrane', (.14,.15,.135), weather=True)
frame = material('oxidised window frames', (.23,.25,.225), .72, .55, True)
rust = material('rusted iron', (.25,.105,.038), .9, .4, True)
glass = [material('dirty glass '+str(i), c, .28, .40) for i,c in enumerate([
    (.055,.095,.105),(.10,.15,.16),(.16,.19,.18),(.08,.12,.13),(.23,.245,.21)])]
dark = material('unlit interiors and fractures', (.025,.032,.030))
stains = [material('rain stains '+str(i), c, weather=True) for i,c in enumerate([
    (.105,.115,.085),(.18,.17,.13),(.24,.235,.20)])]
moss = [material('moss '+str(i), c, weather=True) for i,c in enumerate([
    (.075,.13,.025),(.13,.19,.045),(.19,.235,.065),(.07,.11,.038)])]
paint = material('faded safety markings', (.48,.48,.37), weather=True)
metal = material('weathered HVAC steel', (.22,.25,.245), .75, .6, True)

class Batch:
    def __init__(self, name):
        self.name=name; self.v=[]; self.f=[]; self.mi=[]
    def poly(self, points, mat):
        i=len(self.v); self.v.extend(tuple(p) for p in points)
        self.f.append(tuple(range(i,i+len(points)))); self.mi.append(mat)
    def box(self, center, size, mat, angle=0):
        x,y,z=center; dx,dy,dz=(v/2 for v in size)
        c,s=math.cos(angle),math.sin(angle)
        v=[(x+c*u*dx-s*w*dy,y+s*u*dx+c*w*dy,z+t*dz)
           for u,w,t in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        for f in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]:
            self.poly([v[i] for i in f],mat)
    def tube(self,a,b,r,mat,n=8):
        a,b=Vector(a),Vector(b); axis=(b-a).normalized()
        u=axis.cross(Vector((0,0,1)) if abs(axis.z)<.9 else Vector((1,0,0))).normalized()
        v=axis.cross(u); rings=[]
        for p in (a,b):rings.append([p+r*(math.cos(i*math.tau/n)*u+math.sin(i*math.tau/n)*v) for i in range(n)])
        for i in range(n):self.poly([rings[0][i],rings[0][(i+1)%n],rings[1][(i+1)%n],rings[1][i]],mat)
        self.poly(rings[0][::-1],mat);self.poly(rings[1],mat)
    def finish(self):
        if not self.v:return
        me=bpy.data.meshes.new(self.name);me.from_pydata(self.v,[],self.f);me.update()
        for m in materials:me.materials.append(m)
        for p,mi in zip(me.polygons,self.mi):p.material_index=mi
        o=bpy.data.objects.new(self.name,me);collection.objects.link(o);o.parent=root
        return o

def point(rx,ry,a,z):return (rx*math.cos(a),ry*math.sin(a),z)

def arc_solid(batch,rx,ry,width,z,height,a0,a1,steps,mat):
    for j in range(steps):
        a=a0+(a1-a0)*j/steps;b=a0+(a1-a0)*(j+1)/steps
        v=[point(x,y,t,h) for h in (z,z+height) for x,y,t in
           [(rx,ry,a),(rx,ry,b),(rx-width,ry-width,b),(rx-width,ry-width,a)]]
        for f in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(2,3,7,6)]:batch.poly([v[i] for i in f],mat)
        if j==0:batch.poly([v[i] for i in (3,0,4,7)],mat)
        if j==steps-1:batch.poly([v[i] for i in (1,2,6,5)],mat)

def wall_patch(batch,rx,ry,a,z,width,height,mat,offset=.07):
    # Irregular perimeter with local angular coordinates, attached to curved wall.
    da=width/max(rx,ry)
    points=[]
    for i in range(14):
        t=i*math.tau/14;rr=random.uniform(.72,1.18)
        points.append(point(rx+offset,ry+offset,a+math.cos(t)*da*.5*rr,max(.04,z+math.sin(t)*height*.5*rr)))
    center=point(rx+offset+.008,ry+offset+.008,a,z)
    for i in range(len(points)):batch.poly([center,points[i],points[(i+1)%len(points)]],mat)

def make_wing(name,rx,ry,width,a0d,a1d,floors,bays):
    a0,a1=map(math.radians,(a0d,a1d));H=5.2;top=floors*H+1
    structure=Batch(name+' | floor slabs and concrete bands')
    windows=Batch(name+' | glazing and broken panes')
    frames=Batch(name+' | mullions and concrete piers')
    aging=Batch(name+' | peeling surfaces and rain streaks')
    green=Batch(name+' | facade moss')
    cracks=Batch(name+' | cracks and exposed rebar')
    roofs=Batch(name+' | roof and parapets')
    arc_solid(structure,rx,ry,width,0,1,a0,a1,bays,edge)
    for floor in range(floors):
        z=1+floor*H
        arc_solid(structure,rx,ry,width,z,.30,a0,a1,bays,concrete)
        for R,S,sign in ((rx,ry,1),(rx-width,ry-width,-1)):
            arc_solid(structure,R+.06,S+.06,.26,z+.30,1.22,a0,a1,bays,concrete)
            arc_solid(structure,R+.08,S+.08,.30,z+4.66,.54,a0,a1,bays,concrete)
            for j in range(bays):
                a=a0+(a1-a0)*j/bays;b=a0+(a1-a0)*(j+1)/bays;mid=(a+b)/2
                offs=.022*sign
                lo=z+1.54;hi=z+4.64
                corners=[point(R+offs,S+offs,t,h) for t,h in ((a,lo),(b,lo),(b,hi),(a,hi))]
                if random.random()<.055:
                    # Dark recess and jagged remnants, no full pane.
                    windows.poly([point(R-.22*sign,S-.22*sign,t,h) for t,h in ((a,lo),(b,lo),(b,hi),(a,hi))],dark)
                    windows.poly([corners[0],corners[1],point(R+offs,S+offs,b-(b-a)*.27,lo+.8)],glass[0])
                    windows.poly([corners[3],corners[2],point(R+offs,S+offs,mid,hi-.95)],glass[1])
                else:
                    windows.poly(corners,random.choice(glass))
                tangent=math.atan2(S*math.cos(a),-R*math.sin(a))
                p=point(R+.04*sign,S+.04*sign,a,(lo+hi)/2)
                frames.box(p,(.105,.14,hi-lo+.08),frame,tangent)
                p1=point(R+.07*sign,S+.07*sign,a,z+3.74);p2=point(R+.07*sign,S+.07*sign,b,z+3.74)
                frames.tube(p1,p2,.047,frame,4)
                if j%7==0:
                    frames.box(point(R+.10*sign,S+.10*sign,a,z+2.6),(.50,.40,5.2),concrete,tangent)
                if random.random()<.55:
                    # Narrow dirt trail below a sill, varied lengths and widths.
                    wall_patch(aging,R,S,mid,z+1.04,random.uniform(.16,1.4),random.uniform(.4,1.35),random.choice(stains),.092*sign)
                if random.random()<.16:
                    wall_patch(aging,R,S,mid,z+.83,random.uniform(.2,.65),random.uniform(.2,.7),edge,.10*sign)
                if random.random()<(.30 if floor==0 else .055):
                    wall_patch(green,R,S,mid,z+.72,random.uniform(.5,2.3),random.uniform(.6,1.55),random.choice(moss),.12*sign)
                if random.random()<.08:
                    aa=mid;zz=z+1.4
                    for k in range(5):
                        an=aa+random.uniform(-.0025,.0025);zn=zz-random.uniform(.08,.24)
                        cracks.tube(point(R+.14*sign,S+.14*sign,aa,zz),point(R+.14*sign,S+.14*sign,an,zn),.013,dark,4)
                        aa,zz=an,zn
    arc_solid(roofs,rx,ry,width,top,.28,a0,a1,bays,roofmat)
    for R,S in ((rx,ry),(rx-width+.36,ry-width+.36)):
        arc_solid(roofs,R,S,.38,top+.28,.9,a0,a1,bays,concrete)
        arc_solid(roofs,R+.08,S+.08,.54,top+1.18,.12,a0,a1,bays,edge)
    for j in range(bays):
        a=a0+(a1-a0)*(j+.5)/bays
        if j%3==0:
            roofs.tube(point(rx-1,ry-1,a,top+.30),point(rx-width+1,ry-width+1,a,top+.30),.025,edge,4)
        if random.random()<.65:
            R=rx-random.choice([.9,width-1.1]);S=ry-(rx-R)
            p=point(R,S,a,top+.31)
            for k in range(random.randint(2,5)):
                x=p[0]+random.uniform(-1,1);y=p[1]+random.uniform(-1,1);r=random.uniform(.25,1.1)
                verts=[(x+math.cos(i*math.tau/9)*r*random.uniform(.5,1.2),y+math.sin(i*math.tau/9)*r*random.uniform(.5,1.2),top+.315) for i in range(9)]
                green.poly(verts,random.choice(moss))
        if j%13==0:
            # Vertical rusty downpipes.
            p=point(rx+.35,ry+.35,a,top)
            cracks.tube(p,(p[0],p[1],.3),.095,rust,10)
            for z in (2,7,12,17,22):
                if z<top:frames.box((p[0],p[1],z),(.32,.32,.09),frame)
    # Solid end walls, with weathering and framed small windows.
    for a in (a0,a1):
        p=point(rx-width/2,ry-width/2,a,top/2)
        structure.box(p,(width,.45,top),concrete,a)
    for b in (structure,windows,frames,aging,green,cracks,roofs):b.finish()
    return top

main_top=make_wing('A - five-storey outer research wing',109,88,19,-60,240,5,180)
inner_top=make_wing('B - four-storey inner laboratories',70,51,13,5,225,4,110)
rotunda_top=make_wing('C - central circular research core',22,22,7,0,360,4,64)

# Rectangular connecting laboratories and entrances.
connect=Batch('D - enclosed bridges and entrance hall')
details=Batch('D - bridge glazing and doors')
def block_building(cx,cy,sx,sy,h):
    connect.box((cx,cy,h/2),(sx,sy,h),concrete)
    for side in (-1,1):
        for floor in range(int(h/5.2)):
            z=floor*5.2+3
            for j in range(max(1,int(sx/2.8))):
                x=cx-sx/2+1.4+j*2.8
                details.box((x,cy+side*(sy/2+.025),z),(2.45,.055,2.9),random.choice(glass))
                details.box((x,cy+side*(sy/2+.075),z+.4),(2.5,.065,.085),frame)
    connect.box((cx,cy,h+.15),(sx+.4,sy+.4,.3),roofmat)
    for y in (cy-sy/2,cy+sy/2):connect.box((cx,y,h+.65),(sx,.35,1),concrete)
    for x in (cx-sx/2,cx+sx/2):connect.box((x,cy,h+.65),(.35,sy,1),concrete)

block_building(0,48,22,30,21.8)
block_building(-75,0,27,10,16.6)
block_building(38,5,39,10,16.6)
# Front entrance sits between two open horseshoe ends.
block_building(0,-66,47,13,11.4)
for j in range(12):details.box((-20.5+j*3.7,-72.55,4.1),(3.3,.12,6.3),glass[j%len(glass)])
connect.box((0,-77,7.5),(29,9,.65),concrete)
for x in (-12,12):connect.box((x,-80,3.7),(.8,.8,7.4),edge)
for j in range(8):connect.box((0,-79-j*.75,.12*(8-j)),(31,1.5,.24*(8-j)),concrete)
for x in (-9,-3,3,9):details.box((x,-72.72,2.6),(2.3,.12,4.9),glass[0])
connect.finish();details.finish()

# Rooftop service blocks, helipad and worn technical plant.
plant=Batch('E - rooftop mechanical rooms')
equipment=Batch('E - HVAC ducts fans water tanks')
rusty=Batch('E - roof pipework and railings')
grime=Batch('E - roof stains moss and cracked surfacing')
def rooftop_room(x,y,w,d,z,h):
    plant.box((x,y,z+h/2),(w,d,h),concrete)
    plant.box((x,y,z+h+.18),(w+.6,d+.6,.36),roofmat)
    for xx in (x-w/2,x+w/2):plant.box((xx,y,z+h+.55),(.28,d,.75),concrete)
    for yy in (y-d/2,y+d/2):plant.box((x,yy,z+h+.55),(w,.28,.75),concrete)
    for i in range(max(1,int(w/3))):
        xx=x-w/2+1.5+3*i
        plant.box((xx,y-d/2-.04,z+h*.5),(1.6,.1,h*.55),dark)
        for j in range(8):equipment.box((xx,y-d/2-.12,z+h*.25+j*h*.055),(1.6,.10,.07),metal)
    for i in range(int(w)):
        if random.random()<.5:
            xx=x-w/2+i;ww=random.uniform(.15,.7);hh=random.uniform(.5,h*.8)
            grime.box((xx,y-d/2-.075,z+h-hh/2),(ww,.02,hh),random.choice(stains))

rooftop_room(-5,73,28,18,main_top+.3,5.4)
rooftop_room(70,39,23,14,main_top+.3,4.3)
rooftop_room(-62,42,20,12,main_top+.3,3.5)
rooftop_room(-19,37,16,10,inner_top+.3,3.2)
# Helipad platform above rear research wing.
plant.box((31,65,main_top+.65),(31,29,1.0),concrete)
plant.box((31,65,main_top+1.17),(30,28,.04),roofmat)
for i in range(100):
    a=i*math.tau/100;b=(i+.78)*math.tau/100
    equipment.poly([(31+r*math.cos(t),65+r*math.sin(t),main_top+1.20) for r,t in ((10,a),(10,b),(9.78,b),(9.78,a))],paint)
for x in (28,34):equipment.box((x,65,main_top+1.21),(.42,9,.025),paint)
equipment.box((31,65,main_top+1.21),(6,.42,.025),paint)

def hvac(x,y,z,w=3,d=2):
    equipment.box((x,y,z+.65),(w,d,1.3),metal)
    equipment.box((x,y,z+1.34),(w+.15,d+.15,.08),edge)
    for xx in (x-w*.25,x+w*.25):
        equipment.tube((xx,y,z+1.37),(xx,y,z+1.41),d*.36,dark,24)
        for i in range(8):
            a=i*math.tau/8
            rusty.tube((xx,y,z+1.45),(xx+math.cos(a)*d*.34,y+math.sin(a)*d*.34,z+1.45),.025,frame,5)
    for i in range(8):equipment.box((x,y-d/2-.04,z+.15+i*.13),(w-.2,.05,.045),dark)

for i in range(25):
    a=math.radians(-45+i*11);x,y,_=point(99,78,a,0)
    if 25<x<50 and y>48:continue
    hvac(x,y,main_top+.3,random.uniform(2.2,4),2.2)
    if i%3==0:
        equipment.box((x+2.8,y,main_top+.85),(3.6,.8,.8),metal)
        rusty.tube((x+1,y,main_top+.7),(x+6,y,main_top+.7),.14,rust,10)
for i in range(9):
    a=math.radians(20+i*24);x,y,_=point(63,44,a,0);hvac(x,y,inner_top+.3,2.5,1.8)
for x,y,z in [(-8,73,33),(0,73,33),(70,39,31.6)]:
    equipment.tube((x,y,z),(x,y,z+2.1),1.25,metal,32)
    equipment.tube((x,y,z+2.1),(x,y,z+2.3),1.35,edge,32)
    rusty.tube((x,y,z-1),(x,y,z+2.5),.09,rust,10)
# Roof communication masts and satellite receivers.
for x,y,z in [(-12,73,33),(67,39,31.5),(-62,42,31)]:
    rusty.tube((x,y,z),(x,y,z+5.6),.11,frame,12)
    for dz in (2,3.5,4.7):rusty.tube((x-1.4,y,z+dz),(x+1.4,y,z+dz),.035,frame,6)
    for dx,dy in [(-2,-1),(2,-1),(0,2)]:rusty.tube((x,y,z+3.2),(x+dx,y+dy,z),.018,rust,5)

# Additional roof cracking, puddle marks and patchy moss, all visible geometry.
for i in range(210):
    a=random.uniform(math.radians(-55),math.radians(235));inset=random.uniform(1.2,17.8)
    x,y,z=point(109-inset,88-inset,a,main_top+.31)
    if random.random()<.45:
        path=[(x,y,z)]
        for j in range(random.randint(3,7)):
            path.append((path[-1][0]+random.uniform(-.8,.8),path[-1][1]+random.uniform(.2,1.3),z))
        for p,q in zip(path,path[1:]):grime.tube(p,q,.025,dark,4)
    else:
        r=random.uniform(.5,2.5)
        verts=[(x+math.cos(j*math.tau/11)*r*random.uniform(.7,1.3),y+math.sin(j*math.tau/11)*r*random.uniform(.7,1.3),z+.015) for j in range(11)]
        grime.poly(verts,random.choice(moss if i%3==0 else stains))
plant.finish();equipment.finish();rusty.finish();grime.finish()

# Short ivy tendrils concentrated along lower wet north/west walls, not landscape.
ivy=Batch('F - clinging moss and ivy at damp wall joints')
for k in range(75):
    a=random.uniform(math.radians(80),math.radians(230));h=random.uniform(2,11)
    for j in range(int(h*5)):
        z=j*.2;a+=random.uniform(-.0012,.0012)
        p=point(109.18,88.18,a,z)
        q=point(109.18,88.18,a+random.uniform(-.002,.002),z+.25)
        ivy.tube(p,q,.018,moss[0],4)
        rr=random.uniform(.09,.23);side=random.choice((-1,1));b=a+side*.002
        pp=point(109.20,88.20,b,z+.08)
        tangent=Vector((-math.sin(a),math.cos(a),0))*rr
        center=Vector(pp)
        ivy.poly([center-tangent,center+Vector((0,0,rr*1.3)),center+tangent,center-Vector((0,0,rr*.7))],random.choice(moss))
ivy.finish()

# Asset metadata and uncluttered architectural presentation.
root['main_storeys']=5
root['weathering']='Geometric moss, ivy, cracks, spalls and runoff marks; procedural concrete microtexture'
root['scope']='Building exterior only; no terrain, trees, roads or vehicles; no finished interiors'
bpy.context.view_layer.update()
for ob in bpy.context.selected_objects:ob.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.clip_end=3000
            r=area.spaces.active.region_3d;r.view_location=(0,0,15);r.view_distance=290
            r.view_rotation=Vector((1,-1.5,1.3)).to_track_quat('Z','Y')
print('Created facility:',len(collection.objects),'objects;',sum(len(o.data.polygons) for o in collection.objects if o.type=='MESH'),'faces')

# FINAL ARCHITECTURAL DETAIL PASS
# Connect the entrance to the horseshoe ends and enclose the central core.
connect=Batch('G - entrance connecting wings')
details=Batch('G - connector windows and thresholds')
block_building(-37,-66,29,13,16.6)
block_building(37,-66,29,13,16.6)
block_building(0,27,12,22,16.6)
connect.finish();details.finish()
core=Batch('G - central research core roof')
core.tube((0,0,rotunda_top+.03),(0,0,rotunda_top+.29),15.1,roofmat,96)
core.box((0,0,rotunda_top+1.2),(14,12,1.8),concrete)
core.box((0,0,rotunda_top+2.13),(14.4,12.4,.12),metal)
for j in range(9):core.box((-6+j*1.5,0,rotunda_top+2.24),(.12,11,.09),edge)
core.finish()

# Larger, irregular damp growth is legible at facade viewing distances.
growth=Batch('H - layered facade moss colonies')
damage=Batch('H - deep rain trails and damaged concrete')
for rx,ry,width,a0d,a1d,floors in [(109,88,19,-60,240,5),(70,51,13,5,225,4),(22,22,7,0,360,4)]:
    for inner in (False,True):
        R=rx-(width if inner else 0);S=ry-(width if inner else 0);sgn=-1 if inner else 1
        for i in range(70 if rx>30 else 24):
            a=random.uniform(math.radians(a0d),math.radians(a1d))
            z=random.uniform(.3,1.1)
            wall_patch(growth,R,S,a,z,random.uniform(1.2,4.5),random.uniform(.5,1.5),random.choice(moss),.16*sgn)
            for j in range(random.randint(2,5)):
                wall_patch(growth,R,S,a+random.uniform(-.012,.012),z+random.uniform(-.2,.7),random.uniform(.3,1.2),random.uniform(.2,.6),random.choice(moss),.175*sgn)
        for floor in range(floors):
            z=1+5.2*floor
            for i in range(40 if rx>30 else 15):
                a=random.uniform(math.radians(a0d),math.radians(a1d));da=random.uniform(.001,.004)
                bottom=z+random.uniform(.30,.8);top=z+1.51
                damage.poly([point(R+.17*sgn,S+.17*sgn,a-da,top),point(R+.17*sgn,S+.17*sgn,a+da,top),point(R+.17*sgn,S+.17*sgn,a+da*.15,bottom),point(R+.17*sgn,S+.17*sgn,a-da*.20,bottom-.08)],random.choice(stains))
            if floor in (1,3):
                for i in range(10):
                    a=random.uniform(math.radians(a0d),math.radians(a1d))
                    wall_patch(growth,R,S,a,z+1.0,random.uniform(1,3.0),.7,random.choice(moss),.18*sgn)
growth.finish();damage.finish()

# Service-end walls get inset windows, fractured render and rain streaks.
ends=Batch('H - aged laboratory end walls')
for rx,ry,width,a0d,a1d,floors in [(109,88,19,-60,240,5),(70,51,13,5,225,4)]:
    for deg in (a0d,a1d):
        a=math.radians(deg);sgn=-1 if deg==a0d else 1
        u=Vector((math.cos(a),math.sin(a),0));n=Vector((-math.sin(a),math.cos(a),0))*sgn
        origin=Vector(point(rx-width/2,ry-width/2,a,0))+n*.26
        for floor in range(floors):
            for dx in (-5,0,5):
                c=origin+u*dx+Vector((0,0,3.4+floor*5.2))
                ends.box(c,(1.8,.10,2.7),random.choice(glass),a)
                ends.box(c+Vector((0,0,-1.4)),(2.05,.25,.15),edge,a)
                for j in range(3):
                    cc=c+u*random.uniform(-.9,.9)+n*.1+Vector((0,0,-2.0))
                    ends.box(cc,(random.uniform(.06,.25),.025,random.uniform(.6,1.2)),random.choice(stains),a)
        for i in range(14):
            start=origin+u*random.uniform(-8,8)+Vector((0,0,random.uniform(1,22)))+n*.10
            for j in range(5):
                end=start+u*random.uniform(-.35,.35)+Vector((0,0,-random.uniform(.12,.5)))
                ends.tube(start,end,.025,dark,4);start=end
ends.finish()

# Keep the aged surfaces broad and muted rather than speckled at building scale.
for m in materials:
    if not m.use_nodes:continue
    for n in m.node_tree.nodes:
        if n.type=='TEX_NOISE' and n.inputs['Scale'].default_value<2:
            n.inputs['Scale'].default_value=.19
            n.inputs['Detail'].default_value=3
        if n.type=='VALTORGB':
            base=m.diffuse_color[:3]
            n.color_ramp.elements[0].color=(*(v*.52 for v in base),1)
            n.color_ramp.elements[-1].color=(*(min(v*1.14,.8) for v in base),1)
bpy.context.view_layer.update()
print('Final detail pass complete:',len(collection.objects),'objects')
