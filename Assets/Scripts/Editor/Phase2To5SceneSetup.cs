#if UNITY_EDITOR
using System.IO;
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
using Outpost404.UI;

namespace Outpost404.EditorTools
{
    public static class Phase2To5SceneSetup
    {
        [MenuItem("Outpost 404/Setup Full Game Scene (Phases 2-5)", false, 20)]
        public static void BuildFullScene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 1. Ensure Asset Directories exist
            EnsureDirectories();

            // 2. Generate or Load ScriptableObjects
            var itemWood = GetOrCreateItem("Item_Wood", "Composite Timber", "Harvested structural alien wood.", ItemType.Resource, 64, 0f, 0f, new Color(0.62f, 0.42f, 0.22f));
            var itemStone = GetOrCreateItem("Item_Stone", "Conductive Ore", "Basalt ore laced with crystalline conduits.", ItemType.Resource, 64, 0f, 0f, new Color(0.18f, 0.75f, 0.88f));
            var itemRawMeat = GetOrCreateItem("Item_RawMeat", "Raw Anomaly Meat", "Protein slab harvested from anomaly beasts.", ItemType.Consumable, 32, 18f, 0f, new Color(0.85f, 0.22f, 0.32f));
            var itemRations = GetOrCreateItem("Item_Rations", "Bio-Ration Pack", "Standard emergency survival ration.", ItemType.Consumable, 16, 45f, 25f, new Color(0.95f, 0.82f, 0.25f));
            var itemScrap = GetOrCreateItem("Item_Scrap", "Titanium Scrap", "Hull fragments salvaged from crash wreckage.", ItemType.Material, 64, 0f, 0f, new Color(0.72f, 0.74f, 0.78f));

            var nodeTree = GetOrCreateResourceNode("Node_AlienTree", "Alien Timber Tree", 50f, itemWood, 2, 4);
            var nodeRock = GetOrCreateResourceNode("Node_PulseRock", "Pulse Vein Rock", 75f, itemStone, 3, 5);

            var enemyStats = GetOrCreateEnemyStats("Enemy_AnomalyProwler", "Anomaly Prowler", 65f, 2.6f, 4.8f, 9.5f, 1.8f, 15f, 1.25f, itemRawMeat, 1, 2);

            // 3. Materials
            Material groundCrashMat = GetOrCreateMaterial("Assets/Materials/M_Ground_Crash.mat", new Color(0.24f, 0.26f, 0.28f));
            Material groundOvergrowthMat = GetOrCreateMaterial("Assets/Materials/M_Ground_Overgrowth.mat", new Color(0.14f, 0.28f, 0.16f));
            Material groundCrystalMat = GetOrCreateMaterial("Assets/Materials/M_Ground_Crystal.mat", new Color(0.12f, 0.18f, 0.26f));
            Material wallMat = GetOrCreateMaterial("Assets/Materials/M_Wall.mat", new Color(0.15f, 0.16f, 0.18f));
            Material treeTrunkMat = GetOrCreateMaterial("Assets/Materials/M_TreeTrunk.mat", new Color(0.35f, 0.25f, 0.15f));
            Material treeFoliageMat = GetOrCreateMaterial("Assets/Materials/M_TreeFoliage.mat", new Color(0.12f, 0.55f, 0.32f));
            Material rockMat = GetOrCreateMaterial("Assets/Materials/M_PulseRock.mat", new Color(0.22f, 0.25f, 0.30f), new Color(0f, 0.7f, 0.9f) * 0.4f);
            Material enemyMat = GetOrCreateMaterial("Assets/Materials/M_EnemyProwler.mat", new Color(0.65f, 0.15f, 0.25f), new Color(0.9f, 0.2f, 0.3f) * 0.6f);
            Material lootMat = GetOrCreateMaterial("Assets/Materials/M_LootDrop.mat", new Color(0.95f, 0.85f, 0.25f), new Color(0.95f, 0.85f, 0.25f) * 0.5f);

            // 4. Lighting & Ambience
            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun == null)
            {
                var sunObj = new GameObject("Directional Light");
                sun = sunObj.AddComponent<Light>();
            }
            sun.type = LightType.Directional;
            sun.color = new Color(0.96f, 0.95f, 1.0f);
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            // 5. Environment Root
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");

            // Clean previous procedural children in Environment
            while (envRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(envRoot.transform.GetChild(0).gameObject);
            }

            // Remove old standalone Ground or Obstacles if present
            var oldGround = GameObject.Find("Ground");
            if (oldGround != null && oldGround.transform.parent != envRoot.transform) Object.DestroyImmediate(oldGround);
            var oldObstacle = GameObject.Find("Obstacle_Pillar");
            if (oldObstacle != null) Object.DestroyImmediate(oldObstacle);

