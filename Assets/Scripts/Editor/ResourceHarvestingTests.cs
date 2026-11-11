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
    }
}
