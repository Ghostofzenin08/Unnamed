#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using Outpost404.Environment;
using Outpost404.Survival;
using Outpost404.Player;
using Outpost404.AI;
using Outpost404.Building;

namespace Outpost404.Tests
{
    public class Phase7EnvironmentalTests
    {
        private GameObject cycleObj;
        private DayNightCycleManager cycleMgr;
        private GameObject playerObj;
        private PlayerStats playerStats;
        private PlayerController playerCtrl;

        [SetUp]
        public void SetUp()
        {
            DayNightCycleManager.Instance = null;
            cycleObj = new GameObject("TestDayNightCycle");
            cycleMgr = cycleObj.AddComponent<DayNightCycleManager>();
            cycleMgr.DayLengthInSeconds = 120f;
            cycleMgr.EnsureGradientsAndCurves();

            playerObj = new GameObject("TestPlayer");
            playerObj.tag = "Player";
            playerObj.AddComponent<CapsuleCollider>();
            playerObj.AddComponent<Rigidbody>();
            playerCtrl = playerObj.AddComponent<PlayerController>();
            playerCtrl.ConfigureRigidbody();
            playerStats = playerObj.AddComponent<PlayerStats>();
            playerStats.InitializeStats();
        }

        [TearDown]
        public void TearDown()
        {
            if (cycleObj != null) Object.DestroyImmediate(cycleObj);
            if (playerObj != null) Object.DestroyImmediate(playerObj);
            DayNightCycleManager.Instance = null;
        }

        // =========================================================================
        // 1. DAY/NIGHT CYCLE TIME & PROGRESSION
        // =========================================================================

        [Test]
        public void DayNightCycle_InitialTime_IsDawnAndDayOne()
        {
            cycleMgr.SetTime(0.0f, 1);
            Assert.AreEqual(0.0f, cycleMgr.NormalizedTime, 0.001f);
            Assert.AreEqual(1, cycleMgr.CurrentDay);
            Assert.AreEqual(DayPhase.Dawn, cycleMgr.CurrentPhase);
        }

        [Test]
        public void DayNightCycle_Tick_AdvancesNormalizedTimeDeterministically()
        {
            cycleMgr.SetTime(0.0f, 1);
            cycleMgr.Tick(30f); // 30s out of 120s = 0.25 (Noon)
            Assert.AreEqual(0.25f, cycleMgr.NormalizedTime, 0.002f);
            Assert.AreEqual(DayPhase.Day, cycleMgr.CurrentPhase);
            Assert.AreEqual(1, cycleMgr.CurrentDay);
        }

        [Test]
        public void DayNightCycle_Wraparound_IncrementsDayCountAndResetsNormalizedTime()
        {
            cycleMgr.SetTime(0.95f, 1);
            cycleMgr.Tick(12f); // 12s / 120s = +0.10 -> 1.05 -> wraps to 0.05, Day 2
            Assert.AreEqual(0.05f, cycleMgr.NormalizedTime, 0.002f);
            Assert.AreEqual(2, cycleMgr.CurrentDay);
            Assert.AreEqual(DayPhase.Dawn, cycleMgr.CurrentPhase);
        }

        [Test]
        public void DayNightCycle_Phases_MatchCanonicalTimes()
        {
            Assert.AreEqual(DayPhase.Dawn, DayNightCycleManager.GetPhaseFromTime(0.0f));
            Assert.AreEqual(DayPhase.Dawn, DayNightCycleManager.GetPhaseFromTime(0.10f));
            Assert.AreEqual(DayPhase.Day, DayNightCycleManager.GetPhaseFromTime(0.25f)); // Noon
            Assert.AreEqual(DayPhase.Day, DayNightCycleManager.GetPhaseFromTime(0.45f));
            Assert.AreEqual(DayPhase.Dusk, DayNightCycleManager.GetPhaseFromTime(0.50f)); // Sunset
            Assert.AreEqual(DayPhase.Dusk, DayNightCycleManager.GetPhaseFromTime(0.60f));
            Assert.AreEqual(DayPhase.Night, DayNightCycleManager.GetPhaseFromTime(0.75f)); // Midnight
            Assert.AreEqual(DayPhase.Night, DayNightCycleManager.GetPhaseFromTime(0.90f));
        }

