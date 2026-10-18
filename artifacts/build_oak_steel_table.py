import bpy, bmesh, math, os, json
import numpy as np
from mathutils import Vector

BASE=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(BASE,'Assets','Blender','OakSteelTable','Textures')
os.makedirs(OUT,exist_ok=True)
TARGET=os.path.join(BASE,'Assets','Blender','oak_steel_table.blend')
assert not os.path.exists(TARGET),'Target exists; do not overwrite another model'
scene=bpy.data.scenes.new('Oak and Steel Table - 1000 x 1200 x 1300 mm')
bpy.context.window.scene=scene
collection=bpy.data.collections.new('Oak Steel Table');scene.collection.children.link(collection)
root=bpy.data.objects.new('Oak_Steel_Table_1m_1p2m_1p3m',None);collection.objects.link(root)
root['dimensions_m']='Width 1.0 / depth 1.2 / height 1.3'
root['reference']='Light oak top, dark powder-coated steel square-tube frame, rubber feet'

def save_image(name,data,linear=False,alpha=None):
    h,w=data.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    im.colorspace_settings.name='Non-Color' if linear else 'sRGB'
    pixels=np.ones((h,w,4),np.float32)
    pixels[:,:,:3]=data[:,:,None] if data.ndim==2 else data
    if alpha is not None:pixels[:,:,3]=alpha
    im.pixels.foreach_set(np.clip(pixels,0,1).ravel());im.update()
    choices=[e.identifier for e in im.bl_rna.properties['file_format'].enum_items]
    im.file_format=next(v for v in choices if v=='PNG')
    path=os.path.join(OUT,name+'.png');im.filepath_raw=path;im.save();bpy.data.images.remove(im)
    im=bpy.data.images.load(path,check_existing=True);im.colorspace_settings.name='Non-Color' if linear else 'sRGB'
    return im

def noise(n,gx,gy,seed):
    grid=np.random.default_rng(seed).random((gy,gx)).astype(np.float32)
    x=np.arange(n)/n*gx;y=np.arange(n)/n*gy;ix=x.astype(int);iy=y.astype(int)
    fx=x-ix;fy=y-iy;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
    a=grid[iy[:,None]%gy,ix[None,:]%gx];b=grid[iy[:,None]%gy,(ix[None,:]+1)%gx]
    c=grid[(iy[:,None]+1)%gy,ix[None,:]%gx];d=grid[(iy[:,None]+1)%gy,(ix[None,:]+1)%gx]
    return ((a*(1-fx)+b*fx)*(1-fy[:,None])+(c*(1-fx)+d*fx)*fy[:,None]).astype(np.float32)

def normalmap(h,strength):
    dx=(np.roll(h,-1,1)-np.roll(h,1,1))*strength
    dy=(np.roll(h,-1,0)-np.roll(h,1,0))*strength
    out=np.stack([-dx,-dy,np.ones_like(dx)],2);out/=np.linalg.norm(out,axis=2)[:,:,None]
    return out*.5+.5

N=2048;u=np.arange(N,dtype=np.float32)[None,:]/N;v=np.arange(N,dtype=np.float32)[:,None]/N
randomfine=np.random.default_rng(102).random((N,N)).astype(np.float32)
long=noise(N,12,290,104);slow=noise(N,5,12,105)
phase=v*92+1.9*np.sin(u*7+v*13)+.65*np.sin(u*18+v*36)+1.1*(slow-.5)
grain=(.5+.5*np.sin(phase*math.tau))**12
finegrain=(.5+.5*np.sin(phase*math.tau*3.14+.2*np.sin(u*30)))**18
grain=grain*.62+finegrain*.14
# Two subtle elliptical grain swirls, not dark cartoon knots.
for cx,cy in [(.28,.30),(.76,.74)]:
    du=(u-cx)/.25;dv=(v-cy)/.055
    influence=np.exp(-(du*du+dv*dv)*1.4)*.75
    rings=(.5+.5*np.sin(np.sqrt(du*du+dv*dv+.06)*35))**10
    grain=grain*(1-influence)+rings*influence*.75
