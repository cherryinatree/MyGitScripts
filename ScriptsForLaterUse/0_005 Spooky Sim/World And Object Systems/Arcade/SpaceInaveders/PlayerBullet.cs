using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float speed = 900f;
    [SerializeField] private float topPadding = 80f;
    [SerializeField] private int damage = 1;

    private RectTransform rect;
    private RectTransform playArea;
    private ArcadeController owner;

    private bool destroyed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Initialize(ArcadeController owner, RectTransform playArea)
    {
        this.owner = owner;
        this.playArea = playArea;
    }

    private void Update()
    {
        rect.anchoredPosition +=
            Vector2.up *
            speed *
            Time.deltaTime;

        if (playArea != null)
        {
            float top =
                playArea.rect.height / 2f + topPadding;

            if (rect.anchoredPosition.y > top)
                DestroySelf();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SpaceInvadersBoss boss =
            other.GetComponentInParent<SpaceInvadersBoss>();

        if (boss != null)
        {
            boss.TakeDamage(damage);
            DestroySelf();
            return;
        }

        Invader invader =
            other.GetComponentInParent<Invader>();

        if (invader == null)
            return;

        invader.TakeDamage(damage);

        DestroySelf();
    }

    private void DestroySelf()
    {
        if (destroyed)
            return;

        destroyed = true;

        if (owner != null)
            owner.BulletDestroyed(this);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (owner != null)
            owner.BulletDestroyed(this);
    }
}