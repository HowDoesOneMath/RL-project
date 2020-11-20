using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNN : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        List<int> dims = new List<int>();

        dims.Add(3);
        dims.Add(2);

        NeuralNetwork nn = new NeuralNetwork(dims);

        Jatrix entered = new Jatrix(3, 2);
        entered[0, 0] = 1;
        entered[1, 0] = 0;
        entered[2, 0] = 0;

        entered[0, 1] = 0;
        entered[1, 1] = 1;
        entered[2, 1] = -1;

        nn.BeatPerceptronsWithHammer(1);

        Jatrix result = nn.GetOutputs(entered);
        Debug.Log(result.width + ", " + result.height);

        result.DebugMatrix();

        nn.DebugPerceptron(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
