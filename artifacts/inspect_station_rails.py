import bpy,json,os
from mathutils import Vector
root=os.getcwd()
report={'file':bpy.data.filepath,'scenes':[s.name for s in bpy.data.scenes],'objects':[]}
for o in bpy.data.objects:
 if o.type=='MESH' and ('Graphite' in o.name and 'Details' in o.name or any(x in o.name.lower() for x in ['rail','shoe'])):
  components=[]; me=o.data;adj=[[] for _ in me.vertices]
  for e in me.edges:
   a,b=e.vertices;adj[a].append(b);adj[b].append(a)
  seen=set()
  for v in me.vertices:
   if v.index in seen:continue
   stack=[v.index];seen.add(v.index);ids=[]
   while stack:
    i=stack.pop();ids.append(i)
    for j in adj[i]:
     if j not in seen:seen.add(j);stack.append(j)
   pts=[o.matrix_world@me.vertices[i].co for i in ids];lo=[min(p[k] for p in pts) for k in range(3)];hi=[max(p[k] for p in pts) for k in range(3)]
   components.append({'index':len(components),'verts':len(ids),'min':[round(v,4) for v in lo],'max':[round(v,4) for v in hi],'size':[round(hi[k]-lo[k],4) for k in range(3)]})
  report['objects'].append({'name':o.name,'parts':components})
with open(os.path.join(root,'artifacts/station_rails_inspection.json'),'w') as f:json.dump(report,f,indent=2)
print('INSPECTED',[(o['name'],len(o['parts'])) for o in report['objects']])
