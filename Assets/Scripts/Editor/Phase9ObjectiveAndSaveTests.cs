#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Outpost404.Player;
using Outpost404.Survival;
using Outpost404.Combat;
using Outpost404.Inventory;
using Outpost404.Building;
using Outpost404.Harvesting;
using Outpost404.AI;
using Outpost404.Environment;
using Outpost404.Objective;
using Outpost404.Persistence;
using Outpost404.UI;
using Outpost404.EditorTools;

namespace Outpost404.Tests
{
    public class Phase9ObjectiveAndSaveTests
    {
        private GameObject testRoot;
        private InventorySystem inventory;
        private PlayerStats playerStats;
        private BeaconObjective beacon;
        private MissionStatistics statistics;
        private SaveSystem saveSystem;

        private ItemDataSO oreItem;
        private ItemDataSO scrapItem;
        private ItemDataSO timberItem;
        private ItemDataSO meatItem;

        [SetUp]
        public void Setup()
        {
            testRoot = new GameObject("TestRoot_Phase9");

            // Setup mock player with stats and inventory
            var playerObj = new GameObject("Player_Test");
            playerObj.transform.SetParent(testRoot.transform);
            playerStats = playerObj.AddComponent<PlayerStats>();
            playerStats.InitializeStats();

            inventory = playerObj.AddComponent<InventorySystem>();
            inventory.InitializeSlots();

            // Setup mock SOS beacon
            var beaconObj = new GameObject("Beacon_Test");
            beaconObj.transform.SetParent(testRoot.transform);
            beacon = beaconObj.AddComponent<BeaconObjective>();
            beacon.EnsureDefaultStages();

            // Setup mock items
            oreItem = ScriptableObject.CreateInstance<ItemDataSO>();
            oreItem.ItemId = "Item_Stone";
            oreItem.ItemName = "Conductive Ore";

            scrapItem = ScriptableObject.CreateInstance<ItemDataSO>();
            scrapItem.ItemId = "Item_Scrap";
            scrapItem.ItemName = "Titanium Scrap";

            timberItem = ScriptableObject.CreateInstance<ItemDataSO>();
            timberItem.ItemId = "Item_Wood";
            timberItem.ItemName = "Composite Timber";

            meatItem = ScriptableObject.CreateInstance<ItemDataSO>();
            meatItem.ItemId = "Item_RawMeat";
            meatItem.ItemName = "Raw Anomaly Meat";

            // Configure beacon stages with mock items
            var stageList = beacon.Stages;
            stageList[0].primaryItem = oreItem;
            stageList[0].primaryRequired = 4;
            stageList[0].secondaryItem = scrapItem;
            stageList[0].secondaryRequired = 3;

            stageList[1].primaryItem = timberItem;
            stageList[1].primaryRequired = 6;
            stageList[1].secondaryItem = oreItem;
            stageList[1].secondaryRequired = 4;

            stageList[2].primaryItem = meatItem;
            stageList[2].primaryRequired = 3;
            stageList[2].secondaryItem = scrapItem;
            stageList[2].secondaryRequired = 5;

            // Setup statistics tracker
            var statsObj = new GameObject("MissionStatistics_Test");
            statsObj.transform.SetParent(testRoot.transform);
            statistics = statsObj.AddComponent<MissionStatistics>();
            statistics.ResetStats();
            MissionStatistics.Instance = statistics;

            // Setup SaveSystem
            var saveObj = new GameObject("SaveSystem_Test");
            saveObj.transform.SetParent(testRoot.transform);
            saveSystem = saveObj.AddComponent<SaveSystem>();
            saveSystem.ConfigureTargets(playerStats, inventory, beacon, null, statistics);
            saveSystem.ItemCatalog.Add(oreItem);
            saveSystem.ItemCatalog.Add(scrapItem);
            saveSystem.ItemCatalog.Add(timberItem);
            saveSystem.ItemCatalog.Add(meatItem);
        }

        [TearDown]
        public void TearDown()
        {
            if (testRoot != null)
            {
                Object.DestroyImmediate(testRoot);
            }
            if (MissionStatistics.Instance == statistics)
            {
                MissionStatistics.Instance = null;
            }
            if (BeaconObjective.Instance == beacon)
            {
                BeaconObjective.Instance = null;
            }
            if (SaveSystem.Instance == saveSystem)
            {
                SaveSystem.Instance = null;
            }

            if (oreItem != null) Object.DestroyImmediate(oreItem);
            if (scrapItem != null) Object.DestroyImmediate(scrapItem);
            if (timberItem != null) Object.DestroyImmediate(timberItem);
            if (meatItem != null) Object.DestroyImmediate(meatItem);
        }

