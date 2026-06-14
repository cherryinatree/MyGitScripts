using UnityEngine;

[CreateAssetMenu(
    fileName = "SpaceInvadersLevelData",
    menuName = "Arcade/Space Invaders/Level Data")]
public class SpaceInvadersLevelData : ScriptableObject
{
    [Header("Level")]
    public string levelName = "Level";
    public SpaceInvadersLevelType levelType = SpaceInvadersLevelType.Normal;

    [Header("Diver Rush")]
    public DiverRushData diverRushData;

    [Header("Boss")]
    public SpaceInvadersBossData bossData;

    [Header("General")]
    public AudioClip levelMusic;
    public int levelCompleteBonus = 100;
    public float powerUpDropChance = 0.1f;

    [Header("Normal Level")]
    public int rows = 5;
    public int columns = 11;
    public float spacingX = 60f;
    public float spacingY = 50f;

    public float moveAmount = 20f;
    public float startingMoveDelay = 0.5f;
    public float minimumMoveDelay = 0.08f;
    public float dropDistance = 20f;

    public float shootIntervalMin = 1.5f;
    public float shootIntervalMax = 3f;
    public int maxEnemyBullets = 2;

    public int maxActiveDivers = 1;
    public float diveCheckInterval = 3f;
    [Range(0f, 1f)] public float diveChance = 0.25f;

    [Header("Diver Rush Level")]
    public int totalDiversToSpawn = 30;
    public int maxDiversOnScreen = 4;
    public float diverSpawnInterval = 0.75f;
    public float diverRushSpeedMultiplier = 1.25f;
    public bool awardPointsOnlyIfKilled = true;

}