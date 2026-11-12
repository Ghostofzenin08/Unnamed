using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Outpost404.Objective;
using Outpost404.Survival;

namespace Outpost404.UI
{
    /// <summary>
    /// Displays mission completion end-screens:
    /// - Victory Screen: extraction countdown survived, shows full mission statistics and restart button.
    /// - Defeat Screen: player killed by starvation, predators, or hypothermia, shows stats and restart button.
    /// </summary>
    [DisallowMultipleComponent]
    public class MissionEndUI : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private BeaconObjective targetBeacon;
        [SerializeField] private PlayerStats targetPlayerStats;
        [SerializeField] private MissionStatistics targetStatistics;

        [Header("Victory Screen Elements")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text victoryTitleText;
        [SerializeField] private Text victorySubtitleText;
        [SerializeField] private Text victoryStatsText;
        [SerializeField] private Button victoryRestartButton;

        [Header("Defeat Screen Elements")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private Text defeatTitleText;
        [SerializeField] private Text defeatSubtitleText;
        [SerializeField] private Text defeatStatsText;
        [SerializeField] private Button defeatRestartButton;

        public GameObject VictoryPanel => victoryPanel;
        public GameObject DefeatPanel => defeatPanel;
        public Text VictoryStatsText => victoryStatsText;
        public Text DefeatStatsText => defeatStatsText;
        public Button VictoryRestartButton => victoryRestartButton;
        public Button DefeatRestartButton => defeatRestartButton;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private void Start()
        {
            ResolveReferences();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        public void ResolveReferences()
        {
            if (targetBeacon == null) targetBeacon = BeaconObjective.Instance ?? FindFirstObjectByType<BeaconObjective>();
            if (targetStatistics == null) targetStatistics = MissionStatistics.Instance ?? FindFirstObjectByType<MissionStatistics>();
            if (targetPlayerStats == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) targetPlayerStats = player.GetComponent<PlayerStats>();
            }
        }

        private void WireButtons()
        {
            if (victoryRestartButton != null)
            {
                victoryRestartButton.onClick.RemoveAllListeners();
                victoryRestartButton.onClick.AddListener(RestartMission);
            }

            if (defeatRestartButton != null)
            {
                defeatRestartButton.onClick.RemoveAllListeners();
                defeatRestartButton.onClick.AddListener(RestartMission);
            }
        }

        private void SubscribeToEvents()
        {
            if (targetBeacon != null) targetBeacon.OnVictoryAchieved += ShowVictory;
            if (targetPlayerStats != null) targetPlayerStats.OnPlayerDied += ShowDefeat;
        }

        private void UnsubscribeFromEvents()
        {
            if (targetBeacon != null) targetBeacon.OnVictoryAchieved -= ShowVictory;
            if (targetPlayerStats != null) targetPlayerStats.OnPlayerDied -= ShowDefeat;
        }

        public void ShowVictory()
        {
            ResolveReferences();
            if (defeatPanel != null) defeatPanel.SetActive(false);

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (victoryTitleText != null)
            {
                victoryTitleText.text = "MISSION ACCOMPLISHED\nEXTRACTION SUCCESSFUL";
            }

            if (victorySubtitleText != null)
            {
                victorySubtitleText.text = "Orbital dropship successfully extracted Dr. Nolan Cross from Outpost 404.";
            }

            if (victoryStatsText != null)
            {
                victoryStatsText.text = FormatStatisticsSummary();
            }
        }

        public void ShowDefeat()
        {
            // Do not show defeat if victory already won
            if (targetBeacon != null && targetBeacon.IsVictory) return;
            if (victoryPanel != null && victoryPanel.activeSelf) return;

            ResolveReferences();

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }

            if (defeatTitleText != null)
            {
                defeatTitleText.text = "SIGNAL LOST\nVITALS CRITICAL";
            }

            if (defeatSubtitleText != null)
            {
                defeatSubtitleText.text = "Expedition life support terminated in Outpost 404.";
            }

            if (defeatStatsText != null)
            {
                defeatStatsText.text = FormatStatisticsSummary();
            }
        }

        public string FormatStatisticsSummary()
        {
            int days = targetStatistics != null ? targetStatistics.DaysSurvived : 1;
            int enemies = targetStatistics != null ? targetStatistics.EnemiesSlain : 0;
            int resources = targetStatistics != null ? targetStatistics.ResourcesHarvested : 0;
            int structures = targetStatistics != null ? targetStatistics.StructuresBuilt : 0;

            return $"EXPEDITION REPORT:\n\n" +
                   $"- Days Survived: {days}\n" +
                   $"- Enemies Slain: {enemies}\n" +
                   $"- Resources Harvested: {resources}\n" +
                   $"- Structures Built: {structures}";
        }

        public void RestartMission()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                if (!string.IsNullOrEmpty(activeScene.name))
                {
                    SceneManager.LoadScene(activeScene.name);
                }
                else if (activeScene.buildIndex >= 0)
                {
                    SceneManager.LoadScene(activeScene.buildIndex);
                }
            }
        }
    }
}
