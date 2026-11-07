using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FacilityEscape;
using UnityEngine;

public static class TestEscapeCampaignLan
{
    const string Path="artifacts/campaign-client.json";
    static MultiEscapeCampaign c;
    static MultiEscapeLanSession lan;
    static int command,checks;
    static MultiEscapeLanProbe.Report Report()
    {try{return JsonUtility.FromJson<MultiEscapeLanProbe.Report>(File.ReadAllText(Path));}catch(IOException){return null;}}
    static void Check(bool yes,string label){if(!yes)throw new Exception(label);checks++;}
    static async Task Wait(Func<MultiEscapeLanProbe.Report,bool> condition,string label)
    {
        double deadline=Time.realtimeSinceStartupAsDouble+10;
        while(Time.realtimeSinceStartupAsDouble<deadline)
        {
            var r=Report();if(r!=null&&DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()-r.timestamp<3000&&condition(r)){checks++;return;}
            await Task.Delay(100);
        }
        throw new Exception("Timeout: "+label+" / "+JsonUtility.ToJson(Report()));
    }
    static int Send(string action,int id=0,Vector3 position=default)
    {
        command++;File.WriteAllText(Path+".command.json",JsonUtility.ToJson(new MultiEscapeLanProbe.Command {id=command,action=action,controlId=id,position=position}));return command;
    }
    static async Task MoveB(Vector3 p)
    {int id=Send("move",position:p);await Wait(r=>r.command==id&&Vector3.Distance(c.playerB.transform.position,p)<.25f,"B position received by host");}
    static void MoveA(Vector3 p){c.playerA.GetComponent<MultiEscapeWalk>().Warp(p);Physics.SyncTransforms();}
    static async Task ClientAction(int id)
    {int cmd=Send("campaign",id);await Wait(r=>r.command==cmd,"client processed action "+id);await Task.Delay(180);}
    public static string Host()
    {
        c=UnityEngine.Object.FindAnyObjectByType<MultiEscapeCampaign>();lan=c.game.Lan;lan.Host();return lan.Status;
    }
    public static async Task<string> Run()
    {
        c=UnityEngine.Object.FindAnyObjectByType<MultiEscapeCampaign>();lan=c.game.Lan;checks=0;command=Report()?.command??0;
        await Wait(r=>r.connected&&lan.Connected,"two processes connected");
        Check(lan.IsHost&&Report().bInput,"host A client B ownership");
        c.game.StartRound();
        UnityEngine.Object.FindAnyObjectByType<MultiEscapeLocalSession>().SetMouseCaptured(false);
        await Wait(r=>r.state.round==c.game.RoundNumber&&r.state.stage==EscapeStage.ColorLock,"shared chapter one");
        // A wrong answer damages both replicas, then the unchanged chair puzzles release them.
        c.game.PressColor(c.playerA,(LockColor)(((int)c.game.Rules.GetColorAnswer(0)+1)%4));
        await Wait(r=>r.state.heartsA==2&&r.state.heartsB==2,"shared mistake damage");
        await Task.Delay(3100);
        for(int i=0;i<c.game.Rules.ColorCount;i++)c.game.PressColor(c.playerA,c.game.Rules.GetColorAnswer(i));
        for(int i=0;i<3;i++)for(int n=0;n<(int)c.game.Rules.GetSymbolAnswer(i);n++)c.game.RotateDial(c.playerA,i,1);
        c.game.PullLever(c.playerA);
        await Wait(r=>r.state.campaign!=null&&r.state.campaign.chapter==EscapeChapter.Search,"client enters chapter two");
        Check(Report().canSendVoice&&Report().canReceiveVoice,"two way voice remains unlocked");
        MoveA(new Vector3(-.4f,.05f,18.6f));await MoveB(new Vector3(2.25f,.05f,18));await ClientAction(6);
        await Wait(r=>r.campaignFeedback!=null&&r.campaignFeedback.Contains("증거 3개"),"client sees rejected puzzle input reason");
        await MoveB(new Vector3(3,.05f,13.1f));await ClientAction(1);
        await Wait(r=>r.state.campaign.evidence==2&&c.State.evidence==2,"B clue replicated");
        MoveA(new Vector3(-3,.05f,10.2f));Check(c.Act(EscapeRole.Blindfolded,0),"A clue zero");
        MoveA(new Vector3(0,.05f,15.9f));Check(c.Act(EscapeRole.Blindfolded,2),"A clue two");
        // Host time controls the entire ambush, including exactly one damage event.
        c.Rules.Advance(139-c.State.elapsed);
        await Wait(r=>r.state.campaign.swarmHit&&r.state.campaign.heartsA==1&&r.state.campaign.heartsB==1,"swarm damage replicated once");
        await MoveB(new Vector3(-.4f,.05f,18.6f));MoveA(new Vector3(2.25f,.05f,18));
        for(int i=0;i<3;i++)for(int n=0;n<c.State.code[i];n++)Check(c.Act(EscapeRole.Blindfolded,3+i),"code dial");
        Check(c.Act(EscapeRole.Blindfolded,6),"host opens evidence door");
        await Wait(r=>r.state.campaign.chapter==EscapeChapter.Chase,"client enters chase");
        await MoveB(new Vector3(1.25f,.05f,41));MoveA(new Vector3(-1.25f,.05f,41));
        Send("campaign-hold",8);
        for(int i=0;i<32;i++){c.Act(EscapeRole.Blindfolded,7);await Task.Delay(120);}
        await Wait(r=>r.state.campaign.gateOpen&&c.State.gateOpen,"network two-person gate");Send("campaign-hold",-1);
        await MoveB(new Vector3(-1.1f,.05f,49.3f));MoveA(new Vector3(.4f,.05f,50.4f));Send("campaign-hold",9);
        await Task.Delay(500);Check(c.Act(EscapeRole.Blindfolded,10),"B boosts A over wall");
        await Wait(r=>r.state.campaign.lifted==1&&r.aPosition.z>53.8f&&r.aPosition.y<.2f&&!c.playerA.GetComponent<MultiEscapeWalk>().IsClimbing,"A boost replicated");Send("campaign-hold",-1);
        MoveA(new Vector3(.4f,.05f,54.1f));Check(c.Act(EscapeRole.Blindfolded,11),"A lowers rope");
        await Wait(r=>r.state.campaign.ropeDown,"rope state replicated");
        await MoveB(new Vector3(.4f,.05f,50.4f));await ClientAction(10);
        await Wait(r=>r.bClimbing&&c.playerB.GetComponent<MultiEscapeWalk>().IsClimbing,"B climb animation runs on both peers");
        await Wait(r=>r.state.campaign.lifted==3&&r.bPosition.z>53.8f&&r.bPosition.y<.2f&&!c.playerB.GetComponent<MultiEscapeWalk>().IsClimbing,"B climbs on both peers");
        MoveA(new Vector3(0,.05f,62));Check(c.Act(EscapeRole.Blindfolded,12),"A exits");
        await MoveB(new Vector3(.4f,.05f,62));await ClientAction(12);
        await Wait(r=>r.state.campaign.chapter==EscapeChapter.Won&&c.State.chapter==EscapeChapter.Won,"shared final victory");
        c.game.StartRound();
        await Wait(r=>r.state.round==c.game.RoundNumber&&r.state.campaign.chapter==EscapeChapter.Chair&&r.state.campaign.lifted==0,"restart clears campaign on both peers");
        int leave=Send("leave");await Wait(r=>r.command==leave&&!r.connected,"client departure");
        await Wait(r=>c.game.Rules.Stage==EscapeStage.Ready&&c.State.chapter==EscapeChapter.Chair,"disconnect returns host to lobby");
        string result="PASS: "+checks+" two-process LAN campaign checks (Editor host + Windows Player client).";
        File.WriteAllText("artifacts/campaign-lan-result.txt",result);return result;
    }
}
