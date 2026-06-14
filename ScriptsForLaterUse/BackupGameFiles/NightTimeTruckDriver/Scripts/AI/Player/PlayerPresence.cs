using UnityEngine;

public class PlayerPresence : MonoBehaviour
{
    public static Transform CurrentPlayerTransform;

    [Header("Assign References")]
    public Transform onFootPlayer;
    public Transform truck;

    void Update()
    {
        // Decide which one is "active"
        if (onFootPlayer.gameObject.activeInHierarchy)
        {
            CurrentPlayerTransform = onFootPlayer;
        }
        else if (truck.gameObject.activeInHierarchy)
        {
            CurrentPlayerTransform = truck;
        }
    }
}
