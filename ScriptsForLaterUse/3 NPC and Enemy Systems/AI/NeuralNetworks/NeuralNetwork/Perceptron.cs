using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Perceptron
{

    public float[] weights;
    public float[] weightsDelta;
    public float learningRate;
    private float[] input;
    public float bias;
    public float guess;



    public float Compute(float[] inputs)
    {
        input = inputs;
        float sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            sum += inputs[i] * weights[i] + bias;
        }

        float output = Sign(sum);
        guess = output;
        return output;
    }

    private float Sign(float num)
    {

        float output = 0;
        if (num >= 0)
        {
            output = 1;
        }
        else
        {
            output = -1;
        }
        return output;
    }

    public void Train(float answer)
    {
        float error = answer - guess;
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] += learningRate * error * input[i];
        }

    }

    public void RandomWights(float howMany)
    {
    // Random r = new Random();
        double random = (Random.Range(21, 25) - 10) / 10.0;
        weights = new float[2];
        for (int i = 0; i < howMany; i++)
        {
            weights[i] = (float)((Random.Range(210, 250) - 100) / 100.0);
        }
    }

}

