using UnityEngine;
using Outpost404.Inventory;

namespace Outpost404.Harvesting
{
    /// <summary>
    /// World loot drop entity. Features subtle floating/bobbing animations and trigger collection
    /// into the player's InventorySystem.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LootPickup : MonoBehaviour
    {
        [Header("Drop Properties")]
        [SerializeField] private ItemDataSO itemData;
        [SerializeField] private int quantity = 1;

        [Header("Animation Settings")]
        [SerializeField] private float bobFrequency = 2.5f;
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 initialPosition;
        private float animationSeed;
        private Collider col;

        public ItemDataSO ItemData => itemData;
        public int Quantity { get => quantity; set => quantity = Mathf.Max(1, value); }

        private void Awake()
        {
            col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            initialPosition = transform.position;
            animationSeed = Random.Range(0f, 100f);
        }

        private void Start()
        {
            ApplyVisualColor();
        }

        private void Update()
        {
            // Hover bobbing and slow rotation for high readability in top-down view
            float newY = initialPosition.y + Mathf.Sin((Time.time + animationSeed) * bobFrequency) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }

        public void Initialize(ItemDataSO item, int count)
        {
            itemData = item;
            quantity = count;
            initialPosition = transform.position;
            ApplyVisualColor();
        }

        public void ApplyVisualColor()
        {
            if (itemData == null) return;
            var renderer = GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", itemData.ItemColor);
                renderer.SetPropertyBlock(propBlock);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var inventory = other.GetComponent<InventorySystem>();
            if (inventory == null && other.attachedRigidbody != null)
            {
                inventory = other.attachedRigidbody.GetComponent<InventorySystem>();
            }

            if (inventory != null)
            {
                Collect(inventory);
            }
        }

        public bool Collect(InventorySystem inventory)
        {
            if (inventory == null || itemData == null || quantity <= 0) return false;

            if (inventory.AddItem(itemData, quantity, out int remaining))
            {
                Outpost404.Audio.AudioManager.Instance?.PlayPickupChime();

                if (remaining <= 0)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        DestroyImmediate(gameObject);
                    }
                    return true;
                }
                else
                {
                    quantity = remaining;
                    return true;
                }
            }
            return false;
        }
    }
}
