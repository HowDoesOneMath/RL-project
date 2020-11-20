using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuralNetwork
{
    public bool isOk { get; private set; } = true;
    //bool isDirty = false;

    public Jatrix[] perceptrons;
    public Jatrix[] weightMatrices;
    //Jatrix combined;

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
        weightMatrices = new Jatrix[dimensions.Count - 1];

        for (int i = 0; i < dimensions.Count - 1; ++i)
        {
            perceptrons[i] = new Jatrix(dimensions[i + 1], 1);
            weightMatrices[i] = new Jatrix(dimensions[i + 1], dimensions[i]);
        }
    }

    public NeuralNetwork(NeuralNetwork copy)
    {
        inputSize = copy.inputSize;
        outputSize = copy.outputSize;

        weightMatrices = new Jatrix[copy.weightMatrices.Length];

        for (int i = 0; i < weightMatrices.Length; ++i)
        {
            weightMatrices[i] = new Jatrix(copy.weightMatrices[i]);
        }
    }

    public void BeatPerceptronsWithHammer(float strength)
    {
        RandomizeWeights(strength);
    }

    public void RandomizeWeights(float range)
    {
        for (int i = 0; i < weightMatrices.Length; ++i)
        {
            Jatrix mat = weightMatrices[i];

            for (int j = 0; j < mat.width; ++j)
            {
                for (int k = 0; k < mat.height; ++k)
                {
                    mat[j, k] += Random.Range(-range, range);
                }
            }
        }
    }

    public void BackPropagate(float severity, float outputDifference)
    {
        for (int i = weightMatrices.Length - 1; i >= 0; --i)
        {
            for (int j = 0; j < perceptrons[i].width; ++j)
            {
                for (int k = 0; k < weightMatrices[i].height; ++k)
                {
                    weightMatrices[i][j, k] -= perceptrons[i][j, 0] * severity * outputDifference;
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

        Jatrix combined = new Jatrix(inputs);

        for (int i = 0; i < weightMatrices.Length; ++i)
        {
            combined *= weightMatrices[i];

            for (int j = 0; j < combined.width; ++j)
            {
                //combined[j, 0] = 2f / (1f + Mathf.Exp(-combined[j, 0])) - 1;
                if (combined[j, 0] > 0.5f)
                    combined[j, 0] = 1;
                else if (combined[j, 0] < 0.5f)
                    combined[j, 0] = -1;
                else
                    combined[j, 0] = 0;
            }

            perceptrons[i] = new Jatrix(combined);
        }

        return combined;
    }

    public void DebugPerceptron(int number)
    {
        perceptrons[number].DebugMatrix();
    }

    public void DebugMatrix(int number)
    {
        weightMatrices[number].DebugMatrix();
    }
}
