import bpy,math,json,os
from mathutils import Vector,Matrix
s=bpy.app.driver_namespace['fitted_export'];groups=s['groups'];out=s['out'];cx=s['cx']
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=4;scene.cycles.device='CPU'
scene.render.bake.margin=8;scene.render.bake.use_selected_to_active=False
# Bake the actual Blender colour and fabric normals instead of exporting flat colours.
for ob,entry in zip(groups,s['manifest']):
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    mat=ob.data.materials[0];nt=mat.node_tree;bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');output=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL')
    res=2048 if 'Digital' in mat.name else 1024
    for kind in ['BaseColor','Normal']:
        if kind=='Normal' and not any(n.type=='BUMP' for n in nt.nodes):continue
        img=bpy.data.images.new(mat.name+'_'+kind,res,res,alpha=False)
        img.colorspace_settings.name='sRGB' if kind=='BaseColor' else 'Non-Color'
        node=nt.nodes.new('ShaderNodeTexImage');node.image=img;nt.nodes.active=node
        emission=None
        if kind=='BaseColor':
            emission=nt.nodes.new('ShaderNodeEmission')
            color=bs.inputs['Base Color'];emission.inputs['Color'].default_value=color.default_value
            if color.is_linked:nt.links.new(color.links[0].from_socket,emission.inputs['Color'])
            nt.links.new(emission.outputs[0],output.inputs['Surface'])
        print('BAKING',mat.name,kind,flush=True)
        bpy.ops.object.bake(type='EMIT' if kind=='BaseColor' else 'NORMAL')
        if emission:nt.nodes.remove(emission);nt.links.new(bs.outputs['BSDF'],output.inputs['Surface'])
        path=out+'/Textures/'+mat.name+'_'+kind+'.png';img.filepath_raw=path;img.file_format='PNG';img.save()
        entry[kind]='Assets/Characters/FittedSoldier/Textures/'+os.path.basename(path)
        nt.nodes.remove(node)
# Move the exported meshes to a foot-centred origin after baking world-space fabric.
for ob in groups:ob.data.transform(Matrix.Translation((-cx,0,0)))
floor=min(v.co.z for o in groups for v in o.data.vertices)
for ob in groups:ob.data.transform(Matrix.Translation((0,0,-floor)))
zoff=-floor
arm=bpy.data.armatures.new('FittedSoldierRig');rig=bpy.data.objects.new('FittedSoldierRig',arm);s['coll'].objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
spec={
 'Hips':((0,0,.88),(0,0,1.03),None),
 'Spine':((0,0,1.03),(0,0,1.19),'Hips'),
 'Chest':((0,0,1.19),(0,0,1.38),'Spine'),
 'Neck':((0,0,1.38),(0,0,1.48),'Chest'),
 'Head':((0,0,1.48),(0,0,1.68),'Neck')}
for sign,side in [(-1,'L'),(1,'R')]:
    spec.update({
     'Shoulder_'+side:((sign*.06,0,1.37),(sign*.18,0,1.37),'Chest'),
     'UpperArm_'+side:((sign*.18,0,1.37),(sign*.29,0,1.14),'Shoulder_'+side),
     'Forearm_'+side:((sign*.29,0,1.14),(sign*.39,-.015,.94),'UpperArm_'+side),
     'Hand_'+side:((sign*.39,-.015,.94),(sign*.425,-.025,.81),'Forearm_'+side),
     'Thigh_'+side:((sign*.105,0,.90),(sign*.145,-.015,.485),'Hips'),
     'Shin_'+side:((sign*.145,-.015,.485),(sign*.16,.008,.11),'Thigh_'+side),
     'Foot_'+side:((sign*.16,.008,.11),(sign*.16,-.15,.055),'Shin_'+side)})
for name,(head,tail,parent) in spec.items():
    b=arm.edit_bones.new(name);b.head=Vector(head)+Vector((0,0,zoff));b.tail=Vector(tail)+Vector((0,0,zoff))
    if parent:b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def blend(a,b,t):return {a:1-t,b:t}
for ob in groups:
    vg={n:ob.vertex_groups.new(name=n) for n in spec}
    for v in ob.data.vertices:
        x,y,z=v.co;xabs=abs(x);z-=zoff;side='L' if x<0 else 'R'
        if z>1.46:w={'Head':1}
        elif xabs>(.19 if z>1.22 else .27) and z>.78:
            if z>1.27:w=blend('UpperArm_'+side,'Chest',1-smooth(.17,.235,xabs))
            elif z>1.08:w=blend('Forearm_'+side,'UpperArm_'+side,smooth(1.08,1.20,z))
            elif z>.91:w=blend('Hand_'+side,'Forearm_'+side,smooth(.91,.97,z))
            else:w={'Hand_'+side:1}
        elif z>1.34:w=blend('Chest','Head',smooth(1.38,1.48,z))
        elif z>1.05:w={'Chest':1} # Rigid vest and its MOLLE must retain shape.
        elif z>.87:w={'Hips':1}
        elif z>.72:w=blend('Thigh_'+side,'Hips',smooth(.76,.88,z))
        elif z>.40:w=blend('Shin_'+side,'Thigh_'+side,smooth(.42,.56,z))
        elif z>.15:w={'Shin_'+side:1}
        else:w=blend('Foot_'+side,'Shin_'+side,smooth(.12,.20,z))
        for n,value in w.items():
            if value>0:vg[n].add([v.index],value,'REPLACE')
    mod=ob.modifiers.new('GameRig','ARMATURE');mod.object=rig;ob.parent=rig
    ob.data.calc_loop_triangles()
# Remove source geometry only in the background export process.
for o in list(bpy.data.objects):
    if o not in groups and o!=rig:bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for o in groups+[rig]:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=out+'/FittedSoldier.fbx',use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'artifacts/FittedSoldier_GameRig.blend'),compress=True)
report={'materials':s['manifest'],'vertices':sum(len(o.data.vertices) for o in groups),'triangles':sum(len(o.data.loop_triangles) for o in groups),'height':max(v.co.z for o in groups for v in o.data.vertices),'bones':len(arm.bones)}
with open(out+'/manifest.json','w') as f:json.dump(report,f,indent=2)
print('EXPORT COMPLETE',report,flush=True)
