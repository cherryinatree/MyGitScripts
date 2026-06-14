using UnityEngine;

public class HornController : MonoBehaviour
{
    public KeyCode hornKey = KeyCode.H;

    void Update()
    {
        if (Input.GetKeyDown(hornKey))
        {
            Debug.Log("Horn pressed!");

            GrabTruckBehavior[] grabbers = FindObjectsOfType<GrabTruckBehavior>();
            foreach (var g in grabbers)
            {
                g.SetRepelled();
            }
        }
    }
}
