using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using HollowCreek;

public static class BuildHollowCreek
{
    const string Root = "Assets/HollowCreek";
    static Material steel, dark, paper, accent, red, flesh, snow;
    static PanelSettings panel;
    static Texture2D flag;
    static Texture2D[] tiles;
    static GameObject playerPrefab;
    static readonly List<Light> lights = new List<Light>();
    static Material Mat(string name, Color color, bool emission = false)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m, path); }
        m.color = color; m.SetFloat("_Glossiness", .2f);
        if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * .8f); }
        EditorUtility.SetDirty(m); return m;
    }
    static GameObject Obj(string name, Transform parent, Vector3 position)
    { var g = new GameObject(name); if (parent != null) g.transform.SetParent(parent, false); g.transform.position = position; return g; }
    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collide = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
        g.transform.SetParent(parent, false); g.transform.position = position; g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = material;
        if (!collide) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }
    static Transform Marker(string name, Transform parent, Vector3 position, float yaw = 0)
    { var t = Obj(name, parent, position).transform; t.rotation = Quaternion.Euler(0, yaw, 0); return t; }
    static Light Light(Transform parent, Vector3 position, Color color, float range = 13)
    {
        var l = Obj("Emergency practical", parent, position).AddComponent<Light>(); l.type = LightType.Point;
        l.color = color; l.intensity = 1.8f; l.range = range; l.shadows = LightShadows.None; lights.Add(l);
        Box("Light housing", parent, position, new Vector3(.7f, .12f, .25f), accent, false); return l;
    }
    static void Sign(Transform parent, string text, Vector3 position, float yaw = 0, float size = .15f)
    {
        var go = Obj(text, parent, position); go.transform.rotation = Quaternion.Euler(0, yaw + 180, 0);
        var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.fontSize = 48; tm.characterSize = size * .45f;
        tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(.65f, .9f, .84f);
    }
    static HollowCreekTarget Target(HollowCreekCampaign c, Transform parent, Interaction kind, string label, Vector3 position, Vector3 size, Route route = Route.Undecided, Material material = null)
    {
        var g = Box(kind + " · " + label, parent, position, size, material ?? steel);
        var target = g.AddComponent<HollowCreekTarget>(); target.campaign = c; target.interaction = kind; target.label = label; target.route = route;
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere); dot.name = "Interactable indicator"; UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());
        dot.GetComponent<Renderer>().sharedMaterial = accent; dot.transform.SetParent(g.transform, false);
        dot.transform.position = position + Vector3.up * (size.y / 2 + .17f);
        // Marker parent scale must be unit-sized to avoid elongated indicators.
        var anchor = Obj("Indicator anchor", parent, dot.transform.position);
        dot.transform.SetParent(anchor.transform, true); dot.transform.localScale = Vector3.one * .07f; target.marker = dot.transform;
        return target;
    }
    static Transform Soldier(Transform parent, string name, Vector3 position, float yaw = 0)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/FittedSoldier/FittedSoldierVisual.prefab");
        var g = (GameObject)PrefabUtility.InstantiatePrefab(source); g.name = name; g.transform.SetParent(parent, false); g.transform.position = position; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return g.transform;
    }
    static Transform Monster(Transform parent)
    {
        var g = Obj("N · Project Null", parent, Vector3.zero);
        Box("Distorted torso", g.transform, new Vector3(0, 1.6f, 0), new Vector3(.65f, 1.4f, .4f), dark, false);
        Box("Head", g.transform, new Vector3(.12f, 2.48f, .08f), new Vector3(.38f, .48f, .4f), flesh, false);
        for (int s = -1; s <= 1; s += 2) {
            var arm = Box("Long arm", g.transform, new Vector3(s * .6f, 1.28f, .18f), new Vector3(.17f, 1.75f, .2f), flesh, false); arm.transform.rotation = Quaternion.Euler(12, 0, s * 15);
            Box("Leg", g.transform, new Vector3(s * .2f, .5f, 0), new Vector3(.24f, 1, .28f), dark, false);
            Box("Eye", g.transform, new Vector3(.12f + s * .09f, 2.52f, .29f), new Vector3(.055f, .035f, .025f), red, false);
        }
        g.SetActive(false); return g.transform;
    }
    static HollowCreekCampaign Setup(Scene scene, int number, SnowFirstPerson existing = null)
    {
        lights.Clear(); SceneManager.SetActiveScene(scene);
        var go = Obj("Hollow Creek Campaign", null, Vector3.zero);
        var c = go.AddComponent<HollowCreekCampaign>(); c.sceneNumber = number;
        if (existing != null) c.player = existing;
        else {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene); g.name = "Solo Player"; c.player = g.GetComponent<SnowFirstPerson>();
            foreach (var cam in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Where(cam => !cam.transform.IsChildOf(g.transform))) cam.gameObject.SetActive(false);
        }
        c.player.snowfall = number == 1 ? c.player.snowfall : null;
        var ui = Obj("Hollow Creek HUD", go.transform, Vector3.zero);
        var doc = ui.AddComponent<UIDocument>(); doc.panelSettings = panel; doc.sortingOrder = 40;
        var hud = ui.AddComponent<HollowCreekHud>(); hud.stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts(Solo)/UI/HollowCreek.uss");
        hud.flagPreview = flag; hud.flagTiles = tiles; hud.campaign = c; c.hud = hud;
        var audio = go.AddComponent<HollowCreekAtmosphere>(); audio.campaign = c;
        c.monster = Monster(go.transform);
        if (number != 1) {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.12f, .15f, .17f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = new Color(.025f, .04f, .05f); RenderSettings.fogDensity = .025f;
            RenderSettings.skybox = null;
            c.player.viewCamera.clearFlags = CameraClearFlags.SolidColor; c.player.viewCamera.backgroundColor = new Color(.02f, .03f, .04f);
            foreach (var l in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>())) if (l.type == LightType.Directional) l.intensity = .18f;
            var lamp = Obj("Player flashlight", c.player.viewCamera.transform, c.player.viewCamera.transform.position).AddComponent<Light>();
            lamp.transform.localPosition = new Vector3(.18f, -.12f, .1f); lamp.transform.localRotation = Quaternion.identity;
            lamp.type = LightType.Spot; lamp.range = 24; lamp.spotAngle = 68; lamp.intensity = 3; lamp.shadows = LightShadows.Soft;
            lamp.color = new Color(.83f, .89f, .82f);
        }
        return c;
    }
    static void Finish(Scene scene, HollowCreekCampaign c, string path)
    {
        c.GetComponent<HollowCreekAtmosphere>().practicalLights = lights.ToArray();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception("Save failed: " + path);
    }
    static void Collisions(GameObject root)
    {
        foreach (var f in root.GetComponentsInChildren<MeshFilter>(true)) {
            if (f.sharedMesh == null || f.GetComponent<Collider>() != null) continue;
            var collider = f.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = f.sharedMesh;
        }
    }
    static void Interior(HollowCreekCampaign c)
    {
        var scene = c.gameObject.scene;
        foreach (var root in scene.GetRootGameObjects().Where(g => g.name.StartsWith("Underground_"))) Collisions(root);
        Box("B1 ceiling", c.transform, new Vector3(0, 4.05f, -13), new Vector3(36, .22f, 72), dark);
        Box("B2 ceiling", c.transform, new Vector3(86, 4.05f, 0), new Vector3(84, .22f, 21), dark);
        foreach (float z in new[] { 6f, -3f, -13f, -23f, -33f }) Light(c.transform, new Vector3(0, 3.1f, z), new Color(.66f, .83f, .8f));
        foreach (float x in new[] { 125f, 115f, 105f, 95f, 85f, 75f, 65f }) Light(c.transform, new Vector3(x, 3.1f, 0), new Color(.7f, .72f, .6f));
    }
    static void Surface(Scene scene)
    {
        var oldPlayer = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SnowFirstPerson>()).Single();
        var clone = UnityEngine.Object.Instantiate(oldPlayer.gameObject); clone.GetComponent<SnowFirstPerson>().snowfall = null;
        playerPrefab = PrefabUtility.SaveAsPrefabAsset(clone, Root + "/SoloPlayer.prefab"); UnityEngine.Object.DestroyImmediate(clone);
        var c = Setup(scene, 1, oldPlayer);
        c.start = Marker("Truck arrival", c.transform, new Vector3(8, .08f, -92), -20);
        oldPlayer.transform.SetPositionAndRotation(c.start.position, c.start.rotation);
        var team = new List<Transform>();
        team.Add(Soldier(c.transform, "Miller · squad leader", new Vector3(6, .08f, -87), 165));
        team.Add(Soldier(c.transform, "Reed", new Vector3(3, .08f, -86), 180));
        team.Add(Soldier(c.transform, "Cole", new Vector3(2, .08f, -88), 120));
        team.Add(Soldier(c.transform, "Hayes", new Vector3(6, .08f, -90), 30)); c.teammates = team.ToArray();
        Target(c, c.transform, Interaction.Profile, "밀러에게 연구자 프로필 받기", new Vector3(6, 1.2f, -87.55f), new Vector3(.4f, .3f, .06f), material: paper);
        Target(c, c.transform, Interaction.Entrance, "연구소 출입 인증", new Vector3(1.5f, 1.8f, -55.3f), new Vector3(.55f, .8f, .4f));
        Sign(c.transform, "HOLLOW CREEK\nACCESS CONTROL", new Vector3(1.5f, 2.6f, -55.55f), 180, .09f);
        Light(c.transform, new Vector3(1.2f, 3, -56), new Color(.7f, .84f, .72f), 8);
        Finish(scene, c, "Assets/Scene/SoloScene1.unity");
    }
    static void ChapterOne(Scene scene)
    {
        var c = Setup(scene, 2); Interior(c);
        c.start = Marker("Lift start", c.transform, new Vector3(0, .12f, 6.7f), 180);
        c.upperSpawn = Marker("Upper rescue landing", c.transform, new Vector3(0, .12f, 5.8f), 180);
        c.prisonSpawn = Marker("Prison wreck landing", c.transform, new Vector3(125, .12f, 0), 270);
        c.lift = Box("Movable lift platform", c.transform, new Vector3(0, -.03f, 6), new Vector3(3.1f, .12f, 3), steel).transform;
        c.cable = Box("Severed lift cable", c.transform, new Vector3(1.1f, 2.3f, 6), new Vector3(.06f, 4.6f, .06f), dark, false).transform;
        Target(c, c.transform, Interaction.Lift, "승강기 조작반 작동", new Vector3(.9f, 1.1f, 5.3f), new Vector3(.65f, .6f, .35f));
        c.upperProps = Obj("CASE 1 objects", c.transform, Vector3.zero);
        c.prisonProps = Obj("CASE 2 objects", c.transform, Vector3.zero);
        // One barrier for each branch; the unused branch is hidden with its props.
        c.passageBarrier = Box("Upper locked partition", c.upperProps.transform, new Vector3(0, 1.4f, 2.5f), new Vector3(3.2f, 2.8f, .18f), steel);
        Target(c, c.upperProps.transform, Interaction.Acrostic, "벽면 서약 읽고 잠금 해제", new Vector3(1.2f, 1.45f, 3), new Vector3(.6f, .75f, .14f), Route.Upper, paper);
        Target(c, c.upperProps.transform, Interaction.NextArea, "연구실로 이동", new Vector3(0, 1.25f, -3), new Vector3(.6f, 1, .4f), Route.Upper);
        c.prisonBarrier = Target(c, c.prisonProps.transform, Interaction.WeakWall, "균열 난 벽 밀기", new Vector3(121.7f, 1.35f, 0), new Vector3(.2f, 2.7f, 3.4f), Route.Prison).gameObject;
        Target(c, c.prisonProps.transform, Interaction.Wounded, "부상당한 동료 확인", new Vector3(124, .6f, 1.3f), new Vector3(.4f, .6f, .6f), Route.Prison, paper);
        Target(c, c.prisonProps.transform, Interaction.NextArea, "감옥 밖 연구 통로로 이동", new Vector3(118, 1.25f, 0), new Vector3(.5f, 1, .7f), Route.Prison);
        c.teammates = new[] { Soldier(c.transform, "Miller", new Vector3(-1.4f, .1f, 5.5f)), Soldier(c.transform, "Reed", new Vector3(1.4f, .1f, 6.8f)), Soldier(c.transform, "Hayes", new Vector3(-1.3f, .1f, 7.5f)), Soldier(c.transform, "Cole", new Vector3(1.3f, .1f, 7.8f)) };
        Sign(c.transform, "B1 / RESEARCH\nB2 / CONTAINMENT", new Vector3(-1.5f, 2.3f, 3.1f), 0, .1f);
        Finish(scene, c, "Assets/Scene/SoloScene2.unity");
    }
    static void Desk(Transform parent, Vector3 p)
    {
        Box("Research desk", parent, p + Vector3.up * .8f, new Vector3(1.9f, .12f, .9f), steel);
        for (int s = -1; s <= 1; s += 2) Box("Desk foot", parent, p + new Vector3(s * .75f, .4f, 0), new Vector3(.08f, .8f, .65f), dark);
    }
    static void Research(Scene scene)
    {
        var c = Setup(scene, 3); Interior(c);
        c.upperSpawn = Marker("B1 research entry", c.transform, new Vector3(0, .1f, -9), 180);
        c.prisonSpawn = Marker("B2 prison exit", c.transform, new Vector3(106, .1f, 0), 270);
        c.start = c.upperSpawn;
        c.upperProps = Obj("CASE 1 search objects", c.transform, Vector3.zero); c.prisonProps = Obj("CASE 2 search objects", c.transform, Vector3.zero);
        var up = c.upperProps.transform; var low = c.prisonProps.transform;
        foreach (var p in new[] { new Vector3(-4, 0, -17), new Vector3(5, 0, -23), new Vector3(-4, 0, -29), new Vector3(3, 0, -33) }) Desk(up, p);
        var positions = new[] { new Vector3(-4, .95f, -17), new Vector3(5, .95f, -23), new Vector3(-4, .95f, -29), new Vector3(3, .95f, -33) };
        var lower = new[] { new Vector3(104, 1.1f, 2), new Vector3(98, 1.1f, -1.5f), new Vector3(84, 1.1f, 2), new Vector3(68, 1.1f, -2) };
        for (int i = 0; i < 4; i++) {
            Target(c, up, Interaction.Note1 + i, "연구일지 " + (i + 1), positions[i], new Vector3(.55f, .1f, .4f), Route.Upper, paper);
            Desk(low, lower[i] - Vector3.up * 1.1f);
            Target(c, low, Interaction.Note1 + i, "연구일지 " + (i + 1), lower[i], new Vector3(.55f, .1f, .4f), Route.Prison, paper);
        }
        Target(c, up, Interaction.Digits, "디지털 표시기 잠금", new Vector3(0, 1.3f, -24), new Vector3(.9f, 1.3f, .5f), Route.Upper);
        Sign(up, "8 > 7 > 4 > 4 > ?\nCOUNT THE SEGMENTS", new Vector3(0, 2.4f, -23.7f), 0, .11f);
        Target(c, up, Interaction.Cabinet, "벽장 열기", new Vector3(-7, 1.2f, -20), new Vector3(1.2f, 2.4f, .8f), Route.Upper);
        Target(c, low, Interaction.Sliding, "성조기 슬라이딩 잠금", new Vector3(94, 1.2f, 1.3f), new Vector3(.8f, 1.2f, .6f), Route.Prison);
        c.labLeaves = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>()).Where(t => t.name.Contains("viewing wall") && (t.name.Contains("door leaf") || t.name.Contains("door handle"))).Select(t => t.gameObject).ToArray();
        ResearchPassage(c);
        // Existing leaf meshes remain in place; only their active state follows the puzzle lock.
        foreach (var p in new[] { new Vector3(0, 0, -35), new Vector3(62, 0, 0) }) {
            var parent = p.x < 30 ? up : low; var route = p.x < 30 ? Route.Upper : Route.Prison;
            Desk(parent, p);
            Box("Laptop screen", parent, p + new Vector3(-.3f, 1.15f, 0), new Vector3(.7f, .5f, .08f), dark);
            Target(c, parent, Interaction.Radio, "비상 무전 채널 입력", p + new Vector3(.5f, 1.1f, 0), new Vector3(.35f, .5f, .3f), route);
        }
        c.teammates = new[] { Soldier(up, "Miller · note guide", new Vector3(-2, .1f, -16)), Soldier(up, "Reed · terminal guide", new Vector3(2, .1f, -23)) };
        Target(c, up, Interaction.Guide, "리드에게 단서 묻기", new Vector3(2, 1.3f, -22.65f), new Vector3(.4f, .35f, .2f), Route.Upper);
        Sign(up, "PROJECT NULL\nRESEARCH ARCHIVE", new Vector3(-17.6f, 2.9f, -23), 90, .16f);
        Sign(low, "N / CONTAINMENT\nNO UNAUTHORIZED ENTRY", new Vector3(92.5f, 2.8f, 0), 90, .12f);
        Finish(scene, c, "Assets/Scene/SoloScene3.unity");
    }
    static void Finale(Scene scene)
    {
        var c = Setup(scene, 4); var t = c.transform;
        c.start = Marker("Pursuit start", t, new Vector3(0, .1f, 0));
        Box("Main corridor floor", t, new Vector3(0, -.15f, 25), new Vector3(6, .3f, 76), steel);
        Box("Main corridor ceiling", t, new Vector3(0, 3.7f, 25), new Vector3(6, .25f, 76), dark);
        foreach (int side in new[] { -1, 1 }) Box("Corridor wall", t, new Vector3(side * 3, 1.75f, 25), new Vector3(.25f, 3.5f, 76), steel);
        for (int z = 6; z < 60; z += 10) {
            Light(t, new Vector3(0, 3.3f, z), new Color(.95f, .4f, .22f), 12);
            Sign(t, "EXIT  ^", new Vector3(0, 2.8f, z + 2), 180, .13f);
            // Low cross-corridor obstacles require the existing player's new Space jump.
            Box("Fallen service pipe", t, new Vector3(0, .32f, z + 5), new Vector3(5.6f, .64f, .55f), dark);
        }
        Box("Safe room floor", t, new Vector3(0, -.15f, 66), new Vector3(8, .3f, 12), steel);
        Box("Safe room ceiling", t, new Vector3(0, 3.7f, 66), new Vector3(8, .2f, 12), dark);
        foreach (int side in new[] { -1, 1 }) Box("Safe room side", t, new Vector3(side * 4, 1.7f, 66), new Vector3(.2f, 3.4f, 12), steel);
        Box("Safe room end", t, new Vector3(0, 1.7f, 72), new Vector3(8, 3.4f, .2f), steel);
        Light(t, new Vector3(0, 3.2f, 67), new Color(.6f, .8f, .73f), 12);
        c.sealDoor = Box("Sealed fire door", t, new Vector3(0, 1.7f, 61), new Vector3(6, 3.4f, .25f), dark); c.sealDoor.SetActive(false);
        Target(c, t, Interaction.SealDoor, "방화문 잠그기", new Vector3(1.7f, 1.3f, 64), new Vector3(.6f, .7f, .35f));
        c.collapsedExit = Box("Collapsed emergency exit", t, new Vector3(0, 1.1f, 71), new Vector3(3, 2.2f, 1.3f), dark); c.collapsedExit.SetActive(false);
        Target(c, t, Interaction.Vent, "환풍구 안으로 피신", new Vector3(-3.7f, 1, 68), new Vector3(.35f, 1.4f, 1.5f));
        Sign(t, "VENTILATION\nSERVICE ACCESS", new Vector3(-3.65f, 2.2f, 68), 90, .09f);
        c.ventSpawn = Marker("Vent checkpoint", t, new Vector3(12, .1f, 65));
        // Two connected low ducts. Outer corner stays open for a continuous turn.
        Box("Duct floor A", t, new Vector3(12, -.1f, 79), new Vector3(2.2f, .2f, 44), steel);
        Box("Duct roof A", t, new Vector3(12, 1.45f, 79), new Vector3(2.2f, .15f, 44), dark);
        Box("Duct west", t, new Vector3(10.9f, .7f, 79), new Vector3(.12f, 1.5f, 44), steel);
        Box("Duct east", t, new Vector3(13.1f, .7f, 78), new Vector3(.12f, 1.5f, 42), steel);
        Box("Duct floor B", t, new Vector3(25, -.1f, 100), new Vector3(28, .2f, 2.2f), steel);
        Box("Duct roof B", t, new Vector3(25, 1.45f, 100), new Vector3(28, .15f, 2.2f), dark);
        Box("Duct north", t, new Vector3(25, .7f, 101.1f), new Vector3(28, 1.5f, .12f), steel);
        Box("Duct south", t, new Vector3(26, .7f, 98.9f), new Vector3(26, 1.5f, .12f), steel);
        for (int z = 67; z <= 97; z += 10) Light(t, new Vector3(12, 1.2f, z), new Color(.52f, .7f, .64f), 8);
        Light(t, new Vector3(29, 1.2f, 100), new Color(.5f, .7f, .8f), 12);
        Target(c, t, Interaction.Exit, "외부 출구 열기", new Vector3(38, .65f, 100), new Vector3(.2f, 1.25f, 2));
        c.chaseRoute = new[] { Marker("Chase 0", t, new Vector3(0, .1f, -11)), Marker("Chase 1", t, new Vector3(0, .1f, 64)) };
        c.ventRoute = new[] { Marker("Vent 0", t, new Vector3(12, .1f, 57)), Marker("Vent 1", t, new Vector3(12, .1f, 100)), Marker("Vent 2", t, new Vector3(38, .1f, 100)) };
        Box("Snow outside", t, new Vector3(60, -.3f, 100), new Vector3(50, .5f, 55), snow);
        c.endingSpawn = Marker("Survivor overlook", t, new Vector3(41, .1f, 100), 90);
        var station = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/SurfaceResearchStation/Prefabs/SurfaceResearchStation.prefab"));
        station.name = "Collapsing Hollow Creek facility"; station.transform.SetParent(t, false); station.transform.position = new Vector3(62, 0, 100); station.transform.rotation = Quaternion.Euler(0, 90, 0);
        c.collapsePieces = new[] { station.transform };
        Light(t, new Vector3(44, 7, 100), new Color(.65f, .76f, .9f), 50);
        c.teammates = new[] { Soldier(t, "Miller", new Vector3(-1.5f, .1f, 3)), Soldier(t, "Reed", new Vector3(1.5f, .1f, 3)), Soldier(t, "Hayes", new Vector3(-1.6f, .1f, 5)), Soldier(t, "Cole", new Vector3(1.6f, .1f, 5)) };
        Finish(scene, c, "Assets/Scene/SoloScene4.unity");
    }
    static void ResearchPassage(HollowCreekCampaign c)
    {
        if (c.transform.Find("Observation service doorway") != null) return;
        var wall = c.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>()).Single(r=>r.name=="B2 experiment room | E closed box wall");
        var b = wall.bounds; var material = wall.sharedMaterial;
        var root = Obj("Observation service doorway", c.transform, Vector3.zero);
        // Keep the imported wall as a disabled scene override, with an opening in matching panels.
        wall.gameObject.SetActive(false);
        float halfOpening = 1.5f;
        foreach (int side in new[]{-1,1}) {
            float length = b.extents.z-halfOpening;
            Box("Original wall side panel", root.transform, new Vector3(b.center.x,b.center.y,b.center.z+side*(halfOpening+length/2)),new Vector3(b.size.x,b.size.y,length),material);
        }
        Box("Door lintel",root.transform,new Vector3(b.center.x,3.2f,0),new Vector3(b.size.x,1,3),material);
        var door = Box("Puzzle-linked service shutter",root.transform,new Vector3(b.center.x,1.35f,0),new Vector3(b.size.x,2.7f,3),material);
        c.labLeaves=c.labLeaves.Concat(new[]{door}).ToArray();
    }
    public static string RepairExisting()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode first");
        for(int i=1;i<=4;i++) {
            var scene=EditorSceneManager.OpenScene("Assets/Scene/SoloScene"+i+".unity",OpenSceneMode.Single);
            var c=UnityEngine.Object.FindAnyObjectByType<HollowCreekCampaign>();
            foreach(var tm in c.GetComponentsInChildren<TextMesh>(true)) {tm.transform.Rotate(0,180,0);tm.characterSize*=.45f;}
            if(i==3)ResearchPassage(c);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.OpenScene("Assets/Scene/SoloScene1.unity");return "Corrected sign facing and added puzzle-linked observation doorway";
    }
    public static string FixArchiveSign()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scene/SoloScene3.unity",OpenSceneMode.Single);
        var sign=GameObject.Find("PROJECT NULL\nRESEARCH ARCHIVE");
        sign.transform.SetPositionAndRotation(new Vector3(-17.6f,2.9f,-23),Quaternion.Euler(0,270,0));
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene("Assets/Scene/SoloScene1.unity",OpenSceneMode.Single);return "Moved archive wall label away from puzzle sign";
    }
    static void Flag()
    {
        const int size = 384; var pixels = new Color[size * size];
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            int top = size - 1 - y;
            bool canton = x < 168 && top < 207;
            Color color = top * 13 / size % 2 == 0 ? new Color(.65f, .08f, .12f) : new Color(.84f, .82f, .7f);
            if (canton) {
                color = new Color(.08f, .16f, .3f);
                // 48-star historical flag: six rows of eight stars for the 1953 setting.
                float dx = (x % 21) - 10, dy = (top % 34) - 17;
                if (Mathf.Abs(dx) + Mathf.Abs(dy) < 5 || Mathf.Abs(dx) < 1.2f && Mathf.Abs(dy) < 7) color = new Color(.9f, .88f, .75f);
            }
            float age = .94f + .06f * Mathf.Sin(x * .21f + y * .63f); pixels[y * size + x] = color * age;
        }
        flag = Texture("FlagReference", pixels, size, size); tiles = new Texture2D[8];
        for (int index = 0; index < 8; index++) {
            var tile = new Color[128 * 128]; int col = index % 3, row = 2 - index / 3;
            for (int y = 0; y < 128; y++) Array.Copy(pixels, (row * 128 + y) * size + col * 128, tile, y * 128, 128);
            tiles[index] = Texture("FlagTile" + (index + 1), tile, 128, 128);
        }
    }
    static Texture2D Texture(string name, Color[] pixels, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.SetPixels(pixels); tex.Apply();
        string path = Root + "/Textures/" + name + ".png"; File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static string Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Requires stopped, compiled editor.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: " + SceneManager.GetSceneAt(i).path);
        if (File.Exists(Root + "/installed.txt")) throw new Exception("Campaign already installed; inspect and edit instead of rebuilding.");
        foreach (var d in new[] { Root, Root + "/Materials", Root + "/Textures", "artifacts/backups" }) Directory.CreateDirectory(d);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        foreach (int i in new[] { 1, 2 }) File.Copy("Assets/Scene/SoloScene" + i + ".unity", "artifacts/backups/SoloScene" + i + "_before_campaign_" + stamp + ".unity");
        steel = Mat("Worn steel", new Color(.18f, .25f, .26f)); dark = Mat("Carbon", new Color(.055f, .065f, .07f));
        paper = Mat("Aged paper", new Color(.72f, .67f, .49f)); accent = Mat("Interaction cyan", new Color(.2f, .65f, .53f), true);
        red = Mat("Warning red", new Color(.65f, .1f, .04f), true); flesh = Mat("N pale tissue", new Color(.5f, .55f, .43f)); snow = Mat("Snow", new Color(.74f, .81f, .85f));
        Flag();
        panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.name = "HollowCreekPanel"; panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1280, 720); panel.match = .5f;
        var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/MultiplayerPrototype/EscapePanelSettings.asset"); panel.themeStyleSheet = existing.themeStyleSheet;
        AssetDatabase.CreateAsset(panel, Root + "/HollowCreekPanel.asset");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene("Assets/Scene/SoloScene1.unity", OpenSceneMode.Single); Surface(scene);
        scene = EditorSceneManager.OpenScene("Assets/Scene/SoloScene2.unity", OpenSceneMode.Single);
        // Preserve a geometry-only copy for chapter 2 before adding chapter 1 systems.
        EditorSceneManager.SaveScene(scene, "Assets/Scene/SoloScene3.unity", true);
        ChapterOne(scene);
        scene = EditorSceneManager.OpenScene("Assets/Scene/SoloScene3.unity", OpenSceneMode.Single); Research(scene);
        scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); Finale(scene);
        var build = new List<EditorBuildSettingsScene>();
        for (int i = 1; i <= 4; i++) build.Add(new EditorBuildSettingsScene("Assets/Scene/SoloScene" + i + ".unity", true));
        EditorBuildSettings.scenes = build.ToArray();
        AssetDatabase.SaveAssets(); File.WriteAllText(Root + "/installed.txt", "Hollow Creek campaign installed " + stamp);
        EditorSceneManager.OpenScene("Assets/Scene/SoloScene1.unity", OpenSceneMode.Single);
        return "Installed four solo scenes. Existing surface movement/gate and underground B1/B2 geometry preserved. Backups: " + stamp;
    }
}
