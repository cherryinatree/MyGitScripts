// HotbarSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class HotbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image background;
    public Image icon;
    public TMP_Text countText;
    public GameObject highlight;

    [HideInInspector] public int slotIndex;
    System.Action<int> onClick;

    public void Init(int index, System.Action<int> onClicked)
    {
        slotIndex = index;
        onClick = onClicked;
        SetIcon(null);
        SetCount(0);
        SetSelected(false);
    }

    public void SetIcon(Sprite s)
    {
        if (icon) { icon.sprite = s; icon.enabled = s != null; }
    }

    public void SetCount(int c)
    {
        if (!countText) return;
        countText.text = c > 1 ? c.ToString() : "";
    }

    public void SetSelected(bool selected)
    {
        if (highlight) highlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(slotIndex);
    }
}
