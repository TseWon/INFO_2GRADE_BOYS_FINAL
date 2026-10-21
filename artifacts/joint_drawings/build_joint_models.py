import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT=Path(__file__).resolve().parent
OUT.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new('Joint fabrication review - mm')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=.001
scene.unit_settings.length_unit='MILLIMETERS'
parts=bpy.data.collections.new('JOINTS - four independent parts')
scene.collection.children.link(parts)
studio=bpy.data.collections.new('CAMERAS - exact orthographic axes')
scene.collection.children.link(studio)

def attach(o,c=parts):
    for col in list(o.users_collection): col.objects.unlink(o)
    c.objects.link(o)
    return o

def cylinder(d,lo,hi,r):
    d=Vector(d)
    bpy.ops.mesh.primitive_cylinder_add(vertices=64,radius=r,depth=hi-lo,location=d*((lo+hi)/2))
    o=attach(bpy.context.object)
    o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    return o

def boolean(obj,tool,op):
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Exact solid '+op,'BOOLEAN'); mod.operation=op; mod.solver='EXACT'; mod.object=tool
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(tool,do_unlink=True)

specs=[
 {'id':'J2','title':'2구 조인트','dirs':[(1,0,0),(0,0,1)],'lengths':[45,45],'offset':(0,0,0)},
 {'id':'J3E','title':'3구 (1) - 30mm 연장형','dirs':[(1,0,0),(0,-1,0),(0,0,-1)],'lengths':[75,45,45],'offset':(110,0,0)},
 {'id':'J3','title':'3구 (2) - 기본형','dirs':[(1,0,0),(0,-1,0),(0,0,-1)],'lengths':[45,45,45],'offset':(0,-150,0)},
 {'id':'J4','title':'4구 조인트','dirs':[(1,0,0),(-1,0,0),(0,-1,0),(0,0,-1)],'lengths':[45,45,45,45],'offset':(140,-150,0)}]
views={'front':((1,0,0),(0,0,1),(0,-1,0)), 'top':((1,0,0),(0,1,0),(0,0,1)), 'side':((0,1,0),(0,0,1),(1,0,0))}
report=[]
mat=bpy.data.materials.new('Steel - illustrative finish');mat.diffuse_color=(.38,.47,.56,1)
mat.use_nodes=True
bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Base Color'].default_value=(.38,.47,.56,1);bs.inputs['Metallic'].default_value=.7;bs.inputs['Roughness'].default_value=.28

def projection(obj,axes):
    u,v,eye=map(Vector,axes)
    mesh=obj.data
    bm=bmesh.new();bm.from_mesh(mesh);bm.normal_update()
    bvh=BVHTree.FromBMesh(bm)
    lines=[]
    for e in bm.edges:
        if len(e.link_faces)!=2: continue
        n1,n2=[f.normal for f in e.link_faces]
        a,b=e.verts[0].co.copy(),e.verts[1].co.copy()
        d1,d2=n1.dot(eye),n2.dot(eye)
        silhouette=(d1>1e-5)!=(d2>1e-5)
        sharp=n1.dot(n2)<math.cos(math.radians(25))
        if not (silhouette or sharp): continue
        pa=(a.dot(u),a.dot(v));pb=(b.dot(u),b.dot(v))
        if math.dist(pa,pb)<.005: continue
        # Split candidate edges so hidden/visible transitions follow the actual solid.
        cuts=max(1,min(80,math.ceil((b-a).length/2)))
        for j in range(cuts):
            t0=j/cuts;t1=(j+1)/cuts;p=a.lerp(b,(t0+t1)/2)
            hit=bvh.ray_cast(p+eye*1000,-eye,2000)
            visible=hit[0] is None or abs((hit[0]-p).dot(eye))<.05
            q0=a.lerp(b,t0);q1=a.lerp(b,t1)
            lines.append([round(q0.dot(u),5),round(q0.dot(v),5),round(q1.dot(u),5),round(q1.dot(v),5),int(visible)])
    coords=[(vert.co.dot(u),vert.co.dot(v)) for vert in mesh.vertices]
    bm.free()
    return {'bounds':[min(x for x,y in coords),min(y for x,y in coords),max(x for x,y in coords),max(y for x,y in coords)],'lines':lines}

def make_camera(name,center,direction,scale):
    c=bpy.data.cameras.new(name);c.type='ORTHO';c.ortho_scale=scale;c.clip_start=.1;c.clip_end=5000
    ob=bpy.data.objects.new(name,c);studio.objects.link(ob)
    ob.location=Vector(center)+Vector(direction).normalized()*1000
    ob.rotation_euler=(Vector(center)-ob.location).to_track_quat('-Z','Y').to_euler()
    return ob

