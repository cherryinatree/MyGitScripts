// ShopTriggerNet.cs
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class ShopTriggerNet : MonoBehaviour
{
    [Header("Refs")]
    public ShopVendorNet vendor;
    public GameObject shopUIPrefabOrPanel;
    public bool instantiateUI = false;
    public GameObject interactPromptUI;

    [Header("Input")]
    public InputActionReference interactActionRef; // optional, E/gamepad South

    CharacterInventoryNet currentLocal;
    GameObject uiInstance;
    bool inRangeLocal;
    InputAction _interact;

    // === Added: cursor state tracking ===
    bool _uiOpen;
    bool _prevCursorVisible;
    CursorLockMode _prevCursorLock;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void Awake()
    {
        _interact = interactActionRef ? interactActionRef.action
                                      : new InputAction(type: InputActionType.Button, binding: "<Keyboard>/e");
        _interact.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable() => _interact?.Enable();

    void OnDisable()
    {
        _interact?.Disable();
        // Safety: if disabled while UI is open, restore cursor
        if (_uiOpen) RestoreCursor();
    }

    void Update()
    {
        if (inRangeLocal && _interact != null && _interact.WasPressedThisFrame())
        {
            if (uiInstance && uiInstance.activeSelf) CloseLocal();
            else OpenForLocal(currentLocal);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        var inv = other.GetComponentInParent<CharacterInventoryNet>();
        if (!inv) return;

        // Only open UI for the entering player on their own client
        if (!inv.IsOwner && !inv.IsLocalPlayer) return;

        currentLocal = inv;
        inRangeLocal = true;
        if (interactPromptUI && !_uiOpen) interactPromptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        var inv = other.GetComponentInParent<CharacterInventoryNet>();
        if (!inv || inv != currentLocal) return;

        inRangeLocal = false;
        if (interactPromptUI) interactPromptUI.SetActive(false);
        CloseLocal();
        currentLocal = null;
    }

    void OpenForLocal(CharacterInventoryNet localInv)
    {
        if (!localInv) return;

        if (!vendor)
        {
            vendor = FindObjectOfType<ShopVendorNet>();
            if (!vendor) { Debug.LogError("[ShopTriggerNet] No vendor assigned."); return; }
        }

        if (instantiateUI)
        {
            if (!uiInstance)
            {
                if (!shopUIPrefabOrPanel) { Debug.LogError("[ShopTriggerNet] No shop UI prefab assigned."); return; }
                uiInstance = Instantiate(shopUIPrefabOrPanel);
            }
            uiInstance.SetActive(true);
        }
        else
        {
            if (!shopUIPrefabOrPanel) { Debug.LogError("[ShopTriggerNet] No shop UI panel assigned."); return; }
            shopUIPrefabOrPanel.SetActive(true);
            uiInstance = shopUIPrefabOrPanel;
        }

        var ui = uiInstance.GetComponent<ShopUIControllerNet>() ?? uiInstance.AddComponent<ShopUIControllerNet>();
        ui.Bind(this, vendor, localInv, localInv.db);

        if (interactPromptUI) interactPromptUI.SetActive(false);

        // === Added: show/unlock mouse for this local player ===
        SaveAndShowCursor();
        _uiOpen = true;
    }

    void CloseLocal()
    {
        if (!uiInstance) return;

        uiInstance.SetActive(false);

        // === Added: hide/restore mouse when leaving shop ===
        RestoreCursor();
        _uiOpen = false;

        if (inRangeLocal && interactPromptUI) interactPromptUI.SetActive(true);
    }

    // === Added: cursor helpers ===
    void SaveAndShowCursor()
    {
        _prevCursorVisible = Cursor.visible;
        _prevCursorLock = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void RestoreCursor()
    {
        Cursor.visible = _prevCursorVisible;
        Cursor.lockState = _prevCursorLock;
    }

    // Called by UI
    public void UI_Buy(string itemId, int qty)
    {
        if (!currentLocal) return;

        var vendorNO = vendor.GetComponent<NetworkObject>();
        if (!vendorNO || !vendorNO.IsSpawned)
        {
            Debug.LogError("[ShopTriggerNet] Vendor NetworkObject not spawned.");
            return;
        }
        vendor.RequestBuyServerRpc(currentLocal.NetworkObject, itemId, Mathf.Max(1, qty));
    }
}
