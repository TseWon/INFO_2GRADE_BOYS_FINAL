import bpy,os,json,traceback
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
ART=os.path.join(ROOT,'artifacts/surface_station')
scene=bpy.data.scenes['Surface Research Station']
bpy.context.window.scene=scene
camera=scene.camera
original=(camera.location.copy(),camera.rotation_euler.copy(),camera.data.ortho_scale,scene.render.resolution_x,scene.render.resolution_y,scene.cycles.samples,scene.render.filepath)
views=[('station_rear',(-34,40,23),(0,1,2),42,1100,850),('station_front',(0,-60,4),(0,0,3.7),34,1300,480),('station_detail',(19,-25,10),(5,-8,2.8),17,1100,850)]
def render_next():
 try:
  name,pos,target,scale,w,h=views.pop(0)
  camera.location=pos;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=scale
  scene.render.resolution_x=w;scene.render.resolution_y=h;scene.cycles.samples=20
  scene.render.filepath=os.path.join(ART,name+'.png')
  bpy.ops.render.render(write_still=True)
  with open(os.path.join(ART,'render_progress.txt'),'a',encoding='utf-8') as f:f.write(name+' complete\n')
  if views:return 1.0
  camera.location, camera.rotation_euler,camera.data.ortho_scale,scene.render.resolution_x,scene.render.resolution_y,scene.cycles.samples,scene.render.filepath=original
  bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'Assets/Blender/SurfaceResearchStation/SurfaceResearchStation.blend'))
  with open(os.path.join(ART,'render_progress.txt'),'a',encoding='utf-8') as f:f.write('DONE\n')
 except Exception:
  with open(os.path.join(ART,'render_progress.txt'),'a',encoding='utf-8') as f:f.write(traceback.format_exc())
 return None
with open(os.path.join(ART,'render_progress.txt'),'w') as f:f.write('STARTED\n')
bpy.app.timers.register(render_next,first_interval=2)
print('Review views queued')
