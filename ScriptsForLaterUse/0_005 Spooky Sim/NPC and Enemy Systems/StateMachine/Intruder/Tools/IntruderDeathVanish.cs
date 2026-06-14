using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class IntruderDeathVanish : MonoBehaviour
{
    public float vanishDelay = 0.2f;

    private Health _hp;
    private Intruder _intruder;
    private bool _handled;

    private void Awake()
    {
        _hp = GetComponent<Health>();
        _intruder = GetComponent<Intruder>();
    }

    private void Update()
    {
        if (_handled) return;
        if (_hp == null) return;

        if (_hp.IsDead)
            HandleDeath();
    }

    private void HandleDeath()
    {
        _handled = true;

        // Tell your systems it's gone
        if (_intruder != null)
            _intruder.Neutralize(); // you already call this elsewhere, so use it

        // Visual disappear (optional)
        StartCoroutine(VanishRoutine());
    }

    private IEnumerator VanishRoutine()
    {
        // disable collisions immediately so robots stop bumping it
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // hide meshes immediately (or swap to death FX)
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        yield return new WaitForSeconds(vanishDelay);

        Destroy(gameObject);
    }
}
