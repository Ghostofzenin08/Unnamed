using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Outpost404.Survival;

namespace Outpost404.Environment
{
    /// <summary>
    /// Manages the Universal Render Pipeline (URP) Global Volume and post-processing profile.
    /// Provides stylized tonemapping, bloom radiance, color adjustments, and dynamically
    /// modulates the Vignette intensity during nighttime anomaly surges or critical freezing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    public class AtmospherePostProcessManager : MonoBehaviour
    {
        public static AtmospherePostProcessManager Instance { get; set; }

        [Header("Volume Reference")]
        [SerializeField] private Volume volume;

        [Header("Dynamic Vignette Configuration")]
        [Tooltip("Subtle baseline vignette intensity for standard daylight conditions.")]
        [Range(0f, 1f)]
        [SerializeField] private float baselineVignette = 0.25f;

        [Tooltip("Additional vignette intensity applied during night anomaly phases.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float nightVignetteBonus = 0.12f;

        [Tooltip("Additional vignette intensity applied during critical freezing hypothermia.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float criticalFreezingVignetteBonus = 0.30f;

        [Tooltip("Transition speed when interpolating vignette depth.")]
        [SerializeField] private float vignetteTransitionSpeed = 3.0f;

        // Cached Volume Overrides
        private Tonemapping tonemapping;
        private Bloom bloom;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;

        // External System References
        private PlayerStats cachedPlayerStats;
        private DayNightCycleManager cachedDayNightCycle;
        private float currentVignetteTarget = 0.25f;

        public Volume Volume => volume != null ? volume : (volume = GetComponent<Volume>());
        public VolumeProfile Profile => Volume != null ? (Volume.sharedProfile != null ? Volume.sharedProfile : (Application.isPlaying ? Volume.profile : null)) : null;

        public Tonemapping TonemappingComponent => tonemapping;
        public Bloom BloomComponent => bloom;
        public Vignette VignetteComponent => vignette;
        public ColorAdjustments ColorAdjustmentsComponent => colorAdjustments;

        public float BaselineVignette { get => baselineVignette; set => baselineVignette = Mathf.Clamp01(value); }
        public float NightVignetteBonus { get => nightVignetteBonus; set => nightVignetteBonus = Mathf.Clamp01(value); }
        public float CriticalFreezingVignetteBonus { get => criticalFreezingVignetteBonus; set => criticalFreezingVignetteBonus = Mathf.Clamp01(value); }
        public float CurrentVignetteTarget => currentVignetteTarget;
        public float CurrentVignetteIntensity => (vignette != null) ? vignette.intensity.value : baselineVignette;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (volume == null)
            {
                volume = GetComponent<Volume>();
            }

            ResolveProfileComponents();
        }

        private void Start()
        {
            ResolveExternalReferences();
            EvaluateVignetteTarget(1f); // Snap immediately on start
        }

        private void Update()
        {
            EvaluateVignetteTarget(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Locates and caches the 4 required post-processing overrides from the Volume profile.
        /// </summary>
        public void ResolveProfileComponents()
        {
            var prof = Profile;
            if (prof == null) return;

            prof.TryGet(out tonemapping);
            prof.TryGet(out bloom);
            prof.TryGet(out vignette);
            prof.TryGet(out colorAdjustments);
        }

        public void ResolveExternalReferences()
        {
            if (cachedPlayerStats == null)
            {
                cachedPlayerStats = Object.FindAnyObjectByType<PlayerStats>();
            }
            if (cachedDayNightCycle == null)
            {
                cachedDayNightCycle = DayNightCycleManager.Instance ?? Object.FindAnyObjectByType<DayNightCycleManager>();
            }
        }

        /// <summary>
        /// Calculates target vignette intensity based on time of day and player freezing state,
        /// interpolating smoothly towards the target.
        /// </summary>
        public float EvaluateVignetteTarget(float deltaTime = -1f)
        {
            ResolveExternalReferences();
            if (vignette == null) ResolveProfileComponents();

            float target = baselineVignette;

            // Nighttime anomaly darkening
            if (cachedDayNightCycle != null && cachedDayNightCycle.CurrentPhase == DayPhase.Night)
            {
                target += nightVignetteBonus;
            }

            // Severe freezing tunnel vision
            if (cachedPlayerStats != null)
            {
                if (cachedPlayerStats.IsCriticalFreezing)
                {
                    target += criticalFreezingVignetteBonus;
                }
                else if (cachedPlayerStats.IsInHypothermia)
                {
                    target += criticalFreezingVignetteBonus * 0.45f;
                }
            }

            currentVignetteTarget = Mathf.Clamp(target, 0f, 0.85f);

            if (vignette != null)
            {
                if (deltaTime < 0f || deltaTime >= 1f)
                {
                    vignette.intensity.value = currentVignetteTarget;
                }
                else
                {
                    vignette.intensity.value = Mathf.MoveTowards(
                        vignette.intensity.value,
                        currentVignetteTarget,
                        deltaTime * vignetteTransitionSpeed
                    );
                }
            }

            return currentVignetteTarget;
        }

        /// <summary>
        /// Applies standard stylized visual settings to the profile components.
        /// </summary>
        public void ApplyDefaultProfileSettings()
        {
            ResolveProfileComponents();

            if (tonemapping != null)
            {
                tonemapping.mode.overrideState = true;
                tonemapping.mode.value = TonemappingMode.ACES;
            }

            if (bloom != null)
            {
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 1.2f;
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.9f;
            }

            if (vignette != null)
            {
                vignette.intensity.overrideState = true;
                vignette.intensity.value = baselineVignette;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.4f;
                vignette.color.overrideState = true;
                vignette.color.value = Color.black;
            }

            if (colorAdjustments != null)
            {
                colorAdjustments.contrast.overrideState = true;
                colorAdjustments.contrast.value = 10f;
                colorAdjustments.saturation.overrideState = true;
                colorAdjustments.saturation.value = 12f;
            }
        }
    }
}
