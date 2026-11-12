using System;
using UnityEngine;
using Outpost404.Combat;
using Outpost404.Player;

namespace Outpost404.Survival
{
    /// <summary>
    /// Manages the player's core survival attributes: Health, Stamina, and Hunger.
    /// Handles continuous time depletion, sprint consumption, natural regeneration,
    /// and starvation damage according to survival game balance.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float healthRegenRate = 2f;
        [SerializeField] private float healthRegenHungerThreshold = 75f;
        [SerializeField] private float starvationDamageRate = 4f;

        [Header("Stamina Settings")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina = 100f;
        [SerializeField] private float sprintStaminaCost = 25f;
        [SerializeField] private float staminaRegenRate = 18f;
        [SerializeField] private float staminaRegenDelay = 1.0f;

        [Header("Hunger Settings")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger = 100f;
        [SerializeField] private float baseHungerDrainRate = 0.8f;
        [SerializeField] private float sprintHungerMultiplier = 2.0f;

        [Header("Environmental Temperature & Hypothermia")]
        [SerializeField] private float normalBodyTemperature = 37f;
        [SerializeField] private float hypothermiaThreshold = 32f;
        [SerializeField] private float criticalFreezingThreshold = 25f;
        [SerializeField] private float minBodyTemperature = 20f;
        [SerializeField] private float currentBodyTemperature = 37f;
        [SerializeField] private float hypothermiaDamageRate = 3.0f;
        [SerializeField] private float criticalFreezingDamageRate = 7.0f;
        [SerializeField] private float hypothermiaSpeedPenalty = 0.30f;
        [SerializeField] private float criticalFreezingSpeedPenalty = 0.55f;
        [SerializeField] private float bodyCoolingRate = 0.45f;
        [SerializeField] private float bodyWarmingRate = 3.5f;

        // Runtime state timers
        private float staminaRegenTimer = 0f;
        private float shiverAudioTimer = 0f;
        private bool isDead = false;
        private bool wasHypothermic = false;
        private float currentEffectiveAmbient = 22f;
        private bool isNearHeatSource = false;
        private PlayerController playerController;

        // Decoupled observation events (current, max)
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<float, float> OnHungerChanged;
        public event Action<float> OnBodyTemperatureChanged;
        public event Action<bool> OnHypothermiaStateChanged;
        public event Action OnPlayerDied;

        // IDamageable implementation
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        // Survival properties
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float CurrentHunger => currentHunger;
        public float MaxHunger => maxHunger;
        public bool CanSprint => !isDead && currentStamina > 2f;

        // Temperature & Hypothermia Properties
        public float CurrentBodyTemperature => currentBodyTemperature;
        public float NormalBodyTemperature => normalBodyTemperature;
        public float HypothermiaThreshold => hypothermiaThreshold;
        public float CriticalFreezingThreshold => criticalFreezingThreshold;
        public float MinBodyTemperature => minBodyTemperature;
        public bool IsInHypothermia => currentBodyTemperature < hypothermiaThreshold;
        public bool IsCriticalFreezing => currentBodyTemperature < criticalFreezingThreshold;
        public float CurrentEffectiveAmbientTemperature => currentEffectiveAmbient;
        public bool IsNearHeatSource => isNearHeatSource;

        public float MovementSpeedModifier
        {
            get
            {
                if (currentBodyTemperature < criticalFreezingThreshold)
                {
                    return Mathf.Clamp01(1f - criticalFreezingSpeedPenalty);
                }
                if (currentBodyTemperature < hypothermiaThreshold)
                {
                    return Mathf.Clamp01(1f - hypothermiaSpeedPenalty);
                }
                return 1.0f;
            }
        }

        public float MaxHealthProperty { get => maxHealth; set => maxHealth = Mathf.Max(1f, value); }
        public float MaxStaminaProperty { get => maxStamina; set => maxStamina = Mathf.Max(1f, value); }
        public float MaxHungerProperty { get => maxHunger; set => maxHunger = Mathf.Max(1f, value); }
        public float SprintStaminaCost { get => sprintStaminaCost; set => sprintStaminaCost = value; }
        public float HypothermiaDamageRate { get => hypothermiaDamageRate; set => hypothermiaDamageRate = Mathf.Max(0f, value); }
        public float CriticalFreezingDamageRate { get => criticalFreezingDamageRate; set => criticalFreezingDamageRate = Mathf.Max(0f, value); }
        public float BodyCoolingRate { get => bodyCoolingRate; set => bodyCoolingRate = Mathf.Max(0f, value); }
        public float BodyWarmingRate { get => bodyWarmingRate; set => bodyWarmingRate = Mathf.Max(0f, value); }

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            InitializeStats();
        }

        public void InitializeStats()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentBodyTemperature = normalBodyTemperature;
            isDead = false;
            wasHypothermic = false;
            staminaRegenTimer = 0f;

            NotifyAllStats();
        }

