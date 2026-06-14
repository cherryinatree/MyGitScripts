
using UnityEngine;

[CreateAssetMenu(
    fileName = "SpaceInvadersBossData",
    menuName = "Arcade/Space Invaders/Boss Data")]
public class SpaceInvadersBossData : ScriptableObject
{
    [Header("Boss")]
    public string bossName = "Boss";
    public SpaceInvadersBoss bossPrefab;
    public int maxHealth = 1000;
    public int scoreValue = 1000;

    [Header("Phases")]
    public SpaceInvadersBossPhase[] phases;

    [Header("Projectile Prefabs")]
    public SpaceInvadersBossProjectile projectilePrefab;
    public SpaceInvadersBossProjectile missilePrefab;
    public SpaceInvadersBossProjectile minePrefab;
    public SpaceInvadersBossLaser laserPrefab;

    [Header("Projectile Sprites")]
    public Sprite bulletSprite;
    public Sprite missileSprite;
    public Sprite mineSprite;
}