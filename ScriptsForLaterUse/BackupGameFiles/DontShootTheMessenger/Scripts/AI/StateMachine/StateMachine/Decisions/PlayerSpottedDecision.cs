using UnityEngine;

/// <summary>
/// Returns true when the player is considered "spotted".
/// Uses MonsterPerception signals and includes dwell + memory to prevent flicker.
/// </summary>
public class PlayerSpottedDecision : CombatDecision
{
    [Header("References")]
    public MonsterPerception perception;

    [Header("Timing")]
    [Tooltip("How long the player must be seen before the decision turns true.")]
    public float spotDwellTime = 0.25f;

    [Tooltip("How long to remember a spot after losing sight (prevents flicker).")]
    public float spotMemoryTime = 1.0f;

    private float _spotTimer;         // accumulates while seeing
    private float _lastSpottedTime;   // last time we had visual

    protected override void Awake()
    {
        base.Awake();
        if (perception == null)
            perception = GetComponentInParent<MonsterPerception>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _spotTimer = 0f;
        _lastSpottedTime = -999f;
    }

    public override bool Decide()
    {
        if (perception == null) return false;

        // Consider any of these as "seeing" to be robust to your implementation:
        bool seeing = perception.PlayerInConeVisible || perception.IsPlayerSpotted || perception.CanSeePlayer;

        if (seeing)
        {
            _spotTimer += Time.deltaTime;
            _lastSpottedTime = Time.time;
        }
        else
        {
            // decay the dwell timer gently instead of hard reset (feel free to hard reset if you prefer)
            _spotTimer = Mathf.Max(0f, _spotTimer - Time.deltaTime * 0.5f);
        }

        // True if we've seen long enough OR within memory window
        bool dwellPassed = _spotTimer >= spotDwellTime;
        bool recent = (Time.time - _lastSpottedTime) <= spotMemoryTime;

        return dwellPassed || recent;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        _spotTimer = 0f;
    }
}
