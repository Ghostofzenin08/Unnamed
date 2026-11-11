#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Outpost404.Tests
{
    public class AssetModelTests
    {
        private readonly string[] assetNames = new string[]
        {
            "AlienPredatorBeast.obj",
            "AlienBeastMeat.glb",
            "AlienOrganicShrub.glb",
            "AlienOrganicShrubAlt.glb",
            "AlienTimberTree.glb",
            "ConductiveOre.glb",
            "EmergencyRationContainer.glb",
            "HarvestedTimberLogs.glb",
            "MineralBoulder.glb",
            "OrbitalAstronaut.glb",
            "SalvagedMetallicHull.obj"
        };

        [Test]
        public void StagedAssets_All11ModelsLoadWithValidMeshesAndRenderers()
        {
            foreach (var assetName in assetNames)
            {
                string path = $"Assets/Art/Models/{assetName}";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                Assert.IsNotNull(model, $"Model at '{path}' failed to load via AssetDatabase.");

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(renderers.Length, 0, $"Model '{assetName}' must contain at least one Renderer.");

                MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
                SkinnedMeshRenderer[] skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                int meshCount = filters.Length + skinned.Length;
                Assert.Greater(meshCount, 0, $"Model '{assetName}' must contain at least one mesh filter or skinned mesh renderer.");

                Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool hasBounds = false;
                foreach (var r in renderers)
                {
                    if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
                    else bounds.Encapsulate(r.bounds);
                }
                Debug.Log($"[ModelData] {assetName} => bounds size: {bounds.size}, center: {bounds.center}");
            }
        }
    }
}
#endif
