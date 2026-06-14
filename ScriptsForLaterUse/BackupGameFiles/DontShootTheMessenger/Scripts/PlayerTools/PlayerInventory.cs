// PlayerInventory.cs
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public string heldCoordinates; // null or empty = no coords

    private void Awake() { Instance = this; }

    public void ReceiveCoordinates(string coordinates)
    {
        heldCoordinates = coordinates;
        Debug.Log($"Player received coordinates: {heldCoordinates}");
    }

    public void ClearCoordinates()
    {
        heldCoordinates = null;
    }
}
