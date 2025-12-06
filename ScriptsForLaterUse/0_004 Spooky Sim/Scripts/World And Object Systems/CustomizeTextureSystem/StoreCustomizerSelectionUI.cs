using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreCustomizerSelectionUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private StoreCustomizerTool tool;
    [SerializeField] private FinishMaterialLibrary library;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroup; // optional

    [Header("Disable These While UI Open")]
    [SerializeField] private List<Behaviour> disableWhileOpen = new();

    [Header("Finish Grid")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private FinishOptionButton buttonPrefab;

    [Header("Preview")]
    [SerializeField] private RawImage selectedPreviewRaw;
    [SerializeField] private Image selectedPreviewImage;
    [SerializeField] private TMP_Text selectedNameText;

    [Header("Tint Sliders (0-1)")]
    [SerializeField] private Slider rSlider;
    [SerializeField] private Slider gSlider;
    [SerializeField] private Slider bSlider;
    [SerializeField] private Slider aSlider;

    [Header("Tiling")]
    [SerializeField] private TMP_InputField tilingX;
    [SerializeField] private TMP_InputField tilingY;

    [Header("Close Button (optional)")]
    [SerializeField] private Button closeButton;

    public bool IsOpen { get; private set; }

    private readonly Dictionary<Behaviour, bool> _prevEnabled = new();
    private FinishMaterialEntry _selected;
    private Vector2 _tiling = Vector2.one;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);

        HookSlider(rSlider);
        HookSlider(gSlider);
        HookSlider(bSlider);
        HookSlider(aSlider);

        if (tilingX) tilingX.onEndEdit.AddListener(_ => OnTilingChanged());
        if (tilingY) tilingY.onEndEdit.AddListener(_ => OnTilingChanged());

        BuildGrid();
        SetOpen(false);
    }

    private void HookSlider(Slider s)
    {
        if (!s) return;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.onValueChanged.AddListener(_ => PushToTool());
    }

    private void BuildGrid()
    {
        if (!library || !gridParent || !buttonPrefab) return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
            Destroy(gridParent.GetChild(i).gameObject);

        foreach (var entry in library.finishes)
        {
            var b = Instantiate(buttonPrefab, gridParent);
            b.Bind(this, entry);
        }

        if (library.finishes.Count > 0)
            SelectFinish(library.finishes[0]);
    }

    public void Toggle() => SetOpen(!IsOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        IsOpen = open;

        if (root) root.SetActive(open);

        if (canvasGroup)
        {
            canvasGroup.alpha = open ? 1f : 0f;
            canvasGroup.interactable = open;
            canvasGroup.blocksRaycasts = open;
        }

        // Cursor
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;

        // Disable/restore scripts
        if (open)
        {
            _prevEnabled.Clear();
            foreach (var b in disableWhileOpen)
            {
                if (!b) continue;
                _prevEnabled[b] = b.enabled;
                b.enabled = false;
            }
        }
        else
        {
            foreach (var kvp in _prevEnabled)
            {
                if (kvp.Key) kvp.Key.enabled = kvp.Value;
            }
            _prevEnabled.Clear();
        }
    }

    public void SelectFinish(FinishMaterialEntry entry)
    {
        _selected = entry;
        if (_selected == null) return;

        if (selectedNameText) selectedNameText.text = _selected.displayName;

        // Preview: prefer sprite -> image, else texture -> rawimage
        if (selectedPreviewImage)
        {
            selectedPreviewImage.sprite = _selected.thumbnail;
            selectedPreviewImage.enabled = _selected.thumbnail != null;
        }

        if (selectedPreviewRaw)
        {
            selectedPreviewRaw.texture = _selected.previewTexture != null ? _selected.previewTexture : null;
            selectedPreviewRaw.enabled = selectedPreviewRaw.texture != null;
        }

        // defaults
        _tiling = _selected.defaultTiling;
        if (tilingX) tilingX.text = _tiling.x.ToString("0.##");
        if (tilingY) tilingY.text = _tiling.y.ToString("0.##");

        if (rSlider) rSlider.value = _selected.defaultTint.r;
        if (gSlider) gSlider.value = _selected.defaultTint.g;
        if (bSlider) bSlider.value = _selected.defaultTint.b;
        if (aSlider) aSlider.value = _selected.defaultTint.a;

        PushToTool();
    }

    private void OnTilingChanged()
    {
        float x = _tiling.x, y = _tiling.y;
        if (tilingX && float.TryParse(tilingX.text, out var px)) x = Mathf.Max(0.01f, px);
        if (tilingY && float.TryParse(tilingY.text, out var py)) y = Mathf.Max(0.01f, py);
        _tiling = new Vector2(x, y);
        PushToTool();
    }

    private void PushToTool()
    {
        if (!tool || _selected == null) return;

        var tint = new Color(
            rSlider ? rSlider.value : 1f,
            gSlider ? gSlider.value : 1f,
            bSlider ? bSlider.value : 1f,
            aSlider ? aSlider.value : 1f
        );

        tool.SetSelection(new FinishSelection
        {
            finishId = _selected.id,
            material = _selected.material,
            tint = tint,
            tiling = _tiling
        });

    }
}
