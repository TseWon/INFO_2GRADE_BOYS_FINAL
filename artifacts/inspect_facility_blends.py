import bpy,os,json
root=r'C:\Users\user\OneDrive\문서\GitHub\INFO_2GRADE_BOYS_FINAL'
for rel in [r'Assets\Blender\abandoned_research_facility.blend',r'artifacts\abandoned_research_facility_before_material_fix.blend',r'artifacts\surface_station\SurfaceResearch_frame.blend',r'artifacts\surface_station\SurfaceResearch_editable_parts.blend',r'artifacts\surface_station\SurfaceResearch_detailed.blend',r'Assets\Blender\SurfaceResearchStation\SurfaceResearchStation.blend']:
 p=os.path.join(root,rel)
 try:
  with bpy.data.libraries.load(p,link=False) as (src,dst):
   dst.scenes=src.scenes[:]
  sc=dst.scenes[0] if dst.scenes else None
  objs=[]
  if sc:
   for o in sc.objects:
    if o.type in {'MESH','CURVE'}:
     objs.append({'name':o.name,'type':o.type,'loc':list(o.location),'dim':list(o.dimensions),'verts':len(o.data.vertices) if o.type=='MESH' else 0})
  print(json.dumps({'file':rel,'scenes':[x.name for x in dst.scenes],'objects':objs[:45],'total':len(objs)}))
  for sc in dst.scenes:bpy.data.scenes.remove(sc)
 except Exception as e:print(json.dumps({'file':rel,'error':str(e)}))
