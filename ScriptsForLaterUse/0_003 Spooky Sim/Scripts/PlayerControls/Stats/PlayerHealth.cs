using System;
using UnityEngine;

namespace Cherry.Combat
{
    /// <summary>
    /// Simple, modular health component for players (or any living entity).
    /// - Damage / Heal
    /// - Optional regen
    /// - Invincibility (i-frames) after hit
    /// - Events for UI, audio, VFX, death handling
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1)] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Damage Tuning")]
        [Tooltip("Multiplier applied to incoming damage. 1 = normal, <1 = resistant, >1 = vulnerable.")]
        [SerializeField] private float damageMultiplier = 1f;

        [Tooltip("If true, health can't go below 1 (useful for tutorial sections).")]
        [SerializeField] private bool preventDeath = false;

        [Header("Invincibility Frames")]
        [Tooltip("Seconds of invulnerability after taking damage.")]
        [SerializeField, Min(0f)] private float invincibilityDuration = 0.25f;
        private float invincibleUntil = -1f;

        [Header("Regeneration (Optional)")]
        [SerializeField] private bool useRegen = false;
        [SerializeField, Min(0f)] private float regenPerSecond = 2f;
        [SerializeField, Min(0f)] private float regenDelayAfterHit = 3f;
        private float lastDamageTime = -999f;

        [Header("Debug")]
        [SerializeField] private bool logChanges = false;

        // --------- Events ---------
        /// <summary> Fired whenever health changes (current, max). Great for UI. </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary> Fired when damage is actually applied (amount after multipliers). </summary>
        public event Action<float> OnDamaged;

        /// <summary> Fired when healed. </summary>
        public event Action<float> OnHealed;

        /// <summary> Fired once when health reaches 0. </summary>
        public event Action OnDied;

        // --------- Public Properties ---------
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth <= 0 ? 0 : currentHealth / maxHealth;
        public bool IsDead { get; private set; }
        public bool IsInvincible => Time.time < invincibleUntil;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            if (currentHealth <= 0) currentHealth = maxHealth;
            BroadcastHealthChanged();
        }

        private void Update()
        {
            if (IsDead || !useRegen) return;

            if (Time.time >= lastDamageTime + regenDelayAfterHit && currentHealth < maxHealth)
            {
                Heal(regenPerSecond * Time.deltaTime);
            }
        }

        // --------- Core API ---------

        /// <summary>
        /// Apply raw damage. Returns true if damage was applied.
        /// </summary>
        public bool TakeDamage(float amount, GameObject source = null)
        {
            if (IsDead) return false;
            if (amount <= 0f) return false;
            if (IsInvincible) return false;

            float finalDamage = amount * damageMultiplier;
            finalDamage = Mathf.Max(0f, finalDamage);

            if (finalDamage <= 0f) return false;

            lastDamageTime = Time.time;
            invincibleUntil = Time.time + invincibilityDuration;

            float newHealth = currentHealth - finalDamage;

            if (preventDeath)
                newHealth = Mathf.Max(1f, newHealth);

            SetHealthInternal(newHealth);

            OnDamaged?.Invoke(finalDamage);

            if (logChanges)
                Debug.Log($"{name} took {finalDamage} damage (raw {amount}) from {(source ? source.name : "unknown")}");

            if (currentHealth <= 0f && !IsDead)
                Die();
            Debug.Log("player health after damage: " + currentHealth);
            return true;
        }

        /// <summary>
        /// Heal health. Returns actual healed amount.
        /// </summary>
        public float Heal(float amount)
        {
            if (IsDead) return 0f;
            if (amount <= 0f) return 0f;

            float before = currentHealth;
            SetHealthInternal(currentHealth + amount);
            float healed = currentHealth - before;

            if (healed > 0f)
            {
                OnHealed?.Invoke(healed);

                if (logChanges)
                    Debug.Log($"{name} healed {healed} HP (requested {amount})");
            }

            return healed;
        }

        /// <summary>
        /// Fully restore health.
        /// </summary>
        public void FullHeal()
        {
            if (IsDead) return;
            SetHealthInternal(maxHealth);
        }

        /// <summary>
        /// Increase max health; optionally fill to new max.
        /// </summary>
        public void AddMaxHealth(float amount, bool alsoHealToMax = true)
        {
            if (amount <= 0f) return;
            maxHealth += amount;
            if (alsoHealToMax) currentHealth = maxHealth;
            else currentHealth = Mathf.Min(currentHealth, maxHealth);
            BroadcastHealthChanged();
        }

        /// <summary>
        /// Revive after death with a given percent of max health.
        /// </summary>
        public void Revive(float percent = 1f)
        {
            percent = Mathf.Clamp01(percent);
            IsDead = false;
            SetHealthInternal(maxHealth * percent);
        }

        // --------- Internals ---------

        private void SetHealthInternal(float value)
        {
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            BroadcastHealthChanged();
        }

        private void BroadcastHealthChanged()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            IsDead = true;
            currentHealth = 0f;
            BroadcastHealthChanged();

            if (logChanges)
                Debug.Log($"{name} died.");

            OnDied?.Invoke();
        }

        // --------- Inspector Helpers ---------
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
        }
    }
}
