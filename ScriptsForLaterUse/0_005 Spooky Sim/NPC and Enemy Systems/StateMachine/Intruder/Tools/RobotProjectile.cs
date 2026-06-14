using UnityEngine;

[DisallowMultipleComponent]
public class RobotProjectile : MonoBehaviour
{
    private GameObject _owner;
    private float _damage;
    private float _speed;
    private Vector3 _dir;

    [SerializeField] private Rigidbody rb;
    [SerializeField] private float lifeSeconds = 6f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    public void Init(GameObject owner, float damage, float speed, Vector3 dir)
    {
        _owner = owner;
        _damage = damage;
        _speed = speed;
        _dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;

        if (rb != null)
        {
            rb.linearVelocity = _dir * _speed;
        }

        Destroy(gameObject, lifeSeconds);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        Debug.Log($"Projectile hit {other.name}");
        // Don't hit the shooter
        if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;

        var hp = other.GetComponentInParent<Health>();
        if (hp != null && !hp.IsDead)
        {
            hp.TakeDamage(_damage, _owner);

            // Optional hit reaction (safe if missing)
            var react = other.GetComponentInParent<IntruderHitReact>();
            if (react != null) react.PlayHit();
        }

        Destroy(gameObject);
    }
}
