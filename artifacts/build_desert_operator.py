import bpy,bmesh,math,os,json,shutil
import numpy as np
from mathutils import Vector,Quaternion
ROOT=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
OUT=os.path.join(ROOT,'Assets/Blender/DesertOperator');TEX=os.path.join(OUT,'Textures')
os.makedirs(TEX,exist_ok=True)
scene=bpy.context.scene
assert scene.name.startswith('Desert Operator')
rig=next(o for o in scene.objects if o.type=='ARMATURE');rig.name='DesertOperator_Rig'
parts=[];bindings={};materials={}
source=open(os.path.join(ROOT,'artifacts/build_tactical_soldier.py'),encoding='utf-8').read()
exec(source[source.index('def register('):source.index('# Tailored torso')])
def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def smoothnoise(n,g,seed):
 rng=np.random.default_rng(seed);a=rng.random((g+1,g+1));a[-1]=a[0];a[:,-1]=a[:,0]
 q=np.arange(n)*g/n;i=q.astype(int);f=q-i;f=f*f*(3-2*f)
 return (a[i[:,None],i[None,:]]*(1-f[None,:])+a[i[:,None],i[None,:]+1]*f[None,:])*(1-f[:,None])+(a[i[:,None]+1,i[None,:]]*(1-f[None,:])+a[i[:,None]+1,i[None,:]+1]*f[None,:])*f[:,None]
def bitmap(name,a,color=True,alpha=None):
 n=a.shape[0];rgba=np.ones((n,n,4),np.float32);rgba[:,:,:3]=a[:,:,None] if a.ndim==2 else a
 if alpha is not None:rgba[:,:,3]=alpha
 im=bpy.data.images.new(name,width=n,height=n,alpha=True)
 im.colorspace_settings.name='sRGB' if color else 'Non-Color'
 im.pixels.foreach_set(rgba.ravel());im.filepath_raw=os.path.join(TEX,name+'.png')
 im.file_format=next(i.identifier for i in im.bl_rna.properties['file_format'].enum_items if i.identifier=='PNG');im.save();im.pack();return im
def mat(name,col,rough=.7,metal=0,kind=None):
 m=bpy.data.materials.new('DO_'+name);m.use_nodes=True;m.diffuse_color=(*col,1)
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 if kind:
  n=2048 if kind=='camo' else 1024;yy,xx=np.mgrid[:n,:n];fine=smoothnoise(n,155,25);broad=smoothnoise(n,17,4)
  weave=(np.sin(xx*np.pi)*np.sin(yy*np.pi*.5)*.5+.5)
  rgb=np.ones((n,n,3))*np.array(col)
  if kind=='camo':
   # Multi-scale sandy digital pattern: block boundaries, large muted fields, smaller flecks.
   nn=256;f=smoothnoise(nn,14,19)*.66+smoothnoise(nn,40,37)*.34
   palette=np.array([[.31,.30,.245],[.45,.44,.36],[.58,.55,.45],[.70,.665,.55],[.77,.735,.64]])
   ix=np.digitize(f,[.30,.40,.52,.65]);rgb=np.repeat(np.repeat(palette[ix],8,0),8,1)
   fleck=np.repeat(np.repeat(smoothnoise(nn,80,11)>.77,8,0),8,1);rgb=np.where(fleck[:,:,None],np.array([.38,.365,.29]),rgb)
  rgb*=((.94+.08*fine+.022*weave)[:,:,None]);height=(fine-.5)*.03+weave*.022
  if kind in ['camo','cloth']:
   rip=((xx%64<2)|(yy%64<2)).astype(float);height+=rip*.015;rgb*=1-rip[:,:,None]*.022
  if kind=='suede':height+=(broad-.5)*.04
  dx=(np.roll(height,-1,1)-np.roll(height,1,1))*2;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*2
  normal=np.dstack((-dx,-dy,np.ones_like(dx)));normal/=np.linalg.norm(normal,axis=2)[:,:,None]
  imgs={'BaseColor':bitmap(m.name+'_BaseColor',np.clip(rgb,0,1)), 'Normal':bitmap(m.name+'_Normal',normal*.5+.5,False), 'Roughness':bitmap(m.name+'_Roughness',np.clip(rough+(fine-.5)*.14,0,1),False)}
  bitmap(m.name+'_MetallicSmoothness',np.ones((n,n))*metal,False,1-np.clip(rough+(fine-.5)*.14,0,1))
  for k,im in imgs.items():
   nd=m.node_tree.nodes.new('ShaderNodeTexImage');nd.image=im
   if k=='Normal':
    nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.38;m.node_tree.links.new(nd.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],p.inputs['Normal'])
   else:m.node_tree.links.new(nd.outputs['Color'],p.inputs['Base Color' if k=='BaseColor' else 'Roughness'])
 materials[name]=m;return m
