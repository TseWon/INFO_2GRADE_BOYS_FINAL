import bpy
import math
from pathlib import Path
out=Path(__file__).resolve().parent
s=next(s for s in bpy.data.scenes if s.name=='Joint fabrication review - mm')
bpy.context.window.scene=s
parts=[o for o in s.objects if o.type=='MESH']
for part in parts:
    part.data.set_sharp_from_angle(angle=math.radians(30))
    bpy.data.objects[part.name+'_ISO'].data.ortho_scale=max(part.dimensions)*(1.15 if part.name=='J3E' else 2.1)
    for o in parts: o.hide_render=(o!=part)
    s.camera=bpy.data.objects[part.name+'_ISO']
    s.render.filepath=str(out/(part.name+'_iso.png'))
    bpy.ops.render.render(write_still=True)
for o in parts:o.hide_render=False
s.camera=bpy.data.objects['00_Overview']
s.render.filepath=str(out/'joints_overview.png')
bpy.ops.render.render(write_still=True)
readme=bpy.data.texts.new('설계 조건 및 투상 기준')
readme.write('확정: 스틸, 소켓 내경25.5mm, 나사구멍 없음. 가정: 외경30mm, 삽입30mm, 연장부 내부20mm. J3E +X축 300mm 순증: 입구45→345mm, 멈춤면15→315mm. 정면 -Y에서+Y, 윗면 +Z에서-Z, 우측면 +X에서-X. 배치는 제3각법. 허용공차와 체결력 미확정; 제작업체 검토용. 각 객체는 독립된 폐곡면 메시이며 좌표단위mm(scene scale0.001).')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.clip_end=5000
            area.spaces.active.shading.color_type='MATERIAL'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(out/'pipe_joints_review.blend'))
print('RENDERS AND NORMAL BLEND SAVED')
