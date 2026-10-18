import bpy, bmesh, math, os, json, numpy as np
from mathutils import Vector
BASE = r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT = os.path.join(BASE, 'Assets', 'Blender', 'ResearchFacility', 'Textures')
os.makedirs(OUT, exist_ok=True)
assert bpy.data.filepath.endswith('abandoned_research_facility.blend')

def enum_value(owner, prop, value):
    values=[x.identifier for x in owner.bl_rna.properties[prop].enum_items]
    assert value in values, (prop, values)
    setattr(owner,prop,value)

def noise(n, gx, gy, seed):
    rng=np.random.default_rng(seed)
    grid=rng.random((gy,gx)).astype(np.float32)
    x=np.arange(n,dtype=np.float32)/n*gx;y=np.arange(n,dtype=np.float32)/n*gy
    ix=x.astype(int);iy=y.astype(int);fx=x-ix;fy=y-iy
    fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
    a=grid[iy[:,None]%gy,ix[None,:]%gx]
    b=grid[iy[:,None]%gy,(ix[None,:]+1)%gx]
    c=grid[(iy[:,None]+1)%gy,ix[None,:]%gx]
    d=grid[(iy[:,None]+1)%gy,(ix[None,:]+1)%gx]
    return ((a*(1-fx)+b*fx)*(1-fy[:,None])+(c*(1-fx)+d*fx)*fy[:,None]).astype(np.float32)

def save_image(name, rgb, color=True, alpha=None):
    h,w=rgb.shape[:2];im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    if color:im.colorspace_settings.name='sRGB'
    else:im.colorspace_settings.name='Non-Color'
    rgba=np.ones((h,w,4),np.float32)
    if rgb.ndim==2:rgba[:,:,:3]=rgb[:,:,None]
    else:rgba[:,:,:3]=rgb
    if alpha is not None:rgba[:,:,3]=alpha
    im.pixels.foreach_set(np.clip(rgba,0,1).ravel());im.update()
    im.filepath_raw=os.path.join(OUT,name+'.png')
    enum_value(im,'file_format','PNG');im.save()
    # Reload from the actual PNG to give Blender and Unity identical encoded files.
    path=im.filepath_raw;bpy.data.images.remove(im)
    im=bpy.data.images.load(path,check_existing=True)
    im.colorspace_settings.name='sRGB' if color else 'Non-Color'
    return im

def maps(kind, n, seed):
    broad=noise(n,5,5,seed)
    mid=noise(n,35,35,seed+1)
    fine=noise(n,250,250,seed+2)
    grit=np.random.default_rng(seed+3).random((n,n)).astype(np.float32)
    streak=noise(n,75,3,seed+4)
    value=.80+(broad-.5)*.28+(mid-.5)*.18+(fine-.5)*.11+(grit-.5)*.07
    base={'Concrete':(.49,.46,.40),'DampConcrete':(.40,.395,.31),'Roof':(.18,.20,.18),
          'Moss':(.12,.19,.045),'Rust':(.28,.105,.035),'Metal':(.25,.28,.26),
          'Glass':(.10,.15,.16),'Dark':(.045,.052,.048),'Paint':(.51,.51,.40)}[kind]
    albedo=np.array(base)[None,None,:]*value[:,:,None]
    height=.25*mid+.12*fine+.035*grit
    rough=np.clip(.76+(fine-.5)*.18,0,1)
    metallic=np.zeros((n,n),np.float32)
    if kind in ('Concrete','DampConcrete'):
        runoff=np.clip((.48-streak)*2.8,0,.62)*(0.4+0.6*broad)
        albedo*=1-runoff[:,:,None]*.65
        pores=(grit>.992)&(fine<.48)
        albedo[pores]*=.43;height[pores]-=.4
        # Sparse hairline fractures, with tapered irregular branches.
        xx=np.arange(n)[None,:]/n;yy=np.arange(n)[:,None]/n
        line=.33+.024*np.sin(yy*27)+.009*np.sin(yy*93)
        fracture=(np.abs(xx-line)<.0008)&(yy>.19)&(yy<.66)
        albedo[fracture]*=.36;height[fracture]-=.25
        if kind=='DampConcrete':
            growth=np.clip((noise(n,11,8,seed+7)+.3*mid-.57)*4,0,1)
            green=np.stack([.075+.09*fine,.115+.13*fine,.025+.045*fine],axis=2)
            albedo=albedo*(1-growth[:,:,None])+green*growth[:,:,None]
            height+=growth*(.2+.3*fine)
    elif kind=='Roof':
        growth=np.clip((broad+.3*mid-.81)*4,0,.8)
        albedo=albedo*(1-growth[:,:,None])+np.array([.10,.15,.035])*growth[:,:,None]
        rough=np.clip(.85+.12*(mid-.5),0,1)
    elif kind=='Moss':
        height=.7*fine+.2*grit;rough[:]=.94
    elif kind=='Rust':
        patches=np.clip((mid-.4)*3,0,1)
        albedo=albedo*(1-patches[:,:,None]*.6)+np.array([.045,.035,.025])*patches[:,:,None]
        metallic[:]=.25;rough[:]=.87
    elif kind=='Metal':
        metallic[:]=.68;rough[:]=.58
    elif kind=='Glass':
        albedo*=1-np.clip((.4-streak)*2,0,.3)[:,:,None]
        height*=.015;rough=np.clip(.21+.16*mid,0,1);metallic[:]=.25
    elif kind=='Dark':
        rough[:]=.9
    # Periodic central differences: tangent-space normal map, OpenGL +Y.
    dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*1.6
    dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*1.6
    normals=np.stack([-dx,-dy,np.ones_like(dx)],axis=2)
    normals/=np.linalg.norm(normals,axis=2)[:,:,None]
    images={
        'albedo':save_image(kind+'_BaseColor',albedo,True),
        'normal':save_image(kind+'_Normal',normals*.5+.5,False),
        'roughness':save_image(kind+'_Roughness',rough,False),
        'metallicSmoothness':save_image(kind+'_MetallicSmoothness',np.repeat(metallic[:,:,None],3,axis=2),False,1-rough)
    }
    return images

