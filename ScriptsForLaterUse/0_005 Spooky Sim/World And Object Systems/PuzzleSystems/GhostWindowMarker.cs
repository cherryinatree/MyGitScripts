using UnityEngine;

[DisallowMultipleComponent]
public class GhostWindowMarker : MonoBehaviour
{
    [Tooltip("Create this layer in Project Settings > Tags and Layers.")]
    [SerializeField] private string windowMaskLayerName = "GhostWindowMask";

    void OnEnable() => ApplyLayer();
    void OnValidate() => ApplyLayer();

    void ApplyLayer()
    {
        int layer = LayerMask.NameToLayer(windowMaskLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"GhostWindowMarker: Layer '{windowMaskLayerName}' not found. Create it in Tags and Layers.");
            return;
        }

        gameObject.layer = layer;
    }
}