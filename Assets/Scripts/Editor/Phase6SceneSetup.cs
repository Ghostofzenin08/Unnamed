#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Outpost404.Player;
using Outpost404.CameraControl;
using Outpost404.Survival;
using Outpost404.Inventory;
using Outpost404.Harvesting;
using Outpost404.Combat;
using Outpost404.AI;
using Outpost404.Crafting;
using Outpost404.Building;
using Outpost404.UI;

namespace Outpost404.EditorTools
{
    public static class Phase6SceneSetup
    {
        [MenuItem("Outpost 404/Setup Full Game Scene (Phase 6 - Crafting & 3D Assets)", false, 10)]
        public static void BuildPhase6Scene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            EnsureDirectories();

            // 1. 3D Model Prefabs
            GameObject astronautModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/OrbitalAstronaut.glb");
            GameObject beastModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienPredatorBeast.obj");
            GameObject treeModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienTimberTree.glb");
            GameObject rockModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/MineralBoulder.glb");
            GameObject shrubModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienOrganicShrub.glb");
            GameObject shrubAltModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienOrganicShrubAlt.glb");
            GameObject hullModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SalvagedMetallicHull.obj");
            GameObject rationsModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmergencyRationContainer.glb");
            GameObject logsModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/HarvestedTimberLogs.glb");
            GameObject oreModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/ConductiveOre.glb");
            GameObject meatModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/AlienBeastMeat.glb");

            // 2. Hologram Materials
            Material validHoloMat = GetOrCreateHologramMaterial("Assets/Materials/M_Hologram_Valid.mat", new Color(0.1f, 0.95f, 0.25f, 0.42f), new Color(0.2f, 1f, 0.3f) * 0.5f);
            Material blockedHoloMat = GetOrCreateHologramMaterial("Assets/Materials/M_Hologram_Blocked.mat", new Color(0.95f, 0.15f, 0.15f, 0.45f), new Color(1f, 0.2f, 0.2f) * 0.6f);

            // 3. Structure Prefabs (Barricade & Heat Beacon)
            GameObject barricadePrefab = GetOrCreateBarricadePrefab(logsModel);
            GameObject heatBeaconPrefab = GetOrCreateHeatBeaconPrefab(oreModel);

            // 4. Items & ScriptableObjects
            var itemWood = GetOrCreateItem("Item_Wood", "Composite Timber", "Harvested structural alien wood.", ItemType.Resource, 64, 0f, 0f, 1.0f, null, new Color(0.62f, 0.42f, 0.22f));
            var itemStone = GetOrCreateItem("Item_Stone", "Conductive Ore", "Basalt ore laced with crystalline conduits.", ItemType.Resource, 64, 0f, 0f, 1.0f, null, new Color(0.18f, 0.75f, 0.88f));
            var itemRawMeat = GetOrCreateItem("Item_RawMeat", "Raw Anomaly Meat", "Protein slab harvested from anomaly beasts.", ItemType.Consumable, 32, 18f, 0f, 1.0f, null, new Color(0.85f, 0.22f, 0.32f));
            var itemRations = GetOrCreateItem("Item_Rations", "Bio-Ration Pack", "Standard emergency survival ration.", ItemType.Consumable, 16, 45f, 25f, 1.0f, null, new Color(0.95f, 0.82f, 0.25f));
            var itemScrap = GetOrCreateItem("Item_Scrap", "Titanium Scrap", "Hull fragments salvaged from crash wreckage.", ItemType.Material, 64, 0f, 0f, 1.0f, null, new Color(0.72f, 0.74f, 0.78f));

            var itemBarricade = GetOrCreateItem("Item_WoodBarricade", "Reinforced Barricade", "Heavy timber defensive barrier to block predators.", ItemType.Structure, 16, 0f, 0f, 1.0f, barricadePrefab, new Color(0.7f, 0.5f, 0.3f));
            var itemHeatBeacon = GetOrCreateItem("Item_PulseBeacon", "Pulse Heat Beacon", "Thermal aura generator providing an area-of-effect healing zone.", ItemType.Structure, 8, 0f, 0f, 1.0f, heatBeaconPrefab, new Color(1f, 0.65f, 0.15f));
            var itemAxe = GetOrCreateItem("Item_MakeshiftAxe", "Makeshift Tool Axe", "Fabricated high-impact salvage axe with 2x harvesting efficiency.", ItemType.Equipment, 1, 0f, 0f, 2.0f, null, new Color(0.3f, 0.85f, 0.7f));

