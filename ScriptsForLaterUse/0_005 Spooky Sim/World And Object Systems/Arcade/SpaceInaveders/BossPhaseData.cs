using UnityEngine;

[System.Serializable]
public class BossPhaseData
{
    [Header("Phase")]
    public string phaseName = "Phase";

    [Tooltip("Boss enters this phase when health percent is less than or equal to this value.")]
    [Range(0f, 1f)]
    public float healthPercentThreshold = 1f;

    [Header("Animation")]
    public Sprite[] idleFrames;
    public Sprite[] attackFrames;
    public Sprite[] damagedFrames;
    public float animationFrameRate = 8f;

    [Header("Movement")]
    public float moveSpeed = 120f;
    public float horizontalRange = 300f;
    public float verticalBobAmount = 30f;
    public float verticalBobSpeed = 2f;

    [Header("Attacks")]
    public BossAttackPattern[] attackPatterns;
    public float attackIntervalMin = 1.5f;
    public float attackIntervalMax = 3.5f;
}