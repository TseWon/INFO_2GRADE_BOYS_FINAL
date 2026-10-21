import bpy, math, json, csv
from pathlib import Path
from collections import Counter
from mathutils import Vector

OUT = Path(__file__).resolve().parent
OUT.mkdir(parents=True, exist_ok=True)
scene = bpy.data.scenes.new('공포게임 부스 - 전체 부품 사용')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
collections = {}
for name in ['01_Pipes_29', '02_Joints_18', '03_Annotations', '04_Context', '05_Cameras_Lights']:
    coll = bpy.data.collections.new(name)
    scene.collection.children.link(coll)
    collections[name] = coll

def move(obj, group):
    for c in list(obj.users_collection): c.objects.unlink(obj)
    collections[group].objects.link(obj)
    return obj

def material(name, color, metallic=0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    bs = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metallic
    bs.inputs['Roughness'].default_value = .38
    return mat

m2 = material('2m | Blue', (.035,.28,.66), .25)
m15 = material('1.5m | Teal', (.02,.58,.42), .2)
jm = {2:material('2-way | Purple',(.48,.16,.66)), 3:material('3-way | Ivory',(.88,.88,.78)), 4:material('4-way | Orange',(1,.32,.035))}
ink = material('Ink',(.035,.065,.10))
red = material('Wi-Fi zone',(.56,.15,.18))
floor_mat = material('Background',(.82,.86,.9))
pad = material('Booth floor',(.64,.73,.8))

def cylinder(name, a, b, radius, mat, group):
    a,b = Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=radius, depth=(b-a).length, location=(a+b)/2)
    obj=bpy.context.object
    obj.name=name
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    obj.data.materials.append(mat)
    for p in obj.data.polygons: p.use_smooth=True
    return move(obj,group)

def box(name, loc, dims, mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    obj=bpy.context.object; obj.name=name; obj.dimensions=dims
    obj.data.materials.append(mat)
    return move(obj,'04_Context')

font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
def label(name, body, loc, size=.16, mat=ink):
    data=bpy.data.curves.new(name,'FONT'); data.body=body; data.font=font
    data.size=size; data.align_x='CENTER'; data.align_y='CENTER'
    obj=bpy.data.objects.new(name,data); collections['03_Annotations'].objects.link(obj)
    obj.location=loc; data.materials.append(mat)
    return obj

# Plan: top row A--B--C, middle D--E--F [OPEN] G, bottom H--I.
xy={'A':(0,1.5),'B':(2,1.5),'C':(4,1.5),'D':(0,0),'E':(2,0),'F':(4,0),'G':(5.5,0),'H':(4,-1.5),'I':(5.5,-1.5)}
plan=[('A','B'),('B','C'),('D','E'),('E','F'),('A','D'),('B','E'),('C','F'),('F','H'),('G','I'),('H','I')]
nodes={f'{k}{level}':Vector((x,y,z)) for level,z in [('L',0),('U',2)] for k,(x,y) in xy.items()}
edges=[(a+l,b+l) for l in ['L','U'] for a,b in plan]+[(k+'L',k+'U') for k in xy]
adj={k:[] for k in nodes}
for a,b in edges: adj[a].append(b); adj[b].append(a)
assert Counter(len(v) for v in adj.values())=={2:2,3:10,4:6}
assert Counter(round((nodes[b]-nodes[a]).length,3) for a,b in edges)=={2:17,1.5:12}
visited=set(); stack=['AL']
while stack:
    n=stack.pop()
    if n not in visited: visited.add(n); stack.extend(adj[n])
assert len(visited)==18

pipe_rows=[]
for idx,(a,b) in enumerate(edges,1):
    length=round((nodes[b]-nodes[a]).length,3)
    name=f'P{idx:02d}_{length}m_{a}_{b}'
    obj=cylinder(name,nodes[a],nodes[b],.023,m2 if length==2 else m15,'01_Pipes_29')
    obj['stock_length_m']=length; obj['end_a']=a; obj['end_b']=b
    pipe_rows.append({'ID':f'P{idx:02d}','length_m':length,'end_a':a,'end_b':b})

def joint(name, center, dirs, mat):
    # Single mesh per joint, containing a hub and hollow socket sleeves.
    verts=[]; faces=[]
    for d in dirs:
        d=Vector(d); u=d.cross(Vector((0,0,1)))
        if u.length < .01: u=d.cross(Vector((0,1,0)))
        u.normalize(); v=d.cross(u).normalized(); base=len(verts)
        for dist,r in [(0,.040),(.105,.040),(.105,.026),(0,.026)]:
            for j in range(24):
                t=2*math.pi*j/24
                verts.append(center+d*dist+r*(math.cos(t)*u+math.sin(t)*v))
        for ring in range(4):
            for j in range(24):
                faces.append((base+ring*24+j,base+ring*24+(j+1)%24,base+((ring+1)%4)*24+(j+1)%24,base+((ring+1)%4)*24+j))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); collections['02_Joints_18'].objects.link(obj)
    mesh.materials.append(mat)
    obj['socket_count']=len(dirs); obj['connected_nodes']=', '.join(adj[name.split('_')[-1]])
    return obj

