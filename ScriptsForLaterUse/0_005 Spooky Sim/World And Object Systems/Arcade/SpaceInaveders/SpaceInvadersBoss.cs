using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum SpaceInvadersBossAttackPattern
{
    MissileVolley,
    LaserSweep,
    BulletRain,
    SpiralShots,
    DropMines,
    ChargeDash
}

[System.Serializable]
public class SpaceInvadersBossPhase
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
    public float verticalBobAmount = 24f;
    public float verticalBobSpeed = 2f;

    [Header("Attacks")]
    public SpaceInvadersBossAttackPattern[] attackPatterns;
    public float attackIntervalMin = 1.5f;
    public float attackIntervalMax = 3.5f;
}


[RequireComponent(typeof(RectTransform))]
public class SpaceInvadersBoss : MonoBehaviour
{
    private enum BossAnimState
    {
        Idle,
        Attack,
        Damaged
    }

    [Header("Hit Flash")]
    [SerializeField] private float damagedAnimDuration = 0.2f;

    private RectTransform rect;
    private Image image;

    private SpaceInvadersBossManager manager;
    private SpaceInvadersBossData data;
    private SpaceInvadersBossPhase currentPhase;
    private ArcadeController player;
    private RectTransform playArea;

    private Vector2 startPosition;

    private int maxHealth;
    private int currentHealth;
    private int currentPhaseIndex = -1;

    private float animationTimer;
    private int animationFrameIndex;
    private BossAnimState animState = BossAnimState.Idle;

    private float attackTimer;
    private bool attacking;
    private bool dead;

    public RectTransform Rect => rect;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Initialize(
        SpaceInvadersBossManager bossManager,
        SpaceInvadersBossData bossData,
        RectTransform newPlayArea,
        ArcadeController newPlayer,
        Vector2 spawnPosition)
    {
        manager = bossManager;
        data = bossData;
        playArea = newPlayArea;
        player = newPlayer;

        startPosition = spawnPosition;
        rect.anchoredPosition = spawnPosition;

        maxHealth = Mathf.Max(1, data.maxHealth);
        currentHealth = maxHealth;

        dead = false;
        attacking = false;

        UpdatePhase(true);
        ResetAttackTimer();
    }

    private void Update()
    {
        if (dead || data == null)
            return;

        UpdatePhase(false);
        UpdateMovement();
        UpdateAnimation();
        UpdateAttacks();
    }

    private void UpdatePhase(bool force)
    {
        if (data.phases == null || data.phases.Length == 0)
            return;

        float healthPercent =
            currentHealth / (float)maxHealth;

        int selectedIndex = 0;

        for (int i = 0; i < data.phases.Length; i++)
        {
            if (healthPercent <= data.phases[i].healthPercentThreshold)
                selectedIndex = i;
        }

        if (!force && selectedIndex == currentPhaseIndex)
            return;

        currentPhaseIndex = selectedIndex;
        currentPhase = data.phases[currentPhaseIndex];

        animationFrameIndex = 0;
        animationTimer = 0f;

        Debug.Log($"Boss phase changed to: {currentPhase.phaseName}");
    }

    private void UpdateMovement()
    {
        if (currentPhase == null)
            return;

        float x =
            Mathf.Sin(Time.time * currentPhase.moveSpeed * 0.01f) *
            currentPhase.horizontalRange;

        float y =
            Mathf.Sin(Time.time * currentPhase.verticalBobSpeed) *
            currentPhase.verticalBobAmount;

        rect.anchoredPosition =
            startPosition + new Vector2(x, y);
    }

    private void UpdateAnimation()
    {
        if (currentPhase == null || image == null)
            return;

        Sprite[] frames = GetCurrentFrames();

        if (frames == null || frames.Length == 0)
            return;

        animationTimer += Time.deltaTime;

        float frameDelay =
            1f / Mathf.Max(1f, currentPhase.animationFrameRate);

        if (animationTimer < frameDelay)
            return;

        animationTimer = 0f;

        animationFrameIndex++;

        if (animationFrameIndex >= frames.Length)
            animationFrameIndex = 0;

        image.sprite = frames[animationFrameIndex];
    }

