using UnityEngine;
using Outpost404.Inventory;

namespace Outpost404.Harvesting
{
    public enum HarvestToolType
    {
        None,
        Axe,
        Pickaxe,
        LaserCutter
    }

    /// <summary>
    /// ScriptableObject defining durability and drop properties of harvestable world nodes.
    /// </summary>
    [CreateAssetMenu(fileName = "Node_", menuName = "Outpost404/Resource Node Data")]
    public class ResourceNodeSO : ScriptableObject
    {
        [Header("Node Identity")]
        [SerializeField] private string nodeName = "Resource Node";
        [SerializeField] private float maxHealth = 60f;
        [SerializeField] private HarvestToolType requiredTool = HarvestToolType.None;

        [Header("Yield & Loot")]
        [SerializeField] private ItemDataSO dropItem;
        [SerializeField] private int minDropCount = 2;
        [SerializeField] private int maxDropCount = 5;

        [Tooltip("Prefab spawned when drops are scattered into the world.")]
        [SerializeField] private GameObject lootPickupPrefab;

        public string NodeName { get => nodeName; set => nodeName = value; }
        public float MaxHealth { get => maxHealth; set => maxHealth = Mathf.Max(1f, value); }
        public HarvestToolType RequiredTool { get => requiredTool; set => requiredTool = value; }
        public ItemDataSO DropItem { get => dropItem; set => dropItem = value; }
        public int MinDropCount { get => minDropCount; set => minDropCount = Mathf.Max(1, value); }
        public int MaxDropCount { get => maxDropCount; set => maxDropCount = Mathf.Max(minDropCount, value); }
        public GameObject LootPickupPrefab { get => lootPickupPrefab; set => lootPickupPrefab = value; }
    }
}