pore=np.clip((long-.68)*3,0,.7)
tone=.98-.25*grain-.09*pore+.10*(slow-.5)+.035*(randomfine-.5)
woodcolor=np.array([.76,.585,.385])[None,None,:]*tone[:,:,None]
woodheight=.14*grain+.04*pore+.012*randomfine
woodrough=np.clip(.40+.075*grain+.04*(slow-.5),.3,.55)
woodmaps={'albedo':save_image('Oak_BaseColor',woodcolor),
          'normal':save_image('Oak_Normal',normalmap(woodheight,1.2),True),
          'roughness':save_image('Oak_Roughness',woodrough,True),
          'metallicSmoothness':save_image('Oak_MetallicSmoothness',np.zeros_like(woodcolor),True,1-woodrough)}

N=1024;grain=noise(N,240,240,202);low=noise(N,12,12,203)
steelcolor=np.array([.042,.034,.027])[None,None,:]*(.92+.13*low[:,:,None]+.06*grain[:,:,None])
steelrough=np.clip(.50+.1*(grain-.5),.42,.6)
steelmaps={'albedo':save_image('PowderCoat_BaseColor',steelcolor),
           'normal':save_image('PowderCoat_Normal',normalmap(grain*.035,.7),True),
           'roughness':save_image('PowderCoat_Roughness',steelrough,True),
           'metallicSmoothness':save_image('PowderCoat_MetallicSmoothness',np.full((N,N,3),.55,np.float32),True,1-steelrough)}
manifest=[]
def textured(name,maps,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    nodes=m.node_tree.nodes;links=m.node_tree.links;nodes.clear()
    p=nodes.new('ShaderNodeBsdfPrincipled');out=nodes.new('ShaderNodeOutputMaterial');links.new(p.outputs[0],out.inputs[0])
    tex=nodes.new('ShaderNodeTexCoord');images={}
    for key,im in maps.items():
        t=nodes.new('ShaderNodeTexImage');t.image=im;links.new(tex.outputs['UV'],t.inputs['Vector']);images[key]=t
    links.new(images['albedo'].outputs['Color'],p.inputs['Base Color'])
    links.new(images['roughness'].outputs['Color'],p.inputs['Roughness'])
    links.new(images['metallicSmoothness'].outputs['Color'],p.inputs['Metallic'])
    normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.5
    links.new(images['normal'].outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],p.inputs['Normal'])
    manifest.append({'name':name,'maps':{k:os.path.relpath(im.filepath_raw,BASE).replace('\\','/') for k,im in maps.items()}})
    return m
wood=textured('Table - light oak satin finish',woodmaps,(.57,.365,.19))
steel=textured('Table - dark bronze powder coated steel',steelmaps,(.042,.034,.027))
def plain(name,color,metallic,rough):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metallic
    manifest.append({'name':name,'color':list(color),'metallic':metallic,'roughness':rough})
    return m
rubber=plain('Table - black rubber foot pads',(.017,.015,.012),0,.88)
hardware=plain('Table - blackened mounting hardware',(.09,.08,.065),.8,.35)

def assign_uv(obj,kind):
    me=obj.data;uv=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        normal=p.normal
        for li in p.loop_indices:
            q=obj.matrix_world@me.vertices[me.loops[li].vertex_index].co
            if kind=='wood':
                if abs(normal.z)>.5:coords=(q.x+.5,(q.y+.6)/1.2)
                elif abs(normal.y)>.5:coords=(q.x+.5,(q.z-1.265)*3+.17)
                else:coords=((q.y+.6)/1.2,(q.z-1.265)*3+.67)
            elif abs(normal.z)>.5:coords=(q.x*8,q.y*8)
            elif abs(normal.x)>.5:coords=(q.y*8,q.z*8)
            else:coords=(q.x*8,q.z*8)
            uv.data[li].uv=coords

