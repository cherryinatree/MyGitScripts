using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivationFunction
{
    public static float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    public static float ReLU(float x)
    {
        return Mathf.Max(0f, x);
    }

    public static float Tanh(float x)
    {
        return Mathf.Tan(x);
    }
}

