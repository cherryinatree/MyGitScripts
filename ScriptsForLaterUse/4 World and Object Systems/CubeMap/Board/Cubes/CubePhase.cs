using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GROUNDTYPE { PlayerSpawn, EnemySpawn, Ground, Obstacle, Occupied }
public enum CUBEPHASE { NORMAL, MOVE, DOUBLEMOVE, ATTACK, MAGIC, CURSORCUBE, ORIGINCUBE, ITEM, SPECIAL, CAPTURE }
public enum GROUNDMODIFY { Normal = 0, Hazard = -20, Heal = 10, Buff = 20 };

public class CubePhase : MonoBehaviour
{
    public GROUNDTYPE type = GROUNDTYPE.Ground;
    public CUBEPHASE myPhase = CUBEPHASE.NORMAL;
    public CUBEPHASE previousPhase = CUBEPHASE.NORMAL;
    public GROUNDMODIFY groundModify = GROUNDMODIFY.Normal;



    public void ResetSpawnCubes()
    {
        if (type == GROUNDTYPE.PlayerSpawn || type == GROUNDTYPE.EnemySpawn)
        {
            type = GROUNDTYPE.Ground;
        }
    }

    public void AstarPhaseReset()
    {

        
       // if (myPhase != CUBEPHASE.CURSORCUBE)
       if(gameObject != CombatSingleton.Instance.CursorCube)
        {
            myPhase = CUBEPHASE.NORMAL;
        }
        previousPhase = CUBEPHASE.NORMAL;
    }
    public void BecomeCursor()
    {
        if (CombatSingleton.Instance.actionData.Preview == PREVIEWMODE.ActionMulti)
        {  
            if (Astar.WithAbilityRange(false))
            {
                previousPhase = StringToPhase(CombatSingleton.Instance.actionData.ChosenAbility.phase);
            }
            else
            {
                CurrentToPreviousForCursor();
            }
           
        }
        else
        {
            CurrentToPreviousForCursor();
        }
        myPhase = CUBEPHASE.CURSORCUBE;
    }



    private void CurrentToPreviousForCursor()
    {
        if(myPhase != CUBEPHASE.CURSORCUBE)
        {

            previousPhase = myPhase;
        }
    }


    public void PreviousPhase()
    {
        myPhase = previousPhase;
    }
    public CUBEPHASE StringToPhase(string phase)
    {

        CUBEPHASE shape2 = CUBEPHASE.NORMAL;
        switch (phase)
        {
            case ("attack"):
                shape2 = CUBEPHASE.ATTACK;
                break;
            case ("magic"):
                shape2 = CUBEPHASE.MAGIC;
                break;
        }
        return shape2;
    }
}
