using UnityEngine;

[CreateAssetMenu(
    fileName = "DiverRushData",
    menuName = "Arcade/Space Invaders/Diver Rush Data")]
public class DiverRushData : ScriptableObject
{
    [Header("Spawning")]
    public Invader diverPrefab;
    public Sprite diverSprite;
    public Sprite[] diverHealthSprites;

    public int totalDiversToSpawn = 30;
    public int maxDiversOnScreen = 4;
    public float spawnInterval = 0.75f;

    [Header("Spawn Area")]
    [Tooltip("How far beyond the top of the play area divers spawn.")]
    public float spawnTopPadding = 80f;

    [Tooltip("How far beyond the sides divers can spawn.")]
    public float spawnSidePadding = 40f;

    [Tooltip("How far beyond the bottom of the play area divers fly before disappearing.")]
    public float exitBottomPadding = 140f;

    [Header("Diver Stats")]
    public int diverHealth = 1;
    public int diverScoreValue = 25;

    [Header("Movement")]
    public float baseDiveSpeed = 520f;
    public float curveWidth = 220f;
    public float curveHeight = 160f;

    [Tooltip("Controls speed during the diving path.")]
    public AnimationCurve diveSpeedCurve =
        new AnimationCurve(
            new Keyframe(0f, 0.75f),
            new Keyframe(0.25f, 1.35f),
            new Keyframe(0.65f, 1.15f),
            new Keyframe(1f, 0.95f));

    [Header("Level")]
    public int levelCompleteBonus = 150;
}