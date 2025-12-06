using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Cherry.Inventory;
using Unity.VisualScripting;

public class InventorySlotEntryView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button button;

    [Header("Hold-to-add")]
    [SerializeField] private float holdDelay = 0.25f;
    [SerializeField] private float repeatEvery = 0.07f;

    private int _slotIndex;
    private CraftingStationUI _ui;
    private Coroutine _holdCo;
    private bool _held;

    public void Bind(int slotIndex, CraftingStationUI ui)
    {
        _slotIndex = slotIndex;
        _ui = ui;

        if (button != null)
        {
           // button.onClick.RemoveAllListeners();
          //  button.onClick.AddListener(() => _ui.TryAddFromInventorySlot(_slotIndex));
        }
    }

    public void Refresh(Inventory inv)
    {
        var s = inv.Slots[_slotIndex];

        if (icon != null)
        {
            icon.enabled = s.item != null;
            // NOTE: rename "icon" below if your ItemDefinition uses a different field/property
            icon.sprite = s.item != null ? s.item.Icon : null;
        }

        if (amountText != null)
            amountText.text = (s.item != null && s.amount > 0) ? s.amount.ToString() : "";
    }

    public void SetInteractable(bool canInteract)
    {
        if (button != null) button.interactable = canInteract;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.interactable) return;

        _held = true;
        _ui.TryAddFromInventorySlot(_slotIndex);

        if (_holdCo != null) StopCoroutine(_holdCo);
        _holdCo = StartCoroutine(HoldRepeat());
    }

    public void OnPointerUp(PointerEventData eventData) => StopHold();
    public void OnPointerExit(PointerEventData eventData) => StopHold();

    private void StopHold()
    {
        _held = false;
        if (_holdCo != null) StopCoroutine(_holdCo);
        _holdCo = null;
    }

    private IEnumerator HoldRepeat()
    {
        yield return new WaitForSeconds(holdDelay);

        while (_held)
        {
            _ui.TryAddFromInventorySlot(_slotIndex);
            yield return new WaitForSeconds(repeatEvery);
        }
    }
}
