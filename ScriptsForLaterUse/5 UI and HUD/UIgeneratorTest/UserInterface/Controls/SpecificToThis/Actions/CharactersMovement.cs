using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CharactersMovement
{
    List<GameObject> path = new List<GameObject>();
    public float speed = 4;
    private bool isMoving = false;
    private bool firstMove = true;

    static GameObject cube;


    public void CursorCheck()
    {
        if (CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().myCube != CombatSingleton.Instance.CursorCube)
        {
            CombatSingleton.Instance.CursorCube.GetComponent<Cube>().NoLongerCursor();
            CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().myCube.GetComponent<Cube>().BecomeCursor();
        }


    }
    public void Movement(Vector3 direction)
    {

        if (cube == null)
        {
            cube = CubeRetriever.FindCubeInDirection(
                CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().myCube, direction);

            Debug.Log(cube);
        }

    }

    public void MoveCharacter()
    {
        Debug.Log(cube);
        if (cube != null)
        {
            Debug.Log("CursorCheck1" + cube);
            if (CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().myCube != cube)
            {
                Debug.Log("CursorCheck2" + cube);
                move();
            }
        }
    }


    private void move()
    {



        float stepp = speed * Time.deltaTime;
        float yIndex = cube.transform.position.y + ContantCharacterPosition.Y_addedToCubePosition;

        Transform character = CombatSingleton.Instance.Combatants[0].transform;

        if (firstMove)
        {
            firstMove = false;
            character.gameObject.GetComponent<CombatCharacter>().FaceDirection(faceThisWay(
                character.gameObject.GetComponent<CombatCharacter>().myCube.transform.position, cube.transform.position));
        }
        character.gameObject.GetComponent<Animator>().SetTrigger("Moving");


        Vector3 pathV3 = new Vector3(cube.transform.position.x, character.position.y, cube.transform.position.z);
        character.position = Vector3.MoveTowards(character.position, pathV3, stepp);
        character.position = new Vector3(character.position.x, yIndex, character.position.z);


        Vector2 characterV2 = new Vector2(character.position.x, character.position.z);
        Vector2 pathV2 = new Vector2(cube.transform.position.x, cube.transform.position.z);



        if (Vector2.Distance(characterV2, pathV2) < 0.0001f)
        {
            
            //character.gameObject.GetComponent<Animator>().ResetTrigger("Moving");
            //PositionCharacterOnCube();
            isMoving = false;
            firstMove = true;
            CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().myCube = cube;
            CombatSingleton.Instance.CursorCube.GetComponent<Cube>().NoLongerCursor();
            CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().myCube.GetComponent<Cube>().BecomeCursor();
            cube = null;
        }

    }




    private void PositionCharacterOnCube()
    {
        float yIndex = cube.transform.position.y + 0.5f;
        CombatSingleton.Instance.Combatants[0].transform.position =
            new Vector3(cube.transform.position.x, yIndex, cube.transform.position.z);
        CombatSingleton.Instance.Combatants[0].GetComponent<CombatCharacter>().NewCube(cube);
    }

    private float faceThisWay(Vector3 start, Vector3 end)
    {
        start.y = 0;
        end.y = 0;
        Vector3 dir = (start - end).normalized;

        float direction = 0;
        if (dir.x == 1)
        {
            direction = 270;
        }
        else if (dir.x == -1)
        {

            direction = 90;
        }
        else if (dir.z == 1)
        {

            direction = 180;
        }
        else if (dir.z == -1)
        {

            direction = 0;
        }

        return direction;
    }

}
