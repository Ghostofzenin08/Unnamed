using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Outpost404.Crafting;
using Outpost404.Inventory;

namespace Outpost404.Building
{
    /// <summary>
    /// Coordinates real-time holographic ghost structure placement.
    /// Casts rays against the ground plane, verifies collision box clearance,
    /// paints ghost materials green (valid) or red (blocked), and constructs structures on confirm.
    /// </summary>
    public class StructurePlacer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private InventorySystem inventory;
        [SerializeField] private CraftingManager craftingManager;

        [Header("Placement Layers & Range")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField] private float maxBuildDistance = 10f;
        [SerializeField] private float rotationStepDegrees = 45f;

        [Header("Hologram Materials")]
        [SerializeField] private Material validHologramMaterial;
        [SerializeField] private Material blockedHologramMaterial;

        // Active placement state
        private bool isPlacing = false;
        private BuildableStructure currentPrefab;
        private RecipeSO currentRecipe;
        private ItemDataSO currentRequiredItem;
        private GameObject ghostObject;
        private Renderer[] ghostRenderers;
        private float currentYaw = 0f;
        private bool isCurrentLocationValid = false;
        private Vector3 currentSnappedPosition;

        public event Action<BuildableStructure, Vector3> OnStructurePlaced;
        public event Action OnPlacementCancelled;

        public bool IsPlacing => isPlacing;
        public bool IsCurrentLocationValid => isCurrentLocationValid;
        public Vector3 CurrentSnappedPosition => currentSnappedPosition;
        public float MaxBuildDistance { get => maxBuildDistance; set => maxBuildDistance = Mathf.Max(1f, value); }
        public Transform PlayerTransform { get => playerTransform; set => playerTransform = value; }
        public LayerMask GroundLayer { get => groundLayer; set => groundLayer = value; }
        public LayerMask ObstacleLayers { get => obstacleLayers; set => obstacleLayers = value; }
        public Material ValidHologramMaterial { get => validHologramMaterial; set => validHologramMaterial = value; }
        public Material BlockedHologramMaterial { get => blockedHologramMaterial; set => blockedHologramMaterial = value; }
        public InventorySystem Inventory { get => inventory; set => inventory = value; }
        public CraftingManager CraftingManager { get => craftingManager; set => craftingManager = value; }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
            if (inventory == null) inventory = GetComponentInParent<InventorySystem>();
            if (craftingManager == null) craftingManager = GetComponentInParent<CraftingManager>();

