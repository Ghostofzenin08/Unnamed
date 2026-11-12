using UnityEngine;
using UnityEngine.UI;
using Outpost404.Survival;

namespace Outpost404.UI
{
    /// <summary>
    /// Displays survival gauges (Health, Stamina, Hunger) and alerts.
    /// Fully decoupled from stats logic by subscribing to C# events on PlayerStats.
    /// </summary>
    public class SurvivalHUD : MonoBehaviour
    {
        [Header("Target Player Reference")]
        [Tooltip("PlayerStats instance to observe. Auto-locates Player if left null.")]
        [SerializeField] private PlayerStats targetStats;

        [Header("Health Gauge UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Color healthNormalColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color healthCriticalColor = new Color(0.9f, 0.15f, 0.15f);

        [Header("Stamina Gauge UI")]
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image staminaFillImage;
        [SerializeField] private Color staminaNormalColor = new Color(0.15f, 0.65f, 1f);
        [SerializeField] private Color staminaExhaustedColor = new Color(0.4f, 0.4f, 0.6f);

        [Header("Hunger Gauge UI")]
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private Image hungerFillImage;
        [SerializeField] private Color hungerNormalColor = new Color(0.95f, 0.65f, 0.1f);
        [SerializeField] private Color hungerStarvingColor = new Color(0.85f, 0.2f, 0.2f);

        [Header("Alert & Death Banners")]
        [SerializeField] private GameObject starvationWarningBanner;
        [SerializeField] private GameObject deathBanner;

        [Header("Weather, Clock & Environmental Thermometer")]
        [SerializeField] private Text clockText;
        [SerializeField] private Image phaseIconImage;
        [SerializeField] private Text ambientTempText;
        [SerializeField] private Text bodyTempText;
        [SerializeField] private Slider bodyTempSlider;
        [SerializeField] private Image bodyTempFillImage;
        [SerializeField] private GameObject frostWarningBanner;

        public PlayerStats TargetStats { get => targetStats; set => SetTargetStats(value); }
        public Text ClockText => clockText;
        public Image PhaseIconImage => phaseIconImage;
        public Text AmbientTempText => ambientTempText;
        public Text BodyTempText => bodyTempText;
        public Slider BodyTempSlider => bodyTempSlider;
        public GameObject FrostWarningBanner => frostWarningBanner;

        private void Start()
        {
            ResolveFillImages();

            if (targetStats == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    targetStats = player.GetComponent<PlayerStats>();
                }
            }

            if (targetStats != null)
            {
                SubscribeToStats();
                targetStats.NotifyAllStats();
            }

            if (starvationWarningBanner != null) starvationWarningBanner.SetActive(false);
            if (deathBanner != null) deathBanner.SetActive(false);
            if (frostWarningBanner != null) frostWarningBanner.SetActive(false);
        }

        private void Update()
        {
            UpdateClockAndWeatherDisplay();
        }

        private void ResolveFillImages()
        {
            if (healthFillImage == null && healthSlider != null && healthSlider.fillRect != null)
            {
                healthFillImage = healthSlider.fillRect.GetComponent<Image>();
            }
            if (staminaFillImage == null && staminaSlider != null && staminaSlider.fillRect != null)
            {
                staminaFillImage = staminaSlider.fillRect.GetComponent<Image>();
            }
            if (hungerFillImage == null && hungerSlider != null && hungerSlider.fillRect != null)
            {
                hungerFillImage = hungerSlider.fillRect.GetComponent<Image>();
            }
            if (bodyTempFillImage == null && bodyTempSlider != null && bodyTempSlider.fillRect != null)
            {
                bodyTempFillImage = bodyTempSlider.fillRect.GetComponent<Image>();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromStats();
        }

        public void SetTargetStats(PlayerStats stats)
        {
            UnsubscribeFromStats();
            targetStats = stats;
            if (targetStats != null)
            {
                SubscribeToStats();
                targetStats.NotifyAllStats();
            }
        }

        private void SubscribeToStats()
        {
            if (targetStats == null) return;
            targetStats.OnHealthChanged += HandleHealthChanged;
            targetStats.OnStaminaChanged += HandleStaminaChanged;
            targetStats.OnHungerChanged += HandleHungerChanged;
            targetStats.OnBodyTemperatureChanged += HandleBodyTemperatureChanged;
            targetStats.OnHypothermiaStateChanged += HandleHypothermiaChanged;
            targetStats.OnPlayerDied += HandlePlayerDied;
        }

        private void UnsubscribeFromStats()
        {
            if (targetStats == null) return;
            targetStats.OnHealthChanged -= HandleHealthChanged;
            targetStats.OnStaminaChanged -= HandleStaminaChanged;
            targetStats.OnHungerChanged -= HandleHungerChanged;
            targetStats.OnBodyTemperatureChanged -= HandleBodyTemperatureChanged;
            targetStats.OnHypothermiaStateChanged -= HandleHypothermiaChanged;
            targetStats.OnPlayerDied -= HandlePlayerDied;
        }

        private void HandleHealthChanged(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (healthSlider != null)
            {
                healthSlider.value = ratio;
            }
            if (healthFillImage != null)
            {
                healthFillImage.color = ratio < 0.25f ? healthCriticalColor : healthNormalColor;
            }
        }

        private void HandleStaminaChanged(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (staminaSlider != null)
            {
                staminaSlider.value = ratio;
            }
            if (staminaFillImage != null)
            {
                staminaFillImage.color = ratio < 0.1f ? staminaExhaustedColor : staminaNormalColor;
            }
        }

        private void HandleHungerChanged(float current, float max)
        {
            float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (hungerSlider != null)
            {
                hungerSlider.value = ratio;
            }
            if (hungerFillImage != null)
            {
                hungerFillImage.color = ratio < 0.2f ? hungerStarvingColor : hungerNormalColor;
            }

            if (starvationWarningBanner != null)
            {
                starvationWarningBanner.SetActive(ratio <= 0.001f);
            }
        }

        private void HandlePlayerDied()
        {
            if (deathBanner != null)
            {
                deathBanner.SetActive(true);
            }
        }

        private void HandleBodyTemperatureChanged(float temp)
        {
            if (bodyTempText != null)
            {
                bodyTempText.text = $"Body: {temp:F1}°C";
                if (temp < (targetStats != null ? targetStats.CriticalFreezingThreshold : 25f))
                {
                    bodyTempText.color = new Color(0.95f, 0.25f, 0.25f);
                }
                else if (temp < (targetStats != null ? targetStats.HypothermiaThreshold : 32f))
                {
                    bodyTempText.color = new Color(0.35f, 0.85f, 1f);
                }
                else
                {
                    bodyTempText.color = new Color(0.3f, 0.95f, 0.45f);
                }
            }

            if (bodyTempSlider != null)
            {
                // Visual gauge normalized between 20°C and 40°C
                float fraction = Mathf.Clamp01((temp - 20f) / 20f);
                bodyTempSlider.value = fraction;
                if (bodyTempFillImage != null)
                {
                    bodyTempFillImage.color = temp < 32f ? new Color(0.2f, 0.75f, 1f) : new Color(1f, 0.65f, 0.2f);
                }
            }
        }

        private void HandleHypothermiaChanged(bool isInHypothermia)
        {
            if (frostWarningBanner != null)
            {
                frostWarningBanner.SetActive(isInHypothermia);
            }
        }

        private void UpdateClockAndWeatherDisplay()
        {
            if (Outpost404.Environment.DayNightCycleManager.Instance != null)
            {
                var mgr = Outpost404.Environment.DayNightCycleManager.Instance;
                if (clockText != null)
                {
                    clockText.text = $"{mgr.GetFormattedClockTime()} ({mgr.CurrentPhase})";
                }

                if (ambientTempText != null)
                {
                    float amb = mgr.CurrentAmbientTemperature;
                    ambientTempText.text = $"Amb: {amb:F0}°C";
                    ambientTempText.color = amb < 0f ? new Color(0.35f, 0.85f, 1f) : (amb > 20f ? new Color(1f, 0.75f, 0.3f) : Color.white);
                }

                if (phaseIconImage != null)
                {
                    switch (mgr.CurrentPhase)
                    {
                        case Outpost404.Environment.DayPhase.Dawn:
                            phaseIconImage.color = new Color(1f, 0.75f, 0.4f);
                            break;
                        case Outpost404.Environment.DayPhase.Day:
                            phaseIconImage.color = new Color(1f, 0.95f, 0.4f);
                            break;
                        case Outpost404.Environment.DayPhase.Dusk:
                            phaseIconImage.color = new Color(1f, 0.5f, 0.25f);
                            break;
                        case Outpost404.Environment.DayPhase.Night:
                            phaseIconImage.color = new Color(0.35f, 0.55f, 0.95f);
                            break;
                    }
                }
            }
        }

        // Method for manual binding by setup tools
        public void ConfigureComponents(Slider health, Slider stamina, Slider hunger, GameObject starveBanner = null, GameObject deadBanner = null)
        {
            healthSlider = health;
            staminaSlider = stamina;
            hungerSlider = hunger;
            starvationWarningBanner = starveBanner;
            deathBanner = deadBanner;
            ResolveFillImages();
        }

        public void ConfigureEnvironmentalUI(Text clock, Image phaseIcon, Text ambientText, Text bodyText, Slider tempSlider, GameObject frostBanner)
        {
            clockText = clock;
            phaseIconImage = phaseIcon;
            ambientTempText = ambientText;
            bodyTempText = bodyText;
            bodyTempSlider = tempSlider;
            frostWarningBanner = frostBanner;
            ResolveFillImages();
        }
    }
}
