using TMPro;
using UnityEngine;

public class SpaceInvadersManager : MonoBehaviour, IArcadePlayable
{
    public static SpaceInvadersManager Instance;

    [Header("Game")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int pointsPerTicket = 25;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Player Spawn")]
    [SerializeField] private ArcadeController playerPrefab;
    [SerializeField] private RectTransform gamePanel;
    [SerializeField] private RectTransform playerSpawnPoint;
    [SerializeField] private Vector2 fallbackPlayerSpawnPosition = new Vector2(0, -250);

    [SerializeField] private DiverRushManager diverRushManager;
    [SerializeField] private SpaceInvadersBossManager bossManager;

    [Header("Invaders")]
    [SerializeField] private InvaderManager invaderManager;

    private ArcadeCabinet currentCabinet;
    private ArcadeController playerInstance;

    private int score;
    private int lives;
    private bool gameActive;
    private bool ticketsPaid;

    [Header("Levels")]
    [SerializeField] private SpaceInvadersLevelData[] levels;
    [SerializeField] private bool loopLevels = true;

    [Header("Power Up Bonus")]
    [SerializeField] private int bonusTicketsPending;

    [Header("Audio")]
    [SerializeField] private SpaceInvadersAudio audioController;

    private int currentLevelIndex;

    private void Awake()
    {
        Instance = this;
    }

    public void BeginFromCabinet(ArcadeCabinet cabinet)
    {
        currentCabinet = cabinet;
        StartGame();
    }

    public void EndFromCabinet()
    {
        EndGame(false);
    }

    public void StartGame()
    {
        currentLevelIndex = 0;
        bonusTicketsPending = 0;
        gameActive = true;
        ticketsPaid = false;

        score = 0;
        lives = startingLives;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        SpawnPlayer();

        StartCurrentLevel();

        UpdateUI();
    }

    private void StartCurrentLevel()
    {
        SpaceInvadersLevelData level = GetCurrentLevel();

        if (level == null)
            return;

        if (audioController != null)
            audioController.PlayMusic(level.levelMusic);

        DisableAllLevelManagers();

        switch (level.levelType)
        {
            case SpaceInvadersLevelType.Normal:
                invaderManager.BeginGame(
                    gamePanel,
                    playerInstance,
                    level);
                break;

            case SpaceInvadersLevelType.DiverRush:
                diverRushManager.BeginGame(
                    gamePanel,
                    playerInstance,
                    level.diverRushData);
                break;

            case SpaceInvadersLevelType.Boss:
                bossManager.BeginGame(
                    gamePanel,
                    playerInstance,
                    level.bossData);
                break;
        }
    }

    private void DisableAllLevelManagers()
    {
        if (invaderManager != null)
            invaderManager.EndGame();

        if (diverRushManager != null)
            diverRushManager.EndGame();

        if (bossManager != null)
            bossManager.EndGame();
    }
    private SpaceInvadersLevelData GetCurrentLevel()
    {
        if (levels == null || levels.Length == 0)
            return null;

        int index =
            Mathf.Clamp(
                currentLevelIndex,
                0,
                levels.Length - 1);

        return levels[index];
    }
    private void SpawnPlayer()
    {
        if (playerPrefab == null || gamePanel == null)
        {
            Debug.LogError("SpaceInvadersManager is missing Player Prefab or Game Panel.");
            return;
        }

        if (playerInstance != null)
            Destroy(playerInstance.gameObject);

        playerInstance = Instantiate(playerPrefab, gamePanel);

        Vector2 spawnPosition =
            playerSpawnPoint != null
                ? playerSpawnPoint.anchoredPosition
                : fallbackPlayerSpawnPosition;

        playerInstance.Initialize(gamePanel, spawnPosition);
    }

    public void AddScore(int amount)
    {
        if (!gameActive)
            return;

        score += amount;
        UpdateUI();
    }

    public void PlayerWasHit(ArcadeController player)
    {
        if (!gameActive || player == null)
            return;
        PlayPlayerHitSound();
        lives--;
        UpdateUI();

        if (lives <= 0)
        {
            player.ExplodeAndDie();
            EndGame(true);
            return;
        }

        Vector2 spawnPosition =
            playerSpawnPoint != null
                ? playerSpawnPoint.anchoredPosition
                : fallbackPlayerSpawnPosition;

        player.ExplodeAndRespawn(spawnPosition);
    }

    public void WinWave()
    {
        if (!gameActive)
            return;

        SpaceInvadersLevelData level = GetCurrentLevel();

        if (level != null)
            AddScore(level.levelCompleteBonus);

        if (audioController != null)
            audioController.LevelComplete();

        currentLevelIndex++;

        bool hasMoreLevels =
            levels != null &&
            currentLevelIndex < levels.Length;

        if (!hasMoreLevels)
        {
            if (loopLevels)
                currentLevelIndex = 0;
            else
            {
                GameOver();
                return;
            }
        }

        StartCurrentLevel();
    }
    public void AddBonusTickets(int amount)
    {
        bonusTicketsPending += amount;
    }

    public void TemporarilySlowEnemies()
    {
        if (invaderManager != null)
            invaderManager.TemporarilySlowEnemies();
    }

    public void PlayShootSound()
    {
        if (audioController != null)
            audioController.Shoot();
    }

    public void PlayEnemyKilledSound()
    {
        if (audioController != null)
            audioController.EnemyKilled();
    }

    public void PlayPlayerHitSound()
    {
        if (audioController != null)
            audioController.PlayerHit();
    }

    public void PlayPowerUpSound()
    {
        if (audioController != null)
            audioController.PowerUp();
    }

    public void PlayEnemyStepSound()
    {
        if (audioController != null)
            audioController.EnemyStep();
    }

    public void GameOver()
    {
        EndGame(true);
    }

    private void EndGame(bool payTickets)
    {
        if (!gameActive)
            return;
        if (audioController != null)
        {
            audioController.GameOver();
            audioController.StopMusic();
        }
        gameActive = false;

        if (invaderManager != null)
            invaderManager.EndGame();

        if (currentCabinet != null)
            currentCabinet.ShowGameOverScreen();

        if (payTickets)
            PayTicketsOnce();
    }

    private void PayTicketsOnce()
    {
        if (ticketsPaid)
            return;

        ticketsPaid = true;

        int ticketsEarned =
    Mathf.FloorToInt(score / (float)pointsPerTicket) +
    bonusTicketsPending;

        Debug.Log($"Tickets earned: {ticketsEarned}");

        if (currentCabinet != null)
            currentCabinet.DispensePlayerTickets(ticketsEarned);
        else
            Debug.LogWarning("No ArcadeCabinet connected, so tickets could not dispense.");
    }

    private void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }
}