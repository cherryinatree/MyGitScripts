using System.Collections.Generic;
using UnityEngine;

public class DiverRushManager : MonoBehaviour
{
    private class RushDiver
    {
        public Invader invader;

        public Vector2 p0;
        public Vector2 p1;
        public Vector2 p2;
        public Vector2 p3;

        public float t;
        public float approximateDistance;
    }

    [Header("Runtime")]
    [SerializeField] private Transform diverParent;

    private readonly List<RushDiver> activeDivers = new();

    private RectTransform playArea;
    private ArcadeController player;
    private DiverRushData data;

    private bool gameActive;

    private int spawnedCount;
    private float spawnTimer;

    public void BeginGame(
        RectTransform newPlayArea,
        ArcadeController newPlayer,
        DiverRushData rushData)
    {
        playArea = newPlayArea;
        player = newPlayer;
        data = rushData;

        if (diverParent == null)
            diverParent = playArea;

        if (data == null)
        {
            Debug.LogError("DiverRushManager needs DiverRushData.");
            return;
        }

        if (data.diverPrefab == null)
        {
            Debug.LogError("DiverRushData needs a diver prefab.");
            return;
        }

        ClearLevel();

        gameActive = true;
        spawnedCount = 0;
        spawnTimer = 0f;
    }

    public void EndGame()
    {
        gameActive = false;
        ClearLevel();
    }

    private void Update()
    {
        if (!gameActive)
            return;

        UpdateSpawning();
        UpdateDivers();
        CleanupDeadDivers();
        CheckLevelComplete();
    }

    private void UpdateSpawning()
    {
        if (spawnedCount >= data.totalDiversToSpawn)
            return;

        if (activeDivers.Count >= data.maxDiversOnScreen)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
            return;

        spawnTimer = data.spawnInterval;

        SpawnDiver();
    }

    private void SpawnDiver()
    {
        spawnedCount++;

        Invader invader =
            Instantiate(data.diverPrefab, diverParent);

        Vector2 start = GetRandomSpawnPosition();
        Vector2 target = GetExitPosition(start);

        invader.Rect.anchoredPosition = start;

        invader.Initialize(
            null,
            InvaderType.Diver,
            data.diverSprite,
            data.diverHealthSprites,
            data.diverScoreValue,
            data.diverHealth);

        RushDiver rushDiver = CreatePath(invader, start, target);

        activeDivers.Add(rushDiver);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float halfWidth = playArea.rect.width * 0.5f;

        float x =
            Random.Range(
                -halfWidth - data.spawnSidePadding,
                halfWidth + data.spawnSidePadding);

        float y =
            playArea.rect.height * 0.5f +
            data.spawnTopPadding;

        return new Vector2(x, y);
    }

    private Vector2 GetExitPosition(Vector2 start)
    {
        float halfWidth = playArea.rect.width * 0.5f;

        float x;

        if (player != null)
        {
            x = player.Rect.anchoredPosition.x;

            x += Random.Range(-180f, 180f);
        }
        else
        {
            x = Random.Range(-halfWidth, halfWidth);
        }

        x = Mathf.Clamp(
            x,
            -halfWidth - data.spawnSidePadding,
            halfWidth + data.spawnSidePadding);

        float y =
            -playArea.rect.height * 0.5f -
            data.exitBottomPadding;

        return new Vector2(x, y);
    }

    private RushDiver CreatePath(
        Invader invader,
        Vector2 start,
        Vector2 end)
    {
        float side =
            Random.value > 0.5f
                ? 1f
                : -1f;

        Vector2 mid =
            player != null
                ? player.Rect.anchoredPosition
                : Vector2.zero;

        Vector2 p1 =
            start +
            new Vector2(
                side * data.curveWidth,
                -data.curveHeight * 0.25f);

        Vector2 p2 =
            mid +
            new Vector2(
                -side * data.curveWidth,
                data.curveHeight);

        RushDiver rushDiver = new RushDiver
        {
            invader = invader,
            p0 = start,
            p1 = p1,
            p2 = p2,
            p3 = end,
            t = 0f
        };

        rushDiver.approximateDistance =
            EstimateBezierLength(
                rushDiver.p0,
                rushDiver.p1,
                rushDiver.p2,
                rushDiver.p3,
                24);

        rushDiver.approximateDistance =
            Mathf.Max(1f, rushDiver.approximateDistance);

        return rushDiver;
    }

    private void UpdateDivers()
    {
        for (int i = activeDivers.Count - 1; i >= 0; i--)
        {
            RushDiver diver = activeDivers[i];

            if (diver.invader == null)
            {
                activeDivers.RemoveAt(i);
                continue;
            }

            float speedMultiplier =
                data.diveSpeedCurve != null
                    ? Mathf.Max(
                        0.05f,
                        data.diveSpeedCurve.Evaluate(diver.t))
                    : 1f;

            diver.t +=
                (data.baseDiveSpeed *
                 speedMultiplier *
                 Time.deltaTime) /
                diver.approximateDistance;

            diver.t = Mathf.Clamp01(diver.t);

            Vector2 position =
                CubicBezier(
                    diver.p0,
                    diver.p1,
                    diver.p2,
                    diver.p3,
                    diver.t);

            diver.invader.Rect.anchoredPosition = position;

            if (player != null &&
                player.CanBeHit &&
                RectsOverlap(diver.invader.Rect, player.Rect))
            {
                player.Hit();

                Destroy(diver.invader.gameObject);
                activeDivers.RemoveAt(i);
                continue;
            }

            if (diver.t >= 1f)
            {
                // Important: missed divers are gone and award no points.
                Destroy(diver.invader.gameObject);
                activeDivers.RemoveAt(i);
            }
        }
    }

    private void CleanupDeadDivers()
    {
        activeDivers.RemoveAll(diver => diver.invader == null);
    }

    private void CheckLevelComplete()
    {
        if (spawnedCount < data.totalDiversToSpawn)
            return;

        if (activeDivers.Count > 0)
            return;

        gameActive = false;

        if (SpaceInvadersManager.Instance != null)
        {
            SpaceInvadersManager.Instance.AddScore(
                data.levelCompleteBonus);

            SpaceInvadersManager.Instance.WinWave();
        }
    }

    private void ClearLevel()
    {
        foreach (RushDiver diver in activeDivers)
        {
            if (diver.invader != null)
                Destroy(diver.invader.gameObject);
        }

        activeDivers.Clear();
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

    private Vector2 CubicBezier(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        float t)
    {
        float u = 1f - t;

        return
            u * u * u * p0 +
            3f * u * u * t * p1 +
            3f * u * t * t * p2 +
            t * t * t * p3;
    }

    private float EstimateBezierLength(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        int samples)
    {
        float distance = 0f;
        Vector2 previous = p0;

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector2 current =
                CubicBezier(p0, p1, p2, p3, t);

            distance += Vector2.Distance(previous, current);
            previous = current;
        }

        return distance;
    }
}