using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeAstar : MonoBehaviour
{

    public int G = 0;
    public int H = 0;
    public int F { get { return G + H; } }
    private GameObject previous;

    public bool traveled = false;
    public bool walkable = true;

    public List<GameObject> CubeNeighbors;


    public void AstarReset()
    {
        G = 0;
        H = 0;

        traveled = false;
    }


    public void CalculateGandH(GameObject start, GameObject end, GameObject item)
    {

        G = CalculateDistance(start, item);
        H = CalculateDistance(end, item);
    }

    private int CalculateDistance(GameObject startEnd, GameObject item)
    {
        int distanceX = (int)Mathf.Abs(startEnd.transform.position.x - item.transform.position.x);
        int distanceY = (int)Mathf.Abs(startEnd.transform.position.y - item.transform.position.y);

        return distanceX + distanceY;
    }


    public GameObject PreviousCube
    {
        get { return previous; }
        set { previous = value; }
    }

    public bool isWalkable()
    {
        return walkable;
    }

}
