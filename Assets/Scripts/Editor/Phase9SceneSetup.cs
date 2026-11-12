#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Outpost404.Player;
using Outpost404.Survival;
using Outpost404.Combat;
using Outpost404.Inventory;
using Outpost404.Crafting;
using Outpost404.Building;
using Outpost404.Harvesting;
using Outpost404.AI;
using Outpost404.Environment;
using Outpost404.Audio;
using Outpost404.CameraControl;
using Outpost404.Objective;
using Outpost404.Persistence;
using Outpost404.UI;

namespace Outpost404.EditorTools
{
    public static class Phase9SceneSetup
    {
        [MenuItem("Outpost 404/Setup Full Game Scene (Phase 9 - Objective, Expanded Map & Persistence)", false, 12)]
        public static void BuildPhase9Scene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Phase8SceneSetup.BuildPhase8Scene();
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<AudioManager>() == null)
            {
                Phase8SceneSetup.BuildPhase8Scene();
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Load Staged 3D Models and Prefabs
            GameObject astronautModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/OrbitalAstronaut.glb");
            GameObject beastModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienPredatorBeast.obj");
            GameObject treeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienTimberTree.glb");
            GameObject shrubModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienOrganicShrub.glb");
            GameObject shrubAltModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienOrganicShrubAlt.glb");
            GameObject rockModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/MineralBoulder.glb");
            GameObject oreModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/ConductiveOre.glb");
            GameObject hullModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SalvagedMetallicHull.obj");
            GameObject meatModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienBeastMeat.glb");
            GameObject rationsModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmergencyRationContainer.glb");
            GameObject logsModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/HarvestedTimberLogs.glb");

            GameObject barricadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Prefab_Barricade.prefab");
            GameObject heatBeaconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Prefab_HeatBeacon.prefab");

            // 2. Load Item & Node Data ScriptableObjects
            var itemWood = AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/ScriptableObjects/Items/Item_Wood.asset");
            var itemStone = AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/ScriptableObjects/Items/Item_Stone.asset");
            var itemScrap = AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/ScriptableObjects/Items/Item_Scrap.asset");
            var itemRawMeat = AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/ScriptableObjects/Items/Item_RawMeat.asset");
            var itemRations = AssetDatabase.LoadAssetAtPath<ItemDataSO>("Assets/ScriptableObjects/Items/Item_Rations.asset");

            var nodeTree = AssetDatabase.LoadAssetAtPath<ResourceNodeSO>("Assets/ScriptableObjects/Nodes/Node_AlienTree.asset");
            var nodeRock = AssetDatabase.LoadAssetAtPath<ResourceNodeSO>("Assets/ScriptableObjects/Nodes/Node_PulseRock.asset");
            var enemyStats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>("Assets/ScriptableObjects/Enemies/Enemy_AnomalyProwler.asset");

            // 3. Materials
            Material groundCrashMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Ground_Crash.mat");
            Material groundOvergrowthMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Ground_Overgrowth.mat");
            Material groundCrystalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Ground_Crystal.mat");
            Material groundDeltaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_PulseRock.mat");
            Material wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Wall.mat");
            Material enemyProwlerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_EnemyProwler.mat");

            // 4. Critical 3D Visual Fix: Player Configuration
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                player.tag = "Player";
            }

            var col = player.GetComponent<CapsuleCollider>();
            if (col == null) col = player.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.radius = 0.5f;
            col.height = 2.0f;

            // Strip any primitive mesh renderer on player root
            var pMr = player.GetComponent<MeshRenderer>();
            if (pMr != null) Object.DestroyImmediate(pMr);
            var pMf = player.GetComponent<MeshFilter>();
            if (pMf != null) Object.DestroyImmediate(pMf);

            // Disable / Remove primitive orange capsule and visor GameObject
            Transform primitiveVis = player.transform.Find("Visual");
            if (primitiveVis != null) Object.DestroyImmediate(primitiveVis.gameObject);

            // Position player at Crash Site in Sector Alpha
            player.transform.position = new Vector3(-30f, 0.05f, -20f);
            player.transform.rotation = Quaternion.identity;

            // Configure authentic 3D OrbitalAstronaut visual
            Transform existingAstronaut = player.transform.Find("Visual_OrbitalAstronaut");
            if (existingAstronaut != null) Object.DestroyImmediate(existingAstronaut.gameObject);

            if (astronautModel != null)
            {
                GameObject astronautVis = Object.Instantiate(astronautModel);
                astronautVis.name = "Visual_OrbitalAstronaut";
                astronautVis.transform.SetParent(player.transform, false);
                // Elevate pivot by 0.95m so astronaut's feet align precisely with ground
                astronautVis.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                astronautVis.transform.localRotation = Quaternion.identity;
                astronautVis.transform.localScale = new Vector3(1.9f, 1.9f, 1.9f);

                var childCols = astronautVis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }

