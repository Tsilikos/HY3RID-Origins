// HY3RID Origins — Phase 3 URP 2D activation script.
// Run from the menu: HY3RID / Setup Phase 3 (URP 2D)
// or in batch mode: -executeMethod Phase3Setup.Run
//
// What this does:
//   1. Creates Renderer2DData + UniversalRenderPipelineAsset in Assets/Settings/
//   2. Activates URP 2D as the project render pipeline (GraphicsSettings)
//   3. Patches the existing scene:
//      – Replaces the Phase 1 3D Point Light with a URP Global Light2D (intensity 0.03)
//      – Adds UniversalAdditionalCameraData to the main camera
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class Phase3Setup
{
    [MenuItem("HY3RID/Setup Phase 3 (URP 2D)")]
    public static void RunMenu() => Run();

    public static void Run()
    {
        Debug.Log("[Phase3Setup] Activating URP 2D renderer...");
        ActivateUrp2D();
        PatchScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Phase3Setup] Phase 3 done. Open Lab_Floor_01.unity and press Play.");
    }

    // -------------------------------------------------------------------------
    // Step 1: Create Renderer2DData + URP asset, set as active pipeline.
    // -------------------------------------------------------------------------
    static void ActivateUrp2D()
    {
        const string r2DPath  = "Assets/Settings/URPRenderer2D.asset";
        const string urpPath  = "Assets/Settings/URPAsset_2D.asset";

        Directory.CreateDirectory("Assets/Settings");

        // Renderer2DData — the 2D-specific renderer (enables Light2D / ShadowCaster2D).
        var r2D = AssetDatabase.LoadAssetAtPath<Renderer2DData>(r2DPath);
        if (r2D == null)
        {
            r2D = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(r2D, r2DPath);
            Debug.Log($"[Phase3Setup] Created Renderer2DData at {r2DPath}");
        }

        // UniversalRenderPipelineAsset — must be created AFTER renderer is saved to disk.
        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
        if (urpAsset == null)
        {
            urpAsset = UniversalRenderPipelineAsset.Create(r2D);
            AssetDatabase.CreateAsset(urpAsset, urpPath);
            Debug.Log($"[Phase3Setup] Created URP 2D asset at {urpPath}");
        }

        // Activate as the global render pipeline via ProjectSettings/GraphicsSettings.asset.
        var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (gs.Length > 0)
        {
            var so = new SerializedObject(gs[0]);
            var prop = so.FindProperty("m_CustomRenderPipeline");
            if (prop != null)
            {
                prop.objectReferenceValue = urpAsset;
                so.ApplyModifiedProperties();
                Debug.Log("[Phase3Setup] URP 2D set as active render pipeline.");
            }
        }
        else
        {
            Debug.LogWarning("[Phase3Setup] GraphicsSettings not found — pipeline not activated.");
        }
    }

    // -------------------------------------------------------------------------
    // Step 2: Patch the scene: replace 3D ambient light, add camera component.
    // -------------------------------------------------------------------------
    static void PatchScene()
    {
        const string scenePath = "Assets/Scenes/Lab_Floor_01.unity";

        if (!File.Exists(scenePath))
        {
            Debug.LogWarning($"[Phase3Setup] Scene not found at {scenePath} — run Setup Phase 1 first.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // ---- Replace Phase 1 3D Point Light with URP Global Light2D ----
        var ambientGO = GameObject.Find("AmbientLight");
        if (ambientGO != null)
        {
            // Remove the legacy 3D light (used as a fallback before URP was active).
            var legacyLight = ambientGO.GetComponent<Light>();
            if (legacyLight != null)
                Object.DestroyImmediate(legacyLight);

            // Add (or reuse) a URP 2D Global Light for the very-dark ambient layer.
            var globalLight = ambientGO.GetComponent<Light2D>()
                ?? ambientGO.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.03f;
            globalLight.color     = new Color(0.50f, 0.55f, 0.70f); // cool lab blue-white

            Debug.Log("[Phase3Setup] AmbientLight → URP Global Light2D (intensity=0.03).");
        }
        else
        {
            Debug.LogWarning("[Phase3Setup] 'AmbientLight' not found — skipping light replacement.");
        }

        // ---- Add UniversalAdditionalCameraData to the main camera ----
        // Required for URP to know this camera uses the 2D renderer.
        var camGO = GameObject.FindWithTag("MainCamera");
        if (camGO != null)
        {
            var cameraData = camGO.GetComponent<UniversalAdditionalCameraData>()
                ?? camGO.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.SetRenderer(0); // index 0 = the Renderer2DData we created
            Debug.Log("[Phase3Setup] UniversalAdditionalCameraData wired to Main Camera (renderer 0).");
        }
        else
        {
            Debug.LogWarning("[Phase3Setup] MainCamera not found — skipping camera patch.");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Phase3Setup] Scene saved: {scenePath}");
    }
}