    private Sprite[] GetCurrentFrames()
    {
        if (currentPhase == null)
            return null;

        switch (animState)
        {
            case BossAnimState.Attack:
                if (currentPhase.attackFrames != null &&
                    currentPhase.attackFrames.Length > 0)
                    return currentPhase.attackFrames;
                break;

            case BossAnimState.Damaged:
                if (currentPhase.damagedFrames != null &&
                    currentPhase.damagedFrames.Length > 0)
                    return currentPhase.damagedFrames;
                break;
        }

        return currentPhase.idleFrames;
    }

    private void UpdateAttacks()
    {
        if (currentPhase == null)
            return;

        if (attacking)
            return;

        attackTimer -= Time.deltaTime;

        if (attackTimer > 0f)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        attacking = true;
        animState = BossAnimState.Attack;

        SpaceInvadersBossAttackPattern attack =
            PickAttack();

        switch (attack)
        {
            case SpaceInvadersBossAttackPattern.MissileVolley:
                yield return MissileVolley();
                break;

            case SpaceInvadersBossAttackPattern.LaserSweep:
                yield return LaserSweep();
                break;

            case SpaceInvadersBossAttackPattern.BulletRain:
                yield return BulletRain();
                break;

            case SpaceInvadersBossAttackPattern.SpiralShots:
                yield return SpiralShots();
                break;

            case SpaceInvadersBossAttackPattern.DropMines:
                yield return DropMines();
                break;

            case SpaceInvadersBossAttackPattern.ChargeDash:
                yield return ChargeDash();
                break;
        }

        animState = BossAnimState.Idle;
        attacking = false;

        ResetAttackTimer();
    }

    private SpaceInvadersBossAttackPattern PickAttack()
    {
        if (currentPhase.attackPatterns == null ||
            currentPhase.attackPatterns.Length == 0)
        {
            return SpaceInvadersBossAttackPattern.MissileVolley;
        }

        return currentPhase.attackPatterns[
            Random.Range(0, currentPhase.attackPatterns.Length)];
    }

    private void ResetAttackTimer()
    {
        if (currentPhase == null)
        {
            attackTimer = 2f;
            return;
        }

        attackTimer =
            Random.Range(
                currentPhase.attackIntervalMin,
                currentPhase.attackIntervalMax);
    }

