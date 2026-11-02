import bpy,json
print('RECOVERY',bpy.data.filepath,flush=True)
c=bpy.data.collections.get('UNSAVED_Tactical_Soldier')
print('GEAR',len(c.objects) if c else None,flush=True)
print('FINAL DETAILS',[o.name for o in c.objects if any(s in o.name for s in ['TailoredSmoothCollar','BootVelcro','FrontContoured','SewnThigh'])],flush=True)
print('EXPORTCOLL',bpy.data.collections.get('Unity_Export_FittedSoldier'),flush=True)