joint_rows=[]
for n,others in adj.items():
    dirs=[(nodes[o]-nodes[n]).normalized() for o in others]
    degree=len(dirs)
    dots=[round(dirs[i].dot(dirs[j]),4) for i in range(degree) for j in range(i)]
    assert all(d in [0,-1] for d in dots)
    assert dots.count(-1)==(1 if degree==4 else 0), (n,dots)
    joint(f'J{degree}_{n}',nodes[n],dirs,jm[degree])
    joint_rows.append({'node':n,'joint_way':degree,'connections':','.join(others)})

box('Ground',(2.75,0,-.09),(7.5,5.3,.08),floor_mat)
for name,loc,dims in [('Booth1',(1,.75,-.035),(1.9,1.4,.025)),('Booth2',(3,.75,-.035),(1.9,1.4,.025)),('Booth3',(4.75,-.75,-.035),(1.4,1.4,.025))]: box(name,loc,dims,pad)
box('WiFi room - schematic only',(4.75,.75,-.025),(1.38,1.38,.02),red)
label('B1','부스 1\n2m × 1.5m',(1,.75,.01),.23)
label('B2','부스 2\n2m × 1.5m',(3,.75,.01),.23)
label('B3','부스 3\n1.5m × 1.5m',(4.75,-.75,.01),.20)
label('Wifi','와이파이실\n위치 표시 / 실측 아님',(4.75,.75,.01),.16,jm[3])
label('Open','이 면은 상·하 가로대 없음',(4.8,.12,.025),.105,red)
label('Title','공포게임 부스 | 전체 부품 사용안',(2.75,2.15,.01),.25)
label('Legend','파랑 2m × 17   ·   초록 1.5m × 12   ·   주황 4구 × 6',(2.75,-2.15,.01),.16)
for k,(x,y) in xy.items():
    label('Node_'+k,k,(x-.13,y-.14,2.13),.15)

scene.world=bpy.data.worlds.new('Booth World'); scene.world.use_nodes=True
next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.7,.78,.88,1)
next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND').inputs[1].default_value=.7
ld=bpy.data.lights.new('Softbox','AREA'); ld.energy=1900; ld.shape='DISK'; ld.size=8
lo=bpy.data.objects.new('Softbox',ld); collections['05_Cameras_Lights'].objects.link(lo); lo.location=(1,-3,8)
def camera(name,pos,target,scale):
    data=bpy.data.cameras.new(name); data.type='ORTHO'; data.ortho_scale=scale
    obj=bpy.data.objects.new(name,data); collections['05_Cameras_Lights'].objects.link(obj)
    obj.location=pos; obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj
cam=camera('01_Assembly_3D',(8,-10,10),(2.7,.1,.5),9)
top=camera('02_Plan',(2.75,0,12),(2.75,0,0),7.7)
scene.camera=cam
scene.render.engine='CYCLES'; scene.cycles.samples=24
scene.render.resolution_x=1500; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene['design_note']='Nominal pipe centerline model. Actual outside dimensions depend on connector insertion depth. F-G top and bottom intentionally OPEN. Joint 3-way assumed orthogonal corner; 2-way assumed 90-degree elbow. No structural certification.'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.shading.color_type='MATERIAL'

for filename,rows in [('pipes.csv',pipe_rows),('joints.csv',joint_rows)]:
    with (OUT/filename).open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)
report={'pipes':dict(Counter(str(r['length_m']) for r in pipe_rows)),'joints':dict(Counter(str(r['joint_way']) for r in joint_rows)),'connected_nodes':len(visited),'joint_direction_check':'passed: 4-way = orthogonal T plus perpendicular socket','open_side':'F-G both upper and lower','nominal_dimensions_m':{'booth_1':[2,1.5,2],'booth_2':[2,1.5,2],'booth_3':[1.5,1.5,2]}}
(OUT/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
text=bpy.data.texts.new('READ_ME'); text.write(scene['design_note'])
font.pack()
# Write only this new scene and its dependencies; existing user scenes are untouched.
bpy.data.libraries.write(str(OUT/'horror_booths.blend'),{scene,text},fake_user=True)
scene.render.filepath=str(OUT/'booths_3d.png')
print(json.dumps(report,ensure_ascii=False))
print('SAVED',OUT/'horror_booths.blend')
