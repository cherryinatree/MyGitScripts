using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cherry.Inventory;

public class CraftingStationUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Inventory Strip (player)")]
    [SerializeField] private Transform inventoryListRoot;
    [SerializeField] private InventorySlotEntryView inventoryEntryPrefab;

    [Header("Machine Grid (4 slots)")]
    [SerializeField] private CraftingGridSlotView[] gridSlots = new CraftingGridSlotView[4];

    [Header("Preview")]
    [SerializeField] private Image outputImage;
    [SerializeField] private TMP_Text outputName;
    [SerializeField] private TMP_Text outputInfo;
    [SerializeField] private TMP_Text requirementsText;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;

    [Header("Progress (optional)")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressLabel;

    private CraftingStation _station;
    private IInventoryOps _ops;
    private Inventory _invView;

    private readonly List<InventorySlotEntryView> _invEntries = new();

    private CraftingRecipeSO _currentRecipe;

    private void Awake()
    {
        if (root != null) root.SetActive(false);

        if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(CraftingStation station, IInventoryOps ops, Inventory inventoryView)
    {

        // Show the mouse cursor
        Cursor.visible = true;
        // Unlock the cursor so it can move freely
        Cursor.lockState = CursorLockMode.None;

        _station = station;
        _ops = ops;
        _invView = inventoryView;

        if (_station == null || _ops == null || _invView == null) return;
        Debug.Log("CraftingStationUI: Opened UI for station");
        HookEvents(true);

        BuildInventoryStrip();
        WireGrid();

        RefreshAll();

        if (root != null) root.SetActive(true);
    }

    public void Close()
    {

        // Show the mouse cursor
        Cursor.visible = false;

        // Unlock the cursor so it can move freely
        Cursor.lockState = CursorLockMode.Locked;


        if (_station != null && _ops != null && !_station.IsBusy)
        {
            // Requirement: if user exits without starting, anything in grid goes back.
            _station.EjectAllToPlayer(_ops);
        }

        HookEvents(false);

        if (root != null) root.SetActive(false);

        _station = null;
        _ops = null;
        _invView = null;
        _currentRecipe = null;
    }

    private void OnDisable()
    {
        // Safety: if panel is turned off externally.
        if (root != null && root.activeSelf)
            Close();
    }

    private void HookEvents(bool hook)
    {
        if (_station == null) return;

        if (hook)
        {
            _station.OnBusyChanged += OnBusyChanged;
            _station.OnStorageChanged += RefreshAll;
            _station.OnProgress01Changed += OnProgressChanged;
            _invView.OnInventoryChanged += RefreshAll;
        }
        else
        {
            _station.OnBusyChanged -= OnBusyChanged;
            _station.OnStorageChanged -= RefreshAll;
            _station.OnProgress01Changed -= OnProgressChanged;
            if (_invView != null) _invView.OnInventoryChanged -= RefreshAll;
        }
    }

    private void BuildInventoryStrip()
    {
        // Clear old
        for (int i = 0; i < _invEntries.Count; i++)
            if (_invEntries[i] != null) Destroy(_invEntries[i].gameObject);
        _invEntries.Clear();

        if (inventoryListRoot == null || inventoryEntryPrefab == null) return;

        var slots = _invView.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var e = Instantiate(inventoryEntryPrefab, inventoryListRoot);
            e.Bind(i, this);
            _invEntries.Add(e);
        }
    }

    private void WireGrid()
    {
        for (int i = 0; i < gridSlots.Length; i++)
        {
            if (gridSlots[i] == null) continue;
            int index = i;
            gridSlots[i].onClick = () => OnGridSlotClicked(index);
        }
    }

    private void RefreshAll()
    {
        if (_station == null || _invView == null) return;

        // Update inventory strip visuals
        for (int i = 0; i < _invEntries.Count; i++)
            _invEntries[i].Refresh(_invView);

        // Update 4-slot grid from station stored list
        for (int i = 0; i < gridSlots.Length; i++)
        {
            if (gridSlots[i] == null) continue;
            gridSlots[i].SetEmpty();
        }

        var stored = _station.Stored;
        for (int i = 0; i < stored.Count && i < gridSlots.Length; i++)
        {
            var s = stored[i];
            gridSlots[i].Set(s.item, s.amount);
        }

        // Preview
        _currentRecipe = _station.GetBestCraftableRecipe();
        UpdatePreview(_currentRecipe);

        bool busy = _station.IsBusy;
        SetInteractable(!busy);
    }

    private void UpdatePreview(CraftingRecipeSO recipe)
    {
        if (outputImage != null) outputImage.enabled = false;
        if (outputName != null) outputName.text = "—";
        if (outputInfo != null) outputInfo.text = "";
        if (requirementsText != null) requirementsText.text = "";

        if (recipe == null)
        {
            if (startButton != null) startButton.interactable = false;
            return;
        }

        int craftable = _station.GetCraftableCount(recipe);

        if (outputImage != null)
        {
            outputImage.enabled = true;
            outputImage.sprite = recipe.outputIcon; // if null, it'll just be blank
        }

        if (outputName != null) outputName.text = string.IsNullOrWhiteSpace(recipe.displayName) ? recipe.name : recipe.displayName;
        if (outputInfo != null) outputInfo.text = craftable > 0 ? $"Can craft: {craftable}" : "Load required items";

        if (requirementsText != null)
        {
            var reqLines = new List<string>();
            foreach (var ing in recipe.inputs)
                reqLines.Add($"{ing.item.name} x{ing.amount}");
            requirementsText.text = string.Join("\n", reqLines);
        }

        if (startButton != null) startButton.interactable = (craftable > 0) && !_station.IsBusy;
    }

    private void OnStartClicked()
    {
        if (_station == null || _station.IsBusy) return;
        if (_currentRecipe == null) return;

        if (_station.TryStartCrafting(_currentRecipe))
        {
            // lock UI while crafting; opener prevents re-opening while busy.
            SetInteractable(false);
            Close();
        }
    }

    private void OnBusyChanged(bool busy)
    {
        SetInteractable(!busy);
        if (!busy)
        {
            // crafting ended -> allow interaction again
            RefreshAll();
        }
    }

    private void OnProgressChanged(float t01)
    {
        if (progressSlider != null) progressSlider.value = t01;
        if (progressLabel != null) progressLabel.text = _station != null && _station.IsBusy ? "Crafting..." : "Ready";
    }

    private void SetInteractable(bool canInteract)
    {
        // Inventory buttons
        for (int i = 0; i < _invEntries.Count; i++)
            _invEntries[i].SetInteractable(canInteract);

        // Grid buttons
        for (int i = 0; i < gridSlots.Length; i++)
            if (gridSlots[i] != null) gridSlots[i].SetInteractable(canInteract);

        if (startButton != null) startButton.interactable = canInteract && _currentRecipe != null && _station != null && _station.GetCraftableCount(_currentRecipe) > 0;
    }

    // Called by InventorySlotEntryView (click & hold)
    public void TryAddFromInventorySlot(int slotIndex)
    {
        Debug.Log($"CraftingStationUI: TryAddFromInventorySlot {slotIndex}");
        if (_station == null || _station.IsBusy) return;

        var slots = _invView.Slots;
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var stack = slots[slotIndex];
        if (stack.item == null || stack.amount <= 0) return;

        _station.TryDeposit(_ops, stack.item, 1);
        RefreshAll();
    }

    private void OnGridSlotClicked(int gridIndex)
    {
        if (_station == null || _station.IsBusy) return;

        var stored = _station.Stored;
        if (gridIndex < 0 || gridIndex >= stored.Count) return;

        var s = stored[gridIndex];
        if (s.item == null || s.amount <= 0) return;

        _station.TryWithdraw(_ops, s.item, 1);
        RefreshAll();
    }
}