    public void TakeDamage(int damage)
    {
        if (dead)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        StartCoroutine(DamagedAnimRoutine());

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator DamagedAnimRoutine()
    {
        animState = BossAnimState.Damaged;

        yield return new WaitForSeconds(damagedAnimDuration);

        if (!attacking)
            animState = BossAnimState.Idle;
    }

    private void Die()
    {
        if (dead)
            return;

        dead = true;

        if (manager != null)
            manager.BossKilled(this, data.scoreValue);

        Destroy(gameObject);
    }

    private IEnumerator MissileVolley()
    {
        int missileCount =
            currentPhaseIndex >= 2
                ? 7
                : 4;

        for (int i = 0; i < missileCount; i++)
        {
            Vector2 offset =
                new Vector2(
                    Random.Range(-120f, 120f),
                    -60f);

            SpawnProjectile(
                data.missilePrefab,
                data.missileSprite,
                rect.anchoredPosition + offset,
                Vector2.down,
                260f,
                true);

            yield return new WaitForSeconds(0.18f);
        }
    }

    private IEnumerator LaserSweep()
    {
        if (data.laserPrefab == null)
            yield break;

        float startX =
            Random.value > 0.5f
                ? -playArea.rect.width * 0.45f
                : playArea.rect.width * 0.45f;

        float direction =
            startX < 0f
                ? 1f
                : -1f;

        SpaceInvadersBossLaser laser =
            Instantiate(
                data.laserPrefab,
                manager.EffectParent);

        laser.Initialize(
            playArea,
            player,
            new Vector2(startX, 0f),
            direction,
            0.65f,
            1.4f,
            500f);

        yield return new WaitForSeconds(1.7f);
    }

    private IEnumerator BulletRain()
    {
        int bulletCount =
            currentPhaseIndex >= 2
                ? 24
                : 14;

        float halfWidth = playArea.rect.width * 0.5f;

        for (int i = 0; i < bulletCount; i++)
        {
            Vector2 position =
                new Vector2(
                    Random.Range(-halfWidth + 50f, halfWidth - 50f),
                    playArea.rect.height * 0.5f + 40f);

            SpawnProjectile(
                data.projectilePrefab,
                data.bulletSprite,
                position,
                Vector2.down,
                Random.Range(240f, 420f),
                false);

            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator SpiralShots()
    {
        int waves =
            currentPhaseIndex >= 2
                ? 6
                : 4;

        int bulletsPerWave = 10;

        float angleOffset = 0f;

        for (int wave = 0; wave < waves; wave++)
        {
            for (int i = 0; i < bulletsPerWave; i++)
            {
                float angle =
                    angleOffset +
                    i *
                    (360f / bulletsPerWave);

                Vector2 direction =
                    new Vector2(
                        Mathf.Cos(angle * Mathf.Deg2Rad),
                        Mathf.Sin(angle * Mathf.Deg2Rad));

                if (direction.y > 0f)
                    direction.y *= -0.35f;

                direction.Normalize();

                SpawnProjectile(
                    data.projectilePrefab,
                    data.bulletSprite,
                    rect.anchoredPosition,
                    direction,
                    260f,
                    false);
            }

            angleOffset += 18f;

            yield return new WaitForSeconds(0.22f);
        }
    }

    private IEnumerator DropMines()
    {
        int mineCount =
            currentPhaseIndex >= 2
                ? 6
                : 3;

        for (int i = 0; i < mineCount; i++)
        {
            Vector2 offset =
                new Vector2(
                    Random.Range(-160f, 160f),
                    -70f);

            SpawnProjectile(
                data.minePrefab,
                data.mineSprite,
                rect.anchoredPosition + offset,
                Vector2.down,
                150f,
                false);

            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator ChargeDash()
    {
        Vector2 original = rect.anchoredPosition;

        float side =
            Random.value > 0.5f
                ? 1f
                : -1f;

        Vector2 offscreen =
            new Vector2(
                side * (playArea.rect.width * 0.5f + 250f),
                original.y);

        while (Vector2.Distance(rect.anchoredPosition, offscreen) > 4f)
        {
            rect.anchoredPosition =
                Vector2.MoveTowards(
                    rect.anchoredPosition,
                    offscreen,
                    900f * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(0.4f);

        Vector2 chargeStart =
            new Vector2(
                -side * (playArea.rect.width * 0.5f + 250f),
                player != null
                    ? player.Rect.anchoredPosition.y + 120f
                    : -120f);

        rect.anchoredPosition = chargeStart;

        Vector2 chargeEnd =
            new Vector2(
                side * (playArea.rect.width * 0.5f + 250f),
                chargeStart.y);

        while (Vector2.Distance(rect.anchoredPosition, chargeEnd) > 4f)
        {
            rect.anchoredPosition =
                Vector2.MoveTowards(
                    rect.anchoredPosition,
                    chargeEnd,
                    1100f * Time.deltaTime);

            if (player != null &&
                player.CanBeHit &&
                RectsOverlap(rect, player.Rect))
            {
                player.Hit();
            }

            yield return null;
        }

        rect.anchoredPosition = original;
    }

    private void SpawnProjectile(
        SpaceInvadersBossProjectile prefab,
        Sprite sprite,
        Vector2 position,
        Vector2 direction,
        float speed,
        bool homing)
    {
        if (prefab == null)
            return;

        SpaceInvadersBossProjectile projectile =
            Instantiate(
                prefab,
                manager.ProjectileParent);

        projectile.Initialize(
            playArea,
            player,
            sprite,
            position,
            direction,
            speed,
            homing);
    }

    private bool RectsOverlap(RectTransform a, RectTransform b)
    {
        Rect rectA = new Rect(
            a.anchoredPosition - a.sizeDelta * 0.5f,
            a.sizeDelta);

        Rect rectB = new Rect(
            b.anchoredPosition - b.sizeDelta * 0.5f,
            b.sizeDelta);

        return rectA.Overlaps(rectB);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerBullet bullet =
            other.GetComponentInParent<PlayerBullet>();

        if (bullet != null)
        {
            TakeDamage(1);
            Destroy(bullet.gameObject);
        }
    }
}
