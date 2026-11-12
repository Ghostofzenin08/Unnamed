#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Outpost404.Environment;

namespace Outpost404.Tests
{
    [TestFixture]
    public class TerrainAndEnvironmentTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [Test, Order(1)]
        public void Scene_BuildSculptedTerrain_ExecutesCleanlyAndSaves()
        {
            Assert.DoesNotThrow(() =>
            {
                Outpost404.EditorTools.TerrainAndEnvironmentSetup.BuildSculptedTerrainAndEnvironment();
            }, "TerrainAndEnvironmentSetup.BuildSculptedTerrainAndEnvironment must execute without errors.");
        }

        [Test, Order(2)]
        public void Scene_Terrain_ExistsWithTerrainDataAndCollider()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var env = GameObject.Find("Environment");
            Assert.IsNotNull(env, "Environment root GameObject must exist in scene.");

            var terrain = env.GetComponentInChildren<Terrain>();
            Assert.IsNotNull(terrain, "Native Unity Terrain component must exist under Environment root.");
            Assert.IsTrue(terrain.gameObject.activeInHierarchy, "Terrain GameObject must be active in hierarchy.");

            // Verify Terrain position & bounds
            Vector3 pos = terrain.transform.position;
            Assert.AreEqual(-60f, pos.x, 0.5f, "Terrain origin X must be -60m to center 120m world.");
            Assert.AreEqual(0f, pos.y, 0.1f, "Terrain origin Y must be 0m.");
            Assert.AreEqual(-60f, pos.z, 0.5f, "Terrain origin Z must be -60m to center 120m world.");

            // Verify TerrainData
            TerrainData tData = terrain.terrainData;
            Assert.IsNotNull(tData, "Terrain must have a valid TerrainData asset assigned.");
            Assert.AreEqual(120f, tData.size.x, 0.5f, "Terrain size X must be 120m.");
            Assert.AreEqual(18f, tData.size.y, 0.5f, "Terrain height size Y must be 18m.");
            Assert.AreEqual(120f, tData.size.z, 0.5f, "Terrain size Z must be 120m.");
            Assert.AreEqual(129, tData.heightmapResolution, "Heightmap resolution must be 129.");

            // Verify TerrainCollider
            var tCol = terrain.GetComponent<TerrainCollider>();
            Assert.IsNotNull(tCol, "Terrain must have a TerrainCollider component.");
            Assert.IsTrue(tCol.enabled, "TerrainCollider must be enabled.");
            Assert.AreEqual(tData, tCol.terrainData, "TerrainCollider must reference the terrain's TerrainData.");

