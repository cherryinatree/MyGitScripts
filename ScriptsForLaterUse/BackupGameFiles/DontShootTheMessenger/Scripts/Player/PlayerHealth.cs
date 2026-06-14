using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float regenRate = 5f;   // health per second
    public float regenDelay = 3f;  // delay after last damage before regen starts

    [Header("Invincibility Frames")]
    public float iFrameDuration = 1f;
    private bool isInvincible = false;

    [Header("Damage FX")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.2f;
    public AudioClip damageSound;

    [Header("Knockback (CharacterController)")]
    [Tooltip("Horizontal push magnitude applied when taking damage.")]
    public float knockbackForce = 5f;
    [Tooltip("Extra upward component added to the knockback direction.")]
    public float knockbackUpward = 0.5f;
    [Tooltip("How long the knockback impulse lasts (seconds).")]
    public float knockbackDuration = 0.2f;
    [Tooltip("Damping applied to knockback velocity over time (0 = none, higher = stronger damping).")]
    public float knockbackDamping = 6f;
    [Tooltip("Ignore new knockbacks while invincible (recommended).")]
    public bool ignoreKnockbackDuringIFrames = true;

    [Header("References")]
    public HUDController hud;
    public Renderer playerRenderer;     // for flash
    public AudioSource audioSource;
    private CharacterController characterController;

    private Coroutine regenCoroutine;
    private Coroutine knockbackCoroutine;

    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (hud == null) hud = FindObjectOfType<HUDController>();
        if (playerRenderer == null) playerRenderer = GetComponentInChildren<Renderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();

        UpdateHUD();
    }

    // ----------------------------------------------------------------------------
    // Legacy simple damage (no source). Keeps compatibility with existing callers.
    // ----------------------------------------------------------------------------
    public void TakeDamage(int amount)
    {
        if (IsDead || isInvincible) return;
        ApplyDamage(amount, hasSource: false, sourcePosition: Vector3.zero);
    }

    // ----------------------------------------------------------------------------
    // Directional damage with knockback
    // ----------------------------------------------------------------------------
    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (IsDead || isInvincible) return;
        ApplyDamage(amount, hasSource: true, sourcePosition: sourcePosition);
    }

    private void ApplyDamage(float amount, bool hasSource, Vector3 sourcePosition)
    {
        // reduce health
        currentHealth -= Mathf.Abs(amount);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHUD();

        // SFX/flash
        if (damageSound != null && audioSource != null) audioSource.PlayOneShot(damageSound);
        if (playerRenderer != null) StartCoroutine(FlashRoutine());

        // Knockback (optional if we have a source)
        if (hasSource && characterController != null)
        {
            if (!ignoreKnockbackDuringIFrames || !isInvincible)
                StartKnockback(sourcePosition);
        }

        // i-frames
        StartCoroutine(InvincibilityRoutine());

        // regen handling
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenRoutine());

        if (IsDead) Die();
    }

    // ----------------------------------------------------------------------------
    // HUD
    // ----------------------------------------------------------------------------
    private void UpdateHUD()
    {
        if (hud != null)
            hud.SetHealth(currentHealth / maxHealth);

        // Debug if you want: 
        // Debug.Log($"Health: {currentHealth}/{maxHealth}");
    }

    // ----------------------------------------------------------------------------
    // I-Frames
    // ----------------------------------------------------------------------------
    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(iFrameDuration);
        isInvincible = false;
    }

    // ----------------------------------------------------------------------------
    // Flash
    // ----------------------------------------------------------------------------
    private IEnumerator FlashRoutine()
    {
        Material mat = playerRenderer.material;
        Color originalColor = mat.color;
        mat.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        mat.color = originalColor;
    }

    // ----------------------------------------------------------------------------
    // Knockback (applied over time via CharacterController.Move)
    // ----------------------------------------------------------------------------
    private void StartKnockback(Vector3 sourcePosition)
    {
        if (knockbackCoroutine != null) StopCoroutine(knockbackCoroutine);
        knockbackCoroutine = StartCoroutine(KnockbackRoutine(sourcePosition));
    }

    private IEnumerator KnockbackRoutine(Vector3 sourcePosition)
    {
        float t = 0f;

        // initial impulse
        Vector3 dir = (transform.position - sourcePosition);
        dir.y = 0f; // horizontal base
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward; // fallback
        dir.Normalize();
        dir.y += knockbackUpward; // add lift

        // treat as "velocity" in m/s for Move
        Vector3 knockVel = dir.normalized * knockbackForce / Mathf.Max(0.01f, knockbackDuration);

        // optional: if you have your own gravity application elsewhere, omit/adjust this
        Vector3 extraGravity = Physics.gravity * 0.5f; // mild gravity influence during knockback

        while (t < knockbackDuration)
        {
            float dt = Time.deltaTime;
            t += dt;

            // exponential damping
            float damp = Mathf.Exp(-knockbackDamping * dt);
            knockVel *= damp;

            // move
            Vector3 frameMove = (knockVel + extraGravity) * dt;
            characterController.Move(frameMove);

            yield return null;
        }
    }

    // ----------------------------------------------------------------------------
    // Regeneration
    // ----------------------------------------------------------------------------
    private IEnumerator RegenRoutine()
    {
        // wait out delay
        yield return new WaitForSeconds(regenDelay);

        while (!IsDead && currentHealth < maxHealth)
        {
            currentHealth += regenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            UpdateHUD();
            yield return null;
        }
    }

    // ----------------------------------------------------------------------------
    // Death
    // ----------------------------------------------------------------------------
    private void Die()
    {
        Debug.Log("Player has died.");
        // TODO: disable controls, play death anim, game over, etc.
    }
}