            // 5a. Biome Ground Zones (50m x 50m arena)
            CreateGroundZone(envRoot.transform, "Ground_CrashZone", new Vector3(0f, -0.5f, -12f), new Vector3(50f, 1f, 26f), groundCrashMat);
            CreateGroundZone(envRoot.transform, "Ground_OvergrowthZone", new Vector3(-13f, -0.5f, 13f), new Vector3(24f, 1f, 24f), groundOvergrowthMat);
            CreateGroundZone(envRoot.transform, "Ground_CrystalZone", new Vector3(13f, -0.5f, 13f), new Vector3(24f, 1f, 24f), groundCrystalMat);

            // 5b. Perimeter Walls
            float arenaHalf = 25f;
            float wallThickness = 1.2f;
            float wallHeight = 4f;
            CreateWall(envRoot.transform, "Wall_North", new Vector3(0f, wallHeight * 0.5f, arenaHalf), new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
            CreateWall(envRoot.transform, "Wall_South", new Vector3(0f, wallHeight * 0.5f, -arenaHalf), new Vector3(arenaHalf * 2f + wallThickness, wallHeight, wallThickness), wallMat);
            CreateWall(envRoot.transform, "Wall_East", new Vector3(arenaHalf, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaHalf * 2f), wallMat);
            CreateWall(envRoot.transform, "Wall_West", new Vector3(-arenaHalf, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, arenaHalf * 2f), wallMat);

            // 5c. Harvestable Trees (Overgrowth Zone)
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
                CreateTree(envRoot.transform, $"Tree_Alien_{i + 1}", treePositions[i], treeTrunkMat, treeFoliageMat, nodeTree);
            }

