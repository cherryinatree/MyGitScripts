using UnityEngine;

[System.Serializable]
public struct FinishSelection
{
    public string finishId;  // <-- important for saving
    public Material material;
    public Color tint;
    public Vector2 tiling;
}