allmaps={kind:maps(kind,2048 if kind in ('Concrete','DampConcrete') else 1024,1900+i*71)
         for i,kind in enumerate(['Concrete','DampConcrete','Roof','Moss','Rust','Metal','Glass','Dark','Paint'])}

# Replace procedural materials with portable UV image maps.
manifest=[]
def convert(m,kind,tint=(1,1,1,1)):
    m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
    p=n.new('ShaderNodeBsdfPrincipled');out=n.new('ShaderNodeOutputMaterial');l.new(p.outputs[0],out.inputs['Surface'])
    imgs=allmaps[kind];coord=n.new('ShaderNodeTexCoord')
    nodes={}
    for key,image in imgs.items():
        t=n.new('ShaderNodeTexImage');t.image=image;nodes[key]=t;l.new(coord.outputs['UV'],t.inputs['Vector'])
    l.new(nodes['albedo'].outputs['Color'],p.inputs['Base Color'])
    l.new(nodes['roughness'].outputs['Color'],p.inputs['Roughness'])
    l.new(nodes['metallicSmoothness'].outputs['Color'],p.inputs['Metallic'])
    normal=n.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.65
    l.new(nodes['normal'].outputs['Color'],normal.inputs['Color']);l.new(normal.outputs['Normal'],p.inputs['Normal'])
    p.inputs['Alpha'].default_value=1
    m['portable_texture_family']=kind
    manifest.append({'name':m.name,'family':kind,'tint':list(tint),
        'maps':{k:os.path.relpath(v.filepath_raw,BASE).replace('\\','/') for k,v in imgs.items()}})

facility_mats=[m for m in bpy.data.materials if m.name.startswith('Facility |')]
for m in facility_mats:
    s=m.name
    if 'moss' in s:kind='Moss'
    elif 'glass' in s:kind='Glass'
    elif 'rusted' in s:kind='Rust'
    elif 'asphalt' in s:kind='Roof'
    elif 'limestone' in s or 'spalled' in s:kind='Concrete'
    elif 'rain stains' in s or 'unlit' in s:kind='Dark'
    elif 'safety' in s:kind='Paint'
    else:kind='Metal'
    convert(m,kind)
damp=bpy.data.materials.get('Facility | integrated moss damp concrete') or bpy.data.materials.new('Facility | integrated moss damp concrete')
damp.diffuse_color=(.28,.32,.19,1);convert(damp,'DampConcrete')

# Coplanar decal polygons are replaced by the wall/roof texture itself.
overlay_tokens=('peeling surfaces and rain streaks','facade moss','roof stains moss and cracked surfacing',
                'clinging moss and ivy','layered facade moss colonies','deep rain trails','dense moss around')