            // 5. Recipes
            var recipeBarricade = GetOrCreateRecipe("Recipe_WoodBarricade", "Reinforced Barricade", "Erects a durable defense wall to protect outposts.", CraftingCategory.Structures, itemBarricade, 1, 0.5f, new[] { (itemWood, 4) });
            var recipeHeatBeacon = GetOrCreateRecipe("Recipe_PulseBeacon", "Pulse Heat Beacon", "Deploys a thermal beacon that restores health within its aura.", CraftingCategory.Structures, itemHeatBeacon, 1, 1.0f, new[] { (itemWood, 3), (itemStone, 2) });
            var recipeAxe = GetOrCreateRecipe("Recipe_MakeshiftAxe", "Makeshift Tool Axe", "Assembles an axe that doubles damage against resource nodes.", CraftingCategory.Tools, itemAxe, 1, 0.75f, new[] { (itemWood, 2), (itemScrap, 2) });

            List<RecipeSO> allRecipes = new List<RecipeSO> { recipeBarricade, recipeHeatBeacon, recipeAxe };

            // 6. Resource Node & Enemy ScriptableObjects
            var nodeTree = GetOrCreateResourceNode("Node_AlienTree", "Alien Timber Tree", 50f, itemWood, 2, 4);
            var nodeRock = GetOrCreateResourceNode("Node_PulseRock", "Pulse Vein Rock", 75f, itemStone, 3, 5);
            var enemyStats = GetOrCreateEnemyStats("Enemy_AnomalyProwler", "Anomaly Prowler", 65f, 2.6f, 4.8f, 9.5f, 1.8f, 15f, 1.25f, itemRawMeat, 1, 2);

            // 7. Materials for World Ground & Props
            Material groundCrashMat = GetOrCreateMaterial("Assets/Materials/M_Ground_Crash.mat", new Color(0.24f, 0.26f, 0.28f));
            Material groundOvergrowthMat = GetOrCreateMaterial("Assets/Materials/M_Ground_Overgrowth.mat", new Color(0.14f, 0.28f, 0.16f));
            Material groundCrystalMat = GetOrCreateMaterial("Assets/Materials/M_Ground_Crystal.mat", new Color(0.12f, 0.18f, 0.26f));
            Material wallMat = GetOrCreateMaterial("Assets/Materials/M_Wall.mat", new Color(0.15f, 0.16f, 0.18f));