for spec in specs:
    if spec['id']=='J4':
        obj=cylinder((1,0,0),-45,45,13.5)
        branches=list(zip(spec['dirs'][2:],spec['lengths'][2:]))
    else:
        bpy.ops.mesh.primitive_uv_sphere_add(segments=64,ring_count=32,radius=13.5)
        obj=attach(bpy.context.object)
        branches=list(zip(spec['dirs'],spec['lengths']))
    obj.name=spec['id']
    for d,L in branches: boolean(obj,cylinder(d,-.25,L,13.5),'UNION')
    for d,L in zip(spec['dirs'],spec['lengths']):
        boolean(obj,cylinder(d,L-30,L+1,12.75),'DIFFERENCE')
        if L>45: boolean(obj,cylinder(d,15,L-30+.01,10),'DIFFERENCE')
    bm=bmesh.new();bm.from_mesh(obj.data)
    if any(not e.is_manifold for e in bm.edges):
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0001)
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.00001)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data);obj.data.update()
    nonmanifold=sum(not e.is_manifold for e in bm.edges)
    volume=bm.calc_volume(signed=False);bm.free()
    assert nonmanifold==0,(spec['id'],nonmanifold)
    dims=[round(v,4) for v in obj.dimensions]
    expected={'J2':[58.5,27,58.5],'J3E':[88.5,58.5,58.5],'J3':[58.5,58.5,58.5],'J4':[90,58.5,58.5]}[spec['id']]
    assert all(abs(a-b)<.01 for a,b in zip(dims,expected)),(spec['id'],dims)
    datum={'id':spec['id'],'title':spec['title'],'dimensions_mm':dims,'ports':len(spec['dirs']),'directions':spec['dirs'],'mouths_from_origin_mm':spec['lengths'],'socket_ID_mm':25.5,'OD_mm':27,'socket_wall_mm':.75,'insertion_depth_mm':30,'nonmanifold_edges':nonmanifold,'volume_mm3':volume,'views':{k:projection(obj,axes) for k,axes in views.items()}}
    for d,L in zip(spec['dirs'],spec['lengths']):
        hit=obj.ray_cast(Vector(d)*(L+1),-Vector(d),distance=L+50)
        # In the extended branch the centre ray traverses the provisional relief bore;
        # measure the seating annulus instead at radius 11mm (between R10 and R12.75).
        radial=Vector((0,11,0)) if d[0] else Vector((11,0,0))
        seat=obj.ray_cast(Vector(d)*(L+1)+radial,-Vector(d),distance=L+50)
        assert seat[0]
        depth=(Vector(d)*L+radial-seat[1]).length
        assert abs(depth-30)<.02,(spec['id'],d,depth)
    obj.data.materials.append(mat)
    obj['socket_inner_diameter_mm']=25.5;obj['outer_diameter_mm']=27;obj['socket_wall_mm']=.75;obj['socket_insertion_depth_mm_assumed']=30
    obj['status']='Steel; ID25.5 and OD27 confirmed; no screw holes. REVIEW ONLY - socket depth, tolerances, fit and internal shoulder assumed.'
    obj['branch_directions']=str(spec['dirs'])
    obj.location=spec['offset']
    for p in obj.data.polygons:p.use_smooth=True
    bpy.context.view_layer.update()
    localcenter=(Vector([min(v.co[i] for v in obj.data.vertices) for i in range(3)])+Vector([max(v.co[i] for v in obj.data.vertices) for i in range(3)]))/2
    center=localcenter+obj.location
    for key,axes in views.items(): make_camera(spec['id']+'_'+key,center,axes[2],max(dims)*1.35)
    direction=(1,-1,1) if spec['id']=='J2' else (1,-1,-.8)
    make_camera(spec['id']+'_ISO',center,direction,max(dims)*2.1)
    report.append(datum)

scene.render.engine='BLENDER_WORKBENCH'
scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL'
scene.display.shading.show_shadows=True;scene.display.shading.show_cavity=True
scene.display.shading.cavity_type='BOTH';scene.display.shading.show_object_outline=True
scene.display.shading.background_type='WORLD';scene.world=bpy.data.worlds.new('Joint white world');scene.world.color=(1,1,1)
scene.render.resolution_x=1000;scene.render.resolution_y=760;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
scene.camera=make_camera('00_Overview',(95,-65,0),(1,-1,-1),360)
scene['drawing_views']='front: eye -Y, image X/Z; top: eye +Z, image X/Y; right side: eye +X, image Y/Z; third-angle placement'
scene['extension']='J3E +X mouth 75, stop 45; standard mouth45, stop15; net compensation30mm'
scene['units']='Geometry in millimetres; scene scale_length=.001. Draft assumptions, not fabrication release.'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.clip_end=5000
        area.spaces.active.shading.color_type='MATERIAL'
(OUT/'model_geometry.json').write_text(json.dumps(report,ensure_ascii=False),encoding='utf-8')
(OUT/'validation.json').write_text(json.dumps([{k:v for k,v in r.items() if k!='views'} for r in report],ensure_ascii=False,indent=2),encoding='utf-8')
bpy.data.libraries.write(str(OUT/'pipe_joints_review.blend'),{scene},fake_user=True)
print('CREATED four closed solids, exact dimensions, 30mm socket seats, 12 model-derived projections')