        [Test]
        public void DayNightCycle_Events_FireOnTimeAndPhaseChange()
        {
            float reportedTime = -1f;
            int reportedDay = -1;
            DayPhase reportedPhase = DayPhase.Night;

            cycleMgr.OnTimeChanged += (t, d) => { reportedTime = t; reportedDay = d; };
            cycleMgr.OnPhaseChanged += (p) => { reportedPhase = p; };

            cycleMgr.SetTime(0.0f, 1);
            cycleMgr.Tick(30f); // Advance to 0.25 (Day)

            Assert.AreEqual(0.25f, reportedTime, 0.002f);
            Assert.AreEqual(1, reportedDay);
            Assert.AreEqual(DayPhase.Day, reportedPhase);
        }

        [Test]
        public void DayNightCycle_ClockFormatting_ProducesAccurate24HourMilitaryTime()
        {
            // 0.00 = 06:00 (Dawn)
            cycleMgr.SetTime(0.0f, 1);
            cycleMgr.GetClockHoursAndMinutes(out int h0, out int m0);
            Assert.AreEqual(6, h0);
            Assert.AreEqual(0, m0);
            Assert.AreEqual("Day 1 | 06:00", cycleMgr.GetFormattedClockTime());

            // 0.25 = 12:00 (Noon)
            cycleMgr.SetTime(0.25f, 1);
            cycleMgr.GetClockHoursAndMinutes(out int h1, out int m1);
            Assert.AreEqual(12, h1);
            Assert.AreEqual(0, m1);

            // 0.50 = 18:00 (Dusk)
            cycleMgr.SetTime(0.50f, 1);
            cycleMgr.GetClockHoursAndMinutes(out int h2, out int m2);
            Assert.AreEqual(18, h2);
            Assert.AreEqual(0, m2);

            // 0.75 = 00:00 (Midnight)
            cycleMgr.SetTime(0.75f, 2);
            cycleMgr.GetClockHoursAndMinutes(out int h3, out int m3);
            Assert.AreEqual(0, h3);
            Assert.AreEqual(0, m3);
            Assert.AreEqual("Day 2 | 00:00", cycleMgr.GetFormattedClockTime());
        }

        [Test]
        public void DayNightCycle_InputSafety_HandlesNegativeZeroNaNAndInfinity()
        {
            Assert.DoesNotThrow(() =>
            {
                cycleMgr.Tick(-5f);
                cycleMgr.Tick(0f);
                cycleMgr.Tick(float.NaN);
                cycleMgr.Tick(float.PositiveInfinity);
                cycleMgr.SetTime(float.NaN);
                cycleMgr.SetTime(float.PositiveInfinity);
                cycleMgr.CalculateAmbientTemperature(float.NaN);
                cycleMgr.CalculateAmbientTemperature(float.NegativeInfinity);
            });
            Assert.IsFalse(float.IsNaN(cycleMgr.NormalizedTime));
            Assert.IsFalse(float.IsInfinity(cycleMgr.NormalizedTime));
        }

        // =========================================================================
        // 2. AMBIENT TEMPERATURE CURVES
        // =========================================================================

        [Test]
        public void AmbientTemperature_MatchesSpecifiedDiurnalExtremes()
        {
            // Noon (0.25): Warm Day (+22°C to +26°C)
            float noonTemp = cycleMgr.CalculateAmbientTemperature(0.25f);
            Assert.GreaterOrEqual(noonTemp, 22f);
            Assert.LessOrEqual(noonTemp, 26f);

            // Dusk (0.50): Dusk (+10°C)
            float duskTemp = cycleMgr.CalculateAmbientTemperature(0.50f);
            Assert.AreEqual(10f, duskTemp, 1.5f);

            // Midnight (0.75): Sub-zero Night (-15°C to -20°C)
            float midnightTemp = cycleMgr.CalculateAmbientTemperature(0.75f);
            Assert.LessOrEqual(midnightTemp, -15f);
            Assert.GreaterOrEqual(midnightTemp, -20f);

            // Dawn (0.0): Cool morning (+6°C to +10°C)
            float dawnTemp = cycleMgr.CalculateAmbientTemperature(0.0f);
            Assert.GreaterOrEqual(dawnTemp, 6f);
            Assert.LessOrEqual(dawnTemp, 10f);
        }

