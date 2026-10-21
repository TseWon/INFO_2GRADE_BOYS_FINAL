using System;
using System.Collections.Generic;
using System.IO;
using FacilityEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public static class BuildMultiEscapePrototype
{
    const string AssetRoot = "Assets/MultiplayerPrototype";
    static Material concrete, dark, steel, cushion, amber, white, red, blue, yellow, green;
    static MultiEscapeGame game;
    static readonly List<GameObject> wrists = new List<GameObject>();
    static readonly List<GameObject> restraints = new List<GameObject>();
    static readonly List<Light> shockLights = new List<Light>();

    public static string Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before building.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scene/MultiScene1.unity") throw new Exception("Open MultiScene1 first.");
        if (GameObject.Find("MultiEscapePrototype") != null) throw new Exception("Prototype already exists; edit it in place.");
        wrists.Clear(); restraints.Clear(); shockLights.Clear();
        Directory.CreateDirectory(AssetRoot + "/Materials");
        Directory.CreateDirectory(AssetRoot + "/Audio");
        Directory.CreateDirectory(AssetRoot + "/Prefabs");
        AssetDatabase.Refresh();
        concrete = Mat("Concrete", new Color(.22f, .25f, .26f));
        dark = Mat("DarkMetal", new Color(.055f, .07f, .075f));
        steel = Mat("BrushedSteel", new Color(.38f, .43f, .46f));
        cushion = Mat("ChairPadding", new Color(.055f, .085f, .085f));
        amber = Mat("SafetyYellow", new Color(.88f, .59f, .12f));
        white = Mat("LightStrip", new Color(.73f, .91f, .95f), true);
        red = Mat("ButtonRed", new Color(.85f, .055f, .04f), true);
        blue = Mat("ButtonBlue", new Color(.025f, .22f, .95f), true);
        yellow = Mat("ButtonYellow", new Color(.98f, .8f, .025f), true);
        green = Mat("ButtonGreen", new Color(.035f, .7f, .13f), true);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build multiplayer escape prototype");
        var root = new GameObject("MultiEscapePrototype");
        Undo.RegisterCreatedObjectUndo(root, "Build escape prototype");
        var systems = Group("00_GameSystems", root.transform);
        var environment = Group("01_Room_ReplaceVisuals", root.transform);
        var chairs = Group("02_Chairs", root.transform);
        var players = Group("03_PlayerRigs", root.transform);
        var puzzle = Group("04_OperatorConsole", root.transform);
        var clues = Group("05_ObserverWall", root.transform);
        var exit = Group("06_Exit", root.transform);

        game = systems.gameObject.AddComponent<MultiEscapeGame>();
        Set(game, "startAutomatically", false);
        var view = systems.gameObject.AddComponent<MultiEscapeView>();
        Set(view, "game", game);
        var feedback = systems.gameObject.AddComponent<MultiEscapeFeedback>();
        Set(feedback, "game", game);

        Box("Floor", environment, new Vector3(0, -.15f, 1), new Vector3(10, .3f, 11), concrete);
        Box("BackWall", environment, new Vector3(0, 2, -3.8f), new Vector3(10, 4, .25f), concrete);
        Box("LeftWall", environment, new Vector3(-5, 2, 1), new Vector3(.25f, 4, 10), concrete);
        Box("RightWall", environment, new Vector3(5, 2, 1), new Vector3(.25f, 4, 10), concrete);
        Box("FrontLeft", environment, new Vector3(-3.1f, 2, 5.5f), new Vector3(3.8f, 4, .25f), concrete);
        Box("FrontRight", environment, new Vector3(3.1f, 2, 5.5f), new Vector3(3.8f, 4, .25f), concrete);
        Box("ExitLintel", environment, new Vector3(0, 3.65f, 5.5f), new Vector3(2.4f, .7f, .25f), dark);
        for (int i = -4; i <= 4; i += 2)
        {
            Box("FloorSeam_" + i, environment, new Vector3(i, .008f, .8f), new Vector3(.025f, .01f, 9), dark, false);
            Box("WallRib_" + i, environment, new Vector3(i, 2, 5.3f), new Vector3(.08f, 4, .07f), steel, false);
        }
        Box("OverheadStrip", environment, new Vector3(0, 3.8f, 0), new Vector3(4.5f, .08f, .18f), white, false);
        PointLight("RoomFill", environment, new Vector3(0, 3.4f, .5f), new Color(.8f, .91f, 1), 3, 10);
        PointLight("FrontFill", environment, new Vector3(-1.5f, 2.5f, -2.5f), Color.white, 2, 7);

        BuildChair(chairs, "Chair_A", -1.45f);
        BuildChair(chairs, "Chair_B", 1.45f);
        var playerA = BuildPlayer(players, "Player_A_Operator", new Vector3(-1.45f, 0, -1.65f),
            new Vector3(-1.45f, 1.15f, .15f), EscapeRole.Blindfolded, new Vector3(-2.85f, .05f, -1.1f));
        var playerB = BuildPlayer(players, "Player_B_Observer", new Vector3(1.45f, 0, -1.65f),
            new Vector3(-.3f, 1.65f, 2.4f), EscapeRole.Observer, new Vector3(2.85f, .05f, -1.1f));
        Set(game, "playerA", playerA); Set(game, "playerB", playerB);

        Box("ArmrestConsoleSupport", puzzle, new Vector3(-1.45f, .65f, .25f), new Vector3(2.2f, .12f, .45f), steel);
        Box("ConsoleVisual_ReplaceMesh", puzzle, new Vector3(-1.45f, 1.08f, .22f), new Vector3(2.35f, 1.65f, .16f), dark);
        Label("LockOneTitle", puzzle, "01 / COLOR SEQUENCE", new Vector3(-1.45f, 1.74f, .12f), .052f, Color.white);
        var row = Group("ColorButtons_KeepLogicRoots", puzzle);
        var palette = new[] { blue, yellow, red, green };
        var colors = new[] { LockColor.Blue, LockColor.Yellow, LockColor.Red, LockColor.Green };
        for (int i = 0; i < 4; i++)
        {
            float x = -2.17f + .48f * i;
            var button = Group("Button_" + (i + 1) + "_" + colors[i], row);
            button.position = new Vector3(x, 1.46f, .08f);
            var collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(.36f, .28f, .16f);
            var visual = Box("Visual_ReplaceMesh", button, button.position, new Vector3(.34f, .25f, .13f), palette[i], false);
            var control = button.gameObject.AddComponent<MultiEscapeInteractable>();
            ConfigureControl(control, EscapeControl.ColorButton, colors[i], 0);
            Label("PositionNumber", row, (i + 1).ToString(), new Vector3(x, 1.24f, -.015f), .045f, Color.white);
        }
        var locked = Label("PanelLockedIndicator", puzzle, "LOCKED / WAIT", new Vector3(-1.45f, 1.97f, .13f), .055f, Color.white).gameObject;
        locked.SetActive(false);
        Set(view, "panelLockedIndicator", locked);

        var symbols = Group("SymbolPanel_UnlocksAfterColors", puzzle);
        var dialTransforms = new UnityEngine.Object[3];
        var dialLabels = new UnityEngine.Object[3];
        for (int i = 0; i < 3; i++)
        {
            float x = -2.15f + i * .58f;
            var dial = Group("Dial_" + i, symbols);
            var rotor = Group("Rotor_Visual_ReplaceMesh", dial);
            rotor.position = new Vector3(x, .91f, .025f);
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "DialDisc";
            disc.transform.SetParent(rotor, false);
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            disc.transform.localScale = new Vector3(.42f, .04f, .42f);
            disc.GetComponent<Renderer>().sharedMaterial = steel;
            UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            Box("RotatingMarker", rotor, rotor.position + new Vector3(0, .15f, -.05f), new Vector3(.035f, .075f, .025f), white, false);
            dialTransforms[i] = rotor;
            dialLabels[i] = Label("SelectedSymbol", dial, "O", new Vector3(x, .91f, -.045f), .065f, Color.white);
            Label("DialNumber", dial, "D" + (i + 1), new Vector3(x, 1.12f, -.04f), .03f, Color.white);
            for (int side = 0; side < 2; side++)
            {
                var controlRoot = Group(side == 0 ? "TurnLeft" : "TurnRight", dial);
                controlRoot.position = new Vector3(x + (side == 0 ? -.13f : .13f), .59f, .04f);
                var collider = controlRoot.gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(.23f, .2f, .16f);
                Box("Visual_ReplaceMesh", controlRoot, controlRoot.position, new Vector3(.22f, .18f, .12f), amber, false);
                Label("Direction", controlRoot, side == 0 ? "<" : ">", controlRoot.position + new Vector3(0, 0, -.075f), .043f, Color.black);
                ConfigureControl(controlRoot.gameObject.AddComponent<MultiEscapeInteractable>(),
                    side == 0 ? EscapeControl.DialCounterclockwise : EscapeControl.DialClockwise, LockColor.Red, i);
            }
        }
        var lever = Group("ConfirmLever_KeepLogicRoot", symbols);
        lever.position = new Vector3(-.49f, .84f, .015f);
        var leverCollider = lever.gameObject.AddComponent<BoxCollider>();
        leverCollider.size = new Vector3(.28f, .6f, .25f);
        Box("Base_Visual", lever, lever.position + new Vector3(0, -.1f, .04f), new Vector3(.25f, .42f, .12f), steel, false);
        var leverPivot = Group("HandlePivot", lever);
        leverPivot.position = lever.position + new Vector3(0, -.16f, -.07f);
        Box("Handle_ReplaceMesh", leverPivot, leverPivot.position + new Vector3(0, .2f, -.06f), new Vector3(.07f, .4f, .08f), amber, false);
        Box("Grip", leverPivot, leverPivot.position + new Vector3(0, .38f, -.06f), new Vector3(.24f, .08f, .1f), amber, false);
        Label("ConfirmLabel", lever, "PULL", lever.position + new Vector3(0, -.44f, -.1f), .035f, Color.white);
        ConfigureControl(lever.gameObject.AddComponent<MultiEscapeInteractable>(), EscapeControl.ConfirmLever, LockColor.Red, 0);
        Set(view, "symbolPanel", symbols.gameObject);
        SetArray(view, "dialVisuals", dialTransforms);
        SetArray(view, "dialTexts", dialLabels);
        SetArray(view, "wristRestraints", wrists.ToArray());
        SetArray(view, "remainingRestraints", restraints.ToArray());
        Set(feedback, "leverHandle", leverPivot);
        SetArray(feedback, "chairShockLights", shockLights.ToArray());

        Box("InstructionBoardBacking", clues, new Vector3(.15f, 2.1f, 3.4f), new Vector3(4.7f, 2.1f, .13f), dark);
        Label("BoardHeading", clues, "OBSERVER / INSTRUCTIONS", new Vector3(.15f, 3.04f, 3.31f), .065f, Color.white);
        var clueText = Label("ObserverOnlyText", clues, "", new Vector3(.15f, 2.18f, 3.30f), .047f, new Color(.9f, 1, .88f));
        var clue = clueText.gameObject.AddComponent<MultiEscapeClueBoard>();
        Set(clue, "game", game);
        clueText.gameObject.layer = 30;
        var positions = Label("ObserverOnlyButtonMap", clues, "BUTTON POSITIONS:  1 BLUE / 2 YELLOW / 3 RED / 4 GREEN",
            new Vector3(.15f, 1.27f, 3.30f), .036f, Color.white);
        positions.gameObject.layer = 30;

        var door = Box("ExitDoor_ReplaceVisual", exit, new Vector3(0, 1.55f, 5.5f), new Vector3(2.4f, 3.1f, .25f), steel);
        Set(feedback, "exitDoor", door.transform);
        Label("ExitSign", exit, "EXIT", new Vector3(0, 3.4f, 5.2f), .16f, Color.white);
        Box("CorridorFloor", exit, new Vector3(0, -.15f, 7.3f), new Vector3(3, .3f, 3), concrete);
        Box("CorridorBack", exit, new Vector3(0, 1.5f, 8.75f), new Vector3(3, 3, .2f), dark);
        Box("CorridorLeft", exit, new Vector3(-1.5f, 1.5f, 7.2f), new Vector3(.2f, 3, 3), dark);
        Box("CorridorRight", exit, new Vector3(1.5f, 1.5f, 7.2f), new Vector3(.2f, 3, 3), dark);
        PointLight("ExitGlow", exit, new Vector3(0, 2.5f, 6.8f), new Color(.6f, 1, .8f), 3, 5);

        var source = systems.gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0; source.volume = .35f;
        Set(game, "audioSource", source);
        Set(game, "wrongAnswerSound", MakeTone("WrongAnswer", 160, .3f));
        Set(game, "electricShockSound", MakeTone("ElectricShock", 70, .4f));
        Set(game, "unlockSound", MakeTone("Unlock", 680, .18f));

        var hud = Group("07_HUD_LocalSession", root.transform);
        var panel = ScriptableObject.CreateInstance<PanelSettings>();
        panel.name = "EscapePanelSettings";
        panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        panel.referenceResolution = new Vector2Int(1280, 720);
        panel.match = .5f;
        AssetDatabase.CreateAsset(panel, AssetRoot + "/EscapePanelSettings.asset");
        var document = hud.gameObject.AddComponent<UIDocument>();
        document.panelSettings = panel;
        document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts(Multi)/UI/MultiEscapeHud.uxml");
        var session = hud.gameObject.AddComponent<MultiEscapeLocalSession>();
        Set(session, "game", game); Set(session, "playerA", playerA); Set(session, "playerB", playerB);

        var originalCamera = GameObject.Find("Main Camera");
        if (originalCamera != null)
        {
            Undo.RecordObject(originalCamera, "Disable original camera");
            originalCamera.SetActive(false);
        }
        var key = GameObject.Find("Directional Light");
        if (key != null)
        {
            var light = key.GetComponent<Light>();
            Undo.RecordObject(light, "Tune prototype lighting");
            light.intensity = .7f;
            light.color = new Color(.85f, .92f, 1);
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.35f, .39f, .43f);
        RenderSettings.fog = false;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        PrefabUtility.SaveAsPrefabAsset(root, AssetRoot + "/Prefabs/FacilityEscapePrototype.prefab");
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0, 1.4f, .5f), Quaternion.Euler(25, 25, 0), 10);
        return "Built and saved MultiScene1: 2 chairs, 2 roles, 4 color buttons, 3 dials, lever, observer board, door, HUD, sounds, reusable prefab.";
    }

    static MultiEscapePlayer BuildPlayer(Transform parent, string name, Vector3 position, Vector3 lookAt, EscapeRole role, Vector3 stand)
    {
        var rig = Group(name, parent); rig.position = position;
        var cameraRoot = Group("ViewCamera", rig); cameraRoot.localPosition = new Vector3(0, 1.8f, 0);
        cameraRoot.rotation = Quaternion.LookRotation(lookAt - cameraRoot.position);
        var camera = cameraRoot.gameObject.AddComponent<Camera>();
        camera.fieldOfView = role == EscapeRole.Blindfolded ? 68 : 72;
        camera.nearClipPlane = .04f; camera.farClipPlane = 40;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f, .04f, .045f);
        camera.enabled = role == EscapeRole.Blindfolded;
        camera.cullingMask = role == EscapeRole.Blindfolded ? ~(1 << 30) : ~0;
        cameraRoot.gameObject.AddComponent<AudioListener>().enabled = camera.enabled;
        if (role == EscapeRole.Blindfolded) cameraRoot.gameObject.tag = "MainCamera";
        var player = rig.gameObject.AddComponent<MultiEscapePlayer>();
        Set(player, "game", game); Set(player, "role", (int)role); Set(player, "viewCamera", camera);
        Set(player, "localInput", role == EscapeRole.Blindfolded); Set(player, "interactionDistance", 4f);
        var motor = rig.gameObject.AddComponent<CharacterController>();
        motor.height = 1.7f; motor.radius = .22f; motor.center = new Vector3(0, .9f, 0); motor.enabled = false;
        var standRoot = Group(name + "_StandUpPoint", parent); standRoot.position = stand;
        var walk = rig.gameObject.AddComponent<MultiEscapeWalk>();
        Set(walk, "game", game); Set(walk, "player", player); Set(walk, "standUpPoint", standRoot);
        return player;
    }

    static void BuildChair(Transform parent, string name, float x)
    {
        var chair = Group(name, parent);
        var visual = Group("Visual_ReplaceWithChairAsset", chair);
        Box("Pedestal", visual, new Vector3(x, .25f, -1.4f), new Vector3(.7f, .5f, .7f), steel);
        Box("Seat", visual, new Vector3(x, .65f, -1.4f), new Vector3(1, .22f, 1), cushion);
        Box("Back", visual, new Vector3(x, 1.2f, -1.92f), new Vector3(1, 1.35f, .2f), cushion);
        Box("Headrest", visual, new Vector3(x, 1.97f, -1.92f), new Vector3(.55f, .3f, .22f), dark);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Armrest" + side, visual, new Vector3(x + side * .59f, 1, -1), new Vector3(.19f, .13f, 1.55f), steel);
            wrists.Add(Box("WristRestraint" + side, chair, new Vector3(x + side * .59f, 1.1f, -.7f), new Vector3(.26f, .12f, .15f), amber));
            restraints.Add(Box("AnkleRestraint" + side, chair, new Vector3(x + side * .22f, .24f, -.7f), new Vector3(.25f, .13f, .25f), amber));
        }
        restraints.Add(Box("WaistRestraint", chair, new Vector3(x, .88f, -1.25f), new Vector3(1.04f, .1f, .14f), amber));
        var flash = PointLight("ElectricShockFlash", chair, new Vector3(x, 1.5f, -.85f), new Color(.5f, .7f, 1), 8, 3);
        flash.enabled = false; shockLights.Add(flash);
    }

    static Material Mat(string name, Color color, bool emission = false)
    {
        var mat = new Material(Shader.Find("Standard")); mat.name = name; mat.color = color;
        mat.SetFloat("_Glossiness", .27f);
        if (emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * .45f); }
        AssetDatabase.CreateAsset(mat, AssetRoot + "/Materials/" + name + ".mat");
        return mat;
    }
    static Transform Group(string name, Transform parent)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj.transform;
    }
    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.position = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    static TextMesh Label(string name, Transform parent, string text, Vector3 position, float size, Color color)
    {
        var obj = Group(name, parent); obj.position = position;
        var label = obj.gameObject.AddComponent<TextMesh>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text; label.fontSize = 64; label.characterSize = size;
        label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.color = color;
        label.GetComponent<Renderer>().sharedMaterial = label.font.material;
        label.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        return label;
    }
    static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        var obj = Group(name, parent); obj.position = position;
        var light = obj.gameObject.AddComponent<Light>(); light.type = LightType.Point;
        light.color = color; light.intensity = intensity; light.range = range; return light;
    }
    static void ConfigureControl(MultiEscapeInteractable control, EscapeControl type, LockColor color, int dial)
    {
        Set(control, "game", game); Set(control, "control", (int)type); Set(control, "color", (int)color); Set(control, "dialIndex", dial);
    }
    static void Set(UnityEngine.Object target, string name, object value)
    {
        var so = new SerializedObject(target); var property = so.FindProperty(name);
        if (property == null) throw new Exception(target.name + ": missing " + name);
        if (value is UnityEngine.Object reference) property.objectReferenceValue = reference;
        else if (value is bool b) property.boolValue = b;
        else if (value is int i) property.intValue = i;
        else if (value is float f) property.floatValue = f;
        else throw new Exception("Unsupported serialized value: " + name);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(target); var property = so.FindProperty(name); property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static AudioClip MakeTone(string name, double frequency, double duration)
    {
        string path = AssetRoot + "/Audio/" + name + ".wav";
        int rate = 22050, samples = (int)(rate * duration);
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
            writer.Write((short)2); writer.Write((short)16); writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
            for (int i = 0; i < samples; i++)
            {
                double envelope = Math.Min(1, i / 200.0) * (1 - (double)i / samples);
                double wave = Math.Sin(2 * Math.PI * frequency * i / rate);
                writer.Write((short)(wave * envelope * 13000));
            }
        }
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
