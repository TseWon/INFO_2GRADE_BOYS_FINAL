import bpy,bmesh,math,json,ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

out=Path(__file__).resolve().parent
scene=bpy.data.scenes['Joint fabrication review - mm']
bpy.context.window.scene=scene
obj=bpy.data.objects['J3E']
assert abs(obj.dimensions.x-360)<.01
for v in obj.data.vertices:
    x=v.co.x
    if x>=315: v.co.x=x-270
    elif x>15: v.co.x=15+(x-15)*.1
obj.data.update();bpy.context.view_layer.update()
assert all(abs(a-b)<.01 for a,b in zip(obj.dimensions,(90,60,60)))
bm=bmesh.new();bm.from_mesh(obj.data)
assert all(e.is_manifold for e in bm.edges)
volume=bm.calc_volume(signed=False);bm.free()
seat=obj.ray_cast(Vector((76,11,0)),Vector((-1,0,0)),distance=100)
assert seat[0] and abs(seat[1].x-45)<.01
center=obj.location+Vector((30,-15,-15))
axes={'front':((1,0,0),(0,0,1),(0,-1,0)),'top':((1,0,0),(0,1,0),(0,0,1)),'side':((0,1,0),(0,0,1),(1,0,0))}
for key,direction in [('front',(0,-1,0)),('top',(0,0,1)),('side',(1,0,0)),('ISO',(1,-1,-.8))]:
    cam=bpy.data.objects['J3E_'+key]
    cam.location=center+Vector(direction).normalized()*1000
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=90*(2.1 if key=='ISO' else 1.35)
cam=bpy.data.objects['00_Overview'];center=Vector((95,-65,0))
cam.location=center+Vector((1,-1,-1)).normalized()*1000
cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=360
scene['extension']='J3E +X mouth75, stop45; standard mouth45, stop15; net compensation30mm'
for t in bpy.data.texts:
    if t.name.startswith('설계 조건'):
        val=t.as_string().replace('300mm','30mm').replace('345','75').replace('315','45')
        t.clear();t.write(val)
tree=ast.parse((out/'build_joint_models.py').read_text(encoding='utf-8'))
func=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='projection')
exec(compile(ast.Module(body=[func],type_ignores=[]),'projection','exec'))
data=json.loads((out/'model_geometry.json').read_text(encoding='utf-8'))
row=next(r for r in data if r['id']=='J3E')
row.update(title='3구 (1) - 30mm 연장형',dimensions_mm=[90,60,60],mouths_from_origin_mm=[75,45,45],volume_mm3=volume)
row['views']={k:projection(obj,a) for k,a in axes.items()}
obj.data.calc_loop_triangles()
if 'mesh_vertices' in row:row['mesh_vertices']=[list(v.co) for v in obj.data.vertices]
if 'mesh_triangles' in row:row['mesh_triangles']=[list(t.vertices) for t in obj.data.loop_triangles]
(out/'model_geometry.json').write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8')
(out/'validation.json').write_text(json.dumps([{k:v for k,v in r.items() if k not in ['views','mesh_vertices','mesh_triangles']} for r in data],ensure_ascii=False,indent=2),encoding='utf-8')
print('VERIFIED J3E: 90x60x60mm, mouth75, seat45, net extension30mm, closed mesh')
exec(compile((out/'render_joint_models.py').read_text(encoding='utf-8'),'render_joint_models.py','exec'))
