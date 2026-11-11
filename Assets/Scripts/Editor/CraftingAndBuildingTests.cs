using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Outpost404.Crafting;
using Outpost404.Building;
using Outpost404.Inventory;
using Outpost404.Survival;
using Outpost404.Player;
using Outpost404.Combat;
using Outpost404.Harvesting;

namespace Outpost404.Tests
{
    public class CraftingAndBuildingTests
    {
        private GameObject playerObj;
        private InventorySystem inventory;
        private CraftingManager craftingManager;
        private StructurePlacer structurePlacer;

        private ItemDataSO woodItem;
        private ItemDataSO oreItem;
        private ItemDataSO scrapItem;
        private ItemDataSO barricadeItem;
        private ItemDataSO axeItem;

        private RecipeSO barricadeRecipe;
        private RecipeSO axeRecipe;
        private GameObject barricadePrefabObj;
        private BuildableStructure barricadeStructure;

        [SetUp]
        public void SetUp()
        {
            playerObj = new GameObject("TestPlayer");
            playerObj.tag = "Player";
            inventory = playerObj.AddComponent<InventorySystem>();
            inventory.InitializeSlots();

            craftingManager = playerObj.AddComponent<CraftingManager>();
            craftingManager.TargetInventory = inventory;

            structurePlacer = playerObj.AddComponent<StructurePlacer>();
            structurePlacer.Inventory = inventory;
            structurePlacer.CraftingManager = craftingManager;

            // Items
            woodItem = ScriptableObject.CreateInstance<ItemDataSO>();
            woodItem.ItemId = "item_wood";
            woodItem.ItemName = "Composite Timber";
            woodItem.MaxStack = 64;

            oreItem = ScriptableObject.CreateInstance<ItemDataSO>();
            oreItem.ItemId = "item_stone";
            oreItem.ItemName = "Conductive Ore";
            oreItem.MaxStack = 64;

            scrapItem = ScriptableObject.CreateInstance<ItemDataSO>();
            scrapItem.ItemId = "item_scrap";
            scrapItem.ItemName = "Titanium Scrap";
            scrapItem.MaxStack = 64;

            barricadeItem = ScriptableObject.CreateInstance<ItemDataSO>();
            barricadeItem.ItemId = "item_barricade";
            barricadeItem.ItemName = "Reinforced Barricade";
            barricadeItem.ItemType = ItemType.Structure;
            barricadeItem.MaxStack = 16;

            axeItem = ScriptableObject.CreateInstance<ItemDataSO>();
            axeItem.ItemId = "item_axe";
            axeItem.ItemName = "Makeshift Axe";
            axeItem.ItemType = ItemType.Equipment;
            axeItem.HarvestMultiplier = 2.5f;

            // Structure Prefab
            barricadePrefabObj = new GameObject("Prefab_Barricade");
            barricadeStructure = barricadePrefabObj.AddComponent<BuildableStructure>();
            barricadeStructure.StructureName = "Reinforced Barricade";
            barricadeStructure.MaxDurability = 150f;
            barricadeStructure.PlacementExtents = new Vector3(1.2f, 1f, 0.4f);
            var col = barricadePrefabObj.AddComponent<BoxCollider>();
            col.size = new Vector3(2.4f, 2f, 0.8f);

            barricadeItem.StructurePrefab = barricadePrefabObj;

            // Barricade Recipe: 4 Wood -> 1 Barricade
            barricadeRecipe = ScriptableObject.CreateInstance<RecipeSO>();
            barricadeRecipe.RecipeId = "recipe_barricade";
            barricadeRecipe.RecipeName = "Reinforced Barricade";
            barricadeRecipe.Category = CraftingCategory.Structures;
            barricadeRecipe.Ingredients.Add(new IngredientRequirement(woodItem, 4));
            barricadeRecipe.OutputItem = barricadeItem;
            barricadeRecipe.OutputCount = 1;

            // Axe Recipe: 2 Wood + 2 Scrap -> 1 Axe
            axeRecipe = ScriptableObject.CreateInstance<RecipeSO>();
            axeRecipe.RecipeId = "recipe_axe";
            axeRecipe.RecipeName = "Makeshift Axe";
            axeRecipe.Category = CraftingCategory.Tools;
            axeRecipe.Ingredients.Add(new IngredientRequirement(woodItem, 2));
            axeRecipe.Ingredients.Add(new IngredientRequirement(scrapItem, 2));
            axeRecipe.OutputItem = axeItem;
            axeRecipe.OutputCount = 1;

            craftingManager.RegisterRecipe(barricadeRecipe);
            craftingManager.RegisterRecipe(axeRecipe);
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObj != null) Object.DestroyImmediate(playerObj);
            if (barricadePrefabObj != null) Object.DestroyImmediate(barricadePrefabObj);

