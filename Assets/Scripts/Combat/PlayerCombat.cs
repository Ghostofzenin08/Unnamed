using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Outpost404.Survival;

namespace Outpost404.Combat
{
    /// <summary>
    /// Executes player melee attacks with cooldown throttling, stamina consumption,
    /// and spatial overlap detection against IDamageable entities (Enemies and Resource Nodes).
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Attributes")]
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float attackCooldown = 0.45f;
        [SerializeField] private float staminaCost = 12f;

        [Header("Hit Detection Box / Sphere")]
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackRadius = 1.1f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private float lastAttackTime = -99f;
        private PlayerStats playerStats;

        public event Action OnAttackExecuted;
        public event Action<IDamageable, float> OnTargetHit;

        public float AttackDamage { get => attackDamage; set => attackDamage = Mathf.Max(0f, value); }
        public float AttackCooldown { get => attackCooldown; set => attackCooldown = Mathf.Max(0.05f, value); }
        public float StaminaCost { get => staminaCost; set => staminaCost = Mathf.Max(0f, value); }
        public PlayerStats TargetStats
        {
            get
            {
                if (playerStats == null) playerStats = GetComponent<PlayerStats>();
                return playerStats;
            }
            set => playerStats = value;
        }

        public bool CanAttack => (Time.time >= lastAttackTime + attackCooldown) &&
                                 (TargetStats == null || TargetStats.CurrentStamina >= staminaCost);

        private void Awake()
        {
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            var stats = TargetStats;
            if (stats != null && stats.IsDead) return;

            bool attackRequested = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                attackRequested = true;
            }
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                attackRequested = true;
            }
            if (Mouse.current == null && Gamepad.current == null)
            {
                attackRequested = Input.GetMouseButtonDown(0);
            }
#else
            attackRequested = Input.GetMouseButtonDown(0);
#endif

            if (attackRequested)
            {
                TryAttack();
            }
        }

        /// <summary>
        /// Executes attack logic. Returns true if the attack successfully fired.
        /// Publicly callable for deterministic automated unit testing.
        /// </summary>
        public bool TryAttack()
        {
            if (!CanAttack) return false;

            // Stamina consumption
            var stats = TargetStats;
            if (stats != null)
            {
                if (!stats.ConsumeStamina(staminaCost))
                {
                    return false;
                }
            }

            lastAttackTime = Time.time;
            OnAttackExecuted?.Invoke();

            ExecuteHitDetection();
            return true;
        }

        /// <summary>
        /// Overlap sphere in the forward attack direction.
        /// </summary>
        public int ExecuteHitDetection()
        {
            Physics.SyncTransforms();
            Vector3 hitCenter = transform.position + (transform.forward * attackRange) + (Vector3.up * 0.5f);
            Collider[] hits = Physics.OverlapSphere(hitCenter, attackRadius, hitLayers, QueryTriggerInteraction.Ignore);

            HashSet<IDamageable> hitEntities = new HashSet<IDamageable>();
            int targetsHit = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                // Ignore self
                if (hits[i].gameObject == gameObject || hits[i].transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable damageable = hits[i].GetComponent<IDamageable>();
                if (damageable == null && hits[i].attachedRigidbody != null)
                {
                    damageable = hits[i].attachedRigidbody.GetComponent<IDamageable>();
                }
                if (damageable == null)
                {
                    damageable = hits[i].GetComponentInParent<IDamageable>();
                }

                if (damageable != null && !damageable.IsDead && hitEntities.Add(damageable))
                {
                    Vector3 closestPoint = hits[i].ClosestPoint(hitCenter);
                    damageable.TakeDamage(attackDamage, closestPoint, -transform.forward);
                    OnTargetHit?.Invoke(damageable, attackDamage);
                    targetsHit++;
                }
            }

            return targetsHit;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 hitCenter = transform.position + (transform.forward * attackRange) + (Vector3.up * 0.5f);
            Gizmos.DrawWireSphere(hitCenter, attackRadius);
        }
    }
}
