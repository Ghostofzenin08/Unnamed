using System;
using UnityEngine;

namespace Outpost404.Environment
{
    public enum DayPhase
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    /// <summary>
    /// Manages the planetary day/night cycle, directional celestial sun lighting,
    /// dynamic ambient atmosphere, and diurnal ambient temperature fluctuations.
    /// Exposes decoupled C# events for time progression and phase shifts.
    /// </summary>
    [DisallowMultipleComponent]
    public class DayNightCycleManager : MonoBehaviour
    {
        public static DayNightCycleManager Instance { get; set; }

        [Header("Cycle Configuration")]
        [Tooltip("Full duration of a complete 24-hour planetary day in real seconds.")]
        [SerializeField] private float dayLengthInSeconds = 120f;

        [Tooltip("Current normalized time in [0.0, 1.0) where 0.0=Dawn (06:00), 0.25=Noon (12:00), 0.5=Dusk (18:00), 0.75=Midnight (00:00).")]
        [Range(0f, 1f)]
        [SerializeField] private float normalizedTime = 0.0f;

        [Tooltip("Current survival day index (starts at Day 1).")]
        [SerializeField] private int currentDay = 1;

        [SerializeField] private DayPhase currentPhase = DayPhase.Dawn;

        [Header("Sun & Celestial Lighting")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Gradient sunColorGradient;
        [SerializeField] private AnimationCurve sunIntensityCurve;
        [SerializeField] private Gradient ambientColorGradient;

        [Header("Ambient Temperature Curve (°C)")]
        [Tooltip("Maps normalized time [0, 1] to ambient temperature in Celsius.")]
        [SerializeField] private AnimationCurve temperatureCurve;

        // Decoupled Observation Events
        public event Action<float, int> OnTimeChanged; // (normalizedTime, currentDay)
        public event Action<DayPhase> OnPhaseChanged;   // (newPhase)

        // Public Properties
        public float DayLengthInSeconds
        {
            get => dayLengthInSeconds;
            set => dayLengthInSeconds = Mathf.Max(1f, value);
        }

        public float NormalizedTime => normalizedTime;
        public int CurrentDay => currentDay;
        public DayPhase CurrentPhase => currentPhase;
        public Light SunLight { get => sunLight; set => sunLight = value; }
        public float CurrentAmbientTemperature => CalculateAmbientTemperature(normalizedTime);

        private void Awake()
        {
            if (Application.isPlaying && Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureGradientsAndCurves();
            currentPhase = GetPhaseFromTime(normalizedTime);
            UpdateLightingAndEnvironment();
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (sunLight == null)
            {
                var dirObj = GameObject.Find("Directional Light");
                if (dirObj != null) sunLight = dirObj.GetComponent<Light>();
                if (sunLight == null)
                {
                    var allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                    for (int i = 0; i < allLights.Length; i++)
                    {
                        if (allLights[i].type == LightType.Directional && (allLights[i].transform.parent == null || allLights[i].name.Contains("Directional") || allLights[i].name.Contains("Sun")))
                        {
                            sunLight = allLights[i];
                            break;
                        }
                    }
                }
            }
            UpdateLightingAndEnvironment();
            OnTimeChanged?.Invoke(normalizedTime, currentDay);
            OnPhaseChanged?.Invoke(currentPhase);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Deterministic time progression step callable during runtime or from automated unit tests.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

            float length = Mathf.Max(0.1f, dayLengthInSeconds);
            normalizedTime += deltaTime / length;

            if (normalizedTime >= 1.0f)
            {
                int elapsedDays = Mathf.FloorToInt(normalizedTime);
                currentDay += elapsedDays;
                normalizedTime -= elapsedDays;
            }

            CheckPhaseTransition();
            UpdateLightingAndEnvironment();
            OnTimeChanged?.Invoke(normalizedTime, currentDay);
        }

        /// <summary>
        /// Explicitly jump to a designated normalized time and optional day index.
        /// </summary>
        public void SetTime(float time, int day = -1)
        {
            if (float.IsNaN(time) || float.IsInfinity(time)) time = 0f;

            while (time < 0f) time += 1f;
            while (time >= 1f) time -= 1f;

            normalizedTime = time;
            if (day > 0) currentDay = day;

            CheckPhaseTransition();
            UpdateLightingAndEnvironment();
            OnTimeChanged?.Invoke(normalizedTime, currentDay);
        }

        public void SetTimeAndDay(float time, int day)
        {
            SetTime(time, day);
        }

        public static DayPhase GetPhaseFromTime(float time)
        {
            while (time < 0f) time += 1f;
            while (time >= 1f) time -= 1f;

            // 0.00 = Sunrise/Dawn, 0.25 = Noon, 0.50 = Sunset/Dusk, 0.75 = Midnight
            if (time < 0.15f) return DayPhase.Dawn;
            if (time < 0.50f) return DayPhase.Day;
            if (time < 0.65f) return DayPhase.Dusk;
            return DayPhase.Night;
        }

        private void CheckPhaseTransition()
        {
            DayPhase newPhase = GetPhaseFromTime(normalizedTime);
            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(currentPhase);
            }
        }

        /// <summary>
        /// Evaluates ambient temperature curve across diurnal cycle.
        /// Warm Day (+22°C to +26°C), Dusk (+10°C), Sub-zero Night (-15°C to -20°C).
        /// </summary>
        public float CalculateAmbientTemperature(float time)
        {
            if (float.IsNaN(time) || float.IsInfinity(time)) time = 0f;
            while (time < 0f) time += 1f;
            while (time >= 1f) time -= 1f;

            if (temperatureCurve != null && temperatureCurve.length > 0)
            {
                return temperatureCurve.Evaluate(time);
            }

            // Piecewise trigonometric fallback if curve unassigned
            // At 0.25 (Noon) peak +24°C, at 0.75 (Midnight) trough -18°C
            float rad = (time - 0.25f) * Mathf.PI * 2f;
            float cos = Mathf.Cos(rad); // 1 at noon, -1 at midnight, 0 at dawn/dusk
            if (cos >= 0f)
            {
                // Day/Dusk side: 10°C at 0 up to 24°C at 1
                return Mathf.Lerp(10f, 24f, cos);
            }
            else
            {
                // Night side: 10°C at 0 down to -18°C at -1
                return Mathf.Lerp(10f, -18f, -cos);
            }
        }

        public void UpdateLightingAndEnvironment()
        {
            EnsureGradientsAndCurves();

            if (sunLight != null)
            {
                // Rotate directional light 360 degrees around X axis:
                // 0.00 (Dawn) = 0°, 0.25 (Noon) = 90° (overhead), 0.50 (Sunset) = 180°, 0.75 (Midnight) = 270°
                float sunAngle = normalizedTime * 360f;
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, -35f, 0f);

                sunLight.color = sunColorGradient.Evaluate(normalizedTime);
                sunLight.intensity = sunIntensityCurve.Evaluate(normalizedTime);
            }

            Color currentAmbient = ambientColorGradient.Evaluate(normalizedTime);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = currentAmbient;
            RenderSettings.ambientSkyColor = currentAmbient;
            RenderSettings.ambientEquatorColor = currentAmbient * 0.8f;
            RenderSettings.ambientGroundColor = currentAmbient * 0.4f;

            if (RenderSettings.skybox != null)
            {
                if (RenderSettings.skybox.HasProperty("_Tint"))
                {
                    RenderSettings.skybox.SetColor("_Tint", currentAmbient);
                }
                if (RenderSettings.skybox.HasProperty("_SkyTint"))
                {
                    RenderSettings.skybox.SetColor("_SkyTint", currentAmbient);
                }
                if (RenderSettings.skybox.HasProperty("_GroundColor"))
                {
                    RenderSettings.skybox.SetColor("_GroundColor", currentAmbient * 0.5f);
                }
            }
        }

