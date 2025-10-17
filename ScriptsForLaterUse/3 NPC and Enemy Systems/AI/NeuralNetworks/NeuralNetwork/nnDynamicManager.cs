using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nnDynamicManager : MonoBehaviour
{


    List<Matrix> weights;
    List<Matrix> bias;

    Matrix[] Layers;
    Matrix[] LayersDelta;

    float learningRate = 0.02f;


    public nnDynamicManager(int[] layerMap, float lr)
    {
        weights = new List<Matrix>();
        bias = new List<Matrix>();
        Layers = new Matrix[layerMap.Length-1];
        LayersDelta = new Matrix[layerMap.Length-1];
        learningRate = lr;

        for (int i = 0; i < layerMap.Length-1; i++)
        {

            weights.Add(new Matrix(layerMap[i + 1], layerMap[i]));

        }

        for (int i = 1; i < layerMap.Length; i++)
        {

            bias.Add(new Matrix(layerMap[i], 1));
        }
        foreach (var layer in weights)
        {
            layer.Randomize();
        }
        foreach (var layer in bias)
        {
            layer.Randomize();
        }

    }

    /*
    public nnDynamicManager(int[] layerMap, float lr, Matrix cloneWeight, Matrix cloneBias)
    {
        weights = new List<Matrix>();
        bias = new List<Matrix>();
        Layers = new Matrix[layerMap.Length - 1];
        LayersDelta = new Matrix[layerMap.Length - 1];
        learningRate = lr;

        for (int i = 0; i < layerMap.Length - 1; i++)
        {

            weights.Add(new Matrix(layerMap[i + 1], layerMap[i]));

        }

        for (int i = 1; i < layerMap.Length; i++)
        {

            bias.Add(new Matrix(layerMap[i], 1));
        }
        foreach (var layer in weights)
        {
            layer.Randomize();
        }
        foreach (var layer in bias)
        {
            layer.Randomize();
        }

    }
    */

    public float[] FeedForward(float[] inputs)
    {
        for (int i = 0; i < weights.Count; i++)
        {
            if (i == 0)
            {
                if (weights[i].matrix.Length == inputs.Length)
                {
                    Layers[i] = Matrix.ElementWiseMultiply(weights[i], Matrix.FromArray(inputs));
                }
                else
                {
                    Layers[i] = Matrix.DotMultiply(weights[i], Matrix.FromArray(inputs));
                }
            }
            else
            {
                Layers[i] = Matrix.DotMultiply(weights[i], Layers[i-1]);
            }

            Layers[i].ElementWiseAdd(bias[i]);
            Layers[i].SigmoidMatrix();
            LayersDelta[i] = Layers[i];

        }

        return Matrix.toArray(Layers[Layers.Length - 1]);
    }


    public void TrainNN(Matrix inputs, Matrix answer)
    {


        Matrix outputErrors = Matrix.ElementWiseSubtract(answer, LayersDelta[LayersDelta.Length - 1]);


        Matrix gradientsO = Matrix.DeltaSigmoidMatrix(LayersDelta[LayersDelta.Length - 1]);
        gradientsO.ElementWiseMultiply(outputErrors);
        gradientsO.ScaleMultiply(learningRate);

        Matrix transposeO = Matrix.Transpose(Layers[Layers.Length-2]);
        Matrix weightsdeltaO = Matrix.DotMultiply(gradientsO, transposeO);

        weights[weights.Count - 1].ElementWiseAdd(weightsdeltaO);
        bias[bias.Count - 1].ElementWiseAdd(gradientsO);
        //=================================================================

        Matrix weights_ho_transpose;
        Matrix hidden_errors = outputErrors;

        Matrix gradients;
        Matrix transpose;
        Matrix weightsdelta;

        for (int i = Layers.Length - 1; i > 0; i--)
        {

            weights_ho_transpose = Matrix.Transpose(weights[i]);
            hidden_errors = Matrix.DotMultiply(weights_ho_transpose, hidden_errors);

            gradients = Matrix.DeltaSigmoidMatrix(Layers[i - 1]);
            gradients = Matrix.DotMultiply(gradients, hidden_errors);
            gradients.ScaleMultiply(learningRate);
            Debug.Log("i: "+i);
            if(i ==1)
            {

                transpose = Matrix.Transpose(inputs);
            }
            else
            {

                transpose = Matrix.Transpose(Layers[i - 2]);
            }
            weightsdelta = Matrix.DotMultiply(gradients, transpose);

            weights[i-1].ElementWiseAdd(weightsdelta);
            bias[i - 1].ElementWiseAdd(gradients);

        }
    }

}
