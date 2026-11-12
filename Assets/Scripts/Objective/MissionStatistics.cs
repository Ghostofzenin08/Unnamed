using System;
using UnityEngine;
using Outpost404.Environment;

namespace Outpost404.Objective
{
    /// <summary>
    /// Tracks mission statistics across the survival expedition:
    /// Days Survived, Enemies Slain, Resources Harvested, and Structures Built.
    /// Provides data for Victory and Defeat end-screens and persistence serialization.
    /// </summary>
    [DisallowMultipleComponent]
    public class MissionStatistics : MonoBehaviour
    {
        public static MissionStatistics Instance { get; set; }

        [Header("Mission Metrics")]
        [SerializeField] private int enemiesSlain = 0;
        [SerializeField] private int resourcesHarvested = 0;
        [SerializeField] private int structuresBuilt = 0;
        [SerializeField] private int cachedDaysSurvived = 1;

        public event Action OnStatisticsChanged;

        public int EnemiesSlain => enemiesSlain;
        public int ResourcesHarvested => resourcesHarvested;
        public int StructuresBuilt => structuresBuilt;

        public int DaysSurvived
        {
            get
            {
                if (DayNightCycleManager.Instance != null)
                {
                    return DayNightCycleManager.Instance.CurrentDay;
                }
                return Mathf.Max(1, cachedDaysSurvived);
            }
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
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RecordEnemySlain()
        {
            enemiesSlain++;
            OnStatisticsChanged?.Invoke();
        }

        public void RecordResourceHarvested()
        {
            resourcesHarvested++;
            OnStatisticsChanged?.Invoke();
        }

        public void RecordStructureBuilt()
        {
            structuresBuilt++;
            OnStatisticsChanged?.Invoke();
        }

        public void SetStats(int days, int enemies, int resources, int structures)
        {
            cachedDaysSurvived = Mathf.Max(1, days);
            enemiesSlain = Mathf.Max(0, enemies);
            resourcesHarvested = Mathf.Max(0, resources);
            structuresBuilt = Mathf.Max(0, structures);
            OnStatisticsChanged?.Invoke();
        }

        public void ResetStats()
        {
            cachedDaysSurvived = 1;
            enemiesSlain = 0;
            resourcesHarvested = 0;
            structuresBuilt = 0;
            OnStatisticsChanged?.Invoke();
        }
    }
}
