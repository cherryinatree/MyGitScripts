using UnityEngine;

public class RoadSegment : MonoBehaviour
{
    private EndlessRoadManager manager;
    private int myPositionInLine = 0;

    public int segmentIndex;
    public GameObject horrorEvent; // Assign a ghost, object, etc.


    
    void Start()
    {
        manager = FindObjectOfType<EndlessRoadManager>();

        // Hide the event until triggered
        if (horrorEvent != null)
            horrorEvent.SetActive(false);
    }
    public void SetPositionInLine(int position)
    {
        myPositionInLine = position;
    }



    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || !other.CompareTag("Truck")) return;

        // Trigger event if it exists
        if (horrorEvent != null)
        {
            horrorEvent.SetActive(true);
            // Optional: Animate or do something creepy
        }

     //   manager.SpawnSegment();
       // manager.RemoveOldestSegment();
    }
}