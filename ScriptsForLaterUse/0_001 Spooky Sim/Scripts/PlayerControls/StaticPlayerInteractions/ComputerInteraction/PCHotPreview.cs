using UnityEngine;

/// <summary>
/// Optional gizmo drawer: attach to your cursor object if you want to see the hotspot in the editor.
/// (Purely visual aid; PControls has a runtime marker as well.)
/// </summary>
[ExecuteAlways]
public class PCHotPreview : MonoBehaviour
{
    public PControls controls;

    private void OnDrawGizmos()
    {
        if (controls == null) return;
        Gizmos.color = Color.yellow;
        var wp = controls.transform.position; // fallback
        try
        {
            wp = controls.GetType().GetMethod("HotspotWorldPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(controls, null) as Vector3? ?? wp;
        }
        catch { }

        Gizmos.DrawWireSphere(wp, 0.01f);
    }
}