        /// <summary>
        /// Formats current time into a military 24h clock string (e.g. "Day 1 | 14:00").
        /// </summary>
        public string GetFormattedClockTime()
        {
            GetClockHoursAndMinutes(out int hours, out int minutes);
            return $"Day {currentDay} | {hours:D2}:{minutes:D2}";
        }

        public void GetClockHoursAndMinutes(out int hours, out int minutes)
        {
            // 0.00 normalized time corresponds to 06:00 (Dawn)
            float totalHours = (normalizedTime * 24f + 6f) % 24f;
            hours = Mathf.FloorToInt(totalHours) % 24;
            minutes = Mathf.Clamp(Mathf.FloorToInt((totalHours - Mathf.Floor(totalHours)) * 60f), 0, 59);
        }

        public void EnsureGradientsAndCurves()
        {
            if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
            {
                sunColorGradient = new Gradient();
                sunColorGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(1.0f, 0.72f, 0.45f), 0.0f),  // Warm golden morning
                        new GradientColorKey(new Color(1.0f, 0.98f, 0.92f), 0.25f), // Bright noon
                        new GradientColorKey(new Color(1.0f, 0.52f, 0.22f), 0.50f), // Amber dusk
                        new GradientColorKey(new Color(0.35f, 0.52f, 0.85f), 0.75f),// Dim cool moonlight
                        new GradientColorKey(new Color(1.0f, 0.72f, 0.45f), 1.0f)   // Dawn loop
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(1.0f, 1.0f)
                    }
                );
            }

            if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
            {
                sunIntensityCurve = new AnimationCurve();
                sunIntensityCurve.AddKey(new Keyframe(0.0f, 0.65f));  // Dawn
                sunIntensityCurve.AddKey(new Keyframe(0.25f, 1.30f)); // Bright Noon
                sunIntensityCurve.AddKey(new Keyframe(0.50f, 0.55f)); // Dusk
                sunIntensityCurve.AddKey(new Keyframe(0.75f, 0.18f)); // Moonlight
                sunIntensityCurve.AddKey(new Keyframe(1.0f, 0.65f));  // Dawn
            }

            if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            {
                ambientColorGradient = new Gradient();
                ambientColorGradient.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.35f, 0.32f, 0.30f), 0.0f),  // Dawn
                        new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 0.25f), // Noon
                        new GradientColorKey(new Color(0.32f, 0.25f, 0.28f), 0.50f), // Dusk
                        new GradientColorKey(new Color(0.08f, 0.10f, 0.18f), 0.75f), // Dark blue night
                        new GradientColorKey(new Color(0.35f, 0.32f, 0.30f), 1.0f)   // Dawn
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(1.0f, 1.0f)
                    }
                );
            }

            if (temperatureCurve == null || temperatureCurve.length == 0)
            {
                temperatureCurve = new AnimationCurve();
                temperatureCurve.AddKey(new Keyframe(0.0f, 8.0f));    // Dawn +8°C
                temperatureCurve.AddKey(new Keyframe(0.25f, 24.0f));  // Warm Day +24°C
                temperatureCurve.AddKey(new Keyframe(0.50f, 10.0f));  // Dusk +10°C
                temperatureCurve.AddKey(new Keyframe(0.75f, -18.0f)); // Sub-zero Night -18°C
                temperatureCurve.AddKey(new Keyframe(1.0f, 8.0f));    // Dawn +8°C
            }
        }
    }
}
