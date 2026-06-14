using UnityEngine;
using SUPERCharacter;
using Cherry.Character;

[AddComponentMenu("Cherry/World/Moving Platform Ride Zone (SUPER Character)")]
public class RideZoneParenting : MonoBehaviour
{
    [Tooltip("Optional: only objects with this tag will be processed.")]
    [SerializeField] private string requiredTag = "Player";

    [Tooltip("If true, will parent a child named VisualRoot (NOT the Rigidbody root). Optional.")]
    [SerializeField] private bool parentVisualRoot = false;

    [SerializeField] private string visualRootName = "VisualRoot";

    private Transform _platformRoot;
    private Rigidbody _platformRb;

    private void Awake()
    {
        _platformRoot = transform.parent != null ? transform.parent : transform;
        _platformRb = _platformRoot.GetComponentInParent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        var controller = other.GetComponentInParent<CherryCharacterController>();
        if (controller == null) return;

        controller.SetActivePlatform(_platformRb, _platformRoot);

        if (parentVisualRoot)
        {
            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                Transform vr = FindNamedChild(rb.transform, visualRootName);
                if (vr != null) vr.SetParent(_platformRoot, true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

        var controller = other.GetComponentInParent<CherryCharacterController>();
        if (controller == null) return;

        controller.ClearActivePlatform(_platformRoot);

        if (parentVisualRoot)
        {
            var rb = other.attachedRigidbody;
            if (rb != null)
            {
                Transform vr = FindNamedChild(rb.transform, visualRootName);
                if (vr != null && vr.parent == _platformRoot) vr.SetParent(null, true);
            }
        }
    }

    private static Transform FindNamedChild(Transform root, string childName)
    {
        if (string.IsNullOrEmpty(childName)) return null;
        var all = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in all) if (t.name == childName) return t;
        return null;
    }
}
