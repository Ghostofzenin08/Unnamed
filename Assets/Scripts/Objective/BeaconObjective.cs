using System;
using System.Collections.Generic;
using UnityEngine;
using Outpost404.Inventory;
using Outpost404.AI;
using Outpost404.Audio;

namespace Outpost404.Objective
{
    [Serializable]
    public class BeaconStageRequirement
    {
        public string stageName;
        [TextArea(2, 3)]
        public string description;
        public ItemDataSO primaryItem;
        public int primaryRequired;
        public ItemDataSO secondaryItem;
        public int secondaryRequired;

        public BeaconStageRequirement(string name, string desc, ItemDataSO pItem, int pReq, ItemDataSO sItem, int sReq)
        {
            stageName = name;
            description = desc;
            primaryItem = pItem;
            primaryRequired = pReq;
            secondaryItem = sItem;
            secondaryRequired = sReq;
        }
    }

    /// <summary>
    /// Coordinates the multi-stage SOS Extraction Distress Beacon in Sector Alpha.
    /// Manages 3-stage material repair sequence, 60-second extraction countdown,
    /// nocturnal anomaly prowler surge defense, and victory resolution.
    /// </summary>
    [DisallowMultipleComponent]
    public class BeaconObjective : MonoBehaviour
    {
        public static BeaconObjective Instance { get; set; }

        [Header("Beacon Repair Progression")]
        [Tooltip("Current repair stage: 0=Unrepaired, 1=Power Matrix fixed, 2=Dish fixed, 3=Core fixed (Ready).")]
        [SerializeField] private int currentStage = 0;
        [SerializeField] private List<BeaconStageRequirement> stages = new List<BeaconStageRequirement>();

        [Header("Extraction Countdown & Defense")]
        [SerializeField] private float countdownDuration = 60f;
        [SerializeField] private float countdownRemaining = 60f;
        [SerializeField] private bool isCountdownActive = false;
        [SerializeField] private bool isVictory = false;

        [Header("Horde Surge Defense")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private List<Transform> hordeSpawnPoints = new List<Transform>();
        [SerializeField] private float surgeSpawnInterval = 10f;
        private float surgeSpawnTimer = 0f;

        // Decoupled Observation Events
        public event Action<int, string> OnStageRepaired; // (newStageIndex, stageName)
        public event Action<float> OnCountdownStarted;    // (totalDuration)
        public event Action<float, float> OnCountdownTick; // (remainingTime, totalDuration)
        public event Action OnVictoryAchieved;

        public int CurrentStage => currentStage;
        public int TotalStages => stages != null ? stages.Count : 3;
        public bool IsRepaired => currentStage >= TotalStages;
        public float StageProgress => TotalStages > 0 ? Mathf.Clamp01((float)currentStage / TotalStages) : 0f;
        public bool IsCountdownActive => isCountdownActive;
        public float CountdownRemaining => countdownRemaining;
        public float CountdownDuration => countdownDuration;
        public bool IsVictory => isVictory;
        public IReadOnlyList<BeaconStageRequirement> Stages => stages;

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

            EnsureDefaultStages();
            countdownRemaining = countdownDuration;
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
            if (isCountdownActive && !isVictory)
            {
                TickCountdown(Time.deltaTime);
            }
        }

        public void EnsureDefaultStages()
        {
            if (stages != null && stages.Count >= 3) return;

            stages = new List<BeaconStageRequirement>
            {
                new BeaconStageRequirement(
                    "Stage 1: Power Matrix",
                    "Restore backup battery cells and capacitor matrix with conductive crystalline ore and scrap.",
                    null, 4, null, 3),
                new BeaconStageRequirement(
                    "Stage 2: Signal Dish Array",
                    "Assemble parabolic dish scaffolding and transmission waveguides.",
                    null, 6, null, 4),
                new BeaconStageRequirement(
                    "Stage 3: Activation Core",
                    "Ignite high-output distress transponder using bio-fuel anomaly meat and insulated scrap.",
                    null, 3, null, 5)
            };
        }

        public void ConfigureStages(BeaconStageRequirement stage1, BeaconStageRequirement stage2, BeaconStageRequirement stage3)
        {
            stages = new List<BeaconStageRequirement> { stage1, stage2, stage3 };
        }

        public BeaconStageRequirement GetCurrentStageRequirement()
        {
            if (stages == null || currentStage < 0 || currentStage >= stages.Count) return null;
            return stages[currentStage];
        }

        public bool CanRepairCurrentStage(InventorySystem inventory)
        {
            return CanRepairStage(currentStage, inventory);
        }

