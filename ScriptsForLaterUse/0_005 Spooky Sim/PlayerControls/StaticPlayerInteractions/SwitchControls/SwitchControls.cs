using UnityEngine;

public class SwitchControls : MonoBehaviour
{
    [Header("Scripts to toggle")]
    [SerializeField] private MonoBehaviour Flop0;
    [SerializeField] private MonoBehaviour Flop1;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float flopCooldownSeconds = 0.5f;

    private bool isFlop0Active = true;
    private float _nextAllowedFlopTime;

    private void Start()
    {
        // Safety checks
        if (Flop0 == null || Flop1 == null)
        {
            Debug.LogError($"{name}: Assign Flop0 and Flop1 in the inspector.");
            enabled = false;
            return;
        }

        SetFlopState(true);
        _nextAllowedFlopTime = 0f;
    }

    public void FlopScripts()
    {
        // Cooldown gate
        if (Time.time < _nextAllowedFlopTime)
            return;

        _nextAllowedFlopTime = Time.time + flopCooldownSeconds;

        // Toggle
        SetFlopState(!isFlop0Active);
    }

    public void SetFlopState(bool makeFlop0Active)
    {
        isFlop0Active = makeFlop0Active;

        Flop0.enabled = isFlop0Active;
        Flop1.enabled = !isFlop0Active;
    }
}
