using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum InvaderType
{
    Normal,
    Shooter,
    Diver,
    Tank
}

[RequireComponent(typeof(RectTransform))]
public class Invader : MonoBehaviour
{
    [Header("Invader")]
    [SerializeField] private InvaderType invaderType = InvaderType.Normal;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private int maxHealth = 1;

    [Header("Diving")]
    [SerializeField] private float diveBaseSpeed = 420f;
    [SerializeField] private float returnBaseSpeed = 360f;
    [SerializeField] private float loopWidth = 180f;
    [SerializeField] private float loopHeight = 140f;
    [SerializeField] private float diveOvershootDistance = 130f;

    [Header("Dive Speed Curves")]
    [Tooltip("Controls speed while diving. Higher values make the invader move faster at that part of the path.")]
    [SerializeField]
    private AnimationCurve diveSpeedCurve =
        new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.35f, 1.35f),
            new Keyframe(0.75f, 1.1f),
            new Keyframe(1f, 0.8f));

    [Tooltip("Controls speed while returning to formation.")]
    [SerializeField]
    private AnimationCurve returnSpeedCurve =
        new AnimationCurve(
            new Keyframe(0f, 1.15f),
            new Keyframe(0.5f, 0.85f),
            new Keyframe(1f, 1.05f));

    private RectTransform rect;
    private Image image;

    private InvaderManager manager;
    private Coroutine diveRoutine;

    private Sprite defaultSprite;
    private Sprite[] spritesByRemainingHealth;

    private int currentHealth;
    private Vector2 homePosition;

    private bool diving;
    private bool diveReportedActive;

    public RectTransform Rect => rect;
    public InvaderType Type => invaderType;
    public bool IsDiving => diving;

    public bool CanDive =>
        invaderType == InvaderType.Diver &&
        !diving &&
        gameObject.activeInHierarchy;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Initialize(
        InvaderManager manager,
        InvaderType type,
        Sprite sprite,
        Sprite[] healthSprites,
        int score,
        int health)
    {
        this.manager = manager;

        invaderType = type;
        scoreValue = score;

        defaultSprite = sprite;
        spritesByRemainingHealth = healthSprites;

        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;

        homePosition = rect.anchoredPosition;

        RefreshSprite();
    }

    public void SetHomePosition(Vector2 position)
    {
        homePosition = position;

        if (!diving)
            rect.anchoredPosition = homePosition;
    }

    public void OffsetHomePosition(Vector2 offset)
    {
        homePosition += offset;

        if (!diving)
            rect.anchoredPosition += offset;
    }

    public void TakeDamage(int damage = 1)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Kill(true);
            return;
        }

        RefreshSprite();
    }

    private void RefreshSprite()
    {
        if (image == null)
            return;

        Sprite spriteToUse = defaultSprite;

        if (spritesByRemainingHealth != null &&
            spritesByRemainingHealth.Length >= currentHealth &&
            currentHealth > 0)
        {
            Sprite healthSprite =
                spritesByRemainingHealth[currentHealth - 1];

            if (healthSprite != null)
                spriteToUse = healthSprite;
        }

        image.sprite = spriteToUse;
    }

    public void Kill(bool awardScore)
    {
        ReportDiveEndedIfNeeded();

        if (awardScore && SpaceInvadersManager.Instance != null)
            SpaceInvadersManager.Instance.AddScore(scoreValue);

        if (manager != null)
            manager.InvaderKilled(this);

        Destroy(gameObject);
    }

    public void StartDive(ArcadeController player)
    {
        if (!CanDive)
            return;

        if (player == null)
            return;

        diveRoutine = StartCoroutine(DiveRoutine(player));
    }

    private IEnumerator DiveRoutine(ArcadeController player)
    {
        diving = true;
        diveReportedActive = true;

        if (manager != null)
            manager.NotifyDiveStarted(this);

        Vector2 start = rect.anchoredPosition;

        float side =
            player.Rect.anchoredPosition.x >= start.x
                ? 1f
                : -1f;

        Vector2 playerPositionAtStart =
            player.Rect.anchoredPosition;

        Vector2 diveTarget =
            playerPositionAtStart +
            Vector2.down * diveOvershootDistance;

        Vector2 diveControlA =
            start +
            new Vector2(side * loopWidth, -loopHeight * 0.25f);

        Vector2 diveControlB =
            playerPositionAtStart +
            new Vector2(-side * loopWidth * 0.7f, loopHeight);

        yield return MoveAlongBezier(
            start,
            diveControlA,
            diveControlB,
            diveTarget,
            diveBaseSpeed,
            diveSpeedCurve,
            player,
            true);

        if (!gameObject.activeInHierarchy)
            yield break;

        Vector2 returnStart = rect.anchoredPosition;

        Vector2 returnControlA =
            returnStart +
            new Vector2(-side * loopWidth, loopHeight * 0.75f);

        Vector2 returnControlB =
            homePosition +
            new Vector2(side * loopWidth * 0.6f, -loopHeight * 0.45f);

        yield return MoveAlongBezier(
            returnStart,
            returnControlA,
            returnControlB,
            homePosition,
            returnBaseSpeed,
            returnSpeedCurve,
            player,
            false);

        rect.anchoredPosition = homePosition;

        diving = false;
        diveRoutine = null;

        ReportDiveEndedIfNeeded();
    }

    private IEnumerator MoveAlongBezier(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        float baseSpeed,
        AnimationCurve speedCurve,
        ArcadeController player,
        bool canHitPlayer)
    {
        float approximateDistance =
            EstimateBezierLength(p0, p1, p2, p3, 20);

        approximateDistance =
            Mathf.Max(approximateDistance, 1f);

        float t = 0f;

        while (t < 1f)
        {
            float speedMultiplier =
                speedCurve != null
                    ? Mathf.Max(0.05f, speedCurve.Evaluate(t))
                    : 1f;

            t +=
                (baseSpeed * speedMultiplier * Time.deltaTime) /
                approximateDistance;

            t = Mathf.Clamp01(t);

            rect.anchoredPosition =
                CubicBezier(p0, p1, p2, p3, t);

            if (canHitPlayer &&
                player != null &&
                manager != null &&
                player.CanBeHit &&
                manager.RectsOverlap(rect, player.Rect))
            {
                HitPlayer(player);
                yield break;
            }

            yield return null;
        }
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
            Vector2 current = CubicBezier(p0, p1, p2, p3, t);

            distance += Vector2.Distance(previous, current);
            previous = current;
        }

        return distance;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ArcadeController player =
            other.GetComponentInParent<ArcadeController>();

        if (player != null)
            HitPlayer(player);
    }

    private void HitPlayer(ArcadeController player)
    {
        if (player == null)
            return;

        if (!player.CanBeHit)
            return;

        player.Hit();

        if (invaderType == InvaderType.Diver && diving)
            Kill(false);
    }

    private void ReportDiveEndedIfNeeded()
    {
        if (!diveReportedActive)
            return;

        diveReportedActive = false;
        diving = false;

        if (manager != null)
            manager.NotifyDiveEnded(this);
    }

    private void OnDestroy()
    {
        ReportDiveEndedIfNeeded();
    }
}