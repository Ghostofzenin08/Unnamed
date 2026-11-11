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

        // Runtime state timers
        private float staminaRegenTimer = 0f;
        private bool isDead = false;
        private PlayerController playerController;

        // Decoupled observation events (current, max)
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<float, float> OnHungerChanged;
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

        public float MaxHealthProperty { get => maxHealth; set => maxHealth = Mathf.Max(1f, value); }
        public float MaxStaminaProperty { get => maxStamina; set => maxStamina = Mathf.Max(1f, value); }
        public float MaxHungerProperty { get => maxHunger; set => maxHunger = Mathf.Max(1f, value); }
        public float SprintStaminaCost { get => sprintStaminaCost; set => sprintStaminaCost = value; }

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
            isDead = false;
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
        }

        public void TakeDamage(float amount, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead || amount <= 0f || float.IsNaN(amount)) return;

            currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

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
        }
    }
}