            if (woodItem != null) Object.DestroyImmediate(woodItem);
            if (oreItem != null) Object.DestroyImmediate(oreItem);
            if (scrapItem != null) Object.DestroyImmediate(scrapItem);
            if (barricadeItem != null) Object.DestroyImmediate(barricadeItem);
            if (axeItem != null) Object.DestroyImmediate(axeItem);
            if (barricadeRecipe != null) Object.DestroyImmediate(barricadeRecipe);
            if (axeRecipe != null) Object.DestroyImmediate(axeRecipe);
        }

        [Test]
        public void Recipe_CanCraft_ReturnsTrueWhenIngredientsSufficient()
        {
            inventory.AddItem(woodItem, 6);
            Assert.IsTrue(barricadeRecipe.CanCraft(inventory));
        }

        [Test]
        public void Recipe_CanCraft_ReturnsFalseWhenIngredientsInsufficient()
        {
            inventory.AddItem(woodItem, 3); // Needs 4
            Assert.IsFalse(barricadeRecipe.CanCraft(inventory));
        }

        [Test]
        public void Recipe_CanCraft_AggregatesDuplicateIngredientsSafely()
        {
            var multiReqRecipe = ScriptableObject.CreateInstance<RecipeSO>();
            multiReqRecipe.Ingredients.Add(new IngredientRequirement(woodItem, 3));
            multiReqRecipe.Ingredients.Add(new IngredientRequirement(woodItem, 2)); // Total required = 5
            multiReqRecipe.OutputItem = barricadeItem;

            inventory.AddItem(woodItem, 4);
            Assert.IsFalse(multiReqRecipe.CanCraft(inventory), "4 wood should not satisfy requirement of 3 + 2 = 5");

            inventory.AddItem(woodItem, 1);
            Assert.IsTrue(multiReqRecipe.CanCraft(inventory), "5 wood should satisfy requirement of 3 + 2 = 5");

            Object.DestroyImmediate(multiReqRecipe);
        }

        [Test]
        public void Recipe_CanCraft_NullOrEmptyInputs_ReturnsFalseSafely()
        {
            Assert.IsFalse(barricadeRecipe.CanCraft(null));

            var emptyRecipe = ScriptableObject.CreateInstance<RecipeSO>();
            Assert.IsFalse(emptyRecipe.CanCraft(inventory));
            Object.DestroyImmediate(emptyRecipe);
        }

        [Test]
        public void CraftingManager_Craft_ConsumesIngredientsAndYieldsOutput()
        {
            inventory.AddItem(woodItem, 4);
            inventory.AddItem(oreItem, 10);

            bool success = craftingManager.Craft(barricadeRecipe);

            Assert.IsTrue(success);
            Assert.AreEqual(0, inventory.GetItemCount(woodItem), "Wood should have been consumed.");
            Assert.AreEqual(10, inventory.GetItemCount(oreItem), "Ore should remain untouched.");
            Assert.AreEqual(1, inventory.GetItemCount(barricadeItem), "Barricade should be placed in inventory.");
        }

        [Test]
        public void CraftingManager_Craft_InsufficientIngredients_FailsAndLeavesInventoryIntact()
        {
            inventory.AddItem(woodItem, 2);
            inventory.AddItem(scrapItem, 1); // Axe needs 2 wood + 2 scrap

            bool eventFired = false;
            craftingManager.OnCraftFailed += (rec, reason) => eventFired = true;

            bool success = craftingManager.Craft(axeRecipe);

            Assert.IsFalse(success);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(2, inventory.GetItemCount(woodItem));
            Assert.AreEqual(1, inventory.GetItemCount(scrapItem));
            Assert.AreEqual(0, inventory.GetItemCount(axeItem));
        }

