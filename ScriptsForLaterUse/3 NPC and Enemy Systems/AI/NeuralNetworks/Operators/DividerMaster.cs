using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DividerMaster : MonoBehaviour
{
    public int[] nnLayersSize = { 2, 3, 1 };


    public GameObject Spawn;
    public float howmany = 10;
    public GameObject min;
    public GameObject max;
    public GameObject Divider;


    public float learningRate = 0.01f;

    int guesses;
    public ObjectSpawner Spawner;

    private nnDynamicManager nn;

    protected void Start()
    {
        Spawner = new ObjectSpawner();
        Spawner.SpawnRandomPosition(Spawn, howmany, max.transform.position, min.transform.position);
        InitalizeNN();
    }

    private void InitalizeNN()
    {
        
        nn = new nnDynamicManager(nnLayersSize, learningRate);

    }

    public void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            foreach(GameObject cube in GameObject.FindGameObjectsWithTag("Cube"))
            {
                Guess(cube);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            foreach (GameObject cube in GameObject.FindGameObjectsWithTag("Cube"))
            {
                Destroy(cube);
            }
            foreach (GameObject cube in GameObject.FindGameObjectsWithTag("done"))
            {
                Destroy(cube);
            }
            Spawner.SpawnRandomPosition(Spawn, howmany, max.transform.position, min.transform.position);
        }
    }

    public void Guess(GameObject cube)
    {

        guesses = 0;
        //for (int i = 0; i < 10; i++)
        
            float[] inputs = new float[2];
            inputs[0] = cube.transform.position.y;
            inputs[1] = cube.transform.position.x;
            float[] guess = nn.FeedForward(inputs);
            float[] checkAnswer = new float[1];
            float answer = 1;

            if (cube.transform.position.x >= Divider.transform.position.x)
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
            //Debug.Log("answer: " + answer.ToString());
            //Debug.Log("guess: " + guess[0]);
            //Debug.Log("checkAnswer: " + checkAnswer[0]);


        if (checkAnswer[0] == answer)
            {
                guesses += 1;
                if (checkAnswer[0] == 1)
                    {
                    //Debug.Log("blue");
                    cube.GetComponent<Renderer>().material.color = Color.blue;
                cube.transform.tag = "done";
            }
                else
                    {
                    //Debug.Log("green");
                    cube.GetComponent<Renderer>().material.color = Color.green;
                cube.transform.tag = "done";
                    }   
            }
            else
            {
                float[] floatAnswer = new float[1];
                floatAnswer[0] = answer;
                Matrix answeres = Matrix.FromArray(floatAnswer);
               // Matrix matrixGuess = Matrix.FromArray(guess);
                Matrix input = Matrix.FromArray(inputs);
                nn.TrainNN(input, answeres);
            }
        
        //guessText.setText(guesses + "/" + rects.size() + "Guessed Correct");
    }
}
