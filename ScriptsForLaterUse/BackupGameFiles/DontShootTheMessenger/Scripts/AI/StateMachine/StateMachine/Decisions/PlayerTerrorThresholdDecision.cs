using UnityEngine;

/// <summary>
/// Returns true when the player's terror reaches/exceeds a threshold.
/// Supports hysteresis to avoid flapping (engage at High, release at Low).
/// </summary>
public class PlayerTerrorThresholdDecision : CombatDecision
{
    [Header("References")]
    public Transform player;
    public PlayerTerror playerTerror;   // attach to the player

    [Header("Thresholds")]
    [Tooltip("Terror at/above which this decision becomes true.")]
    [Range(0f, 100f)] public float engageThreshold = 70f;

    [Tooltip("If useHysteresis is true, the decision remains true until terror drops below this.")]
    [Range(0f, 100f)] public float releaseThreshold = 60f;

    [Tooltip("Use hysteresis (engage at one level, release at another).")]
    public bool useHysteresis = true;

    private bool _latched;

    protected override void Awake()
    {
        base.Awake();
        if (player == null)
        {
            // Try to find player from any known place, or leave it null.
            var perception = GetComponentInParent<MonsterPerception>();
            if (perception != null && perception.player != null)
                player = perception.player;
        }
        if (playerTerror == null && player != null)
            playerTerror = player.GetComponent<PlayerTerror>();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        _latched = false;
    }

    public override bool Decide()
    {
        if (playerTerror == null) return false;

        float t = playerTerror.Current;

        if (!useHysteresis)
        {
            return t >= engageThreshold;
        }

        // Hysteresis: once engaged, stay true until we cross back below release
        if (!_latched && t >= engageThreshold)
            _latched = true;
        else if (_latched && t <= releaseThreshold)
            _latched = false;

        return _latched;
    }

    public override void OnExitState()
    {
        base.OnExitState();
        _latched = false;
    }
}
