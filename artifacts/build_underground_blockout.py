"""Build separate, editable B1/B2 room blockouts from the supplied floor sketches."""
import bpy
import math
import os
import sys
from mathutils import Vector

ROOT = r"C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL"
OUT = os.path.join(ROOT, "Assets", "Blender", "UndergroundResearchFacility")
PREV = os.path.join(ROOT, "artifacts", "previews")
os.makedirs(OUT, exist_ok=True)
os.makedirs(PREV, exist_ok=True)

WALL_H = 3.7
DOOR_H = 2.7
WALL_T = 0.28
FLOOR_T = 0.25

def material(name, color, roughness=0.82, metallic=0.0, texture_scale=3.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    nodes = m.node_tree.nodes
    bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    tex = nodes.new('ShaderNodeTexNoise')
    tex.inputs['Scale'].default_value = texture_scale
    tex.inputs['Detail'].default_value = 2
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.075
    bump.inputs['Distance'].default_value = 0.04
    m.node_tree.links.new(tex.outputs['Fac'], bump.inputs['Height'])
    m.node_tree.links.new(bump.outputs['Normal'], bsdf.inputs['Normal'])
    return m

def init(name):
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for c in list(bpy.data.collections):
        if c.name != 'Collection':
            bpy.data.collections.remove(c)
    bpy.context.scene.name = name
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    global MAT
    MAT = {
        'floor': material('B1B2 | worn industrial floor', (.125,.142,.148), .88, .03, 7),
        'wall': material('B1B2 | aged cold concrete', (.08,.09,.094), .92, .02, 5),
        'edge': material('B1B2 | oxidized blue-grey steel', (.075,.099,.109), .68, .55, 9),
        'dark': material('B1B2 | blackened steel', (.035,.047,.051), .76, .50, 8),
        'yellow': material('B1B2 | faded safety yellow', (.394,.262,.037), .82, .15, 12),
        'glass': material('B1B2 | dirty observation glass', (.09,.24,.29), .24, .05, 5),
    }
    glass = MAT['glass']
    glass.diffuse_color = (.09, .24, .29, .38)
    shader = next(n for n in glass.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    shader.inputs['Alpha'].default_value = .38
    if 'Transmission Weight' in shader.inputs:
        shader.inputs['Transmission Weight'].default_value = .48
    if hasattr(glass, 'surface_render_method'):
        glass.surface_render_method = 'DITHERED'

def collection(name):
    c = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(c)
    return c

def cube(name, loc, size, mat, coll):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(MAT[mat])
    for c in list(o.users_collection): c.objects.unlink(o)
    coll.objects.link(o)
    return o

def beam(name,a,b,radius,mat,coll):
    a,b=Vector(a),Vector(b)
    mid=(a+b)/2
    bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=radius,depth=(b-a).length,location=mid)
    o=bpy.context.object;o.name=name
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    o.data.materials.append(MAT[mat])
    for c in list(o.users_collection): c.objects.unlink(o)
    coll.objects.link(o)
    return o

def wall(name, a, b, coll, opening=None):
    """Straight wall in XY with optional centered clear door interval."""
    a, b = Vector(a), Vector(b)
    d = b-a
    length = d.length
    angle = math.atan2(d.y, d.x)
    intervals = [(0,length)]
    if opening:
        middle, width = opening
        lo = max(0,middle-width/2)
        hi = min(length,middle+width/2)
        intervals = [(0,lo),(hi,length)]
    for n,(lo,hi) in enumerate(intervals):
        if hi-lo < .03: continue
        mid = a + d*((lo+hi)/(2*length))
        o = cube(f'{name} | wall {n}', (mid.x,mid.y,WALL_H/2), (hi-lo,WALL_T,WALL_H), 'wall', coll)
        o.rotation_euler.z = angle
    if opening:
        mid = a + d*(middle/length)
        o = cube(f'{name} | lintel', (mid.x,mid.y,(DOOR_H+WALL_H)/2), (width,WALL_T,WALL_H-DOOR_H), 'wall', coll)
        o.rotation_euler.z = angle
        sill = cube(f'{name} | safety threshold', (mid.x,mid.y,.008), (width,.08,.016), 'yellow', coll)
        sill.rotation_euler.z = angle

def rect(name, cx, cy, sx, sy, coll, doors=None, ceiling=None, skip=(), with_floor=True):
    doors=doors or {}
    floor = cube(f'{name} | floor', (cx,cy,-FLOOR_T/2), (sx,sy,FLOOR_T), 'floor', coll) if with_floor else None
    x0,x1=cx-sx/2,cx+sx/2
    y0,y1=cy-sy/2,cy+sy/2
    for side,a,b in [
        ('S',(x0,y0),(x1,y0)),('E',(x1,y0),(x1,y1)),
        ('N',(x1,y1),(x0,y1)),('W',(x0,y1),(x0,y0))]:
        if side in skip: continue
        op=doors.get(side)
        wall(f'{name} | {side}',a,b,coll,op)
    # Ceiling is a separate collection so later room edits need no booleans.
    if ceiling:
        cube(f'{name} | ceiling', (cx,cy,WALL_H+.10), (sx,sy,.20), 'dark', ceiling)
    return floor

def corridor(name, cx, cy, sx, sy, coll, axis='Y', ceiling=None, branch=None, side_doors=None):
    cube(f'{name} | floor', (cx,cy,-FLOOR_T/2), (sx,sy,FLOOR_T), 'floor', coll)
    if axis=='Y':
        x0,x1=cx-sx/2,cx+sx/2; y0,y1=cy-sy/2,cy+sy/2
        wall(f'{name} | west',(x0,y0),(x0,y1),coll)
        wall(f'{name} | east',(x1,y0),(x1,y1),coll,branch)
    else:
        x0,x1=cx-sx/2,cx+sx/2; y0,y1=cy-sy/2,cy+sy/2
        wall(f'{name} | south',(x0,y0),(x1,y0),coll,side_doors)
        wall(f'{name} | north',(x0,y1),(x1,y1),coll,side_doors)
    if ceiling:
        cube(f'{name} | ceiling',(cx,cy,WALL_H+.10),(sx,sy,.20),'dark',ceiling)

def hex_room(name,cx,cy,r,rotation,coll,open_edges=(),ceiling=None,solid_walls=True,floor_ring=False,axis_scale=(1,1)):
    verts=[Vector((cx+r*axis_scale[0]*math.cos(rotation+i*math.pi/3),
                   cy+r*axis_scale[1]*math.sin(rotation+i*math.pi/3))) for i in range(6)]
    mesh=bpy.data.meshes.new(f'{name} floor mesh')
    if floor_ring:
        inner=[Vector((cx+r*.53*math.cos(rotation+i*math.pi/3),cy+r*.53*math.sin(rotation+i*math.pi/3))) for i in range(6)]
        coords=[(v.x,v.y,z) for ring,z in ((verts,-FLOOR_T),(verts,0),(inner,-FLOOR_T),(inner,0)) for v in ring]
        faces=[]
        for i in range(6):
            j=(i+1)%6
            faces.extend([(6+i,6+j,18+j,18+i),(i,j,6+j,6+i),
                          (12+j,12+i,18+i,18+j),(12+i,12+j,j,i)])
        mesh.from_pydata(coords,[],faces)
    else:
        mesh.from_pydata([(v.x,v.y,-FLOOR_T) for v in verts]+[(v.x,v.y,0) for v in verts],[],
                         [tuple(range(6,12)),tuple(range(5,-1,-1))]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)])
    o=bpy.data.objects.new(f'{name} | hex floor',mesh);coll.objects.link(o);o.data.materials.append(MAT['floor'])
    for i in range(6):
        a,b=verts[i],verts[(i+1)%6]
        if solid_walls:
            wall(f'{name} | facet {i+1}',a,b,coll,((b-a).length/2,3.2) if i in open_edges else None)
        mid=(a+b)/2
        # A simple structural edge distinguishes containment from standard rooms.
        post=cube(f'{name} | corner post {i+1}',(a.x,a.y,WALL_H/2),(.25,.25,WALL_H),'edge',coll)
    if ceiling:
        cm=bpy.data.meshes.new(f'{name} ceiling mesh')
        cm.from_pydata([(v.x,v.y,WALL_H+.15) for v in verts],[],[tuple(range(6))])
        co=bpy.data.objects.new(f'{name} | ceiling',cm);ceiling.objects.link(co);co.data.materials.append(MAT['dark'])

