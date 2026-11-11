using NUnit.Framework;
using UnityEngine;
using Outpost404.Harvesting;
using Outpost404.Inventory;

namespace Outpost404.Tests
{
    public class ResourceHarvestingTests
    {
        private GameObject nodeObj;
        private ResourceNode node;
        private ResourceNodeSO nodeSO;
        private ItemDataSO dropItem;

        [SetUp]
        public void SetUp()
        {
            dropItem = ScriptableObject.CreateInstance<ItemDataSO>();
            dropItem.ItemId = "timber_test";
            dropItem.ItemName = "Timber Test";
            dropItem.MaxStack = 20;

            nodeSO = ScriptableObject.CreateInstance<ResourceNodeSO>();
            nodeSO.NodeName = "Test Alien Tree";
            nodeSO.MaxHealth = 50f;
            nodeSO.DropItem = dropItem;
            nodeSO.MinDropCount = 2;
            nodeSO.MaxDropCount = 4;

            nodeObj = new GameObject("TestNode");
            nodeObj.AddComponent<BoxCollider>();
            node = nodeObj.AddComponent<ResourceNode>();
            node.Configure(nodeSO);
        }

        [TearDown]
        public void TearDown()
        {
            if (nodeObj != null) Object.DestroyImmediate(nodeObj);
            if (nodeSO != null) Object.DestroyImmediate(nodeSO);
            if (dropItem != null) Object.DestroyImmediate(dropItem);
        }

        [Test]
        public void ResourceNode_InitializesWithConfiguredHealth()
        {
            Assert.AreEqual(50f, node.CurrentHealth);
            Assert.AreEqual(50f, node.MaxHealth);
            Assert.IsFalse(node.IsDead);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            node.TakeDamage(20f);
            Assert.AreEqual(30f, node.CurrentHealth);
            Assert.IsFalse(node.IsDead);
        }

        [Test]
        public void TakeDamage_DepletionTriggersDeathAndEvent()
        {
            bool depletedFired = false;
            node.OnNodeDepleted += (n) => depletedFired = true;
            node.SpawnLootOnDepletion = false; // Do not leak objects into test scene

            node.TakeDamage(60f);

            Assert.IsTrue(node.IsDead);
            Assert.AreEqual(0f, node.CurrentHealth);
            Assert.IsTrue(depletedFired);
        }

        [Test]
        public void LootPickup_Collect_AddsToInventory()
        {
            GameObject pickupObj = new GameObject("TestPickup");
            pickupObj.AddComponent<BoxCollider>();
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(dropItem, 3);

            GameObject playerObj = new GameObject("TestPlayer");
            var inventory = playerObj.AddComponent<InventorySystem>();
            inventory.InitializeSlots();

            bool collected = pickup.Collect(inventory);

            Assert.IsTrue(collected);
            Assert.AreEqual(3, inventory.GetItemCount(dropItem));

            Object.DestroyImmediate(playerObj);
            if (pickupObj != null) Object.DestroyImmediate(pickupObj);
        }

        [Test]
        public void LootPickup_PartialCollect_LeavesRemainingAndReturnsTrue()
        {
            dropItem.MaxStack = 2; // Small stack limit
            GameObject pickupObj = new GameObject("TestPickup");
            pickupObj.AddComponent<BoxCollider>();
            var pickup = pickupObj.AddComponent<LootPickup>();
            pickup.Initialize(dropItem, 5); // 5 items

            GameObject playerObj = new GameObject("TestPlayer");
            var inventory = playerObj.AddComponent<InventorySystem>();
            inventory.InitializeSlots();
            // Fill all slots except 1 space
            for (int i = 0; i < inventory.SlotCapacity - 1; i++)
            {
                var filler = ScriptableObject.CreateInstance<ItemDataSO>();
                filler.MaxStack = 1;
                inventory.AddItem(filler, 1);
            }

            // Only 1 slot free with maxStack 2 -> fits only 2 items
            bool collected = pickup.Collect(inventory);

            Assert.IsTrue(collected, "Partial collection should report success.");
            Assert.AreEqual(3, pickup.Quantity, "3 items should remain on world pickup.");
            Assert.AreEqual(2, inventory.GetItemCount(dropItem));

            Object.DestroyImmediate(playerObj);
            if (pickupObj != null) Object.DestroyImmediate(pickupObj);
        }

        [Test]
        public void PlayerInteraction_TryInteract_HarvestsResourceNode()
        {
            GameObject playerObj = new GameObject("TestPlayer");
            var inventory = playerObj.AddComponent<InventorySystem>();
            inventory.InitializeSlots();
            var interaction = playerObj.AddComponent<Outpost404.Player.PlayerInteraction>();
            interaction.InteractRadius = 5f;
            interaction.HarvestDamage = 25f;

            nodeObj.transform.position = playerObj.transform.position + new Vector3(1.5f, 0f, 0f);
            float initialHealth = node.CurrentHealth;

            bool interacted = interaction.TryInteract();

            Assert.IsTrue(interacted, "Player interaction should harvest nearby resource node.");
            Assert.AreEqual(initialHealth - 25f, node.CurrentHealth, 0.01f);

            Object.DestroyImmediate(playerObj);
        }
    }
}
