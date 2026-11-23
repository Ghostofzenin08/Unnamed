#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Outpost404.Environment;
using Outpost404.Audio;

namespace Outpost404.EditorTools
{
    /// <summary>
    /// Automates the creation and setup of URP Post-Processing Global Volumes,
    /// stylized post-processing profiles, biome environmental particle systems,
    /// and dynamic terrain splatmap footstep audio detection.
    /// </summary>
    public static class AtmosphericAndPolishSetup
    {
        public const string ScenePath = "Assets/Scenes/SampleScene.unity";
        public const string SettingsDirectory = "Assets/Settings";
        public const string ProfilePath = "Assets/Settings/PostProcess_GlobalVolume.asset";

        [MenuItem("Outpost 404/Setup Atmospheric FX and Post-Processing", false, 15)]
        public static void BuildAtmosphericFXAndPostProcessing()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var scene = (activeScene.path == ScenePath && activeScene.isLoaded)
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 1. Ensure Profile Asset Directory and Asset exist
            if (!AssetDatabase.IsValidFolder(SettingsDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // 2. Configure Tonemapping (ACES for stylized cinematic contrast)
            if (!profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // 3. Configure Bloom (intensity 1.2, threshold 0.9 for visors, crystal veins, beacon lights)
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.2f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;

            // 4. Configure Vignette (baseline 0.25, deepening dynamically at night or freezing)
            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.25f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.4f;
            vignette.color.overrideState = true;
            vignette.color.value = Color.black;

            // 5. Configure Color Adjustments (contrast +10, saturation +12 for crisp top-down readability)
            if (!profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            colorAdj.contrast.overrideState = true;
            colorAdj.contrast.value = 10f;
            colorAdj.saturation.overrideState = true;
            colorAdj.saturation.value = 12f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // 6. Setup or update Global Post-Processing Volume in scene
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");

            GameObject volObj = GameObject.Find("Global_PostProcess_Volume");
            if (volObj == null)
            {
                volObj = new GameObject("Global_PostProcess_Volume");
                volObj.transform.SetParent(envRoot.transform, false);
            }

            var volume = volObj.GetComponent<Volume>();
            if (volume == null) volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;

            var ppMgr = volObj.GetComponent<AtmospherePostProcessManager>();
            if (ppMgr == null) ppMgr = volObj.AddComponent<AtmospherePostProcessManager>();
            ppMgr.ApplyDefaultProfileSettings();

            // 7. Setup Biome Environmental Particle Systems
            GameObject fxObj = GameObject.Find("Atmospheric_Particle_Systems");
            if (fxObj == null)
            {
                fxObj = new GameObject("Atmospheric_Particle_Systems");
                fxObj.transform.SetParent(envRoot.transform, false);
            }

            var particleMgr = fxObj.GetComponent<BiomeParticleManager>();
            if (particleMgr == null) particleMgr = fxObj.AddComponent<BiomeParticleManager>();
            particleMgr.EnsureEmitters();

            // 8. Ensure Player has TerrainSurfaceAudio attached
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null)
            {
                var surfaceAudio = player.GetComponent<TerrainSurfaceAudio>();
                if (surfaceAudio == null) player.AddComponent<TerrainSurfaceAudio>();
            }

            // 9. Save scene and project assets
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> URP Post-Processing Global Volume, Biome Particle Systems, and Terrain Surface Audio successfully configured!");
        }
    }
}
#endif
