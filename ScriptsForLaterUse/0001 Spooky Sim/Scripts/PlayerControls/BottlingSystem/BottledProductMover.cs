using UnityEngine;

[AddComponentMenu("Stations/Bottled Product Mover")]
public class BottledProductMover : MonoBehaviour
{
    public Transform target;
    [Min(0.05f)] public float travelDuration = 0.6f;
    [Min(0f)] public float arcHeight = 0.2f;
    public bool parentToTargetOnArrive = true;
    public Vector3 finalLocalOffset = Vector3.zero; // optional shelf offset

    private Vector3 _start;
    private float _t;

    private void OnEnable()
    {
        _start = transform.position;
        _t = 0f;
    }

    private void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        _t += Time.deltaTime / Mathf.Max(0.05f, travelDuration);
        float u = Mathf.Clamp01(_t);

        Vector3 end = target.position;
        Vector3 p = Vector3.Lerp(_start, end, u);
        if (arcHeight > 0f)
        {
            float arc = 4f * u * (1f - u) * arcHeight; // simple parabola
            p.y += arc;
        }

        transform.position = p;

        if (u >= 1f)
        {
            if (parentToTargetOnArrive)
            {
                transform.SetParent(target, true);
                transform.localPosition += finalLocalOffset;
            }
            enabled = false; // stop moving (leave the bottle)
        }
    }
}
