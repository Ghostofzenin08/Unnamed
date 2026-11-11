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

        public PlayerStats TargetStats { get => targetStats; set => SetTargetStats(value); }

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
            targetStats.OnPlayerDied += HandlePlayerDied;
        }

        private void UnsubscribeFromStats()
        {
            if (targetStats == null) return;
            targetStats.OnHealthChanged -= HandleHealthChanged;
            targetStats.OnStaminaChanged -= HandleStaminaChanged;
            targetStats.OnHungerChanged -= HandleHungerChanged;
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
    }
}
