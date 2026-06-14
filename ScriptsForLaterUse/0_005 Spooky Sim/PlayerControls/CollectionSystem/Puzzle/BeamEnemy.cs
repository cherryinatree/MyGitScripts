using System;
using System.Collections.Generic;
using UnityEngine;
using Cherry.Inventory;

[AddComponentMenu("Combat/Beam Enemy")]
public class BeamEnemy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 20f;
    [SerializeField] private float currentHealth = 20f;

    [Tooltip("Only these ray definitions can damage this enemy (empty means any damaging ray).")]
    public List<BeamRayDefinition> damageFromRays = new();

    [Header("Damage Rules")]
    [Tooltip("Prevents multiple reflected beam segments from damaging this enemy more than once in the same frame.")]
    public bool limitDamageToOncePerFrame = true;

    [Header("Drop (Harvestable)")]
    public ItemDefinition dropItem;
    [Min(1)] public int dropQuantity = 1;

    [Tooltip("Prefab that already has HarvestableItemSource configured (recommended).")]
    public GameObject dropHarvestablePrefab;

    [Tooltip("If no prefab, we will create a bare HarvestableItemSource. This may not look pretty.")]
    public bool allowBareDropFallback = true;

    [Header("Death")]
    public bool destroyOnDeath = true;

    private bool _dead;
    private bool _dropped;
    private int _lastDamageFrame = -1;

    public bool IsDead => _dead;
    public float CurrentHealth => currentHealth;

    // amount, ray, hitPoint, beamOrigin
    public event Action<float, BeamRayDefinition, Vector3, Transform> BeamDamaged;

    // ray, hitPoint, beamOrigin
    public event Action<BeamRayDefinition, Vector3, Transform> BeamKilled;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public bool CanBeDamagedBy(BeamRayDefinition ray)
    {
        if (ray == null) return false;
        if (ray.damagePerSecond <= 0f) return false;

        return damageFromRays == null
               || damageFromRays.Count == 0
               || damageFromRays.Contains(ray);
    }

    /// <returns>true if this call killed the enemy</returns>
    public bool ApplyBeamDamage(float amount, BeamRayDefinition ray, Vector3 hitPoint, Transform beamOrigin)
    {
        if (_dead) return false;
        if (!CanBeDamagedBy(ray)) return false;

        if (limitDamageToOncePerFrame && _lastDamageFrame == Time.frameCount)
            return false;

        _lastDamageFrame = Time.frameCount;

        float finalAmount = Mathf.Max(0f, amount);
        if (finalAmount <= 0f) return false;

        currentHealth -= finalAmount;
        currentHealth = Mathf.Max(0f, currentHealth);

        BeamDamaged?.Invoke(finalAmount, ray, hitPoint, beamOrigin);

        if (currentHealth <= 0f)
        {
            _dead = true;

            BeamKilled?.Invoke(ray, hitPoint, beamOrigin);

            SpawnDropAndTryCollect(hitPoint, beamOrigin);

            if (destroyOnDeath)
                Destroy(gameObject);

            return true;
        }

        return false;
    }

    private void SpawnDropAndTryCollect(Vector3 at, Transform beamOrigin)
    {
        if (_dropped) return;
        _dropped = true;

        GameObject go = null;

        if (dropHarvestablePrefab != null)
        {
            go = Instantiate(dropHarvestablePrefab, at, Quaternion.identity);
        }
        else if (allowBareDropFallback)
        {
            go = new GameObject("EnemyDrop");
            go.transform.position = at;
            go.AddComponent<SphereCollider>().isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"{name}: No drop prefab set, and bare fallback disabled.");
            return;
        }

        var his = go.GetComponent<HarvestableItemSource>();
        if (his == null) his = go.AddComponent<HarvestableItemSource>();

        his.item = dropItem;
        his.quantity = dropQuantity;

        if (beamOrigin != null)
            his.TryHarvestFromBeam(at, beamOrigin);
        else
            his.TryHarvestFromBeam(at, Camera.main != null ? Camera.main.transform : transform);
    }
}