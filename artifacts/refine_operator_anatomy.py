import bpy, math, os, json, numpy as np
from mathutils import Vector
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
exec(compile(open(os.path.join(ROOT,'artifacts/anatomy_review.py'),encoding='utf-8').read(),'review','exec'))
ob=bpy.data.objects['Uniform_Ripstop'];parts=components(ob)
def smooth(ids,iterations,mask):
 adj=[[] for v in ob.data.vertices]
 for e in ob.data.edges:
  a,b=e.vertices;adj[a].append(b);adj[b].append(a)
 coords=np.array([v.co[:] for v in ob.data.vertices]);weights={i:mask(*coords[i]) for i in ids}
 for it in range(iterations):
  for strength in (.5,-.51):
   out=coords.copy()
   for i in ids:
    if weights[i]>.001:out[i]+=strength*weights[i]*(coords[adj[i]].mean(axis=0)-coords[i])
   coords=out
 for i in ids:ob.data.vertices[i].co=coords[i]
def gauss(v,c,w):return math.exp(-((v-c)/w)**2)
def step(a,b,v):
 t=max(0,min(1,(v-a)/(b-a)));return t*t*(3-2*t)
if STAGE=='pelvis':
 assert not ob.get('pelvis_revision_20261027'), 'Pelvis stage already applied'
 coords=np.array([v.co[:] for v in ob.data.vertices]);np.save(os.path.join(ROOT,'artifacts/body_original_coordinates.npy'),coords)
 smooth(parts[0],25,lambda x,y,z:gauss(z,.94,.105)*.92)
 for i in parts[0]:
  v=ob.data.vertices[i];x,y,z=v.co
  rear=step(-.005,.105,y)
  seat=gauss(z,.899,.100)
  lateral=.65+.35*gauss(abs(x),.082,.065)
  v.co.y+=.052*rear*seat*lateral
  # Wider iliac/upper-seat silhouette, fading fully into the belt and thighs.
  v.co.x+=math.copysign(.007,x)*step(.095,.19,abs(x))*gauss(z,.925,.077)
  # Remove the apron-like central bulge without altering the crotch topology.
  central=gauss(x,0,.066)*gauss(z,.875,.060)
  v.co.y-=.016*central*step(.025,.12,y)
  v.co.y+=.007*central*(1-step(-.09,-.015,y))
  # Ease the back of the proximal thigh into the seat.
  v.co.y+=.009*rear*gauss(z,.79,.067)
 ob.data.update();ob['pelvis_revision_20261027']=True
 review('pelvis_clay',True)
 print('Stage pelvis complete, unchanged vertex count',len(ob.data.vertices))
if STAGE=='pelvis_refine':
 assert not ob.get('pelvis_refine_20261027')
 # Fair the existing back surface across the old saddle/leg union ridge.
 for i in parts[0]:
  v=ob.data.vertices[i];x,y,z=v.co
  w=step(.80,.88,z)*(1-step(.99,1.045,z))*step(.025,.09,y)
  radius=.212-.042*step(.95,1.04,z)
  depth=.13+.045*gauss(z,.904,.085)
  target=.012+depth*math.sqrt(max(.01,1-(x/radius)**2))
  target-=.006*gauss(x,0,.028)*gauss(z,.865,.055)
  v.co.y=y*(1-w)+target*w
 # Relax the old union edges with a spatially limited true Laplacian pass.
 adj=[[] for v in ob.data.vertices]
 for e in ob.data.edges:
  a,b=e.vertices;adj[a].append(b);adj[b].append(a)
 for iteration in range(22):
  coords=[v.co.copy() for v in ob.data.vertices]
  for i in parts[0]:
   x,y,z=coords[i];w=.38*gauss(z,.954,.065)
   if w>.01:ob.data.vertices[i].co=coords[i].lerp(sum((coords[j] for j in adj[i]),Vector())/len(adj[i]),w)
 ob.data.normals_split_custom_set([(0,0,0)]*len(ob.data.loops));ob.data.update();ob['pelvis_refine_20261027']=True
 review('pelvis_refined',True)
