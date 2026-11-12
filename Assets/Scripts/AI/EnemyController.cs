using System;
using UnityEngine;
using UnityEngine.AI;
using Outpost404.Combat;
using Outpost404.Harvesting;

namespace Outpost404.AI
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    /// <summary>
    /// Autonomous enemy controller utilizing a robust NavMeshAgent Finite State Machine:
    /// Patrol -> Chase -> Attack -> Die.
    /// Implements IDamageable for combat interactions.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Stats Configuration")]
        [SerializeField] private EnemyStatsSO stats;

        [Header("State & Targeting")]
        [SerializeField] private EnemyState currentState = EnemyState.Patrol;
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private Transform[] patrolWaypoints;

        [Header("Night Anomaly Surge Configuration")]
        [SerializeField] private bool isNightSurgeActive = false;
        [SerializeField] private float nightDetectionMultiplier = 1.67f; // e.g. 12m -> 20m
        [SerializeField] private float nightChaseSpeedMultiplier = 1.20f; // +20%
        [SerializeField] private float nightAttackDamageMultiplier = 1.25f; // +25%

        private NavMeshAgent agent;
        private Collider col;
        private float currentHealth;
        private float attackTimer = 0f;
        private float waitTimer = 0f;
        private int currentWaypointIndex = 0;
        private Vector3 spawnOrigin;
        private bool isDead = false;
        private Renderer[] cachedRenderers;
        private Coroutine hitFlashCoroutine;

        public event Action<EnemyState> OnStateChanged;
        public event Action OnEnemyDied;
        public event Action OnAttackLanded;
        public event Action<bool> OnNightSurgeChanged;

        public EnemyState CurrentState => currentState;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => stats != null ? stats.MaxHealth : 60f;
        public bool IsDead => isDead;
        public Transform TargetPlayer => targetPlayer;
        public bool IsNightSurgeActive => isNightSurgeActive;
        public float CurrentDetectionRadius => (stats != null ? stats.DetectionRadius : 12f) * (isNightSurgeActive ? nightDetectionMultiplier : 1.0f);
        public float CurrentLoseSightRadius
        {
            get
            {
                float baseLose = stats != null ? stats.LoseSightRadius : 14f;
                float scaledLose = baseLose * (isNightSurgeActive ? nightDetectionMultiplier : 1.0f);
                return Mathf.Max(CurrentDetectionRadius * 1.2f, scaledLose);
            }
        }
        public float CurrentChaseSpeed => (stats != null ? stats.ChaseSpeed : 4.8f) * (isNightSurgeActive ? nightChaseSpeedMultiplier : 1.0f);
        public float CurrentAttackDamage => (stats != null ? stats.AttackDamage : 15f) * (isNightSurgeActive ? nightAttackDamageMultiplier : 1.0f);
        public float NightDetectionMultiplier { get => nightDetectionMultiplier; set => nightDetectionMultiplier = value; }
        public float NightChaseSpeedMultiplier { get => nightChaseSpeedMultiplier; set => nightChaseSpeedMultiplier = value; }
        public float NightAttackDamageMultiplier { get => nightAttackDamageMultiplier; set => nightAttackDamageMultiplier = value; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            col = GetComponent<Collider>();
            spawnOrigin = transform.position;
            InitializeEnemy();
        }

        public void Configure(EnemyStatsSO newStats)
        {
            stats = newStats;
            InitializeEnemy();
        }

        public void InitializeEnemy()
        {
            isDead = false;
            currentHealth = MaxHealth;
            attackTimer = 0f;
            waitTimer = 0f;
            currentState = EnemyState.Patrol;

            if (col != null) col.enabled = true;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = stats != null ? stats.PatrolSpeed : 2.5f;
            }
        }

        private void OnEnable()
        {
            SubscribeToDayNightCycle();
        }

        private void OnDisable()
        {
            UnsubscribeFromDayNightCycle();
        }

        public void SubscribeToDayNightCycle(Outpost404.Environment.DayNightCycleManager mgr = null)
        {
            var targetMgr = mgr != null ? mgr : Outpost404.Environment.DayNightCycleManager.Instance;
            if (targetMgr != null)
            {
                targetMgr.OnPhaseChanged -= HandleDayPhaseChanged;
                targetMgr.OnPhaseChanged += HandleDayPhaseChanged;
                ApplyNightSurge(targetMgr.CurrentPhase == Outpost404.Environment.DayPhase.Night);
            }
        }

        public void UnsubscribeFromDayNightCycle(Outpost404.Environment.DayNightCycleManager mgr = null)
        {
            var targetMgr = mgr != null ? mgr : Outpost404.Environment.DayNightCycleManager.Instance;
            if (targetMgr != null)
            {
                targetMgr.OnPhaseChanged -= HandleDayPhaseChanged;
            }
        }

        public void ApplyNightSurge(bool active)
        {
            if (isNightSurgeActive == active) return;
            isNightSurgeActive = active;

            if (currentState == EnemyState.Chase && agent != null && agent.isOnNavMesh)
            {
                agent.speed = CurrentChaseSpeed;
            }

            OnNightSurgeChanged?.Invoke(isNightSurgeActive);
        }

        private void HandleDayPhaseChanged(Outpost404.Environment.DayPhase phase)
        {
            ApplyNightSurge(phase == Outpost404.Environment.DayPhase.Night);
        }

        private void Start()
        {
            FindPlayerIfNull();
        }

        private void Update()
        {
            TickFSM(Time.deltaTime);
        }

        /// <summary>
        /// Explicit deterministic FSM tick logic, callable by automated tests.
        /// </summary>
        public void TickFSM(float deltaTime)
        {
            if (isDead || currentState == EnemyState.Dead) return;

            FindPlayerIfNull();

            // Advance attack cooldown
            if (attackTimer > 0f)
            {
                attackTimer -= deltaTime;
            }

            float distToPlayer = targetPlayer != null
                ? Vector3.Distance(transform.position, targetPlayer.position)
                : float.MaxValue;

            switch (currentState)
            {
                case EnemyState.Patrol:
                    UpdatePatrolState(distToPlayer, deltaTime);
                    break;

                case EnemyState.Chase:
                    UpdateChaseState(distToPlayer);
                    break;

                case EnemyState.Attack:
                    UpdateAttackState(distToPlayer);
                    break;
            }
        }

        private bool IsPlayerValidAndAlive()
        {
            if (targetPlayer == null) return false;
            IDamageable playerDamageable = targetPlayer.GetComponentInParent<IDamageable>() ?? targetPlayer.GetComponent<IDamageable>();
            if (playerDamageable != null && playerDamageable.IsDead) return false;
            return true;
        }

        private void UpdatePatrolState(float distToPlayer, float deltaTime)
        {
            float detectRadius = CurrentDetectionRadius;
            if (IsPlayerValidAndAlive() && distToPlayer <= detectRadius)
            {
                TransitionTo(EnemyState.Chase);
                return;
            }

            if (agent == null || !agent.isOnNavMesh) return;

            if (agent.isStopped)
            {
                agent.isStopped = false;
            }
            agent.speed = stats != null ? stats.PatrolSpeed : 2.5f;

            if (patrolWaypoints != null && patrolWaypoints.Length > 0)
            {
                Transform wp = patrolWaypoints[currentWaypointIndex];
                if (wp != null)
                {
                    agent.SetDestination(wp.position);
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                    {
                        waitTimer += deltaTime;
                        float waitDuration = stats != null ? stats.WaypointWaitDuration : 2f;
                        if (waitTimer >= waitDuration)
                        {
                            waitTimer = 0f;
                            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
                        }
                    }
                }
            }
            else
            {
                // Wander around spawn origin
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                {
                    waitTimer += deltaTime;
                    float waitDuration = stats != null ? stats.WaypointWaitDuration : 2f;
                    if (waitTimer >= waitDuration)
                    {
                        waitTimer = 0f;
                        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * (stats != null ? stats.PatrolRadius : 6f);
                        randomDirection += spawnOrigin;
                        randomDirection.y = spawnOrigin.y;
                        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                        {
                            agent.SetDestination(hit.position);
                        }
                    }
                }
            }
        }

        private void UpdateChaseState(float distToPlayer)
        {
            float attackRange = stats != null ? stats.AttackRange : 1.8f;
            float loseRadius = CurrentLoseSightRadius;

            if (!IsPlayerValidAndAlive() || distToPlayer > loseRadius)
            {
                TransitionTo(EnemyState.Patrol);
                return;
            }

            if (distToPlayer <= attackRange)
            {
                TransitionTo(EnemyState.Attack);
                return;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = CurrentChaseSpeed;
                agent.SetDestination(targetPlayer.position);
            }
        }

        private void UpdateAttackState(float distToPlayer)
        {
            float attackRange = stats != null ? stats.AttackRange : 1.8f;

            if (!IsPlayerValidAndAlive())
            {
                TransitionTo(EnemyState.Patrol);
                return;
            }

            if (distToPlayer > (attackRange * 1.35f))
            {
                TransitionTo(EnemyState.Chase);
                return;
            }

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            // Face target player during attack
            Vector3 lookDirection = targetPlayer.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(lookDirection),
                    360f * Time.deltaTime
                );
            }

            if (attackTimer <= 0f)
            {
                ExecuteAttack();
            }
        }

        public void ExecuteAttack()
        {
            float cd = stats != null ? stats.AttackCooldown : 1.25f;
            float damage = CurrentAttackDamage;
            attackTimer = cd;

            if (targetPlayer != null)
            {
                IDamageable playerDamageable = targetPlayer.GetComponent<IDamageable>();
                if (playerDamageable != null && !playerDamageable.IsDead)
                {
                    playerDamageable.TakeDamage(damage, targetPlayer.position, (targetPlayer.position - transform.position).normalized);
                    OnAttackLanded?.Invoke();
                }
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHealth -= amount;
            PlayHitFeedback(hitNormal);

            // Retaliate: switch to chase if struck while idling or patrolling
            if (currentState == EnemyState.Patrol && targetPlayer != null)
            {
                TransitionTo(EnemyState.Chase);
            }

            if (currentHealth <= 0.0001f)
            {
                Die();
            }
        }

        public void PlayHitFeedback(Vector3 hitNormal = default)
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;

            if (hitNormal.sqrMagnitude > 0.01f && agent != null && agent.isOnNavMesh)
            {
                agent.Move(hitNormal.normalized * 0.22f);
            }

            if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
            hitFlashCoroutine = StartCoroutine(DoHitFlash());
        }

        private System.Collections.IEnumerator DoHitFlash()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
                cachedRenderers = GetComponentsInChildren<Renderer>();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].GetPropertyBlock(block);
                    block.SetColor("_Color", new Color(1.5f, 0.4f, 0.4f, 1f));
                    cachedRenderers[i].SetPropertyBlock(block);
                }
            }

            yield return new WaitForSeconds(0.08f);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                {
                    cachedRenderers[i].SetPropertyBlock(null);
                }
            }
            hitFlashCoroutine = null;
        }

        public void Die()
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;
            TransitionTo(EnemyState.Dead);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }

            if (col != null) col.enabled = false;

            SpawnDropLoot();
            OnEnemyDied?.Invoke();
            Outpost404.Objective.MissionStatistics.Instance?.RecordEnemySlain();

            if (Application.isPlaying)
            {
                Destroy(gameObject, 0.1f);
            }
        }

        private void SpawnDropLoot()
        {
            if (stats == null || stats.DropItem == null) return;

            int count = UnityEngine.Random.Range(stats.MinDropCount, stats.MaxDropCount + 1);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    0.3f,
                    UnityEngine.Random.Range(-0.5f, 0.5f)
                );
                GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                drop.name = $"Drop_{stats.DropItem.ItemName}";
                drop.transform.position = transform.position + offset;
                drop.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                var pickup = drop.AddComponent<LootPickup>();
                pickup.Initialize(stats.DropItem, 1);
            }
        }

        public void TransitionTo(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            if (newState == EnemyState.Patrol)
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = stats != null ? stats.PatrolSpeed : 2.5f;
                }
            }

            OnStateChanged?.Invoke(newState);
        }

        public void SetTargetPlayer(Transform t)
        {
            targetPlayer = t;
        }

        public void SetWaypoints(Transform[] points)
        {
            patrolWaypoints = points;
        }

        private void FindPlayerIfNull()
        {
            if (targetPlayer != null) return;
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }
        }
    }
}
