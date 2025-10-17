using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GroundOld : MonoBehaviour
{
    public GROUNDTYPE type = GROUNDTYPE.Ground;
    public CUBEPHASE myPhase = CUBEPHASE.NORMAL;
    public CUBEPHASE previousPhase = CUBEPHASE.NORMAL;

    public bool walkable = true;


    //*********************Color Change with phases************************************
    //*********************Use CubeColor() to activate************************************
    private Renderer rend;
    private Material selected;
    private Material origin;
    private Material blue;
    private Material lightBlue;
    private Material red;
    private Material purple;
    private Material origional;


    //*********************used in the Astar Algorithm************************************
    public int G = 0;
    public int H = 0;
    public int F { get { return G + H; } }
    public GameObject previous;

    public bool traveled = false;
    //*************************************************************************************


    private void Awake()
    {

        rend = gameObject.GetComponent<Renderer>();
        origional = rend.material;
        selected = (Material)Resources.Load("Materials/Yellow", typeof(Material));
        origin = (Material)Resources.Load("Materials/Origin", typeof(Material));
        blue = (Material)Resources.Load("Materials/Blue", typeof(Material));
        lightBlue = (Material)Resources.Load("Materials/LightBlue", typeof(Material));
        red = (Material)Resources.Load("Materials/Red", typeof(Material));
        purple = (Material)Resources.Load("Materials/Purple", typeof(Material));

        //CombatSingleton.Instance.Cubes.Add(gameObject);
        //CombatSingleton.Instance.CubesGround.Add(this);

        if (myPhase == CUBEPHASE.CURSORCUBE)
        {
            CubeColor();
            CombatSingleton.Instance.CursorCube = gameObject;
        }
    }



    //*********************used in the Astar Algorithm************************************
    public void AstarReset()
    {
          G = 0;
          H = 0;

          traveled = false;

        if(myPhase != CUBEPHASE.CURSORCUBE)
        {
            myPhase = CUBEPHASE.NORMAL;
        }
        previousPhase = CUBEPHASE.NORMAL;
        CubeColor();
    }


    public void ResetSpawnCubes()
    {
        if(type == GROUNDTYPE.PlayerSpawn || type == GROUNDTYPE.EnemySpawn)
        {
            type = GROUNDTYPE.Ground;
        }
    }


    //*********************Called on by the cursor************************************
    public void BecomeCursor()
    {
        previousPhase = myPhase;
        myPhase = CUBEPHASE.CURSORCUBE;
        CubeColor();
        CombatSingleton.Instance.CursorCube = gameObject;
    }
    public void NoLongerCursor()
    {

        myPhase = previousPhase;
        CubeColor();
    }


    //*********************Call on when the cube phase is changed************************************
    public void CubeColor()
    {
        if (myPhase == CUBEPHASE.MOVE)
        {
                rend.material = blue;
        }
        else if (myPhase == CUBEPHASE.DOUBLEMOVE)
        {
                rend.material = lightBlue;
        }
        else if (myPhase == CUBEPHASE.ATTACK)
        {
                rend.material = red;
        }
        else if (myPhase == CUBEPHASE.MAGIC)
        {
                rend.material = purple;
            
        }
        else if (myPhase == CUBEPHASE.NORMAL)
        {
            rend.material = origional;
        }
        else if (myPhase == CUBEPHASE.CURSORCUBE)
        {
            rend.material = selected;
        }
        else if (myPhase == CUBEPHASE.ORIGINCUBE)
        {
            rend.material = origin;
        }

    }
}
