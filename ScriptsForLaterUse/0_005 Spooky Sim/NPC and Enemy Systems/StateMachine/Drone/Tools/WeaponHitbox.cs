using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private Collider hitCollider;

    private readonly HashSet<Health> _hitThisSwing = new();
    private GameObject _owner;
    private float _damage;
    private bool _active;

    private void Awake()
    {
        if (hitCollider == null) hitCollider = GetComponent<Collider>();
        if (hitCollider != null) hitCollider.enabled = false;
    }

    public void BeginSwing(GameObject owner, float damage)
    {
        _owner = owner;
        _damage = damage;
        _hitThisSwing.Clear();
        _active = true;

        if (hitCollider != null) hitCollider.enabled = true;
    }

    public void EndSwing()
    {
        _active = false;
        if (hitCollider != null) hitCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        if (_owner != null && other.transform.IsChildOf(_owner.transform)) return; // don't hit self

        var hp = other.GetComponentInParent<Health>();
        if (hp == null || hp.IsDead) return;

        // Only hit each target once per swing
        if (_hitThisSwing.Contains(hp)) return;
        _hitThisSwing.Add(hp);

        hp.TakeDamage(_damage, _owner);

        // Optional: trigger hit reaction if intruder has animator helper
        var react = other.GetComponentInParent<IntruderHitReact>();
        if (react != null) react.PlayHit();
    }
}