            EnsureDefaultHologramMaterials();
        }

        private void EnsureDefaultHologramMaterials()
        {
            if (validHologramMaterial == null)
            {
                validHologramMaterial = new Material(Shader.Find("Standard"));
                validHologramMaterial.color = new Color(0.1f, 0.9f, 0.2f, 0.45f);
                SetMaterialTransparent(validHologramMaterial);
            }
            if (blockedHologramMaterial == null)
            {
                blockedHologramMaterial = new Material(Shader.Find("Standard"));
                blockedHologramMaterial.color = new Color(0.95f, 0.15f, 0.15f, 0.45f);
                SetMaterialTransparent(blockedHologramMaterial);
            }
        }

        private static void SetMaterialTransparent(Material mat)
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        public void StartPlacement(BuildableStructure structurePrefab, RecipeSO recipe = null, ItemDataSO requiredItem = null)
        {
            if (structurePrefab == null) return;

            CancelPlacement();

            currentPrefab = structurePrefab;
            currentRecipe = recipe;
            currentRequiredItem = requiredItem;
            isPlacing = true;
            currentYaw = 0f;

            CreateGhost();
        }

        /// <summary>
        /// Explicitly updates ghost placement location and evaluates collision/distance validity.
        /// Used by input raycasting and automated test suites.
        /// </summary>
        public bool SetPlacementLocation(Vector3 position, Quaternion rotation)
        {
            currentSnappedPosition = position;
            currentYaw = rotation.eulerAngles.y;
            Vector3 extents = currentPrefab != null ? currentPrefab.PlacementExtents : Vector3.one;
            float playerDist = playerTransform != null ? Vector3.Distance(playerTransform.position, position) : 0f;
            isCurrentLocationValid = EvaluatePlacement(position, rotation, extents, playerDist);

            if (ghostObject != null)
            {
                ghostObject.transform.position = position;
                ghostObject.transform.rotation = rotation;
                UpdateGhostMaterial(isCurrentLocationValid ? validHologramMaterial : blockedHologramMaterial);
            }

            return isCurrentLocationValid;
        }

        public void CancelPlacement()
        {
            if (ghostObject != null)
            {
                if (Application.isPlaying) Destroy(ghostObject);
                else DestroyImmediate(ghostObject);
            }

            ghostObject = null;
            ghostRenderers = null;
            currentPrefab = null;
            currentRecipe = null;
            currentRequiredItem = null;
            isPlacing = false;
            isCurrentLocationValid = false;

            OnPlacementCancelled?.Invoke();
        }

        private void CreateGhost()
        {
            if (currentPrefab == null) return;

            ghostObject = Instantiate(currentPrefab.gameObject);
            ghostObject.name = $"{currentPrefab.name}_Ghost";

            // Strip functional logic from the ghost
            var structures = ghostObject.GetComponentsInChildren<BuildableStructure>(true);
            for (int i = 0; i < structures.Length; i++) DestroyImmediate(structures[i]);

            var colliders = ghostObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) DestroyImmediate(colliders[i]);

            var obstacles = ghostObject.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true);
            for (int i = 0; i < obstacles.Length; i++) DestroyImmediate(obstacles[i]);

            var lights = ghostObject.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) lights[i].enabled = false;

            ghostRenderers = ghostObject.GetComponentsInChildren<Renderer>(true);
            UpdateGhostMaterial(validHologramMaterial);
        }

        private void Update()
        {
            if (!isPlacing || currentPrefab == null) return;

            // Auto-cancel placement if player dies
            if (playerTransform != null)
            {
                var stats = playerTransform.GetComponent<Outpost404.Survival.PlayerStats>();
                if (stats != null && stats.IsDead)
                {
                    CancelPlacement();
                    return;
                }
            }

            // Raycast and update position & validity FIRST to prevent single-frame input lag or stale confirmation
            UpdatePlacementRaycast();
            HandleInput();
        }

        private void HandleInput()
        {
            // Do not confirm placement if pointer is over UI buttons
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Rotate ghost with 'R' or Mouse Wheel Scroll
            bool rotatePressed = false;
            float scrollDelta = 0f;
            bool confirmPressed = false;
            bool cancelPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame) rotatePressed = true;
                if (Keyboard.current.escapeKey.wasPressedThisFrame) cancelPressed = true;
            }
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) confirmPressed = true;
                if (Mouse.current.rightButton.wasPressedThisFrame) cancelPressed = true;
                scrollDelta = Mouse.current.scroll.ReadValue().y;
            }
            if (Keyboard.current == null && Mouse.current == null)
            {
                rotatePressed = Input.GetKeyDown(KeyCode.R);
                confirmPressed = Input.GetMouseButtonDown(0);
                cancelPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
                scrollDelta = Input.mouseScrollDelta.y;
            }
#else
            rotatePressed = Input.GetKeyDown(KeyCode.R);
            confirmPressed = Input.GetMouseButtonDown(0);
            cancelPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
            scrollDelta = Input.mouseScrollDelta.y;
#endif

            if (rotatePressed)
            {
                currentYaw = (currentYaw + rotationStepDegrees) % 360f;
            }
            else if (Mathf.Abs(scrollDelta) > 0.05f)
            {
                if (scrollDelta > 0f)
                {
                    currentYaw = (currentYaw + rotationStepDegrees) % 360f;
                }
                else
                {
                    currentYaw = (currentYaw - rotationStepDegrees + 360f) % 360f;
                }
            }

            if (cancelPressed)
            {
                CancelPlacement();
                return;
            }

            if (confirmPressed && isCurrentLocationValid)
            {
                TryConfirmPlacement();
            }
        }

        private void UpdatePlacementRaycast()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null || ghostObject == null) return;

            Vector3 mousePos = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 m = Mouse.current.position.ReadValue();
                mousePos = new Vector3(m.x, m.y, 0f);
            }
            else
            {
                mousePos = Input.mousePosition;
            }
#else
            mousePos = Input.mousePosition;
