using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeColor : MonoBehaviour
{

    //*********************Color Change with phases************************************
    //*********************Use CubeColor() to activate************************************
    private Renderer rend;
    private Material selected;
    private Material origin;
    private Material item;
    private Material capture;
    private Material special;
    private Material blue;
    private Material lightBlue;
    private Material red;
    private Material purple;
    private Material origional;



    private void Awake()
    {

        rend = gameObject.GetComponent<Renderer>();
        origional = rend.material;
        //origional = (Material)Resources.Load("Materials/Green1", typeof(Material));
        selected = (Material)Resources.Load("Materials/Yellow", typeof(Material));
        origin = (Material)Resources.Load("Materials/Origin", typeof(Material));
        item = (Material)Resources.Load("Materials/Item", typeof(Material));
        capture = (Material)Resources.Load("Materials/Capture", typeof(Material));
        special = (Material)Resources.Load("Materials/Special", typeof(Material));
        blue = (Material)Resources.Load("Materials/Blue", typeof(Material));
        lightBlue = (Material)Resources.Load("Materials/LightBlue", typeof(Material));
        red = (Material)Resources.Load("Materials/Red", typeof(Material));
        purple = (Material)Resources.Load("Materials/Purple", typeof(Material));
    }





    public void MyCubeColor(CUBEPHASE myPhase)
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
        else if (myPhase == CUBEPHASE.ITEM)
        {
            rend.material = item;
        }
        else if (myPhase == CUBEPHASE.CAPTURE)
        {
            rend.material = capture;
        }
        else if (myPhase == CUBEPHASE.SPECIAL)
        {
            rend.material = special;
        }

    }
}
