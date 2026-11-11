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

            var lights = ghostObject.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++) lights[i].enabled = false;

            ghostRenderers = ghostObject.GetComponentsInChildren<Renderer>(true);
            UpdateGhostMaterial(validHologramMaterial);
        }

        private void Update()
        {
            if (!isPlacing || currentPrefab == null) return;

            HandleInput();
            UpdatePlacementRaycast();
        }

        private void HandleInput()
        {
            // Rotate ghost with 'R'
            bool rotatePressed = false;
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
            }
            if (Keyboard.current == null && Mouse.current == null)
            {
                rotatePressed = Input.GetKeyDown(KeyCode.R);
                confirmPressed = Input.GetMouseButtonDown(0);
                cancelPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
            }
#else
            rotatePressed = Input.GetKeyDown(KeyCode.R);
            confirmPressed = Input.GetMouseButtonDown(0);
            cancelPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
#endif

            if (rotatePressed)
            {
                currentYaw = (currentYaw + rotationStepDegrees) % 360f;
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

            if (Physics.Raycast(ray, out RaycastHit hit, 150f, groundLayer, QueryTriggerInteraction.Ignore))
            {
                candidatePos = hit.point;
                hitGround = true;
            }
            else
            {
                // Fallback to ground plane at y = 0
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    candidatePos = ray.GetPoint(enter);
                    hitGround = true;
                }
            }

            if (hitGround)
            {
                currentSnappedPosition = candidatePos;
                Quaternion rot = Quaternion.Euler(0f, currentYaw, 0f);
                ghostObject.transform.position = candidatePos;
                ghostObject.transform.rotation = rot;

                Vector3 extents = currentPrefab.PlacementExtents;
                float playerDist = playerTransform != null ? Vector3.Distance(playerTransform.position, candidatePos) : 0f;

                isCurrentLocationValid = EvaluatePlacement(candidatePos, rot, extents, playerDist);
                UpdateGhostMaterial(isCurrentLocationValid ? validHologramMaterial : blockedHologramMaterial);
            }
        }

        /// <summary>
        /// Pure evaluation function checking distance limits and collision clearance.
        /// Fully callable for automated unit testing.
        /// </summary>
        public bool EvaluatePlacement(Vector3 position, Quaternion rotation, Vector3 extents, float playerDistance)
        {
            if (playerDistance > maxBuildDistance)
            {
                return false;
            }

            // Sync transforms prior to physics overlap test
            Physics.SyncTransforms();

            Vector3 checkCenter = position + Vector3.up * extents.y;
            Vector3 checkHalfExtents = extents * 0.92f; // Slight margin for tolerance

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

            // 1. If tied to a recipe, validate and craft
            if (currentRecipe != null)
            {
                var mgr = CraftingManager;
                if (mgr != null)
                {
                    if (!mgr.CanCraft(currentRecipe)) return false;
                    if (!mgr.Craft(currentRecipe)) return false;
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
