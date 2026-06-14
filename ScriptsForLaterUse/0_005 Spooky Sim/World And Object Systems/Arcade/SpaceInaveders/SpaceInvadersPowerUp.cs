using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SpaceInvadersPowerUp : MonoBehaviour
{
    [Header("Power Up")]
    [SerializeField] private SpaceInvadersPowerUpType powerUpType;
    [SerializeField] private float fallSpeed = 220f;
    [SerializeField] private int bonusTicketAmount = 10;

    [Header("Visual")]
    [SerializeField] private Image image;

    private RectTransform rect;
    private RectTransform playArea;

    public void Initialize(
        SpaceInvadersPowerUpType type,
        Sprite sprite,
        RectTransform newPlayArea)
    {
        powerUpType = type;
        playArea = newPlayArea;

        rect = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
            image.sprite = sprite;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();
    }

    private void Update()
    {
        rect.anchoredPosition +=
            Vector2.down *
            fallSpeed *
            Time.deltaTime;

        if (playArea != null &&
            rect.anchoredPosition.y < -playArea.rect.height * 0.5f - 100f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ArcadeController player =
            other.GetComponentInParent<ArcadeController>();

        if (player == null)
            return;

        player.ApplyPowerUp(
            powerUpType,
            bonusTicketAmount);

        Destroy(gameObject);
    }
}