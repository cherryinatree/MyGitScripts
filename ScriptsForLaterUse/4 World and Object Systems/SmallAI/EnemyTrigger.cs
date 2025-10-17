using UnityEngine;

public class EnemyTrigger : MonoBehaviour
{
    public EnemyController enemy;
    public bool makeAppear; // true = come out, false = retreat
    public DirectionGate gate; // optional; only needed on the Appear trigger
    private bool fired;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (!other.CompareTag("Player")) return;

        if (makeAppear)
        {
            enemy.gameObject.SetActive(true); // ensure active
            enemy.Appear();
            if (gate) gate.NotifyAppearFired(); // now allow retreat
        }
        else
        {
            enemy.Retreat();
        }

        fired = true; // remove if you want reusability
        // Optionally disable this trigger after firing:
        // gameObject.SetActive(false);
    }
}