def stair_run(name,cx,y_start,width,run,rise,steps,coll,axis='Y'):
    depth=run/steps
    for i in range(steps):
        h=rise*(i+1)/steps
        if axis=='Y':
            cube(f'{name} | step {i+1:02d}',(cx,y_start+(i+.5)*depth,h/2),(width,depth,h),'edge',coll)
        else:
            cube(f'{name} | step {i+1:02d}',(cx+(i+.5)*depth,y_start,h/2),(depth,width,h),'edge',coll)

def descending_stairs(name,cx,y_start,width,run,drop,steps,coll):
    depth=run/steps
    for i in range(steps):
        z=-drop*(i+1)/steps
        cube(f'{name} | descending tread {i+1:02d}',
             (cx,y_start+(i+.5)*depth,z-.09),(width,depth,.18),'edge',coll)

def wall_with_openings(name,x0,x1,y,centers,width,coll):
    gaps=sorted((c-width/2,c+width/2) for c in centers)
    last=x0
    for i,(lo,hi) in enumerate(gaps):
        if lo>last:
            cube(f'{name} | solid span {i}',((last+lo)/2,y,WALL_H/2),(lo-last,WALL_T,WALL_H),'wall',coll)
        cube(f'{name} | lintel {i}',((lo+hi)/2,y,(DOOR_H+WALL_H)/2),(hi-lo,WALL_T,WALL_H-DOOR_H),'wall',coll)
        last=hi
    if last<x1:
        cube(f'{name} | solid end',((last+x1)/2,y,WALL_H/2),(x1-last,WALL_T,WALL_H),'wall',coll)

