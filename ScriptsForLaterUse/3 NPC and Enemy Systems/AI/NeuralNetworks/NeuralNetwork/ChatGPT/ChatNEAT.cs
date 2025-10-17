using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ChatNEAT
{
    private int inputSize;
    private int outputSize;
    private int populationSize;
    private int maxGenerations;
    private int maxSpecies;
    private float compatibilityThreshold;
    private float compatibilityDisjointCoefficient;
    private float compatibilityExcessCoefficient;
    private float compatibilityWeightCoefficient;
    private float mutationRate;
    private float mutationPower;
    private int elitism;
    private Func<int, int, float> compatibilityFunction;
    private ActivationFunction activationFunction;
    private List<Genome> genomes;
    private int currentGeneration;
    private int currentSpecies;

    public ChatNEAT(int inputSize, int outputSize, int populationSize, int maxGenerations,
        int maxSpecies, float compatibilityThreshold, float compatibilityDisjointCoefficient,
        float compatibilityExcessCoefficient, float compatibilityWeightCoefficient, float mutationRate,
        float mutationPower, int elitism, Func<int, int, float> compatibilityFunction,
        ActivationFunction activationFunction)
    {
        this.inputSize = inputSize;
        this.outputSize = outputSize;
        this.populationSize = populationSize;
        this.maxGenerations = maxGenerations;
        this.maxSpecies = maxSpecies;
        this.compatibilityThreshold = compatibilityThreshold;
        this.compatibilityDisjointCoefficient = compatibilityDisjointCoefficient;
        this.compatibilityExcessCoefficient = compatibilityExcessCoefficient;
        this.compatibilityWeightCoefficient = compatibilityWeightCoefficient;
        this.mutationRate = mutationRate;
        this.mutationPower = mutationPower;
        this.elitism = elitism;
        this.compatibilityFunction = compatibilityFunction;
        this.activationFunction = activationFunction;
        genomes = new List<Genome>();
        currentGeneration = 1;
        currentSpecies = 1;
        InitializePopulation();
    }

    private void InitializePopulation()
    {
        for (int i = 0; i < populationSize; i++)
        {
            genomes.Add(new Genome(inputSize, outputSize, activationFunction));
        }
    }

    public void Evolve()
    {
        for (int generation = 1; generation <= maxGenerations; generation++)
        {
            Debug.Log("Generation " + generation + ":");
            EvaluateFitness();
            SortPopulationByFitness();
            Debug.Log("Best fitness: " + genomes[0].Fitness);
            Debug.Log("Species count: " + currentSpecies);
            if (generation == maxGenerations)
            {
                break;
            }

            List<Genome> newPopulation = new List<Genome>();
            int eliteCount = Mathf.RoundToInt(elitism * populationSize);
            for (int i = 0; i < eliteCount; i++)
            {
                newPopulation.Add(genomes[i].Copy());
            }

            while (newPopulation.Count < populationSize)
            {
                Genome parent1 = SelectParent();
                Genome parent2 = SelectParent();

                Genome child;
                if (parent1.Fitness > parent2.Fitness)
                {
                    child = parent1.Crossover(parent2);
                }
                else
                {
                    child = parent2.Crossover(parent1);
                }

                child.Mutate(mutationRate, mutationPower);

                if (IsCompatible(child, newPopulation))
                {
                    newPopulation.Add(child);
                }
            }

            genomes = newPopulation;
            currentGeneration++;
        }
    }

    private void EvaluateFitness()
    {
        foreach (Genome genome in genomes)
        {
            genome.Fitness = 0f;
            NeuralNetwork neuralNetwork = genome.ToNeuralNetwork();
            //
        }
    }
}