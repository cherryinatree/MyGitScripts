using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionsOutOfCombat
{

    private CharactersMovement CharactersMovement;
    public void Key(string key)
    {
        if(CharactersMovement == null)
        {
            CharactersMovement = new CharactersMovement();
        }
        switch (key)
        {

            //*********************** Movement Buttons ************************

            case "MoveUp":
                //Movement(Camera.main.transform.forward);
                Movement(Camera.main.GetComponent<CameraFacing>().FaceWhichWay(key));
                break;

            case "MoveDown":
                //Movement(-Camera.main.transform.forward);
                Movement(Camera.main.GetComponent<CameraFacing>().FaceWhichWay(key));
                break;

            case "MoveLeft":
                //Movement(-Camera.main.transform.right);
                Movement(Camera.main.GetComponent<CameraFacing>().FaceWhichWay(key));
                break;

            case "MoveRight":
                //Movement(Camera.main.transform.right);
                Movement(Camera.main.GetComponent<CameraFacing>().FaceWhichWay(key));
                break;

            //*********************** Exit and Select ************************
            case "Back":
                //EscapeButton();
                break;

            case "Jump":

                if (CombatSingleton.Instance.battleSystem.isKeyboardControl)
                {
                    //selectKey.SelectCube();
                }
                break;


            case "Cycle":
                CycleThroughCharacters();
                break;


            //*********************** Test Buttons ************************
            case "testTurnChange":
                CombatSingleton.Instance.battleSystem.TurnChange();
                break;
        }
    }


    private void Movement(Vector3 direction)
    {
        Debug.Log(CombatSingleton.Instance.battleSystem.State);
        if (CombatSingleton.Instance.battleSystem.State == BATTLESTATE.OUTOFCOMBAT)
        {
            CharactersMovement.Movement(direction);
        }
    }
    private void CycleThroughCharacters()
    {
        Debug.Log("Cycle");
        List<GameObject> CycleFocusGroup = new List<GameObject>();
        foreach (GameObject ally in CombatSingleton.Instance.Combatants)
        {
            if (ally.GetComponent<CombatCharacter>().team == 0 && ally.GetComponent<CombatCharacter>().myStats.actionsRemaining > 0)
            {
                CycleFocusGroup.Add(ally);
            }
        }
        int focus = 0;
        for (int i = 0; i < CycleFocusGroup.Count; i++)
        {
            if (CycleFocusGroup[i] == CombatSingleton.Instance.FocusCharacter)
            {
                focus = i;
                break;
            }
        }
        if (focus == CycleFocusGroup.Count - 1)
        {
            focus = 0;
        }
        else
        {
            focus++;
        }
        CombatSingleton.Instance.FocusCharacter = CycleFocusGroup[focus];

        CombatSingleton.Instance.CursorCube.GetComponent<Cube>().NoLongerCursor();
        CombatSingleton.Instance.FocusCharacter.GetComponent<CombatCharacter>().myCube.GetComponent<Cube>().BecomeCursor();
    }

    
}
