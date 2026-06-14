using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SpaceInvadersBossLaser : MonoBehaviour
{
    [Header("Laser")]
    [SerializeField] private Image image;
    [SerializeField] private Collider2D laserCollider;

    [Header("Visuals")]
    [SerializeField] private Color warningColor = new Color(1f, 1f, 0f, 0.45f);
    [SerializeField] private Color activeColor = new Color(1f, 0f, 0f, 0.85f);

    private RectTransform rect;
    private RectTransform playArea;
    private ArcadeController player;

    private float direction;
    private float warningTime;
    private float activeTime;
    private float sweepSpeed;

    private bool active;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (laserCollider == null)
            laserCollider = GetComponent<Collider2D>();
    }

    public void Initialize(
        RectTransform newPlayArea,
        ArcadeController newPlayer,
        Vector2 startPosition,
        float sweepDirection,
        float warningDuration,
        float activeDuration,
        float speed)
    {
        playArea = newPlayArea;
        player = newPlayer;
        direction = Mathf.Sign(sweepDirection);
        warningTime = warningDuration;
        activeTime = activeDuration;
        sweepSpeed = speed;

        rect.anchoredPosition = startPosition;

        rect.sizeDelta =
            new Vector2(
                48f,
                playArea.rect.height + 200f);

        if (image != null)
            image.color = warningColor;

        if (laserCollider != null)
            laserCollider.enabled = false;

        StartCoroutine(LaserRoutine());
    }

    private IEnumerator LaserRoutine()
    {
        yield return new WaitForSeconds(warningTime);

        active = true;

        if (image != null)
            image.color = activeColor;

        if (laserCollider != null)
            laserCollider.enabled = true;

        yield return new WaitForSeconds(activeTime);

        Destroy(gameObject);
    }

    private void Update()
    {
        if (!active)
            return;

        rect.anchoredPosition +=
            Vector2.right *
            direction *
            sweepSpeed *
            Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ArcadeController arcadePlayer =
            other.GetComponentInParent<ArcadeController>();

        if (arcadePlayer == null)
            return;

        arcadePlayer.Hit();
    }
}