using UnityEngine;

[AddComponentMenu("Gameplay/Ectoplasm Coater")]
public class EctoplasmCoater : MonoBehaviour
{
    [Header("Coating")]
    public BeamRayDefinition coatRayOverride;   // set to Ray_GhostGreen
    [Min(0.1f)] public float coatSecondsOverride = 6f;

    [Header("Lifecycle")]
    public bool destroyOnCoat = true;

    private void OnCollisionEnter(Collision c) => TryCoat(c.collider);
    private void OnTriggerEnter(Collider other) => TryCoat(other);

    private void TryCoat(Collider col)
    {
        var mirror = col.GetComponentInParent<ReflectiveSurface>();
        if (mirror == null) return;

        mirror.ApplyCoating(coatRayOverride, coatSecondsOverride);

        if (destroyOnCoat)
            Destroy(gameObject);
    }
}