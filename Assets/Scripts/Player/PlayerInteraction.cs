using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Outpost404.Harvesting;
using Outpost404.Inventory;
using Outpost404.Survival;

namespace Outpost404.Player
{
    /// <summary>
    /// Handles player interaction with world entities (picking up loot, interacting with and harvesting nodes, consuming food).
    /// Supports both legacy and new Unity Input Systems.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRadius = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode quickEatKey = KeyCode.Q;
        [SerializeField] private float harvestDamage = 20f;

        private InventorySystem inventory;
        private PlayerStats playerStats;

        public float InteractRadius { get => interactRadius; set => interactRadius = Mathf.Max(0.5f, value); }
        public float HarvestDamage { get => harvestDamage; set => harvestDamage = Mathf.Max(1f, value); }
        public InventorySystem Inventory { get => inventory; set => inventory = value; }

        /// <summary>
        /// Calculates effective harvest damage incorporating any tool harvest multipliers in inventory.
        /// </summary>
        public float GetEffectiveHarvestDamage()
        {
            float mult = 1.0f;
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (inventory != null && inventory.Slots != null)
            {
                for (int i = 0; i < inventory.Slots.Count; i++)
                {
                    var slot = inventory.Slots[i];
                    if (!slot.IsEmpty && slot.item != null && slot.item.HarvestMultiplier > mult)
                    {
                        mult = slot.item.HarvestMultiplier;
                    }
                }
            }
            return harvestDamage * mult;
        }

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (playerStats != null && playerStats.IsDead) return;

            bool interactPressed = false;
            bool eatPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame) interactPressed = true;
                if (Keyboard.current.qKey.wasPressedThisFrame) eatPressed = true;
            }

            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonNorth.wasPressedThisFrame)
                {
                    interactPressed = true;
                }
                if (Gamepad.current.dpad.up.wasPressedThisFrame)
                {
                    eatPressed = true;
                }
            }

            if (Keyboard.current == null && Gamepad.current == null)
            {
                interactPressed = Input.GetKeyDown(interactKey);
                eatPressed = Input.GetKeyDown(quickEatKey);
            }
#else
            interactPressed = Input.GetKeyDown(interactKey);
            eatPressed = Input.GetKeyDown(quickEatKey);
#endif

            if (interactPressed)
            {
                TryInteract();
            }

            if (eatPressed)
            {
                TryConsumeQuickFood();
            }
        }

        public bool TryInteract()
        {
            Physics.SyncTransforms();
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactableLayers, QueryTriggerInteraction.Collide);

            // 1. First priority: collect nearby loot pickups
            for (int i = 0; i < hits.Length; i++)
            {
                var pickup = hits[i].GetComponent<LootPickup>() ?? hits[i].GetComponentInParent<LootPickup>();
                if (pickup == null && hits[i].attachedRigidbody != null)
                {
                    pickup = hits[i].attachedRigidbody.GetComponent<LootPickup>();
                }

                if (pickup != null && inventory != null)
                {
                    if (pickup.Collect(inventory))
                    {
                        return true;
                    }
                }
            }

            // 2. Second priority: trigger-based harvesting of resource nodes (trees, rocks)
            for (int i = 0; i < hits.Length; i++)
            {
                var node = hits[i].GetComponent<ResourceNode>() ?? hits[i].GetComponentInParent<ResourceNode>();
                if (node == null && hits[i].attachedRigidbody != null)
                {
                    node = hits[i].attachedRigidbody.GetComponent<ResourceNode>();
                }

                if (node != null && !node.IsDead)
                {
                    Vector3 closestPoint = hits[i].ClosestPoint(transform.position);
                    Vector3 dir = (node.transform.position - transform.position).normalized;
                    node.TakeDamage(GetEffectiveHarvestDamage(), closestPoint, dir);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Consumes the first available food item in the player's inventory to restore hunger and health.
        /// </summary>
        public bool TryConsumeQuickFood()
        {
            if (inventory == null) inventory = GetComponent<InventorySystem>();
            if (playerStats == null) playerStats = GetComponent<PlayerStats>();
            if (inventory == null || playerStats == null || playerStats.IsDead) return false;

            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                var slot = inventory.Slots[i];
                if (!slot.IsEmpty && slot.item != null && slot.item.ItemType == ItemType.Consumable)
                {
                    return inventory.ConsumeItem(slot.item, playerStats);
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
