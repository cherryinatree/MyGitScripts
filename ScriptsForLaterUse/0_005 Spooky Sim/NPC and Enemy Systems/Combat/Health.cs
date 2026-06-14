using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [Min(1f)] public float MaxHealth = 100f;
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    [System.Serializable] public class DamageEvent : UnityEvent<float, GameObject> { } // amount, attacker
    public DamageEvent OnDamaged;
    public UnityEvent OnDied;

    private void Awake()
    {
        if (currentHealth <= 0f) currentHealth = MaxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
    }

    public void TakeDamage(float amount, GameObject attacker = null)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnDamaged?.Invoke(amount, attacker);

        if (currentHealth <= 0f)
            OnDied?.Invoke();
    }
}