            // 5d. Harvestable Crystalline Rocks (Mineral Zone)
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
                CreateRock(envRoot.transform, $"Rock_Pulse_{i + 1}", rockPositions[i], rockMat, nodeRock);
            }

            // 5e. World Loot Pickups (Scattered around Crash Enclave)
            CreatePickup(envRoot.transform, "Pickup_Scrap_1", new Vector3(2f, 0.4f, -6f), itemScrap, 5, lootMat);
            CreatePickup(envRoot.transform, "Pickup_Scrap_2", new Vector3(-3f, 0.4f, -8f), itemScrap, 4, lootMat);
            CreatePickup(envRoot.transform, "Pickup_Rations_1", new Vector3(4f, 0.4f, -4f), itemRations, 2, lootMat);

            // 6. Player Configuration
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.tag = "Player";
            }
            player.transform.position = new Vector3(0f, 1f, -10f);
            player.transform.rotation = Quaternion.identity;

            var pCtrl = player.GetComponent<PlayerController>();
            if (pCtrl == null) pCtrl = player.AddComponent<PlayerController>();
            pCtrl.ConfigureRigidbody();

            var pStats = player.GetComponent<PlayerStats>();
            if (pStats == null) pStats = player.AddComponent<PlayerStats>();
            pStats.InitializeStats();

            var pCombat = player.GetComponent<PlayerCombat>();
            if (pCombat == null) pCombat = player.AddComponent<PlayerCombat>();

            var pInteract = player.GetComponent<PlayerInteraction>();
            if (pInteract == null) pInteract = player.AddComponent<PlayerInteraction>();

            var pInv = player.GetComponent<InventorySystem>();
            if (pInv == null) pInv = player.AddComponent<InventorySystem>();
            pInv.InitializeSlots();

            // 7. Enemy Patrols
            GameObject enemiesRoot = GameObject.Find("Enemies");
            if (enemiesRoot == null) enemiesRoot = new GameObject("Enemies");
            while (enemiesRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(enemiesRoot.transform.GetChild(0).gameObject);
            }

            CreateEnemy(enemiesRoot.transform, "Enemy_Prowler_Alpha", new Vector3(-12f, 1f, 12f), enemyMat, enemyStats);
            CreateEnemy(enemiesRoot.transform, "Enemy_Prowler_Beta", new Vector3(14f, 1f, 14f), enemyMat, enemyStats);

            // 8. Main Camera
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

            // 9. UI Canvas & Survival HUD
            SetupHUDCanvas(pStats);

            // 10. NavMesh Bake
            BakeNavMesh();

            // Mark Scene Dirty and Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> Full survival sandbox scene constructed and saved successfully!");
        }

        private static void CreateGroundZone(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = name;
            ground.transform.SetParent(parent);
            ground.transform.position = pos;
            ground.transform.localScale = size;
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Flag as Static for NavMesh baking
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

        private static void CreateTree(Transform parent, string name, Vector3 pos, Material trunkMat, Material foliageMat, ResourceNodeSO nodeSO)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent);
            tree.transform.position = pos;

            // Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            trunk.transform.localScale = new Vector3(0.7f, 1.5f, 0.7f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMat;
            Object.DestroyImmediate(trunk.GetComponent<Collider>());

            // Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "FoliageCanopy";
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = new Vector3(0f, 3.8f, 0f);
            canopy.transform.localScale = new Vector3(3.2f, 1.8f, 3.2f);
            canopy.GetComponent<MeshRenderer>().sharedMaterial = foliageMat;
            Object.DestroyImmediate(canopy.GetComponent<Collider>());

            // NavMesh Obstacle & Resource Node on root
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

        private static void CreateRock(Transform parent, string name, Vector3 pos, Material rockMat, ResourceNodeSO nodeSO)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = name;
            rock.transform.SetParent(parent);
            rock.transform.position = pos + new Vector3(0f, 0.8f, 0f);
            rock.transform.localScale = new Vector3(1.8f, 1.6f, 1.8f);
            rock.transform.rotation = Quaternion.Euler(15f, 35f, 10f);
            rock.GetComponent<MeshRenderer>().sharedMaterial = rockMat;

            var rNode = rock.AddComponent<ResourceNode>();
            rNode.Configure(nodeSO);

            var navObstacle = rock.AddComponent<NavMeshObstacle>();
            navObstacle.carving = true;
            navObstacle.size = new Vector3(2.2f, 2f, 2.2f);
        }

        private static void CreateEnemy(Transform parent, string name, Vector3 pos, Material enemyMat, EnemyStatsSO stats)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = name;
            enemy.tag = "Untagged";
            enemy.transform.SetParent(parent);
            enemy.transform.position = pos;
            enemy.GetComponent<MeshRenderer>().sharedMaterial = enemyMat;

            // Stylized head crest for top-down direction
            GameObject crest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crest.name = "HeadCrest";
            crest.transform.SetParent(enemy.transform);
            crest.transform.localPosition = new Vector3(0f, 0.65f, 0.45f);
            crest.transform.localScale = new Vector3(0.35f, 0.25f, 0.45f);
            crest.GetComponent<MeshRenderer>().sharedMaterial = enemyMat;
            Object.DestroyImmediate(crest.GetComponent<Collider>());

            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = stats.PatrolSpeed;
            agent.angularSpeed = 480f;
            agent.acceleration = 16f;
            agent.stoppingDistance = 1.2f;

            var controller = enemy.AddComponent<EnemyController>();
            controller.Configure(stats);
        }

        private static void CreatePickup(Transform parent, string name, Vector3 pos, ItemDataSO item, int count, Material lootMat)
        {
            GameObject pickupObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickupObj.name = name;
            pickupObj.transform.SetParent(parent);
            pickupObj.transform.position = pos;
            pickupObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            pickupObj.GetComponent<MeshRenderer>().sharedMaterial = lootMat;

            var col = pickupObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(item, count);
        }

        private static void SetupHUDCanvas(PlayerStats pStats)
        {
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

            // Root panel for bars (bottom-left)
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

            // Starvation Warning Banner (top-center)
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

            // Death Banner (center)
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
                "Assets/ScriptableObjects/Items",
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

        private static ItemDataSO GetOrCreateItem(string id, string name, string desc, ItemType type, int maxStack, float cal, float heal, Color col)
        {
            string path = $"Assets/ScriptableObjects/Items/{id}.asset";
            ItemDataSO item = AssetDatabase.LoadAssetAtPath<ItemDataSO>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemDataSO>();
                item.ItemId = id;
                item.ItemName = name;
                item.Description = desc;
                item.ItemType = type;
                item.MaxStack = maxStack;
                item.CaloricValue = cal;
                item.HealValue = heal;
                item.ItemColor = col;
                AssetDatabase.CreateAsset(item, path);
            }
            return item;
        }

        private static ResourceNodeSO GetOrCreateResourceNode(string id, string name, float hp, ItemDataSO drop, int min, int max)
        {
            string path = $"Assets/ScriptableObjects/Nodes/{id}.asset";
            ResourceNodeSO node = AssetDatabase.LoadAssetAtPath<ResourceNodeSO>(path);
            if (node == null)
            {
                node = ScriptableObject.CreateInstance<ResourceNodeSO>();
                node.NodeName = name;
                node.MaxHealth = hp;
                node.DropItem = drop;
                node.MinDropCount = min;
                node.MaxDropCount = max;
                AssetDatabase.CreateAsset(node, path);
            }
            return node;
        }

        private static EnemyStatsSO GetOrCreateEnemyStats(string id, string name, float hp, float patrolSpd, float chaseSpd, float detectRad, float atkRange, float atkDmg, float atkCd, ItemDataSO drop, int min, int max)
        {
            string path = $"Assets/ScriptableObjects/Enemies/{id}.asset";
            EnemyStatsSO stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(path);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<EnemyStatsSO>();
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
                AssetDatabase.CreateAsset(stats, path);
            }
            return stats;
        }
    }
}
#endif
