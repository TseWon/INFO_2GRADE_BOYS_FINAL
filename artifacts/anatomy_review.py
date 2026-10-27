import bpy, os, math, json
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
ART=os.path.join(ROOT,'artifacts')
def components(ob):
 adj=[[] for v in ob.data.vertices]
 for e in ob.data.edges:
  a,b=e.vertices;adj[a].append(b);adj[b].append(a)
 todo=set(range(len(adj)));out=[]
 while todo:
  seed=todo.pop();ids=[seed];stack=[seed]
  while stack:
   for j in adj[stack.pop()]:
    if j in todo:todo.remove(j);stack.append(j);ids.append(j)
  out.append(ids)
 return sorted(out,key=len,reverse=True)
def review(tag,body_only=False):
 s=bpy.context.scene;cam=s.camera
 state=(cam.matrix_world.copy(),cam.data.ortho_scale,s.render.resolution_x,s.render.resolution_y,s.render.filepath,s.render.film_transparent)
 hidden={o:o.hide_render for o in s.objects}
 lightstate={o:(o.matrix_world.copy(),o.data.energy) for o in s.objects if o.type=='LIGHT'}
 s.objects['Preview floor'].hide_render=True
 for c in s.view_layers[0].layer_collection.children:
  if c.name.startswith('LOD'):c.exclude=True
 if body_only:
  for o in bpy.data.collections['01_CHARACTER - rigged export'].objects:
   if o.type=='MESH' and o.name not in ['Uniform_Ripstop','Gloves_and_Boot_Uppers','Outsoles_and_Glove_Reinforcement']:o.hide_render=True
 cam.data.ortho_scale=1.66 if body_only else 1.97
 target=Vector((0,0,.80 if body_only else .91))
 s.render.resolution_x=560;s.render.resolution_y=850;s.render.film_transparent=True
 try:
  for label,d in [('front',(0,-1,0)),('back',(0,1,0)),('side',(-1,0,0))]:
   direction=Vector(d);right=direction.cross(Vector((0,0,1)))
   cam.location=target+direction*6;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
   for name,side,height,dist,energy in [('Large soft key',2.2,2.8,3,500),('Soft fill',-2,1.6,2.5,280),('Rim light',1,2.7,-2,400)]:
    light=s.objects[name];light.location=target+direction*dist+right*side+Vector((0,0,height));light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler();light.data.energy=energy
   bpy.context.view_layer.update()
   s.render.filepath=os.path.join(ART,'anatomy_'+tag+'_'+label+'.png');bpy.ops.render.render(write_still=True)
 finally:
  cam.matrix_world=state[0];cam.data.ortho_scale=state[1];s.render.resolution_x=state[2];s.render.resolution_y=state[3];s.render.filepath=state[4];s.render.film_transparent=state[5]
  for o,h in hidden.items():o.hide_render=h
  for o,(mat,energy) in lightstate.items():o.matrix_world=mat;o.data.energy=energy
 print('Orthographic review saved: '+tag)
