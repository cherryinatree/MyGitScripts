using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InvaderRowSetup
{
    public string rowName = "Normal Row";

    [Header("Type")]
    public InvaderType type = InvaderType.Normal;

    [Header("Sprites")]
    [Tooltip("Main sprite used if no health-specific sprite is available.")]
    public Sprite sprite;

    [Tooltip(
        "Optional. Used mostly for tanks. Index 0 = 1 HP remaining, " +
        "Index 1 = 2 HP remaining, Index 2 = 3 HP remaining, etc.")]
    public Sprite[] spritesByRemainingHealth;

    [Header("Stats")]
    public int scoreValue = 10;
    public int health = 1;
}

public class InvaderManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private RectTransform playArea;
    [SerializeField] private Invader invaderPrefab;
    [SerializeField] private Transform invaderParent;

    [Header("Formation Spawn")]
    [SerializeField] private RectTransform formationStartPoint;
    [SerializeField] private Vector2 fallbackFormationStartPosition = new Vector2(0, 220f);

    [Header("Rows")]
    [SerializeField] private InvaderRowSetup[] rowSetups;

    [Header("Fallback Sprites")]
    [SerializeField] private Sprite[] fallbackInvaderSprites;

    [Header("Formation")]
    [SerializeField] private int rows = 5;
    [SerializeField] private int columns = 11;
    [SerializeField] private float spacingX = 60f;
    [SerializeField] private float spacingY = 50f;

    [Header("Movement")]
    [SerializeField] private float moveAmount = 20f;
    [SerializeField] private float startingMoveDelay = 0.5f;
    [SerializeField] private float minimumMoveDelay = 0.08f;
    [SerializeField] private float dropDistance = 20f;
    [SerializeField] private float sidePadding = 50f;

    [Header("Enemy Shooting")]
    [SerializeField] private EnemyBullet enemyBulletPrefab;
    [SerializeField] private Transform enemyBulletParent;
    [SerializeField] private float shootIntervalMin = 1.0f;
    [SerializeField] private float shootIntervalMax = 2.5f;
    [SerializeField] private int maxEnemyBullets = 3;

    [Header("Diving Enemies")]
    [SerializeField] private int maxActiveDivers = 2;
    [SerializeField] private float diveCheckInterval = 2.0f;
    [SerializeField, Range(0f, 1f)] private float diveChance = 0.5f;

    private readonly List<Invader> invaders = new();
    private readonly List<EnemyBullet> activeEnemyBullets = new();

    private ArcadeController player;

    private bool gameActive;
    private bool movingRight = true;

    private float moveTimer;
    private float currentMoveDelay;
    private float shootTimer;
    private float diveTimer;

    private int activeDivers;
    private SpaceInvadersLevelData currentLevelData;
    [Header("Power Ups")]
    [SerializeField] private SpaceInvadersPowerUp powerUpPrefab;
    [SerializeField] private Transform powerUpParent;



    [System.Serializable]
    public class PowerUpSpriteSetup
    {
        public SpaceInvadersPowerUpType type;
        public Sprite sprite;
    }

    [SerializeField] private PowerUpSpriteSetup[] powerUpSprites;

    private void ApplyLevelData(SpaceInvadersLevelData levelData)
    {
        currentLevelData = levelData;

        if (levelData == null)
            return;

        rows = levelData.rows;
        columns = levelData.columns;

        spacingX = levelData.spacingX;
        spacingY = levelData.spacingY;

        moveAmount = levelData.moveAmount;
        startingMoveDelay = levelData.startingMoveDelay;
        minimumMoveDelay = levelData.minimumMoveDelay;
        dropDistance = levelData.dropDistance;

        shootIntervalMin = levelData.shootIntervalMin;
        shootIntervalMax = levelData.shootIntervalMax;
        maxEnemyBullets = levelData.maxEnemyBullets;

        maxActiveDivers = levelData.maxActiveDivers;
        diveCheckInterval = levelData.diveCheckInterval;
        diveChance = levelData.diveChance;
    }

    public void BeginGame(
    RectTransform newPlayArea,
    ArcadeController newPlayer,
    SpaceInvadersLevelData levelData)
    {
        playArea = newPlayArea;
        ApplyLevelData(levelData);
        player = newPlayer;

        if (powerUpParent == null)
            powerUpParent = playArea;
        if (invaderParent == null)
            invaderParent = transform;

        if (enemyBulletParent == null)
            enemyBulletParent = playArea;

        gameActive = true;

        currentMoveDelay = startingMoveDelay;
        moveTimer = currentMoveDelay;

        shootTimer = Random.Range(shootIntervalMin, shootIntervalMax);
        diveTimer = diveCheckInterval;

        ClearWave();
        ClearEnemyBullets();

        SpawnWave();
    }

    public void EndGame()
    {
        gameActive = false;

        ClearWave();
        ClearEnemyBullets();
    }

    private void Update()
    {
        if (!gameActive)
            return;

        UpdateFormationMovement();
        UpdateEnemyShooting();
        UpdateDiving();
        CheckInvaderPlayerContacts();
    }

    public void SpawnWave()
    {
        ClearWave();

        movingRight = true;
        activeDivers = 0;

        currentMoveDelay = startingMoveDelay;

        Vector2 formationStart = GetFormationStartPosition();

        float width = (columns - 1) * spacingX;
        float startX = formationStart.x - width * 0.5f;
        float startY = formationStart.y;

        for (int row = 0; row < rows; row++)
        {
            InvaderRowSetup setup = GetRowSetup(row);

            for (int col = 0; col < columns; col++)
            {
                Invader invader = Instantiate(invaderPrefab, invaderParent);

                Vector2 position = new Vector2(
                    startX + col * spacingX,
                    startY - row * spacingY);

                invader.Rect.anchoredPosition = position;

                invader.Initialize(
                    this,
                    setup.type,
                    setup.sprite,
                    setup.spritesByRemainingHealth,
                    setup.scoreValue,
                    setup.health);

                invaders.Add(invader);
            }
        }
    }

    private InvaderRowSetup GetRowSetup(int row)
    {
        if (rowSetups != null && rowSetups.Length > 0)
        {
            int index = Mathf.Min(row, rowSetups.Length - 1);

            if (rowSetups[index] != null)
                return rowSetups[index];
        }

        Sprite fallbackSprite = null;

        if (fallbackInvaderSprites != null && fallbackInvaderSprites.Length > 0)
        {
            fallbackSprite =
                fallbackInvaderSprites[
                    Mathf.Min(row, fallbackInvaderSprites.Length - 1)];
        }

        return new InvaderRowSetup
        {
            rowName = $"Fallback Row {row}",
            type = InvaderType.Normal,
            sprite = fallbackSprite,
            scoreValue = 10,
            health = 1
        };
    }

    private Vector2 GetFormationStartPosition()
    {
        if (formationStartPoint != null)
            return formationStartPoint.anchoredPosition;

        return fallbackFormationStartPosition;
    }

    private void UpdateFormationMovement()
    {
        moveTimer -= Time.deltaTime;

        if (moveTimer > 0f)
            return;

        moveTimer = currentMoveDelay;

        MoveFormation();
    }

    private void MoveFormation()
    {
        bool hitWall = false;
        SpaceInvadersManager.Instance.PlayEnemyStepSound();

        float edge = playArea.rect.width / 2f - sidePadding;

        Vector2 movement =
            Vector2.right * (movingRight ? moveAmount : -moveAmount);

        foreach (Invader invader in invaders)
        {
            if (invader == null)
                continue;

            invader.OffsetHomePosition(movement);

            if (invader.Rect.anchoredPosition.x > edge ||
                invader.Rect.anchoredPosition.x < -edge)
            {
                hitWall = true;
            }
        }

        if (!hitWall)
            return;

        movingRight = !movingRight;

        Vector2 drop = Vector2.down * dropDistance;

        foreach (Invader invader in invaders)
        {
            if (invader == null)
                continue;

            invader.OffsetHomePosition(drop);
        }
    }

    private void UpdateEnemyShooting()
    {
        if (enemyBulletPrefab == null)
            return;

        shootTimer -= Time.deltaTime;

        if (shootTimer > 0f)
            return;

        shootTimer = Random.Range(shootIntervalMin, shootIntervalMax);

        activeEnemyBullets.RemoveAll(bullet => bullet == null);

        if (activeEnemyBullets.Count >= maxEnemyBullets)
            return;

        Invader shooter = PickShooter();

        if (shooter == null)
            return;

        EnemyBullet bullet = Instantiate(enemyBulletPrefab, enemyBulletParent);

        RectTransform bulletRect = bullet.GetComponent<RectTransform>();

        bulletRect.anchoredPosition =
            shooter.Rect.anchoredPosition + Vector2.down * 35f;

        bullet.Initialize(playArea, this);

        activeEnemyBullets.Add(bullet);
    }

    private Invader PickShooter()
    {
        List<Invader> shooters = new();

        foreach (Invader invader in invaders)
        {
            if (invader == null)
                continue;

            if (invader.IsDiving)
                continue;

            if (invader.Type != InvaderType.Shooter)
                continue;

            shooters.Add(invader);
        }

        if (shooters.Count == 0)
            return null;

        return shooters[Random.Range(0, shooters.Count)];
    }

    private void UpdateDiving()
    {
        if (player == null)
            return;

        if (activeDivers >= maxActiveDivers)
            return;

        diveTimer -= Time.deltaTime;

        if (diveTimer > 0f)
            return;

        diveTimer = diveCheckInterval;

        if (Random.value > diveChance)
            return;

        Invader diver = PickAvailableDiver();

        if (diver != null)
            diver.StartDive(player);
    }

    private Invader PickAvailableDiver()
    {
        List<Invader> candidates = new();

        foreach (Invader invader in invaders)
        {
            if (invader == null)
                continue;

            if (invader.CanDive)
                candidates.Add(invader);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void CheckInvaderPlayerContacts()
    {
        if (player == null)
            return;

        if (!player.CanBeHit)
            return;

        foreach (Invader invader in invaders)
        {
            if (invader == null)
                continue;

            if (RectsOverlap(invader.Rect, player.Rect))
            {
                player.Hit();

                if (invader.Type == InvaderType.Diver && invader.IsDiving)
                    invader.Kill(false);

                return;
            }
        }
    }

    public bool RectsOverlap(RectTransform a, RectTransform b)
    {
        Rect rectA = new Rect(
            a.anchoredPosition - a.sizeDelta * 0.5f,
            a.sizeDelta);

        Rect rectB = new Rect(
            b.anchoredPosition - b.sizeDelta * 0.5f,
            b.sizeDelta);

        return rectA.Overlaps(rectB);
    }

    public void InvaderKilled(Invader invader)
    {
        Vector2 deathPosition =
         invader != null
        ? invader.Rect.anchoredPosition
        : Vector2.zero;

        TrySpawnPowerUp(deathPosition);

        SpaceInvadersManager.Instance.PlayEnemyKilledSound();
        invaders.Remove(invader);

        activeDivers = Mathf.Max(0, activeDivers);

        UpdateSpeedFromRemainingInvaders();

        if (invaders.Count == 0)
        {
            if (SpaceInvadersManager.Instance != null)
                SpaceInvadersManager.Instance.WinWave();
        }
    }
    private void TrySpawnPowerUp(Vector2 position)
    {
        if (powerUpPrefab == null)
            return;

        if (currentLevelData == null)
            return;

        if (Random.value > currentLevelData.powerUpDropChance)
            return;

        SpaceInvadersPowerUpType type =
            GetRandomPowerUpType();

        Sprite sprite =
            GetPowerUpSprite(type);

        SpaceInvadersPowerUp powerUp =
            Instantiate(
                powerUpPrefab,
                powerUpParent);

        RectTransform rect =
            powerUp.GetComponent<RectTransform>();

        rect.anchoredPosition = position;

        powerUp.Initialize(
            type,
            sprite,
            playArea);
    }
    private SpaceInvadersPowerUpType GetRandomPowerUpType()
    {
        SpaceInvadersPowerUpType[] values =
            (SpaceInvadersPowerUpType[])
            System.Enum.GetValues(
                typeof(SpaceInvadersPowerUpType));

        return values[
            Random.Range(
                0,
                values.Length)];
    }

    private Sprite GetPowerUpSprite(
        SpaceInvadersPowerUpType type)
    {
        foreach (PowerUpSpriteSetup setup in powerUpSprites)
        {
            if (setup.type == type)
                return setup.sprite;
        }

        return null;
    }
    private Coroutine slowRoutine;

    public void TemporarilySlowEnemies()
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine());
    }

    private IEnumerator SlowRoutine()
    {
        float originalMoveDelay = currentMoveDelay;
        float originalShootMin = shootIntervalMin;
        float originalShootMax = shootIntervalMax;

        currentMoveDelay *= 1.75f;
        shootIntervalMin *= 1.75f;
        shootIntervalMax *= 1.75f;

        yield return new WaitForSeconds(6f);

        currentMoveDelay = originalMoveDelay;
        shootIntervalMin = originalShootMin;
        shootIntervalMax = originalShootMax;

        slowRoutine = null;
    }
    private void UpdateSpeedFromRemainingInvaders()
    {
        int totalInvaders = rows * columns;

        if (totalInvaders <= 0)
            return;

        float remainingPercent =
            invaders.Count / (float)totalInvaders;

        currentMoveDelay =
            Mathf.Lerp(
                minimumMoveDelay,
                startingMoveDelay,
                remainingPercent);
    }

    public void NotifyDiveStarted(Invader invader)
    {
        activeDivers++;
        activeDivers = Mathf.Clamp(activeDivers, 0, maxActiveDivers);
    }

    public void NotifyDiveEnded(Invader invader)
    {
        activeDivers--;
        activeDivers = Mathf.Max(0, activeDivers);
    }

    public void EnemyBulletDestroyed(EnemyBullet bullet)
    {
        activeEnemyBullets.Remove(bullet);
    }

    private void ClearWave()
    {
        foreach (Invader invader in invaders)
        {
            if (invader != null)
                Destroy(invader.gameObject);
        }

        invaders.Clear();
        activeDivers = 0;
    }

    private void ClearEnemyBullets()
    {
        foreach (EnemyBullet bullet in activeEnemyBullets)
        {
            if (bullet != null)
                Destroy(bullet.gameObject);
        }

        activeEnemyBullets.Clear();
    }
}