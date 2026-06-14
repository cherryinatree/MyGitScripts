using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class IntruderHitReactor : MonoBehaviour
{
    public Animator Anim;
    public string HitTrigger = "Hit";
    public string DieTrigger = "Die";

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
        if (Anim == null) Anim = GetComponentInChildren<Animator>();

        _health.OnDamaged.AddListener((amt, atk) =>
        {
            if (Anim != null && !_health.IsDead) Anim.SetTrigger(HitTrigger);
        });

        _health.OnDied.AddListener(() =>
        {
            if (Anim != null) Anim.SetTrigger(DieTrigger);
        });
    }
}
