using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum SHAPE { PLUS, SQUARE, LINE, CONE}
public static class PathDisplay
{
    public static List<GameObject> MovemmentSquares(GameObject originCube, SHAPE shape, int distance, CUBEPHASE phase)
    {
        List<GameObject> actionSquares = new List<GameObject>();


        actionSquares = FindActionSquares.FindTheActionSquares(originCube, shape, distance);

        foreach (GameObject cube in actionSquares)
        {
            cube.GetComponent<Cube>().ChangeCubePhase(phase);
        }
        return actionSquares;
    }

    public static List<GameObject> AbilitySquares(GameObject originCube, Abilities ability)
    {
        List<GameObject> actionSquares = new List<GameObject>();
        List<GameObject> removeSquares = new List<GameObject>();

        CUBEPHASE phase = originCube.GetComponent<CubePhase>().StringToPhase(ability.phase);

        if (ability.isSelf)
        {
            actionSquares.Add(originCube);

        }
        else
        {

            actionSquares = FindActionSquares.FindAllActionSquares(originCube, FindActionSquares.StringToShape(ability.shape), ability.maxDistance);
            removeSquares = FindActionSquares.FindAllActionSquares(originCube, FindActionSquares.StringToShape(ability.shape), ability.minDistance);

            foreach (GameObject cube in removeSquares)
            {
                if (actionSquares.Contains(cube))
                {
                    actionSquares.Remove(cube);
                }
            }

            foreach (GameObject cube in actionSquares)
            {
                cube.GetComponent<Cube>().ChangeCubePhase(phase);
            }

            if (ability.isSingleTarget)
            {
                actionSquares = TeamSquares(actionSquares, ability.isFriendly);
            }
        }

        foreach (GameObject cube in actionSquares)
        {
            cube.GetComponent<Cube>().ChangeCubePhase(phase);
        }
        return actionSquares;
    }
    public static List<GameObject> ItemSquares(GameObject originCube)
    {

        List<GameObject> actionSquares = FindActionSquares.FindAllActionSquares(originCube, SHAPE.PLUS, 1);

        foreach (GameObject cube in actionSquares)
        {
            cube.GetComponent<Cube>().ChangeCubePhase(CUBEPHASE.ITEM);
        }
        return actionSquares;
    }
    public static List<GameObject> CaptureSquares(GameObject originCube)
    {
        List<GameObject> actionSquares = FindActionSquares.FindAllActionSquares(originCube, SHAPE.PLUS, 1);

        foreach (GameObject cube in actionSquares)
        {
            cube.GetComponent<Cube>().ChangeCubePhase(CUBEPHASE.CAPTURE);
        }
        return actionSquares;
    }

    public static List<GameObject> SpecialSquares(GameObject originCube, SHAPE shape, int distance, CUBEPHASE phase)
    {

        List<GameObject> actionSquares = FindActionSquares.FindTheActionSquares(originCube, shape, distance);

        foreach (GameObject cube in actionSquares)
        {
            cube.GetComponent<Cube>().ChangeCubePhase(phase);
        }
        return actionSquares;
    }

    private static List<GameObject> TeamSquares(List<GameObject> actionSquares, bool isReturnFriendly)
    {
        List<GameObject> updatedActionSquares = new List<GameObject>();

        foreach (GameObject cube in actionSquares)
        {
            if (cube.GetComponent<Cube>().MyType == GROUNDTYPE.Occupied)
            {
                updatedActionSquares.Add(cube);
            }
        }

        List<GameObject> removeSquares = new List<GameObject>();

        RoundsController roundsController = GameObject.Find("GameMaster").GetComponent<RoundsController>();


        foreach (GameObject character in CombatSingleton.Instance.Combatants)
        {
            if (isReturnFriendly)
            {
                if (character.GetComponent<CharacterMain>().aligence != roundsController.battleteams[roundsController.currentTeam].myTeam)
                {
                    removeSquares.Add(character.GetComponent<CombatCharacter>().myCube);
                }
            }
            else
            {

                if (character.GetComponent<CharacterMain>().aligence == roundsController.battleteams[roundsController.currentTeam].myTeam)
                {
                    removeSquares.Add(character.GetComponent<CombatCharacter>().myCube);
                }
            }
        }

        foreach (GameObject cube in removeSquares)
        {
            if (updatedActionSquares.Contains(cube))
            {
                updatedActionSquares.Remove(cube);
            }
        }

        return updatedActionSquares;
    }
   /*
    private static List<GameObject> FindActionSquares(GameObject originCube, SHAPE shape, int distance)
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

    private static List<GameObject> FindAllActionSquares(GameObject originCube, SHAPE shape, int distance)
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

    private static SHAPE StringToShape(string shape)
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
  */

}
