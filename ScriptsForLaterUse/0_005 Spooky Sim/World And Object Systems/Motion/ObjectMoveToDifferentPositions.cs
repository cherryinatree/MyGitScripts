using UnityEngine;

public class ObjectMoveToDifferentPositions : MonoBehaviour
{
    public ObjectMoveTo[] places;
    private int currentIndex = 0;
    

    public void MoveToNextPlace()
    {
        if (places.Length == 0) return;

        places[currentIndex].StopMove();
        currentIndex = (currentIndex + 1) % places.Length;
        places[currentIndex].BeginMove();
        Debug.Log("Moving to place index: " + currentIndex);
    }
}
