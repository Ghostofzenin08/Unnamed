using UnityEngine;
using Outpost404.Harvesting;
using Outpost404.Inventory;

namespace Outpost404.Player
{
    /// <summary>
    /// Handles player interaction with world entities (picking up loot, interacting with stations).
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRadius = 2.5f;
        [SerializeField] private LayerMask interactableLayers = ~0;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private InventorySystem inventory;

        private void Awake()
        {
            inventory = GetComponent<InventorySystem>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }
        }

        public bool TryInteract()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, interactRadius, interactableLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var pickup = hits[i].GetComponent<LootPickup>();
                if (pickup != null && inventory != null)
                {
                    if (pickup.Collect(inventory))
                    {
                        return true;
                    }
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
