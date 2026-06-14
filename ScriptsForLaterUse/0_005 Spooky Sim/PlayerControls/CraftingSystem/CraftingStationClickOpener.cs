using UnityEngine;
using Cherry.Inventory;

[AddComponentMenu("Stations/Crafting Station Click Opener")]
public class CraftingStationClickOpener : MonoBehaviour
{
    [SerializeField] private CraftingStationUI ui;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private float maxUseDistance = 3.0f;

    [Header("Player Inventory")]
    [SerializeField] private InventoryOpsAdapter playerInventoryOpsBehaviour; // must implement IInventoryOps
    [SerializeField] private Inventory playerInventoryView;            // used to display slots

    private IInventoryOps PlayerOps => playerInventoryOpsBehaviour as IInventoryOps;

    public void HandleRayHit(RaycastHit hit)
    {
        var station = hit.collider.GetComponentInParent<CraftingStation>();
        if (station == null) return;
        TryOpen(station);
    }

    public void Interact()
    {
        var station = GetComponent<CraftingStation>();
        if (station == null) return;
        TryOpen(station);
    }

    private void TryOpen(CraftingStation station)
    {
        if (station == null || ui == null) return;
        if (station.IsBusy) return; // locked while crafting
        Debug.Log("Opening Crafting Station UI");
        if (playerRoot != null)
        {
            float d = Vector3.Distance(playerRoot.position, station.transform.position);
            if (d > maxUseDistance) return;
        }
        Debug.Log("playerOps: " + PlayerOps);
        Debug.Log("playerInventoryView: " + playerInventoryView);
        if (PlayerOps == null || playerInventoryView == null) return;
        Debug.Log("Binding Player Inventory to Crafting Station UI");
        ui.Open(station, PlayerOps, playerInventoryView);
    }
}
