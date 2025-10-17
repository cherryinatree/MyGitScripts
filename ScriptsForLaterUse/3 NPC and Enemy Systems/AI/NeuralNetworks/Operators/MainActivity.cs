using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainActivity {

public float[] weights = new float[2];
private float[] inputs = new float[2];
private Perceptron neuron;
public float learningRate = 0.01f;
public float bias = 0.1f;
public static Canvas canvas;
int guesses;

Animation animFadeIn;
Animation animFadeOut;
private nnManager nn;

    protected void onCreate()
{

    neuron = new Perceptron();
    neuron.RandomWights(1);
    neuron.learningRate = learningRate;
    neuron.bias = bias;

    InitalizeNN();
}

private void InitalizeNN()
{
    nn = new nnManager(2, 2, 1);

}



    public void Guess()
    {

        guesses = 0;
        //ArrayList<Rect> rects = myCanvas.rects;
        for (int i = 0; i<10; i++) {
            float[] inputs = new float[2];
           // inputs[0] = rect.bottom;
            //inputs[1] = rect.top;
            float[] guess = nn.FeedForward(inputs);
            float[] checkAnswer = new float[1];
            float answer = 1;

            if (true)
            {
                answer = 1;
            }
            else
            {
                answer = 0;
            }
            if (guess[0] >= 0.5)
            {
                checkAnswer[0] = 1;
            }
            else
            {
                checkAnswer[0] = 0;
            }
            Debug.Log("answer: " + answer.ToString());
            Debug.Log("guess: " + guess[0]);
            Debug.Log("checkAnswer: " + checkAnswer[0]);


            if (checkAnswer[0] == answer)
            {
                guesses += 1;
                if (guess[0] == 1)
                {

                }
                else
                {

                }
            }
            else
            {
                float[] floatAnswer = new float[1];
                floatAnswer[0] = answer;
                Matrix answeres = Matrix.FromArray(floatAnswer);
                Matrix matrixGuess = Matrix.FromArray(guess);
                Matrix input = Matrix.FromArray(inputs);
                nn.TrainNN(input, answeres);
            }
        }
        //guessText.setText(guesses + "/" + rects.size() + "Guessed Correct");
    }
/*
        if (Input.GetKey(KeyCode.Alpha1))
        {
            foreach (GameObject aCube in cubes.cubes)
            {
                inputs[0] = aCube.transform.position.y;
                inputs[1] = aCube.transform.position.x;
                float[] guess = nn.FeedForward(inputs);
                // inputs[1] = aCube.transform.position.y;
                float[] checkAnswer = new float[1];
                if (guess[0] >= 0.5f)
                {

                    checkAnswer[0] = 1;
                }
                else { checkAnswer[0] = -1; }
                //float[] checkAnswer = neuron.Sign(guess,0);
                if (checkAnswer[0] == float.Parse(aCube.name))
                {
                    if (checkAnswer[0] == 1)
                    {
                        aCube.GetComponent<Renderer>().material.color = Color.red;
                    }
                    else
                    {

                        aCube.GetComponent<Renderer>().material.color = Color.green;
                    }
                }
                else
                {
                    float[] floatAnswer = new float[1];
                    floatAnswer[0] = float.Parse(aCube.name);
                    if (floatAnswer[0] == -1)
                    {
                        floatAnswer[0] = 0;
                    }
                    Matrix answer = Matrix.FromArray(floatAnswer);
                    Matrix matrixGuess = Matrix.FromArray(guess);
                    Matrix input = Matrix.FromArray(inputs);
                    nn.TrainNN(input, answer);

                    //nn.TrainIH(float.Parse(aCube.name), guess[0], inputs);
                    // weights = neuron.weights;
                }
            }
        }*/
    }