        [Test]
        public void CraftingManager_Craft_NullRecipeOrInventory_FailsGracefully()
        {
            Assert.IsFalse(craftingManager.Craft(null));

            craftingManager.TargetInventory = null;
            Assert.IsFalse(craftingManager.Craft(barricadeRecipe));
        }

        [Test]
        public void BuildableStructure_InitializesWithMaxDurability()
        {
            Assert.AreEqual(150f, barricadeStructure.CurrentDurability);
            Assert.AreEqual(150f, barricadeStructure.MaxDurability);
            Assert.IsFalse(barricadeStructure.IsDead);
            Assert.AreEqual(1f, barricadeStructure.DurabilityFraction, 0.001f);
        }

        [Test]
        public void BuildableStructure_TakeDamage_ReducesDurabilityAndFiresEvent()
        {
            float reportedDurability = -1f;
            barricadeStructure.OnDurabilityChanged += (structObj, durability) => reportedDurability = durability;

            barricadeStructure.TakeDamage(50f);

            Assert.AreEqual(100f, barricadeStructure.CurrentDurability);
            Assert.AreEqual(100f, reportedDurability);
            Assert.IsFalse(barricadeStructure.IsDead);
        }

        [Test]
        public void BuildableStructure_DurabilityZero_FlagsDead()
        {
            bool destroyedFired = false;
            barricadeStructure.OnStructureDestroyed += (s) => destroyedFired = true;

            barricadeStructure.TakeDamage(200f);

            Assert.AreEqual(0f, barricadeStructure.CurrentDurability);
            Assert.IsTrue(barricadeStructure.IsDead);
            Assert.IsTrue(destroyedFired);
        }

        [Test]
        public void BuildableStructure_Repair_RestoresDurabilityClampedToMax()
        {
            barricadeStructure.TakeDamage(80f);
            Assert.AreEqual(70f, barricadeStructure.CurrentDurability);

            barricadeStructure.Repair(40f);
            Assert.AreEqual(110f, barricadeStructure.CurrentDurability);

            barricadeStructure.Repair(100f);
            Assert.AreEqual(150f, barricadeStructure.CurrentDurability, "Repair must clamp to maxDurability");
        }

        [Test]
        public void BuildableStructure_HeatBeacon_RegeneratesNearbyDamagedPlayer()
        {
            var beaconObj = new GameObject("PulseBeacon");
            var beacon = beaconObj.AddComponent<BuildableStructure>();
            beacon.StructureName = "Heat Beacon";
            beacon.PlacementExtents = new Vector3(0.8f, 0.8f, 0.8f);
            beacon.AuraRadius = 5f;
            beacon.HealthRegenPerSecond = 20f;
            beacon.SetCurrentDurabilityDirect(100f);

            var pStats = playerObj.AddComponent<PlayerStats>();
            pStats.InitializeStats();
            pStats.TakeDamage(40f); // 60 / 100 HP

            // Position player inside aura (2m away)
            playerObj.transform.position = new Vector3(2f, 0f, 0f);
            beaconObj.transform.position = Vector3.zero;

            // Emulate aura tick
            Collider col = playerObj.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();

            beacon.ApplyHeatBeaconAura(1.0f);

            Assert.Greater(pStats.CurrentHealth, 60f, "Player inside heat beacon radius should regenerate health.");

            Object.DestroyImmediate(beaconObj);
            Object.DestroyImmediate(pStats);
            Object.DestroyImmediate(col);
        }