def elevator_shaft(name,cx,cy,coll):
    """A full vertical passage around the 7 x 7 m lift deck."""
    cube(f'{name} | lower shaft pit',(cx,cy,-3.35),(6.3,6.3,.25),'dark',coll)
    for sx in (-1,1):
        for sy in (-1,1):
            cube(f'{name} | vertical guide {sx:+d} {sy:+d}',
                 (cx+sx*3.2,cy+sy*3.2,1.15),(.18,.18,8.9),'edge',coll)
    for z in (-3.15,5.55):
        for sx in (-1,1):
            cube(f'{name} | shaft side frame {sx:+d} z{z}',
                 (cx+sx*3.2,cy,z),( .14,6.4,.16),'edge',coll)

def polygon_envelope(name,points,coll,doors=None,skip_edges=()):
    """Continuous exterior corridor skin around several inner rooms."""
    mesh=bpy.data.meshes.new(f'{name} floor mesh')
    mesh.from_pydata([(x,y,-.02) for x,y in points],[],[tuple(range(len(points)))])
    o=bpy.data.objects.new(f'{name} | lower walkway floor',mesh)
    coll.objects.link(o);o.data.materials.append(MAT['floor'])
    doors=doors or {}
    for i,a in enumerate(points):
        if i in skip_edges:
            continue
        b=points[(i+1)%len(points)]
        length=(Vector(b)-Vector(a)).length
        wall(f'{name} | boundary {i+1}',a,b,coll,(length/2,doors[i]) if i in doors else None)

