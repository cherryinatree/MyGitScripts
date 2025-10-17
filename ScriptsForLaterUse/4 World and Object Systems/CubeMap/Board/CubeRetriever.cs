using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CubeRetriever
{

    public static bool CheckCubeForCharacter()
    {
        GameObject character = GetCharacterOnCursor();

        if (character != null)
        {

            return true;

        }
        else
        {
            return false;
        }
    }
    public static bool CheckCubeForCharacter(GameObject cube)
    {
        foreach (GameObject characterCheck in CombatSingleton.Instance.Combatants)
        {
            if (characterCheck.GetComponent<CombatCharacter>().myCube == cube)
            {
                return true;
            }
        }

        return false;
    }

    public static bool CheckCubeForCharacter(Cube cube)
    {
        foreach (GameObject characterCheck in CombatSingleton.Instance.Combatants)
        {
            if (characterCheck.GetComponent<CombatCharacter>().myCube == cube.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    public static bool AreCubesNotNormal()
    {
        foreach(Cube cube in CombatSingleton.Instance.Cubes)
        {
            if(cube.MyPhase != CUBEPHASE.NORMAL && cube.MyPhase != CUBEPHASE.CURSORCUBE)
            {
                return true;
            }
        }
        return false;
    }

    public static GameObject GetCharacterOnCursor()
    {
        foreach(GameObject character in CombatSingleton.Instance.Combatants)
        {
            if(character.GetComponent<CombatCharacter>().myCube.GetComponent<Cube>().MyPhase == CUBEPHASE.CURSORCUBE)
            {
                return character;
            }
        }
        return null;
    }


    public static GameObject GetCharacterOnCube(GameObject cube)
    {
        foreach (GameObject character in CombatSingleton.Instance.Combatants)
        {
            if (character.GetComponent<CombatCharacter>().myCube == cube)
            {
                return character;
            }
        }
        return null;
    }
    public static GameObject GetCharacterOnCube(Cube cube)
    {
        foreach (GameObject character in CombatSingleton.Instance.Combatants)
        {
            if (character.GetComponent<CombatCharacter>().myCube == cube.gameObject)
            {
                return character;
            }
        }
        return null;
    }

    public static GameObject FindCursorCube()
    {
        foreach (Cube cube in CombatSingleton.Instance.Cubes)
        {
            if (cube.MyPhase == CUBEPHASE.CURSORCUBE)
            {
                return cube.gameObject;
            }
        }
        return null;
    }

    public static GameObject FindCubeInDirection(GameObject start, Vector3 direction)
    {
        Collider[] tempPos1;
        Vector3 halfExtends = new Vector3(0.25f, 1.45f, 0.25f);




        tempPos1 = Physics.OverlapBox(start.transform.position + (direction), halfExtends);


        if (tempPos1.Length > 0)
        {
            for (int i = 0; i < tempPos1.Length; i++)
            {
                if (tempPos1[i].gameObject.GetComponent<Cube>())
                {
                    return tempPos1[i].gameObject;
                }
            }

            return null;
        }
        else
        { 
            return null; 
        }
    }
}
