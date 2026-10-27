import bpy,math,json,os
from mathutils import Vector
scene=bpy.context.scene;rig=scene.objects['DesertOperator_Rig']
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
segments={}
for sgn in [-1,1]:
 side='L' if sgn<0 else 'R'
 eb=rig.data.edit_bones.new('Shoulder_'+side);eb.head=(sgn*.065,0,1.465);eb.tail=rig.data.edit_bones['UpperArm_'+side].head;eb.parent=rig.data.edit_bones['Chest'];rig.data.edit_bones['UpperArm_'+side].parent=eb
 for f,name in enumerate(['Index','Middle','Ring','Little']):
  x=sgn*(.352+f*.018);ln=[.069,.078,.073,.056][f];z=.888-[.002,0,.002,.008][f]
  points=[(x,-.012,z),(x+sgn*.001,-.015,z-ln*.36),(x+sgn*.002,-.025,z-ln*.68),(x+sgn*.001,-.037,z-ln*.89)]
  parent=rig.data.edit_bones['Hand_'+side]
  for j in range(3):
   n=name+str(j+1)+'_'+side;b=rig.data.edit_bones.new(n);b.head=points[j];b.tail=points[j+1];b.parent=parent;parent=b;segments[n]=(Vector(points[j]),Vector(points[j+1]))
 points=[(sgn*.352,-.009,.928),(sgn*.333,-.016,.908),(sgn*.331,-.032,.888),(sgn*.342,-.044,.88)]
 parent=rig.data.edit_bones['Hand_'+side]
 for j in range(3):
  n='Thumb'+str(j+1)+'_'+side;b=rig.data.edit_bones.new(n);b.head=points[j];b.tail=points[j+1];b.parent=parent;parent=b;segments[n]=(Vector(points[j]),Vector(points[j+1]))
bpy.ops.object.mode_set(mode='OBJECT')
gloves=scene.objects['Gloves_and_Boot_Uppers']
for name in segments:gloves.vertex_groups.new(name=name)
for v in gloves.data.vertices:
 p=gloves.matrix_world@v.co
 groups={gloves.vertex_groups[g.group].name:g.weight for g in v.groups}
 side='L' if p.x<0 else 'R';hand='Hand_'+side
 if groups.get(hand,0)<.5:continue
 ax=abs(p.x)
 thumb=ax<.346 and p.z>.87
 if not thumb and p.z>.887:continue
 distances=[]
 for n,(a,b) in segments.items():
  if not n.endswith('_'+side) or (n.startswith('Thumb')!=thumb):continue
  t=max(0,min(1,(p-a).dot(b-a)/(b-a).length_squared));dist=(p-a.lerp(b,t)).length
  distances.append((dist,n))
 distances.sort();nearest=distances[:2]
 # Preserve the palm and blend through the knuckle into each finger chain.
 mix=max(0,min(1,(.902-p.z)/.028)) if not thumb else max(0,min(1,(.354-ax)/.017))
 for g in list(v.groups):gloves.vertex_groups[g.group].remove([v.index])
 gloves.vertex_groups[hand].add([v.index],1-mix,'REPLACE')
 w=[1/(d+.002)**3 for d,n in nearest];total=sum(w)
 for ww,(d,n) in zip(w,nearest):gloves.vertex_groups[n].add([v.index],mix*ww/total,'REPLACE')
mapping={n:n for n in ['Hips','Spine','Chest','Neck','Head']}
for side,word in [('L','Left'),('R','Right')]:
 for b,h in [('Shoulder','Shoulder'),('UpperArm','UpperArm'),('Forearm','LowerArm'),('Hand','Hand'),('Thigh','UpperLeg'),('Shin','LowerLeg'),('Foot','Foot')]:mapping[word+h]=b+'_'+side
 for f in ['Thumb','Index','Middle','Ring','Little']:
  for j,suffix in enumerate(['Proximal','Intermediate','Distal']):mapping[word+f+suffix]=f+str(j+1)+'_'+side
out=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL\Assets\Blender\DesertOperator'
with open(os.path.join(out,'HumanoidBoneMapping.json'),'w') as f:json.dump(mapping,f,indent=2)
rig['rig_note']='49-bone FK humanoid skeleton including clavicles and three phalanges per digit. Animation clips are not included.'
print('Rig bones:',len(rig.data.bones),'finger weights assigned; humanoid mapping saved')