        [Test]
        public void BuildableStructure_HeatBeacon_DoesNotHealPlayerOutsideRadius()
        {
            var beaconObj = new GameObject("PulseBeacon");
            var beacon = beaconObj.AddComponent<BuildableStructure>();
            beacon.AuraRadius = 4f;
            beacon.HealthRegenPerSecond = 20f;

            var pStats = playerObj.AddComponent<PlayerStats>();
            pStats.InitializeStats();
            pStats.TakeDamage(40f); // 60 / 100 HP

            // Position player outside aura (12m away)
            playerObj.transform.position = new Vector3(12f, 0f, 0f);
            beaconObj.transform.position = Vector3.zero;

            Collider col = playerObj.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();

            beacon.ApplyHeatBeaconAura(1.0f);

            Assert.AreEqual(60f, pStats.CurrentHealth, "Player outside heat beacon radius must not be healed.");

            Object.DestroyImmediate(beaconObj);
            Object.DestroyImmediate(pStats);
            Object.DestroyImmediate(col);
        }

        [Test]
        public void StructurePlacer_EvaluatePlacement_WithinDistanceAndClear_ReturnsTrue()
        {
            Vector3 pos = new Vector3(2f, 0f, 2f);
            Quaternion rot = Quaternion.identity;
            Vector3 extents = new Vector3(1f, 1f, 1f);

            bool valid = structurePlacer.EvaluatePlacement(pos, rot, extents, 3f);
            Assert.IsTrue(valid);
        }

        [Test]
        public void StructurePlacer_EvaluatePlacement_ExceedingMaxDistance_ReturnsFalse()
        {
            structurePlacer.MaxBuildDistance = 8f;
            Vector3 pos = new Vector3(20f, 0f, 0f);
            Quaternion rot = Quaternion.identity;
            Vector3 extents = new Vector3(1f, 1f, 1f);

            bool valid = structurePlacer.EvaluatePlacement(pos, rot, extents, 20f);
            Assert.IsFalse(valid, "Placement beyond max build distance must be rejected.");
        }

        [Test]
        public void StructurePlacer_EvaluatePlacement_BlockedByObstacle_ReturnsFalse()
        {
            var obstacleObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacleObj.name = "Obstacle_Rock";
            obstacleObj.transform.position = new Vector3(2f, 1f, 2f);
            obstacleObj.transform.localScale = Vector3.one * 2f;
            Physics.SyncTransforms();

            Vector3 placePos = new Vector3(2f, 0f, 2f);
            Quaternion rot = Quaternion.identity;
            Vector3 extents = new Vector3(1.2f, 1f, 1.2f);

            bool valid = structurePlacer.EvaluatePlacement(placePos, rot, extents, 2f);

            Assert.IsFalse(valid, "Placement overlapping a solid obstacle must evaluate as blocked.");

            Object.DestroyImmediate(obstacleObj);
        }

        [Test]
        public void HarvestMultiplier_EquippedTool_BoostsHarvestDamage()
        {
            var pInteract = playerObj.AddComponent<PlayerInteraction>();
            pInteract.HarvestDamage = 20f;
            pInteract.Inventory = inventory;

            var pCombat = playerObj.AddComponent<PlayerCombat>();
            pCombat.AttackDamage = 25f;
            pCombat.Inventory = inventory;

            var nodeObj = new GameObject("TestNode");
            nodeObj.AddComponent<SphereCollider>();
            var node = nodeObj.AddComponent<ResourceNode>();
            node.InitializeNode();

            // Baseline without tool
            float baseHarvestDmg = pInteract.GetEffectiveHarvestDamage();
            float baseCombatDmg = pCombat.GetDamageAgainst(node);
            Assert.AreEqual(20f, baseHarvestDmg);
            Assert.AreEqual(25f, baseCombatDmg);

            // Add Makeshift Axe (2.5x multiplier)
            inventory.AddItem(axeItem, 1);

            float boostedHarvestDmg = pInteract.GetEffectiveHarvestDamage();
            float boostedCombatDmg = pCombat.GetDamageAgainst(node);

            Assert.AreEqual(50f, boostedHarvestDmg, 0.01f, "20 * 2.5 = 50 damage");
            Assert.AreEqual(62.5f, boostedCombatDmg, 0.01f, "25 * 2.5 = 62.5 damage");

            Object.DestroyImmediate(pInteract);
            Object.DestroyImmediate(pCombat);
            Object.DestroyImmediate(nodeObj);
        }
    }
}
