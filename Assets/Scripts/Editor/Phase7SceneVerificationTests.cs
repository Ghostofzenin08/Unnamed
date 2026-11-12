#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Outpost404.Environment;
using Outpost404.Player;
using Outpost404.Survival;
using Outpost404.AI;
using Outpost404.Building;
using Outpost404.UI;
using Outpost404.EditorTools;

namespace Outpost404.Tests
{
    public class Phase7SceneVerificationTests
    {
        [Test, Order(1)]
        public void Scene_BuildPhase7Scene_ExecutesCleanlyAndSaves()
        {
            Assert.DoesNotThrow(() =>
            {
                Phase7SceneSetup.BuildPhase7Scene();
            }, "Phase7SceneSetup.BuildPhase7Scene must execute without errors.");
        }

        [Test, Order(2)]
        public void Scene_DayNightCycle_ConfiguredWithDirectionalSunAndCurves()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var cycle = Object.FindFirstObjectByType<DayNightCycleManager>();
            Assert.IsNotNull(cycle, "DayNightCycleManager component must exist in scene.");

            Assert.IsNotNull(cycle.SunLight, "DayNightCycleManager must have SunLight assigned.");
            Assert.AreEqual(LightType.Directional, cycle.SunLight.type, "SunLight must be a Directional Light.");
            Assert.Greater(cycle.DayLengthInSeconds, 0f, "DayLengthInSeconds must be positive.");

            // Verify ambient temperature calculation
            float noon = cycle.CalculateAmbientTemperature(0.25f);
            float midnight = cycle.CalculateAmbientTemperature(0.75f);
            Assert.Greater(noon, 20f, "Noon ambient temperature must be warm (>20°C).");
            Assert.Less(midnight, -10f, "Midnight ambient temperature must be sub-zero (<-10°C).");
        }

        [Test, Order(3)]
        public void Scene_Player_ConfiguredWithHypothermiaAndThermalStats()
        {
            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player with tag 'Player' must exist in scene.");

            var stats = player.GetComponent<PlayerStats>();
            Assert.IsNotNull(stats, "Player must have PlayerStats component.");
            Assert.AreEqual(37f, stats.NormalBodyTemperature, 0.01f);
            Assert.AreEqual(32f, stats.HypothermiaThreshold, 0.01f);
            Assert.AreEqual(25f, stats.CriticalFreezingThreshold, 0.01f);

            var ctrl = player.GetComponent<PlayerController>();
            Assert.IsNotNull(ctrl, "Player must have PlayerController component.");
        }

        [Test, Order(4)]
        public void Scene_Enemies_ConfiguredForNocturnalSurge()
        {
            var enemies = GameObject.Find("Enemies");
            Assert.IsNotNull(enemies, "Enemies root must exist.");
            Assert.GreaterOrEqual(enemies.transform.childCount, 2, "Enemies must have at least 2 active prowlers.");

            for (int i = 0; i < enemies.transform.childCount; i++)
            {
                var enemyObj = enemies.transform.GetChild(i).gameObject;
                var enemyCtrl = enemyObj.GetComponent<EnemyController>();
                Assert.IsNotNull(enemyCtrl, $"Enemy {enemyObj.name} must have EnemyController.");

                float dayDetect = enemyCtrl.CurrentDetectionRadius;
                enemyCtrl.ApplyNightSurge(true);
                float nightDetect = enemyCtrl.CurrentDetectionRadius;
                Assert.Greater(nightDetect, dayDetect, $"Enemy {enemyObj.name} detection radius must expand during Night Anomaly Surge.");
                enemyCtrl.ApplyNightSurge(false);
            }
        }

        [Test, Order(5)]
        public void Scene_HUD_ConfiguredWithClockWeatherAndThermometer()
        {
            var canvas = GameObject.Find("HUD_Canvas");
            Assert.IsNotNull(canvas, "HUD_Canvas must exist.");

            var hud = canvas.GetComponent<SurvivalHUD>();
            Assert.IsNotNull(hud, "HUD_Canvas must have SurvivalHUD component.");

            Transform clockPanel = canvas.transform.Find("ClockWeatherPanel");
            Assert.IsNotNull(clockPanel, "HUD_Canvas must contain ClockWeatherPanel.");

            Transform clockText = clockPanel.Find("ClockText");
            Assert.IsNotNull(clockText, "ClockWeatherPanel must have ClockText.");
            Assert.IsNotNull(clockText.GetComponent<Text>(), "ClockText must have UI Text component.");

            Transform ambText = clockPanel.Find("AmbientTempText");
            Assert.IsNotNull(ambText, "ClockWeatherPanel must have AmbientTempText.");
            Assert.IsNotNull(ambText.GetComponent<Text>(), "AmbientTempText must have UI Text component.");

            Transform statsPanel = canvas.transform.Find("StatsPanel");
            Assert.IsNotNull(statsPanel, "StatsPanel must exist.");

            Transform thermoBar = statsPanel.Find("BodyTemperatureBar");
            Assert.IsNotNull(thermoBar, "StatsPanel must have BodyTemperatureBar.");
            Assert.IsNotNull(thermoBar.GetComponent<Slider>(), "BodyTemperatureBar must have Slider component.");

            Transform frostBanner = canvas.transform.Find("FrostWarningBanner");
            Assert.IsNotNull(frostBanner, "HUD_Canvas must have FrostWarningBanner.");
            Assert.IsFalse(frostBanner.gameObject.activeSelf, "FrostWarningBanner must be initially inactive.");
        }

        [Test, Order(6)]
        public void Scene_BuildableStructures_HeatBeaconConfiguredWithThermalWarmth()
        {
            GameObject heatBeaconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Structures/Prefab_HeatBeacon.prefab");
            Assert.IsNotNull(heatBeaconPrefab, "Prefab_HeatBeacon must exist.");

            var structure = heatBeaconPrefab.GetComponent<BuildableStructure>();
            Assert.IsNotNull(structure, "Prefab_HeatBeacon must have BuildableStructure component.");
            Assert.AreEqual(StructureType.HeatBeacon, structure.Type);
            Assert.Greater(structure.AuraRadius, 0f, "AuraRadius must be positive.");
            Assert.GreaterOrEqual(structure.ThermalWarmthBonus, 30f, "ThermalWarmthBonus must provide at least +30°C warmth.");
        }
    }
}
#endif
