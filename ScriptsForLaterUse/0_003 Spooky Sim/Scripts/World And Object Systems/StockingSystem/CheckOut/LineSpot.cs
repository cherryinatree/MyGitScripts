using UnityEngine;

public class LineSpot : MonoBehaviour
{
   public GameObject occupiedBy;
    
    
    public void SetSpotOccupier(GameObject occupier)
    {

       occupiedBy = occupier;
    }

    public bool IsSpotOccupied()
    {
        return occupiedBy != null;
    }

    public void ClearSpot()
    {
        occupiedBy = null;
    }
}
