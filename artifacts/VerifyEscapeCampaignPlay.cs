using System;
using System.Reflection;
using System.Threading.Tasks;
using FacilityEscape;
using UnityEngine;

public static class VerifyEscapeCampaignPlay
{
    static MultiEscapeCampaign c;
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
    static void Warp(MultiEscapePlayer p,Vector3 where){p.GetComponent<MultiEscapeWalk>().Warp(where);Physics.SyncTransforms();}
    static void Aim(MultiEscapePlayer p,Vector3 target)
    {
        var desired=Quaternion.LookRotation(target-p.ViewCamera.transform.position).eulerAngles;
        var current=p.ViewCamera.transform.eulerAngles;
        p.Look(new Vector2(Mathf.DeltaAngle(current.y,desired.y),-Mathf.DeltaAngle(current.x,desired.x))/2);
        Physics.SyncTransforms();
    }
    static void Act(MultiEscapePlayer p,int id,Vector3 from)
    {
        Warp(p,from);Aim(p,c.targets[id].transform.position);
        Check(Physics.Raycast(p.InteractionRay,out var hit,3.4f)&&hit.collider.GetComponentInParent<MultiEscapeCampaignTarget>()==c.targets[id],"Visible collider "+id);
        Check(c.Act(p.Role,id),"Accepted action "+id);
    }
    public static string Setup()
    {
        if(!Application.isPlaying)throw new Exception("Play Mode required");
        c=UnityEngine.Object.FindAnyObjectByType<MultiEscapeCampaign>();checks=0;
        var ui=UnityEngine.Object.FindAnyObjectByType<MultiEscapeLocalSession>();
        // Only this temporary Play Mode session uses local testing; serialized LAN stays enabled.
        c.game.Lan=null;
        typeof(MultiEscapeLocalSession).GetField("lan",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(ui,null);
        ui.StartRound();ui.SetMouseCaptured(false);
        for(int i=0;i<c.game.Rules.ColorCount;i++)c.game.PressColor(c.playerA,c.game.Rules.GetColorAnswer(i));
        for(int i=0;i<3;i++)for(int j=0;j<(int)c.game.Rules.GetSymbolAnswer(i);j++)c.game.RotateDial(c.playerA,i,1);
        c.game.PullLever(c.playerA);
        Check(c.State.chapter==EscapeChapter.Search,"chair continues to search");
        Warp(c.playerA,new Vector3(0,.05f,10));Aim(c.playerA,new Vector3(0,1.5f,17));
        return "Search ready for capture";
    }
    public static async Task<string> Run()
    {
        Setup();
        await Task.Delay(1400);
        Act(c.playerA,0,new Vector3(-3,.05f,10.2f));
        Act(c.playerB,1,new Vector3(3,.05f,13.1f));
        Act(c.playerA,2,new Vector3(0,.05f,15.9f));
        Check(c.State.evidence==7,"all evidence collected");
        Warp(c.playerB,new Vector3(-.4f,.05f,18.6f));
        for(int i=0;i<3;i++)for(int j=0;j<c.State.code[i];j++)Act(c.playerA,3+i,new Vector3(2.25f,.05f,18));
        Act(c.playerA,6,new Vector3(2.25f,.05f,18));
        Check(c.State.chapter==EscapeChapter.Chase,"door unlock starts chase");
        await Task.Delay(1400);
        // Scan the traversable marked route at capsule height after the door opens.
        Warp(c.playerA,new Vector3(-4,.05f,21));Warp(c.playerB,new Vector3(-4,.05f,22));
        for(int i=2;i<9;i++)
        {
            var from=c.pursuitRoute[i-1].position+Vector3.up;
            var to=c.pursuitRoute[i].position+Vector3.up;
            if(to.z>42)break;
            Check(!Physics.SphereCast(from,.28f,(to-from).normalized,out var hit,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore),"Pursuit route segment "+i+(hit.collider!=null?" blocked by "+hit.collider.name:""));
        }
        Warp(c.playerA,new Vector3(-1.25f,.05f,41));Warp(c.playerB,new Vector3(1.25f,.05f,41));
        for(int i=0;i<24;i++)
        {c.Act(EscapeRole.Blindfolded,7);c.Act(EscapeRole.Observer,8);await Task.Delay(120);}
        Check(c.State.gateOpen,"real-time hold gate");
        await Task.Delay(1000);
        Act(c.playerB,9,new Vector3(-1.1f,.05f,49.3f));
        Act(c.playerA,10,new Vector3(.4f,.05f,50.4f));
        Check(c.playerA.GetComponent<MultiEscapeWalk>().IsClimbing,"climb starts without teleport");
        Check(!c.Act(EscapeRole.Blindfolded,11),"interaction blocked during climb");
        await Task.Delay(1700);
        Check(c.State.lifted==1&&c.playerA.transform.position.z>52,"first player boosted");
        Act(c.playerA,11,new Vector3(.4f,.05f,54.1f));
        Act(c.playerB,10,new Vector3(.4f,.05f,50.4f));
        await Task.Delay(1700);
        Check(c.State.lifted==3,"second player uses rope");
        Act(c.playerA,12,new Vector3(0,.05f,62));
        Check(!c.Rules.Terminal,"first exit waits");
        Act(c.playerB,12,new Vector3(0,.05f,62));
        Check(c.State.chapter==EscapeChapter.Won,"both players win");
        return "PASS: "+checks+" live scene checks (raycast interaction, clear pursuit route, timed gate, boost, rope, two-person ending).";
    }
    public static string Ambush()
    {
        Setup();c.Rules.Advance(134.8);return "Ambush scheduled in 0.2 seconds";
    }
    public static async Task<string> Caught()
    {
        Setup();c.State.chapter=EscapeChapter.Chase;c.State.chaseElapsed=9;
        Warp(c.playerA,c.RoutePosition(0)+new Vector3(.5f,.05f,0));Warp(c.playerB,new Vector3(0,.05f,22));
        await Task.Delay(500);
        Check(c.State.chapter==EscapeChapter.Failed&&c.State.failure.Contains("붙잡"),"killer contact fails team through actual scene geometry");
        return "PASS: live killer contact triggers team failure.";
    }
}