        public bool CanRepairStage(int stageIndex, InventorySystem inventory)
        {
            if (inventory == null) return false;
            if (stages == null || stageIndex < 0 || stageIndex >= stages.Count) return false;

            var req = stages[stageIndex];
            if (req.primaryItem != null && inventory.GetItemCount(req.primaryItem) < req.primaryRequired)
            {
                return false;
            }
            if (req.secondaryItem != null && inventory.GetItemCount(req.secondaryItem) < req.secondaryRequired)
            {
                return false;
            }

            return true;
        }

        public bool TryRepairCurrentStage(InventorySystem inventory)
        {
            if (inventory == null || IsRepaired) return false;
            if (!CanRepairCurrentStage(inventory)) return false;

            var req = stages[currentStage];
            if (req.primaryItem != null && req.primaryRequired > 0)
            {
                inventory.RemoveItem(req.primaryItem, req.primaryRequired);
            }
            if (req.secondaryItem != null && req.secondaryRequired > 0)
            {
                inventory.RemoveItem(req.secondaryItem, req.secondaryRequired);
            }

            currentStage++;
            string stageName = currentStage <= stages.Count ? req.stageName : "All Stages Completed";

            AudioManager.Instance?.PlayCraftingSuccess();
            OnStageRepaired?.Invoke(currentStage, stageName);

            return true;
        }

        public bool TryActivateExtraction()
        {
            if (!IsRepaired || isCountdownActive || isVictory) return false;

            isCountdownActive = true;
            countdownRemaining = countdownDuration;
            surgeSpawnTimer = 0f;

            AudioManager.Instance?.PlayPickupChime();
            OnCountdownStarted?.Invoke(countdownDuration);

            // Immediate initial horde surge
            SpawnSurgeWave();

            return true;
        }

        public void TickCountdown(float deltaTime)
        {
            if (!isCountdownActive || isVictory || deltaTime <= 0f || float.IsNaN(deltaTime)) return;

            countdownRemaining -= deltaTime;
            OnCountdownTick?.Invoke(Mathf.Max(0f, countdownRemaining), countdownDuration);

            // Spawn periodic surge waves during defense
            surgeSpawnTimer += deltaTime;
            if (surgeSpawnTimer >= surgeSpawnInterval)
            {
                surgeSpawnTimer = 0f;
                SpawnSurgeWave();
            }

            if (countdownRemaining <= 0f)
            {
                countdownRemaining = 0f;
                isCountdownActive = false;
                isVictory = true;
                TriggerVictory();
            }
        }

        private void SpawnSurgeWave()
        {
            if (!Application.isPlaying) return;

            // Alert all existing enemies to enter surge state and pursue beacon/player
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && !enemies[i].IsDead)
                {
                    enemies[i].ApplyNightSurge(true);
                }
            }

            // Spawn additional horde beasts at perimeter spawn points if assigned
            if (enemyPrefab != null && hordeSpawnPoints != null && hordeSpawnPoints.Count > 0)
            {
                Transform sp = hordeSpawnPoints[UnityEngine.Random.Range(0, hordeSpawnPoints.Count)];
                if (sp != null)
                {
                    GameObject spawned = Instantiate(enemyPrefab, sp.position, Quaternion.identity);
                    var ctrl = spawned.GetComponent<EnemyController>();
                    if (ctrl != null) ctrl.ApplyNightSurge(true);
                }
            }
        }

        public void TriggerVictory()
        {
            isVictory = true;
            isCountdownActive = false;
            countdownRemaining = 0f;

            AudioManager.Instance?.PlayCraftingSuccess();
            OnVictoryAchieved?.Invoke();
        }

        public void SetStageDirect(int stage)
        {
            currentStage = Mathf.Clamp(stage, 0, TotalStages);
            OnStageRepaired?.Invoke(currentStage, GetCurrentStageRequirement()?.stageName ?? "Completed");
        }

        public void SetCountdownDirect(bool active, float remaining)
        {
            isCountdownActive = active;
            countdownRemaining = Mathf.Max(0f, remaining);
            if (isCountdownActive)
            {
                OnCountdownStarted?.Invoke(countdownDuration);
                OnCountdownTick?.Invoke(countdownRemaining, countdownDuration);
            }
        }

        public void ResetObjective()
        {
            currentStage = 0;
            isCountdownActive = false;
            countdownRemaining = countdownDuration;
            isVictory = false;
            surgeSpawnTimer = 0f;
        }
    }
}
