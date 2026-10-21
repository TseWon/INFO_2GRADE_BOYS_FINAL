import bpy
from pathlib import Path
out=Path(__file__).resolve().parent
# This script runs only in a dedicated background Blender process with factory startup.
for ob in list(bpy.data.objects):bpy.data.objects.remove(ob,do_unlink=True)
exec(compile((out/'build_joint_models.py').read_text(encoding='utf-8'),'build_joint_models.py','exec'))
active=bpy.context.scene
for old in list(bpy.data.scenes):
    if old!=active:bpy.data.scenes.remove(old)
exec(compile((out/'render_joint_models.py').read_text(encoding='utf-8'),'render_joint_models.py','exec'))
