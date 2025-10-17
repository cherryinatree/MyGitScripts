using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{

    public float howManyCubes = 20;
    public GameObject CubeSpawning;
    public GameObject Devider;
    private cubeSpawn cubes;
    public float[] weights = new float[2];
    private float[] inputs = new float[2];
    private Perceptron neuron;
    public float learningRate = 0.01f;
    public float bias = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        cubes = new cubeSpawn(CubeSpawning, howManyCubes, Devider);
        neuron = new Perceptron();
        neuron.RandomWights(1);
        neuron.learningRate = learningRate;
        neuron.bias = bias;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            foreach (GameObject aCube in cubes.cubes)
            {
                inputs[0] = aCube.transform.position.y;
               // inputs[1] = aCube.transform.position.y;
                float checkAnswer = neuron.Compute(inputs);
                if (checkAnswer == float.Parse(aCube.name))
                {
                    if (checkAnswer == 1)
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
                    neuron.Train(float.Parse(aCube.name));
                    Debug.Log(1);
                    weights = neuron.weights;
                }
            }

        }
    }
}
