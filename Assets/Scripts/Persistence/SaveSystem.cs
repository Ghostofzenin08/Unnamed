using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Outpost404.Player;
using Outpost404.Survival;
using Outpost404.Inventory;
using Outpost404.Building;
using Outpost404.Environment;
using Outpost404.Objective;

namespace Outpost404.Persistence
{
    /// <summary>
    /// Coordinates JSON serialization and persistence of the entire game world state:
    /// Player transform and vitals, inventory stacks, placed world defenses,
    /// SOS beacon stage progression, day/night diurnal cycle, and expedition statistics.
    /// Supports Quick-save (F5) and Quick-load (F9) with toast notifications.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; set; }

        public const string DefaultSaveFileName = "outpost404_save.json";

        [Header("Target References")]
        [SerializeField] private PlayerStats targetPlayerStats;
        [SerializeField] private InventorySystem targetInventory;
        [SerializeField] private BeaconObjective targetBeacon;
        [SerializeField] private DayNightCycleManager targetDayNightCycle;
        [SerializeField] private MissionStatistics targetStatistics;

        [Header("Prefab Catalogs for Structure Restoration")]
        [SerializeField] private GameObject barricadePrefab;
        [SerializeField] private GameObject heatBeaconPrefab;
        [SerializeField] private List<ItemDataSO> itemCatalog = new List<ItemDataSO>();

        [Header("UI Toast Notification")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private Text toastText;
        [SerializeField] private float toastDisplayDuration = 2.5f;

        private Coroutine activeToastCoroutine;

        public event Action<SaveData> OnGameSaved;
        public event Action<SaveData> OnGameLoaded;

        public List<ItemDataSO> ItemCatalog => itemCatalog;
        public GameObject BarricadePrefab { get => barricadePrefab; set => barricadePrefab = value; }
        public GameObject HeatBeaconPrefab { get => heatBeaconPrefab; set => heatBeaconPrefab = value; }

        public static string GetDefaultSaveFilePath()
        {
            return Path.Combine(Application.persistentDataPath, DefaultSaveFileName);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ResolveSceneReferences();
            if (toastRoot != null) toastRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            HandleQuickSaveLoadInputs();
        }

        public void ConfigureTargets(PlayerStats stats, InventorySystem inv, BeaconObjective bcon = null, DayNightCycleManager dnc = null, MissionStatistics statsTracker = null)
        {
            targetPlayerStats = stats;
            targetInventory = inv;
            targetBeacon = bcon;
            targetDayNightCycle = dnc;
            targetStatistics = statsTracker;
        }

        public void ResolveSceneReferences()
        {
            if (targetPlayerStats == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    targetPlayerStats = player.GetComponent<PlayerStats>();
                    if (targetInventory == null) targetInventory = player.GetComponent<InventorySystem>();
                }
            }
            if (targetBeacon == null) targetBeacon = BeaconObjective.Instance ?? FindFirstObjectByType<BeaconObjective>();
            if (targetDayNightCycle == null) targetDayNightCycle = DayNightCycleManager.Instance ?? FindFirstObjectByType<DayNightCycleManager>();
            if (targetStatistics == null) targetStatistics = MissionStatistics.Instance ?? FindFirstObjectByType<MissionStatistics>();

            if (itemCatalog == null || itemCatalog.Count == 0)
            {
                PopulateItemCatalog();
            }
        }

        public void PopulateItemCatalog()
        {
            if (itemCatalog == null) itemCatalog = new List<ItemDataSO>();
            itemCatalog.Clear();

            // Collect all ItemDataSO loaded or referenced
            var loaded = Resources.FindObjectsOfTypeAll<ItemDataSO>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && !itemCatalog.Contains(loaded[i]))
                {
                    itemCatalog.Add(loaded[i]);
                }
            }
        }

        public ItemDataSO FindItemInCatalog(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || itemCatalog == null) return null;
            for (int i = 0; i < itemCatalog.Count; i++)
            {
                var it = itemCatalog[i];
                if (it != null && (string.Equals(it.ItemId, itemId, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(it.name, itemId, StringComparison.OrdinalIgnoreCase)))
                {
                    return it;
                }
            }
            return null;
        }

        private void HandleQuickSaveLoadInputs()
        {
            bool quickSaveTriggered = false;
            bool quickLoadTriggered = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f5Key.wasPressedThisFrame) quickSaveTriggered = true;
                if (Keyboard.current.f9Key.wasPressedThisFrame) quickLoadTriggered = true;
            }