            // Verify Navigation Static flag
            var flags = GameObjectUtility.GetStaticEditorFlags(terrain.gameObject);
            Assert.IsTrue((flags & StaticEditorFlags.NavigationStatic) != 0, "Terrain must have NavigationStatic editor flag set.");
        }

        [Test, Order(3)]
        public void Scene_Terrain_TopographyAndBiomeTopographyConfigured()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var env = GameObject.Find("Environment");
            var terrain = env.GetComponentInChildren<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist.");

            TerrainData tData = terrain.terrainData;
            Assert.IsNotNull(tData, "TerrainData must exist.");

            // Verify 3 Stylized PBR TerrainLayers
            TerrainLayer[] layers = tData.terrainLayers;
            Assert.IsNotNull(layers, "Terrain layers must not be null.");
            Assert.GreaterOrEqual(layers.Length, 3, "Terrain must have at least 3 TerrainLayers assigned.");

            Assert.IsNotNull(layers[0].diffuseTexture, "Layer 1 (Basalt) must have a diffuse texture.");
            Assert.IsNotNull(layers[1].diffuseTexture, "Layer 2 (Moss) must have a diffuse texture.");
            Assert.IsNotNull(layers[2].diffuseTexture, "Layer 3 (Crystalline Rock) must have a diffuse texture.");

            // Verify Sector Alpha: Impact crater bowl vs raised rim
            // Center of crash site at (-30, -30)
            float craterCenterY = terrain.SampleHeight(new Vector3(-30f, 0f, -30f));
            float craterRimY = terrain.SampleHeight(new Vector3(-30f + 14f, 0f, -30f));
            Assert.Less(craterCenterY, 1.2f, "Crater floor at center must be a sunken bowl (< 1.2m).");
            Assert.Greater(craterRimY, 2.2f, "Crater rim must rise above surrounding terrain (> 2.2m).");
            Assert.Greater(craterRimY - craterCenterY, 1.2f, "Crater rim must be significantly higher than sunken floor.");

            // Verify Crater outer rim smooth continuity without abrupt cliff tears (19.5m vs 20.5m from crash center)
            float innerBorderY = terrain.SampleHeight(new Vector3(-30f + 19.5f, 0f, -30f));
            float outerBorderY = terrain.SampleHeight(new Vector3(-30f + 20.5f, 0f, -30f));
            Assert.Less(Mathf.Abs(outerBorderY - innerBorderY), 0.35f, "Crater rim outer boundary must transition smoothly without vertical cliff tears.");

            // Verify Sector Beta: Rolling hills in Deep Timber Forest (-30, +30)
            float betaY = terrain.SampleHeight(new Vector3(-30f, 0f, 30f));
            Assert.Greater(betaY, 0.5f, "Sector Beta forest floor must have positive elevation.");

            // Verify Perimeter Mountain Ring cliffs near borders (X = ±57m or Z = ±57m)
            float northCliffY = terrain.SampleHeight(new Vector3(0f, 0f, 57f));
            float southCliffY = terrain.SampleHeight(new Vector3(0f, 0f, -57f));
            float eastCliffY = terrain.SampleHeight(new Vector3(57f, 0f, 0f));
            float westCliffY = terrain.SampleHeight(new Vector3(-57f, 0f, 0f));

            Assert.Greater(northCliffY, 8.0f, "North perimeter mountain cliffs must rise high (> 8.0m).");
            Assert.Greater(southCliffY, 8.0f, "South perimeter mountain cliffs must rise high (> 8.0m).");
            Assert.Greater(eastCliffY, 8.0f, "East perimeter mountain cliffs must rise high (> 8.0m).");
            Assert.Greater(westCliffY, 8.0f, "West perimeter mountain cliffs must rise high (> 8.0m).");
        }

        [Test, Order(4)]
        public void Scene_ElevationGrounding_PlayerAndWorldEntitiesRestOnTerrain()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var env = GameObject.Find("Environment");
            var terrain = env.GetComponentInChildren<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist.");

            float TerrainY(Vector3 pos) => terrain.SampleHeight(pos) + terrain.transform.position.y;

            // 1. Verify Player grounding
            var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            Assert.IsNotNull(player, "Player must exist.");
            float expectedPlayerY = TerrainY(player.transform.position);
            Assert.AreEqual(expectedPlayerY, player.transform.position.y, 0.05f, "Player root must rest firmly on terrain surface.");

            // 2. Verify SOS Beacon grounding
            var beacon = GameObject.Find("Beacon_SOS_Terminal");
            Assert.IsNotNull(beacon, "Beacon_SOS_Terminal must exist.");
            float expectedBeaconY = TerrainY(beacon.transform.position);
            Assert.AreEqual(expectedBeaconY, beacon.transform.position.y, 0.05f, "Beacon terminal must rest firmly on terrain surface.");

            // 3. Verify 30+ Trees grounding in Sector Beta
            int treeCount = 0;
            foreach (Transform child in env.transform)
            {
                if (child.name.StartsWith("Tree_Alien_"))
                {
                    treeCount++;
                    float expectedTreeY = TerrainY(child.position);
                    Assert.AreEqual(expectedTreeY, child.position.y, 0.05f, $"{child.name} must rest firmly on terrain surface.");
                }
            }
            Assert.GreaterOrEqual(treeCount, 30, "At least 30 alien timber trees must exist in scene.");

            // 4. Verify 30+ Rocks grounding in Sector Gamma
            int rockCount = 0;
            foreach (Transform child in env.transform)
            {
                if (child.name.StartsWith("Rock_Pulse_"))
                {
                    rockCount++;
                    float expectedRockY = TerrainY(child.position);
                    Assert.AreEqual(expectedRockY, child.position.y, 0.05f, $"{child.name} must rest firmly on terrain surface.");
                }
            }
            Assert.GreaterOrEqual(rockCount, 30, "At least 30 mineral boulders must exist in scene.");

            // 5. Verify Crashed Pod Wreckage grounding
            var pod = GameObject.Find("Pod_Wreckage_Crashed");
            Assert.IsNotNull(pod, "Pod_Wreckage_Crashed must exist.");
            float expectedPodY = TerrainY(pod.transform.position) + 0.4f;
            Assert.AreEqual(expectedPodY, pod.transform.position.y, 0.05f, "Pod wreckage must sit cleanly grounded in crater.");

            // 6. Verify Horde Spawn Points grounding
            var hordeRoot = GameObject.Find("HordeSpawnPoints");
            Assert.IsNotNull(hordeRoot, "HordeSpawnPoints must exist.");
            Assert.GreaterOrEqual(hordeRoot.transform.childCount, 4, "Must have at least 4 horde spawn points.");
            foreach (Transform sp in hordeRoot.transform)
            {
                float expectedSpY = TerrainY(sp.position) + 0.1f;
                Assert.AreEqual(expectedSpY, sp.position.y, 0.05f, $"{sp.name} must rest firmly on ground.");
            }

            // 7. Verify 10+ Resource Pickups grounding across sectors
            int pickupCount = 0;
            foreach (Transform child in env.transform)
            {
                if (child.name.StartsWith("Pickup_"))
                {
                    pickupCount++;
                    float expectedPickupY = TerrainY(child.position) + 0.25f;
                    Assert.AreEqual(expectedPickupY, child.position.y, 0.05f, $"{child.name} pickup must rest firmly on terrain surface.");
                }
            }
            Assert.GreaterOrEqual(pickupCount, 10, "At least 10 starter resource pickups must exist grounded in scene.");

            // 8. Verify Prowler Enemies grounding in Sector Delta
            var enemiesRoot = GameObject.Find("Enemies");
            Assert.IsNotNull(enemiesRoot, "Enemies root must exist.");
            Assert.GreaterOrEqual(enemiesRoot.transform.childCount, 4, "At least 4 active prowlers must exist in scene.");
            foreach (Transform enemy in enemiesRoot.transform)
            {
                float expectedEnemyY = TerrainY(enemy.position) + 0.05f;
                Assert.AreEqual(expectedEnemyY, enemy.position.y, 0.05f, $"{enemy.name} prowler must rest firmly on terrain surface.");
            }
        }

        [Test, Order(5)]
        public void Scene_NavMesh_BakedAndValidAcrossSculptedTerrain()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var triangulation = NavMesh.CalculateTriangulation();

            Assert.Greater(triangulation.vertices.Length, 0, "Baked NavMesh must contain vertices across the terrain.");
            Assert.Greater(triangulation.indices.Length, 0, "Baked NavMesh must contain triangle polygons.");

            var env = GameObject.Find("Environment");
            var terrain = env != null ? env.GetComponentInChildren<Terrain>() : null;
            Assert.IsNotNull(terrain, "Terrain must exist.");

            // Sample NavMesh at Player position
            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player must exist.");
            NavMeshHit hit;
            bool hitPlayer = NavMesh.SamplePosition(player.transform.position, out hit, 2.5f, NavMesh.AllAreas);
            Assert.IsTrue(hitPlayer, "Player position must be on or immediately adjacent to valid NavMesh.");

            // Sample NavMesh at SOS Beacon position in Sector Alpha crater
            var beacon = GameObject.Find("Beacon_SOS_Terminal");
            Assert.IsNotNull(beacon, "Beacon must exist.");
            bool hitBeacon = NavMesh.SamplePosition(beacon.transform.position, out hit, 2.5f, NavMesh.AllAreas);
            Assert.IsTrue(hitBeacon, "Beacon position in crater bowl must be on or adjacent to valid NavMesh.");

            // Sample NavMesh in Sector Beta forest hills
            Vector3 betaPos = new Vector3(-20f, 0f, 20f);
            betaPos.y = terrain.SampleHeight(betaPos) + terrain.transform.position.y;
            NavMeshHit hitBeta;
            bool hasHitBeta = NavMesh.SamplePosition(betaPos, out hitBeta, 3.0f, NavMesh.AllAreas);
            Assert.IsTrue(hasHitBeta, "Sector Beta forest hills must have walkable NavMesh.");

            // Sample NavMesh in Sector Gamma terraces
            Vector3 gammaPos = new Vector3(20f, 0f, 20f);
            gammaPos.y = terrain.SampleHeight(gammaPos) + terrain.transform.position.y;
            NavMeshHit hitGamma;
            bool hasHitGamma = NavMesh.SamplePosition(gammaPos, out hitGamma, 3.0f, NavMesh.AllAreas);
            Assert.IsTrue(hasHitGamma, "Sector Gamma terraces must have walkable NavMesh.");

            // Sample NavMesh in Sector Delta anomaly hive
            Vector3 deltaPos = new Vector3(20f, 0f, -20f);
            deltaPos.y = terrain.SampleHeight(deltaPos) + terrain.transform.position.y;
            NavMeshHit hitDelta;
            bool hasHitDelta = NavMesh.SamplePosition(deltaPos, out hitDelta, 3.0f, NavMesh.AllAreas);
            Assert.IsTrue(hasHitDelta, "Sector Delta anomaly hive must have walkable NavMesh.");

            // Cross-sector pathfinding: Verify complete path from Sector Beta forest to Player in Sector Alpha
            var pathBetaToPlayer = new NavMeshPath();
            bool hasPathBeta = NavMesh.CalculatePath(hitBeta.position, player.transform.position, NavMesh.AllAreas, pathBetaToPlayer);
            Assert.IsTrue(hasPathBeta && pathBetaToPlayer.status == NavMeshPathStatus.PathComplete, "NavMesh path from Sector Beta to Player in Sector Alpha must be PathComplete.");

            // Cross-sector pathfinding: Verify complete path from Sector Delta hive to Player in Sector Alpha
            var pathDeltaToPlayer = new NavMeshPath();
            bool hasPathDelta = NavMesh.CalculatePath(hitDelta.position, player.transform.position, NavMesh.AllAreas, pathDeltaToPlayer);
            Assert.IsTrue(hasPathDelta && pathDeltaToPlayer.status == NavMeshPathStatus.PathComplete, "NavMesh path from Sector Delta to Player in Sector Alpha must be PathComplete.");
        }

        [Test, Order(6)]
        public void Scene_DayNightCycle_StylizedSkyboxMaterialsConfigured()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var dayNightManager = Object.FindFirstObjectByType<DayNightCycleManager>();
            Assert.IsNotNull(dayNightManager, "DayNightCycleManager must exist in scene.");

            Assert.IsNotNull(dayNightManager.DawnSkyboxMaterial, "DawnSkyboxMaterial must be assigned.");
            Assert.IsNotNull(dayNightManager.DaySkyboxMaterial, "DaySkyboxMaterial must be assigned.");
            Assert.IsNotNull(dayNightManager.DuskSkyboxMaterial, "DuskSkyboxMaterial must be assigned.");
            Assert.IsNotNull(dayNightManager.NightSkyboxMaterial, "NightSkyboxMaterial must be assigned.");

            Assert.IsTrue(dayNightManager.DawnSkyboxMaterial.name.Contains("Sunrise") || dayNightManager.DawnSkyboxMaterial.name.Contains("Dawn"), "DawnSkyboxMaterial must reference a Sunrise/Dawn skybox material.");
            Assert.IsTrue(dayNightManager.DaySkyboxMaterial.name.Contains("Day"), "DaySkyboxMaterial must reference a Day skybox material.");
            Assert.IsTrue(dayNightManager.DuskSkyboxMaterial.name.Contains("Sunset") || dayNightManager.DuskSkyboxMaterial.name.Contains("Dusk"), "DuskSkyboxMaterial must reference a Sunset/Dusk skybox material.");
            Assert.IsTrue(dayNightManager.NightSkyboxMaterial.name.Contains("Night"), "NightSkyboxMaterial must reference a Night skybox material.");
        }

        [Test, Order(7)]
        public void DayNightCycle_SkyboxTransitions_SwapsWithDiurnalPhases()
        {
            var originalSkybox = RenderSettings.skybox;
            try
            {
                var go = new GameObject("DayNightCycle_Test");
                var manager = go.AddComponent<DayNightCycleManager>();

                Material baseMat = new Material(Shader.Find("Skybox/Panoramic") ?? Shader.Find("Skybox/6 Sided") ?? Shader.Find("Standard"));
                Material dawnMat = new Material(baseMat) { name = "Test_DawnSky" };
                Material dayMat = new Material(baseMat) { name = "Test_DaySky" };
                Material duskMat = new Material(baseMat) { name = "Test_DuskSky" };
                Material nightMat = new Material(baseMat) { name = "Test_NightSky" };

                manager.DawnSkyboxMaterial = dawnMat;
                manager.DaySkyboxMaterial = dayMat;
                manager.DuskSkyboxMaterial = duskMat;
                manager.NightSkyboxMaterial = nightMat;

                // Test Dawn phase (0.05f = Dawn)
                manager.SetTime(0.05f);
                Assert.AreEqual(DayPhase.Dawn, manager.CurrentPhase);
                Assert.AreEqual(dawnMat, manager.GetSkyboxForPhase(DayPhase.Dawn));
                Assert.AreEqual(dawnMat, RenderSettings.skybox);

                // Test Day phase (0.25f = Noon)
                manager.SetTime(0.25f);
                Assert.AreEqual(DayPhase.Day, manager.CurrentPhase);
                Assert.AreEqual(dayMat, manager.GetSkyboxForPhase(DayPhase.Day));
                Assert.AreEqual(dayMat, RenderSettings.skybox);

                // Test Dusk phase (0.55f = Dusk)
                manager.SetTime(0.55f);
                Assert.AreEqual(DayPhase.Dusk, manager.CurrentPhase);
                Assert.AreEqual(duskMat, manager.GetSkyboxForPhase(DayPhase.Dusk));
                Assert.AreEqual(duskMat, RenderSettings.skybox);

                // Test Night phase (0.75f = Midnight)
                manager.SetTime(0.75f);
                Assert.AreEqual(DayPhase.Night, manager.CurrentPhase);
                Assert.AreEqual(nightMat, manager.GetSkyboxForPhase(DayPhase.Night));
                Assert.AreEqual(nightMat, RenderSettings.skybox);

                Object.DestroyImmediate(go);
                Object.DestroyImmediate(baseMat);
                Object.DestroyImmediate(dawnMat);
                Object.DestroyImmediate(dayMat);
                Object.DestroyImmediate(duskMat);
                Object.DestroyImmediate(nightMat);
            }
            finally
            {
                RenderSettings.skybox = originalSkybox;
            }
        }
    }
}
#endif
