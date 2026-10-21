from pathlib import Path

root=Path(__file__).resolve().parent
for name in ['build_joint_models.py','render_joint_models.py','build_drawings_pdf.py']:
    p=root/name
    s=p.read_text(encoding='utf-8')
    for before,after in [('300mm','30mm'),('300 추가','30 추가'),('300,','30,'),('360','90'),('345','75'),('315','45'),('30cm','3cm')]:
        s=s.replace(before,after)
    s=s.replace("max(dims)*(1.15 if spec['id']=='J3E' else 2.1)","max(dims)*2.1")
    s=s.replace("max(part.dimensions)*(1.15 if part.name=='J3E' else 2.1)","max(part.dimensions)*2.1")
    s=s.replace("make_camera('00_Overview',(195,-55,0),(1,-1,-1),540)","make_camera('00_Overview',(95,-65,0),(1,-1,-1),360)")
    if name=='render_joint_models.py':
        s=s.replace("readme=bpy.data.texts.new('설계 조건 및 투상 기준')","readme=bpy.data.texts.get('설계 조건 및 투상 기준') or bpy.data.texts.new('설계 조건 및 투상 기준')\nreadme.clear()")
    if name=='build_drawings_pdf.py':
        s=s.replace('REV A','REV B')
        s=s.replace("projection(d,'top',133,218,.55,", "projection(d,'top',100,213,1,")
        s=s.replace("projection(d,'front',133,125,.55,", "projection(d,'front',100,127,1,")
        s=s.replace("projection(d,'side',338,125,1,", "projection(d,'side',229,127,1,")
        s=s.replace('278,177,120,76','286,160,108,82').replace("text(338,177,","text(340,155,")
        s=s.replace('정면·윗면 0.55:1 / 우측면 1:1','정면·윗면·우측면 1:1')
        s=s.replace('참고 이미지의 90mm/30mm 연장은 적용하지 않음.','사용자 정정에 따라 30mm 연장 적용.')
        s=s.replace('s=.95;ox=42;oy=178','s=1.5;ox=165;oy=185')
        s=s.replace('X(349)','X(79)').replace('X(140)','X(30)').replace('X(330)','X(60)')
        s=s.replace("text(22,132,", "text(22,126,").replace("text(22,122,", "text(22,116,").replace("text(22,113,", "text(22,107,")
        s=s.replace('(0.95:1)', '(1.5:1)')
    p.write_text(s,encoding='utf-8')
print('Sources updated for 30mm extension / 90mm total / REV B')
