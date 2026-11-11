using System;
using System.Collections.Generic;
using UnityEngine;
using Outpost404.Inventory;
using Outpost404.Harvesting;

namespace Outpost404.Crafting
{
    /// <summary>
    /// Manages the registry of recipes, validates ingredient availability in inventory,
    /// consumes required resources, and yields crafted tools, structures, and consumables.
    /// </summary>
    public class CraftingManager : MonoBehaviour
    {
        [Header("Recipe Registry")]
        [SerializeField] private List<RecipeSO> registeredRecipes = new List<RecipeSO>();

        [Header("Inventory Reference")]
        [SerializeField] private InventorySystem targetInventory;

        [Header("Loot Drop Prefab (Overflow Fallback)")]
        [SerializeField] private GameObject lootPickupPrefab;

        public event Action<RecipeSO> OnRecipeCrafted;
        public event Action<RecipeSO, string> OnCraftFailed;

        public IReadOnlyList<RecipeSO> RegisteredRecipes => registeredRecipes;
        public InventorySystem TargetInventory
        {
            get
            {
                if (targetInventory == null) targetInventory = GetComponent<InventorySystem>();
                return targetInventory;
            }
            set => targetInventory = value;
        }

        private void Awake()
        {
            if (targetInventory == null) targetInventory = GetComponent<InventorySystem>();
        }

        public void RegisterRecipe(RecipeSO recipe)
        {
            if (recipe != null && !registeredRecipes.Contains(recipe))
            {
                registeredRecipes.Add(recipe);
            }
        }

        public void SetRecipes(IEnumerable<RecipeSO> recipes)
        {
            registeredRecipes.Clear();
            if (recipes != null)
            {
                registeredRecipes.AddRange(recipes);
            }
        }

        public List<RecipeSO> GetRecipesByCategory(CraftingCategory category)
        {
            List<RecipeSO> list = new List<RecipeSO>();
            for (int i = 0; i < registeredRecipes.Count; i++)
            {
                if (registeredRecipes[i] != null && registeredRecipes[i].Category == category)
                {
                    list.Add(registeredRecipes[i]);
                }
            }
            return list;
        }

        public bool CanCraft(RecipeSO recipe)
        {
            if (recipe == null) return false;
            var inv = TargetInventory;
            if (inv == null) return false;
            return recipe.CanCraft(inv);
        }

        /// <summary>
        /// Validates and consumes ingredients from inventory WITHOUT adding crafted items to inventory.
        /// Used for direct holographic structure placement to prevent item duplication exploits.
        /// </summary>
        public bool ConsumeIngredients(RecipeSO recipe)
        {
            var inv = TargetInventory;
            if (recipe == null)
            {
                OnCraftFailed?.Invoke(recipe, "Null recipe.");
                return false;
            }
            if (inv == null)
            {
                OnCraftFailed?.Invoke(recipe, "No inventory available.");
                return false;
            }
            if (!recipe.CanCraft(inv))
            {
                OnCraftFailed?.Invoke(recipe, "Insufficient ingredients.");
                return false;
            }

            // Aggregate duplicate ingredient entries to consume in unified counts
            Dictionary<ItemDataSO, int> requiredTotals = new Dictionary<ItemDataSO, int>();
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var req = recipe.Ingredients[i];
                if (req != null && req.Item != null && req.Count > 0)
                {
                    if (requiredTotals.ContainsKey(req.Item))
                        requiredTotals[req.Item] += req.Count;
                    else
                        requiredTotals[req.Item] = req.Count;
                }
            }

            foreach (var pair in requiredTotals)
            {
                inv.RemoveItem(pair.Key, pair.Value);
            }

            OnRecipeCrafted?.Invoke(recipe);
            return true;
        }

        /// <summary>
        /// Crafts the specified recipe, consuming ingredients and placing the output in inventory.
        /// Any overflow is dropped safely as a world loot pickup.
        /// </summary>
        public bool Craft(RecipeSO recipe)
        {
            var inv = TargetInventory;
            if (recipe == null)
            {
                OnCraftFailed?.Invoke(recipe, "Null recipe.");
                return false;
            }
            if (inv == null)
            {
                OnCraftFailed?.Invoke(recipe, "No inventory available.");
                return false;
            }
            if (!recipe.CanCraft(inv))
            {
                OnCraftFailed?.Invoke(recipe, "Insufficient ingredients.");
                return false;
            }

            // Aggregate duplicate ingredient entries to consume in unified counts
            Dictionary<ItemDataSO, int> requiredTotals = new Dictionary<ItemDataSO, int>();
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var req = recipe.Ingredients[i];
                if (req != null && req.Item != null && req.Count > 0)
                {
                    if (requiredTotals.ContainsKey(req.Item))
                        requiredTotals[req.Item] += req.Count;
                    else
                        requiredTotals[req.Item] = req.Count;
                }
            }

            foreach (var pair in requiredTotals)
            {
                inv.RemoveItem(pair.Key, pair.Value);
            }

            // Yield crafted output
            inv.AddItem(recipe.OutputItem, recipe.OutputCount, out int remaining);

            // Handle overflow if inventory is full
            if (remaining > 0)
            {
                DropOverflowLoot(recipe.OutputItem, remaining);
            }

            OnRecipeCrafted?.Invoke(recipe);
            return true;
        }

        private void DropOverflowLoot(ItemDataSO item, int count)
        {
            Vector3 spawnPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.4f;
            GameObject lootObj;
            if (lootPickupPrefab != null)
            {
                lootObj = Instantiate(lootPickupPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                lootObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lootObj.transform.position = spawnPos;
                lootObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                var col = lootObj.GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }

            var pickup = lootObj.GetComponent<LootPickup>();
            if (pickup == null) pickup = lootObj.AddComponent<LootPickup>();
            pickup.Initialize(item, count);
        }
    }
}
