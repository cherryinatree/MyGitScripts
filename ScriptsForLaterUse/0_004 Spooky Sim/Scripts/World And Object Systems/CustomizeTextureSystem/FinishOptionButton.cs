using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinishOptionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image imageThumb;     // for Sprite thumbnails
    [SerializeField] private RawImage rawThumb;    // for Texture preview
    [SerializeField] private TMP_Text label;

    private FinishMaterialEntry _entry;
    private StoreCustomizerSelectionUI _ui;

    public void Bind(StoreCustomizerSelectionUI ui, FinishMaterialEntry entry)
    {
        _ui = ui;
        _entry = entry;

        if (!button) button = GetComponent<Button>();
        if (label) label.text = entry.displayName;

        if (imageThumb)
        {
            imageThumb.sprite = entry.thumbnail;
            imageThumb.enabled = entry.thumbnail != null;
        }

        if (rawThumb)
        {
            rawThumb.texture = entry.previewTexture;
            rawThumb.enabled = entry.previewTexture != null;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _ui.SelectFinish(_entry));
    }
}
