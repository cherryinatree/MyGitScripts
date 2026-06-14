using UnityEngine;

public class SpaceInvadersBossManager : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private SpaceInvadersBossData defaultBossData;
    [SerializeField] private RectTransform bossSpawnPoint;
    [SerializeField] private Vector2 fallbackBossSpawnPosition = new Vector2(0f, 220f);

    [Header("Parents")]
    [SerializeField] private Transform bossParent;
    [SerializeField] private Transform projectileParent;
    [SerializeField] private Transform effectParent;

    private RectTransform playArea;
    private ArcadeController player;
    private SpaceInvadersBoss activeBoss;
    private SpaceInvadersBossData activeBossData;

    private bool gameActive;

    public Transform ProjectileParent =>
        projectileParent != null
            ? projectileParent
            : playArea;

    public Transform EffectParent =>
        effectParent != null
            ? effectParent
            : playArea;

    public void BeginGame(
        RectTransform newPlayArea,
        ArcadeController newPlayer,
        SpaceInvadersBossData bossData)
    {
        playArea = newPlayArea;
        player = newPlayer;

        activeBossData =
            bossData != null
                ? bossData
                : defaultBossData;

        if (activeBossData == null)
        {
            Debug.LogError("SpaceInvadersBossManager has no boss data.");
            return;
        }

        if (activeBossData.bossPrefab == null)
        {
            Debug.LogError("Boss data has no boss prefab.");
            return;
        }

        if (bossParent == null)
            bossParent = playArea;

        if (projectileParent == null)
            projectileParent = playArea;

        if (effectParent == null)
            effectParent = playArea;

        ClearBossLevel();

        gameActive = true;

        SpawnBoss();
    }

    public void EndGame()
    {
        gameActive = false;
        ClearBossLevel();
    }

    private void SpawnBoss()
    {
        Vector2 spawnPosition =
            bossSpawnPoint != null
                ? bossSpawnPoint.anchoredPosition
                : fallbackBossSpawnPosition;

        activeBoss =
            Instantiate(
                activeBossData.bossPrefab,
                bossParent);

        activeBoss.Initialize(
            this,
            activeBossData,
            playArea,
            player,
            spawnPosition);
    }

    public void BossKilled(
        SpaceInvadersBoss boss,
        int scoreValue)
    {
        if (!gameActive)
            return;

        if (boss != activeBoss)
            return;

        gameActive = false;

        if (SpaceInvadersManager.Instance != null)
        {
            SpaceInvadersManager.Instance.AddScore(scoreValue);
            SpaceInvadersManager.Instance.WinWave();
        }
    }

    private void ClearBossLevel()
    {
        if (activeBoss != null)
            Destroy(activeBoss.gameObject);

        activeBoss = null;

        ClearChildren(projectileParent);
        ClearChildren(effectParent);
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}