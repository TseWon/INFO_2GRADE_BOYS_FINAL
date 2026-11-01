import bpy,bmesh,shutil,json,os
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
scene=bpy.data.scenes['Surface Research Station'];bpy.context.window.scene=scene
root=bpy.data.objects['SurfaceResearchStation']
backup=os.path.join(ROOT,'artifacts/backups/SurfaceResearchStation_before_normal_fix.blend')
if not os.path.exists(backup):shutil.copy2(bpy.data.filepath,backup)
rows=[]
for ob in scene.objects:
 if ob.type!='MESH' or ob.parent!=root or ob.name.startswith('LOD1_'):continue
 bm=bmesh.new();bm.from_mesh(ob.data);bm.normal_update()
 before=[f.normal.copy() for f in bm.faces]
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.normal_update()
 changed=sum(a.dot(f.normal)<0 for a,f in zip(before,bm.faces))
 if changed:rows.append({'mesh':ob.name,'faces_reoriented':changed})
 bm.to_mesh(ob.data);bm.free();ob.data.update()
with open(os.path.join(ROOT,'artifacts/surface_station/normal_repair.json'),'w') as f:json.dump(rows,f,indent=2)
print('NORMAL_REPAIR',len(rows),'meshes',sum(r['faces_reoriented'] for r in rows),'faces')
exec(compile(open(os.path.join(ROOT,'artifacts/export_surface_station.py'),encoding='utf-8').read(),'export_surface_station.py','exec'))