        private void Update()
        {
            if (isDead) return;

            bool isSprinting = playerController != null && playerController.IsSprinting && playerController.IsMoving;
            Tick(Time.deltaTime, isSprinting);
        }

        /// <summary>
        /// Explicit deterministic tick logic for survival decay, stamina drain, and regeneration.
        /// Publicly callable for automated unit tests.
        /// </summary>
        public void Tick(float deltaTime, bool isSprinting)
        {
            if (isDead || deltaTime <= 0f || float.IsNaN(deltaTime)) return;

            // 1. Hunger Depletion
            float hungerDrain = baseHungerDrainRate * (isSprinting ? sprintHungerMultiplier : 1.0f) * deltaTime;
            currentHunger = Mathf.Clamp(currentHunger - hungerDrain, 0f, maxHunger);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);

            // 2. Stamina Management
            if (isSprinting && currentStamina > 0f)
            {
                currentStamina = Mathf.Clamp(currentStamina - (sprintStaminaCost * deltaTime), 0f, maxStamina);
                staminaRegenTimer = staminaRegenDelay;
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            }
            else
            {
                if (staminaRegenTimer > 0f)
                {
                    float consumed = Mathf.Min(staminaRegenTimer, deltaTime);
                    staminaRegenTimer -= consumed;
                    float remainingDelta = deltaTime - consumed;
                    if (remainingDelta > 0f && currentStamina < maxStamina)
                    {
                        currentStamina = Mathf.Clamp(currentStamina + (staminaRegenRate * remainingDelta), 0f, maxStamina);
                        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                    }
                }
                else if (currentStamina < maxStamina)
                {
                    currentStamina = Mathf.Clamp(currentStamina + (staminaRegenRate * deltaTime), 0f, maxStamina);
                    OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                }
            }

            // 3. Health Regeneration or Starvation Damage
            if (currentHunger <= 0f)
            {
                // Starvation takes continuous damage
                TakeDamage(starvationDamageRate * deltaTime);
            }
            else if (currentHunger >= healthRegenHungerThreshold && currentHealth < maxHealth)
            {
                // Well-fed condition regenerates health
                currentHealth = Mathf.Clamp(currentHealth + (healthRegenRate * deltaTime), 0f, maxHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }

            // 4. Environmental Temperature & Hypothermia
            float ambient = 22f;
            if (Outpost404.Environment.DayNightCycleManager.Instance != null)
            {
                ambient = Outpost404.Environment.DayNightCycleManager.Instance.CurrentAmbientTemperature;
            }

            bool nearHeat = Outpost404.Building.BuildableStructure.IsPositionNearHeatBeacon(transform.position, out float heatBonus);
            if (nearHeat)
            {
                ambient += heatBonus;
            }

            currentEffectiveAmbient = ambient;
            isNearHeatSource = nearHeat;

            TickTemperature(deltaTime, ambient, nearHeat);
        }

