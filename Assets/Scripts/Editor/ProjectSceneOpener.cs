#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Outpost404.EditorTools
{
    /// <summary>
    /// Ensures that when the Unity Editor opens or loads the project without an active scene,
    /// it automatically restores and loads the primary gameplay level (Assets/Scenes/SampleScene.unity)
    /// rather than leaving the user on a blank, unsaved 'Untitled' scene.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSceneOpener
    {
        public const string DefaultScenePath = "Assets/Scenes/SampleScene.unity";

        static ProjectSceneOpener()
        {
            EditorApplication.delayCall += OnEditorStartup;
        }

        private static void OnEditorStartup()
        {
            if (Application.isBatchMode) return;

            EnsureBuildSettingsConfigured();
            EnsureDefaultSceneOpen();
        }

        [MenuItem("Outpost 404/Open Game Scene (SampleScene)", false, 1)]
        public static bool OpenGameSceneMenu()
        {
            return OpenSceneDirect(DefaultScenePath);
        }

        public static bool EnsureDefaultSceneOpen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            bool isUntitledOrEmpty = !activeScene.IsValid() ||
                                     string.IsNullOrEmpty(activeScene.path) ||
                                     activeScene.name == "Untitled";

            if (isUntitledOrEmpty && File.Exists(DefaultScenePath))
            {
                return OpenSceneDirect(DefaultScenePath);
            }

            return false;
        }

        public static bool EnsureBuildSettingsConfigured()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool found = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == DefaultScenePath)
                {
                    found = true;
                    if (!scenes[i].enabled)
                    {
                        scenes[i].enabled = true;
                        EditorBuildSettings.scenes = scenes;
                    }
                    break;
                }
            }

            if (!found)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
                list.Insert(0, new EditorBuildSettingsScene(DefaultScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
                return true;
            }

            return false;
        }

        public static bool OpenSceneDirect(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Outpost 404] Cannot open scene. Path not found: {path}");
                return false;
            }

            try
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Debug.Log($"<color=green>[Outpost 404]</color> Restored active level: {path}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Outpost 404] Failed opening {path}: {ex.Message}");
                return false;
            }
        }
    }
}
#endif
