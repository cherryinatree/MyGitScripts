using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NNManger
{

    int Inodes;
    int Hnodes;
    int Onodes;
    Perceptron[] inputNeurons = new Perceptron[2];
    Perceptron[] hiddenNeurons = new Perceptron[3];
    Perceptron[] outputNeurons = new Perceptron[1];
    List<Perceptron[]> brain = new List<Perceptron[]>();

    public void MakeNet(int Ilayer, int Hlayer, int Olayer)
    {
        Inodes = Ilayer;
        Hnodes = Hlayer;
        Onodes = Olayer;
    }
}