removed=[]
for o in list(bpy.context.scene.objects):
    if any(t in o.name for t in overlay_tokens):
        removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)

deleted=0
seen_global={}
for o in list(bpy.context.scene.objects):
    if o.type!='MESH':continue
    me=o.data
    bm=bmesh.new();bm.from_mesh(me);duplicates=[]
    for f in bm.faces:
        key=tuple(sorted(tuple(round(c,4) for c in o.matrix_world@v.co) for v in f.verts))
        if key in seen_global:duplicates.append(f)
        else:seen_global[key]=o.name
    if duplicates:
        bmesh.ops.delete(bm,geom=duplicates,context='FACES');deleted+=len(duplicates)
    # Recalculate connected faces conservatively; preserve original winding on standalone glass sheets.
    bm.to_mesh(me);bm.free();me.update()
    damp_index=len(me.materials);me.materials.append(damp)
    uv=me.uv_layers.get('UVMap') or me.uv_layers.new(name='UVMap')
    for face in me.polygons:
        mat=me.materials[face.material_index]
        c=face.center
        if 'concrete' in mat.name.lower() and 'damp' not in mat.name.lower():
            if c.z<2.6 or (abs(face.normal.z)<.3 and (int(c.x/8)+int(c.y/8))%11==0):
                face.material_index=damp_index;mat=damp
        family=mat.get('portable_texture_family','Concrete')
        tile={'Concrete':6.,'DampConcrete':6.,'Roof':8.,'Moss':2.,'Rust':2.,'Metal':3.,'Glass':4.,'Dark':4.,'Paint':4.}[family]
        N=face.normal
        # Cylindrical projection for curved vertical wings; planar UVs for slabs and rectangular additions.
        curved=o.name.startswith(('A -','B -','C -')) and abs(N.z)<.45
        radius=98 if o.name.startswith('A -') else 61 if o.name.startswith('B -') else 19
        ratio=109/88 if o.name.startswith('A -') else 70/51 if o.name.startswith('B -') else 1
        angles=[math.atan2(me.vertices[me.loops[li].vertex_index].co.y*ratio,me.vertices[me.loops[li].vertex_index].co.x) for li in face.loop_indices] if curved else []
        if angles and max(angles)-min(angles)>math.pi:angles=[a+math.tau if a<0 else a for a in angles]
        for j,li in enumerate(face.loop_indices):
            v=me.vertices[me.loops[li].vertex_index].co
            if curved:u,w=angles[j]*radius,v.z
            elif abs(N.z)>.5:u,w=v.x,v.y
            elif abs(N.y)>=abs(N.x):u,w=v.x,v.z
            else:u,w=v.y,v.z
            uv.data[li].uv=(u/tile,w/tile)
    # Remove unused material slots so Unity imports only each mesh's actual materials.
    used=sorted(set(p.material_index for p in me.polygons));oldm=list(me.materials);remap={old:i for i,old in enumerate(used)}
    oldindices=[p.material_index for p in me.polygons];me.materials.clear()
    for i in used:me.materials.append(oldm[i])
    for p,idx in zip(me.polygons,oldindices):p.material_index=remap[idx]
    me.update()

for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.clip_start=.3;area.spaces.active.clip_end=1000

for image in bpy.data.images:
    if image.filepath and 'ResearchFacility' in image.filepath:
        image.filepath=bpy.path.relpath(image.filepath)
root=bpy.data.objects['Research_Facility_220m']
root['weathering']='UV image textures: aged concrete, integrated moss, roof grime; no coplanar overlay decals'
root['material_conversion']='Portable base color, tangent normal, roughness and metallic/smoothness PNG maps'
with open(os.path.join(BASE,'artifacts','facility_material_manifest.json'),'w',encoding='utf-8') as f:json.dump(manifest,f,indent=2)
report={'removed_overlay_objects':removed,'removed_exact_duplicate_faces':deleted,
        'mesh_count':sum(o.type=='MESH' for o in bpy.context.scene.objects),
        'texture_families':len(allmaps),'texture_files':len(allmaps)*4}
with open(os.path.join(BASE,'artifacts','facility_surface_fix_report.json'),'w',encoding='utf-8') as f:json.dump(report,f,indent=2)
previous=bpy.context.preferences.filepaths.save_version;bpy.context.preferences.filepaths.save_version=0
try:bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
finally:bpy.context.preferences.filepaths.save_version=previous
print(json.dumps(report))