            var pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl == null) pCtrl = player.AddComponent<PlayerController>();
            pCtrl.ConfigureRigidbody();

            var pStats = player.GetComponent<PlayerStats>();
            if (pStats == null) pStats = player.AddComponent<PlayerStats>();
            pStats.InitializeStats();

            var pInv = player.GetComponent<InventorySystem>();
            if (pInv == null) pInv = player.AddComponent<InventorySystem>();
            pInv.InitializeSlots();

            var pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat == null) pCombat = player.AddComponent<PlayerCombat>();

            var pPlacer = player.GetComponent<StructurePlacer>();
            if (pPlacer == null) pPlacer = player.AddComponent<StructurePlacer>();

            pCombat.TargetStructurePlacer = pPlacer;

            // 5. Build Expansive 120m x 120m Map with 4 Biome Sectors
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");
            while (envRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(envRoot.transform.GetChild(0).gameObject);
            }

            var oldGround = GameObject.Find("Ground");
            if (oldGround != null && oldGround.transform.parent != envRoot.transform) Object.DestroyImmediate(oldGround);

            // 5a. 4 Biome Sector Ground Zones (120m x 120m total)
            CreateGroundZone(envRoot.transform, "Ground_SectorAlpha_CrashZone", new Vector3(-30f, -0.5f, -30f), new Vector3(60f, 1f, 60f), groundCrashMat);
            CreateGroundZone(envRoot.transform, "Ground_SectorBeta_Forest", new Vector3(-30f, -0.5f, 30f), new Vector3(60f, 1f, 60f), groundOvergrowthMat);
            CreateGroundZone(envRoot.transform, "Ground_SectorGamma_CrystallineBasin", new Vector3(30f, -0.5f, 30f), new Vector3(60f, 1f, 60f), groundCrystalMat);
            CreateGroundZone(envRoot.transform, "Ground_SectorDelta_AnomalyHive", new Vector3(30f, -0.5f, -30f), new Vector3(60f, 1f, 60f), groundDeltaMat);

            // 5b. Perimeter Walls enclosing 120m x 120m territory (arenaHalf = 60m)
            float arenaHalf = 60f;
            float wallThickness = 1.6f;
            float wallHeight = 5.0f;
            CreateWall(envRoot.transform, "Wall_North", new Vector3(0f, wallHeight * 0.5f, arenaHalf), new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
            CreateWall(envRoot.transform, "Wall_South", new Vector3(0f, wallHeight * 0.5f, -arenaHalf), new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
            CreateWall(envRoot.transform, "Wall_East", new Vector3(arenaHalf, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaHalf * 2f), wallMat);
            CreateWall(envRoot.transform, "Wall_West", new Vector3(-arenaHalf, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaHalf * 2f), wallMat);

            // 5c. Sector Alpha: Crash Site Enclave, Crashed Pod Wreckage, and SOS Beacon Terminal
            GameObject podWreckage = Object.Instantiate(hullModel, envRoot.transform);
            podWreckage.name = "Pod_Wreckage_Crashed";
            podWreckage.transform.position = new Vector3(-25f, 0.4f, -24f);
            podWreckage.transform.rotation = Quaternion.Euler(14f, 42f, -8f);
            podWreckage.transform.localScale = new Vector3(3.5f, 2.8f, 3.5f);
            var podCols = podWreckage.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < podCols.Length; i++) Object.DestroyImmediate(podCols[i]);
            var podBox = podWreckage.AddComponent<BoxCollider>();
            podBox.size = new Vector3(3f, 2f, 3f);
            var podObstacle = podWreckage.AddComponent<NavMeshObstacle>();
            podObstacle.carving = true;
            podObstacle.size = new Vector3(3.2f, 2.5f, 3.2f);

            // Central SOS Extraction Beacon in Sector Alpha
            GameObject beaconObj = GameObject.Find("Beacon_SOS_Terminal");
            if (beaconObj != null) Object.DestroyImmediate(beaconObj);
            beaconObj = new GameObject("Beacon_SOS_Terminal");
            beaconObj.transform.SetParent(envRoot.transform, false);
            beaconObj.transform.position = new Vector3(-30f, 0f, -30f);

            // 3D Visual for Beacon Terminal
            GameObject beaconVis = new GameObject("Visual_BeaconTerminal");
            beaconVis.transform.SetParent(beaconObj.transform, false);
            beaconVis.transform.localPosition = Vector3.zero;

            // Beacon Base Platform
            GameObject beaconBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconBase.name = "BeaconBase";
            beaconBase.transform.SetParent(beaconVis.transform, false);
            beaconBase.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            beaconBase.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
            var baseCol = beaconBase.GetComponent<Collider>();
            if (baseCol != null) Object.DestroyImmediate(baseCol);

            // Beacon Transmitter Mast
            GameObject beaconMast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconMast.name = "BeaconMast";
            beaconMast.transform.SetParent(beaconVis.transform, false);
            beaconMast.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            beaconMast.transform.localScale = new Vector3(0.4f, 1.6f, 0.4f);
            var mastCol = beaconMast.GetComponent<Collider>();
            if (mastCol != null) Object.DestroyImmediate(mastCol);

            // Beacon Dish Array Head
            GameObject beaconDish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beaconDish.name = "BeaconDish";
            beaconDish.transform.SetParent(beaconVis.transform, false);
            beaconDish.transform.localPosition = new Vector3(0f, 3.4f, 0f);
            beaconDish.transform.localScale = new Vector3(1.2f, 0.5f, 1.2f);
            var dishCol = beaconDish.GetComponent<Collider>();
            if (dishCol != null) Object.DestroyImmediate(dishCol);

            // Beacon Glowing Signal Light
            GameObject beaconLightObj = new GameObject("BeaconLight");
            beaconLightObj.transform.SetParent(beaconVis.transform, false);
            beaconLightObj.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            var beaconLight = beaconLightObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(0.2f, 0.85f, 1.0f);
            beaconLight.range = 14f;
            beaconLight.intensity = 2.5f;

            // Beacon Physics & Trigger Collider
            var beaconCol = CylinderOrBoxCollider(beaconObj);
            var beaconObstacle = beaconObj.AddComponent<NavMeshObstacle>();
            beaconObstacle.carving = true;
            beaconObstacle.size = new Vector3(2.8f, 3.5f, 2.8f);

            var beaconLogic = beaconObj.AddComponent<BeaconObjective>();
            beaconLogic.EnsureDefaultStages();
            var stageList = beaconLogic.Stages;
            if (stageList.Count >= 3)
            {
                // Stage 1: Power Matrix (4x Conductive Ore + 3x Salvaged Scrap)
                stageList[0].primaryItem = itemStone;
                stageList[0].primaryRequired = 4;
                stageList[0].secondaryItem = itemScrap;
                stageList[0].secondaryRequired = 3;

                // Stage 2: Signal Dish Array (6x Composite Timber + 4x Conductive Ore)
                stageList[1].primaryItem = itemWood;
                stageList[1].primaryRequired = 6;
                stageList[1].secondaryItem = itemStone;
                stageList[1].secondaryRequired = 4;

                // Stage 3: Activation Core (3x Alien Beast Meat + 5x Salvaged Scrap)
                stageList[2].primaryItem = itemRawMeat;
                stageList[2].primaryRequired = 3;
                stageList[2].secondaryItem = itemScrap;
                stageList[2].secondaryRequired = 5;
            }

            // Horde Spawn Points for Extraction Surge Defense around Sector Alpha beacon
            GameObject hordeSpawnRoot = GameObject.Find("HordeSpawnPoints");
            if (hordeSpawnRoot != null) Object.DestroyImmediate(hordeSpawnRoot);
            hordeSpawnRoot = new GameObject("HordeSpawnPoints");
            hordeSpawnRoot.transform.SetParent(envRoot.transform, false);

            List<Transform> hordeSpawnPoints = new List<Transform>();
            Vector3[] hordePositions = new Vector3[]
            {
                new Vector3(-30f, 0.05f, -8f),   // North
                new Vector3(-30f, 0.05f, -52f),  // South
                new Vector3(-8f, 0.05f, -30f),   // East
                new Vector3(-52f, 0.05f, -30f)   // West
            };
            string[] hordePointNames = new string[] { "SpawnPoint_Horde_North", "SpawnPoint_Horde_South", "SpawnPoint_Horde_East", "SpawnPoint_Horde_West" };
            for (int h = 0; h < hordePositions.Length; h++)
            {
                GameObject sp = new GameObject(hordePointNames[h]);
                sp.transform.SetParent(hordeSpawnRoot.transform, false);
                sp.transform.position = hordePositions[h];
                hordeSpawnPoints.Add(sp.transform);
            }

            // Scrap Metal and Starter Pickups around Sector Alpha
            Create3DPickup(envRoot.transform, "Pickup_Scrap_1", new Vector3(-27f, 0.2f, -18f), itemScrap, 4, hullModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_Scrap_2", new Vector3(-33f, 0.2f, -22f), itemScrap, 3, hullModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_Rations_1", new Vector3(-28f, 0.2f, -19f), itemRations, 2, rationsModel, new Vector3(0.8f, 0.8f, 0.8f));
            Create3DPickup(envRoot.transform, "Pickup_Timber_1", new Vector3(-32f, 0.2f, -25f), itemWood, 4, logsModel, new Vector3(0.8f, 0.8f, 0.8f));
            Create3DPickup(envRoot.transform, "Pickup_ConductiveOre_1", new Vector3(-26f, 0.2f, -28f), itemStone, 3, oreModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_BeastMeat_1", new Vector3(-35f, 0.2f, -21f), itemRawMeat, 2, meatModel, new Vector3(0.75f, 0.75f, 0.75f));

            // 5d. Sector Beta: Deep Timber Canopy Forest (Dense clusters of 30 AlienTimberTree & 16 Shrub nodes)
            // Preserve Tree_Alien_1 for backward compatibility
            Create3DTree(envRoot.transform, "Tree_Alien_1", new Vector3(-16f, 0f, 16f), treeModel, nodeTree);
            Create3DShrub(envRoot.transform, "Shrub_Alien_1", new Vector3(-14f, 0f, 13f), shrubModel);

            Vector3[] betaTreePositions = GenerateGridClusterPositions(-52f, -8f, 8f, 52f, 29, 3217);
            for (int i = 0; i < betaTreePositions.Length; i++)
            {
                Create3DTree(envRoot.transform, $"Tree_Alien_{i + 2}", betaTreePositions[i], treeModel, nodeTree);
            }

            Vector3[] betaShrubPositions = GenerateGridClusterPositions(-50f, -10f, 10f, 50f, 15, 8821);
            for (int i = 0; i < betaShrubPositions.Length; i++)
            {
                GameObject sPrefab = (i % 2 == 0) ? shrubModel : shrubAltModel;
                Create3DShrub(envRoot.transform, $"Shrub_Alien_{i + 2}", betaShrubPositions[i], sPrefab);
            }

            // 5e. Sector Gamma: Crystalline Basin & Mineral Quarry (30 MineralBoulder & Ore nodes)
            // Preserve Rock_Pulse_1 for backward compatibility
            Create3DRock(envRoot.transform, "Rock_Pulse_1", new Vector3(15f, 0f, 15f), rockModel, nodeRock);

            Vector3[] gammaRockPositions = GenerateGridClusterPositions(8f, 52f, 8f, 52f, 29, 9941);
            for (int i = 0; i < gammaRockPositions.Length; i++)
            {
                Create3DRock(envRoot.transform, $"Rock_Pulse_{i + 2}", gammaRockPositions[i], rockModel, nodeRock);
            }

            Vector3[] gammaOrePositions = GenerateGridClusterPositions(12f, 48f, 12f, 48f, 8, 4433);
            for (int i = 0; i < gammaOrePositions.Length; i++)
            {
                Create3DPickup(envRoot.transform, $"Pickup_ConductiveOre_{i + 2}", gammaOrePositions[i] + Vector3.up * 0.2f, itemStone, 2, oreModel, new Vector3(0.7f, 0.7f, 0.7f));
            }

            // 5f. Sector Delta: Anomaly Hive & Prowler Hunting Grounds (Roaming prowlers & high-value caches)
            GameObject enemiesRoot = GameObject.Find("Enemies");
            if (enemiesRoot == null) enemiesRoot = new GameObject("Enemies");
            while (enemiesRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(enemiesRoot.transform.GetChild(0).gameObject);
            }

            Vector3[] deltaProwlerPositions = new Vector3[]
            {
                new Vector3(18f, 0.05f, -16f), // Alpha
                new Vector3(32f, 0.05f, -22f), // Beta
                new Vector3(44f, 0.05f, -34f), // Gamma
                new Vector3(24f, 0.05f, -42f), // Delta
                new Vector3(38f, 0.05f, -14f)  // Epsilon
            };

            string[] prowlerNames = new string[] { "Enemy_Prowler_Alpha", "Enemy_Prowler_Beta", "Enemy_Prowler_Gamma", "Enemy_Prowler_Delta", "Enemy_Prowler_Epsilon" };
            for (int i = 0; i < prowlerNames.Length; i++)
            {
                Create3DEnemy(enemiesRoot.transform, prowlerNames[i], deltaProwlerPositions[i], beastModel, enemyStats, enemyProwlerMat);
            }

            // Save configured prowler as Prefab_AlienProwler for extraction surge horde spawning
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");
            }
            GameObject sampleEnemy = enemiesRoot.transform.Find("Enemy_Prowler_Alpha")?.gameObject;
            GameObject prowlerPrefab = null;
            if (sampleEnemy != null)
            {
                prowlerPrefab = PrefabUtility.SaveAsPrefabAsset(sampleEnemy, "Assets/Prefabs/Enemies/Prefab_AlienProwler.prefab");
            }
            if (prowlerPrefab == null)
            {
                prowlerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Prefab_AlienProwler.prefab");
            }

            // Hook up Horde config (prefab & Sector Alpha perimeter spawn points) to BeaconObjective
            beaconLogic.SetHordeConfig(prowlerPrefab, hordeSpawnPoints);

            // High-value Delta loot caches
            Create3DPickup(envRoot.transform, "Pickup_BeastMeat_2", new Vector3(28f, 0.2f, -28f), itemRawMeat, 3, meatModel, new Vector3(0.75f, 0.75f, 0.75f));
            Create3DPickup(envRoot.transform, "Pickup_BeastMeat_3", new Vector3(42f, 0.2f, -36f), itemRawMeat, 2, meatModel, new Vector3(0.75f, 0.75f, 0.75f));
            Create3DPickup(envRoot.transform, "Pickup_Scrap_3", new Vector3(36f, 0.2f, -40f), itemScrap, 5, hullModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_Rations_2", new Vector3(22f, 0.2f, -38f), itemRations, 3, rationsModel, new Vector3(0.8f, 0.8f, 0.8f));

            // 6. Camera Follow Setup
            Camera cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<TopDownCameraFollow>();
                if (follow != null)
                {
                    follow.Target = player.transform;
                    follow.SnapToTarget();
                }
            }

            // 7. Setup Mission Statistics Singleton
            GameObject statsObj = GameObject.Find("MissionStatistics");
            if (statsObj == null)
            {
                statsObj = new GameObject("MissionStatistics");
            }
            var missionStats = statsObj.GetComponent<MissionStatistics>();
            if (missionStats == null) missionStats = statsObj.AddComponent<MissionStatistics>();

            // 8. Setup HUD Canvas with Beacon Terminal, Countdown Banner & Mission End Screens
            SetupPhase9HUDCanvas(beaconLogic, pInv, pStats, missionStats);

            // 9. Setup SaveSystem
            GameObject saveSystemObj = GameObject.Find("SaveSystem");
            if (saveSystemObj == null)
            {
                saveSystemObj = new GameObject("SaveSystem");
            }
            var saveSys = saveSystemObj.GetComponent<SaveSystem>();
            if (saveSys == null) saveSys = saveSystemObj.AddComponent<SaveSystem>();

            saveSys.BarricadePrefab = barricadePrefab;
            saveSys.HeatBeaconPrefab = heatBeaconPrefab;
            saveSys.PopulateItemCatalog();

            // Wire toast UI
            var hudCanvas = GameObject.Find("HUD_Canvas");
            if (hudCanvas != null)
            {
                var toastRoot = hudCanvas.transform.Find("SaveToastPanel");
                if (toastRoot != null)
                {
                    var toastField = typeof(SaveSystem).GetField("toastRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (toastField != null) toastField.SetValue(saveSys, toastRoot.gameObject);

                    var toastText = toastRoot.GetComponentInChildren<Text>();
                    var textField = typeof(SaveSystem).GetField("toastText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (textField != null) textField.SetValue(saveSys, toastText);
                }
            }

            // 10. Re-bake NavMesh across the entire 120m x 120m expanded terrain
            BakeNavMesh();

            // 11. Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> Phase 9 Scene Setup (Win/Loss Objective, 120m Map Expansion & Persistence) successfully completed!");
        }

        private static void CreateGroundZone(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = name;
            ground.transform.SetParent(parent);
            ground.transform.position = pos;
            ground.transform.localScale = size;
            if (mat != null) ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = size;
            if (mat != null) wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        }

        private static void Create3DTree(Transform parent, string name, Vector3 pos, GameObject treeModel, ResourceNodeSO nodeSO)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent);
            tree.transform.position = pos;

            if (treeModel != null)
            {
                GameObject vis = Object.Instantiate(treeModel);
                vis.name = "Visual_AlienTimberTree";
                vis.transform.SetParent(tree.transform, false);
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localScale = new Vector3(6f, 8f, 6f);
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }

            var col = tree.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 2f, 0f);
            col.radius = 1.0f;
            col.height = 4.2f;

            var rNode = tree.AddComponent<ResourceNode>();
            rNode.Configure(nodeSO);

            var navObstacle = tree.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.size = new Vector3(1.8f, 4f, 1.8f);
        }

        private static void Create3DShrub(Transform parent, string name, Vector3 pos, GameObject shrubModel)
        {
            if (shrubModel == null) return;
            GameObject shrub = new GameObject(name);
            shrub.transform.SetParent(parent);
            shrub.transform.position = pos;

            GameObject vis = Object.Instantiate(shrubModel);
            vis.name = "Visual_Shrub";
            vis.transform.SetParent(shrub.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            var childCols = vis.GetComponentsInChildren<Collider>(true);
            for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);

            var col = shrub.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.radius = 0.8f;
        }

        private static void Create3DRock(Transform parent, string name, Vector3 pos, GameObject rockModel, ResourceNodeSO nodeSO)
        {
            GameObject rock = new GameObject(name);
            rock.transform.SetParent(parent);
            rock.transform.position = pos;

            if (rockModel != null)
            {
                GameObject vis = Object.Instantiate(rockModel);
                vis.name = "Visual_MineralBoulder";
                vis.transform.SetParent(rock.transform, false);
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }

            var col = rock.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.size = new Vector3(2.2f, 1.6f, 2.2f);

            var rNode = rock.AddComponent<ResourceNode>();
            rNode.Configure(nodeSO);

            var navObstacle = rock.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.size = new Vector3(2.4f, 2f, 2.4f);
        }

        private static void Create3DPickup(Transform parent, string name, Vector3 pos, ItemDataSO item, int count, GameObject modelPrefab, Vector3 scale)
        {
            GameObject pickupObj = new GameObject(name);
            pickupObj.transform.SetParent(parent);
            pickupObj.transform.position = pos;

            if (modelPrefab != null)
            {
                GameObject vis = Object.Instantiate(modelPrefab);
                vis.name = "Visual_Pickup";
                vis.transform.SetParent(pickupObj.transform, false);
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localScale = scale;
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }

            var col = pickupObj.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.85f;

            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(item, count);
        }

        private static GameObject Create3DEnemy(Transform parent, string name, Vector3 pos, GameObject beastModel, EnemyStatsSO stats, Material prowlerMat = null)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent);
            enemy.transform.position = pos;

            if (beastModel != null)
            {
                GameObject vis = Object.Instantiate(beastModel);
                vis.name = "Visual_AlienPredatorBeast";
                vis.transform.SetParent(enemy.transform, false);
                // Elevate by 0.92m so paws touch the ground cleanly (scale 4.8 yields 1.82m authentic predator height), facing +Z forward
                vis.transform.localPosition = new Vector3(0f, 0.92f, 0f);
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = new Vector3(4.8f, 4.8f, 4.8f);
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);

                if (prowlerMat != null)
                {
                    var rends = vis.GetComponentsInChildren<Renderer>(true);
                    for (int r = 0; r < rends.Length; r++)
                    {
                        rends[r].sharedMaterial = prowlerMat;
                    }
                }
            }

            var col = enemy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.75f;
            col.height = 1.9f;

            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = stats != null ? stats.PatrolSpeed : 2.8f;
            agent.angularSpeed = 480f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 1.5f;

            var ctrl = enemy.AddComponent<EnemyController>();
            ctrl.Configure(stats);

            return enemy;
        }

        private static Vector3[] GenerateGridClusterPositions(float minX, float maxX, float minZ, float maxZ, int count, int seed)
        {
            var rand = new System.Random(seed);
            List<Vector3> list = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                float x = (float)(minX + rand.NextDouble() * (maxX - minX));
                float z = (float)(minZ + rand.NextDouble() * (maxZ - minZ));
                list.Add(new Vector3(x, 0f, z));
            }
            return list.ToArray();
        }

        private static void SetupPhase9HUDCanvas(BeaconObjective beacon, InventorySystem inv, PlayerStats stats, MissionStatistics missionStats)
        {
            var canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj == null) return;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Beacon Terminal UI Panel
            Transform existingTerminal = canvasObj.transform.Find("Beacon_Terminal_Panel");
            if (existingTerminal != null) Object.DestroyImmediate(existingTerminal.gameObject);

            GameObject termPanelObj = new GameObject("Beacon_Terminal_Panel");
            termPanelObj.transform.SetParent(canvasObj.transform, false);
            var termRt = termPanelObj.AddComponent<RectTransform>();
            termRt.anchorMin = new Vector2(0.5f, 0.5f);
            termRt.anchorMax = new Vector2(0.5f, 0.5f);
            termRt.pivot = new Vector2(0.5f, 0.5f);
            termRt.anchoredPosition = new Vector2(0f, 20f);
            termRt.sizeDelta = new Vector2(460f, 380f);

            var termBg = termPanelObj.AddComponent<Image>();
            termBg.color = new Color(0.08f, 0.12f, 0.16f, 0.94f);

            // Title Header
            GameObject termHeader = CreateUIText(termPanelObj.transform, "Header", "SOS EXTRACTION BEACON", defaultFont, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(420f, 40f), new Color(0.3f, 0.9f, 1.0f));
            GameObject stageTitle = CreateUIText(termPanelObj.transform, "StageTitle", "Stage 1: Power Matrix", defaultFont, 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0f, 105f), new Vector2(400f, 30f), Color.white);
            GameObject stageDesc = CreateUIText(termPanelObj.transform, "StageDescription", "Restore backup power cells.", defaultFont, 13, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0f, 65f), new Vector2(400f, 40f), new Color(0.8f, 0.85f, 0.9f));
            GameObject reqItems = CreateUIText(termPanelObj.transform, "RequiredItems", "- Conductive Ore: 0/4\n- Titanium Scrap: 0/3", defaultFont, 14, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(400f, 60f), Color.white);

            // Progress Slider
            Slider progressSlider = CreateProgressBar(termPanelObj.transform, "ProgressBar", new Vector2(0f, -50f), new Vector2(380f, 20f), new Color(0.2f, 0.8f, 0.35f));
            GameObject progressText = CreateUIText(termPanelObj.transform, "ProgressPercent", "0%", defaultFont, 12, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -50f), new Vector2(80f, 20f), Color.white);

            // Buttons
            Button repairBtn = CreateUIButton(termPanelObj.transform, "RepairButton", "REPAIR STAGE", defaultFont, 14, new Vector2(0f, -105f), new Vector2(240f, 42f), new Color(0.18f, 0.55f, 0.85f));
            Button activateBtn = CreateUIButton(termPanelObj.transform, "ActivateButton", "ACTIVATE SOS EXTRACTION", defaultFont, 14, new Vector2(0f, -105f), new Vector2(280f, 42f), new Color(0.85f, 0.45f, 0.1f));
            activateBtn.gameObject.SetActive(false);

            // 2. Extraction Countdown Banner
            Transform existingCountdown = canvasObj.transform.Find("ExtractionCountdownBanner");
            if (existingCountdown != null) Object.DestroyImmediate(existingCountdown.gameObject);

            GameObject countdownObj = new GameObject("ExtractionCountdownBanner");
            countdownObj.transform.SetParent(canvasObj.transform, false);
            var cdRt = countdownObj.AddComponent<RectTransform>();
            cdRt.anchorMin = new Vector2(0.5f, 1f);
            cdRt.anchorMax = new Vector2(0.5f, 1f);
            cdRt.pivot = new Vector2(0.5f, 1f);
            cdRt.anchoredPosition = new Vector2(0f, -70f);
            cdRt.sizeDelta = new Vector2(400f, 55f);

            var cdBg = countdownObj.AddComponent<Image>();
            cdBg.color = new Color(0.12f, 0.04f, 0.04f, 0.90f);

            GameObject cdText = CreateUIText(countdownObj.transform, "TimerText", "EXTRACTION DROPSHIP INCOMING: 60s", defaultFont, 15, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 10f), new Vector2(380f, 26f), new Color(1f, 0.6f, 0.2f));
            Slider cdSlider = CreateProgressBar(countdownObj.transform, "CountdownBar", new Vector2(0f, -14f), new Vector2(360f, 12f), new Color(1f, 0.35f, 0.15f));
            countdownObj.SetActive(false);

            // Wire BeaconTerminalUI component
            var termUI = canvasObj.GetComponent<BeaconTerminalUI>();
            if (termUI == null) termUI = canvasObj.AddComponent<BeaconTerminalUI>();

            SetPrivateField(termUI, "targetBeacon", beacon);
            SetPrivateField(termUI, "playerInventory", inv);
            SetPrivateField(termUI, "terminalPanel", termPanelObj);
            SetPrivateField(termUI, "stageTitleText", stageTitle.GetComponent<Text>());
            SetPrivateField(termUI, "stageDescriptionText", stageDesc.GetComponent<Text>());
            SetPrivateField(termUI, "requiredItemsText", reqItems.GetComponent<Text>());
            SetPrivateField(termUI, "stageProgressBar", progressSlider);
            SetPrivateField(termUI, "progressPercentText", progressText.GetComponent<Text>());
            SetPrivateField(termUI, "repairButton", repairBtn);
            SetPrivateField(termUI, "repairButtonText", repairBtn.GetComponentInChildren<Text>());
            SetPrivateField(termUI, "activateExtractionButton", activateBtn);
            SetPrivateField(termUI, "activateButtonText", activateBtn.GetComponentInChildren<Text>());
            SetPrivateField(termUI, "countdownBanner", countdownObj);
            SetPrivateField(termUI, "countdownTimerText", cdText.GetComponent<Text>());
            SetPrivateField(termUI, "countdownProgressBar", cdSlider);

            termPanelObj.SetActive(false);

            // 3. Victory Panel
            Transform existingVic = canvasObj.transform.Find("VictoryPanel");
            if (existingVic != null) Object.DestroyImmediate(existingVic.gameObject);

            GameObject vicObj = new GameObject("VictoryPanel");
            vicObj.transform.SetParent(canvasObj.transform, false);
            var vicRt = vicObj.AddComponent<RectTransform>();
            vicRt.anchorMin = Vector2.zero;
            vicRt.anchorMax = Vector2.one;
            vicRt.sizeDelta = Vector2.zero;

            var vicBg = vicObj.AddComponent<Image>();
            vicBg.color = new Color(0.04f, 0.12f, 0.08f, 0.95f);

            GameObject vicTitle = CreateUIText(vicObj.transform, "Title", "MISSION ACCOMPLISHED\nEXTRACTION SUCCESSFUL", defaultFont, 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(600f, 70f), new Color(0.35f, 1f, 0.5f));
            GameObject vicSub = CreateUIText(vicObj.transform, "Subtitle", "Orbital dropship successfully retrieved Dr. Nolan Cross from Outpost 404.", defaultFont, 14, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 65f), new Vector2(600f, 30f), new Color(0.85f, 0.95f, 0.9f));
            GameObject vicStats = CreateUIText(vicObj.transform, "Stats", "EXPEDITION REPORT:\n\n- Days Survived: 1\n- Enemies Slain: 0\n- Resources Harvested: 0\n- Structures Built: 0", defaultFont, 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0f, -20f), new Vector2(360f, 120f), Color.white);
            Button vicBtn = CreateUIButton(vicObj.transform, "RestartButton", "RESTART EXPEDITION", defaultFont, 15, new Vector2(0f, -140f), new Vector2(240f, 44f), new Color(0.2f, 0.7f, 0.35f));
            vicObj.SetActive(false);

            // 4. Defeat Panel
            Transform existingDef = canvasObj.transform.Find("DefeatPanel");
            if (existingDef != null) Object.DestroyImmediate(existingDef.gameObject);

            GameObject defObj = new GameObject("DefeatPanel");
            defObj.transform.SetParent(canvasObj.transform, false);
            var defRt = defObj.AddComponent<RectTransform>();
            defRt.anchorMin = Vector2.zero;
            defRt.anchorMax = Vector2.one;
            defRt.sizeDelta = Vector2.zero;

            var defBg = defObj.AddComponent<Image>();
            defBg.color = new Color(0.14f, 0.04f, 0.04f, 0.95f);

            GameObject defTitle = CreateUIText(defObj.transform, "Title", "SIGNAL LOST\nVITALS CRITICAL", defaultFont, 24, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(600f, 70f), new Color(1f, 0.25f, 0.25f));
            GameObject defSub = CreateUIText(defObj.transform, "Subtitle", "Expedition life support terminated in Outpost 404.", defaultFont, 14, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 65f), new Vector2(600f, 30f), new Color(0.95f, 0.85f, 0.85f));
            GameObject defStats = CreateUIText(defObj.transform, "Stats", "EXPEDITION REPORT:\n\n- Days Survived: 1\n- Enemies Slain: 0\n- Resources Harvested: 0\n- Structures Built: 0", defaultFont, 16, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0f, -20f), new Vector2(360f, 120f), Color.white);
            Button defBtn = CreateUIButton(defObj.transform, "RestartButton", "RESTART EXPEDITION", defaultFont, 15, new Vector2(0f, -140f), new Vector2(240f, 44f), new Color(0.85f, 0.25f, 0.25f));
            defObj.SetActive(false);

            // Wire MissionEndUI component
            var endUI = canvasObj.GetComponent<MissionEndUI>();
            if (endUI == null) endUI = canvasObj.AddComponent<MissionEndUI>();

            SetPrivateField(endUI, "targetBeacon", beacon);
            SetPrivateField(endUI, "targetPlayerStats", stats);
            SetPrivateField(endUI, "targetStatistics", missionStats);
            SetPrivateField(endUI, "victoryPanel", vicObj);
            SetPrivateField(endUI, "victoryTitleText", vicTitle.GetComponent<Text>());
            SetPrivateField(endUI, "victorySubtitleText", vicSub.GetComponent<Text>());
            SetPrivateField(endUI, "victoryStatsText", vicStats.GetComponent<Text>());
            SetPrivateField(endUI, "victoryRestartButton", vicBtn);
            SetPrivateField(endUI, "defeatPanel", defObj);
            SetPrivateField(endUI, "defeatTitleText", defTitle.GetComponent<Text>());
            SetPrivateField(endUI, "defeatSubtitleText", defSub.GetComponent<Text>());
            SetPrivateField(endUI, "defeatStatsText", defStats.GetComponent<Text>());
            SetPrivateField(endUI, "defeatRestartButton", defBtn);

            // 5. Save Toast Notification Panel
            Transform existingToast = canvasObj.transform.Find("SaveToastPanel");
            if (existingToast != null) Object.DestroyImmediate(existingToast.gameObject);

            GameObject toastObj = new GameObject("SaveToastPanel");
            toastObj.transform.SetParent(canvasObj.transform, false);
            var toastRt = toastObj.AddComponent<RectTransform>();
            toastRt.anchorMin = new Vector2(0.5f, 0f);
            toastRt.anchorMax = new Vector2(0.5f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 40f);
            toastRt.sizeDelta = new Vector2(320f, 38f);

            var toastImg = toastObj.AddComponent<Image>();
            toastImg.color = new Color(0.1f, 0.15f, 0.2f, 0.88f);

            CreateUIText(toastObj.transform, "ToastText", "GAME QUICK-SAVED (F5)", defaultFont, 13, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(300f, 32f), new Color(0.4f, 0.9f, 1f));
            toastObj.SetActive(false);
        }

        private static GameObject CreateUIText(Transform parent, string name, string content, Font font, int size, FontStyle style, TextAnchor anchor, Vector2 pos, Vector2 delta, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;

            var txt = obj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = anchor;
            txt.text = content;
            txt.color = color;
            return obj;
        }

        private static Slider CreateProgressBar(Transform parent, string name, Vector2 pos, Vector2 delta, Color fillColor)
        {
            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);
            var rt = barObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;

            var bg = barObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barObj.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.sizeDelta = Vector2.zero;

            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            var slider = barObj.AddComponent<Slider>();
            slider.fillRect = fRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            return slider;
        }

        private static Button CreateUIButton(Transform parent, string name, string label, Font font, int fontSize, Vector2 pos, Vector2 delta, Color btnColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;

            var img = btnObj.AddComponent<Image>();
            img.color = btnColor;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            var txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = label;
            txt.color = Color.white;

            return btn;
        }

        private static BoxCollider CylinderOrBoxCollider(GameObject target)
        {
            var col = target.GetComponent<BoxCollider>();
            if (col == null) col = target.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.8f, 0f);
            col.size = new Vector3(3f, 3.6f, 3f);
            return col;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }

        private static void BakeNavMesh()
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("<color=green>[Outpost 404]</color> Expanded 120m Map NavMesh bake completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Outpost 404] NavMesh bake warning: {ex.Message}");
            }
        }
    }
}
#endif