            // 8. Lighting & Ambience
            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun == null)
            {
                var sunObj = new GameObject("Directional Light");
                sun = sunObj.AddComponent<Light>();
            }
            sun.type = LightType.Directional;
            sun.color = new Color(0.96f, 0.95f, 1.0f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            // 9. Environment Setup
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");
            while (envRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(envRoot.transform.GetChild(0).gameObject);
            }

            var oldGround = GameObject.Find("Ground");
            if (oldGround != null && oldGround.transform.parent != envRoot.transform) Object.DestroyImmediate(oldGround);

            // 9a. Biome Ground Zones (50m x 50m arena)
            CreateGroundZone(envRoot.transform, "Ground_CrashZone", new Vector3(0f, -0.5f, -12f), new Vector3(50f, 1f, 26f), groundCrashMat);
            CreateGroundZone(envRoot.transform, "Ground_OvergrowthZone", new Vector3(-13f, -0.5f, 13f), new Vector3(24f, 1f, 24f), groundOvergrowthMat);
            CreateGroundZone(envRoot.transform, "Ground_CrystalZone", new Vector3(13f, -0.5f, 13f), new Vector3(24f, 1f, 24f), groundCrystalMat);

            // 9b. Perimeter Walls
            float arenaHalf = 25f;
            float wallThickness = 1.2f;
            float wallHeight = 4f;
            CreateWall(envRoot.transform, "Wall_North", new Vector3(0f, wallHeight * 0.5f, arenaHalf), new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
            CreateWall(envRoot.transform, "Wall_South", new Vector3(0f, wallHeight * 0.5f, -arenaHalf), new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
            CreateWall(envRoot.transform, "Wall_East", new Vector3(arenaHalf, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaHalf * 2f), wallMat);
            CreateWall(envRoot.transform, "Wall_West", new Vector3(-arenaHalf, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaHalf * 2f), wallMat);

            // 9c. 3D Trees with AlienTimberTree.glb
            Vector3[] treePositions = new Vector3[]
            {
                new Vector3(-16f, 0f, 16f),
                new Vector3(-10f, 0f, 18f),
                new Vector3(-19f, 0f, 10f),
                new Vector3(-12f, 0f, 8f),
                new Vector3(-7f, 0f, 14f)
            };
            for (int i = 0; i < treePositions.Length; i++)
            {
                Create3DTree(envRoot.transform, $"Tree_Alien_{i + 1}", treePositions[i], treeModel, nodeTree);
            }

            // 9d. 3D Shrub Flora with AlienOrganicShrub.glb / Alt
            Vector3[] shrubPositions = new Vector3[]
            {
                new Vector3(-14f, 0f, 13f),
                new Vector3(-8f, 0f, 10f),
                new Vector3(-20f, 0f, 15f),
                new Vector3(-17f, 0f, 6f)
            };
            for (int i = 0; i < shrubPositions.Length; i++)
            {
                GameObject sPrefab = (i % 2 == 0) ? shrubModel : shrubAltModel;
                Create3DShrub(envRoot.transform, $"Shrub_Alien_{i + 1}", shrubPositions[i], sPrefab);
            }

            // 9e. 3D Crystalline Rocks with MineralBoulder.glb
            Vector3[] rockPositions = new Vector3[]
            {
                new Vector3(15f, 0f, 15f),
                new Vector3(19f, 0f, 11f),
                new Vector3(10f, 0f, 19f),
                new Vector3(18f, 0f, 18f),
                new Vector3(8f, 0f, 12f)
            };
            for (int i = 0; i < rockPositions.Length; i++)
            {
                Create3DRock(envRoot.transform, $"Rock_Pulse_{i + 1}", rockPositions[i], rockModel, nodeRock);
            }

            // 9f. World Pickups with custom 3D models
            Create3DPickup(envRoot.transform, "Pickup_Scrap_1", new Vector3(2f, 0.2f, -6f), itemScrap, 5, hullModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_Scrap_2", new Vector3(-3f, 0.2f, -8f), itemScrap, 4, hullModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_Rations_1", new Vector3(4f, 0.2f, -4f), itemRations, 2, rationsModel, new Vector3(0.8f, 0.8f, 0.8f));
            Create3DPickup(envRoot.transform, "Pickup_Timber_1", new Vector3(-2f, 0.2f, -5f), itemWood, 4, logsModel, new Vector3(0.8f, 0.8f, 0.8f));
            Create3DPickup(envRoot.transform, "Pickup_ConductiveOre_1", new Vector3(3f, 0.2f, -9f), itemStone, 3, oreModel, new Vector3(0.7f, 0.7f, 0.7f));
            Create3DPickup(envRoot.transform, "Pickup_BeastMeat_1", new Vector3(-4f, 0.2f, -3f), itemRawMeat, 2, meatModel, new Vector3(0.75f, 0.75f, 0.75f));

            // 10. Player Configuration with OrbitalAstronaut.glb
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                player.tag = "Player";
                var col = player.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1f, 0f);
                col.radius = 0.5f;
                col.height = 2.0f;
            }
            player.transform.position = new Vector3(0f, 0.05f, -10f);
            player.transform.rotation = Quaternion.identity;

            // Strip any primitive mesh renderer on player root
            var pMr = player.GetComponent<MeshRenderer>();
            if (pMr != null) Object.DestroyImmediate(pMr);
            var pMf = player.GetComponent<MeshFilter>();
            if (pMf != null) Object.DestroyImmediate(pMf);

            // Remove legacy primitive capsule and visor child so astronaut is sole rendered model
            Transform primitiveVis = player.transform.Find("Visual");
            if (primitiveVis != null) Object.DestroyImmediate(primitiveVis.gameObject);

            // Attach 3D OrbitalAstronaut visual
            Transform existingVis = player.transform.Find("Visual_OrbitalAstronaut");
            if (existingVis != null) Object.DestroyImmediate(existingVis.gameObject);

            if (astronautModel != null)
            {
                GameObject astronautVis = Object.Instantiate(astronautModel);
                astronautVis.name = "Visual_OrbitalAstronaut";
                astronautVis.transform.SetParent(player.transform, false);
                astronautVis.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                astronautVis.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                astronautVis.transform.localScale = new Vector3(1.9f, 1.9f, 1.9f);

                // Strip any colliders from the visual model
                var childCols = astronautVis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }

            // Player Controller & Physics
            var pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl == null) pCtrl = player.AddComponent<PlayerController>();
            pCtrl.ConfigureRigidbody();