def glass_barrier(name,x,y0,y1,coll):
    length=y1-y0;yc=(y0+y1)/2
    cube(f'{name} | lower sill',(x,yc,.45),(.18,length,.9),'wall',coll)
    cube(f'{name} | upper beam',(x,yc,3.35),(.18,length,.7),'wall',coll)
    cube(f'{name} | full observation pane',(x,yc,1.9),(.07,length-.20,2.0),'glass',coll)
    for yy in (y0,y1):
        cube(f'{name} | steel jamb {yy:+.2f}',(x,yy,1.9),(.18,.18,2.1),'edge',coll)
    # Divide long observation walls into human-scale panes.
    if length > 6:
        pane_count=4
        for i in range(1,pane_count):
            yy=y0+length*i/pane_count
            cube(f'{name} | glazing mullion {i}',(x,yy,1.9),(.18,.12,2.1),'edge',coll)

def glazed_partition(name,x,y0,y1,coll):
    """Doorway in the middle, observation panes on both sides."""
    gap=3.2
    yc=(y0+y1)/2
    cube(f'{name} | door lintel',(x,yc,(DOOR_H+WALL_H)/2),(WALL_T,gap,WALL_H-DOOR_H),'wall',coll)
    cube(f'{name} | door threshold',(x,yc,.01),(.12,gap,.02),'yellow',coll)
    # Separate leaves can later be pivoted or replaced with an animated Unity door.
    for side,dy in [('left',-.79),('right',.79)]:
        cube(f'{name} | {side} door leaf',(x,dy,DOOR_H/2),(.12,1.52,DOOR_H),'edge',coll)
        cube(f'{name} | {side} door handle',(x-.09,dy+(.55 if side=='left' else -.55),1.18),(.09,.08,.22),'yellow',coll)
    for side,a,b in [('left',y0,yc-gap/2),('right',yc+gap/2,y1)]:
        length=b-a; mid=(a+b)/2
        if length<.2: continue
        cube(f'{name} | {side} lower wall',(x,mid,.42),(WALL_T,length,.84),'wall',coll)
        cube(f'{name} | {side} upper wall',(x,mid,3.35),(WALL_T,length,.7),'wall',coll)
        cube(f'{name} | {side} observation glass',(x,mid,1.92),(.07,max(.1,length-.18),2.14),'glass',coll)
        for yy in (a,b):
            cube(f'{name} | {side} steel jamb',(x,yy,1.92),(.16,.14,2.18),'edge',coll)

def hex_cage_lattice(name,cx,cy,r,rotation,coll,open_edges=()):
    """Sparse crossed-metal cage shell; the outer hex room stays walkable around it."""
    vertices=[Vector((cx+r*math.cos(rotation+i*math.pi/3),cy+r*math.sin(rotation+i*math.pi/3))) for i in range(6)]
    for i in range(6):
        a,b=vertices[i],vertices[(i+1)%6]
        d=b-a; length=d.length
        angle=math.atan2(d.y,d.x)
        for j in range(7):
            if i in open_edges and abs(j/6-.5)*length < 1.6: continue
            p=a+d*(j/6)
            cube(f'{name} | facet {i+1} bar {j+1}',(p.x,p.y,WALL_H/2),(.075,.075,WALL_H),'edge',coll)
        for z in (.15,1.85,3.55):
            mid=(a+b)/2
            if i in open_edges and z < DOOR_H:
                for side,start,end in [('left',0,length/2-1.6),('right',length/2+1.6,length)]:
                    part=a+d*((start+end)/(2*length))
                    rail=cube(f'{name} | facet {i+1} {side} rail {z}',(part.x,part.y,z),(end-start,.07,.07),'dark',coll)
                    rail.rotation_euler.z=angle
            else:
                rail=cube(f'{name} | facet {i+1} rail {z}',(mid.x,mid.y,z),(length,.07,.07),'dark',coll)
                rail.rotation_euler.z=angle
        if i not in open_edges:
            for start,end in [((a.x,a.y,.35),(b.x,b.y,3.3)),((a.x,a.y,3.3),(b.x,b.y,.35))]:
                beam(f'{name} | facet {i+1} crossed brace',start,end,.028,'edge',coll)