        // =========================================================================
        // 1. Scene Integration & Map Expansion Setup
        // =========================================================================

        [Test, Order(1)]
        public void Scene_BuildPhase9Scene_ExecutesCleanlyAndSaves()
        {
            Assert.DoesNotThrow(() =>
            {
                Phase9SceneSetup.BuildPhase9Scene();
            }, "Phase9SceneSetup.BuildPhase9Scene must execute without errors.");
        }

        [Test, Order(2)]
        public void Scene_Player_AstronautVisualIsolation_NoPrimitiveCapsuleObstructing()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player with tag 'Player' must exist in scene.");

            // Assert primitive orange capsule and visor are removed
            Transform primitiveVis = player.transform.Find("Visual");
            Assert.IsNull(primitiveVis, "Player MUST NOT contain the primitive capsule 'Visual' GameObject.");

            Transform primitiveVisor = player.transform.Find("Visor_OrientationIndicator");
            Assert.IsNull(primitiveVisor, "Player MUST NOT contain the primitive 'Visor_OrientationIndicator' GameObject.");

            // Assert root has no MeshRenderer
            Assert.IsNull(player.GetComponent<MeshRenderer>(), "Player root GameObject must not have a MeshRenderer.");
            Assert.IsNull(player.GetComponent<MeshFilter>(), "Player root GameObject must not have a MeshFilter.");

            // Assert authentic 3D OrbitalAstronaut visual is active and configured
            Transform astronautVis = player.transform.Find("Visual_OrbitalAstronaut");
            Assert.IsNotNull(astronautVis, "Player must have 'Visual_OrbitalAstronaut' 3D child.");
            Assert.IsTrue(astronautVis.gameObject.activeSelf, "Visual_OrbitalAstronaut must be active.");

            Renderer[] renderers = astronautVis.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0, "Visual_OrbitalAstronaut must contain active Renderers.");

