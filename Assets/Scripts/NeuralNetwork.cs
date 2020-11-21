using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The brain behind our AI
public class NeuralNetwork
{
    //Determined if something fatal has occured
    public bool isOk { get; private set; } = true;

    //Perceptrons are treated as layers in the Neural Network
    //A sigmoid and unpacking operation is used to force each perceptron to be between 1 and -1 after a weight matrix modifies them
    public Jatrix[] perceptrons;
    public Jatrix[] weightMatrices;

    //Expected dimensions of the input and output matrices
    public int inputSize { get; private set; }
    public int outputSize { get; private set; }

    //New neural network constructor
    //Takes in a list of ints, dictating size of each perceptron
    public NeuralNetwork(List<int> dimensions)
    {
        //Catch if less than 2 dimensions have been provided, as no weight matrices would be generated
        if (dimensions.Count <= 1)
        {
            isOk = false;
            Debug.LogError("ERROR: INSUFFICIENT DIMENSIONS FOR NEURAL NETWORK!");
            return;
        }

        //Sets the input and output sizes to be the beginning and end of the dimension list respectively
        inputSize = dimensions[0];
        outputSize = dimensions[dimensions.Count - 1];

        //Generates a list of perceptrons and weight matrics accordingly
        perceptrons = new Jatrix[dimensions.Count - 1];
        weightMatrices = new Jatrix[dimensions.Count - 1];

        //Create a series of weight matrices, so that they can be multiplied together, to form perceptrons
        //By making the previous' width equal the next height, we can ensure that they are able to be multiplied
        for (int i = 0; i < dimensions.Count - 1; ++i)
        {
            perceptrons[i] = new Jatrix(dimensions[i + 1], 1);
            weightMatrices[i] = new Jatrix(dimensions[i + 1], dimensions[i]);
        }
    }

    //Copy constructor, creates an exact copy of a neural network
    public NeuralNetwork(NeuralNetwork copy)
    {
        inputSize = copy.inputSize;
        outputSize = copy.outputSize;

        weightMatrices = new Jatrix[copy.weightMatrices.Length];
        perceptrons = new Jatrix[copy.perceptrons.Length];

        for (int i = 0; i < weightMatrices.Length; ++i)
        {
            weightMatrices[i] = new Jatrix(copy.weightMatrices[i]);
            perceptrons[i] = new Jatrix(copy.perceptrons[i]);
        }
    }

    //Randomize the weight matrices
    public void BeatPerceptronsWithHammer(float strength)
    {
        RandomizeWeights(strength);
    }

    //Also randomize the weight matrices
    public void RandomizeWeights(float range)
    {
        for (int i = 0; i < weightMatrices.Length; ++i)
        {
            Jatrix mat = weightMatrices[i];

            for (int j = 0; j < mat.width; ++j)
            {
                for (int k = 0; k < mat.height; ++k)
                {
                    //Offset every element of the weight matrices by a given range
                    mat[j, k] += Random.Range(-range, range);
                }
            }
        }
    }

    //Backwards propagation, allowing the neural network to know which actions were bad
    public void BackPropagate(float severity, float outputDifference)
    {
        for (int i = weightMatrices.Length - 1; i >= 0; --i)
        {
            for (int j = 0; j < perceptrons[i].width; ++j)
            {
                for (int k = 0; k < weightMatrices[i].height; ++k)
                {
                    //Changes a given weight matrix value by it's corresponding perceptron value
                    //Greater perceptron values will change the weight matrix by a greater amount
                    weightMatrices[i][j, k] += perceptrons[i][j, 0] * severity * outputDifference;
                }
            }
        }
    }

    //Get outputs from inputs
    //Does the multiplication of all the weight matrices, and generates perceptrons
    public Jatrix GetOutputs(Jatrix inputs)
    {
        //Check if the network is ok before doing this
        if (!isOk)
        {
            Debug.LogError("ERROR: NEURAL NETWORK FAILED CREATION! OPERATION INVALID ON BAD NN!");
            return null;
        }

        //Check if the input size matches what's expected
        if (inputs.width != inputSize)
        {
            Debug.LogError("ERROR: INPUT SIZE DIMENSION MISMATCH!");
            return null;
        }

        //Matrix used to iterate through the weight matrices
        Jatrix combined = new Jatrix(inputs);

        for (int i = 0; i < weightMatrices.Length; ++i)
        {
            //Multiply by a weight matrix
            combined *= weightMatrices[i];

            for (int j = 0; j < combined.width; ++j)
            {
                //Shifted sigmoid, generates a value on [-1, 1] as opposed to [0, 1]
                combined[j, 0] = 2f / (1f + Mathf.Exp(-combined[j, 0])) - 1;

                //Below is the step function originally used

                //if (combined[j, 0] > 0.5f)
                //    combined[j, 0] = 1;
                //else if (combined[j, 0] < 0.5f)
                //    combined[j, 0] = -1;
                //else
                //    combined[j, 0] = 0;
            }

            //Save as a perceptron
            perceptrons[i] = new Jatrix(combined);
        }

        return combined;
    }

    //Debugging. It never served much purpose.
    public void DebugPerceptron(int number)
    {
        perceptrons[number].DebugMatrix();
    }

    public void DebugMatrix(int number)
    {
        weightMatrices[number].DebugMatrix();
    }
}
