using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Outpost404.Crafting;
using Outpost404.Building;
using Outpost404.Inventory;

namespace Outpost404.UI
{
    /// <summary>
    /// UI Window for the Crafting & Base Building system.
    /// Toggled via 'Tab' or 'C'. Displays recipe requirements, checks live inventory stock,
    /// triggers crafting executions, and initiates holographic structure placement.
    /// </summary>
    public class CraftingUI : MonoBehaviour
    {
        [Header("Window Panel")]
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private Transform recipeContainer;
        [SerializeField] private GameObject recipeCardPrefab;

        [Header("System References")]
        [SerializeField] private CraftingManager craftingManager;
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private StructurePlacer structurePlacer;

        [Header("Controls")]
        [SerializeField] private KeyCode toggleKey = KeyCode.C;
        [SerializeField] private KeyCode altToggleKey = KeyCode.Tab;

        private bool isOpen = false;
        private List<GameObject> spawnedCards = new List<GameObject>();

        public bool IsOpen => isOpen;
        public GameObject WindowRoot { get => windowRoot; set => windowRoot = value; }
        public CraftingManager TargetCraftingManager { get => craftingManager; set => craftingManager = value; }
        public InventorySystem TargetInventory { get => inventory; set => inventory = value; }
        public StructurePlacer TargetStructurePlacer { get => structurePlacer; set => structurePlacer = value; }

        private void Start()
        {
            if (craftingManager == null) craftingManager = FindFirstObjectByType<CraftingManager>();
            if (inventory == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) inventory = p.GetComponent<InventorySystem>();
            }
            if (structurePlacer == null) structurePlacer = FindFirstObjectByType<StructurePlacer>();

            if (inventory != null)
            {
                inventory.OnInventoryChanged += HandleInventoryChanged;
            }

            if (windowRoot != null)
            {
                windowRoot.SetActive(false);
            }
            isOpen = false;
        }

        private void OnDestroy()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void Update()
        {
            // Close terminal if player dies
            if (inventory != null)
            {
                var stats = inventory.GetComponent<Outpost404.Survival.PlayerStats>();
                if (stats != null && stats.IsDead)
                {
                    if (isOpen) SetWindowOpen(false);
                    return;
                }
            }

            bool toggleRequested = false;
            bool escapeRequested = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    toggleRequested = true;
                }
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    escapeRequested = true;
                }
            }
            if (Keyboard.current == null)
            {
                toggleRequested = Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey);
                escapeRequested = Input.GetKeyDown(KeyCode.Escape);
            }
#else
            toggleRequested = Input.GetKeyDown(toggleKey) || Input.GetKeyDown(altToggleKey);
            escapeRequested = Input.GetKeyDown(KeyCode.Escape);
#endif

            if (escapeRequested && isOpen)
            {
                SetWindowOpen(false);
                return;
            }

            if (toggleRequested)
            {
                ToggleWindow();
            }
        }

        public void ToggleWindow()
        {
            SetWindowOpen(!isOpen);
        }

        public void SetWindowOpen(bool open)
        {
            isOpen = open;
            if (windowRoot != null)
            {
                windowRoot.SetActive(isOpen);
            }

            if (isOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                RefreshRecipeList();
            }
        }

        private void HandleInventoryChanged()
        {
            if (isOpen)
            {
                RefreshRecipeList();
            }
        }

        public void RefreshRecipeList()
        {
            ClearCards();

            if (craftingManager == null) return;
            var recipes = craftingManager.RegisteredRecipes;
            if (recipes == null || recipes.Count == 0) return;

            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                if (recipe == null) continue;

                CreateRecipeCard(recipe);
            }
        }

        private void ClearCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                {
                    if (Application.isPlaying) Destroy(spawnedCards[i]);
                    else DestroyImmediate(spawnedCards[i]);
                }
            }
            spawnedCards.Clear();
        }

        private void CreateRecipeCard(RecipeSO recipe)
        {
            Transform parent = recipeContainer != null ? recipeContainer : (windowRoot != null ? windowRoot.transform : transform);

            GameObject card = new GameObject($"Card_{recipe.RecipeId}");
            card.transform.SetParent(parent, false);

            var rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480f, 95f);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.18f, 0.92f);

            bool canCraft = craftingManager.CanCraft(recipe);

            // Title & Category text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(0f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(15f, 22f);
            titleRt.sizeDelta = new Vector2(280f, 30f);
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = canCraft ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            titleText.text = $"{recipe.RecipeName} [{recipe.Category}]";

            // Ingredients summary
            GameObject ingObj = new GameObject("Ingredients");
            ingObj.transform.SetParent(card.transform, false);
            var ingRt = ingObj.AddComponent<RectTransform>();
            ingRt.anchorMin = new Vector2(0f, 0.5f);
            ingRt.anchorMax = new Vector2(0f, 0.5f);
            ingRt.pivot = new Vector2(0f, 0.5f);
            ingRt.anchoredPosition = new Vector2(15f, -12f);
            ingRt.sizeDelta = new Vector2(320f, 36f);
            var ingText = ingObj.AddComponent<Text>();
            ingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ingText.fontSize = 13;
            ingText.color = canCraft ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.95f, 0.45f, 0.45f);

            List<string> reqStrings = new List<string>();
            var inv = inventory != null ? inventory : craftingManager.TargetInventory;
            for (int j = 0; j < recipe.Ingredients.Count; j++)
            {
                var req = recipe.Ingredients[j];
                if (req != null && req.Item != null)
                {
                    int owned = inv != null ? inv.GetItemCount(req.Item) : 0;
                    reqStrings.Add($"{req.Item.ItemName}: {owned}/{req.Count}");
                }
            }
            ingText.text = string.Join(" | ", reqStrings);

            // Craft / Build Action Button
            GameObject btnObj = new GameObject("ActionButton");
            btnObj.transform.SetParent(card.transform, false);
            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot = new Vector2(1f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-15f, 0f);
            btnRt.sizeDelta = new Vector2(110f, 44f);

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = canCraft ? new Color(0.2f, 0.65f, 0.35f) : new Color(0.35f, 0.35f, 0.38f);

            var btn = btnObj.AddComponent<Button>();
            btn.interactable = canCraft;

            GameObject btnLabelObj = new GameObject("Label");
            btnLabelObj.transform.SetParent(btnObj.transform, false);
            var labelRt = btnLabelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            var labelText = btnLabelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;

            bool isStructure = recipe.Category == CraftingCategory.Structures ||
                               (recipe.OutputItem != null && recipe.OutputItem.StructurePrefab != null);

            labelText.text = isStructure ? "CONSTRUCT" : "CRAFT";

            btn.onClick.AddListener(() =>
            {
                OnCardButtonClicked(recipe, isStructure);
            });

            spawnedCards.Add(card);
        }

        private void OnCardButtonClicked(RecipeSO recipe, bool isStructure)
        {
            if (isStructure && structurePlacer != null && recipe.OutputItem != null && recipe.OutputItem.StructurePrefab != null)
            {
                var buildable = recipe.OutputItem.StructurePrefab.GetComponent<BuildableStructure>();
                if (buildable != null)
                {
                    SetWindowOpen(false);
                    structurePlacer.StartPlacement(buildable, recipe);
                    return;
                }
            }

            // Normal crafting into inventory
            if (craftingManager != null)
            {
                craftingManager.Craft(recipe);
                RefreshRecipeList();
            }
        }
    }
}
