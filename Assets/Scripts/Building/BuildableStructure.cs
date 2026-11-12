using System;
using UnityEngine;
using UnityEngine.AI;
using Outpost404.Combat;
using Outpost404.Survival;

namespace Outpost404.Building
{
    public enum StructureType
    {
        Barricade,
        HeatBeacon,
        Custom
    }

    /// <summary>
    /// Base component attached to player-constructed structures (Barricades, Pulse Campfires/Heat Beacons).
    /// Implements IDamageable with durability, supports repairs, NavMesh path blocking,
    /// and area-of-effect environmental benefits (e.g. regenerative warmth radius).
    /// </summary>
    public class BuildableStructure : MonoBehaviour, IDamageable
    {
        [Header("Structure Classification")]
        [SerializeField] private StructureType structureType = StructureType.Barricade;
        [SerializeField] private string structureName = "Reinforced Barricade";

        [Header("Durability & Health")]
        [SerializeField] private float maxDurability = 150f;
        [SerializeField] private float currentDurability = 150f;

        [Header("Placement Box Extents")]
        [Tooltip("Half-extents of the placement bounding box used for collision clearance.")]
        [SerializeField] private Vector3 placementExtents = new Vector3(1.2f, 1.0f, 0.4f);

        [Header("Heat Beacon / Pulse Campfire Aura")]
        [SerializeField] private float auraRadius = 5.0f;
        [SerializeField] private float healthRegenPerSecond = 6.0f;
        [SerializeField] private float thermalWarmthBonus = 35.0f;
        [SerializeField] private Light auraLight;

        private bool isDead = false;
        private static readonly System.Collections.Generic.List<BuildableStructure> activeHeatBeacons = new System.Collections.Generic.List<BuildableStructure>();

        public event Action<BuildableStructure, float> OnDurabilityChanged;
        public event Action<BuildableStructure> OnStructureDestroyed;

        public StructureType Type
        {
            get => structureType;
            set
            {
                structureType = value;
                if (structureType == StructureType.HeatBeacon) RegisterBeacon();
                else UnregisterBeacon();
            }
        }
        public string StructureName { get => structureName; set => structureName = value; }
        public float MaxDurability { get => maxDurability; set => maxDurability = Mathf.Max(1f, value); }
        public float CurrentDurability => currentDurability;
        public float CurrentHealth => currentDurability;
        public float MaxHealth => maxDurability;
        public float DurabilityFraction => maxDurability > 0f ? Mathf.Clamp01(currentDurability / maxDurability) : 0f;
        public Vector3 PlacementExtents { get => placementExtents; set => placementExtents = value; }
        public float AuraRadius { get => auraRadius; set => auraRadius = Mathf.Max(0.5f, value); }
        public float HealthRegenPerSecond { get => healthRegenPerSecond; set => healthRegenPerSecond = Mathf.Max(0f, value); }
        public float ThermalWarmthBonus { get => thermalWarmthBonus; set => thermalWarmthBonus = value; }
        public bool IsDead => isDead;
        public static System.Collections.Generic.IReadOnlyList<BuildableStructure> ActiveHeatBeacons => activeHeatBeacons;

        private void OnEnable()
        {
            RegisterBeacon();
        }

        private void OnDisable()
        {
            UnregisterBeacon();
        }

        public void RegisterBeacon()
        {
            if (structureType == StructureType.HeatBeacon && !activeHeatBeacons.Contains(this))
            {
                activeHeatBeacons.Add(this);
            }
        }

        public void UnregisterBeacon()
        {
            activeHeatBeacons.Remove(this);
        }

        public static bool IsPositionNearHeatBeacon(Vector3 position, out float warmthBonus)
        {
            warmthBonus = 0f;
            bool found = false;
            for (int i = 0; i < activeHeatBeacons.Count; i++)
            {
                var beacon = activeHeatBeacons[i];
                if (beacon != null && !beacon.IsDead && beacon.gameObject.activeInHierarchy)
                {
                    float dist = Vector3.Distance(position, beacon.transform.position);
                    if (dist <= beacon.AuraRadius)
                    {
                        warmthBonus = Mathf.Max(warmthBonus, beacon.ThermalWarmthBonus);
                        found = true;
                    }
                }
            }
            return found;
        }

        private void Awake()
        {
            InitializeStructure();
        }

        public void InitializeStructure()
        {
            isDead = false;
            currentDurability = maxDurability;
            if (auraLight == null) auraLight = GetComponentInChildren<Light>();
        }

        private void Update()
        {
            if (isDead) return;

            if (structureType == StructureType.HeatBeacon && healthRegenPerSecond > 0f)
            {
                ApplyHeatBeaconAura();
            }
        }

        /// <summary>
        /// Regenerates nearby player health while within the safe zone beacon radius.
        /// Ignores duplicate hits from multiple child colliders on the same player.
        /// </summary>
        public void ApplyHeatBeaconAura(float deltaTime = -1f)
        {
            float dt = deltaTime >= 0f ? deltaTime : (Time.deltaTime > 0.0001f ? Time.deltaTime : 1.0f);
            Collider[] colliders = Physics.OverlapSphere(transform.position, auraRadius);
            System.Collections.Generic.HashSet<PlayerStats> healed = new System.Collections.Generic.HashSet<PlayerStats>();
            for (int i = 0; i < colliders.Length; i++)
            {
                var stats = colliders[i].GetComponent<PlayerStats>() ?? colliders[i].GetComponentInParent<PlayerStats>();
                if (stats != null && !stats.IsDead && stats.CurrentHealth < stats.MaxHealth && healed.Add(stats))
                {
                    stats.Heal(healthRegenPerSecond * dt);
                }
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

            currentDurability = Mathf.Max(0f, currentDurability - amount);
            OnDurabilityChanged?.Invoke(this, currentDurability);

            if (currentDurability <= 0.0001f)
            {
                DestroyStructure();
            }
        }

        public void Repair(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;

            currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
            OnDurabilityChanged?.Invoke(this, currentDurability);
        }

        public void SetCurrentDurabilityDirect(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount)) return;
            currentDurability = Mathf.Clamp(amount, 0f, maxDurability);
            if (currentDurability <= 0.0001f)
            {
                isDead = true;
            }
        }

        private void DestroyStructure()
        {
            if (isDead) return;
            isDead = true;
            currentDurability = 0f;

            OnStructureDestroyed?.Invoke(this);

            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = false;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (Application.isPlaying)
            {
                Destroy(gameObject, 0.15f);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (structureType == StructureType.HeatBeacon)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, auraRadius);
            }

            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * placementExtents.y, placementExtents * 2f);
        }
    }
}
