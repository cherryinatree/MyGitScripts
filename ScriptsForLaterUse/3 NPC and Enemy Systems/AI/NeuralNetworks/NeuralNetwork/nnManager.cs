using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class nnManager
{

    int Inodes;
    int Hnodes;
    int Onodes;

    Matrix weights_ih;
    Matrix weights_ho;
    Matrix bias_h;
    Matrix bias_o;

    Matrix Hidden;
    Matrix Output;

    Matrix HiddenDelta;
    Matrix OutputDelta;

    float learningRate = 0.02f;


    public nnManager(int Ilayer, int Hlayer, int Olayer)
    {

        weights_ih = new Matrix(Hlayer, Ilayer);
        weights_ho = new Matrix(Olayer, Hlayer);
        bias_h = new Matrix(Hlayer, 1);
        bias_o = new Matrix(Olayer, 1);
        weights_ih.Randomize();
        weights_ho.Randomize();
        bias_h.Randomize();
        bias_o.Randomize();

    }


    public float[] FeedForward(float[] inputs)
    {
        if (weights_ih.matrix.Length == inputs.Length)
        {
            Hidden = Matrix.ElementWiseMultiply(weights_ih, Matrix.FromArray(inputs));
        }
        else
        {

            Hidden = Matrix.DotMultiply(weights_ih, Matrix.FromArray(inputs));
        }
        //Hidden = Matrix.ElementWiseMultiply(weights_ih, Matrix.FromArray(inputs));
        Hidden.ElementWiseAdd(this.bias_h);
        Hidden.SigmoidMatrix();

        HiddenDelta = Hidden;

        //Output = Matrix.ElementWiseMultiply(weights_ho, Hidden);
        Output = weights_ho.DotMultiply(Hidden);
        Output.ElementWiseAdd(bias_o);
        Output.SigmoidMatrix();

        OutputDelta = Output;

        return Matrix.toArray(Output);
    }


    public void TrainNN(Matrix inputs, Matrix answer)
    {

        //minus the ouput from the answer
        Matrix outputErrors = Matrix.ElementWiseSubtract(answer, OutputDelta);

        Matrix gradients = Matrix.DeltaSigmoidMatrix(OutputDelta);
        gradients.ElementWiseMultiply(outputErrors);
        gradients.ScaleMultiply(learningRate);

        Matrix hidden_transpose = Matrix.Transpose(Hidden);
        Matrix weights_ho_delta = Matrix.DotMultiply(gradients, hidden_transpose);

        weights_ho.ElementWiseAdd(weights_ho_delta);
        bias_o.ElementWiseAdd(gradients);


        //==================================================================


        Matrix weights_ho_transpose = Matrix.Transpose(weights_ho);
        Matrix hidden_errors = Matrix.DotMultiply(weights_ho_transpose, outputErrors);

        Matrix hidden_gradient = Matrix.DeltaSigmoidMatrix(Hidden);
        hidden_gradient = Matrix.DotMultiply(hidden_gradient, hidden_errors);
        hidden_gradient.ScaleMultiply(learningRate);

        Matrix inputs_traspose = Matrix.Transpose(inputs);
        Matrix weights_ih_delta = Matrix.DotMultiply(hidden_gradient, inputs_traspose);

        weights_ih.ElementWiseAdd(weights_ih_delta);
        bias_h.ElementWiseAdd(hidden_gradient);





    }

    private float Sigmoid(float x)
    {
        return 1 / (1 + (-x * -x));
    }
}
