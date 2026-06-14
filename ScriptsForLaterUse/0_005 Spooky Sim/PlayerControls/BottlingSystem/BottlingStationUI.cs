using Cherry.Inventory;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[AddComponentMenu("Stations/Bottling Station UI")]
public class BottlingStationUI : MonoBehaviour
{
    [SerializeField] public GameObject panel;     // enable/disable to show UI
    [SerializeField] public TMP_Dropdown itemDropdown; // or TMP_Dropdown if you prefer

    private BottlingStation _station;
    private IInventoryOps _inventory;
    private List<ItemDefinition> _choices = new();

    private void Start()
    {
        panel.SetActive(false);
    }

    public void Open(BottlingStation station, IInventoryOps inventory)
    {
        // Show the mouse cursor
        Cursor.visible = true;

        // Unlock the cursor so it can move freely
        Cursor.lockState = CursorLockMode.None;
        _station = station;
        _inventory = inventory;
        RefreshChoices();
        panel.SetActive(true);
    }

    public void Close()
    {
        // Show the mouse cursor
        Cursor.visible = false;

        // Unlock the cursor so it can move freely
        Cursor.lockState = CursorLockMode.Locked;
        panel.SetActive(false);
        _station = null;
        _inventory = null;
        _choices.Clear();
        itemDropdown.ClearOptions();
    }

    private void RefreshChoices()
    {
        _choices.Clear();
        var options = new List<TMP_Dropdown.OptionData>();

        foreach (var r in _station.AllRecipes)
        {
            int count = _inventory.Count(r.inputItem);
            if (count > 0)
            {
                _choices.Add(r.inputItem);
                options.Add(new TMP_Dropdown.OptionData($"{r.inputItem.DisplayName} (x{count})"));
            }
        }

        itemDropdown.ClearOptions();
        itemDropdown.AddOptions(options);

        if (_choices.Count == 0)
        {
            itemDropdown.options.Add(new TMP_Dropdown.OptionData("No valid items"));
            itemDropdown.value = 0;
        }
    }

    // Hook this to a “Start” button
    public void StartBatchFromSelection()
    {
        if (_station == null || _inventory == null) return;
        if (_choices.Count == 0) return;

        var item = _choices[Mathf.Clamp(itemDropdown.value, 0, _choices.Count - 1)];
        if (_station.BeginBatch(_inventory, item))
            Close();
    }
}