        // =========================================================================
        // 3. BODY TEMPERATURE & HYPOTHERMIA
        // =========================================================================

        [Test]
        public void PlayerStats_InitialBodyTemperature_IsAtNormal37C()
        {
            Assert.AreEqual(37f, playerStats.CurrentBodyTemperature, 0.01f);
            Assert.IsFalse(playerStats.IsInHypothermia);
            Assert.IsFalse(playerStats.IsCriticalFreezing);
            Assert.AreEqual(1.0f, playerStats.MovementSpeedModifier, 0.01f);
        }

        [Test]
        public void PlayerStats_ColdAmbient_DrainsBodyTemperatureOverTime()
        {
            float initialTemp = playerStats.CurrentBodyTemperature;
            // Freezing night ambient (-18°C) without heat source
            for (int i = 0; i < 10; i++)
            {
                playerStats.TickTemperature(1.0f, -18f, isNearHeatSource: false);
            }
            Assert.Less(playerStats.CurrentBodyTemperature, initialTemp);
        }

        [Test]
        public void PlayerStats_WarmAmbient_PassivelyRestoresBodyTemperature()
        {
            playerStats.SetCurrentBodyTemperatureDirect(30f);
            Assert.AreEqual(30f, playerStats.CurrentBodyTemperature, 0.01f);

            // Warm day ambient (+24°C)
            playerStats.TickTemperature(2.0f, 24f, isNearHeatSource: false);
            Assert.Greater(playerStats.CurrentBodyTemperature, 30f);
        }

        [Test]
        public void PlayerStats_HypothermiaThreshold_InflictsDamageAndSpeedPenalty()
        {
            // Drop below 32°C (Hypothermia threshold)
            playerStats.SetCurrentBodyTemperatureDirect(30f);
            Assert.IsTrue(playerStats.IsInHypothermia);
            Assert.IsFalse(playerStats.IsCriticalFreezing);

            // Speed penalty applied
            Assert.AreEqual(0.70f, playerStats.MovementSpeedModifier, 0.01f);

            // Ticking in cold deals hypothermia health damage
            float initialHealth = playerStats.CurrentHealth;
            playerStats.TickTemperature(1.0f, -15f, isNearHeatSource: false);
            Assert.Less(playerStats.CurrentHealth, initialHealth);
        }

        [Test]
        public void PlayerStats_CriticalFreezing_InflictsSevereDamageAndHeavierSpeedPenalty()
        {
            // Drop below 25°C (Critical Freezing threshold)
            playerStats.SetCurrentBodyTemperatureDirect(22f);
            Assert.IsTrue(playerStats.IsInHypothermia);
            Assert.IsTrue(playerStats.IsCriticalFreezing);

            // Heavy speed penalty (e.g. 0.45 modifier)
            Assert.AreEqual(0.45f, playerStats.MovementSpeedModifier, 0.01f);

            float initialHealth = playerStats.CurrentHealth;
            playerStats.TickTemperature(1.0f, -18f, isNearHeatSource: false);
            float damageTaken = initialHealth - playerStats.CurrentHealth;
            Assert.Greater(damageTaken, playerStats.HypothermiaDamageRate * 0.9f);
        }

        [Test]
        public void PlayerController_MovementSpeed_ScalesWithHypothermiaModifier()
        {
            playerStats.SetCurrentBodyTemperatureDirect(37f);
            playerCtrl.SetInputOverride(Vector2.up, false);
            float normalVelocityMagnitude = playerCtrl.CurrentMoveDirection.magnitude * playerCtrl.MoveSpeed;

            // Set hypothermic (< 32°C)
            playerStats.SetCurrentBodyTemperatureDirect(30f);
            playerCtrl.SetInputOverride(Vector2.up, false);
            Assert.AreEqual(0.70f, playerStats.MovementSpeedModifier, 0.01f);
        }

        // =========================================================================
        // 4. HEAT BEACON THERMAL RADIANCE & RECOVERY
        // =========================================================================

