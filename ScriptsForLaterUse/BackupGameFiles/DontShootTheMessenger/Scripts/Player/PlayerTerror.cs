using UnityEngine;

public class PlayerTerror : MonoBehaviour
{
    [Header("Terror Values")]
    [Range(0f, 100f)] public float Current = 0f;
    [Range(0f, 100f)] public float Max = 100f;

    [Header("Rates")]
    [Tooltip("Terror per second lost when calm.")]
    public float decayPerSecond = 4f;

    [Tooltip("Optional multiplier when recently scared.")]
    public float postScareDecayDelay = 2f;

    private float _suppressDecayUntil;

    void Update()
    {
        if (Time.time >= _suppressDecayUntil)
        {
            if (Current > 0f)
            {
                Current -= decayPerSecond * Time.deltaTime;
                if (Current < 0f) Current = 0f;
            }
        }
    }

    /// <summary>Increase terror immediately and optionally delay decay a bit.</summary>
    public void AddTerror(float amount, float suppressDecaySeconds = 0.5f)
    {
        if (amount <= 0f) return;
        Current = Mathf.Clamp(Current + amount, 0f, Max);
        _suppressDecayUntil = Mathf.Max(_suppressDecayUntil, Time.time + Mathf.Max(0f, suppressDecaySeconds));
    }

    /// <summary>Reduce terror immediately.</summary>
    public void AddCalm(float amount)
    {
        if (amount <= 0f) return;
        Current = Mathf.Clamp(Current - amount, 0f, Max);
    }

    // Convenience helpers you can call from AI / level scripts:
    public void OnCreepySoundNearby(float intensity = 8f) => AddTerror(intensity, 1.0f);
    public void OnMonsterSpotted(float intensity = 15f) => AddTerror(intensity, 1.0f);
    public void OnChaseStarted(float intensity = 20f) => AddTerror(intensity, 1.5f);
    public void OnSafeLightEntered(float calm = 10f) => AddCalm(calm);

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("In contact with enemy, adding terror.");
            // Gradually calm down while inside a safe light
            AddTerror(5f * Time.deltaTime);
        }
    }
}
