using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float speed = 400f;
    [SerializeField] private float bottomPadding = 80f;

    private RectTransform rect;
    private RectTransform playArea;
    private InvaderManager manager;

    private bool destroyed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Initialize(RectTransform playArea, InvaderManager manager)
    {
        this.playArea = playArea;
        this.manager = manager;
    }

    private void Update()
    {
        rect.anchoredPosition +=
            Vector2.down *
            speed *
            Time.deltaTime;

        if (playArea != null)
        {
            float bottom =
                -playArea.rect.height / 2f - bottomPadding;

            if (rect.anchoredPosition.y < bottom)
                DestroySelf();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ArcadeController player =
            other.GetComponentInParent<ArcadeController>();

        if (player == null)
            return;

        player.Hit();

        DestroySelf();
    }

    private void DestroySelf()
    {
        if (destroyed)
            return;

        destroyed = true;

        if (manager != null)
            manager.EnemyBulletDestroyed(this);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (manager != null)
            manager.EnemyBulletDestroyed(this);
    }
}