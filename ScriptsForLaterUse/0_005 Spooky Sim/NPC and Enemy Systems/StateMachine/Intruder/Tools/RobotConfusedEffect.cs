using UnityEngine;

[DisallowMultipleComponent]
public class RobotConfusedEffect : MonoBehaviour
{
    public float EndsAt { get; private set; }
    public bool IsActive => Time.time < EndsAt;

    /// <summary>Call this to start confusion for N seconds.</summary>
    public void Begin(float seconds)
    {
        EndsAt = Time.time + Mathf.Max(0.01f, seconds);
        enabled = true;
    }

    private void Update()
    {
        // Auto-remove when done
        if (!IsActive)
        {
            Destroy(this);
        }
    }
}
