import bpy,bmesh,numpy as np,os,math
scene=bpy.context.scene;rig=scene.objects['DesertOperator_Rig']
root=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL\Assets\Blender\DesertOperator\Textures'
for name,scale in [('DO_Desert_Ripstop_BaseColor',[.93,1.03,1.30]),('DO_Coyote_Cordura_BaseColor',[1,1.04,1.18])]:
 im=bpy.data.images[name];a=np.empty(len(im.pixels),np.float32);im.pixels.foreach_get(a);a=a.reshape(-1,4);a[:,:3]=np.clip(a[:,:3]*np.array(scale),0,1);im.pixels.foreach_set(a.ravel());im.filepath_raw=os.path.join(root,im.name+'.png');im.save();im.pack()
for name,col,spec in [('DO_Rubber',(.014,.017,.013),.18),('DO_Seam_Thread',(.16,.126,.064),.22),('DO_Smoked_Lens',(.006,.009,.008),.5)]:
 m=bpy.data.materials[name];p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Specular IOR Level'].default_value=spec;m.diffuse_color=(*col,1)
# Remove only the flat front facepiece from the combined black-fabric object.
o=scene.objects['HeadEquipment_Balaclava'];bm=bmesh.new();bm.from_mesh(o.data);unseen=set(bm.verts)
while unseen:
 start=unseen.pop();stack=[start];component=[start]
 while stack:
  v=stack.pop()
  for edge in v.link_edges:
   q=edge.other_vert(v)
   if q in unseen:unseen.remove(q);stack.append(q);component.append(q)
 points=[o.matrix_world@v.co for v in component];xmin=min(p.x for p in points);xmax=max(p.x for p in points);ymax=max(p.y for p in points);zmax=max(p.z for p in points)
 if xmax-xmin>.08 and ymax<-.055 and zmax<1.64:
  for v in component:bm.verts.remove(v)
bm.to_mesh(o.data);bm.free()
# Cloth follows nose, mouth and chin; it is a curved grid with actual thickness.
rows=[(1.522,.041,-.084),(1.537,.063,-.103),(1.555,.075,-.118),(1.576,.080,-.125),(1.597,.077,-.137),(1.62,.070,-.143),(1.633,.061,-.132)]
verts=[];faces=[];nx=32;nz=30
for j in range(nz):
 z=rows[0][0]+(rows[-1][0]-rows[0][0])*j/(nz-1);rx=np.interp(z,[r[0] for r in rows],[r[1] for r in rows]);yc=np.interp(z,[r[0] for r in rows],[r[2] for r in rows])
 for i in range(nx):
  u=-1+2*i/(nx-1);y=yc+.045*u*u+.0012*math.sin(u*6+z*96)*(1-u*u);verts.append((u*rx,y,z))
for j in range(nz-1):
 for i in range(nx-1):a=j*nx+i;faces.append((a,a+1,a+nx+1,a+nx))
me=bpy.data.meshes.new('Anatomical fabric mask');me.from_pydata(verts,[],faces);me.update();mask=bpy.data.objects.new('Contoured_face_fabric',me);bpy.data.collections['01_CHARACTER - rigged export'].objects.link(mask);me.materials.append(bpy.data.materials['DO_Balaclava'])
bpy.ops.object.select_all(action='DESELECT');mask.select_set(True);bpy.context.view_layer.objects.active=mask
so=mask.modifiers.new('Mask fabric thickness','SOLIDIFY');so.thickness=.002;bpy.ops.object.modifier_apply(modifier=so.name)
for p in mask.data.polygons:p.use_smooth=True
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project();bpy.ops.object.mode_set(mode='OBJECT');g=mask.vertex_groups.new(name='Head');g.add(list(range(len(mask.data.vertices))),1,'REPLACE');mask.parent=rig
mod=mask.modifiers.new('Skeleton','ARMATURE');mod.object=rig
bpy.ops.object.select_all(action='DESELECT');mask.select_set(True);o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.join()
print('Curved anatomical face fabric and muted stitch/rubber colors applied')