def box(name,loc,size,mat,bevel=.001):
    bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
    for v in bm.verts:v.co.x*=size[0];v.co.y*=size[1];v.co.z*=size[2]
    if bevel:
        bmesh.ops.bevel(bm,geom=list(bm.edges),offset=bevel,segments=3)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    me=bpy.data.meshes.new(name);bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);collection.objects.link(o);o.parent=root;o.location=loc;me.materials.append(mat)
    bpy.context.view_layer.update();assign_uv(o,'wood' if mat==wood else 'metal')
    return o

def cylinder(name,loc,radius,depth,mat,segments=32):
    bm=bmesh.new();bmesh.ops.create_cone(bm,cap_ends=True,segments=segments,radius1=radius,radius2=radius,depth=depth)
    me=bpy.data.meshes.new(name);bm.to_mesh(me);bm.free();o=bpy.data.objects.new(name,me);collection.objects.link(o);o.parent=root;o.location=loc;me.materials.append(mat)
    for p in me.polygons:p.use_smooth=len(p.vertices)==4
    bpy.context.view_layer.update();assign_uv(o,'metal');return o

# Dimensions follow drawing text, including its unusually tall 1.3m table height.
box('Solid oak tabletop - 1000 x 1200 x 35 mm',(0,0,1.2825),(1,1.2,.035),wood,.0025)
for x in (-.46,.46):
    for y in (-.56,.56):
        name=('Left' if x<0 else 'Right')+(' front' if y<0 else ' rear')
        box(name+' 40mm square steel leg',(x,y,.6415),(.040,.040,1.247),steel,.0018)
        box(name+' polymer leg end cap',(x,y,.020),(.042,.042,.014),rubber,.002)
        cylinder(name+' adjustable rubber foot',(x,y,.0065),.0205,.013,rubber)
        cylinder(name+' threaded foot stem',(x,y,.018),.006,.012,hardware,20)
        # Mounting tabs face the tabletop underside; screw heads face downward.
        xx=x-math.copysign(.038,x);yy=y-math.copysign(.038,y)
        box(name+' underside mounting tab',(xx,yy,1.263),(.052,.052,.004),steel,.0015)
        cylinder(name+' mounting screw washer',(xx,yy,1.2598),.006,.0016,hardware)
        cylinder(name+' mounting screw head',(xx,yy,1.258),.004,.002,hardware,12)
        box(name+' screw slot',(xx,yy,1.2569),(.0045,.0009,.0003),rubber,.0001)

for y in (-.56,.56):
    box(('Front' if y<0 else 'Rear')+' apron rail',(0,y,1.240),(.880,.030,.050),steel,.0015)
for x in (-.46,.46):
    box(('Left' if x<0 else 'Right')+' apron rail',(x,0,1.240),(.030,1.080,.050),steel,.0015)
# Weld fillets are small and deliberately separate from large flat faces.
for x in (-.46,.46):
    for y in (-.56,.56):
        box('Inner corner weld',(x-math.copysign(.022,x),y-math.copysign(.015,y),1.239),(.004,.018,.037),steel,.0018)

bpy.context.view_layer.update()
bounds=[o.matrix_world@Vector(c) for o in collection.objects if o.type=='MESH' for c in o.bound_box]
dimensions=[max(v[i] for v in bounds)-min(v[i] for v in bounds) for i in range(3)]
assert all(abs(a-b)<.0001 for a,b in zip(dimensions,[1,1.2,1.3])),dimensions
root['measured_dimensions_m']=dimensions
for ob in bpy.context.selected_objects:ob.select_set(False)
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.clip_start=.01;area.spaces.active.clip_end=100
            r=area.spaces.active.region_3d;r.view_location=(0,0,.65);r.view_distance=3.4;r.view_rotation=Vector((1.5,-2,1.2)).to_track_quat('Z','Y')
with open(os.path.join(BASE,'artifacts','table_material_manifest.json'),'w',encoding='utf-8') as f:json.dump(manifest,f,indent=2)
print('Table built, exact dimensions in metres:',dimensions,'mesh objects:',sum(o.type=='MESH' for o in collection.objects))

