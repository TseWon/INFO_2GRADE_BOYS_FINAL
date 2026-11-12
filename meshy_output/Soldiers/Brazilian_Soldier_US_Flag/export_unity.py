import bpy
import os

folder = os.path.dirname(bpy.data.filepath)
mesh = bpy.data.objects["output_unwrapped"]
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
bpy.context.view_layer.objects.active = mesh

output = os.path.join(folder, "Soldier_US_LeftFlag.fbx")
bpy.ops.export_scene.fbx(
    filepath=output,
    use_selection=True,
    axis_forward="-Z",
    axis_up="Y",
    path_mode="COPY",
    embed_textures=True,
    bake_anim=False,
    add_leaf_bones=False,
)
print("EXPORTED", output, os.path.getsize(output), "bytes")
print("MESH", len(mesh.data.vertices), "vertices", len(mesh.data.polygons), "faces")
