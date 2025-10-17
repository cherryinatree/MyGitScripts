using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{

    private CubePhase cubePhase;
    private CubeColor cubeColor; 
    private CubeNeighbors cubeNeighbors;
    private CubeAstar cubeAstar;

    private static bool newRound = false;

    // Start is called before the first frame update
    void Awake()
    {
        cubePhase = GetComponent<CubePhase>();
        cubeColor = GetComponent<CubeColor>();
        cubeNeighbors = GetComponent<CubeNeighbors>();
        cubeAstar = GetComponent<CubeAstar>();

        //CombatSingleton.Instance.Cubes.Add(gameObject);
        if (!newRound) 
        { 
            CombatSingleton.Instance.Cubes.Clear(); 
            newRound = true;
        }
        CombatSingleton.Instance.Cubes.Add(this);
    }

    private void Start()
    {
        if (cubePhase.myPhase == CUBEPHASE.CURSORCUBE)
        {
            CombatSingleton.Instance.CursorCube = gameObject;
            cubeColor.MyCubeColor(cubePhase.myPhase);
        }
        newRound = false;
    }

    public void AstarReset()
    {
        cubeAstar.AstarReset();
        cubePhase.AstarPhaseReset();
        cubeColor.MyCubeColor(cubePhase.myPhase);
    }

    public void ResetSpawnCubes()
    {
        cubePhase.ResetSpawnCubes();
    }


    //*********************Called on by the cursor************************************
    public void BecomeCursor()
    {
        cubePhase.BecomeCursor();
        cubeColor.MyCubeColor(MyPhase);
        CombatSingleton.Instance.CursorCube = gameObject;
    }
    public void NoLongerCursor()
    {
        cubePhase.PreviousPhase();
        cubeColor.MyCubeColor(MyPhase);
    }


    public void ChangeCubePhase(CUBEPHASE phase)
    {
        if(MyPhase == CUBEPHASE.CURSORCUBE)
        {
            PreviousPhase = phase;
        }
        else
        {

            MyPhase = phase;
        }
        cubeColor.MyCubeColor(MyPhase);
    }








    /****************************************************
     * 
     * 
     *         Ease of Access Get Set Variables
     * 
     * 
     * **************************************************/
    public CUBEPHASE MyPhase
    {
        get { return cubePhase.myPhase; }
        set { cubePhase.myPhase = value; }
    }
    public CUBEPHASE PreviousPhase
    {
        get { return cubePhase.previousPhase; }
        set { cubePhase.previousPhase = value; }
    }
    public GROUNDTYPE MyType
    {
        get { return cubePhase.type; }
        set { cubePhase.type = value; }
    }


}
