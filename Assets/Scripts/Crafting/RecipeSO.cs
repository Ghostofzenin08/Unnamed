using System;
using System.Collections.Generic;
using UnityEngine;
using Outpost404.Inventory;

namespace Outpost404.Crafting
{
    public enum CraftingCategory
    {
        Survival,
        Structures,
        Tools
    }

    [Serializable]
    public class IngredientRequirement
    {
        [SerializeField] private ItemDataSO item;
        [SerializeField] private int count = 1;

        public ItemDataSO Item { get => item; set => item = value; }
        public int Count { get => Mathf.Max(1, count); set => count = Mathf.Max(1, value); }

        public IngredientRequirement() { }

        public IngredientRequirement(ItemDataSO item, int count)
        {
            this.item = item;
            this.count = Mathf.Max(1, count);
        }
    }

    /// <summary>
    /// ScriptableObject defining an immutable recipe for crafting tools, consumables, and base structures.
    /// Supports multi-ingredient validation, categorized filtering, and decoupled inventory checks.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Outpost404/Crafting Recipe")]
    public class RecipeSO : ScriptableObject
    {
        [Header("Recipe Identity")]
        [SerializeField] private string recipeId = "recipe_id";
        [SerializeField] private string recipeName = "New Recipe";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Recipe description.";
        [SerializeField] private CraftingCategory category = CraftingCategory.Survival;

        [Header("Ingredients")]
        [SerializeField] private List<IngredientRequirement> ingredients = new List<IngredientRequirement>();

        [Header("Crafting Output")]
        [SerializeField] private ItemDataSO outputItem;
        [SerializeField] private int outputCount = 1;
        [SerializeField] private float craftTime = 0.5f;

        public string RecipeId { get => recipeId; set => recipeId = value; }
        public string RecipeName { get => recipeName; set => recipeName = value; }
        public string Description { get => description; set => description = value; }
        public CraftingCategory Category { get => category; set => category = value; }
        public List<IngredientRequirement> Ingredients
        {
            get
            {
                if (ingredients == null) ingredients = new List<IngredientRequirement>();
                return ingredients;
            }
        }
        public ItemDataSO OutputItem { get => outputItem; set => outputItem = value; }
        public int OutputCount { get => Mathf.Max(1, outputCount); set => outputCount = Mathf.Max(1, value); }
        public float CraftTime { get => Mathf.Max(0f, craftTime); set => craftTime = Mathf.Max(0f, value); }

        /// <summary>
        /// Validates whether the given inventory has all required items in sufficient counts.
        /// Aggregates duplicate ingredients safely to ensure total quantity is met.
        /// </summary>
        public bool CanCraft(InventorySystem inventory)
        {
            if (inventory == null || outputItem == null || ingredients == null || ingredients.Count == 0)
            {
                return false;
            }

            Dictionary<ItemDataSO, int> requiredTotals = new Dictionary<ItemDataSO, int>();
            for (int i = 0; i < ingredients.Count; i++)
            {
                var req = ingredients[i];
                if (req == null || req.Item == null || req.Count <= 0) return false;

                if (requiredTotals.ContainsKey(req.Item))
                {
                    requiredTotals[req.Item] += req.Count;
                }
                else
                {
                    requiredTotals[req.Item] = req.Count;
                }
            }

            foreach (var pair in requiredTotals)
            {
                if (inventory.GetItemCount(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
