using System;
using UnityEngine;
using UnityEngine.UI;
using Outpost404.Objective;
using Outpost404.Inventory;

namespace Outpost404.UI
{
    /// <summary>
    /// Displays the SOS Extraction Distress Beacon interface on the HUD canvas.
    /// Presents the 3-stage repair sequence, required materials vs current inventory count,
    /// a progress bar, the 'REPAIR STAGE' button, extraction trigger, and 60-second countdown banner.
    /// </summary>
    [DisallowMultipleComponent]
    public class BeaconTerminalUI : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private BeaconObjective targetBeacon;
        [SerializeField] private InventorySystem playerInventory;
        [SerializeField] private Transform playerTransform;

        [Header("Terminal Window & Panels")]
        [SerializeField] private GameObject terminalPanel;
        [SerializeField] private Text headerText;
        [SerializeField] private Text stageTitleText;
        [SerializeField] private Text stageDescriptionText;
        [SerializeField] private Text requiredItemsText;
        [SerializeField] private Slider stageProgressBar;
        [SerializeField] private Text progressPercentText;

        [Header("Action Buttons")]
        [SerializeField] private Button repairButton;
        [SerializeField] private Text repairButtonText;
        [SerializeField] private Button activateExtractionButton;
        [SerializeField] private Text activateButtonText;

        [Header("Extraction Defense Countdown Banner")]
        [SerializeField] private GameObject countdownBanner;
        [SerializeField] private Text countdownTimerText;
        [SerializeField] private Slider countdownProgressBar;

        [Header("Proximity Detection")]
        [SerializeField] private float interactionRadius = 5.0f;
        [SerializeField] private bool autoOpenNearBeacon = true;

        public BeaconObjective TargetBeacon { get => targetBeacon; set => targetBeacon = value; }
        public InventorySystem PlayerInventory { get => playerInventory; set => playerInventory = value; }
        public GameObject TerminalPanel => terminalPanel;
        public GameObject CountdownBanner => countdownBanner;
        public Button RepairButton => repairButton;
        public Button ActivateExtractionButton => activateExtractionButton;
        public Slider StageProgressBar => stageProgressBar;
        public Text RequiredItemsText => requiredItemsText;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToBeacon();
            UpdateUI();