camo=mat('Desert_Ripstop',(.62,.59,.49),.81,kind='camo')
cloth=mat('Coyote_Cordura',(.47,.405,.275),.85,kind='cloth')
web=mat('Webbing',(.57,.51,.375),.84,kind='cloth')
suede=mat('Boot_Suede',(.53,.435,.30),.85,kind='suede')
black=mat('Balaclava',(.055,.06,.052),.92,kind='cloth')
rubber=mat('Rubber',(.12,.115,.09),.72)
shell=mat('Composite',(.40,.365,.26),.7,kind='cloth')
metal=mat('Hardware',(.21,.205,.17),.48,.65)
lens=mat('Smoked_Lens',(.018,.027,.025),.15,.25)
thread=mat('Seam_Thread',(.61,.55,.41),.84)
velcro=mat('Velcro',(.24,.235,.19),.96,kind='cloth')
# Retain the continuously skinned garment foundation, rebuilt in the earlier anatomical pass.
body=scene.objects['Mesh_Soldier_Camouflage'];body.name='Uniform_Ripstop'
leath=scene.objects['Mesh_Soldier_Leather'];leath.name='Gloves_and_Boot_Uppers'
sole=scene.objects['Mesh_Soldier_Rubber'];sole.name='Outsoles_and_Glove_Reinforcement'
bm=bmesh.new();bm.from_mesh(sole.data);dl=bm.verts.layers.deform.active
keep={g.index for g in sole.vertex_groups if g.name.startswith(('Foot_','Hand_'))}
for v in list(bm.verts):
 if not any(v[dl].get(i,0)>.5 for i in keep):bm.verts.remove(v)
bm.to_mesh(sole.data);bm.free()
for o in list(scene.objects):
 if o not in [rig,body,leath,sole]:bpy.data.objects.remove(o,do_unlink=True)
for o,m,ratio in [(body,camo,.48),(leath,suede,.27),(sole,rubber,1)]:
 o.data.materials.clear();o.data.materials.append(m)
 if ratio<1:
  active(o);dec=o.modifiers.new('Realtime surface density','DECIMATE');dec.ratio=ratio;bpy.ops.object.modifier_apply(modifier=dec.name)
 # Separate material sets keep equipment editable and preserve all joint weights.
 for p in o.data.polygons:p.use_smooth=True
parts=[];bindings={}

