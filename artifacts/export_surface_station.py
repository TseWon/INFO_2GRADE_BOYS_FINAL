import bpy,bmesh,os,json,math,contextlib,io
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL';OUT=os.path.join(ROOT,'Assets/Blender/SurfaceResearchStation');EX=os.path.join(ROOT,'Assets/Environment/SurfaceResearchStation/Models');ART=os.path.join(ROOT,'artifacts/surface_station')
os.makedirs(EX,exist_ok=True)
scene=bpy.data.scenes['Surface Research Station'];bpy.context.window.scene=scene;root=bpy.data.objects['SurfaceResearchStation']
oldlod=bpy.data.collections.get('SR_LOD1 - export only')
if oldlod:
 for o in list(oldlod.objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(oldlod)
meshes=[o for o in scene.objects if o.type=='MESH' and o.parent==root];assert meshes
report={'name':'Surface Research Station','units':'meters','reference_interpretation':'Compact single-storey surface facility inferred from door-scale; no reliable numerical blueprint scale.','meshes':[],'lods':[],'limitations':['Exterior architecture and closed doors; underground level and furnished playable interiors are not implemented.','No Unity Scene1 replacement, collider integration or play-mode validation performed.']}
points=[];triangles=0
for o in meshes:
 o.data.calc_loop_triangles();count=len(o.data.loop_triangles);triangles+=count;points.extend(o.matrix_world@Vector(p) for p in o.bound_box)
 invalid=sum(not all(math.isfinite(c) for c in v.co) for v in o.data.vertices);assert invalid==0
 assert o.data.uv_layers and len(o.data.uv_layers.active.data)==len(o.data.loops)
 zero=sum(t.area<1e-10 for t in o.data.loop_triangles)
 report['meshes'].append({'name':o.name,'vertices':len(o.data.vertices),'triangles':count,'uv':True,'invalid_vertices':invalid,'zero_area_triangles':zero})
report['bounds_min']=[min(p[k] for p in points) for k in range(3)];report['bounds_max']=[max(p[k] for p in points) for k in range(3)];report['dimensions_m']=[report['bounds_max'][k]-report['bounds_min'][k] for k in range(3)];report['triangles']=triangles
materials={m for o in meshes for m in o.data.materials};report['material_count']=len(materials);imagepaths={};manifest=[]
for m in sorted(materials,key=lambda m:m.name):
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');maps={}
 for suffix in ['BaseColor','Normal','Roughness','MetallicSmoothness']:
  filename=m.name.removeprefix('SR_')+'_'+suffix+'.png';path=os.path.join(OUT,'Textures',filename)
  if os.path.isfile(path):maps[suffix]='Textures/'+filename
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE' and n.image:
   suffix=next((t for t in ['BaseColor','Normal','Roughness'] if t in n.image.name),None)
   if suffix:
    filename=m.name.removeprefix('SR_')+'_'+suffix+'.png';path=os.path.join(OUT,'Textures',filename);assert os.path.isfile(path);n.image.filepath=path;imagepaths[n.image]=filename
 manifest.append({'name':m.name,'base_color':list(p.inputs['Base Color'].default_value),'metallic':p.inputs['Metallic'].default_value,'roughness':p.inputs['Roughness'].default_value,'maps':maps})
def select(objects):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=root
def export(objects,filename):
 select([root]+objects)
 with contextlib.redirect_stdout(io.StringIO()):bpy.ops.export_scene.fbx(filepath=os.path.join(EX,filename),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='RELATIVE',use_mesh_modifiers=True)
 count=0
 for o in objects:o.data.calc_loop_triangles();count+=len(o.data.loop_triangles)
 report['lods'].append({'file':filename,'triangles':count,'mesh_count':len(objects)})
export(meshes,'SurfaceResearchStation.fbx')
lodcol=bpy.data.collections.new('SR_LOD1 - export only');scene.collection.children.link(lodcol);lods=[]
for source in meshes:
 o=source.copy();o.data=source.data.copy();o.name='LOD1_'+source.name;lodcol.objects.link(o);lods.append(o)
 if len(o.data.polygons)>50:
  select([o]);bpy.context.view_layer.objects.active=o;mod=o.modifiers.new('Distance detail reduction','DECIMATE');mod.ratio=.45
  with contextlib.redirect_stdout(io.StringIO()):bpy.ops.object.modifier_apply(modifier=mod.name)
export(lods,'SurfaceResearchStation_LOD1.fbx');lodcol.hide_render=True;lodcol.hide_viewport=True;scene.view_layers[0].layer_collection.children[lodcol.name].exclude=True
# Read the exported file into an isolated verification scene, then remove only that test import.
qa=bpy.data.scenes.new('Station FBX validation');bpy.context.window.scene=qa
with contextlib.redirect_stdout(io.StringIO()):bpy.ops.import_scene.fbx(filepath=os.path.join(EX,'SurfaceResearchStation.fbx'),use_anim=False)
objs=[o for o in qa.objects if o.type=='MESH'];images={n.image for o in objs for m in o.data.materials if m and m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image}
roundtrip={'mesh_count':len(objs),'all_have_uv':all(bool(o.data.uv_layers) for o in objs),'textures_exist':all(os.path.isfile(bpy.path.abspath(i.filepath)) for i in images),'texture_images':len(images)}
assert len(objs)==len(meshes) and roundtrip['all_have_uv'] and roundtrip['textures_exist'],roundtrip
report['fbx_roundtrip']=roundtrip;bpy.context.window.scene=scene
for o in list(qa.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.scenes.remove(qa)
for im,filename in imagepaths.items():im.filepath='//Textures/'+filename
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   sp=area.spaces.active;sp.region_3d.view_distance=44;sp.region_3d.view_location=Vector((0,0,2));sp.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
select([root])
with open(os.path.join(OUT,'AssetReport.json'),'w') as f:json.dump(report,f,indent=2)
with open(os.path.join(OUT,'MaterialManifest.json'),'w') as f:json.dump(manifest,f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'SurfaceResearchStation.blend'))
print('EXPORT VERIFIED',json.dumps({'dimensions':report['dimensions_m'],'lods':report['lods'],'roundtrip':roundtrip,'zero_area_triangles':sum(m['zero_area_triangles'] for m in report['meshes'])}))
