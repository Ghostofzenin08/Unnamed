using NUnit.Framework;
using UnityEngine;
using Outpost404.AI;
using Outpost404.Combat;
using Outpost404.Survival;

namespace Outpost404.Tests
{
    public class EnemyCombatTests
    {
        private GameObject enemyObj;
        private EnemyController enemy;
        private EnemyStatsSO statsSO;
        private GameObject playerObj;
        private PlayerStats playerStats;

        [SetUp]
        public void SetUp()
        {
            statsSO = ScriptableObject.CreateInstance<EnemyStatsSO>();
            statsSO.EnemyName = "Test Prowler";
            statsSO.MaxHealth = 50f;
            statsSO.DetectionRadius = 10f;
            statsSO.AttackRange = 2f;
            statsSO.AttackDamage = 15f;
            statsSO.AttackCooldown = 1.0f;
            statsSO.LoseSightRadius = 15f;
            statsSO.PatrolSpeed = 2f;
            statsSO.ChaseSpeed = 4f;

            enemyObj = new GameObject("TestEnemy");
            enemyObj.AddComponent<SphereCollider>();
            enemy = enemyObj.AddComponent<EnemyController>();
            enemy.Configure(statsSO);

            playerObj = new GameObject("TestPlayer");
            playerObj.tag = "Player";
            playerObj.AddComponent<CapsuleCollider>();
            playerStats = playerObj.AddComponent<PlayerStats>();
            playerStats.InitializeStats();

            enemy.SetTargetPlayer(playerObj.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (enemyObj != null) Object.DestroyImmediate(enemyObj);
            if (playerObj != null) Object.DestroyImmediate(playerObj);
            if (statsSO != null) Object.DestroyImmediate(statsSO);
        }

        [Test]
        public void EnemyFSM_InitialState_IsPatrol()
        {
            Assert.AreEqual(EnemyState.Patrol, enemy.CurrentState);
            Assert.IsFalse(enemy.IsDead);
            Assert.AreEqual(50f, enemy.CurrentHealth);
        }

        [Test]
        public void EnemyFSM_DetectsPlayerWithinRadius_TransitionsToChase()
        {
            enemyObj.transform.position = Vector3.zero;
            playerObj.transform.position = new Vector3(6f, 0f, 0f); // within 10f

            enemy.TickFSM(0.1f);

            Assert.AreEqual(EnemyState.Chase, enemy.CurrentState);
        }

        [Test]
        public void EnemyFSM_CloseToPlayer_TransitionsToAttack()
        {
            enemyObj.transform.position = Vector3.zero;
            playerObj.transform.position = new Vector3(1.2f, 0f, 0f); // within 2f

            enemy.TransitionTo(EnemyState.Chase);
            enemy.TickFSM(0.1f);

            Assert.AreEqual(EnemyState.Attack, enemy.CurrentState);
        }

        [Test]
        public void EnemyFSM_PlayerEscapes_TransitionsBackToPatrol()
        {
            enemyObj.transform.position = Vector3.zero;
            playerObj.transform.position = new Vector3(25f, 0f, 0f); // beyond loseSight 15f

            enemy.TransitionTo(EnemyState.Chase);
            enemy.TickFSM(0.1f);

            Assert.AreEqual(EnemyState.Patrol, enemy.CurrentState);
        }

        [Test]
        public void EnemyFSM_AttackDealsDamageToPlayer()
        {
            enemyObj.transform.position = Vector3.zero;
            playerObj.transform.position = new Vector3(1.0f, 0f, 0f);
            enemy.TransitionTo(EnemyState.Attack);

            float initialPlayerHealth = playerStats.CurrentHealth;
            enemy.ExecuteAttack();

            Assert.Less(playerStats.CurrentHealth, initialPlayerHealth);
            Assert.AreEqual(initialPlayerHealth - statsSO.AttackDamage, playerStats.CurrentHealth, 0.01f);
        }

        [Test]
        public void EnemyFSM_FatalDamage_TransitionsToDead()
        {
            bool diedEventFired = false;
            enemy.OnEnemyDied += () => diedEventFired = true;

            enemy.TakeDamage(100f);

            Assert.IsTrue(enemy.IsDead);
            Assert.AreEqual(EnemyState.Dead, enemy.CurrentState);
            Assert.AreEqual(0f, enemy.CurrentHealth);
            Assert.IsTrue(diedEventFired);
        }

        [Test]
        public void PlayerCombat_TryAttack_DrainsStaminaAndInflictsDamage()
        {
            var combat = playerObj.AddComponent<PlayerCombat>();
            combat.AttackDamage = 25f;
            combat.StaminaCost = 15f;
            combat.AttackCooldown = 0.5f;

            enemyObj.transform.position = playerObj.transform.position + (playerObj.transform.forward * 1.2f);
            float initialStamina = playerStats.CurrentStamina;
            float initialEnemyHealth = enemy.CurrentHealth;

            bool attacked = combat.TryAttack();

            Assert.IsTrue(attacked);
            Assert.Less(playerStats.CurrentStamina, initialStamina);
            Assert.AreEqual(initialEnemyHealth - 25f, enemy.CurrentHealth, 0.01f);
        }

        [Test]
        public void PlayerCombat_ExhaustedStamina_CannotAttack()
        {
            var combat = playerObj.AddComponent<PlayerCombat>();
            combat.StaminaCost = 30f;
            playerStats.ConsumeStamina(95f); // Leaves 5 stamina, cost is 30

            bool attacked = combat.TryAttack();
            Assert.IsFalse(attacked);
        }
    }
}
