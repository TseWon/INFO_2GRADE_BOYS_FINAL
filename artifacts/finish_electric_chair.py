import bpy, os
from mathutils import Vector
BASE=r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL'
scene=bpy.context.scene
root=bpy.data.objects['ElectricChair']
# Curve conductors become real meshes for downstream model import.
bpy.ops.object.select_all(action='DESELECT')
curves=[o for o in root.children if o.type=='CURVE']
for o in curves:o.select_set(True)
if curves:
    bpy.context.view_layer.objects.active=curves[0]
    bpy.ops.object.convert(target='MESH')
# Supply usable UVs for custom topology; primitive meshes already have UV layers.
for o in root.children:
    if o.type!='MESH' or o.data.uv_layers:continue
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(island_margin=.025)
    bpy.ops.object.mode_set(mode='OBJECT')
studio=bpy.data.collections.new('TEMP_Preview_Studio');scene.collection.children.link(studio)
def link(o):
    for c in list(o.users_collection):c.objects.unlink(o)
    studio.objects.link(o)
def point(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('Preview camera');cam=bpy.data.objects.new('Preview camera',camdata)
studio.objects.link(cam);cam.location=(3,-5,3.0);point(cam,(0,0,1.07))
camdata.type='ORTHO';camdata.ortho_scale=2.65;scene.camera=cam
for name,pos,power,size,col in [('Key',(-3,-4,5),470,3,(1,.83,.66)),('Fill',(3,-2,3),210,3,(.73,.84,1)),('Rim',(1,3,4),520,2,(1,.92,.77))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=col
    o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=pos;point(o,(0,0,1))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.005))
floor=bpy.context.object;link(floor)
mat=bpy.data.materials.new('TEMP studio floor');mat.diffuse_color=(.025,.03,.034,1);mat.use_nodes=True
p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.025,.03,.034,1);p.inputs['Roughness'].default_value=.82
floor.data.materials.append(mat)
scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.09,.11,.13,1);bg.inputs[1].default_value=.4
scene.render.resolution_x=960;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=os.path.join(BASE,'artifacts','electric_chair_preview.png')
scene.render.image_settings.file_format='PNG'
bpy.ops.render.render(write_still=True)
scene.camera=None
for o in list(studio.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(studio)
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True);bpy.context.view_layer.objects.active=root
root.empty_display_size=.1
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=4.5
            area.spaces.active.region_3d.view_location=(0,0,1.06)
            area.spaces.active.overlay.show_extras=False
previous_save_version=bpy.context.preferences.filepaths.save_version
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BASE,'Assets','Blender','electric_chair.blend'))
bpy.context.preferences.filepaths.save_version=previous_save_version
print('Saved finished asset with',sum(o.type=='MESH' for o in root.children),'meshes; packed reference and procedural materials included.')
print('Preview:',scene.render.filepath)
