#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Outpost404.Audio;
using Outpost404.CameraControl;

namespace Outpost404.EditorTools
{
    public static class Phase8SceneSetup
    {
        [MenuItem("Outpost 404/Setup Full Game Scene (Phase 8 - Audio & Combat Juice)", false, 11)]
        public static void BuildPhase8Scene()
        {
            // 1. Build Phase 7 base scene (Day/Night, thermal hazards, 3D models, inventory, crafting)
            Phase7SceneSetup.BuildPhase7Scene();

            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 2. Ensure Project Scene Opener and Build Settings are registered
            ProjectSceneOpener.EnsureBuildSettingsConfigured();

            // 3. Setup AudioManager in scene
            GameObject audioMgrObj = GameObject.Find("AudioManager");
            if (audioMgrObj == null)
            {
                audioMgrObj = new GameObject("AudioManager");
            }

            var audioMgr = audioMgrObj.GetComponent<AudioManager>();
            if (audioMgr == null)
            {
                audioMgr = audioMgrObj.AddComponent<AudioManager>();
            }

            audioMgr.EnsureAudioSources();

            if (audioMgr.MusicSource != null)
            {
                audioMgr.MusicSource.loop = true;
                audioMgr.MusicSource.playOnAwake = false;
                audioMgr.MusicSource.spatialBlend = 0f;
                audioMgr.MusicSource.volume = audioMgr.EffectiveMusicVolume;
            }

            if (audioMgr.SFXSource != null)
            {
                audioMgr.SFXSource.loop = false;
                audioMgr.SFXSource.playOnAwake = false;
                audioMgr.SFXSource.spatialBlend = 0f;
                audioMgr.SFXSource.volume = audioMgr.EffectiveSFXVolume;
            }

            audioMgr.PrewarmProceduralClips();

            // 4. Ensure Camera Follow has TopDownCameraFollow with screen shake ready
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var follow = mainCam.GetComponent<TopDownCameraFollow>();
                if (follow == null)
                {
                    follow = mainCam.gameObject.AddComponent<TopDownCameraFollow>();
                }
            }

            // 5. Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> Phase 8 Scene Setup (Audio & Combat Juice) successfully completed and saved!");
        }
    }
}
#endif
