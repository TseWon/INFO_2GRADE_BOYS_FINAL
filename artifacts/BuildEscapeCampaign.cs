using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class BuildEscapeCampaign
{
    static Transform root;
    static Material concrete, metal, rust, yellow, lamp, cloth;
    static MultiEscapeCampaign campaign;
    static List<Light> lights;
    const string Folder = "Assets/MultiplayerCampaign";
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Stop Play Mode first.");
        var game = UnityEngine.Object.FindAnyObjectByType<MultiEscapeGame>();
        if (game == null || game.gameObject.scene.path != "Assets/Scene/MultiScene1.unity") throw new Exception("Open MultiScene1.");
        if (GameObject.Find("08_Campaign") != null) throw new Exception("Campaign already installed; edit existing objects.");
        Directory.CreateDirectory(Folder + "/Materials"); AssetDatabase.Refresh();
        concrete = Mat("AgedConcrete", new Color(.15f,.18f,.17f));
        metal = Mat("BlackSteel", new Color(.075f,.09f,.095f));
        rust = Mat("OxidizedSteel", new Color(.29f,.14f,.065f));
        yellow = Mat("WornSafetyPaint", new Color(.72f,.48f,.13f));
        lamp = Mat("WarmLight", new Color(.9f,.68f,.34f), true);
        cloth = Mat("KillerApron", new Color(.18f,.11f,.085f));
        root = new GameObject("08_Campaign").transform;
        root.SetParent(game.transform.root, false);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Add escape campaign");
        campaign = root.gameObject.AddComponent<MultiEscapeCampaign>(); campaign.game = game;
        var players = game.transform.root.GetComponentsInChildren<MultiEscapePlayer>();
        campaign.playerA = players.Single(p => p.Role == EscapeRole.Blindfolded);
        campaign.playerB = players.Single(p => p.Role == EscapeRole.Observer);
        campaign.hud = game.transform.root.GetComponentInChildren<UIDocument>();
        campaign.targets = new MultiEscapeCampaignTarget[13]; lights = new List<Light>();
        var settings = new SerializedObject(game);
        settings.FindProperty("damageOnMistake").boolValue = true;
        settings.FindProperty("timeLimit").floatValue = 120;
        settings.ApplyModifiedProperties();
        foreach (var p in players)
        {
            var walk = new SerializedObject(p.GetComponent<MultiEscapeWalk>());
            walk.FindProperty("speed").floatValue = 2.9f; walk.ApplyModifiedProperties();
        }
        var oldBack = GameObject.Find("MultiEscapePrototype/06_Exit/CorridorBack");
        if (oldBack != null) { Undo.RecordObject(oldBack, "Connect evidence room"); oldBack.SetActive(false); }
        BuildRoom(); BuildChase(); BuildKiller();
        campaign.alarmLights = lights.ToArray();
        EditorUtility.SetDirty(campaign);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        PrefabUtility.SaveAsPrefabAsset(game.transform.root.gameObject, "Assets/MultiplayerPrototype/Prefabs/FacilityEscapePrototype.prefab");
        AssetDatabase.SaveAssets();
        return "Saved campaign in MultiScene1 and existing prefab. 13 interactions, evidence room, pursuit route, dual switch gate, assisted wall and exit.";
    }
    static void BuildRoom()
    {
        var room = Group("02_EvidenceRoom", root, Vector3.zero);
        Box("Floor", room, new Vector3(0,-.15f,14.4f), new Vector3(10,.3f,11.4f), concrete);
        Box("Ceiling", room, new Vector3(0,3.7f,14.4f), new Vector3(10,.2f,11.4f), metal);
        Box("Wall_L",room,new Vector3(-5,1.8f,14.4f),new Vector3(.2f,3.6f,11.4f),concrete);
        Box("Wall_R",room,new Vector3(5,1.8f,14.4f),new Vector3(.2f,3.6f,11.4f),concrete);
        foreach(float z in new[]{8.9f,20f}) foreach(int side in new[]{-1,1})
            Box("DoorWall",room,new Vector3(side*3.2f,1.8f,z),new Vector3(3.6f,3.6f,.2f),concrete);
        campaign.searchDoor = Box("EvidenceExit",room,new Vector3(0,1.65f,20),new Vector3(2.8f,3.3f,.22f),metal).transform;
        Text("DoorOrder",room,"HEART > EYE > HAND\nBOTH PLAYERS MUST BE HERE",new Vector3(0,3.25f,19.82f),.045f,lamp.color);
        Text("ChapterTitle",room,"02 / EVIDENCE STORAGE",new Vector3(0,3.1f,9.15f),.055f,lamp.color).transform.rotation=Quaternion.Euler(0,180,0);
        Table(room,new Vector3(-3,0,12)); Table(room,new Vector3(3,0,15)); Table(room,new Vector3(0,0,17.8f));
        Vector3[] clues={new Vector3(-3,1.5f,11.6f),new Vector3(3,1.5f,14.6f),new Vector3(0,1.5f,17.4f)};
        for(int i=0;i<3;i++)
        {
            var target=Target(i,room,clues[i],new Vector3(.7f,.7f,.08f),"증거 기록: "+new[]{"심장","눈","손"}[i],yellow);
            target.display=Text("EvidenceText",target.transform,"EVIDENCE",clues[i]+Vector3.back*.06f,.07f,Color.white);
            Text("EvidenceCaption",room,new[]{"01 / THE LAST HEARTBEAT","02 / SOMEONE IS WATCHING","03 / NEVER LET GO"}[i],clues[i]+new Vector3(0,.6f,-.08f),.028f,lamp.color);
            LightAt(room,clues[i]+new Vector3(0,1,-.5f),new Color(1,.8f,.5f),1.3f,4);
        }
        for(int i=0;i<3;i++)
        {
            var pos=new Vector3(1.85f+i*.42f,1.6f,19.7f);
            var target=Target(3+i,room,pos,new Vector3(.35f,.5f,.2f),"암호 "+(i+1)+"번째 숫자 변경",metal);
            target.display=Text("Digit",target.transform,"0",pos+Vector3.back*.12f,.075f,Color.white);
        }
        Target(6,room,new Vector3(2.25f,.95f,19.7f),new Vector3(1.1f,.35f,.18f),"암호 확인 / 함께 출발",yellow);
        Text("Submit",room,"ENTER",new Vector3(2.25f,.95f,19.58f),.045f,Color.white);
        for(int i=0;i<4;i++)
        {
            var p=new Vector3(-4.65f,0,14+i*1.2f);
            Box("StorageCabinet",room,p+Vector3.up*1.1f,new Vector3(.5f,2.2f,.9f),metal);
            for(int j=0;j<3;j++) Box("Drawer",room,p+new Vector3(.26f,.45f+j*.65f,0),new Vector3(.025f,.52f,.78f),rust);
        }
        for(int i=0;i<7;i++) Box("AirVentSlat",room,new Vector3(4.84f,2.6f+i*.08f,12),new Vector3(.12f,.035f,1.2f),metal);
        Text("Warning",room,"DO NOT TRUST THE SILENCE",new Vector3(-2.9f,2.8f,19.8f),.04f,lamp.color);
        LightAt(room,new Vector3(0,3.2f,12),new Color(.65f,.8f,.73f),2,8);
        LightAt(room,new Vector3(0,3.2f,18),new Color(.9f,.65f,.35f),1.5f,8);
        Details(room,9,20);
    }
    static void BuildChase()
    {
        var hall=Group("03_Pursuit",root,Vector3.zero);
        Box("Floor",hall,new Vector3(0,-.15f,42.5f),new Vector3(10,.3f,45),concrete);
        Box("Ceiling",hall,new Vector3(0,3.7f,42.5f),new Vector3(10,.2f,45),metal);
        foreach(int side in new[]{-1,1}) Box("OuterWall",hall,new Vector3(side*5,1.8f,42.5f),new Vector3(.2f,3.6f,45),concrete);
        Box("EndWall",hall,new Vector3(0,1.8f,65),new Vector3(10,3.6f,.2f),metal);
        Box("TurnWall_1",hall,new Vector3(-1.3f,1.5f,27),new Vector3(7.4f,3,.4f),rust);
        Box("TurnWall_2",hall,new Vector3(1.3f,1.5f,34),new Vector3(7.4f,3,.4f),metal);
        Box("TurnWall_3",hall,new Vector3(-1.3f,1.5f,39),new Vector3(7.4f,3,.4f),rust);
        campaign.teamGate=Box("DualSwitchGate",hall,new Vector3(0,1.6f,43),new Vector3(10,3.2f,.3f),metal).transform;
        for(int i=0;i<2;i++)
        {
            var pos=new Vector3(i==0?-1.25f:1.25f,1.45f,42.5f);
            Target(7+i,hall,pos,new Vector3(.45f,.6f,.3f),(i==0?"A":"B")+" 전용 스위치 / 둘 다 2초 유지",yellow,true);
            Text("RoleSwitch",hall,i==0?"A / HOLD E":"B / HOLD E",pos+new Vector3(0,.65f,-.1f),.045f,Color.white);
        }
        Text("GateInstructions",hall,"TWO HANDS / TWO SECONDS",new Vector3(0,3.1f,42.8f),.055f,lamp.color);
        Box("BoostWall",hall,new Vector3(0,1.3f,52),new Vector3(10,2.6f,.5f),concrete);
        Target(9,hall,new Vector3(-1.1f,.35f,50.7f),new Vector3(.9f,.7f,.8f),"동료 발판 받치기",yellow,true);
        Target(10,hall,new Vector3(.4f,1.5f,51.65f),new Vector3(.85f,1.1f,.15f),"동료의 도움 / 밧줄로 담장 넘기",rust);
        Target(11,hall,new Vector3(.4f,1.4f,52.75f),new Vector3(.8f,.5f,.2f),"뒤에 남은 동료에게 밧줄 내리기",yellow);
        Text("WallInstructions",hall,"HELP YOUR FRIEND OVER\nTHEN LOWER THE ROPE",new Vector3(0,2.9f,51.7f),.05f,lamp.color);
        Text("RopeLabel",hall,"LOWER ROPE / E",new Vector3(.4f,2,52.85f),.045f,Color.white).transform.rotation=Quaternion.Euler(0,180,0);
        campaign.landingA=Group("LandingA",hall,new Vector3(-.6f,.05f,54));
        campaign.landingB=Group("LandingB",hall,new Vector3(.6f,.05f,54));
        Target(12,hall,new Vector3(0,1.5f,63.5f),new Vector3(2.6f,3,.25f),"집 밖으로 탈출 / 두 사람 모두 E",yellow);
        Text("FinalExit",hall,"EXIT / BOTH SURVIVORS",new Vector3(0,3.25f,63.35f),.065f,lamp.color);
        LightAt(hall,new Vector3(0,2.9f,62),new Color(.45f,1,.73f),3,8);
        Vector3[] route={new Vector3(0,0,9),new Vector3(0,0,22),new Vector3(3.7f,0,24),new Vector3(3.7f,0,29),
            new Vector3(-3.7f,0,31.5f),new Vector3(-3.7f,0,36),new Vector3(3.7f,0,37.5f),new Vector3(3.7f,0,41),
            new Vector3(0,0,45),new Vector3(0,0,50),new Vector3(0,2.9f,52),new Vector3(0,0,54),new Vector3(0,0,64)};
        var routes=Group("PursuitWaypoints",hall,Vector3.zero);
        campaign.pursuitRoute=route.Select((p,i)=>Group("Waypoint_"+i,routes,p)).ToArray();
        for(int i=2;i<route.Length;i++)
        {
            Vector3 from=route[i-1],to=route[i]; float length=Vector3.Distance(from,to);
            var stripe=Box("EscapeRouteStripe",hall,(from+to)*.5f+Vector3.up*.012f,new Vector3(.075f,.015f,length),yellow,false);
            stripe.transform.rotation=Quaternion.LookRotation(to-from);
        }
        for(int z=23;z<62;z+=5)
        {
            lights.Add(LightAt(hall,new Vector3(0,3.2f,z),new Color(1,.19f,.07f),1,8));
            for(int j=-4;j<=4;j++) Box("MetalGrate",hall,new Vector3(j,.02f,z),new Vector3(.035f,.035f,2),metal,false);
        }
        Details(hall,21,64);
        // Recessed dead ends and clutter leave the marked route clear.
        foreach(var p in new[]{new Vector3(-3.4f,0,24),new Vector3(3.5f,0,32),new Vector3(-3.5f,0,47)})
        {
            var crate=Box("AbandonedCrate",hall,p+Vector3.up*.55f,new Vector3(1.2f,1.1f,1.2f),rust);
            crate.transform.rotation=Quaternion.Euler(0,17,0);
            Box("CrateBand",hall,p+Vector3.up*1.12f,new Vector3(.12f,.03f,1.3f),metal,false);
        }
    }
    static void BuildKiller()
    {
        var k=Group("Killer_Chainsaw",root,new Vector3(0,0,9)); campaign.killer=k;
        Primitive("Torso",k,PrimitiveType.Capsule,new Vector3(0,1.35f,0),new Vector3(.68f,.55f,.42f),cloth);
        Primitive("Mask",k,PrimitiveType.Sphere,new Vector3(0,2.08f,0),new Vector3(.4f,.48f,.38f),concrete);
        for(int side=-1;side<=1;side+=2)
        {
            Primitive("Boot",k,PrimitiveType.Cube,new Vector3(side*.21f,.44f,.04f),new Vector3(.25f,.85f,.35f),metal);
            Primitive("Arm",k,PrimitiveType.Capsule,new Vector3(side*.46f,1.35f,.16f),new Vector3(.2f,.42f,.2f),cloth);
            Primitive("Eye",k,PrimitiveType.Cube,new Vector3(side*.085f,2.12f,.18f),new Vector3(.08f,.035f,.025f),metal);
        }
        Primitive("ChainsawEngine",k,PrimitiveType.Cube,new Vector3(.3f,1,.6f),new Vector3(.4f,.34f,.5f),yellow);
        Primitive("ChainsawBlade",k,PrimitiveType.Cube,new Vector3(.3f,1,1.1f),new Vector3(.09f,.18f,.65f),metal);
        for(int i=0;i<9;i++) Primitive("SawTooth",k,PrimitiveType.Cube,new Vector3(.3f,1.11f,.8f+i*.07f),new Vector3(.12f,.06f,.035f),rust);
        k.gameObject.SetActive(false);
    }
    static void Details(Transform parent,int start,int end)
    {
        for(int z=start;z<end;z+=3)
        {
            Box("CeilingPipe",parent,new Vector3(-4.5f,3.3f,z+1.5f),new Vector3(.13f,.13f,3),rust,false);
            Box("PipeBracket",parent,new Vector3(-4.5f,3.3f,z),new Vector3(.3f,.3f,.07f),metal,false);
            Box("WallSeam",parent,new Vector3(4.88f,1.8f,z),new Vector3(.025f,3.4f,.045f),metal,false);
            Box("CeilingLamp",parent,new Vector3(0,3.51f,z),new Vector3(1.2f,.06f,.18f),lamp,false);
        }
    }
    static void Table(Transform parent,Vector3 p)
    {
        Box("EvidenceTable",parent,p+Vector3.up*.9f,new Vector3(1.7f,.12f,1),rust);
        for(int x=-1;x<=1;x+=2) for(int z=-1;z<=1;z+=2)
            Box("TableLeg",parent,p+new Vector3(x*.72f,.43f,z*.38f),new Vector3(.08f,.86f,.08f),metal);
    }
    static MultiEscapeCampaignTarget Target(int id,Transform parent,Vector3 p,Vector3 scale,string prompt,Material mat,bool hold=false)
    {
        var o=Box("Interact_"+id,parent,p,scale,mat);
        // Keep the interaction root unscaled so child labels retain a predictable size.
        var t=Group("Target_"+id,parent,p); o.transform.SetParent(t,true);
        var target=t.gameObject.AddComponent<MultiEscapeCampaignTarget>();target.action=id;target.prompt=prompt;target.hold=hold;
        campaign.targets[id]=target;return target;
    }
    static Transform Group(string name,Transform parent,Vector3 p)
    { var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=p;return t; }
    static GameObject Box(string n,Transform p,Vector3 at,Vector3 scale,Material m,bool collision=true)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(p,false);o.transform.position=at;o.transform.localScale=scale;
        o.GetComponent<Renderer>().sharedMaterial=m;if(!collision)UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());return o;
    }
    static void Primitive(string name,Transform parent,PrimitiveType type,Vector3 p,Vector3 scale,Material m)
    {
        var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=scale;
        o.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
    }
    static Material Mat(string name,Color color,bool emission=false)
    {
        string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.7f);}return m;
    }
    static TextMesh Text(string name,Transform parent,string value,Vector3 p,float size,Color color)
    {
        var t=Group(name,parent,p).gameObject.AddComponent<TextMesh>();t.text=value;t.characterSize=size;t.fontSize=60;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
        t.gameObject.AddComponent<MultiEscapeWorldText>();return t;
    }
    static Light LightAt(Transform parent,Vector3 p,Color color,float intensity,float range)
    {var l=Group("PracticalLight",parent,p).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;return l;}
}
