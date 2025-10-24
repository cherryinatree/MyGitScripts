using UnityEngine;

public class LookHighlighter : MonoBehaviour
{
    [Header("Raycast")]
    public Camera cam;                 // if null, uses Camera.main
    public float maxDistance = 5f;
    public LayerMask mask = ~0;        // everything by default

    [Header("Behavior")]
    public bool requireTag = false;
    public string targetTag = "Highlightable";

    IHighlightable _current;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
        {
            var t = hit.collider.transform;
            if (requireTag && !t.CompareTag(targetTag) && !(t.parent && t.parent.CompareTag(targetTag)))
            {
                SetCurrent(null);
                return;
            }

            var hi = t.GetComponentInParent<IHighlightable>();
            SetCurrent(hi);
        }
        else
        {
            SetCurrent(null);
        }
    }

    void SetCurrent(IHighlightable next)
    {
        if (_current == next) return;

        if (_current != null) _current.SetHighlighted(false);
        _current = next;
        if (_current != null) _current.SetHighlighted(true);
    }
}

public interface IHighlightable
{
    void SetHighlighted(bool on);
}
