using NUnit.Framework;
using UnityEngine;
using Outpost404.Inventory;

namespace Outpost404.Tests
{
    public class InventorySystemTests
    {
        private GameObject invObj;
        private InventorySystem inventory;
        private ItemDataSO woodItem;
        private ItemDataSO oreItem;

        [SetUp]
        public void SetUp()
        {
            invObj = new GameObject("TestInventory");
            inventory = invObj.AddComponent<InventorySystem>();
            inventory.InitializeSlots();

            woodItem = ScriptableObject.CreateInstance<ItemDataSO>();
            woodItem.ItemId = "test_wood";
            woodItem.ItemName = "Test Wood";
            woodItem.MaxStack = 10;

            oreItem = ScriptableObject.CreateInstance<ItemDataSO>();
            oreItem.ItemId = "test_ore";
            oreItem.ItemName = "Test Ore";
            oreItem.MaxStack = 5;
        }

        [TearDown]
        public void TearDown()
        {
            if (invObj != null) Object.DestroyImmediate(invObj);
            if (woodItem != null) Object.DestroyImmediate(woodItem);
            if (oreItem != null) Object.DestroyImmediate(oreItem);
        }

        [Test]
        public void AddItem_AddsToFirstEmptySlot()
        {
            bool added = inventory.AddItem(woodItem, 5, out int remaining);
            Assert.IsTrue(added);
            Assert.AreEqual(0, remaining);
            Assert.AreEqual(5, inventory.GetItemCount(woodItem));
            Assert.AreEqual(woodItem, inventory.Slots[0].item);
            Assert.AreEqual(5, inventory.Slots[0].quantity);
        }

        [Test]
        public void AddItem_StacksExistingItems()
        {
            inventory.AddItem(woodItem, 4);
            inventory.AddItem(woodItem, 3);

            Assert.AreEqual(7, inventory.GetItemCount(woodItem));
            Assert.AreEqual(7, inventory.Slots[0].quantity);
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
        }

        [Test]
        public void AddItem_ExceedingMaxStack_SpillsToNextSlot()
        {
            inventory.AddItem(woodItem, 8);
            inventory.AddItem(woodItem, 5); // woodItem maxStack is 10

            Assert.AreEqual(13, inventory.GetItemCount(woodItem));
            Assert.AreEqual(10, inventory.Slots[0].quantity);
            Assert.AreEqual(3, inventory.Slots[1].quantity);
        }

        [Test]
        public void AddItem_NullOrZero_ReturnsFalse()
        {
            Assert.IsFalse(inventory.AddItem(null, 5, out int r1));
            Assert.AreEqual(5, r1);
            Assert.IsFalse(inventory.AddItem(woodItem, 0, out int r2));
            Assert.AreEqual(0, r2);
        }

        [Test]
        public void RemoveItem_ReducesQuantityCorrectlyAcrossSlots()
        {
            inventory.AddItem(woodItem, 10);
            inventory.AddItem(woodItem, 5);

            bool removed = inventory.RemoveItem(woodItem, 12);
            Assert.IsTrue(removed);
            Assert.AreEqual(3, inventory.GetItemCount(woodItem));
            Assert.AreEqual(3, inventory.Slots[1].quantity);
        }

        [Test]
        public void RemoveItem_InsufficientQuantity_ReturnsFalseAndLeavesInventoryIntact()
        {
            inventory.AddItem(oreItem, 3);
            bool removed = inventory.RemoveItem(oreItem, 5);
            Assert.IsFalse(removed);
            Assert.AreEqual(3, inventory.GetItemCount(oreItem));
        }

        [Test]
        public void HasItem_ReturnsTrueOnlyWhenQuantitySufficient()
        {
            inventory.AddItem(oreItem, 4);
            Assert.IsTrue(inventory.HasItem(oreItem, 4));
            Assert.IsTrue(inventory.HasItem(oreItem, 2));
            Assert.IsFalse(inventory.HasItem(oreItem, 5));
        }

        [Test]
        public void Clear_EmptiesAllSlotsAndFiresEvent()
        {
            inventory.AddItem(woodItem, 5);
            inventory.AddItem(oreItem, 3);

            bool eventFired = false;
            inventory.OnInventoryChanged += () => eventFired = true;

            inventory.Clear();

            Assert.IsTrue(eventFired);
            Assert.AreEqual(0, inventory.GetItemCount(woodItem));
            Assert.AreEqual(0, inventory.GetItemCount(oreItem));
        }

        [Test]
        public void ConsumeItem_ValidConsumable_RestoresHungerAndHealth()
        {
            var food = ScriptableObject.CreateInstance<ItemDataSO>();
            food.ItemId = "test_food";
            food.ItemType = ItemType.Consumable;
            food.CaloricValue = 25f;
            food.HealValue = 15f;

            inventory.AddItem(food, 2);

            var playerObj = new GameObject("TestPlayerStats");
            var stats = playerObj.AddComponent<Outpost404.Survival.PlayerStats>();
            stats.InitializeStats();
            stats.ConsumeHunger(40f);
            stats.TakeDamage(30f);

            float initialHunger = stats.CurrentHunger;
            float initialHealth = stats.CurrentHealth;

            bool consumed = inventory.ConsumeItem(food, stats);

            Assert.IsTrue(consumed);
            Assert.AreEqual(1, inventory.GetItemCount(food));
            Assert.AreEqual(initialHunger + 25f, stats.CurrentHunger, 0.01f);
            Assert.AreEqual(initialHealth + 15f, stats.CurrentHealth, 0.01f);

            Object.DestroyImmediate(playerObj);
            Object.DestroyImmediate(food);
        }

        [Test]
        public void ConsumeItem_NonConsumable_ReturnsFalseAndLeavesItem()
        {
            inventory.AddItem(woodItem, 3);

            var playerObj = new GameObject("TestPlayerStats");
            var stats = playerObj.AddComponent<Outpost404.Survival.PlayerStats>();
            stats.InitializeStats();

            bool consumed = inventory.ConsumeItem(woodItem, stats);

            Assert.IsFalse(consumed);
            Assert.AreEqual(3, inventory.GetItemCount(woodItem));

            Object.DestroyImmediate(playerObj);
        }

        [Test]
        public void ItemDataSO_ZeroOrNegativeMaxStack_ClampsToOne()
        {
            var item = ScriptableObject.CreateInstance<ItemDataSO>();
            item.MaxStack = -5;
            Assert.AreEqual(1, item.MaxStack);

            item.MaxStack = 0;
            Assert.AreEqual(1, item.MaxStack);

            Object.DestroyImmediate(item);
        }
    }
}
