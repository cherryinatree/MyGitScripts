



using Cherry.Inventory;
using UnityEngine.InputSystem;

using UnityEngine;

/// <summary>
/// New Input System bridge for EquipmentManager.
/// Bind actions to <Keyboard>/1,2,3,4 (or gamepad, etc.) and assign below.
/// </summary>
public class EquipmentHotkeysInput : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipment;

    [Header("Actions (bind to 1/2/3/4)")]
    public InputActionReference rightHandAction; // <Keyboard>/1
    public InputActionReference leftHandAction;  // <Keyboard>/2
    public InputActionReference rigAction;       // <Keyboard>/3
    public InputActionReference visionAction;    // <Keyboard>/4

    private void Reset()
    {
        if (!equipment) equipment = GetComponentInParent<EquipmentManager>();
    }

    private void OnEnable()
    {
        Subscribe(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    private void Subscribe(bool enable)
    {
        Hook(rightHandAction, OnRight, enable);
        Hook(leftHandAction, OnLeft, enable);
        Hook(rigAction, OnRig, enable);
        Hook(visionAction, OnVision, enable);
    }

    private void Hook(InputActionReference aref, System.Action<InputAction.CallbackContext> cb, bool enable)
    {
        if (aref == null || aref.action == null) return;
        if (enable)
        {
            aref.action.performed += cb;
            aref.action.Enable();
        }
        else
        {
            aref.action.performed -= cb;
            aref.action.Disable();
        }
    }

    private void OnRight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) equipment?.CycleNextForSlot(EquipmentSlotType.RightHand);
    }

    private void OnLeft(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) equipment?.CycleNextForSlot(EquipmentSlotType.LeftHand);
    }

    private void OnRig(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) equipment?.CycleNextForSlot(EquipmentSlotType.Suit);
    }

    private void OnVision(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) equipment?.CycleNextForSlot(EquipmentSlotType.AugmentedReality);
    }
}