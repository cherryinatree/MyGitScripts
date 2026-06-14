using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SurfaceType { Floor, Wall, Ceiling }

[DisallowMultipleComponent]
public class StoreSurface : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField, Tooltip("Stable unique id. Do NOT duplicate panels with the same id.")]
    private string uniqueId;

    [Header("Surface")]
    public SurfaceType surfaceType = SurfaceType.Wall;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private int materialIndex = 0;

    [Header("Overrides")]
    [SerializeField] private bool allowTintOverride = true;
    [SerializeField] private bool allowTilingOverride = true;

    [Header("HDRP Property Names")]
    [SerializeField] private string baseColorProperty = "_BaseColor";
    [SerializeField] private string baseColorMapProperty = "_BaseColorMap"; // tiling uses "_BaseColorMap_ST"

    // --- persisted state (what we save) ---
    [SerializeField, Tooltip("Last applied finish id (from FinishMaterialLibrary).")]
    private string currentFinishId = "";

    [SerializeField] private Color currentTint = Color.white;
    [SerializeField] private Vector2 currentTiling = Vector2.one;

    private MaterialPropertyBlock _mpb;

    public string UniqueId => uniqueId;
    public string SaveKey => $"{SceneManager.GetActiveScene().name}|{uniqueId}";

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            var r = GetComponent<Renderer>();
            if (r) targetRenderers = new[] { r };
        }
        _mpb = new MaterialPropertyBlock();

        // Fallback (won’t persist unless you’re in editor), but avoids null keys.
        if (string.IsNullOrWhiteSpace(uniqueId))
            uniqueId = Guid.NewGuid().ToString("N");

        StoreSurfaceRegistry.Register(this);
    }

    private void OnDestroy()
    {
        StoreSurfaceRegistry.Unregister(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(uniqueId))
            uniqueId = Guid.NewGuid().ToString("N");
    }

    [ContextMenu("Generate New Unique Id (DANGEROUS if already saved)")]
    private void GenerateNewId()
    {
        uniqueId = Guid.NewGuid().ToString("N");
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    // Call this from your tool (finishId is what we save)
    public void ApplyFinish(string finishId, Material mat, Color tint, Vector2 tiling)
    {
        if (mat == null || targetRenderers == null) return;

        currentFinishId = finishId;
        currentTint = tint;
        currentTiling = tiling;

        foreach (var r in targetRenderers)
        {
            if (!r) continue;

            var mats = r.sharedMaterials;
            if (materialIndex < 0 || materialIndex >= mats.Length) continue;

            mats[materialIndex] = mat;
            r.sharedMaterials = mats;

            // per-object overrides (no material instancing)
            r.GetPropertyBlock(_mpb, materialIndex);
            _mpb.Clear();

            if (allowTintOverride) _mpb.SetColor(baseColorProperty, currentTint);
            if (allowTilingOverride) _mpb.SetVector(baseColorMapProperty + "_ST",
                new Vector4(currentTiling.x, currentTiling.y, 0f, 0f));

            r.SetPropertyBlock(_mpb, materialIndex);
        }
    }

    public StoreSurfaceState CaptureState()
    {
        return new StoreSurfaceState
        {
            key = SaveKey,
            finishId = currentFinishId,
            tint = currentTint,
            tiling = currentTiling
        };
    }

    public void RestoreState(StoreSurfaceState state, FinishMaterialLibrary library)
    {
        if (library == null) return;
        if (string.IsNullOrWhiteSpace(state.finishId)) return;

        if (!library.TryGetById(state.finishId, out var entry) || entry == null || entry.material == null)
            return;

        ApplyFinish(state.finishId, entry.material, state.tint, state.tiling);
    }


    public StoreSurfaceCustomizationData CaptureCustomization()
    {
        return new StoreSurfaceCustomizationData
        {
            surfaceId = uniqueId,
            sceneName = SceneManager.GetActiveScene().name,
            finishId = currentFinishId,

            tintR = currentTint.r,
            tintG = currentTint.g,
            tintB = currentTint.b,
            tintA = currentTint.a,

            tilingX = currentTiling.x,
            tilingY = currentTiling.y
        };
    }

    public void RestoreCustomization(StoreSurfaceCustomizationData data, FinishMaterialLibrary library)
    {
        if (library == null) return;
        if (string.IsNullOrWhiteSpace(data.finishId)) return;

        if (!library.TryGetById(data.finishId, out var entry) || entry == null || entry.material == null)
            return;

        var tint = new Color(data.tintR, data.tintG, data.tintB, data.tintA);
        var tiling = new Vector2(data.tilingX, data.tilingY);

        ApplyFinish(data.finishId, entry.material, tint, tiling);
    }
}
