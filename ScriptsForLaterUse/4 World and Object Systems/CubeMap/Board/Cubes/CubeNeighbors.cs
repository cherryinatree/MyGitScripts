using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CubeNeighbors : MonoBehaviour
{
    /*
    public List<GameObject> NeighborsPlus;
    public List<GameObject> NeighborsSquare;
    public List<GameObject> NeighborsCone;

    private void Start()
    {
        FindShapeStart();
    }*/


    public List<GameObject> FindNeighborCubes(SHAPE shape)
    {

        List<GameObject> actionSquares = new List<GameObject>();

        List<GameObject> neighborCubes = FindShape(shape);
        //List<Cube> ground = new List<Cube>();

      /*  foreach (GameObject neighbor in neighborCubes)
        {
            if (neighbor != null)
            {
                ground.Add(neighbor.GetComponent<Cube>());
            }
        }*/
        foreach (GameObject neighbor in neighborCubes)
        {
            if (neighbor != null)
            {
                if (CheckCube(neighbor.GetComponent<Cube>()))
                {
                    actionSquares.Add(neighbor.gameObject);
                }
            }
        }

        return actionSquares;
    }

    public List<GameObject> FindAllNeighborCubes(SHAPE shape)
    {

        List<GameObject> actionSquares = new List<GameObject>();

        List<GameObject> neighborCubes = FindShape(shape);
        //List<Cube> ground = new List<Cube>();

       /* foreach (GameObject neighbor in neighborCubes)
        {
            if (neighbor != null)
            {
                ground.Add(neighbor.GetComponent<Cube>());
            }
        }*/
        foreach (GameObject neighbor in neighborCubes)
        {
            if (neighbor != null)
            {
                if (CheckOccupiedCube(neighbor.GetComponent<Cube>()))
                {
                    actionSquares.Add(neighbor.gameObject);
                }
            }
        }

        return actionSquares;
    }

    private List<GameObject> FindShape(SHAPE shape)
    {

        List<GameObject> actionSquares = new List<GameObject>();

        actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.forward));
        if (shape == SHAPE.PLUS || shape == SHAPE.SQUARE)
        {
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.forward));
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right));
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right));
        }
        if (shape == SHAPE.SQUARE)
        {

            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right + Vector3.forward));
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right - Vector3.forward));
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right + Vector3.forward));
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right - Vector3.forward));
        }
        if (shape == SHAPE.CONE)
        {

            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right + Vector3.forward));
            actionSquares.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right + Vector3.forward));
        }





        return actionSquares;
    }
  /*  private List<GameObject> NewFindShape(SHAPE shape)
    {

        if (shape == SHAPE.PLUS)
        {
            return NeighborsPlus;
        }
        if (shape == SHAPE.SQUARE)
        {
            return NeighborsSquare;
        }





        return NeighborsCone;
    }
    private void FindShapeStart()
    {

        NeighborsPlus = new List<GameObject>();
        NeighborsSquare = new List<GameObject>();
        NeighborsCone = new List<GameObject>();

        NeighborsPlus.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.forward));
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.forward));
        NeighborsCone.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.forward));
        
        NeighborsPlus.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.forward));
        NeighborsPlus.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right));
        NeighborsPlus.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right));

        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.forward));
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right));
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right));
        
      
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right + Vector3.forward));
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right - Vector3.forward));
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right + Vector3.forward));
        NeighborsSquare.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right - Vector3.forward));
       

        NeighborsCone.Add(CubeRetriever.FindCubeInDirection(gameObject, -Vector3.right + Vector3.forward));
        NeighborsCone.Add(CubeRetriever.FindCubeInDirection(gameObject, Vector3.right + Vector3.forward));
       

    }*/

    private bool CheckCube(Cube cube)
    {
        if (cube != null)
        {
            if (cube.GetComponent<CubeAstar>().isWalkable() == true && cube.MyType == GROUNDTYPE.Ground)
            {
                return true;
            }
        }

        return false;
    }
    private bool CheckOccupiedCube(Cube cube)
    {
        if (cube != null)
        {
            if (cube.GetComponent<CubeAstar>().isWalkable() == true)
            {
                if (cube.MyType == GROUNDTYPE.Ground || cube.MyType == GROUNDTYPE.Occupied)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