            // Assert pivot is aligned to ground (elevated by ~0.95m so feet rest on terrain at local Y=0)
            Assert.GreaterOrEqual(astronautVis.localPosition.y, 0.85f, "Astronaut localPosition Y must be elevated to align feet with ground.");
            Assert.AreEqual(1.9f, astronautVis.localScale.y, 0.05f, "Astronaut scale must be approximately 1.9m height.");
        }

        [Test, Order(3)]
        public void Scene_MapExpansion_120mTerritoryAndSectorsConfigured()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var env = GameObject.Find("Environment");
            Assert.IsNotNull(env, "Environment root must exist.");

            // 1. Perimeter Walls enclosing 120m x 120m territory (arenaHalf = 60m)
            var wallNorth = env.transform.Find("Wall_North");
            var wallSouth = env.transform.Find("Wall_South");
            var wallEast = env.transform.Find("Wall_East");
            var wallWest = env.transform.Find("Wall_West");

            Assert.IsNotNull(wallNorth, "Wall_North must exist.");
            Assert.IsNotNull(wallSouth, "Wall_South must exist.");
            Assert.IsNotNull(wallEast, "Wall_East must exist.");
            Assert.IsNotNull(wallWest, "Wall_West must exist.");

            Assert.AreEqual(60f, wallNorth.position.z, 1.0f, "Wall_North must enclose at Z = +60m.");
            Assert.AreEqual(-60f, wallSouth.position.z, 1.0f, "Wall_South must enclose at Z = -60m.");
            Assert.AreEqual(60f, wallEast.position.x, 1.0f, "Wall_East must enclose at X = +60m.");
            Assert.AreEqual(-60f, wallWest.position.x, 1.0f, "Wall_West must enclose at X = -60m.");

            // 2. 4 Biome Sector Ground Zones
            var alphaGround = env.transform.Find("Ground_SectorAlpha_CrashZone");
            var betaGround = env.transform.Find("Ground_SectorBeta_Forest");
            var gammaGround = env.transform.Find("Ground_SectorGamma_CrystallineBasin");
            var deltaGround = env.transform.Find("Ground_SectorDelta_AnomalyHive");

            Assert.IsNotNull(alphaGround, "Ground_SectorAlpha_CrashZone must exist.");
            Assert.IsNotNull(betaGround, "Ground_SectorBeta_Forest must exist.");
            Assert.IsNotNull(gammaGround, "Ground_SectorGamma_CrystallineBasin must exist.");
            Assert.IsNotNull(deltaGround, "Ground_SectorDelta_AnomalyHive must exist.");

            // 3. Sector Asset Populations
            // Sector Beta trees count (at least 25 trees)
            var allTrees = env.GetComponentsInChildren<ResourceNode>(true);
            int treeCount = 0;
            int rockCount = 0;
            for (int i = 0; i < allTrees.Length; i++)
            {
                if (allTrees[i].name.StartsWith("Tree_Alien")) treeCount++;
                if (allTrees[i].name.StartsWith("Rock_Pulse")) rockCount++;
            }
            Assert.GreaterOrEqual(treeCount, 25, "Sector Beta forest must contain at least 25 AlienTimberTree nodes.");
            Assert.GreaterOrEqual(rockCount, 25, "Sector Gamma quarry must contain at least 25 MineralBoulder nodes.");

            // 4. Sector Alpha SOS Beacon & Wreckage
            var beacon = env.transform.Find("Beacon_SOS_Terminal");
            Assert.IsNotNull(beacon, "Beacon_SOS_Terminal must exist in Sector Alpha.");
            Assert.IsNotNull(beacon.GetComponent<BeaconObjective>(), "Beacon_SOS_Terminal must have BeaconObjective component.");

            var wreckage = env.transform.Find("Pod_Wreckage_Crashed");
            Assert.IsNotNull(wreckage, "Pod_Wreckage_Crashed must exist in Sector Alpha.");

            // 5. Sector Delta Prowler Hunting Grounds
            var enemies = GameObject.Find("Enemies");
            Assert.IsNotNull(enemies, "Enemies root must exist.");
            Assert.GreaterOrEqual(enemies.transform.childCount, 4, "Sector Delta must have at least 4 active prowler enemies.");
        }

        [Test, Order(4)]
        public void Scene_Enemies_3DPredatorBeastVisualsAndPawsAligned()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var enemies = GameObject.Find("Enemies");
            Assert.IsNotNull(enemies, "Enemies root must exist.");

            for (int i = 0; i < enemies.transform.childCount; i++)
            {
                Transform enemy = enemies.transform.GetChild(i);
                Transform vis = enemy.Find("Visual_AlienPredatorBeast");
                Assert.IsNotNull(vis, $"Enemy '{enemy.name}' must have Visual_AlienPredatorBeast 3D child.");

                Renderer[] r = vis.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(r.Length, 0, $"AlienPredatorBeast visual on '{enemy.name}' must contain active Renderers.");

                // Assert paws elevated above ground
                Assert.GreaterOrEqual(vis.localPosition.y, 0.40f, "Beast visual localPosition Y must be elevated to align paws to ground.");
            }
        }

        [Test, Order(5)]
        public void Scene_HUDCanvas_ConfiguredWithBeaconTerminalAndEndScreens()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var canvas = GameObject.Find("HUD_Canvas");
            Assert.IsNotNull(canvas, "HUD_Canvas must exist.");

            // Beacon Terminal UI
            var termUI = canvas.GetComponent<BeaconTerminalUI>();
            Assert.IsNotNull(termUI, "HUD_Canvas must have BeaconTerminalUI component.");
            Assert.IsNotNull(termUI.TerminalPanel, "BeaconTerminalUI must reference TerminalPanel.");
            Assert.IsNotNull(termUI.CountdownBanner, "BeaconTerminalUI must reference CountdownBanner.");
            Assert.IsNotNull(termUI.RepairButton, "BeaconTerminalUI must reference RepairButton.");
            Assert.IsNotNull(termUI.ActivateExtractionButton, "BeaconTerminalUI must reference ActivateExtractionButton.");

            // Mission End UI
            var endUI = canvas.GetComponent<MissionEndUI>();
            Assert.IsNotNull(endUI, "HUD_Canvas must have MissionEndUI component.");
            Assert.IsNotNull(endUI.VictoryPanel, "MissionEndUI must reference VictoryPanel.");
            Assert.IsNotNull(endUI.DefeatPanel, "MissionEndUI must reference DefeatPanel.");

            // SaveSystem in Scene
            var saveSysObj = GameObject.Find("SaveSystem");
            Assert.IsNotNull(saveSysObj, "SaveSystem GameObject must exist in scene.");
            Assert.IsNotNull(saveSysObj.GetComponent<SaveSystem>(), "SaveSystem component must exist.");

            // MissionStatistics in Scene
            var statsObj = GameObject.Find("MissionStatistics");
            Assert.IsNotNull(statsObj, "MissionStatistics GameObject must exist in scene.");
            Assert.IsNotNull(statsObj.GetComponent<MissionStatistics>(), "MissionStatistics component must exist.");
        }

        // =========================================================================
        // 2. BeaconObjective Unit & Progression Logic
        // =========================================================================

        [Test]
        public void BeaconObjective_InitialState_UnrepairedAndNotCountingDown()
        {
            Assert.AreEqual(0, beacon.CurrentStage);
            Assert.IsFalse(beacon.IsRepaired);
            Assert.IsFalse(beacon.IsCountdownActive);
            Assert.IsFalse(beacon.IsVictory);
            Assert.AreEqual(0f, beacon.StageProgress, 0.001f);
        }

        [Test]
        public void BeaconObjective_CanRepairCurrentStage_ValidatesIngredients()
        {
            // Empty inventory
            Assert.IsFalse(beacon.CanRepairCurrentStage(inventory));

            // Partial ingredients: 4 ore required, only 3 provided
            inventory.AddItem(oreItem, 3);
            inventory.AddItem(scrapItem, 3);
            Assert.IsFalse(beacon.CanRepairCurrentStage(inventory));

            // Exact ingredients provided
            inventory.AddItem(oreItem, 1); // now 4
            Assert.IsTrue(beacon.CanRepairCurrentStage(inventory));
        }

        [Test]
        public void BeaconObjective_TryRepairCurrentStage_ConsumesIngredientsAndAdvancesStage()
        {
            inventory.AddItem(oreItem, 4);
            inventory.AddItem(scrapItem, 3);

            bool repairedStage1 = beacon.TryRepairCurrentStage(inventory);
            Assert.IsTrue(repairedStage1);
            Assert.AreEqual(1, beacon.CurrentStage);
            Assert.AreEqual(0, inventory.GetItemCount(oreItem));
            Assert.AreEqual(0, inventory.GetItemCount(scrapItem));

            // Now on Stage 2: requires 6 wood + 4 ore
            Assert.IsFalse(beacon.CanRepairCurrentStage(inventory));
            inventory.AddItem(timberItem, 6);
            inventory.AddItem(oreItem, 4);

            bool repairedStage2 = beacon.TryRepairCurrentStage(inventory);
            Assert.IsTrue(repairedStage2);
            Assert.AreEqual(2, beacon.CurrentStage);

            // Now on Stage 3: requires 3 meat + 5 scrap
            inventory.AddItem(meatItem, 3);
            inventory.AddItem(scrapItem, 5);

            bool repairedStage3 = beacon.TryRepairCurrentStage(inventory);
            Assert.IsTrue(repairedStage3);
            Assert.AreEqual(3, beacon.CurrentStage);
            Assert.IsTrue(beacon.IsRepaired);
            Assert.AreEqual(1.0f, beacon.StageProgress, 0.001f);
        }

        [Test]
        public void BeaconObjective_ExtractionActivation_FailsBeforeRepair_SucceedsAfterRepair()
        {
            // Fails when not repaired
            beacon.SetStageDirect(0);
            Assert.IsFalse(beacon.TryActivateExtraction());

            beacon.SetStageDirect(2);
            Assert.IsFalse(beacon.TryActivateExtraction());

            // Succeeds when all 3 stages repaired
            beacon.SetStageDirect(3);
            Assert.IsTrue(beacon.IsRepaired);
            Assert.IsTrue(beacon.TryActivateExtraction());

            Assert.IsTrue(beacon.IsCountdownActive);
            Assert.AreEqual(60f, beacon.CountdownRemaining, 0.01f);

            // Duplicate activation fails while counting down
            Assert.IsFalse(beacon.TryActivateExtraction());
        }

        [Test]
        public void BeaconObjective_ExtractionCountdown_DecrementsAndTriggersVictory()
        {
            beacon.SetStageDirect(3);
            beacon.TryActivateExtraction();
            Assert.IsTrue(beacon.IsCountdownActive);

            bool victoryFired = false;
            beacon.OnVictoryAchieved += () => victoryFired = true;

            // Tick 30 seconds
            beacon.TickCountdown(30f);
            Assert.AreEqual(30f, beacon.CountdownRemaining, 0.01f);
            Assert.IsFalse(beacon.IsVictory);
            Assert.IsFalse(victoryFired);

            // Tick remaining 30 seconds
            beacon.TickCountdown(30.1f);
            Assert.AreEqual(0f, beacon.CountdownRemaining);
            Assert.IsFalse(beacon.IsCountdownActive);
            Assert.IsTrue(beacon.IsVictory);
            Assert.IsTrue(victoryFired);
        }

        // =========================================================================
        // 3. SaveSystem & State Persistence Roundtrip
        // =========================================================================

        [Test]
        public void SaveData_SerializationRoundtrip_Fidelity()
        {
            SaveData original = new SaveData
            {
                timestamp = 1234567890L,
                playerPosition = new Vector3Data(new Vector3(-25.4f, 0.05f, 33.2f)),
                playerRotationY = 135f,
                playerHealth = 82.5f,
                playerHunger = 64.0f,
                playerStamina = 91.5f,
                playerBodyTemperature = 35.8f,
                beaconRepairStage = 2,
                isBeaconCountdownActive = true,
                beaconCountdownRemaining = 42.5f,
                dayCount = 3,
                normalizedDiurnalTime = 0.45f,
                enemiesSlain = 7,
                resourcesHarvested = 19,
                structuresBuilt = 4
            };

            original.inventorySlots.Add(new SavedInventorySlot("Item_Wood", 14));
            original.inventorySlots.Add(new SavedInventorySlot("Item_Stone", 8));
            original.inventorySlots.Add(new SavedInventorySlot("Item_Scrap", 5));

            original.placedStructures.Add(new SavedStructure(StructureType.Barricade, new Vector3(10f, 0f, 10f), 45f, 120f, 150f));
            original.placedStructures.Add(new SavedStructure(StructureType.HeatBeacon, new Vector3(15f, 0f, 12f), 0f, 150f, 150f));

            string json = saveSystem.SerializeToJson(original);
            Assert.IsFalse(string.IsNullOrEmpty(json), "Serialized JSON string must not be empty.");

            SaveData restored = saveSystem.DeserializeFromJson(json);
            Assert.IsNotNull(restored, "Deserialized SaveData must not be null.");

            // Assert exact matching values
            Assert.AreEqual(original.playerPosition.x, restored.playerPosition.x, 0.001f);
            Assert.AreEqual(original.playerPosition.y, restored.playerPosition.y, 0.001f);
            Assert.AreEqual(original.playerPosition.z, restored.playerPosition.z, 0.001f);
            Assert.AreEqual(original.playerRotationY, restored.playerRotationY, 0.001f);
            Assert.AreEqual(original.playerHealth, restored.playerHealth, 0.001f);
            Assert.AreEqual(original.playerHunger, restored.playerHunger, 0.001f);
            Assert.AreEqual(original.playerStamina, restored.playerStamina, 0.001f);
            Assert.AreEqual(original.playerBodyTemperature, restored.playerBodyTemperature, 0.001f);

            Assert.AreEqual(original.beaconRepairStage, restored.beaconRepairStage);
            Assert.AreEqual(original.isBeaconCountdownActive, restored.isBeaconCountdownActive);
            Assert.AreEqual(original.beaconCountdownRemaining, restored.beaconCountdownRemaining, 0.01f);

            Assert.AreEqual(original.dayCount, restored.dayCount);
            Assert.AreEqual(original.normalizedDiurnalTime, restored.normalizedDiurnalTime, 0.001f);

            Assert.AreEqual(original.enemiesSlain, restored.enemiesSlain);
            Assert.AreEqual(original.resourcesHarvested, restored.resourcesHarvested);
            Assert.AreEqual(original.structuresBuilt, restored.structuresBuilt);

            Assert.AreEqual(original.inventorySlots.Count, restored.inventorySlots.Count);
            Assert.AreEqual("Item_Wood", restored.inventorySlots[0].itemId);
            Assert.AreEqual(14, restored.inventorySlots[0].quantity);

            Assert.AreEqual(original.placedStructures.Count, restored.placedStructures.Count);
            Assert.AreEqual(StructureType.Barricade, restored.placedStructures[0].structureType);
            Assert.AreEqual(120f, restored.placedStructures[0].currentDurability, 0.01f);
        }

        [Test]
        public void SaveSystem_SaveAndLoadFile_RoundtripFilePersistence()
        {
            string testSavePath = Path.Combine(Application.persistentDataPath, "test_roundtrip_phase9.json");
            try
            {
                if (File.Exists(testSavePath)) File.Delete(testSavePath);

                // Setup state
                playerStats.transform.position = new Vector3(-15f, 0.05f, 25f);
                playerStats.SetStatsDirect(65f, 50f, 75f, 33.5f);
                inventory.AddItem(oreItem, 12);
                beacon.SetStageDirect(1);
                statistics.SetStats(2, 5, 14, 3);

                // Save
                bool saved = saveSystem.SaveGame(testSavePath);
                Assert.IsTrue(saved, "SaveGame must return true on valid write.");
                Assert.IsTrue(File.Exists(testSavePath), "Save file must exist on disk.");

                // Mutate state
                playerStats.transform.position = Vector3.zero;
                playerStats.SetStatsDirect(100f, 100f, 100f, 37f);
                inventory.InitializeSlots();
                beacon.SetStageDirect(0);
                statistics.ResetStats();

                // Load
                bool loaded = saveSystem.LoadGame(testSavePath);
                Assert.IsTrue(loaded, "LoadGame must return true on valid read.");

                // Assert restored state
                Assert.AreEqual(-15f, playerStats.transform.position.x, 0.01f);
                Assert.AreEqual(25f, playerStats.transform.position.z, 0.01f);
                Assert.AreEqual(65f, playerStats.CurrentHealth, 0.1f);
                Assert.AreEqual(50f, playerStats.CurrentHunger, 0.1f);
                Assert.AreEqual(33.5f, playerStats.CurrentBodyTemperature, 0.1f);
                Assert.AreEqual(12, inventory.GetItemCount(oreItem));
                Assert.AreEqual(1, beacon.CurrentStage);
                Assert.AreEqual(5, statistics.EnemiesSlain);
                Assert.AreEqual(14, statistics.ResourcesHarvested);
            }
            finally
            {
                if (File.Exists(testSavePath)) File.Delete(testSavePath);
            }
        }

        // =========================================================================
        // 4. Mission Statistics & End UI
        // =========================================================================

        [Test]
        public void MissionStatistics_Recording_TracksAccurateMetrics()
        {
            statistics.ResetStats();
            Assert.AreEqual(0, statistics.EnemiesSlain);
            Assert.AreEqual(0, statistics.ResourcesHarvested);
            Assert.AreEqual(0, statistics.StructuresBuilt);

            statistics.RecordEnemySlain();
            statistics.RecordEnemySlain();
            Assert.AreEqual(2, statistics.EnemiesSlain);

            statistics.RecordResourceHarvested();
            statistics.RecordResourceHarvested();
            statistics.RecordResourceHarvested();
            Assert.AreEqual(3, statistics.ResourcesHarvested);

            statistics.RecordStructureBuilt();
            Assert.AreEqual(1, statistics.StructuresBuilt);
        }

        [Test]
        public void MissionEndUI_FormatStatisticsSummary_OutputsFormattedReport()
        {
            statistics.SetStats(4, 11, 28, 6);

            var endUIObj = new GameObject("EndUI_Test");
            endUIObj.transform.SetParent(testRoot.transform);
            var endUI = endUIObj.AddComponent<MissionEndUI>();
            var statsField = typeof(MissionEndUI).GetField("targetStatistics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            statsField.SetValue(endUI, statistics);

            string report = endUI.FormatStatisticsSummary();
            Assert.IsTrue(report.Contains("Days Survived: 4"));
            Assert.IsTrue(report.Contains("Enemies Slain: 11"));
            Assert.IsTrue(report.Contains("Resources Harvested: 28"));
            Assert.IsTrue(report.Contains("Structures Built: 6"));
        }
    }
}
#endif