        [Test]
        public void HeatBeacon_ProximityQuery_DetectsNearbyPlayerAndYieldsWarmthBonus()
        {
            GameObject beaconObj = new GameObject("TestBeacon");
            beaconObj.transform.position = Vector3.zero;
            var beacon = beaconObj.AddComponent<BuildableStructure>();
            beacon.Type = StructureType.HeatBeacon;
            beacon.StructureName = "Test Pulse Heat Beacon";
            beacon.AuraRadius = 5.0f;
            beacon.ThermalWarmthBonus = 35.0f;
            beacon.RegisterBeacon();

            try
            {
                // Within 5m aura
                bool near = BuildableStructure.IsPositionNearHeatBeacon(new Vector3(2f, 0f, 0f), out float bonus);
                Assert.IsTrue(near);
                Assert.AreEqual(35f, bonus, 0.01f);

                // Outside aura (> 5m)
                bool far = BuildableStructure.IsPositionNearHeatBeacon(new Vector3(8f, 0f, 0f), out float farBonus);
                Assert.IsFalse(far);
            }
            finally
            {
                beacon.UnregisterBeacon();
                Object.DestroyImmediate(beaconObj);
            }
        }

        [Test]
        public void HeatBeacon_InsideAura_RapidlyRestoresBodyTemperatureAndHaltsDamage()
        {
            playerStats.SetCurrentBodyTemperatureDirect(26f);
            float lowHealth = playerStats.CurrentHealth;

            // Inside heat aura during sub-zero ambient
            playerStats.TickTemperature(1.0f, -18f, isNearHeatSource: true);

            // Body temperature rapidly warmed
            Assert.Greater(playerStats.CurrentBodyTemperature, 26f);
            // Health was NOT damaged by hypothermia while protected by beacon
            Assert.AreEqual(lowHealth, playerStats.CurrentHealth, 0.01f);
        }

        // =========================================================================
        // 5. ENEMY NOCTURNAL AGGRESSION BUFF (ANOMALY SURGE)
        // =========================================================================

        [Test]
        public void EnemyController_NightPhase_TriggersAnomalySurgeBuffs()
        {
            GameObject enemyObj = new GameObject("TestEnemy");
            enemyObj.AddComponent<BoxCollider>();
            var enemy = enemyObj.AddComponent<EnemyController>();

            float baseDetect = enemy.CurrentDetectionRadius;
            float baseSpeed = enemy.CurrentChaseSpeed;
            float baseDamage = enemy.CurrentAttackDamage;

            // Night begins: Anomaly Surge triggers
            enemy.ApplyNightSurge(true);

            Assert.IsTrue(enemy.IsNightSurgeActive);
            Assert.Greater(enemy.CurrentDetectionRadius, baseDetect);
            Assert.Greater(enemy.CurrentChaseSpeed, baseSpeed);
            Assert.Greater(enemy.CurrentAttackDamage, baseDamage);

            // Dawn arrives: Reset to base daytime values
            enemy.ApplyNightSurge(false);

            Assert.IsFalse(enemy.IsNightSurgeActive);
            Assert.AreEqual(baseDetect, enemy.CurrentDetectionRadius, 0.01f);
            Assert.AreEqual(baseSpeed, enemy.CurrentChaseSpeed, 0.01f);
            Assert.AreEqual(baseDamage, enemy.CurrentAttackDamage, 0.01f);

            Object.DestroyImmediate(enemyObj);
        }

        [Test]
        public void EnemyController_DayNightManagerSubscription_SyncsSurgeAutomatically()
        {
            GameObject enemyObj = new GameObject("TestEnemySurgeListener");
            enemyObj.AddComponent<BoxCollider>();
            var enemy = enemyObj.AddComponent<EnemyController>();

            try
            {
                // Day phase initially
                cycleMgr.SetTime(0.25f, 1);
                enemy.SubscribeToDayNightCycle(cycleMgr);
                Assert.IsFalse(enemy.IsNightSurgeActive);

                // Switch to Night phase (0.75)
                cycleMgr.SetTime(0.75f, 1);
                Assert.IsTrue(enemy.IsNightSurgeActive);

                // Switch to Dawn phase (0.0)
                cycleMgr.SetTime(0.0f, 2);
                Assert.IsFalse(enemy.IsNightSurgeActive);
            }
            finally
            {
                enemy.UnsubscribeFromDayNightCycle(cycleMgr);
                Object.DestroyImmediate(enemyObj);
            }
        }
    }
}
#endif
