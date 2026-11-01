import bpy,bmesh,os,math,json
from collections import defaultdict
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/build_surface_station.py'),encoding='utf-8').read(),'station_helpers','exec'))
scene=bpy.data.scenes['Surface Research Station'];bpy.context.window.scene=scene;ROOTOBJ=bpy.data.objects['SurfaceResearchStation'];COLS={k:bpy.data.collections['SR_'+k] for k in ['Shell','Roofs','Decks','Details','Preview','References']};setup_materials()
assert not ROOTOBJ.get('prepared'),'Already prepared'
def prism(name,outline,bottom,top,mat,collection='Decks'):
 N=len(outline);v=[(x,y,z) for z in [bottom,top] for x,y in outline];f=[tuple(reversed(range(N))),tuple(range(N,2*N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)];return mesh(name,v,f,mat,collection)
for ob in list(scene.objects):
 if ob.get('module') in ['Research_Spine','West_Laboratory','East_Laboratory'] and ('Raised foundation' in ob.name or 'Deck rim' in ob.name):bpy.data.objects.remove(ob,do_unlink=True)
MODULE='Rear_Shared_Foundation'
outline=[(-4.3,-.55),(4.3,-.55),(4.3,2.0),(12.5,2.0),(12.5,10.8),(4.3,10.8),(4.3,14.75),(-4.3,14.75),(-4.3,10.8),(-12.5,10.8),(-12.5,2.0),(-4.3,2.0)]
prism('Unified T foundation',outline,0,.78,DARK);prism('Unified deck cap',outline,.78,.86,ROOF)
def roof_curb(mod,cx,cy,x,y,width,length,top,eave,rise,halfwidth):
 global MODULE
 MODULE=mod
 xs=sorted(set([x-width/2,x+width/2]+([cx] if x-width/2<cx<x+width/2 else [])))
 profile=[(x-width/2,top),(x+width/2,top)]+[(xx,eave+rise*(1-abs(xx-cx)/halfwidth)-.025) for xx in reversed(xs)]
 N=len(profile);v=[(xx,yy,z) for yy in [y-length/2,y+length/2] for xx,z in profile];f=[tuple(reversed(range(N))),tuple(range(N,2*N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)];mesh('Roof-contoured equipment curb',v,f,DARK,'Roofs')
for side in [-1,1]:
 mod=('West' if side<0 else 'East')+'_Laboratory';cx=side*7.7
 roof_curb(mod,cx,6.4,cx,7.8,1.86,1.30,5.52,4.35,1.10,4.32)
 roof_curb(mod,cx,6.4,side*9.1,4.5,.88,.88,5.12,4.35,1.10,4.32)
 roof_curb(mod,cx,6.4,side*11.0,9.3,.44,.44,4.77,4.35,1.10,4.32)
roof_curb('West_Laboratory',-7.7,6.4,-7.7,4.7,1.12,1.12,5.55,4.35,1.10,4.32)
roof_curb('Command_Access',0,-8.2,1.45,-7.0,.85,.85,5.90,4.45,1.70,4.42)
for yy in [-6.7,-10.6]:roof_curb('Command_Access',0,-8.2,-1.8,yy,.88,.88,5.52,4.45,1.70,4.42)
roof_curb('Research_Spine',0,7.1,.8,10.4,.85,.85,6.10,4.65,1.75,3.82)
for xx,yy in [(-1.7,4.1),(1.7,7.4)]:roof_curb('Research_Spine',0,7.1,xx,yy,.88,.88,5.64,4.65,1.75,3.82)
# Retain editable individual components in a separate construction checkpoint.
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ART,'SurfaceResearch_editable_parts.blend'))
export_collections=[COLS[k] for k in ['Shell','Roofs','Decks','Details']]
objects=[o for c in export_collections for o in c.objects if o.type in ['MESH','FONT','CURVE']]
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();groups=defaultdict(list)
for o in objects:
 category=next(c.name.replace('SR_','') for c in o.users_collection if c in export_collections)
 evaluated=bpy.data.meshes.new_from_object(o.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
 # Weld collapsed poles on generated dome/radome/dish surfaces only.
 if any(k in o.name for k in ['pressure dome','radar dome','communication reflector']):
  bm=bmesh.new();bm.from_mesh(evaluated);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.000001);bm.to_mesh(evaluated);bm.free();evaluated.update()
 mat=o.data.materials[0] if o.data.materials else PAINT
 groups[(o.get('module','Station'),category,mat.name)].append((o,evaluated,o.matrix_world.copy()))
newobjects=[]
for (module,category,matname),items in groups.items():
 verts=[];faces=[];smooth=[]
 for o,me,matrix in items:
  off=len(verts);verts.extend([matrix@v.co for v in me.vertices]);faces.extend([tuple(i+off for i in p.vertices) for p in me.polygons]);smooth.extend([p.use_smooth for p in me.polygons])
 data=bpy.data.meshes.new(module+'_'+category);data.from_pydata(verts,[],faces);data.update();ob=bpy.data.objects.new(module+'__'+category+'__'+matname.removeprefix('SR_'),data);COLS[category].objects.link(ob);ob.parent=ROOTOBJ;ob['module']=module;data.materials.append(bpy.data.materials[matname])
 for p,s in zip(data.polygons,smooth):p.use_smooth=s
 uv=data.uv_layers.new(name='SurfaceMeters')
 for p in data.polygons:
  axis=max(range(3),key=lambda k:abs(p.normal[k]));axes=[k for k in range(3) if k!=axis]
  for loop in p.loop_indices:
   v=data.vertices[data.loops[loop].vertex_index].co;uv.data[loop].uv=(v[axes[0]]/2.0,v[axes[1]]/2.0)
 newobjects.append(ob)
 for old,me,matrix in items:bpy.data.objects.remove(old,do_unlink=True);bpy.data.meshes.remove(me)
ROOTOBJ['prepared']=True
for m in {m for o in newobjects for m in o.data.materials}:
 if m.use_nodes:
  for n in m.node_tree.nodes:
   if n.type=='TEX_IMAGE' and n.image:
    suffix=next((t for t in ['BaseColor','Normal','Roughness'] if t in n.image.name),None)
    if suffix:n.image.filepath='//Textures/'+m.name.removeprefix('SR_')+'_'+suffix+'.png'
try:scene.render.engine='CYCLES'
except TypeError as e:print(e)
scene.cycles.samples=32;scene.cycles.use_denoising=True
print('CYCLES devices',[(d.name,d.type) for d in bpy.context.preferences.addons['cycles'].preferences.devices])
scene.camera.location=(37,-48,29);scene.camera.rotation_euler=(Vector((0,-.2,2.1))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=43
scene.render.resolution_x=1500;scene.render.resolution_y=1100;scene.render.filepath=os.path.join(ART,'station_pbr_review.png')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'SurfaceResearchStation.blend'))
print('PREPARED',len(newobjects),'modular meshes')
bpy.ops.render.render(write_still=True)
