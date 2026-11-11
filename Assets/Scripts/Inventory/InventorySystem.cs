using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outpost404.Inventory
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemDataSO item;
        public int quantity;

        public InventorySlot()
        {
            item = null;
            quantity = 0;
        }

        public InventorySlot(ItemDataSO item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public bool IsEmpty => item == null || quantity <= 0;

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }

    /// <summary>
    /// Component-based inventory system supporting slot-based storage, item stacking limits,
    /// safe removal, querying, and decoupled event notification.
    /// </summary>
    [DisallowMultipleComponent]
    public class InventorySystem : MonoBehaviour
    {
        [Header("Inventory Capacity")]
        [SerializeField] private int slotCapacity = 16;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        public event Action OnInventoryChanged;

        public int SlotCapacity => slotCapacity;
        public IReadOnlyList<InventorySlot> Slots => slots;

        private void Awake()
        {
            InitializeSlots();
        }

        public void InitializeSlots()
        {
            if (slots == null) slots = new List<InventorySlot>();

            while (slots.Count < slotCapacity)
            {
                slots.Add(new InventorySlot());
            }

            while (slots.Count > slotCapacity)
            {
                slots.RemoveAt(slots.Count - 1);
            }
        }

        /// <summary>
        /// Adds a quantity of items, respecting stacking rules and capacity.
        /// Outputs the remaining count if the inventory fills up.
        /// </summary>
        public bool AddItem(ItemDataSO item, int quantity, out int remaining)
        {
            remaining = quantity;
            if (item == null || quantity <= 0) return false;

            InitializeSlots();

            // 1. Fill existing partial stacks of the same item
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty && slot.item == item && slot.quantity < item.MaxStack)
                {
                    int space = item.MaxStack - slot.quantity;
                    int addCount = Mathf.Min(space, remaining);
                    slot.quantity += addCount;
                    remaining -= addCount;
                }
            }

            // 2. Occupy empty slots if remainder persists
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    int addCount = Mathf.Min(item.MaxStack, remaining);
                    slot.item = item;
                    slot.quantity = addCount;
                    remaining -= addCount;
                }
            }

            bool addedAny = (remaining < quantity);
            if (addedAny)
            {
                OnInventoryChanged?.Invoke();
            }

            return addedAny;
        }

        public bool AddItem(ItemDataSO item, int quantity)
        {
            return AddItem(item, quantity, out _);
        }

        /// <summary>
        /// Deducts a quantity of items across slots. Returns true if the full quantity was removed.
        /// </summary>
        public bool RemoveItem(ItemDataSO item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            if (GetItemCount(item) < quantity) return false;

            int needed = quantity;
            for (int i = 0; i < slots.Count && needed > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty && slot.item == item)
                {
                    if (slot.quantity <= needed)
                    {
                        needed -= slot.quantity;
                        slot.Clear();
                    }
                    else
                    {
                        slot.quantity -= needed;
                        needed = 0;
                    }
                }
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Returns total count of a specific item across all slots.
        /// </summary>
        public int GetItemCount(ItemDataSO item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                {
                    total += slots[i].quantity;
                }
            }
            return total;
        }

        public bool HasItem(ItemDataSO item, int quantity)
        {
            return GetItemCount(item) >= quantity;
        }

        /// <summary>
        /// Consumes a food/medical item from inventory, applying its caloric and heal values to PlayerStats.
        /// </summary>
        public bool ConsumeItem(ItemDataSO item, Outpost404.Survival.PlayerStats stats)
        {
            if (item == null || stats == null || stats.IsDead) return false;
            if (item.ItemType != ItemType.Consumable) return false;
            if (!RemoveItem(item, 1)) return false;

            if (item.CaloricValue > 0f)
            {
                stats.RestoreHunger(item.CaloricValue);
            }
            if (item.HealValue > 0f)
            {
                stats.Heal(item.HealValue);
            }
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Clear();
            }
            OnInventoryChanged?.Invoke();
        }
    }
}
