using UnityEngine;
using Outpost404.Inventory;

namespace Outpost404.AI
{
    /// <summary>
    /// ScriptableObject defining parameters for autonomous AI enemies.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "Outpost404/Enemy Stats")]
    public class EnemyStatsSO : ScriptableObject
    {
        [Header("Identity & Health")]
        [SerializeField] private string enemyName = "Anomaly Prowler";
        [SerializeField] private float maxHealth = 60f;

        [Header("Locomotion & Navigation")]
        [SerializeField] private float patrolSpeed = 2.5f;
        [SerializeField] private float chaseSpeed = 4.8f;
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private float waypointWaitDuration = 2.0f;

        [Header("Perception & Detection")]
        [SerializeField] private float detectionRadius = 9f;
        [SerializeField] private float loseSightRadius = 14f;

        [Header("Combat & Offense")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1.25f;

        [Header("Loot & Rewards")]
        [SerializeField] private ItemDataSO dropItem;
        [SerializeField] private int minDropCount = 1;
        [SerializeField] private int maxDropCount = 3;

        public string EnemyName { get => enemyName; set => enemyName = value; }
        public float MaxHealth { get => Mathf.Max(1f, maxHealth); set => maxHealth = Mathf.Max(1f, value); }
        public float PatrolSpeed { get => patrolSpeed; set => patrolSpeed = value; }
        public float ChaseSpeed { get => chaseSpeed; set => chaseSpeed = value; }
        public float PatrolRadius { get => patrolRadius; set => patrolRadius = value; }
        public float WaypointWaitDuration { get => waypointWaitDuration; set => waypointWaitDuration = value; }
        public float DetectionRadius { get => detectionRadius; set => detectionRadius = value; }
        public float LoseSightRadius { get => loseSightRadius; set => loseSightRadius = value; }
        public float AttackRange { get => attackRange; set => attackRange = value; }
        public float AttackDamage { get => attackDamage; set => attackDamage = value; }
        public float AttackCooldown { get => attackCooldown; set => attackCooldown = value; }
        public ItemDataSO DropItem { get => dropItem; set => dropItem = value; }
        public int MinDropCount { get => Mathf.Max(1, minDropCount); set => minDropCount = Mathf.Max(1, value); }
        public int MaxDropCount { get => Mathf.Max(MinDropCount, maxDropCount); set => maxDropCount = Mathf.Max(MinDropCount, value); }
    }
}