def cage_crown(name,cx,cy,r,z0,height,coll):
    """Arched birdcage crown matching the exterior station's raised hex cage."""
    rib_count=12
    segments=9
    for i in range(rib_count):
        angle=2*math.pi*i/rib_count
        points=[]
        for j in range(segments+1):
            t=j/segments
            rr=r*math.cos(t*math.pi/2)
            z=z0+height*math.sin(t*math.pi/2)
            points.append((cx+rr*math.cos(angle),cy+rr*math.sin(angle),z))
        for j in range(segments):
            beam(f'{name} | arched rib {i+1:02d} segment {j+1:02d}',points[j],points[j+1],.035,'edge',coll)
    for t in (.0,.34,.67):
        rr=r*math.cos(t*math.pi/2)
        z=z0+height*math.sin(t*math.pi/2)
        for j in range(24):
            a=2*math.pi*j/24;b=2*math.pi*(j+1)/24
            beam(f'{name} | crown ring {t:.2f} {j+1:02d}',
                 (cx+rr*math.cos(a),cy+rr*math.sin(a),z),
                 (cx+rr*math.cos(b),cy+rr*math.sin(b),z),.032,'dark',coll)

def stage_view(name, target, radius, target_z=0):
    scene=bpy.context.scene
    # Keep ceilings in the file for game construction but hide them for layout review.
    ceilings=bpy.data.collections.get('90_Ceilings_for_later_use')
    if ceilings: ceilings.hide_render=True; ceilings.hide_viewport=True
    bpy.ops.object.camera_add(location=(target[0]+radius*.64,target[1]-radius*.77,target_z+radius*.75))
    cam=bpy.context.object; cam.name='REVIEW | orthographic camera'
    direction=Vector((target[0],target[1],target_z))-cam.location
    cam.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=radius*1.42
    scene.camera=cam
    bpy.ops.object.light_add(type='AREA',location=(target[0]-12,target[1]-8,24))
    bpy.context.object.name='REVIEW | broad area light';bpy.context.object.data.energy=3000;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=30
    scene.render.engine='BLENDER_WORKBENCH'
    scene.display.shading.light='STUDIO'
    scene.display.shading.color_type='MATERIAL'
    scene.display.shading.show_cavity=True
    scene.display.shading.cavity_type='BOTH'
    scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=os.path.join(PREV,name+'.png')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,name+'.blend'))
    bpy.ops.render.render(write_still=True)
    print('SAVED',scene.render.filepath)
    # A second plan image makes doorway and corridor relationships reviewable.
    bounds=[]
    for o in scene.objects:
        if o.type != 'MESH': continue
        bounds.extend((o.matrix_world @ Vector(corner)) for corner in o.bound_box)
    xmin=min(p.x for p in bounds);xmax=max(p.x for p in bounds)
    ymin=min(p.y for p in bounds);ymax=max(p.y for p in bounds)
    cam.location=((xmin+xmax)/2,(ymin+ymax)/2,100)
    cam.rotation_euler=(0,0,0)
    cam.data.ortho_scale=max((xmax-xmin)*1.12,(ymax-ymin)*1.12*1400/900)
    scene.render.filepath=os.path.join(PREV,name+'_Plan.png')
    bpy.ops.render.render(write_still=True)

