using System;
using FacilityEscape;
using UnityEngine;

public static class VerifyEscapeMovement
{
    public static string Run()
    {
        var c=UnityEngine.Object.FindAnyObjectByType<MultiEscapeCampaign>();
        if(!Application.isPlaying||c==null)throw new Exception("Play Mode required");
        var walk=c.playerA.GetComponent<MultiEscapeWalk>();
        walk.TickStamina(10,true);
        if(!walk.Exhausted||walk.Sprinting||walk.Stamina!=0)throw new Exception("Sprint exhaustion");
        for(int i=0;i<20;i++){walk.TickStamina(.1f,true);if(walk.Sprinting)throw new Exception("Sprint stutters before recovery threshold");}
        if(walk.Stamina<.23f)throw new Exception("Recovery while holding sprint");
        for(int i=0;i<8;i++)walk.TickStamina(.1f,true);
        if(walk.Exhausted||!walk.Sprinting)throw new Exception("Sprint resumes after 30% recovery");
        var from=new Vector3(.4f,.05f,50.4f);var to=c.landingA.position;
        for(int i=0;i<=40;i++)
        {
            Vector3 p=MultiEscapeWalk.ClimbPosition(from,to,i/40f);
            var overlaps=Physics.OverlapCapsule(p+Vector3.up*.35f,p+Vector3.up*1.6f,.28f);
            foreach(var collider in overlaps)
                if(collider.GetComponentInParent<MultiEscapePlayer>()==null)
                    throw new Exception("Climb capsule blocked at "+i+" by "+collider.name);
        }
        if(Vector3.Distance(MultiEscapeWalk.ClimbPosition(from,to,1),to)>.001f)throw new Exception("Climb landing");
        return "PASS: exhaustion hysteresis, held-Shift recovery and 41 climb capsule clearance samples.";
    }
}
