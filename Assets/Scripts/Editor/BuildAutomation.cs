#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Outpost404.EditorTools
{
    /// <summary>
    /// Automates creation of standalone Windows executables for Outpost 404.
    /// Provides one-click editor menu building and headless CI/CD execution.
    /// </summary>
    public static class BuildAutomation
    {
        public const string BuildOutputDirectory = "Builds/Windows";
        public const string BuildExecutablePath = "Builds/Windows/Outpost404.exe";
        public const string DefaultScenePath = "Assets/Scenes/SampleScene.unity";

        /// <summary>
        /// Editor menu trigger to build the Windows standalone executable.
        /// </summary>
        [MenuItem("Outpost 404/Build Windows Executable", false, 100)]
        public static BuildReport BuildWindowsExecutable()
        {
            return PerformBuild(BuildExecutablePath);
        }

        /// <summary>
        /// Executes a Windows Standalone64 build targeting the specified executable path.
        /// </summary>
        public static BuildReport PerformBuild(string targetExePath)
        {
            string outputDir = Path.GetDirectoryName(targetExePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string[] scenes = GetConfiguredBuildScenes();

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = targetExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"<color=cyan>[Outpost 404 Build]</color> Initiating Windows Standalone64 build targeting: {targetExePath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[Outpost 404 Build]</color> Build succeeded! Output: {targetExePath} ({summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s)");
            }
            else
            {
                Debug.LogError($"<color=red>[Outpost 404 Build]</color> Build {summary.result} with {summary.totalErrors} errors!");
            }

            return report;
        }

        /// <summary>
        /// Retrieves the list of scenes configured in EditorBuildSettings, falling back to SampleScene.unity if none enabled.
        /// </summary>
        public static string[] GetConfiguredBuildScenes()
        {
            List<string> scenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    scenes.Add(scene.path);
                }
            }

            if (scenes.Count == 0 || !scenes.Contains(DefaultScenePath))
            {
                if (!scenes.Contains(DefaultScenePath))
                {
                    scenes.Insert(0, DefaultScenePath);
                }
            }

            return scenes.ToArray();
        }
    }
}
#endif
