import bpy, os, math, numpy as np
from mathutils import Vector
BASE=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(BASE,'Assets','Blender','OakSteelTable','Textures')
assert bpy.data.filepath.endswith('oak_steel_table.blend')
root=next(o for o in bpy.context.scene.objects if o.type=='EMPTY' and o.parent is None)
size=list(root.get('measured_dimensions_m',[]))
assert len(size)==3 and abs(size[0]-1)<.001, 'Expected original 1m table; refuse repeated scaling'
# Bake the horizontal enlargement into the meshes. The 1.3m height stays unchanged.
for o in bpy.context.scene.objects:
    if o.type!='MESH':continue
    o.location.x*=2;o.location.y*=2
    for v in o.data.vertices:v.co.x*=2;v.co.y*=2
    o.data.update()
root['dimensions_m']='Width 2.0 / depth 2.4 / height 1.3'
root['measured_dimensions_m']=[2.,2.4,1.3]
root['revision']='Enlarged footprint; enhanced oak grain'
bpy.context.scene.name='Oak and Steel Table - 2000 x 2400 x 1300 mm'
for o in bpy.context.scene.objects:
    if 'Solid oak tabletop' in o.name:o['dimensions_mm']='2000 x 2400 x 35'

source=open(os.path.join(BASE,'artifacts','build_oak_steel_table.py'),encoding='utf-8-sig').read()
exec(source[source.index('def save_image'):source.index('N=2048;')])
N=2048;u=np.arange(N,dtype=np.float32)[None,:]/N;v=np.arange(N,dtype=np.float32)[:,None]/N
fine=np.random.default_rng(123).random((N,N)).astype(np.float32)
long=noise(N,18,340,124);slow=noise(N,5,10,125)
# Broad growth rings plus much finer fibres, readable at game-view distances.
phase=v*58+2.5*np.sin(u*6+v*9)+.8*np.sin(u*17+v*26)+.7*(slow-.5)
growth=(.5+.5*np.sin(phase*math.tau))**7
fibre=(.5+.5*np.sin(phase*math.tau*4.7+.5*np.sin(u*22)))**15
grain=.85*growth+.18*fibre
for cx,cy in [(.24,.24),(.73,.64),(.40,.86)]:
    du=(u-cx)/.22;dv=(v-cy)/.047
    weight=np.exp(-(du*du+dv*dv)*1.2)*.85
    rings=(.5+.5*np.sin(np.sqrt(du*du+dv*dv+.035)*31))**7
    grain=grain*(1-weight)+rings*weight
pores=np.clip((long-.65)*3,0,.75)
tone=1.015-.38*grain-.10*pores+.13*(slow-.5)+.025*(fine-.5)
color=np.array([.79,.61,.40])[None,None,:]*tone[:,:,None]
height=.23*grain+.055*pores+.008*fine
roughness=np.clip(.44+.12*grain+.035*(slow-.5),.35,.65)
save_image('Oak_BaseColor',color)
save_image('Oak_Normal',normalmap(height,1.5),True)
save_image('Oak_Roughness',roughness,True)
save_image('Oak_MetallicSmoothness',np.zeros_like(color),True,1-roughness)
for im in bpy.data.images:
    if im.filepath and 'OakSteelTable' in im.filepath:
        im.reload();im.filepath='//OakSteelTable/Textures/'+os.path.basename(im.filepath)
for m in bpy.data.materials:
    if 'light oak' in m.name:
        for n in m.node_tree.nodes:
            if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.65
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(v) for o in bpy.context.scene.objects if o.type=='MESH' for v in o.bound_box]
dimensions=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
assert all(abs(a-b)<.0001 for a,b in zip(dimensions,[2,2.4,1.3])),dimensions
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            r=area.spaces.active.region_3d;r.view_location=(0,0,.65);r.view_distance=5.3
            r.view_rotation=Vector((1.4,-2,1.6)).to_track_quat('Z','Y')
prev=bpy.context.preferences.filepaths.save_version;bpy.context.preferences.filepaths.save_version=0
try:bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
finally:bpy.context.preferences.filepaths.save_version=prev
print('Replaced existing table asset. Dimensions:',dimensions)
