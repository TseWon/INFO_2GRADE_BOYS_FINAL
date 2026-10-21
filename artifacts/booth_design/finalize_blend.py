import bpy
from pathlib import Path
out=Path(__file__).resolve().parent
scene=next(s for s in bpy.data.scenes if s.name.startswith('공포게임 부스'))
bpy.context.window.scene=scene
scene.camera=bpy.data.objects['01_Assembly_3D']
scene.cycles.use_denoising=True
readme=bpy.data.texts.get('READ_ME')
readme.clear()
readme.write((out/'조립안.md').read_text(encoding='utf-8'))
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.color_type='MATERIAL'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(out/'horror_booths.blend'))
assert len(scene.collection.children['01_Pipes_29'].objects)==29
assert len(scene.collection.children['02_Joints_18'].objects)==18
print('FINAL_VERIFIED: 29 pipes, 18 joints; assembly guide packed; normal startup file saved')
