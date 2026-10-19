using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
public static class TestSnowPlayer {
 static void Teleport(SnowFirstPerson p,Vector3 at,float yaw){var c=p.GetComponent<CharacterController>();c.enabled=false;p.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));c.enabled=true;Physics.SyncTransforms();}
 public static async Task<object> Run(){
  if(!EditorApplication.isPlaying)throw new Exception("Play mode required");EditorApplication.isPaused=false;Application.runInBackground=true;
  var p=UnityEngine.Object.FindAnyObjectByType<SnowFirstPerson>();var start=p.transform.position;var rotation=p.transform.rotation;var cameraRotation=p.viewCamera.transform.localRotation;p.enabled=false;
  try{
   Teleport(p,new Vector3(14,.04f,-79),0);for(int i=0;i<60;i++)p.Move(Vector2.up,false,1f/60);var straight=p.transform.position.z+79;if(Mathf.Abs(straight-2.8f)>.15f)throw new Exception("Forward movement failed "+straight);
   Teleport(p,new Vector3(14,.04f,-79),0);for(int i=0;i<60;i++)p.Move(Vector2.one,false,1f/60);var delta=p.transform.position-new Vector3(14,p.transform.position.y,-79);if(Mathf.Abs(delta.magnitude-straight)>.15f)throw new Exception("Diagonal speed not normalized");
   var camOffset=p.viewCamera.transform.position-p.transform.position;p.Look(new Vector2(8,3),.1f);if(Mathf.Abs(p.transform.eulerAngles.y)<1)throw new Exception("Mouse yaw failed");if(Vector3.Distance(p.viewCamera.transform.localPosition,new Vector3(0,.77f,.02f))>.001f)throw new Exception("Camera not attached");
   var lab=GameObject.Find("Research Facility - Snow Site");RaycastHit wall;if(!Physics.Raycast(new Vector3(-20,.5f,-75),Vector3.forward,out wall,80))throw new Exception("No lab wall collision");
   Teleport(p,new Vector3(wall.point.x,.04f,wall.point.z-3),0);for(int i=0;i<180;i++)p.Move(Vector2.up,false,1f/60);float wallGap=wall.point.z-p.transform.position.z;
   var motor=p.GetComponent<CharacterController>();Vector3 penetrationDirection;float penetrationDepth;
   bool penetrates=Physics.ComputePenetration(motor,p.transform.position,p.transform.rotation,wall.collider,wall.collider.transform.position,wall.collider.transform.rotation,out penetrationDirection,out penetrationDepth);
   if(wallGap<.08f||wallGap>1||(penetrates&&penetrationDepth>.03f))throw new Exception("Lab wall collision failed gap="+wallGap+" penetration="+penetrationDepth);
   var truck=GameObject.Find("Military Truck - Forecourt").GetComponent<BoxCollider>();var b=truck.bounds;Teleport(p,new Vector3(b.center.x,.04f,b.min.z-3),0);for(int i=0;i<240;i++)p.Move(Vector2.up,false,1f/60);
   bool truckPenetrates=Physics.ComputePenetration(motor,p.transform.position,p.transform.rotation,truck,truck.transform.position,truck.transform.rotation,out penetrationDirection,out penetrationDepth);
   if(truckPenetrates&&penetrationDepth>.03f)throw new Exception("Entered truck collider depth="+penetrationDepth);
   var ground=GameObject.Find("Snow-covered ground 400 x 400 m").GetComponent<MeshCollider>();RaycastHit hit;ground.Raycast(new Ray(new Vector3(186,50,0),Vector3.down),out hit,100);Teleport(p,new Vector3(186,hit.point.y+.04f,0),90);for(int i=0;i<240;i++)p.Move(Vector2.up,true,1f/60);if(p.transform.position.x>188.8f||p.transform.position.x<187)throw new Exception("Boundary collision failed "+p.transform.position);
   float boundaryX=p.transform.position.x;int first=Time.frameCount;await Task.Delay(1200);if(Time.frameCount<=first)throw new Exception("Game loop stalled");
   return new{forwardMeters=straight,diagonalMeters=delta.magnitude,labWallGap=wallGap,boundaryStopX=boundaryX,truckCollision=true,cameraFollows=true,frameStart=first,frameEnd=Time.frameCount,particles=p.snowfall.particleCount};
  }finally{Teleport(p,start,rotation.eulerAngles.y);p.viewCamera.transform.localRotation=cameraRotation;p.enabled=true;}
 }
}