#endif
            if (Input.GetKeyDown(KeyCode.F5)) quickSaveTriggered = true;
            if (Input.GetKeyDown(KeyCode.F9)) quickLoadTriggered = true;

            if (quickSaveTriggered)
            {
                QuickSave();
            }
            else if (quickLoadTriggered)
            {
                QuickLoad();
            }
        }

        public void QuickSave()
        {
            bool success = SaveGame();
            if (success)
            {
                ShowToast("GAME QUICK-SAVED (F5)");
            }
            else
            {
                ShowToast("FAILED TO QUICK-SAVE");
            }
        }

        public void QuickLoad()
        {
            bool success = LoadGame();
            if (success)
            {
                ShowToast("GAME QUICK-LOADED (F9)");
            }
            else
            {
                ShowToast("NO SAVE FILE FOUND (F5 TO SAVE)");
            }
        }

        public SaveData CaptureGameState()
        {
            ResolveSceneReferences();

            SaveData data = new SaveData
            {
                timestamp = DateTime.UtcNow.Ticks
            };

            // 1. Player Vitals & Transform
            if (targetPlayerStats != null)
            {
                data.playerPosition = new Vector3Data(targetPlayerStats.transform.position);
                data.playerRotationY = targetPlayerStats.transform.eulerAngles.y;
                data.playerHealth = targetPlayerStats.CurrentHealth;
                data.playerHunger = targetPlayerStats.CurrentHunger;
                data.playerStamina = targetPlayerStats.CurrentStamina;
                data.playerBodyTemperature = targetPlayerStats.CurrentBodyTemperature;
            }

            // 2. Inventory Items
            if (targetInventory != null)
            {
                data.inventorySlots.Clear();
                var slots = targetInventory.Slots;
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (!slot.IsEmpty && slot.item != null)
                    {
                        data.inventorySlots.Add(new SavedInventorySlot(slot.item.ItemId, slot.quantity));
                    }
                    else
                    {
                        data.inventorySlots.Add(new SavedInventorySlot(string.Empty, 0));
                    }
                }
            }

            // 3. Placed World Structures
            data.placedStructures.Clear();
            var structures = FindObjectsByType<BuildableStructure>(FindObjectsSortMode.None);
            for (int i = 0; i < structures.Length; i++)
            {
                var s = structures[i];
                if (s != null && !s.IsDead)
                {
                    data.placedStructures.Add(new SavedStructure(
                        s.Type,
                        s.transform.position,
                        s.transform.eulerAngles.y,
                        s.CurrentDurability,
                        s.MaxDurability
                    ));
                }
            }

            // 4. SOS Beacon Objective
            if (targetBeacon != null)
            {
                data.beaconRepairStage = targetBeacon.CurrentStage;
                data.isBeaconCountdownActive = targetBeacon.IsCountdownActive;
                data.beaconCountdownRemaining = targetBeacon.CountdownRemaining;
            }

            // 5. Day / Night Cycle
            if (targetDayNightCycle != null)
            {
                data.dayCount = targetDayNightCycle.CurrentDay;
                data.normalizedDiurnalTime = targetDayNightCycle.NormalizedTime;
            }

            // 6. Mission Statistics
            if (targetStatistics != null)
            {
                data.enemiesSlain = targetStatistics.EnemiesSlain;
                data.resourcesHarvested = targetStatistics.ResourcesHarvested;
                data.structuresBuilt = targetStatistics.StructuresBuilt;
            }

            return data;
        }

        public void ApplyGameState(SaveData data)
        {
            if (data == null) return;
            ResolveSceneReferences();

            // 1. Restore Player Transform and Vitals
            if (targetPlayerStats != null)
            {
                targetPlayerStats.transform.position = data.playerPosition.ToVector3();
                targetPlayerStats.transform.rotation = Quaternion.Euler(0f, data.playerRotationY, 0f);
                targetPlayerStats.SetStatsDirect(data.playerHealth, data.playerHunger, data.playerStamina, data.playerBodyTemperature);
            }

            // 2. Restore Inventory
            if (targetInventory != null)
            {
                targetInventory.InitializeSlots();
                var slots = targetInventory.Slots;
                for (int i = 0; i < slots.Count; i++)
                {
                    slots[i].Clear();
                }

                if (data.inventorySlots != null)
                {
                    for (int i = 0; i < data.inventorySlots.Count && i < slots.Count; i++)
                    {
                        var savedSlot = data.inventorySlots[i];
                        if (!string.IsNullOrEmpty(savedSlot.itemId) && savedSlot.quantity > 0)
                        {
                            var itemData = FindItemInCatalog(savedSlot.itemId);
                            if (itemData != null)
                            {
                                slots[i].item = itemData;
                                slots[i].quantity = savedSlot.quantity;
                            }
                        }
                    }
                }
            }

            // 3. Restore Placed World Structures
            var existingStructures = FindObjectsByType<BuildableStructure>(FindObjectsSortMode.None);
            for (int i = 0; i < existingStructures.Length; i++)
            {
                if (existingStructures[i] != null)
                {
                    Destroy(existingStructures[i].gameObject);
                }
            }

            if (data.placedStructures != null)
            {
                for (int i = 0; i < data.placedStructures.Count; i++)
                {
                    var saved = data.placedStructures[i];
                    GameObject prefab = (saved.structureType == StructureType.HeatBeacon) ? heatBeaconPrefab : barricadePrefab;
                    if (prefab != null)
                    {
                        GameObject spawned = Instantiate(prefab, saved.position.ToVector3(), Quaternion.Euler(0f, saved.rotationY, 0f));
                        var bs = spawned.GetComponent<BuildableStructure>();
                        if (bs != null)
                        {
                            bs.Type = saved.structureType;
                            bs.MaxDurability = Mathf.Max(1f, saved.maxDurability);
                            bs.SetCurrentDurabilityDirect(saved.currentDurability);
                        }
                    }
                }
            }

            // 4. Restore Beacon Objective
            if (targetBeacon != null)
            {
                targetBeacon.SetStageDirect(data.beaconRepairStage);
                targetBeacon.SetCountdownDirect(data.isBeaconCountdownActive, data.beaconCountdownRemaining);
            }

            // 5. Restore Day/Night Cycle
            if (targetDayNightCycle != null)
            {
                targetDayNightCycle.SetTimeAndDay(data.normalizedDiurnalTime, data.dayCount);
            }

            // 6. Restore Mission Statistics
            if (targetStatistics != null)
            {
                targetStatistics.SetStats(data.dayCount, data.enemiesSlain, data.resourcesHarvested, data.structuresBuilt);
            }

            OnGameLoaded?.Invoke(data);
        }

        public string SerializeToJson(SaveData data)
        {
            if (data == null) return string.Empty;
            return JsonUtility.ToJson(data, true);
        }

        public SaveData DeserializeFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<SaveData>(json);
        }

        public bool SaveGame(string customPath = null)
        {
            try
            {
                string path = !string.IsNullOrEmpty(customPath) ? customPath : GetDefaultSaveFilePath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                SaveData data = CaptureGameState();
                string json = SerializeToJson(data);
                File.WriteAllText(path, json);

                OnGameSaved?.Invoke(data);
                Debug.Log($"<color=green>[SaveSystem]</color> Game saved successfully to: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to save game: {ex.Message}");
                return false;
            }
        }

        public bool LoadGame(string customPath = null)
        {
            try
            {
                string path = !string.IsNullOrEmpty(customPath) ? customPath : GetDefaultSaveFilePath();
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[SaveSystem] No save file found at: {path}");
                    return false;
                }

                string json = File.ReadAllText(path);
                SaveData data = DeserializeFromJson(json);
                if (data == null)
                {
                    Debug.LogError("[SaveSystem] Failed to parse save file JSON.");
                    return false;
                }

                ApplyGameState(data);
                Debug.Log($"<color=green>[SaveSystem]</color> Game loaded successfully from: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load game: {ex.Message}");
                return false;
            }
        }

        public bool SaveFileExists(string customPath = null)
        {
            string path = !string.IsNullOrEmpty(customPath) ? customPath : GetDefaultSaveFilePath();
            return File.Exists(path);
        }

        public void ShowToast(string message)
        {
            if (toastText != null) toastText.text = message;
            if (toastRoot != null)
            {
                toastRoot.SetActive(true);
                if (activeToastCoroutine != null) StopCoroutine(activeToastCoroutine);
                if (gameObject.activeInHierarchy)
                {
                    activeToastCoroutine = StartCoroutine(ToastFadeRoutine());
                }
            }
        }

        private IEnumerator ToastFadeRoutine()
        {
            yield return new WaitForSeconds(toastDisplayDuration);
            if (toastRoot != null) toastRoot.SetActive(false);
            activeToastCoroutine = null;
        }
    }
}
