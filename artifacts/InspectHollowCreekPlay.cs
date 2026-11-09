using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using HollowCreek;
public static class InspectHollowCreekPlay
{
 public static async Task<string> Research()
 {
  HollowCreekSession.NewGame(); HollowCreekSession.Enter(3); HollowCreekSession.Rules.route=Route.Prison;
  await SceneManager.LoadSceneAsync("SoloScene3"); await Task.Delay(200);
  var c=UnityEngine.Object.FindAnyObjectByType<HollowCreekCampaign>();
  c.hud.Document("QA", "Physical B2 route");
  c.player.InputBlocked=false;
  for(int i=0;i<150;i++)c.player.Move(Vector2.up,false,1f/60);
  var before=c.player.transform.position;
  if(before.x>100)throw new Exception("Cage exit blocked: "+before);
  c.Rules.SolveSliding(new[]{1,2,3,4,5,6,7,8,0});c.AfterPuzzle();
  for(int i=0;i<750;i++)c.player.Move(Vector2.up,false,1f/60);
  var after=c.player.transform.position;
  c.hud.Close();
  if(after.x>70)throw new Exception("B2 research corridor blocked: "+after);
  c.player.Teleport(new Vector3(104,.1f,0),Quaternion.Euler(0,270,0));
  UnityEngine.Object.FindObjectsByType<HollowCreekTarget>(FindObjectsInactive.Exclude).First(x=>x.interaction==Interaction.Sliding).campaign=c;
  c.hud.Sliding(new SlidingPuzzle(19531224),()=>{});
  var result="B2 cage exit="+before+"; physical research traversal="+after;
  File.WriteAllText("artifacts/hollow-creek/physical-research.txt",result);return result;
 }
 public static async Task<string> UpperView()
 {
  HollowCreekSession.NewGame(); HollowCreekSession.Enter(3);HollowCreekSession.Rules.route=Route.Upper;
  await SceneManager.LoadSceneAsync("SoloScene3");await Task.Delay(200);
  var c=UnityEngine.Object.FindAnyObjectByType<HollowCreekCampaign>();
  c.player.Teleport(new Vector3(0,.1f,-14),Quaternion.Euler(0,180,0));
  return "B1 research view ready";
 }
}
