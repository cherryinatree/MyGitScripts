using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nnTypeController 
{
  
    public enum NETWORKTYPE { NakedNetwork = 0, NEAT = 1 };
    private NETWORKTYPE networkType;
    private nnDynamicManager brain;


    public nnTypeController(NETWORKTYPE type)
    {
        networkType = type;
    }
       
}
