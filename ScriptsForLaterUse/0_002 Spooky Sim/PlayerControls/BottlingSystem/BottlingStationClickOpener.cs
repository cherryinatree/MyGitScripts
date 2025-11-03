using UnityEngine;

[AddComponentMenu("Stations/Bottling Station Click Opener")]
public class BottlingStationClickOpener : MonoBehaviour
{
    [SerializeField] private BottlingStationUI ui;
    [SerializeField] private Transform playerRoot; // CorePlayer root (to check distance)
    [SerializeField] private float maxUseDistance = 3.0f;
    [SerializeField] private InventoryOpsAdapter playerInventory; // or any IInventoryOps implementation

    public void HandleRayHit(RaycastHit hit)
    {
        var station = hit.collider.GetComponentInParent<BottlingStation>();
        if (station == null) return;

        if (playerRoot != null)
        {
            float d = Vector3.Distance(playerRoot.position, station.transform.position);
            if (d > maxUseDistance) return; // too far to use
        }

        if (ui != null && playerInventory != null)
            ui.Open(station, playerInventory);
    }

    public void Interact()
    {
        //var station = hit.collider.GetComponentInParent<BottlingStation>();
       // if (station == null) return;

       // if (playerRoot != null)
       // {
         //   float d = Vector3.Distance(playerRoot.position, station.transform.position);
           // if (d > maxUseDistance) return; // too far to use
       // }

        if (ui != null && playerInventory != null)
            ui.Open(GetComponent<BottlingStation>(), playerInventory);
    }
}
