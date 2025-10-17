using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FindActionSquares
{

    public static List<GameObject> FindTheActionSquares(GameObject originCube, SHAPE shape, int distance)
    {

        List<GameObject> actionSquares = originCube.GetComponent<CubeNeighbors>().FindNeighborCubes(shape);
        if (shape == SHAPE.CONE)
        {
            actionSquares = originCube.GetComponent<CubeNeighbors>().FindNeighborCubes(SHAPE.LINE);
        }

        for (int i = 1; i < distance; i++)
        {
            List<GameObject> actionSquares2 = new List<GameObject>();
            foreach (GameObject cube in actionSquares)
            {
                foreach (GameObject cube2 in cube.GetComponent<CubeNeighbors>().FindNeighborCubes(shape))
                {
                    actionSquares2.Add(cube2);
                }

            }
            foreach (GameObject cube2 in actionSquares2)
            {
                if (!actionSquares.Contains(cube2))
                {
                    actionSquares.Add(cube2);
                }
            }
        }
        return actionSquares;
    }

    public static List<GameObject> FindAllActionSquares(GameObject originCube, SHAPE shape, int distance)
    {
        if (distance > 0)
        {
            List<GameObject> actionSquares = originCube.GetComponent<CubeNeighbors>().FindAllNeighborCubes(shape);

            if (shape == SHAPE.CONE)
            {
                actionSquares = originCube.GetComponent<CubeNeighbors>().FindAllNeighborCubes(SHAPE.LINE);
            }

            for (int i = 1; i < distance; i++)
            {
                List<GameObject> actionSquares2 = new List<GameObject>();

                foreach (GameObject cube in actionSquares)
                {
                    foreach (GameObject cube2 in cube.GetComponent<CubeNeighbors>().FindAllNeighborCubes(shape))
                    {
                        actionSquares2.Add(cube2);
                    }

                }

                foreach (GameObject cube2 in actionSquares2)
                {
                    if (!actionSquares.Contains(cube2))
                    {
                        actionSquares.Add(cube2);
                    }
                }
            }

            return actionSquares;
        }
        else
        {
            return new List<GameObject>();
        }
    }

    public static SHAPE StringToShape(string shape)
    {
        SHAPE shape2 = SHAPE.PLUS;
        switch (shape)
        {
            case ("plus"):
                shape2 = SHAPE.PLUS;
                break;
            case ("line"):
                shape2 = SHAPE.LINE;
                break;
            case ("cone"):
                shape2 = SHAPE.CONE;
                break;
            case ("square"):
                shape2 = SHAPE.SQUARE;
                break;
        }
        return shape2;
    }

}
