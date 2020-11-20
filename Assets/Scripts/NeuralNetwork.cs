using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork
{
    public bool isOk { get; private set; } = true;
    bool isDirty = false;

    Jatrix[] perceptrons;
    Jatrix combined;

    int inputSize, outputSize;

    public NeuralNetwork(List<int> dimensions)
    {
        if (dimensions.Count <= 1)
        {
            isOk = false;
            Debug.LogError("ERROR: INSUFFICIENT DIMENSIONS FOR NEURAL NETWORK!");
            return;
        }

        inputSize = dimensions[0];
        outputSize = dimensions[dimensions.Count - 1];

        perceptrons = new Jatrix[dimensions.Count - 1];

        for (int i = 0; i < dimensions.Count - 1; ++i)
        {
            perceptrons[i] = new Jatrix(dimensions[i + 1], dimensions[i]);
        }

        RecalculateCombined();
    }

    public NeuralNetwork(NeuralNetwork copy)
    {
        inputSize = copy.inputSize;
        outputSize = copy.outputSize;

        perceptrons = new Jatrix[copy.perceptrons.Length];

        for (int i = 0; i < perceptrons.Length; ++i)
        {
            perceptrons[i] = new Jatrix(copy.perceptrons[i]);
        }

        RecalculateCombined();
    }

    public void BeatPerceptronsWithHammer(float strength)
    {
        RandomizeWeights(strength);
    }

    public void RandomizeWeights(float range)
    {
        isDirty = true;

        for (int i = 0; i < perceptrons.Length; ++i)
        {
            Jatrix mat = perceptrons[i];

            for (int j = 0; j < mat.width; ++j)
            {
                for (int k = 0; k < mat.height; ++k)
                {
                    mat[j, k] += Random.Range(-range, range);
                }
            }
        }
    }

    public Jatrix GetOutputs(Jatrix inputs)
    {
        if (!isOk)
        {
            Debug.LogError("ERROR: NEURAL NETWORK FAILED CREATION! OPERATION INVALID ON BAD NN!");
            return null;
        }

        if (inputs.width != inputSize)
        {
            Debug.LogError("ERROR: INPUT SIZE DIMENSION MISMATCH!");
            return null;
        }

        if (isDirty)
        {
            RecalculateCombined();
        }

        Jatrix toRet = new Jatrix(inputs);

        toRet *= combined;

        return toRet;
    }

    void RecalculateCombined()
    {
        combined = perceptrons[0];

        for (int i = 1; i < perceptrons.Length; ++i)
        {
            combined *= perceptrons[i];
        }

        isDirty = false;
    }

    public void DebugPerceptron(int number)
    {
        perceptrons[number].DebugMatrix();
    }
}
