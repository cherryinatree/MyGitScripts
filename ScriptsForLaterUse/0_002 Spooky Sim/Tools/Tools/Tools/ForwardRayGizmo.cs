using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;
#endif

[ExecuteAlways] // <- runs in edit mode too
[AddComponentMenu("Debug/Forward Ray Gizmo (Always)")]
public class ForwardRayGizmo : MonoBehaviour
{
    [Header("Draw Settings")]
    public Color color = Color.cyan;
    [Min(0f)] public float length = 2f;
    public Vector3 originOffset = Vector3.zero;
    public bool onlyWhenSelected = false;
    public bool drawArrowHead = true;
    [Min(0f)] public float arrowSize = 0.2f;

#if UNITY_EDITOR
    [Header("Editor Extras")]
    public bool thickInEditor = true;
    [Min(1f)] public float lineThickness = 2f;
    public bool labelForward = false;
#endif

    void OnDrawGizmos()
    {
        if (onlyWhenSelected) return;
        Draw();
    }

    void OnDrawGizmosSelected()
    {
        if (!onlyWhenSelected) return;
        Draw();
    }

    void Draw()
    {
        Vector3 origin = transform.position + originOffset;
        Vector3 dir = transform.forward;

#if UNITY_EDITOR
        // Use anti-aliased lines in edit mode for nicer visuals
        if (!Application.isPlaying && thickInEditor)
        {
            Handles.zTest = CompareFunction.Always;
            Handles.color = color;
            Handles.DrawAAPolyLine(lineThickness, origin, origin + dir * length);

            if (drawArrowHead)
            {
                Vector3 tip = origin + dir * length;
                float s = Mathf.Max(0.0001f, arrowSize);
                Handles.DrawAAPolyLine(lineThickness, tip, tip + (-dir + transform.right).normalized * s);
                Handles.DrawAAPolyLine(lineThickness, tip, tip + (-dir - transform.right).normalized * s);
                Handles.DrawAAPolyLine(lineThickness, tip, tip + (-dir + transform.up).normalized * s);
                Handles.DrawAAPolyLine(lineThickness, tip, tip + (-dir - transform.up).normalized * s);
            }

            if (labelForward)
                Handles.Label(origin + dir * (length + arrowSize * 1.2f), "forward");

            return; // don't also draw Gizmos
        }
#endif

        // Fallback to built-in Gizmos (works in edit & play)
        Gizmos.color = color;
        Gizmos.DrawRay(origin, dir * length);

        if (drawArrowHead)
        {
            Vector3 tip = origin + dir * length;
            float s = Mathf.Max(0.0001f, arrowSize);
            Gizmos.DrawRay(tip, (-dir + transform.right).normalized * s);
            Gizmos.DrawRay(tip, (-dir - transform.right).normalized * s);
            Gizmos.DrawRay(tip, (-dir + transform.up).normalized * s);
            Gizmos.DrawRay(tip, (-dir - transform.up).normalized * s);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) SceneView.RepaintAll(); // live update while editing
    }
#endif
}
