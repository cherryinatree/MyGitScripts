using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CubeManipulator 
{

    public static void ResetAllCubes()
    {
        foreach(Cube cube in CombatSingleton.Instance.Cubes)
        {
            cube.AstarReset();
        }
    }

    public static void ChangeCursorCube(Vector3 diection)
    {


        GameObject cube = null;

        cube = CubeRetriever.FindCubeInDirection(CombatSingleton.Instance.CursorCube, diection);

        if(cube != null)
        {
            CombatSingleton.Instance.CursorCube.GetComponent<Cube>().NoLongerCursor();
            cube.GetComponent<Cube>().BecomeCursor();
            Debug.Log("found cube");
        }
        else
        {
            for (int x = -7; x < 7; x++)
            {
                for (int i = 1; i < 25; i++)
                {
                    diection.y = x;
                    if (diection.z > 0.5 || diection.z < -0.5)
                    {
                        if (diection.z < -0.5)
                        {
                            diection.z = -i;
                        }
                        else
                        {
                            diection.z = i;
                        }
                    }
                    if (diection.x > 0.5 || diection.x < -0.5)
                    {
                        if (diection.x < -0.5)
                        {
                            diection.x = -i;
                        }
                        else
                        {
                            diection.x = i;
                        }
                    }
                    
                    cube = CubeRetriever.FindCubeInDirection(CombatSingleton.Instance.CursorCube, diection);
                    if (cube != null)
                    {
                        CombatSingleton.Instance.CursorCube.GetComponent<Cube>().NoLongerCursor();
                        cube.GetComponent<Cube>().BecomeCursor();
                        break;
                    }
                }
                if (cube != null)
                {
                    break;
                }
            }
        }
       

       /* GameObject cube = CubeRetriever.FindCubeInDirection(CombatSingleton.Instance.CursorCube, diection);
        if (cube != null)
        {
            CombatSingleton.Instance.CursorCube.GetComponent<Cube>().NoLongerCursor();
            cube.GetComponent<Cube>().BecomeCursor();
        }*/
    }

}