            // Survival Stats
            var pStats = player.GetComponent<PlayerStats>();
            if (pStats == null) pStats = player.AddComponent<PlayerStats>();
            pStats.InitializeStats();

            // Combat
            var pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat == null) pCombat = player.AddComponent<PlayerCombat>();

            // Interaction
            var pInteract = player.GetComponent<PlayerInteraction>();
            if (pInteract == null) pInteract = player.AddComponent<PlayerInteraction>();

            // Inventory System
            var pInv = player.GetComponent<InventorySystem>();
            if (pInv == null) pInv = player.AddComponent<InventorySystem>();
            pInv.InitializeSlots();
            pInv.Clear();
            // Starting resources for testing crafting & building immediately
            pInv.AddItem(itemWood, 10);
            pInv.AddItem(itemStone, 6);
            pInv.AddItem(itemScrap, 4);
            pInv.AddItem(itemRations, 2);

            // Crafting Manager
            var pCrafting = player.GetComponent<CraftingManager>();
            if (pCrafting == null) pCrafting = player.AddComponent<CraftingManager>();
            pCrafting.TargetInventory = pInv;
            pCrafting.SetRecipes(allRecipes);

            // Structure Placer
            var pPlacer = player.GetComponent<StructurePlacer>();
            if (pPlacer == null) pPlacer = player.AddComponent<StructurePlacer>();
            pPlacer.Inventory = pInv;
            pPlacer.CraftingManager = pCrafting;
            pPlacer.PlayerTransform = player.transform;
            pPlacer.ValidHologramMaterial = validHoloMat;
            pPlacer.BlockedHologramMaterial = blockedHoloMat;
            pCombat.TargetStructurePlacer = pPlacer;

            // 11. Enemy Patrols with AlienPredatorBeast.obj
            GameObject enemiesRoot = GameObject.Find("Enemies");
            if (enemiesRoot == null) enemiesRoot = new GameObject("Enemies");
            while (enemiesRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(enemiesRoot.transform.GetChild(0).gameObject);
            }

            Create3DEnemy(enemiesRoot.transform, "Enemy_Prowler_Alpha", new Vector3(-12f, 0.05f, 12f), beastModel, enemyStats);
            Create3DEnemy(enemiesRoot.transform, "Enemy_Prowler_Beta", new Vector3(14f, 0.05f, 14f), beastModel, enemyStats);

            // 12. Main Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }
            var camFollow = cam.GetComponent<TopDownCameraFollow>();
            if (camFollow == null) camFollow = cam.gameObject.AddComponent<TopDownCameraFollow>();
            camFollow.Target = player.transform;
            camFollow.SnapToTarget();

            // 13. UI Canvas & Survival HUD + Crafting Window
            SetupHUDAndCraftingCanvas(pStats, pCrafting, pInv, pPlacer);

            // 14. NavMesh Bake
            BakeNavMesh();

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> Phase 6 Survival Sandbox scene (Crafting & 3D Assets) built and saved successfully!");
        }

        private static void CreateGroundZone(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = name;
            ground.transform.SetParent(parent);
            ground.transform.position = pos;
            ground.transform.localScale = size;
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = size;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
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
            col.radius = 0.5f;
            col.isTrigger = true;

            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(item, count);
        }

        private static void Create3DEnemy(Transform parent, string name, Vector3 pos, GameObject beastModel, EnemyStatsSO stats)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.SetParent(parent);
            enemy.transform.position = pos;

            if (beastModel != null)
            {
                GameObject vis = Object.Instantiate(beastModel);
                vis.name = "Visual_AlienPredatorBeast";
                vis.transform.SetParent(enemy.transform, false);
                vis.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                vis.transform.localRotation = Quaternion.identity;
                vis.transform.localScale = new Vector3(2.4f, 2.4f, 2.4f);
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }

            var col = enemy.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.radius = 0.55f;
            col.height = 1.5f;

            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = stats.PatrolSpeed;
            agent.angularSpeed = 480f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 1.2f;

            var controller = enemy.AddComponent<EnemyController>();
            controller.Configure(stats);
        }

        private static GameObject GetOrCreateBarricadePrefab(GameObject logsModel)
        {
            string path = "Assets/Prefabs/Structures/Prefab_Barricade.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("Prefab_Barricade");
            if (logsModel != null)
            {
                GameObject vis = Object.Instantiate(logsModel);
                vis.name = "Visual_Logs";
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                vis.transform.localScale = new Vector3(2.8f, 2.8f, 2.8f);
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }
            else
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Barricade_Mesh";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                cube.transform.localScale = new Vector3(2.6f, 1.6f, 0.6f);
                Object.DestroyImmediate(cube.GetComponent<Collider>());
            }

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.8f, 0f);
            col.size = new Vector3(2.6f, 1.6f, 0.8f);

            var buildable = root.AddComponent<BuildableStructure>();
            buildable.StructureName = "Reinforced Barricade";
            buildable.MaxDurability = 150f;
            buildable.PlacementExtents = new Vector3(1.3f, 0.8f, 0.4f);

            var navObstacle = root.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.size = new Vector3(2.6f, 1.6f, 0.8f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject GetOrCreateHeatBeaconPrefab(GameObject oreModel)
        {
            string path = "Assets/Prefabs/Structures/Prefab_HeatBeacon.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                var existingStructure = existing.GetComponent<BuildableStructure>();
                if (existingStructure != null && existingStructure.Type == StructureType.HeatBeacon && existingStructure.ThermalWarmthBonus >= 30f)
                {
                    return existing;
                }
            }

            GameObject root = new GameObject("Prefab_HeatBeacon");
            if (oreModel != null)
            {
                GameObject vis = Object.Instantiate(oreModel);
                vis.name = "Visual_BeaconCore";
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
                var childCols = vis.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < childCols.Length; c++) Object.DestroyImmediate(childCols[c]);
            }
            else
            {
                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "Beacon_Mesh";
                cyl.transform.SetParent(root.transform, false);
                cyl.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                cyl.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
                Object.DestroyImmediate(cyl.GetComponent<Collider>());
            }

            // Warm Aura Light
            GameObject lightObj = new GameObject("AuraLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.2f);
            light.range = 8f;
            light.intensity = 2.5f;

            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.radius = 0.7f;

            var buildable = root.AddComponent<BuildableStructure>();
            buildable.Type = StructureType.HeatBeacon;
            buildable.StructureName = "Pulse Heat Beacon";
            buildable.MaxDurability = 120f;
            buildable.AuraRadius = 5.5f;
            buildable.HealthRegenPerSecond = 5.0f;
            buildable.ThermalWarmthBonus = 35.0f;
            buildable.PlacementExtents = new Vector3(0.8f, 0.8f, 0.8f);

            var navObstacle = root.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.size = new Vector3(1.5f, 1.5f, 1.5f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void SetupHUDAndCraftingCanvas(PlayerStats pStats, CraftingManager pCrafting, InventorySystem pInv, StructurePlacer pPlacer)
        {
            // Ensure EventSystem exists for UI button interaction
            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("HUD_Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var hud = canvasObj.GetComponent<SurvivalHUD>();
            if (hud == null) hud = canvasObj.AddComponent<SurvivalHUD>();

            // Vitals Gauge Panel (bottom-left)
            Transform statsPanel = canvasObj.transform.Find("StatsPanel");
            if (statsPanel == null)
            {
                var panelObj = new GameObject("StatsPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);
                var rt = panelObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(30f, 30f);
                rt.sizeDelta = new Vector2(280f, 120f);
                statsPanel = panelObj.transform;
            }

            Slider healthSlider = CreateBar(statsPanel, "HealthBar", new Vector2(0f, 80f), new Color(0.9f, 0.2f, 0.2f));
            Slider staminaSlider = CreateBar(statsPanel, "StaminaBar", new Vector2(0f, 40f), new Color(0.2f, 0.65f, 0.95f));
            Slider hungerSlider = CreateBar(statsPanel, "HungerBar", new Vector2(0f, 0f), new Color(0.95f, 0.65f, 0.15f));

            // Starvation Warning Banner
            Transform existingStarve = canvasObj.transform.Find("StarvationWarningBanner");
            if (existingStarve != null) Object.DestroyImmediate(existingStarve.gameObject);

            GameObject starveObj = new GameObject("StarvationWarningBanner");
            starveObj.transform.SetParent(canvasObj.transform, false);
            var starveRt = starveObj.AddComponent<RectTransform>();
            starveRt.anchorMin = new Vector2(0.5f, 1f);
            starveRt.anchorMax = new Vector2(0.5f, 1f);
            starveRt.pivot = new Vector2(0.5f, 1f);
            starveRt.anchoredPosition = new Vector2(0f, -40f);
            starveRt.sizeDelta = new Vector2(360f, 48f);
            var starveImg = starveObj.AddComponent<Image>();
            starveImg.color = new Color(0.85f, 0.15f, 0.15f, 0.85f);
            starveObj.SetActive(false);

            // Death Banner
            Transform existingDeath = canvasObj.transform.Find("DeathBanner");
            if (existingDeath != null) Object.DestroyImmediate(existingDeath.gameObject);

            GameObject deathObj = new GameObject("DeathBanner");
            deathObj.transform.SetParent(canvasObj.transform, false);
            var deathRt = deathObj.AddComponent<RectTransform>();
            deathRt.anchorMin = new Vector2(0.5f, 0.5f);
            deathRt.anchorMax = new Vector2(0.5f, 0.5f);
            deathRt.pivot = new Vector2(0.5f, 0.5f);
            deathRt.anchoredPosition = Vector2.zero;
            deathRt.sizeDelta = new Vector2(500f, 120f);
            var deathImg = deathObj.AddComponent<Image>();
            deathImg.color = new Color(0.1f, 0.05f, 0.05f, 0.95f);
            deathObj.SetActive(false);

            hud.ConfigureComponents(healthSlider, staminaSlider, hungerSlider, starveObj, deathObj);
            hud.TargetStats = pStats;

            // Crafting UI Terminal Window (center-right)
            Transform existingCraftPanel = canvasObj.transform.Find("Crafting_Panel");
            if (existingCraftPanel != null) Object.DestroyImmediate(existingCraftPanel.gameObject);

            GameObject craftPanelObj = new GameObject("Crafting_Panel");
            craftPanelObj.transform.SetParent(canvasObj.transform, false);
            var cpRt = craftPanelObj.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(1f, 0.5f);
            cpRt.anchorMax = new Vector2(1f, 0.5f);
            cpRt.pivot = new Vector2(1f, 0.5f);
            cpRt.anchoredPosition = new Vector2(-40f, 0f);
            cpRt.sizeDelta = new Vector2(520f, 480f);

            var cpBg = craftPanelObj.AddComponent<Image>();
            cpBg.color = new Color(0.08f, 0.1f, 0.13f, 0.92f);

            // Header Banner
            GameObject headerObj = new GameObject("Header");
            headerObj.transform.SetParent(craftPanelObj.transform, false);
            var headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, 48f);
            var headerImg = headerObj.AddComponent<Image>();
            headerImg.color = new Color(0.14f, 0.18f, 0.24f, 1f);

            GameObject headerTextObj = new GameObject("HeaderText");
            headerTextObj.transform.SetParent(headerObj.transform, false);
            var htRt = headerTextObj.AddComponent<RectTransform>();
            htRt.anchorMin = Vector2.zero;
            htRt.anchorMax = Vector2.one;
            htRt.sizeDelta = Vector2.zero;
            var htText = headerTextObj.AddComponent<Text>();
            htText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            htText.fontSize = 16;
            htText.fontStyle = FontStyle.Bold;
            htText.alignment = TextAnchor.MiddleCenter;
            htText.color = new Color(0.35f, 0.85f, 1.0f);
            htText.text = "FIELD FABRICATOR [TAB / C]";

            // Recipe Scroll/Container Area
            GameObject recipeContainerObj = new GameObject("RecipeContainer");
            recipeContainerObj.transform.SetParent(craftPanelObj.transform, false);
            var rcRt = recipeContainerObj.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0f, 0f);
            rcRt.anchorMax = new Vector2(1f, 1f);
            rcRt.pivot = new Vector2(0.5f, 0.5f);
            rcRt.anchoredPosition = new Vector2(0f, -28f);
            rcRt.sizeDelta = new Vector2(-24f, -70f);

            var vlg = recipeContainerObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;

            var craftUI = canvasObj.GetComponent<CraftingUI>();
            if (craftUI == null) craftUI = canvasObj.AddComponent<CraftingUI>();

            craftUI.WindowRoot = craftPanelObj;
            craftUI.TargetCraftingManager = pCrafting;
            craftUI.TargetInventory = pInv;
            craftUI.TargetStructurePlacer = pPlacer;
            craftPanelObj.SetActive(false);
        }

        private static Slider CreateBar(Transform parent, string name, Vector2 pos, Color fillColor)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);
            var rt = barObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250f, 24f);

            var bgImg = barObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barObj.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            var slider = barObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static void BakeNavMesh()
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("<color=green>[Outpost 404]</color> NavMesh bake completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Outpost 404] NavMesh bake warning: {ex.Message}");
            }
        }

        private static void EnsureDirectories()
        {
            string[] dirs = new string[]
            {
                "Assets/Materials",
                "Assets/Prefabs/Structures",
                "Assets/ScriptableObjects/Items",
                "Assets/ScriptableObjects/Recipes",
                "Assets/ScriptableObjects/Nodes",
                "Assets/ScriptableObjects/Enemies",
                "Assets/Documentation"
            };

            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
            AssetDatabase.Refresh();
        }

        private static Material GetOrCreateMaterial(string path, Color color, Color? emission = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                if (emission.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission.Value);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Material GetOrCreateHologramMaterial(string path, Color color, Color emission)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static ItemDataSO GetOrCreateItem(string id, string name, string desc, ItemType type, int maxStack, float cal, float heal, float mult, GameObject prefab, Color col)
        {
            string path = $"Assets/ScriptableObjects/Items/{id}.asset";
            ItemDataSO item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDataSO>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.ItemId = id;
            item.ItemName = name;
            item.Description = desc;
            item.ItemType = type;
            item.MaxStack = maxStack;
            item.CaloricValue = cal;
            item.HealValue = heal;
            item.HarvestMultiplier = mult;
            item.StructurePrefab = prefab;
            item.ItemColor = col;
            EditorUtility.SetDirty(item);
            return item;
        }

        private static RecipeSO GetOrCreateRecipe(string id, string name, string desc, CraftingCategory category, ItemDataSO output, int count, float time, (ItemDataSO item, int count)[] ingredients)
        {
            string path = $"Assets/ScriptableObjects/Recipes/{id}.asset";
            RecipeSO recipe = AssetDatabase.LoadAssetAtPath<RecipeSO>(path);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<RecipeSO>();
                AssetDatabase.CreateAsset(recipe, path);
            }
            recipe.RecipeId = id;
            recipe.RecipeName = name;
            recipe.Description = desc;
            recipe.Category = category;
            recipe.OutputItem = output;
            recipe.OutputCount = count;
            recipe.CraftTime = time;
            recipe.Ingredients.Clear();
            for (int i = 0; i < ingredients.Length; i++)
            {
                recipe.Ingredients.Add(new IngredientRequirement(ingredients[i].item, ingredients[i].count));
            }
            EditorUtility.SetDirty(recipe);
            return recipe;
        }

        private static ResourceNodeSO GetOrCreateResourceNode(string id, string name, float hp, ItemDataSO drop, int min, int max)
        {
            string path = $"Assets/ScriptableObjects/Nodes/{id}.asset";
            ResourceNodeSO node = AssetDatabase.LoadAssetAtPath<ResourceNodeSO>(path);
            if (node == null)
            {
                node = ScriptableObject.CreateInstance<ResourceNodeSO>();
                AssetDatabase.CreateAsset(node, path);
            }
            node.NodeName = name;
            node.MaxHealth = hp;
            node.DropItem = drop;
            node.MinDropCount = min;
            node.MaxDropCount = max;
            EditorUtility.SetDirty(node);
            return node;
        }

        private static EnemyStatsSO GetOrCreateEnemyStats(string id, string name, float hp, float patrolSpd, float chaseSpd, float detectRad, float atkRange, float atkDmg, float atkCd, ItemDataSO drop, int min, int max)
        {
            string path = $"Assets/ScriptableObjects/Enemies/{id}.asset";
            EnemyStatsSO stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(path);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
                AssetDatabase.CreateAsset(stats, path);
            }
            stats.EnemyName = name;
            stats.MaxHealth = hp;
            stats.PatrolSpeed = patrolSpd;
            stats.ChaseSpeed = chaseSpd;
            stats.DetectionRadius = detectRad;
            stats.AttackRange = atkRange;
            stats.AttackDamage = atkDmg;
            stats.AttackCooldown = atkCd;
            stats.DropItem = drop;
            stats.MinDropCount = min;
            stats.MaxDropCount = max;
            EditorUtility.SetDirty(stats);
            return stats;
        }
    }
}
#endif
