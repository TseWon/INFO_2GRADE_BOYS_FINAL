import bpy,os
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
rig=bpy.data.objects['FittedSoldierRig'];arm=rig.data
groups=[o for o in bpy.data.objects if o.type=='MESH'];spec={b.name:None for b in arm.bones};zoff=arm.bones['Hips'].head_local.z-.88
for ob in groups:
    ob.vertex_groups.clear()
    for mod in list(ob.modifiers):
        if mod.type=='ARMATURE':ob.modifiers.remove(mod)
code=open(ROOT+'/artifacts/finish_fitted_export.py',encoding='utf-8').read()
exec(code[code.index('def smooth'):code.index('# Remove source geometry')])
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=ROOT+'/Assets/Characters/FittedSoldier/FittedSoldier.fbx',use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
bpy.ops.wm.save_as_mainfile(filepath=ROOT+'/artifacts/FittedSoldier_GameRig.blend',compress=True)
print('Weights corrected: hip pouches follow pelvis, not hands',flush=True)
