using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class IntruderHitReact : MonoBehaviour
{
    public Animator Anim;
    public string HitTrigger = "Hit";
    public string DieTrigger = "Die";
    public float VanishDelay = 0.2f;

    private Health _hp;
    private Intruder _intruder;
    private bool _deadHandled;

    private void Awake()
    {
        if (Anim == null) Anim = GetComponentInChildren<Animator>();
        _hp = GetComponent<Health>();
        _intruder = GetComponent<Intruder>();
    }

    public void PlayHit()
    {
        if (_deadHandled) return;
        if (Anim != null && !string.IsNullOrWhiteSpace(HitTrigger))
            Anim.SetTrigger(HitTrigger);
    }

    private void Update()
    {
        if (_deadHandled) return;
        if (_hp == null) return;

        if (_hp.IsDead)
            StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        _deadHandled = true;

        if (_intruder != null)
            _intruder.Neutralize(); // should set IsActive=false + remove from list

        if (Anim != null && !string.IsNullOrWhiteSpace(DieTrigger))
            Anim.SetTrigger(DieTrigger);

        yield return new WaitForSeconds(VanishDelay);

        // Hide + destroy
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        Destroy(gameObject);
    }
}
