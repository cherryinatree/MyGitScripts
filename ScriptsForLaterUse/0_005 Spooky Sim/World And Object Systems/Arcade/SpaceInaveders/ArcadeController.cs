using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ArcadeController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float edgePadding = 40f;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference fireAction;

    [Header("Combat")]
    [SerializeField] private PlayerBullet bulletPrefab;
    [SerializeField] private Transform bulletParent;
    [SerializeField] private float bulletSpawnOffsetY = 40f;

    [Header("Death / Respawn")]
    [SerializeField] private float respawnDelay = 0.75f;
    [SerializeField] private float invulnerableDuration = 1.5f;
    [SerializeField] private float blinkInterval = 0.12f;
    [SerializeField] private GameObject explosionPrefab;

    private RectTransform playArea;
    private RectTransform playerRect;
    private Image image;
    private Collider2D playerCollider;

    private PlayerBullet activeBullet;

    private bool initialized;
    private bool controlsLocked;
    private bool invulnerable;
    private bool dead;

    public RectTransform Rect => playerRect;
    public bool CanBeHit =>
    initialized &&
    !dead &&
    !invulnerable &&
    !shieldActive;

    [Header("Power Ups")]
    [SerializeField] private float rapidFireDuration = 8f;
    [SerializeField] private float rapidFireCooldown = 0.18f;
    [SerializeField] private float normalFireCooldown = 0.45f;
    [SerializeField] private float multiShotDuration = 8f;
    [SerializeField] private float shieldDuration = 8f;

    private bool rapidFireActive;
    private bool multiShotActive;
    private bool shieldActive;

    private float fireCooldownTimer;
    private Coroutine rapidFireRoutine;
    private Coroutine multiShotRoutine;
    private Coroutine shieldRoutine;
    private void Awake()
    {
        playerRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        playerCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }

    public void Initialize(RectTransform playArea, Vector2 spawnPosition)
    {
        this.playArea = playArea;

        if (bulletParent == null)
            bulletParent = playArea;

        playerRect.anchoredPosition = spawnPosition;

        initialized = true;
        controlsLocked = false;
        invulnerable = false;
        dead = false;

        if (image != null)
            image.enabled = true;

        if (playerCollider != null)
            playerCollider.enabled = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (controlsLocked)
            return;
        if (fireCooldownTimer > 0f)
            fireCooldownTimer -= Time.deltaTime;
        UpdateMovement();
    }

    private void UpdateMovement()
    {
        if (moveAction == null || moveAction.action == null)
            return;

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float move = moveInput.x;

        Vector2 pos = playerRect.anchoredPosition;

        pos.x += move * moveSpeed * Time.deltaTime;

        float limit = playArea.rect.width / 2f - edgePadding;

        pos.x = Mathf.Clamp(pos.x, -limit, limit);

        playerRect.anchoredPosition = pos;
    }

    private void Fire(InputAction.CallbackContext ctx)
    {
        if (!initialized)
            return;

        if (controlsLocked || dead)
            return;

        if (fireCooldownTimer > 0f)
            return;

        fireCooldownTimer =
            rapidFireActive
                ? rapidFireCooldown
                : normalFireCooldown;

        if (multiShotActive)
        {
            FireBullet(Vector2.zero);
            FireBullet(new Vector2(-22f, 0f));
            FireBullet(new Vector2(22f, 0f));
        }
        else
        {
            FireBullet(Vector2.zero);
        }

        SpaceInvadersManager.Instance.PlayShootSound();
    

    Transform parent = bulletParent != null ? bulletParent : playArea;

        activeBullet = Instantiate(bulletPrefab, parent);

        RectTransform bulletRect = activeBullet.GetComponent<RectTransform>();

        bulletRect.anchoredPosition =
            playerRect.anchoredPosition + Vector2.up * bulletSpawnOffsetY;

        activeBullet.Initialize(this, playArea);
    }
    private void FireBullet(Vector2 offset)
    {
        if (!multiShotActive && activeBullet != null)
            return;

        if (bulletPrefab == null)
        {
            Debug.LogWarning("ArcadeController has no PlayerBullet prefab assigned.");
            return;
        }

        Transform parent =
            bulletParent != null
                ? bulletParent
                : playArea;

        PlayerBullet bullet =
            Instantiate(bulletPrefab, parent);

        RectTransform bulletRect =
            bullet.GetComponent<RectTransform>();

        bulletRect.anchoredPosition =
            playerRect.anchoredPosition +
            Vector2.up * bulletSpawnOffsetY +
            offset;

        bullet.Initialize(this, playArea);

        if (!multiShotActive)
            activeBullet = bullet;
    }

    public void BulletDestroyed(PlayerBullet bullet)
    {
        if (activeBullet == bullet)
            activeBullet = null;
    }

    public void Hit()
    {
        if (!CanBeHit)
            return;

        SpaceInvadersManager.Instance.PlayerWasHit(this);
    }
    public void ApplyPowerUp(
    SpaceInvadersPowerUpType type,
    int bonusTicketAmount)
    {
        SpaceInvadersManager.Instance.PlayPowerUpSound();

        switch (type)
        {
            case SpaceInvadersPowerUpType.RapidFire:
                RestartPowerUpRoutine(
                    ref rapidFireRoutine,
                    RapidFireRoutine());
                break;

            case SpaceInvadersPowerUpType.MultiShot:
                RestartPowerUpRoutine(
                    ref multiShotRoutine,
                    MultiShotRoutine());
                break;

            case SpaceInvadersPowerUpType.Shield:
                RestartPowerUpRoutine(
                    ref shieldRoutine,
                    ShieldRoutine());
                break;

            case SpaceInvadersPowerUpType.SlowEnemies:
                SpaceInvadersManager.Instance.TemporarilySlowEnemies();
                break;

            case SpaceInvadersPowerUpType.BonusTickets:
                SpaceInvadersManager.Instance.AddBonusTickets(
                    bonusTicketAmount);
                break;
        }
    }
    private void RestartPowerUpRoutine(
    ref Coroutine routine,
    IEnumerator newRoutine)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(newRoutine);
    }

    private IEnumerator RapidFireRoutine()
    {
        rapidFireActive = true;

        yield return new WaitForSeconds(rapidFireDuration);

        rapidFireActive = false;
        rapidFireRoutine = null;
    }

    private IEnumerator MultiShotRoutine()
    {
        multiShotActive = true;

        yield return new WaitForSeconds(multiShotDuration);

        multiShotActive = false;
        multiShotRoutine = null;
    }

    private IEnumerator ShieldRoutine()
    {
        shieldActive = true;

        // Optional: change ship color while shielded.
        if (image != null)
            image.color = Color.cyan;

        yield return new WaitForSeconds(shieldDuration);

        if (image != null)
            image.color = Color.white;

        shieldActive = false;
        shieldRoutine = null;
    }
    public void ExplodeAndRespawn(Vector2 respawnPosition)
    {
        StartCoroutine(DeathAndRespawnRoutine(respawnPosition));
    }

    public void ExplodeAndDie()
    {
        StartCoroutine(PermanentDeathRoutine());
    }

    private IEnumerator DeathAndRespawnRoutine(Vector2 respawnPosition)
    {
        dead = true;
        controlsLocked = true;
        invulnerable = true;

        SpawnExplosion();

        if (image != null)
            image.enabled = false;

        if (playerCollider != null)
            playerCollider.enabled = false;

        if (activeBullet != null)
            Destroy(activeBullet.gameObject);

        activeBullet = null;

        yield return new WaitForSeconds(respawnDelay);

        playerRect.anchoredPosition = respawnPosition;

        if (image != null)
            image.enabled = true;

        if (playerCollider != null)
            playerCollider.enabled = true;

        dead = false;
        controlsLocked = false;

        float timer = 0f;

        while (timer < invulnerableDuration)
        {
            if (image != null)
                image.enabled = !image.enabled;

            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval;
        }

        if (image != null)
            image.enabled = true;

        invulnerable = false;
    }

    private IEnumerator PermanentDeathRoutine()
    {
        dead = true;
        controlsLocked = true;
        invulnerable = true;

        SpawnExplosion();

        if (image != null)
            image.enabled = false;

        if (playerCollider != null)
            playerCollider.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        Destroy(gameObject);
    }

    private void SpawnExplosion()
    {
        if (explosionPrefab == null)
            return;

        GameObject explosion = Instantiate(explosionPrefab, playArea);
        RectTransform explosionRect = explosion.GetComponent<RectTransform>();

        if (explosionRect != null)
            explosionRect.anchoredPosition = playerRect.anchoredPosition;

        Destroy(explosion, 2f);
    }

    private void EnableInput()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Enable();
            fireAction.action.performed += Fire;
        }
    }

    private void DisableInput()
    {
        if (fireAction != null && fireAction.action != null)
            fireAction.action.performed -= Fire;

        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();

        if (fireAction != null && fireAction.action != null)
            fireAction.action.Disable();
    }
}