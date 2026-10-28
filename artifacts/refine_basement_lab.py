import bpy, math, os, random
from mathutils import Vector
random.seed(72)
BASE=r'C:\Users\PLUS\UNITY_PROJECT\INFO_2GRADE_BOYS_FINAL'
scene=bpy.context.scene
root=bpy.data.objects['B1_Basement_Laboratory']
for name,factor in [('Concrete | aged cold grey',.42),('Concrete | broken slab edges',.60),('Floor | worn industrial tiles',.57),('Floor | varied dark tile',.55),('Steel | oxidized blue grey',.68),('Steel | exposed machined edges',.72),('Paint | faded safety yellow',.82),('Paint | laboratory equipment',.62),('Stencil | faded white',.75)]:
    m=bpy.data.materials[name];c=m.diffuse_color[:];m.diffuse_color=(*[x*factor for x in c[:3]],1)
    for n in m.node_tree.nodes:
        if n.type=='VALTORGB':
            for e in n.color_ramp.elements:
                c=e.color[:];e.color=(*[x*factor for x in c[:3]],1)
        elif n.type=='BSDF_PRINCIPLED' and not n.inputs['Base Color'].is_linked:
            c=n.inputs['Base Color'].default_value[:];n.inputs['Base Color'].default_value=(*[x*factor for x in c[:3]],1)
# Uneven dark seepage at panel joints, using irregular outlines rather than rectangular decals.
grime=bpy.data.materials['Grime | oily seepage']
col=bpy.data.collections['09_Weathering']
for i in range(28):
    x=random.uniform(-5.5,1.0);z=random.uniform(1.9,4.9);width=random.uniform(.06,.18);height=random.uniform(.25,.8)
    verts=[(x-width*.4,4.516,z),(x+width*.5,4.516,z-.015),(x+width*.33,4.516,z-height*.25),(x+width*.12,4.516,z-height*.85),(x+width*.01,4.516,z-height),(x-width*.08,4.516,z-height*.55),(x-width*.31,4.516,z-height*.3)]
    me=bpy.data.meshes.new('Irregular damp stain');me.from_pydata(verts,[],[tuple(range(len(verts)))]);me.update()
    layer=me.uv_layers.new(name='UVMap')
    for li,v in enumerate(verts):layer.data[li].uv=((v[0]-x)/width,(v[2]-z)/height)
    o=bpy.data.objects.new('Damp streak at wall joint',me);col.objects.link(o);o.parent=root;me.materials.append(grime)
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[1].default_value=.22
prev=bpy.context.preferences.filepaths.save_version;bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.context.preferences.filepaths.save_version=prev
print('Saved aged material refinement',flush=True)
# Temporary lighting and camera for presentation only.
studio=bpy.data.collections.new('TEMP_Presentation');scene.collection.children.link(studio)
def aim(o,t):o.rotation_euler=(Vector(t)-o.location).to_track_quat('-Z','Y').to_euler()
cd=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',cd);studio.objects.link(cam);scene.camera=cam
cd.type='ORTHO';cd.ortho_scale=21.7;cam.location=(16,-21,18);aim(cam,(0,-.35,1.65))
for name,loc,power,size,color in [('Key',(-4,-7,13),2530,9,(.76,.87,1)),('Fill',(9,-8,8),1800,8,(.78,.86,1)),('Rim',(1,7,12),3500,7,(.74,.86,1)),('Warm spill',(-8,-4,6),550,5,(1,.74,.45))]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=loc;aim(o,(0,0,1))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.83));ground=bpy.context.object
for c in list(ground.users_collection):c.objects.unlink(ground)
studio.objects.link(ground)
m=bpy.data.materials.new('TEMP background');m.use_nodes=True
p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.018,.024,.032,1);p.inputs['Roughness'].default_value=.92;ground.data.materials.append(m)
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=1500;scene.render.resolution_y=1250;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(BASE,'artifacts','basement_lab_preview.png')
print('Rendering final cutaway',flush=True);bpy.ops.render.render(write_still=True)
cam.location=(7,-11,8);aim(cam,(0,1.5,2.0));cd.ortho_scale=13.2
scene.render.resolution_x=1250;scene.render.resolution_y=1000
scene.render.filepath=os.path.join(BASE,'artifacts','basement_lab_detail.png')
print('Rendering final detail',flush=True);bpy.ops.render.render(write_still=True)
print('DONE',flush=True)
