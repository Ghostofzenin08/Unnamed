using NUnit.Framework;
using UnityEngine;
using Outpost404.Survival;

namespace Outpost404.Tests
{
    public class SurvivalStatsTests
    {
        private GameObject playerObj;
        private PlayerStats stats;

        [SetUp]
        public void SetUp()
        {
            playerObj = new GameObject("TestPlayerStats");
            stats = playerObj.AddComponent<PlayerStats>();
            stats.InitializeStats();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObj != null)
            {
                Object.DestroyImmediate(playerObj);
            }
        }

        [Test]
        public void InitialStats_AreAtMaximumAndNotDead()
        {
            Assert.AreEqual(stats.MaxHealth, stats.CurrentHealth);
            Assert.AreEqual(stats.MaxStamina, stats.CurrentStamina);
            Assert.AreEqual(stats.MaxHunger, stats.CurrentHunger);
            Assert.IsFalse(stats.IsDead);
            Assert.IsTrue(stats.CanSprint);
        }

        [Test]
        public void Tick_DepletesHungerOverTime()
        {
            float initialHunger = stats.CurrentHunger;
            stats.Tick(2.0f, isSprinting: false);
            Assert.Less(stats.CurrentHunger, initialHunger);
        }

        [Test]
        public void Tick_SprintingDepletesHungerFasterThanWalking()
        {
            stats.InitializeStats();
            stats.Tick(1.0f, isSprinting: false);
            float walkingDrain = stats.MaxHunger - stats.CurrentHunger;

            stats.InitializeStats();
            stats.Tick(1.0f, isSprinting: true);
            float sprintingDrain = stats.MaxHunger - stats.CurrentHunger;

            Assert.Greater(sprintingDrain, walkingDrain);
        }

        [Test]
        public void Tick_SprintingDrainsStamina()
        {
            float initialStamina = stats.CurrentStamina;
            stats.Tick(1.0f, isSprinting: true);
            Assert.Less(stats.CurrentStamina, initialStamina);
        }

        [Test]
        public void Tick_RegeneratesStaminaAfterDelay()
        {
            stats.ConsumeStamina(50f);
            float drainedStamina = stats.CurrentStamina;

            // Tick for duration longer than stamina regen delay
            stats.Tick(2.5f, isSprinting: false);
            Assert.Greater(stats.CurrentStamina, drainedStamina);
        }

        [Test]
        public void Tick_ZeroHungerCausesStarvationDamage()
        {
            stats.SetCurrentHungerDirect(0f);
            float initialHealth = stats.CurrentHealth;

            stats.Tick(1.0f, isSprinting: false);
            Assert.Less(stats.CurrentHealth, initialHealth);
        }

        [Test]
        public void Tick_HighHungerRegeneratesDamagedHealth()
        {
            stats.TakeDamage(30f);
            float damagedHealth = stats.CurrentHealth;

            stats.Tick(2.0f, isSprinting: false);
            Assert.Greater(stats.CurrentHealth, damagedHealth);
        }

        [Test]
        public void TakeDamage_DeductsHealthAndFiresEvent()
        {
            bool eventFired = false;
            float reportedHealth = 0f;
            stats.OnHealthChanged += (curr, max) =>
            {
                eventFired = true;
                reportedHealth = curr;
            };

            stats.TakeDamage(25f);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(75f, reportedHealth, 0.01f);
            Assert.AreEqual(75f, stats.CurrentHealth, 0.01f);
        }

        [Test]
        public void TakeDamage_FatalDamageTriggersDeathAndEvent()
        {
            bool diedEventFired = false;
            stats.OnPlayerDied += () => diedEventFired = true;

            stats.TakeDamage(150f);
            Assert.IsTrue(stats.IsDead);
            Assert.AreEqual(0f, stats.CurrentHealth);
            Assert.IsTrue(diedEventFired);
            Assert.IsFalse(stats.CanSprint);
        }

        [Test]
        public void ConsumeStamina_InsufficientStamina_ReturnsFalse()
        {
            stats.ConsumeStamina(95f);
            bool success = stats.ConsumeStamina(20f);
            Assert.IsFalse(success);
        }

        [Test]
        public void RestoreHunger_ClampsToMaxHunger()
        {
            stats.ConsumeHunger(30f);
            stats.RestoreHunger(100f);
            Assert.AreEqual(stats.MaxHunger, stats.CurrentHunger);
        }

        [Test]
        public void Stats_NaNInputs_GracefullyIgnoredWithoutCorruptingState()
        {
            float healthBefore = stats.CurrentHealth;
            float staminaBefore = stats.CurrentStamina;
            float hungerBefore = stats.CurrentHunger;

            stats.TakeDamage(float.NaN);
            stats.Heal(float.NaN);
            stats.ConsumeStamina(float.NaN);
            stats.RestoreStamina(float.NaN);
            stats.ConsumeHunger(float.NaN);
            stats.RestoreHunger(float.NaN);
            stats.Tick(float.NaN, false);
            stats.SetCurrentHealthDirect(float.NaN);

            Assert.IsFalse(float.IsNaN(stats.CurrentHealth), "Health must never become NaN.");
            Assert.IsFalse(float.IsNaN(stats.CurrentStamina), "Stamina must never become NaN.");
            Assert.IsFalse(float.IsNaN(stats.CurrentHunger), "Hunger must never become NaN.");
            Assert.AreEqual(healthBefore, stats.CurrentHealth);
            Assert.AreEqual(staminaBefore, stats.CurrentStamina);
            Assert.AreEqual(hungerBefore, stats.CurrentHunger);
        }
    }
}
