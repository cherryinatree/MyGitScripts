// PlacementSystem.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementSystem : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Inventory inventory;

    [Header("Runtime")]
    public LayerMask placementMask = ~0; // ground by default
    public float maxDistance = 10f;
    public GameObject ghostInstance;
    ItemDefinition currentItem;

    public void BeginPlace(ItemDefinition item)
    {
        if (!item || !item.isPlaceable || !item.placeablePrefab) return;
        if (inventory.Count(item) <= 0) return;
        currentItem = item;
        ghostInstance = Instantiate(item.placeablePrefab);
        SetGhostMode(ghostInstance, true);
    }

    public void CancelPlace()
    {
        if (ghostInstance) Destroy(ghostInstance);
        ghostInstance = null;
        currentItem = null;
    }

    void Update()
    {
        if (!currentItem || !ghostInstance) return;

        // Ray from center screen
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out var hit, maxDistance, placementMask))
        {
            Vector3 pos = hit.point + currentItem.placeOffset;

            if (currentItem.snapToGrid && currentItem.gridSize > 0.01f)
            {
                float g = currentItem.gridSize;
                pos = new Vector3(
                    Mathf.Round(pos.x / g) * g,
                    pos.y,
                    Mathf.Round(pos.z / g) * g
                );
            }

            ghostInstance.transform.position = pos;

            // Rotate with Q/E (requires Input System)
            if (Keyboard.current.qKey.wasPressedThisFrame)
                ghostInstance.transform.Rotate(Vector3.up, -15f, Space.World);
            if (Keyboard.current.eKey.wasPressedThisFrame)
                ghostInstance.transform.Rotate(Vector3.up, 15f, Space.World);

            // Place: Left Click
            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryPlaceAt(pos, ghostInstance.transform.rotation);
        }

        // Cancel with Right Click
        if (Mouse.current.rightButton.wasPressedThisFrame)
            CancelPlace();
    }

    void TryPlaceAt(Vector3 pos, Quaternion rot)
    {
        if (!currentItem) return;
        // from client
        /*if (spawnerNet && characterNet)
        {
            spawnerNet.RequestPlaceServerRpc(characterNet.NetworkObject, currentItem.itemId, pos, rot);
        }*/
        // Optional: Add collision/overlap checks here to validate placement.

        var placed = Instantiate(currentItem.placeablePrefab, pos, rot);
        // mark the placed object as persistent/ownable as needed…

        if (inventory.Remove(currentItem, 1))
        {
            // Continue placing if more exist; stop if none left
            if (inventory.Count(currentItem) <= 0) CancelPlace();
        }
        else
        {
            // Shouldn't happen if we began with an item in inventory
            CancelPlace();
        }
    }

    void SetGhostMode(GameObject go, bool ghost)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = !ghost;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.materials)
            {
                if (ghost) m.SetFloat("_Mode", 2); // if using a shader variant
            }
        }
    }

    
}