def patch(name,outline,y,depth,mat,bone,curve=.5,bevel=.004):
 # Solid tailored front/back panel following an elliptical rib cage.
 verts=[]
 for d in [0,depth]:
  for x,z in outline:verts.append((x,y+curve*x*x+d,z))
 N=len(outline);faces=[tuple(reversed(range(N))),tuple(N+i for i in range(N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);register(o,name,mat,bone)
 active(o);bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
 if bevel:
  b=o.modifiers.new('Bound fabric edges','BEVEL');b.width=bevel;b.segments=3;bpy.ops.object.modifier_apply(modifier=b.name)
  w=o.modifiers.new('Panel normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=w.name)
 return o
def stitch(name,pts,mat=thread,bone='Chest',r=.00065):return line(name,pts,r,mat,bone)
def tape(name,x0,x1,z,y,bone='Chest',curve=.5,width=.016,mat=web):
 steps=10;verts=[];faces=[]
 for i in range(steps+1):
  x=x0+(x1-x0)*i/steps;yy=y+curve*x*x-.0015*math.sin(i*math.pi/steps)
  verts.extend([(x,yy,z-width/2),(x,yy,z+width/2)])
 for i in range(steps):faces.append((i*2,i*2+2,i*2+3,i*2+1))
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);scene.collection.objects.link(o);register(o,name,mat,bone)
 active(o);so=o.modifiers.new('Woven tape thickness','SOLIDIFY');so.thickness=.002;bpy.ops.object.modifier_apply(modifier=so.name)
 return o

# Contoured plate carrier: chamfered arm openings, bound perimeter and curved MOLLE lanes.
outline=[(-.158,1.105),(-.18,1.14),(-.178,1.33),(-.135,1.385),(-.124,1.445),(.124,1.445),(.135,1.385),(.178,1.33),(.18,1.14),(.158,1.105)]
patch('Carrier_front_shaped_shell',outline,-.163,.042,cloth,'Chest',.65,.008)
patch('Carrier_back_shaped_shell',outline,.133,.040,cloth,'Chest',-.5,.008)
for back in [False,True]:
 y=.176 if back else -.168;curve=-.5 if back else .65
 stitch('Carrier_bound_edge',[(x,y+curve*x*x,z) for x,z in outline]+[(outline[0][0],y+curve*outline[0][0]**2,outline[0][1])],web,'Chest',.0025)
 for row in range(7):
  z=1.135+row*.040;xw=.153 if row<5 else .12
  tape('MOLLE_back' if back else 'MOLLE_front',-xw,xw,z,y+(.003 if back else -.003),curve=curve)
  for x in np.arange(-xw+.018,xw,.047):
   stitch('MOLLE_bar_tack',[(x,y+curve*x*x+(.006 if back else -.007),z-.007),(x,y+curve*x*x+(.006 if back else -.007),z+.007)],bone='Chest',r=.0007)
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'
 line('Padded_shoulder_bridge_'+s,[(sgn*.12,-.14,1.407),(sgn*.15,-.065,1.486),(sgn*.15,.035,1.496),(sgn*.125,.15,1.41)],.018,cloth,'Chest')
 line('Shoulder_webbing_'+s,[(sgn*.12,-.165,1.41),(sgn*.15,-.084,1.489),(sgn*.15,.037,1.501),(sgn*.125,.175,1.41)],.009,web,'Chest')
 cube('Shoulder_adjuster_'+s,(sgn*.13,-.153,1.44),(.037,.012,.027),metal,'Chest',.004)
 # Side cummerbund follows the ribs rather than leaving a floating box.
 line('Carrier_cummerbund_'+s,[(sgn*.15,-.14,1.19),(sgn*.205,-.06,1.19),(sgn*.21,.04,1.19),(sgn*.15,.15,1.19)],.027,cloth,'Chest')
 for z in [1.17,1.205]:line('Cummerbund_weave_'+s,[(sgn*.18,-.11,z),(sgn*.224,0,z),(sgn*.18,.13,z)],.006,web,'Chest')
# Three soft rectangular magazine pouches with side gussets, flaps and sewn borders.
for i in [-1,0,1]:
 x=i*.099;y=-.218+.65*x*x
 po=cube('Front_magazine_pouch_'+str(i),(x,y,1.194),(.091,.070,.157),cloth,'Chest',.012)
 # Subtle cloth fullness breaks the perfect planar box.
 for v in po.data.vertices:
  v.co.y+=.0025*math.sin(v.co.z*65)*math.cos(v.co.x*50)
 patch('Pouch_overlap_flap',[(x-.043,1.28),(x+.043,1.28),(x+.042,1.233),(x,1.221),(x-.042,1.233)],y-.039,.006,web,'Chest',0,.003)
 stitch('Pouch_stitched_outline',[(x-.036,y-.039,1.13),(x-.036,y-.039,1.247),(x+.036,y-.039,1.247),(x+.036,y-.039,1.13)],r=.0009)
 tape('Pouch_webbing_loop',x-.034,x+.034,1.175,y-.039,curve=0,width=.014)
 cube('Pouch_pull_tab',(x,y-.047,1.234),(.014,.004,.028),cloth,'Chest',.002)
# Low profile rear hydration sleeve and asymmetric radio.
cube('Rear_hydration_sleeve',(0,.202,1.285),(.226,.065,.275),cloth,'Chest',.022)
for row in range(5):tape('Rear_pack_webbing',-.095,.095,1.19+row*.041,.239,curve=0,width=.017)
stitch('Hydration_seam',[(-.10,.238,1.17),(-.10,.238,1.405),(.10,.238,1.405),(.10,.238,1.17)],r=.001)
line('Hydration_handle',[(-.038,.215,1.418),(-.035,.215,1.449),(.035,.215,1.449),(.038,.215,1.418)],.006,web,'Chest')
cube('Radio_side_pouch',(-.192,.072,1.245),(.053,.074,.142),cloth,'Chest',.009)
line('Radio_antenna',[(-.199,.087,1.31),(-.205,.088,1.48),(-.211,.088,1.54)],.0025,rubber,'Chest')
line('Hydration_hose',[(.08,.22,1.40),(.17,.11,1.50),(.18,-.06,1.47),(.17,-.15,1.34)],.004,rubber,'Chest')

# Waist belt with individual sewn loops, quick-release buckle and accessory pouches.
loft('Padded_waist_belt',(0,0,.986),(0,0,1.039),[(0,.181,.127),(1,.18,.125)],cloth,'Hips',0,segments=64,rings=4)
loft('Belt_webbing',(0,0,1.003),(0,0,1.031),[(0,.185,.131),(1,.184,.13)],web,'Hips',0,segments=64,rings=3)
cube('Quick_release_buckle',(0,-.139,1.018),(.057,.016,.039),metal,'Hips',.006)
cube('Buckle_inset',(0,-.149,1.018),(.032,.005,.025),rubber,'Hips',.004)
for a in np.linspace(0,2*math.pi,12,endpoint=False):
 x=.187*math.cos(a);y=.134*math.sin(a);o=cube('Belt_keeper',(x,y,1.016),(.014,.014,.052),cloth,'Hips',.002)
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'
 cube('Belt_utility_pouch_'+s,(sgn*.204,-.011,.983),(.073,.105,.119),cloth,'Hips',.015)
 cube('Belt_pouch_flap_'+s,(sgn*.205,-.065,1.02),(.073,.008,.045),web,'Hips',.006)
 cube('Rear_belt_pouch_'+s,(sgn*.115,.139,1.002),(.084,.064,.087),cloth,'Hips',.011)
 # Angled cargo-pocket opening and flap wrap the outer thigh.
 cube('Cargo_flap_'+s,(sgn*.212,.005,.815),(.014,.137,.046),camo,'Thigh_'+s,.006)
 for y in [-.05,.05]:sphere('Cargo_snap_'+s,(sgn*.221,y,.81),(.002,.004,.004),rubber,'Thigh_'+s)
 # Raised elbow pads and cuff hems are soft cloth, not armor plates.
 sphere('Elbow_reinforcement_'+s,(sgn*.315,.052,1.17),(.055,.014,.068),camo,'Forearm_'+s)
 loft('Sleeve_cuff_'+s,(sgn*.359,-.014,.981),(sgn*.368,-.014,.945),[(0,.045,.043),(1,.042,.040)],camo,'Forearm_'+s,.0007,rings=5,segments=24)
 cube('Cuff_adjustment_tab_'+s,(sgn*.385,-.041,.966),(.033,.008,.014),cloth,'Forearm_'+s,.003)
 # Soft oval kneepad surround and a molded, faceted hard cap with inset ribs.
 sphere('Knee_textile_surround_'+s,(sgn*.115,-.072,.548),(.073,.030,.098),cloth,'Shin_'+s)
 cap=sphere('Knee_molded_cap_'+s,(sgn*.115,-.098,.55),(.059,.020,.078),shell,'Shin_'+s)
 for z,dx in [(.515,.037),(.58,.037)]:stitch('Knee_flex_channel',[(sgn*.115-dx,-.115,z),(sgn*.115,-.121,z-.003),(sgn*.115+dx,-.115,z)],rubber,'Shin_'+s,.0018)
 # Upper arm Velcro panels, welt seam, and sparse stitching at the pocket opening.
 cube('Sleeve_velcro_panel_'+s,(sgn*.278,-.089,1.344),(.064,.007,.055),web,'UpperArm_'+s,.005)
 stitch('Sleeve_pocket_seam_'+s,[(sgn*.278-.037,-.09,1.302),(sgn*.278-.037,-.09,1.383),(sgn*.278+.037,-.09,1.383)],bone='UpperArm_'+s,r=.0009)
 # Boot tongue, lace eyelets, crosses, toe seam and ankle stitching conform to the upper.
 x=sgn*.12
 for j in range(8):
  z=.113+j*.016;y=-.09+.35*(z-.113)
  for sign in [-1,1]:
   sphere('Boot_eyelet',(x+sign*.031,y+.004,z),(.004,.002,.004),metal,'Foot_'+s)
   line('Boot_lace',[(x+sign*.029,y,z),(x-sign*.028,y-.002,z+.013)],.0014,web,'Foot_'+s)
 stitch('Boot_toe_cap_seam',[(x-.044,-.129,.092),(x-.027,-.147,.098),(x,-.154,.101),(x+.027,-.147,.098),(x+.044,-.129,.092)],web,'Foot_'+s,.001)
 for sign in [-1,1]:stitch('Boot_ankle_seam',[(x+sign*.046,.024,.083),(x+sign*.05,.03,.16),(x+sign*.047,.026,.229)],web,'Foot_'+s,.001)

# A contoured covered skull and jaw, independently shaped from the helmet shell.
loft('Covered_neck',(0,.005,1.47),(0,.005,1.64),[(0,.073,.072),(.4,.071,.075),(1,.079,.081)],black,'Neck',.001,segments=40,rings=14)
loft('Balaclava_skull_and_jaw',(0,.003,1.545),(0,.003,1.795),[(0,.047,.055),(.13,.065,.073),(.35,.087,.096),(.65,.094,.103),(.85,.078,.087),(1,.035,.043)],black,'Head',.0005,segments=48,rings=28)
patch('Molded_lower_face_mask',[(-.073,1.655),(-.042,1.666),(0,1.663),(.042,1.666),(.073,1.655),(.074,1.606),(.052,1.568),(0,1.552),(-.052,1.568),(-.074,1.606)],-.109,.013,black,'Head',1.25,.007)
for x in [-.047,0,.047]:stitch('Face_mask_panel_seam',[(x,-.114+1.25*x*x,1.57),(x*.95,-.118+1.25*x*x,1.61),(x*.83,-.113+1.25*x*x,1.65)],black,'Head',.0008)
# High cut helmet in cloth-covered composite, with a true rim and panel seams.
nv=64;nr=20;verts=[];faces=[]
for j in range(nr):
 for i in range(nv):
  phi=2*math.pi*i/nv;lim=1.65-.27*abs(math.cos(phi))**6;th=.02+(lim-.02)*j/(nr-1)
  rr=1+.008*math.sin(phi*7+th*5)
  verts.append((.123*math.sin(th)*math.cos(phi)*rr,.136*math.sin(th)*math.sin(phi)*rr+.013,1.72+.125*math.cos(th)))
for j in range(nr-1):
 for i in range(nv):a=j*nv+i;b=j*nv+(i+1)%nv;faces.append((a,b,b+nv,a+nv))
faces.append(tuple(reversed(range(nv))))
me=bpy.data.meshes.new('Helmet cover surface');me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new('Helmet_cloth_cover',me);scene.collection.objects.link(o);register(o,o.name,camo,'Head')
active(o);mod=o.modifiers.new('Composite shell thickness','SOLIDIFY');mod.thickness=.006;bpy.ops.object.modifier_apply(modifier=mod.name)
line('Helmet_bound_rim',verts[-nv:]+[verts[-nv]],.0035,shell,'Head')
for phi in [-2.25,-.9,.9,2.25]:
 pts=[(.124*math.sin(th)*math.cos(phi),.137*math.sin(th)*math.sin(phi)+.013,1.72+.126*math.cos(th)) for th in np.linspace(.13,1.4,25)]
 stitch('Helmet_cover_panel_seam',pts,web,'Head',.0011)
for sgn in [-1,1]:
 s='L' if sgn<0 else 'R'
 # Helmet rails are tapered, with negative-looking rubber recesses and real screw heads.
 cube('Helmet_ARC_rail_'+s,(sgn*.121,.015,1.744),(.014,.13,.025),shell,'Head',.006)
 for k in range(5):cube('Rail_recess',(sgn*.13,-.03+k*.021,1.744),(.002,.011,.010),rubber,'Head',.003)
 for y in [-.04,.068]:sphere('Rail_fastener',(sgn*.132,y,1.744),(.002,.004,.004),metal,'Head')
 sphere('Headset_ear_cup_'+s,(sgn*.103,.014,1.676),(.023,.043,.05),rubber,'Head')
 sphere('Headset_outer_shell_'+s,(sgn*.122,.014,1.678),(.009,.033,.038),shell,'Head')
 line('Headset_yoke_'+s,[(sgn*.126,-.019,1.68),(sgn*.139,-.01,1.72),(sgn*.125,.02,1.738)],.003,metal,'Head')
 line('Retention_chin_strap_'+s,[(sgn*.09,.05,1.70),(sgn*.086,.012,1.60),(sgn*.061,-.053,1.559),(sgn*.025,-.078,1.556)],.0045,cloth,'Head')
 cube('Strap_buckle_'+s,(sgn*.078,-.015,1.584),(.015,.009,.021),metal,'Head',.002)
 cube('Helmet_velcro_'+s,(sgn*.105,.025,1.80),(.012,.057,.032),velcro,'Head',.004)
 line('Helmet_bungee_'+s,[(sgn*.031,-.122,1.764),(sgn*.065,-.108,1.796),(sgn*.082,-.052,1.827)],.002,rubber,'Head')
cube('NVG_shroud',(0,-.127,1.776),(.043,.012,.058),shell,'Head',.006)
cube('NVG_socket',(0,-.136,1.774),(.025,.007,.034),metal,'Head',.004)
cube('NVG_latch',(0,-.141,1.786),(.016,.005,.009),rubber,'Head',.002)
for x in [-.016,.016]:
 for z in [1.755,1.797]:sphere('NVG_screw',(x,-.135,z),(.0025,.002,.0025),metal,'Head')
cube('Rear_helmet_velcro',(0,.145,1.75),(.084,.009,.033),velcro,'Head',.005)
cube('Helmet_top_patch',(0,.012,1.846),(.043,.06,.004),velcro,'Head',.003)
# Lens is a tessellated curved surface, so reflections follow its shape from every view.
outline=[(-.096,1.704),(-.065,1.713),(0,1.712),(.065,1.713),(.096,1.704),(.099,1.666),(.080,1.652),(.031,1.653),(.018,1.674),(0,1.681),(-.018,1.674),(-.031,1.653),(-.080,1.652),(-.099,1.666)]
coords=[(x,-.121+1.8*x*x,z) for x,z in outline]
line('Goggle_sealed_frame',coords+[coords[0]],.006,rubber,'Head')
center=(0,-.123,1.693);verts=[center]+coords;faces=[(0,i+1,(i+1)%len(coords)+1) for i in range(len(coords))]
me=bpy.data.meshes.new('Curved ballistic lens');me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new('Goggle_smoked_lens',me);scene.collection.objects.link(o);register(o,o.name,lens,'Head')
for sgn in [-1,1]:
 line('Goggle_retention_band',[(sgn*.099,-.102,1.681),(sgn*.115,-.02,1.694),(sgn*.104,.085,1.694),(sgn*.034,.126,1.696)],.009,black,'Head')
cube('Goggle_rear_adjuster',(0,.133,1.695),(.055,.009,.021),rubber,'Head',.003)

# UV all new pieces and retain functional object names; combine only repetitive micro details.
for ob in list(scene.objects):
 if ob.type!='MESH' or ob in [body,leath,sole]:continue
 if not ob.data.uv_layers:
  active(ob);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
 bone=bindings.get(ob,'Chest');vg=ob.vertex_groups.new(name=bone);vg.add(list(range(len(ob.data.vertices))),1,'REPLACE');ob.parent=rig;mod=ob.modifiers.new('Deform rig','ARMATURE');mod.object=rig
 # Small fabric panels use the same texel density as the uniform, not a full camo map per patch.
 if ob.data.materials[0]==camo:
  for uv in ob.data.uv_layers.active.data:uv.uv*=.65 if 'Helmet' in ob.name else .35
micro=[o for o in scene.objects if o.type=='MESH' and any(t in o.name for t in ['tack','screw','fastener','eyelet','lace','seam','MOLLE','webbing','keeper','Rail_recess'])]
for material in materials.values():
 group=[o for o in micro if len(o.data.materials) and o.data.materials[0]==material]
 if len(group)<2:continue
 bpy.ops.object.select_all(action='DESELECT')
 for o in group:o.select_set(True)
 bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join();group[0].name='Details_'+material.name
 # Avoid touching stale Blender object references after joining.
 micro=[o for o in scene.objects if o.type=='MESH' and any(t in o.name for t in ['tack','screw','fastener','eyelet','lace','seam','MOLLE','webbing','keeper','Rail_recess'])]
scene.unit_settings.scale_length=1
rig['reference']='Four user-provided front/back/left/right desert operator photographs'
rig['height_m']=1.85
scene['build']='Desert operator reference rebuild 2026-10-27'
print(json.dumps({'objects':len(scene.objects),'vertices':sum(len(o.data.vertices) for o in scene.objects if o.type=='MESH'),'materials':len(materials)}))