if STAGE=='torso_limbs':
 assert not ob.get('torso_limbs_revision_20261027')
 def upper(p):
  x,y,z=p;side=math.copysign(1,x);a=abs(x)
  shoulder=step(.16,.245,a)*gauss(z,1.44,.095)
  x-=side*.014*shoulder;y*=1-.12*shoulder;z-=.008*shoulder
  # Modest chest depth, with a softer abdominal transition underneath the vest.
  core=1-step(.15,.23,a)
  y-=.008*gauss(z,1.365,.10)*core*(1-step(-.10,-.025,y))
  # Maintain elbow and wrist landmarks; reduce the exaggerated forearm bulge.
  arm=step(.22,.29,a)*gauss(z,1.102,.055)
  cx=side*(.31+(1.17-z)*.255)
  x=cx+(x-cx)*(1-.09*arm);y*=1-.08*arm
  return Vector((x,y,z))
 def lower(p):
  x,y,z=p;side=math.copysign(1,x);cx=side*(.115 if z>.5 else .118)
  thigh=.035*gauss(z,.72,.075);calf=.095*gauss(z,.399,.065)
  x=cx+(x-cx)*(1-thigh-calf)
  # Calf mass sits behind the shin, with a straighter anterior tibial contour.
  y+=.005*gauss(z,.414,.072)*step(.005,.07,y)
  y+=.005*gauss(z,.40,.09)*(1-step(-.05,.005,y))
  return Vector((x,y,z))
 for i in parts[1]:ob.data.vertices[i].co=upper(ob.data.vertices[i].co)
 for i in parts[0]:ob.data.vertices[i].co=lower(ob.data.vertices[i].co)
 # Apply matching displacements to the existing sewn pockets.
 for ids in parts[2:]:
  center=sum((ob.data.vertices[i].co for i in ids),Vector())/len(ids)
  for i in ids:ob.data.vertices[i].co=(upper if center.z>1 else lower)(ob.data.vertices[i].co)
 adj=[[] for v in ob.data.vertices]
 for e in ob.data.edges:
  a,b=e.vertices;adj[a].append(b);adj[b].append(a)
 for it in range(32):
  coords=[v.co.copy() for v in ob.data.vertices]
  for i in parts[1]:
   x,y,z=coords[i];w=.45*step(.14,.20,abs(x))*gauss(z,1.453,.070)
   if w>.005:ob.data.vertices[i].co=coords[i].lerp(sum((coords[j] for j in adj[i]),Vector())/len(adj[i]),w)
 # Quiet the strongest elbow/knee rings without adding further wrinkles.
 for it in range(7):
  coords=[v.co.copy() for v in ob.data.vertices]
  for i in parts[0]+parts[1]:
   x,y,z=coords[i];w=.33*(gauss(z,.535,.10) if z<1 else gauss(z,1.18,.09)*step(.21,.27,abs(x)))
   if w>.01:ob.data.vertices[i].co=coords[i].lerp(sum((coords[j] for j in adj[i]),Vector())/len(adj[i]),w)
 for gearname in ['LimbEquipment_Webbing','LimbEquipment_Desert_Ripstop','LimbEquipment_Rubber','LimbEquipment_Coyote_Cordura','LimbEquipment_Composite']:
  gear=bpy.data.objects[gearname]
  for v in gear.data.vertices:
   p=gear.matrix_world@v.co
   if p.z>1.02:p=upper(p)
   elif .24<p.z<.85:p=lower(p)
   v.co=gear.matrix_world.inverted()@p
  gear.data.update()
 ob.data.normals_split_custom_set([(0,0,0)]*len(ob.data.loops));ob.data.update();ob['torso_limbs_revision_20261027']=True
 review('after_clay',True)
