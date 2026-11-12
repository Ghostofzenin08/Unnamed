#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Outpost404.EditorTools
{
    public static class SkyboxPackageImporter
    {
        private const string PackagePath = @"C:\Users\HARSHAL\AppData\Roaming\Unity\Asset Store-5.x\Render Knight\Textures MaterialsSkies\Fantasy Skybox FREE.unitypackage";

        [MenuItem("Outpost 404/Import Skybox Package", false, 15)]
        public static void ImportPackage()
        {
            if (!File.Exists(PackagePath))
            {
                Debug.LogError($"[Outpost 404] Skybox package not found at: {PackagePath}");
                return;
            }

            Debug.Log($"[Outpost 404] Importing Skybox package from: {PackagePath} ...");
            AssetDatabase.ImportPackage(PackagePath, false);
            AssetDatabase.Refresh();
            Debug.Log("[Outpost 404] Skybox package import command issued successfully.");
        }
    }
}
#endif
