// HY3RID Origins — Phase 1 project setup script.
// Run from the menu: HY3RID / Setup Phase 1
// Or in batch mode: -executeMethod ProjectSetup.Run
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ProjectSetup
{
    // Entry point for batch mode: Unity -executeMethod ProjectSetup.Run
    public static void Run()
    {
        Debug.Log("[ProjectSetup] Starting Phase 1 setup...");
        SetupInputSystem();
        SetupPhysics2D();
        SetupTags();
        SetupLayers();
        CreateScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ProjectSetup] Phase 1 setup complete. Open Assets/Scenes/Lab_Floor_01.unity and press Play.");
    }

    [MenuItem("HY3RID/Setup Phase 1")]
    public static void RunMenu() => Run();

    // ------------------------------------------------------------------
    // Input System — set Active Input Handling to New Input System
    // ------------------------------------------------------------------
    static void SetupInputSystem()
    {
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (settings.Length == 0) { Debug.LogWarning("[ProjectSetup] Could not load ProjectSettings."); return; }

        var so = new SerializedObject(settings[0]);
        // 0 = Legacy, 1 = New Input System, 2 = Both
        var prop = so.FindProperty("activeInputHandler");
        if (prop != null)
        {
            prop.intValue = 1;
            so.ApplyModifiedProperties();
            Debug.Log("[ProjectSetup] Input System set to New Input System Package.");
        }
    }

    // ------------------------------------------------------------------
    // Physics 2D — zero gravity, top-down
    // ------------------------------------------------------------------
    static void SetupPhysics2D()
    {
        Physics2D.gravity = Vector2.zero;
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (ps.Length > 0)
        {
            var so = new SerializedObject(ps[0]);
            var grav = so.FindProperty("gravity");
            if (grav != null)
            {
                grav.vector2Value = Vector2.zero;
                so.ApplyModifiedProperties();
            }
        }
        Debug.Log("[ProjectSetup] Physics2D gravity = zero.");
    }

    // ------------------------------------------------------------------
    // Tags
    // ------------------------------------------------------------------
    static void SetupTags()
    {
        AddTag("Player");
        AddTag("Enemy");
        AddTag("Obstacle");
        AddTag("Projectile");
        AddTag("SquadMember");
    }

    static void AddTag(string tag)
    {
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (ps.Length == 0) return;
        var so = new SerializedObject(ps[0]);
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }

    // ------------------------------------------------------------------
    // Layers
    // ------------------------------------------------------------------
    static void SetupLayers()
    {
        AddLayer("Character", 8);
        AddLayer("Enemy", 9);
        AddLayer("Obstacle", 10);
        AddLayer("Projectile", 11);
    }

    static void AddLayer(string name, int index)
    {
        var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (ps.Length == 0) return;
        var so = new SerializedObject(ps[0]);
        var layers = so.FindProperty("layers");
        if (layers == null || index >= layers.arraySize) return;
        var el = layers.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(el.stringValue))
        {
            el.stringValue = name;
            so.ApplyModifiedProperties();
        }
    }

    // ------------------------------------------------------------------
    // Scene creation
    // ------------------------------------------------------------------
    static void CreateScene()
    {
        const string scenePath = "Assets/Scenes/Lab_Floor_01.unity";
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Camera ----
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<UnityEngine.Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.039f, 0.039f, 0.039f, 1f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        var labCam = camGO.AddComponent<HY3RIDOrigins.Camera.LabCamera>();

        // ---- Global ambient light (low — flashlight added Phase 3) ----
        // Using a plain PointLight as a work-around when URP 2D Light is not yet configured.
        // Phase 3 replaces this with a URP Global Light 2D at intensity 0.03.
        var ambientLightGO = new GameObject("AmbientLight");
        var light = ambientLightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 500f;
        light.intensity = 0.15f;
        light.color = new Color(0.5f, 0.55f, 0.7f); // slightly cool blue-white lab feel
        ambientLightGO.transform.position = new Vector3(0f, 0f, -5f);

        // ---- Room Builder ----
        var roomBuilderGO = new GameObject("RoomBuilder");
        var roomBuilder = roomBuilderGO.AddComponent<HY3RIDOrigins.Rooms.LabRoomBuilder>();

        // ---- Scene Initializer ----
        var initGO = new GameObject("SceneInitializer");
        var initializer = initGO.AddComponent<HY3RIDOrigins.Rooms.LabSceneInitializer>();

        // Wire serialized references on SceneInitializer
        var soInit = new SerializedObject(initializer);
        soInit.FindProperty("roomBuilder").objectReferenceValue = roomBuilder;
        soInit.FindProperty("labCamera").objectReferenceValue = labCam;
        soInit.ApplyModifiedProperties();

        // Wire labCamera onto RoomBuilder so SetBounds() is called from the scene reference,
        // not via FindObjectOfType (FindObjectOfType is the belt-and-suspenders fallback).
        var soRoom = new SerializedObject(roomBuilder);
        soRoom.FindProperty("labCamera").objectReferenceValue = labCam;
        soRoom.ApplyModifiedProperties();

        // ---- Debug Console ----
        var debugGO = new GameObject("DebugConsole");
        debugGO.AddComponent<HY3RIDOrigins.DevTools.DebugConsole>();

        // ---- Save scene ----
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[ProjectSetup] Scene saved to {scenePath}");

        // Add to build settings
        var buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = buildScenes;
        Debug.Log("[ProjectSetup] Scene added to Build Settings.");
    }
}