if '--only-b2' not in sys.argv:
    init('Underground B1 | Case 1')
    structure=collection('10_B1_Rooms_and_Corridors')
    ceilings=collection('90_Ceilings_for_later_use')
    hex_room('B1 bare hexagonal containment room',0,-19,5.4,0,structure,(1,),ceilings)
    corridor('B1 cage to elevator long corridor',0,-11.9,3.6,4.85,structure,'Y',ceilings)
    rect('B1 7m elevator passage',0,-5.975,7,7,structure,{'N':(3.5,3.2),'S':(3.5,3.2)})
    elevator_shaft('B1 7m elevator vertical shaft',0,-5.975,structure)
    corridor('B1 elevator to research long corridor',0,2.2625,3.6,9.475,structure,'Y',ceilings)
    rect('B1 enlarged research hall',0,23,36,32,structure,{'S':(18,3.2)},ceilings,skip=('N',))
    wall_with_openings('B1 three staircase entrances',-18,18,39,(-5.2,0,5.2),3.3,structure)
    rect('B1 three lane stair hall',0,44.5,16,11,structure,ceiling=ceilings,skip=('S',),with_floor=False)
    for x in (-2.55,2.55):
        wall(f'B1 stair lane divider {x:+.2f}',(x,39),(x,50),structure)
    cube('B1 central stair landing',(0,44.5,-.12),(4.8,11,.24),'floor',structure)
    stair_run('B1 central upward staircase',0,39,3.2,10.5,3.2,12,structure)
    descending_stairs('B1 left downward staircase',-5.2,39,3.2,10.5,3.2,12,structure)
    descending_stairs('B1 right downward staircase',5.2,39,3.2,10.5,3.2,12,structure)
    for x in (-5.2,5.2):
        cube(f'B1 lower stair landing {x:+.1f}',(x,49.7,-3.32),(3.2,.6,.24),'floor',structure)
        for dx in (-1.8,1.8):
            cube(f'B1 lower stair retaining wall {x:+.1f} {dx:+.1f}',
                 (x+dx,44.5,-1.6),(.18,11,3.2),'wall',structure)
    stage_view('Underground_B1_Blockout',(0,12),60)

init('Underground B2 | Case 2')
structure=collection('20_B2_Rooms_and_Corridors')
ceilings=collection('90_Ceilings_for_later_use')
polygon_envelope('B2 continuous outside corridor',
    [(-31.5,-3.8),(-28.5,-10.5),(10.8,-10.5),(10.8,10.5),(-28.5,10.5),(-31.5,3.8)],
    structure,{5:3.6},skip_edges=(2,))
# SoloScene1 uses the station FBX at unit scale.  Its hex cage footing measures
# 10.8087 x 9.3877 m.  Rotate the plan hex for flat side doorways while keeping
# exactly that Unity footprint and almost the same floor area.
unity_cage_width=10.8087
unity_cage_depth=9.3877
hex_radius=5.4
hex_axis_scale=(unity_cage_width/(math.sqrt(3)*hex_radius),unity_cage_depth/(2*hex_radius))
hex_room('B2 Unity-sized bare hexagonal room',-21,0,hex_radius,math.pi/6,
         structure,(2,5),ceilings,axis_scale=hex_axis_scale)
rect('B2 7m elevator passage',-40,0,7,7,structure,{'E':(3.5,3.6)})
elevator_shaft('B2 7m elevator vertical shaft',-40,0,structure)
corridor('B2 elevator to cage with perimeter doors',-31.4522,0,10.0957,3.6,
         structure,'X',ceilings,side_doors=(7.35,2.2))
corridor('B2 widened cage experiment passage',-10.8978,0,9.3957,5.2,structure,'X',ceilings)
rect('B2 experiment room',.8,0,14,14,structure,ceiling=ceilings,skip=('W',))
glazed_partition('B2 cage to experiment viewing wall',-6.2,-7,7,structure)
cube('B2 open junction between separate rooms | floor',
     (11.3,0,-FLOOR_T/2),(7,14,FLOOR_T),'floor',structure)
for side in (-1,1):
    label='south' if side < 0 else 'north'
    y=side*8.75
    cube(f'B2 {label} side corridor continuation | floor',
         (12.8,y,-.145),(4,3.5,.25),'floor',structure)
    cube(f'B2 {label} side corridor continuation | outer wall',
         (19.8,side*10.5,1.85),(18,.28,3.7),'wall',structure)
rect('B2 equally sized specimen escape room',21.8,0,14,21,structure,
     {'E':(10.5,16)},ceilings,skip=('W','N','S'))
corridor('B2 stair passage',30.3,0,3,16,structure,'X',ceilings)
rect('B2 emergency stair',35.8,0,8,16,structure,{'W':(8,16)},ceilings)
stair_run('B2 staircase',31.8,0,16,8,3.2,12,structure,axis='X')
stage_view('Underground_B2_Blockout',(0,0),82)
