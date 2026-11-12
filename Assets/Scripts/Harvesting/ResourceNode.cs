using System;
using System.Collections;
using UnityEngine;
using Outpost404.Combat;
using Outpost404.Inventory;

namespace Outpost404.Harvesting
{
    /// <summary>
    /// Harvestable world node (trees, ore veins, alien flora).
    /// Implements IDamageable so player attacks and tools can damage and harvest materials.
    /// Spawns physics loot pickups upon depletion.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IDamageable
    {
        [Header("Node Configuration")]
        [SerializeField] private ResourceNodeSO nodeData;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool destroyOnDepletion = true;
        [SerializeField] private bool spawnLootOnDepletion = true;

        [Header("Feedback Animation")]
        [SerializeField] private float wobbleDuration = 0.15f;
        [SerializeField] private float wobbleMagnitude = 0.12f;

        private Vector3 originalScale;
        private Coroutine wobbleCoroutine;
        private bool isDead = false;
        private Collider nodeCollider;

        public event Action<ResourceNode> OnNodeDepleted;

        public ResourceNodeSO NodeData => nodeData;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => nodeData != null ? nodeData.MaxHealth : 50f;
        public bool IsDead => isDead;
        public bool SpawnLootOnDepletion { get => spawnLootOnDepletion; set => spawnLootOnDepletion = value; }

        private void Awake()
        {
            originalScale = transform.localScale;
            nodeCollider = GetComponent<Collider>();
            InitializeNode();
        }

        public void InitializeNode()
        {
            isDead = false;
            currentHealth = MaxHealth;
            if (nodeCollider != null) nodeCollider.enabled = true;
        }

        public void Configure(ResourceNodeSO data)
        {
            nodeData = data;
            InitializeNode();
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHealth -= amount;
            PlayHitWobble();

            if (currentHealth <= 0.0001f)
            {
                DepleteNode();
            }
        }

        private void PlayHitWobble()
        {
            if (!Application.isPlaying || !gameObject.activeInHierarchy) return;
            if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
            wobbleCoroutine = StartCoroutine(DoWobble());
        }

        private IEnumerator DoWobble()
        {
            float elapsed = 0f;
            while (elapsed < wobbleDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / wobbleDuration;
                float scaleOffset = Mathf.Sin(progress * Mathf.PI * 2f) * wobbleMagnitude * (1f - progress);
                transform.localScale = originalScale + new Vector3(scaleOffset, -scaleOffset, scaleOffset);
                yield return null;
            }
            transform.localScale = originalScale;
            wobbleCoroutine = null;
        }

        public void DepleteNode()
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;

            SpawnLoot();
            OnNodeDepleted?.Invoke(this);
            Outpost404.Objective.MissionStatistics.Instance?.RecordResourceHarvested();

            if (nodeCollider != null) nodeCollider.enabled = false;

            if (destroyOnDepletion && Application.isPlaying)
            {
                Destroy(gameObject, 0.05f);
            }
        }

        public void SpawnLoot()
        {
            if (!spawnLootOnDepletion || nodeData == null || nodeData.DropItem == null) return;

            int count = UnityEngine.Random.Range(nodeData.MinDropCount, nodeData.MaxDropCount + 1);

            if (nodeData.LootPickupPrefab != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-0.8f, 0.8f),
                        0.5f,
                        UnityEngine.Random.Range(-0.8f, 0.8f)
                    );
                    GameObject drop = Instantiate(nodeData.LootPickupPrefab, transform.position + offset, Quaternion.identity);
                    var pickup = drop.GetComponent<LootPickup>();
                    if (pickup != null)
                    {
                        pickup.Initialize(nodeData.DropItem, 1);
                    }
                }
            }
            else
            {
                // Procedural primitive fallback loot drop
                for (int i = 0; i < count; i++)
                {
                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-0.8f, 0.8f),
                        0.4f,
                        UnityEngine.Random.Range(-0.8f, 0.8f)
                    );
                    GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    drop.name = $"Drop_{nodeData.DropItem.ItemName}";
                    drop.transform.position = transform.position + offset;
                    drop.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

                    var pickup = drop.AddComponent<LootPickup>();
                    pickup.Initialize(nodeData.DropItem, 1);
                }
            }
        }
    }
}
