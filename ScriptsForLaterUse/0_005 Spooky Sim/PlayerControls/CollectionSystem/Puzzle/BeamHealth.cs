using System.Collections.Generic;
using UnityEngine;

public interface IBeamDamageable
{
    bool CanBeDamagedBy(BeamRayDefinition ray);
    bool ApplyBeamDamage(float amount, BeamRayDefinition ray, Vector3 hitPoint, Transform attacker);
    bool IsDead { get; }
}

[AddComponentMenu("Combat/Beam Health")]
public class BeamHealth : MonoBehaviour, IBeamDamageable
{
    public float maxHealth = 20f;
    [SerializeField] private float currentHealth = 20f;

    [Tooltip("Only these rays can damage this enemy.")]
    public List<BeamRayDefinition> damageFromRays = new();

    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public bool CanBeDamagedBy(BeamRayDefinition ray)
    {
        if (ray == null) return false;
        if (ray.damagePerSecond <= 0f) return false;
        return damageFromRays == null || damageFromRays.Count == 0 || damageFromRays.Contains(ray);
    }

    public bool ApplyBeamDamage(float amount, BeamRayDefinition ray, Vector3 hitPoint, Transform attacker)
    {
        if (IsDead) return false;
        if (!CanBeDamagedBy(ray)) return false;

        currentHealth -= Mathf.Max(0f, amount);
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            return true; // killed this call
        }
        return false;
    }
}