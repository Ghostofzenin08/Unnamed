using UnityEngine;

namespace Outpost404.Inventory
{
    public enum ItemType
    {
        Resource,
        Consumable,
        Equipment,
        Material,
        Structure
    }

    /// <summary>
    /// ScriptableObject defining an immutable item definition across the survival sandbox.
    /// Used by inventory, loot tables, resource nodes, and crafting recipes.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "Outpost404/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "item_id";
        [SerializeField] private string itemName = "New Item";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Item description.";
        [SerializeField] private ItemType itemType = ItemType.Resource;

        [Header("Stacking & Storage")]
        [SerializeField] private int maxStack = 64;

        [Header("Survival Properties")]
        [Tooltip("Amount of hunger restored when consumed.")]
        [SerializeField] private float caloricValue = 0f;

        [Tooltip("Amount of health restored when consumed.")]
        [SerializeField] private float healValue = 0f;

        [Header("Tool & Construction Properties")]
        [Tooltip("Multiplier applied to resource damage when harvesting.")]
        [SerializeField] private float harvestMultiplier = 1.0f;

        [Tooltip("Prefab spawned when placing this structure.")]
        [SerializeField] private GameObject structurePrefab;

        [Header("Visual Tint")]
        [SerializeField] private Color itemColor = Color.white;

        public string ItemId { get => itemId; set => itemId = value; }
        public string ItemName { get => itemName; set => itemName = value; }
        public string Description { get => description; set => description = value; }
        public ItemType ItemType { get => itemType; set => itemType = value; }
        public int MaxStack { get => Mathf.Max(1, maxStack); set => maxStack = Mathf.Max(1, value); }
        public float CaloricValue { get => caloricValue; set => caloricValue = value; }
        public float HealValue { get => healValue; set => healValue = value; }
        public float HarvestMultiplier { get => Mathf.Max(1.0f, harvestMultiplier); set => harvestMultiplier = Mathf.Max(1.0f, value); }
        public GameObject StructurePrefab { get => structurePrefab; set => structurePrefab = value; }
        public Color ItemColor { get => itemColor; set => itemColor = value; }
    }
}
