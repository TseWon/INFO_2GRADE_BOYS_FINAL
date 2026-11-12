import bpy, os, math
from mathutils import Vector, kdtree

ROOT = os.getcwd()
SOURCE = os.path.join(ROOT, 'meshy_output/Soldiers/Brazilian_Soldier_US_Flag/Soldier_US_LeftFlag.blend')
REFERENCE = os.path.join(ROOT, 'Assets/Characters/FittedSoldier/FittedSoldier.fbx')
BLEND_OUT = os.path.join(ROOT, 'meshy_output/Soldiers/Brazilian_Soldier_US_Flag/Soldier_US_LeftFlag_Rigged.blend')
FBX_OUT = os.path.join(ROOT, 'Assets/Characters/USSoldier/USSoldier_Rigged.fbx')

bpy.ops.wm.open_mainfile(filepath=SOURCE)
soldier = bpy.data.objects['output_unwrapped']
soldier.name = 'USSoldier_Skinned'
for v in soldier.data.vertices:
    v.co.x *= .947
    v.co.y *= .947
    v.co.z = (v.co.z + .952218) * .947

bpy.ops.import_scene.fbx(filepath=REFERENCE)
rig = bpy.data.objects['FittedSoldierRig']
rig.name = 'USSoldierRig'
refs = [o for o in bpy.data.objects if o.type == 'MESH' and o != soldier]

def fit(p):
    x = p.x
    sign = -1 if x < 0 else 1
    x = sign * (abs(x) if abs(x) <= .22 else .22 + (abs(x)-.22)*1.75)
    return Vector((x, p.y*1.25, p.z*1.05))

count = sum(len(o.data.vertices) for o in refs)
tree = kdtree.KDTree(count)
lookup=[]
for obj in refs:
    matrix = obj.matrix_world
    for vertex in obj.data.vertices:
        tree.insert(fit(matrix @ vertex.co), len(lookup))
        lookup.append((obj, vertex.index))
tree.balance()

groups = {name:soldier.vertex_groups.new(name=name) for name in [b.name for b in rig.data.bones]}
matched = 0
for vertex in soldier.data.vertices:
    _, idx, distance = tree.find(vertex.co)
    src, vi = lookup[idx]
    assignments = [(src.vertex_groups[g.group].name, g.weight) for g in src.data.vertices[vi].groups]
    assignments = [(name, weight) for name,weight in assignments if name in groups and weight > .0001]
    total = sum(weight for _,weight in assignments)
    if total:
        for name,weight in assignments:
            groups[name].add([vertex.index], weight/total, 'REPLACE')
        matched += 1
    else:
        groups['Chest'].add([vertex.index], 1, 'REPLACE')
print('WEIGHTS', matched, len(soldier.data.vertices))

bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
original_bones = {bone.name:(bone.head.copy(), bone.tail.copy()) for bone in rig.data.edit_bones}
for bone in rig.data.edit_bones:
    old_head, old_tail = original_bones[bone.name]
    bone.head = fit(old_head)
    bone.tail = fit(old_tail)
bpy.ops.object.mode_set(mode='OBJECT')

for obj in refs:
    bpy.data.objects.remove(obj, do_unlink=True)
soldier.parent = rig
mod = soldier.modifiers.new('Armature', 'ARMATURE')
mod.object = rig
rig.show_in_front = True
rig.data.display_type = 'STICK'

bpy.ops.wm.save_as_mainfile(filepath=BLEND_OUT)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
soldier.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=FBX_OUT, use_selection=True,
    axis_forward='-Z', axis_up='Y', add_leaf_bones=False,
    bake_anim=False, path_mode='COPY', embed_textures=False,
    use_mesh_modifiers=False)
print('RIGGED_EXPORT', FBX_OUT, os.path.getsize(FBX_OUT), len(soldier.data.vertices))
