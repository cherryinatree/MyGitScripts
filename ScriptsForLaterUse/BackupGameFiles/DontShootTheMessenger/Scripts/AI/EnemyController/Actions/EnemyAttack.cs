using UnityEngine;

/// <summary>
/// Lightweight damage helper used by AttackAction. Swap for your own system if needed.
/// </summary>
public class EnemyAttack : MonoBehaviour
{
    public int damage = 20;
    public float knockback = 3f;

    public void ApplyTo(GameObject target)
    {
        if (!target) return;

        // 1) Interface pattern
        var dmg = target.GetComponent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            ApplyKnockback(target);
            return;
        }

        // 2) Common "Health" component pattern
        var health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            // Expect Health to have TakeDamage(int)
            var m = health.GetType().GetMethod("TakeDamage", new[] { typeof(int) });
            if (m != null) m.Invoke(health, new object[] { damage });
            ApplyKnockback(target);
            return;
        }

        // 3) SendMessage fallback (opt-in)
        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        ApplyKnockback(target);
    }

    private void ApplyKnockback(GameObject target)
    {
        var rb = target.GetComponent<Rigidbody>();
        if (!rb) return;

        Vector3 dir = (target.transform.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        rb.AddForce(dir.normalized * knockback, ForceMode.VelocityChange);
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}