        /// <summary>
        /// Explicit deterministic environmental temperature evaluation for body cooling,
        /// thermal restoration near heat sources, and hypothermia damage.
        /// Publicly callable for automated unit tests.
        /// </summary>
        public void TickTemperature(float deltaTime, float ambientTemperature, bool isNearHeatSource)
        {
            if (isDead || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            if (float.IsNaN(ambientTemperature) || float.IsInfinity(ambientTemperature)) ambientTemperature = 22f;

            if (isNearHeatSource)
            {
                // Active thermal radiation rapidly restores body temperature to 37°C
                currentBodyTemperature = Mathf.MoveTowards(currentBodyTemperature, normalBodyTemperature, bodyWarmingRate * deltaTime);
            }
            else if (ambientTemperature < 15f)
            {
                // Cold ambient temperature (< 15°C) drains body heat towards freezing
                float coldIntensity = Mathf.Clamp01((15f - ambientTemperature) / 35f);
                float drop = bodyCoolingRate * (0.35f + 0.65f * coldIntensity) * deltaTime;
                currentBodyTemperature = Mathf.Clamp(currentBodyTemperature - drop, minBodyTemperature, normalBodyTemperature);
            }
            else if (ambientTemperature >= 20f && currentBodyTemperature < normalBodyTemperature)
            {
                // Pleasant daytime ambient provides passive thermal recovery
                float recovery = (bodyWarmingRate * 0.2f) * deltaTime;
                currentBodyTemperature = Mathf.MoveTowards(currentBodyTemperature, normalBodyTemperature, recovery);
            }

            // Hypothermia health drain occurs only when not protected by a heat beacon
            if (!isNearHeatSource)
            {
                if (currentBodyTemperature < criticalFreezingThreshold)
                {
                    TakeDamage(criticalFreezingDamageRate * deltaTime);

                    // Shivering / teeth chattering cue when body temperature is critical
                    shiverAudioTimer -= deltaTime;
                    if (shiverAudioTimer <= 0f)
                    {
                        shiverAudioTimer = 3.5f;
                        Outpost404.Audio.AudioManager.Instance?.PlayHypothermiaShiver();
                    }
                }
                else if (currentBodyTemperature < hypothermiaThreshold)
                {
                    TakeDamage(hypothermiaDamageRate * deltaTime);
                }
                else
                {
                    shiverAudioTimer = 0f;
                }
            }
            else
            {
                shiverAudioTimer = 0f;
            }

            OnBodyTemperatureChanged?.Invoke(currentBodyTemperature);

            bool isHypo = IsInHypothermia;
            if (isHypo != wasHypothermic)
            {
                wasHypothermic = isHypo;
                OnHypothermiaStateChanged?.Invoke(isHypo);
            }
        }

        public void RestoreWarmth(float deltaTime)
        {
            if (isDead || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            currentBodyTemperature = Mathf.MoveTowards(currentBodyTemperature, normalBodyTemperature, bodyWarmingRate * deltaTime);
            OnBodyTemperatureChanged?.Invoke(currentBodyTemperature);

            bool isHypo = IsInHypothermia;
            if (isHypo != wasHypothermic)
            {
                wasHypothermic = isHypo;
                OnHypothermiaStateChanged?.Invoke(isHypo);
            }
        }

        public void SetCurrentBodyTemperatureDirect(float val)
        {
            if (float.IsNaN(val) || float.IsInfinity(val)) return;
            currentBodyTemperature = Mathf.Clamp(val, minBodyTemperature, normalBodyTemperature);
            OnBodyTemperatureChanged?.Invoke(currentBodyTemperature);

            bool isHypo = IsInHypothermia;
            if (isHypo != wasHypothermic)
            {
                wasHypothermic = isHypo;
                OnHypothermiaStateChanged?.Invoke(isHypo);
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Combat Juice: Trauma screen shake when receiving direct damage
            if (amount >= 1.0f)
            {
                var cam = Camera.main != null ? Camera.main.GetComponent<Outpost404.CameraControl.TopDownCameraFollow>() : null;
                if (cam != null)
                {
                    float shakeAmt = Mathf.Clamp(amount / 30f, 0.15f, 0.45f);
                    cam.TriggerShake(shakeAmt, 0.18f);
                }
            }

            if (currentHealth <= 0.0001f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public bool ConsumeStamina(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return false;

            if (currentStamina >= amount)
            {
                currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);
                staminaRegenTimer = staminaRegenDelay;
                OnStaminaChanged?.Invoke(currentStamina, maxStamina);
                return true;
            }

            return false;
        }

        public void RestoreStamina(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentStamina = Mathf.Clamp(currentStamina + amount, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        public void ConsumeHunger(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHunger = Mathf.Clamp(currentHunger - amount, 0f, maxHunger);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);

            if (currentHunger <= 0.0001f)
            {
                TakeDamage(starvationDamageRate * 0.1f);
            }
        }

        public void RestoreHunger(float amount)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
        }

        public void SetCurrentHealthDirect(float val)
        {
            if (float.IsNaN(val)) return;
            currentHealth = Mathf.Clamp(val, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0.0001f && !isDead)
            {
                Die();
            }
        }

        public void SetCurrentHungerDirect(float val)
        {
            if (float.IsNaN(val)) return;
            currentHunger = Mathf.Clamp(val, 0f, maxHunger);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
        }

        public void SetCurrentStaminaDirect(float val)
        {
            if (float.IsNaN(val)) return;
            currentStamina = Mathf.Clamp(val, 0f, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            currentHealth = 0f;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnPlayerDied?.Invoke();
        }

        public void NotifyAllStats()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
            OnBodyTemperatureChanged?.Invoke(currentBodyTemperature);
            OnHypothermiaStateChanged?.Invoke(IsInHypothermia);
        }
    }
}
