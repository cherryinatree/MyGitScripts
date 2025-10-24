using NUnit.Framework;
using UnityEngine;

public class CheckOutLine : MonoBehaviour
{
    public LineSpot[] Line;


    public void Start()
    {
        FindFirstObjectByType<CustomerStoreManager>().SubscribeCheckoutLines(this);
    }


    public bool IsLineFull()
    {
        foreach (var spot in Line)
        {
            if (!spot.IsSpotOccupied())
            {
                return false;
            }
        }
        return true;
    }

    public bool IsLineEmpty()
    {
        foreach (var spot in Line)
        {
            if (spot.IsSpotOccupied())
            {
                return false;
            }
        }
        return true;
    }

    public bool IsNextSpotAvailable(GameObject customer)
    {
        for (int i = 0; i < Line.Length - 1; i++)
        {
            if (Line[i].occupiedBy == customer)
            {
                if (i > 0)
                {
                    return !Line[i - 1].IsSpotOccupied();
                }
            }
        }
        return false; // customer not found in line
    }

    public int GetCustomerPositionInLine(GameObject customer)
    {
        for (int i = 0; i < Line.Length; i++)
        {
            if (Line[i].occupiedBy == customer)
            {
                return i;
            }
        }
        return -1; // customer not found in line
    }

    public Vector3 GetFirstAvailableSpot(GameObject customer)
    {
        foreach (var spot in Line)
        {
            if (!spot.IsSpotOccupied())
            {
                spot.SetSpotOccupier(customer);
                return spot.transform.position;
            }
        }
        return Vector3.zero; // line is full
    }

    public Vector3 MoveUpInLine(GameObject customer)
    {
        ClearLineSpot(customer);
        return GetFirstAvailableSpot(customer); // reassign to first available spot in
    }

    public void LeaveLine(GameObject customer)
    {
        ClearLineSpot(customer);
    }

    private void ClearLineSpot(GameObject customer)
    {
        foreach (var spot in Line)
        {
            if (spot.occupiedBy == customer)
            {
                spot.ClearSpot();
            }
        }
    }
}