            if (countdownBanner != null) countdownBanner.SetActive(false);
        }

        private void OnDestroy()
        {
            UnsubscribeFromBeacon();
        }

        private void Update()
        {
            HandleProximityCheck();
        }

        public void ResolveReferences()
        {
            if (targetBeacon == null) targetBeacon = BeaconObjective.Instance ?? FindFirstObjectByType<BeaconObjective>();
            if (playerInventory == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerInventory = player.GetComponent<InventorySystem>();
                    playerTransform = player.transform;
                }
            }
        }

        private void WireButtons()
        {
            if (repairButton != null)
            {
                repairButton.onClick.RemoveAllListeners();
                repairButton.onClick.AddListener(OnRepairButtonClicked);
            }

            if (activateExtractionButton != null)
            {
                activateExtractionButton.onClick.RemoveAllListeners();
                activateExtractionButton.onClick.AddListener(OnActivateExtractionClicked);
            }
        }

        private void SubscribeToBeacon()
        {
            if (targetBeacon == null) return;
            targetBeacon.OnStageRepaired += HandleStageRepaired;
            targetBeacon.OnCountdownStarted += HandleCountdownStarted;
            targetBeacon.OnCountdownTick += HandleCountdownTick;
            targetBeacon.OnVictoryAchieved += HandleVictoryAchieved;
        }

        private void UnsubscribeFromBeacon()
        {
            if (targetBeacon == null) return;
            targetBeacon.OnStageRepaired -= HandleStageRepaired;
            targetBeacon.OnCountdownStarted -= HandleCountdownStarted;
            targetBeacon.OnCountdownTick -= HandleCountdownTick;
            targetBeacon.OnVictoryAchieved -= HandleVictoryAchieved;
        }

        private void HandleProximityCheck()
        {
            if (!autoOpenNearBeacon || targetBeacon == null || playerTransform == null || terminalPanel == null) return;

            // When countdown is active, hide terminal panel to not obstruct combat defense
            if (targetBeacon.IsCountdownActive)
            {
                if (terminalPanel.activeSelf) terminalPanel.SetActive(false);
                return;
            }

            float distance = Vector3.Distance(playerTransform.position, targetBeacon.transform.position);
            bool inRange = distance <= interactionRadius;

            if (inRange && !terminalPanel.activeSelf)
            {
                terminalPanel.SetActive(true);
                UpdateUI();
            }
            else if (!inRange && terminalPanel.activeSelf)
            {
                terminalPanel.SetActive(false);
            }
        }

        public void UpdateUI()
        {
            ResolveReferences();
            if (targetBeacon == null) return;

            bool isAllRepaired = targetBeacon.IsRepaired;
            bool isCountdown = targetBeacon.IsCountdownActive;

            // 1. Stage Progress Bar
            float progress = targetBeacon.StageProgress;
            if (stageProgressBar != null) stageProgressBar.value = progress;
            if (progressPercentText != null) progressPercentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";

            // 2. Stage Details
            var currentReq = targetBeacon.GetCurrentStageRequirement();
            if (currentReq != null && !isAllRepaired)
            {
                if (stageTitleText != null) stageTitleText.text = currentReq.stageName;
                if (stageDescriptionText != null) stageDescriptionText.text = currentReq.description;

                // Ingredients required vs held
                int pCount = (playerInventory != null && currentReq.primaryItem != null) ? playerInventory.GetItemCount(currentReq.primaryItem) : 0;
                int sCount = (playerInventory != null && currentReq.secondaryItem != null) ? playerInventory.GetItemCount(currentReq.secondaryItem) : 0;

                string pName = currentReq.primaryItem != null ? currentReq.primaryItem.ItemName : "Primary Material";
                string sName = currentReq.secondaryItem != null ? currentReq.secondaryItem.ItemName : "Secondary Material";

                string pColor = pCount >= currentReq.primaryRequired ? "#44FF44" : "#FF6666";
                string sColor = sCount >= currentReq.secondaryRequired ? "#44FF44" : "#FF6666";

                if (requiredItemsText != null)
                {
                    requiredItemsText.text = $"- <color={pColor}>{pName}: {pCount}/{currentReq.primaryRequired}</color>\n" +
                                            $"- <color={sColor}>{sName}: {sCount}/{currentReq.secondaryRequired}</color>";
                }

                // Repair Button
                bool canRepair = targetBeacon.CanRepairCurrentStage(playerInventory);
                if (repairButton != null)
                {
                    repairButton.gameObject.SetActive(true);
                    repairButton.interactable = canRepair;
                }
                if (repairButtonText != null)
                {
                    repairButtonText.text = canRepair ? "REPAIR STAGE" : "INSUFFICIENT RESOURCES";
                }

                if (activateExtractionButton != null) activateExtractionButton.gameObject.SetActive(false);
            }
            else
            {
                // All stages complete
                if (stageTitleText != null) stageTitleText.text = "ALL REPAIRS COMPLETED";
                if (stageDescriptionText != null) stageDescriptionText.text = "Beacon dish aligned and power matrix operational. Ready to broadcast distress SOS evacuation transponder.";
                if (requiredItemsText != null) requiredItemsText.text = "<color=#44FF44>[READY] All systems operational. Prepare for extraction defense.</color>";

                if (repairButton != null) repairButton.gameObject.SetActive(false);

                if (activateExtractionButton != null)
                {
                    activateExtractionButton.gameObject.SetActive(!isCountdown);
                    activateExtractionButton.interactable = !isCountdown;
                }
                if (activateButtonText != null) activateButtonText.text = "ACTIVATE SOS EXTRACTION";
            }
        }

        public void OnRepairButtonClicked()
        {
            ResolveReferences();
            if (targetBeacon != null && playerInventory != null)
            {
                bool success = targetBeacon.TryRepairCurrentStage(playerInventory);
                if (success)
                {
                    UpdateUI();
                }
            }
        }

        public void OnActivateExtractionClicked()
        {
            ResolveReferences();
            if (targetBeacon != null)
            {
                bool success = targetBeacon.TryActivateExtraction();
                if (success)
                {
                    if (terminalPanel != null) terminalPanel.SetActive(false);
                }
            }
        }

        private void HandleStageRepaired(int newStage, string stageName)
        {
            UpdateUI();
        }

        private void HandleCountdownStarted(float duration)
        {
            if (countdownBanner != null)
            {
                countdownBanner.SetActive(true);
            }
            if (countdownProgressBar != null)
            {
                countdownProgressBar.value = 1.0f;
            }
            if (countdownTimerText != null)
            {
                countdownTimerText.text = $"EXTRACTION IN: {Mathf.CeilToInt(duration)}s";
            }
        }

        private void HandleCountdownTick(float remaining, float total)
        {
            if (countdownBanner != null && !countdownBanner.activeSelf) countdownBanner.SetActive(true);

            if (countdownTimerText != null)
            {
                countdownTimerText.text = $"EXTRACTION IN: {Mathf.CeilToInt(remaining)}s";
            }
            if (countdownProgressBar != null && total > 0f)
            {
                countdownProgressBar.value = Mathf.Clamp01(remaining / total);
            }
        }

        private void HandleVictoryAchieved()
        {
            if (countdownBanner != null) countdownBanner.SetActive(false);
            if (terminalPanel != null) terminalPanel.SetActive(false);
        }
    }
}
