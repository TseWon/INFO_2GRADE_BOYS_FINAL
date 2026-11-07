using System;
using UnityEngine;

namespace FacilityEscape.Editor
{
    public static class MultiEscapeCampaignChecks
    {
        static int count;
        static void Check(bool value, string name)
        { if (!value) throw new Exception("Campaign check failed: " + name); count++; }
        static MultiEscapeCampaignRules Search(int hearts = 3)
        { var r = new MultiEscapeCampaignRules(); r.Begin(hearts, hearts, new System.Random(19)); return r; }
        static void OpenDoor(MultiEscapeCampaignRules r)
        {
            for (int i=0;i<3;i++) { r.Act(EscapeRole.Observer,i); for(int n=0;n<r.State.code[i];n++) r.Act(EscapeRole.Blindfolded,3+i); }
            Check(r.Act(EscapeRole.Blindfolded,6), "solved evidence door");
        }
        public static string Run()
        {
            count=0;
            var r=Search();
            Check(r.Remaining==180 && r.State.heartsA==3,"three minute room");
            Check(!r.Act(EscapeRole.Observer,6),"uncollected clues reject door");
            r.Advance(120);Check(r.Remaining==60&&!r.Silent&&!r.Swarming,"warning threshold");
            r.Advance(12);Check(r.Silent,"breath stops before ambush");
            r.Advance(3);Check(r.Swarming&&!r.Silent,"ambush starts");
            Check(!r.Act(EscapeRole.Observer,0),"no puzzle operation during swarm");
            r.Advance(4);Check(r.State.swarmHit&&r.State.heartsA==2&&r.State.heartsB==2,"single shared bruise");
            r.Advance(1);Check(r.State.heartsA==2,"damage not repeated");
            r.Advance(40);Check(r.State.chapter==EscapeChapter.Failed,"room timeout");
            Check(!r.Act(EscapeRole.Blindfolded,3),"terminal input ignored");
            r=Search();r.Advance(150);Check(r.State.heartsA==2,"long frame still applies ambush once");
            r=Search(1);r.Advance(139);Check(r.Terminal,"last heart bruise fails team");
            r=Search();OpenDoor(r);
            Check(r.State.chapter==EscapeChapter.Chase&&r.Remaining==100,"chase start");
            Check(!r.Act(EscapeRole.Observer,7),"B cannot hold A switch");
            Check(!r.Act(EscapeRole.Blindfolded,8),"A cannot hold B switch");
            r.Act(EscapeRole.Blindfolded,7);r.Advance(.3);Check(!r.State.gateOpen&&r.State.gateProgress==0,"one player cannot open gate");
            for(int i=0;i<8;i++){r.Act(EscapeRole.Blindfolded,7);r.Act(EscapeRole.Observer,8);r.Advance(.25);}
            Check(r.State.gateOpen,"two distinct players open gate");
            Check(!r.Act(EscapeRole.Observer,10),"cannot climb alone");
            r.Act(EscapeRole.Observer,9);Check(!r.Act(EscapeRole.Observer,10),"cannot boost self");
            r.Advance(.6);Check(!r.Act(EscapeRole.Blindfolded,10),"released support expires");
            r.Act(EscapeRole.Observer,9);Check(r.Act(EscapeRole.Blindfolded,10)&&r.State.lifted==1,"partner boost");
            Check(!r.Act(EscapeRole.Observer,11),"ground player cannot lower rope");
            Check(!r.Act(EscapeRole.Observer,10),"second player needs rope");
            Check(!r.Act(EscapeRole.Blindfolded,11),"climbing player cannot lower rope early");
            r.Advance(1.5);
            Check(r.Act(EscapeRole.Blindfolded,11),"upper player lowers rope");
            Check(r.Act(EscapeRole.Observer,10)&&r.State.lifted==3,"partner climbs rope");
            r.Act(EscapeRole.Blindfolded,12);Check(!r.Terminal,"both players required at exit");
            Check(!r.Act(EscapeRole.Observer,12),"climbing player cannot finish early");
            r.Advance(1.5);
            r.Act(EscapeRole.Observer,12);Check(r.State.chapter==EscapeChapter.Won,"team win");
            r.Advance(1000);Check(r.State.chapter==EscapeChapter.Won,"win stays final");
            var replica=new MultiEscapeCampaignRules();replica.Restore(JsonUtility.FromJson<CampaignState>(JsonUtility.ToJson(r.State)));
            Check(replica.State.ropeDown&&replica.State.lifted==3&&replica.State.chapter==EscapeChapter.Won,"wire snapshot preserves campaign");
            r=Search();OpenDoor(r);r.Act(EscapeRole.Blindfolded,7);r.Act(EscapeRole.Observer,8);r.Advance(10);
            Check(!r.State.gateOpen,"long frame cannot invent held switches");
            r.Advance(100);Check(r.Terminal,"chase timeout");
            var chair=new MultiEscapeRules(new[]{LockColor.Red},new[]{DialSymbol.Circle,DialSymbol.Circle,DialSymbol.Circle},120,3,20,3,true);
            chair.StartRound();chair.Advance(20);Check(chair.HeartsA==3,"mistake mode has no periodic damage");
            for(int i=0;i<3;i++){chair.PressColor(EscapeRole.Blindfolded,1);if(i<2)chair.Advance(3);}
            Check(chair.HeartsA==0&&chair.HeartsB==0&&chair.Stage==EscapeStage.Failed,"three errors kill both");
            var copy=MultiEscapeRules.FromSnapshot(chair.Capture());Check(copy.Capture().mistakeDamage,"damage mode survives replication");
            return "PASS: "+count+" campaign checks; "+MultiEscapeChecks.Run();
        }
    }
}
