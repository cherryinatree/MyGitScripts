using System.Collections;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class SpaceInvadersBossProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float homingStrength = 2f;
    [SerializeField] private float lifetime = 8f;

    private RectTransform rect;
    private Image image;

    private RectTransform playArea;
    private ArcadeController player;

    private Vector2 velocity;
    private bool homing;

    private float timer;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    public void Initialize(
        RectTransform newPlayArea,
        ArcadeController newPlayer,
        Sprite sprite,
        Vector2 startPosition,
        Vector2 direction,
        float speed,
        bool shouldHome)
    {
        playArea = newPlayArea;
        player = newPlayer;
        homing = shouldHome;

        rect.anchoredPosition = startPosition;

        if (image != null)
            image.sprite = sprite;

        if (direction == Vector2.zero)
            direction = Vector2.down;

        velocity = direction.normalized * speed;

        timer = lifetime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (homing && player != null)
        {
            Vector2 desiredDirection =
                (player.Rect.anchoredPosition - rect.anchoredPosition)
                .normalized;

            Vector2 desiredVelocity =
                desiredDirection *
                velocity.magnitude;

            velocity =
                Vector2.Lerp(
                    velocity,
                    desiredVelocity,
                    Time.deltaTime * homingStrength);
        }

        rect.anchoredPosition +=
            velocity * Time.deltaTime;

        if (IsOutsidePlayArea())
            Destroy(gameObject);
    }

    private bool IsOutsidePlayArea()
    {
        if (playArea == null)
            return false;

        float halfWidth = playArea.rect.width * 0.5f + 120f;
        float halfHeight = playArea.rect.height * 0.5f + 120f;

        Vector2 pos = rect.anchoredPosition;

        return
            pos.x < -halfWidth ||
            pos.x > halfWidth ||
            pos.y < -halfHeight ||
            pos.y > halfHeight;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ArcadeController arcadePlayer =
            other.GetComponentInParent<ArcadeController>();

        if (arcadePlayer == null)
            return;

        arcadePlayer.Hit();

        Destroy(gameObject);
    }
}
