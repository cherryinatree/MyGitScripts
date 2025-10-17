using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkDNA 
{
    public int[] layerMap = {2,3,3,1};
    public float learningRate = 0.01f;

    public List<Matrix> weights;
    public List<Matrix> bias;
}