#endif

            Ray ray = targetCamera.ScreenPointToRay(mousePos);
            Vector3 candidatePos = Vector3.zero;
            bool hitGround = false;

            // Prioritize ground raycast with walkable slope check
            if (Physics.Raycast(ray, out RaycastHit hit, 150f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y >= 0.55f)
                {
                    candidatePos = hit.point;
                    hitGround = true;
                }
            }

            // Fallback to ground plane at y = 0
            if (!hitGround)
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    candidatePos = ray.GetPoint(enter);
                    hitGround = true;
                }
            }

            if (hitGround)
            {
                if (!ghostObject.activeSelf) ghostObject.SetActive(true);
                Quaternion rot = Quaternion.Euler(0f, currentYaw, 0f);
                SetPlacementLocation(candidatePos, rot);
            }
            else
            {
                isCurrentLocationValid = false;
                if (ghostObject.activeSelf) ghostObject.SetActive(false);
            }
        }

        /// <summary>
        /// Pure evaluation function checking distance limits and collision clearance.
        /// Fully callable for automated unit testing.
        /// </summary>
        public bool EvaluatePlacement(Vector3 position, Quaternion rotation, Vector3 extents, float playerDistance)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                float.IsInfinity(position.x) || float.IsInfinity(playerDistance))
            {
                return false;
            }

            if (playerDistance > maxBuildDistance)
            {
                return false;
            }

            // Sync transforms prior to physics overlap test
            Physics.SyncTransforms();

            Vector3 safeExtents = new Vector3(
                Mathf.Max(0.1f, Mathf.Abs(extents.x)),
                Mathf.Max(0.1f, Mathf.Abs(extents.y)),
                Mathf.Max(0.1f, Mathf.Abs(extents.z))
            );

            Vector3 checkCenter = position + Vector3.up * safeExtents.y;
            Vector3 checkHalfExtents = safeExtents * 0.92f; // Slight margin for tolerance

            Collider[] overlaps = Physics.OverlapBox(checkCenter, checkHalfExtents, rotation, obstacleLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                // Ignore the ghost itself
                if (ghostObject != null && (overlaps[i].gameObject == ghostObject || overlaps[i].transform.IsChildOf(ghostObject.transform)))
                {
                    continue;
                }

                // If collision layer includes ground, ignore pure ground colliders
                if (overlaps[i].name.StartsWith("Ground") || overlaps[i].name.IndexOf("ground", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                // Any other obstacle blocks placement
                return false;
            }

            return true;
        }

        /// <summary>
        /// Executes construction at the snapped location.
        /// Consumes ingredients or structure items, spawns structure, and fires event.
        /// </summary>
        public bool TryConfirmPlacement()
        {
            if (!isPlacing || currentPrefab == null || !isCurrentLocationValid)
            {
                return false;
            }

            // 1. If tied to a recipe, validate and consume ingredients (avoids duplicate item in inventory)
            if (currentRecipe != null)
            {
                var mgr = CraftingManager;
                if (mgr != null)
                {
                    if (!mgr.ConsumeIngredients(currentRecipe)) return false;
                }
                else
                {
                    var inv = Inventory;
                    if (inv == null || !currentRecipe.CanCraft(inv)) return false;

                    for (int i = 0; i < currentRecipe.Ingredients.Count; i++)
                    {
                        var req = currentRecipe.Ingredients[i];
                        if (req != null && req.Item != null && req.Count > 0)
                        {
                            inv.RemoveItem(req.Item, req.Count);
                        }
                    }
                }
            }
            // 2. Or if tied to a pre-crafted item, consume from inventory
            else if (currentRequiredItem != null)
            {
                var inv = Inventory;
                if (inv == null || !inv.HasItem(currentRequiredItem, 1)) return false;
                inv.RemoveItem(currentRequiredItem, 1);
            }

            // 3. Spawn the actual structure in the world
            Quaternion rot = Quaternion.Euler(0f, currentYaw, 0f);
            GameObject spawnedObj = Instantiate(currentPrefab.gameObject, currentSnappedPosition, rot);
            spawnedObj.name = currentPrefab.name;

            var structure = spawnedObj.GetComponent<BuildableStructure>();
            if (structure != null)
            {
                structure.InitializeStructure();
            }

            OnStructurePlaced?.Invoke(structure, currentSnappedPosition);
            Outpost404.Objective.MissionStatistics.Instance?.RecordStructureBuilt();

            // Finish placement
            CancelPlacement();
            return true;
        }

        private void UpdateGhostMaterial(Material mat)
        {
            if (ghostRenderers == null || mat == null) return;
            for (int i = 0; i < ghostRenderers.Length; i++)
            {
                if (ghostRenderers[i] != null)
                {
                    ghostRenderers[i].sharedMaterial = mat;
                }
            }
        }
    }
}
