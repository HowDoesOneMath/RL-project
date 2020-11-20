using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public int winners = 0;
    public int totalCars = 0;

    public CarController prefab;

    public List<int> neuralNetworkShape;
    int inputSize, outputSize;

    public List<Transform> checkpoints;

    List<CarController> allCars;
    List<CarController> bestCars;

    int iteration;

    public float maxWaitTime = 30f;
    float timer = 0f;

    float bestDistance = float.MaxValue;
    float previousBest = float.MaxValue;

    private void Awake()
    {
        allCars = new List<CarController>();
        bestCars = new List<CarController>();

        inputSize = prefab.GetInputSize();
        outputSize = prefab.GetOutputSize();

        neuralNetworkShape.Insert(0, inputSize);
        neuralNetworkShape.Add(outputSize);

        StartYourEngine();
    }

    void StartYourEngine()
    {
        for (int i = 0; i < totalCars; ++i)
        {
            allCars.Add(Instantiate(prefab, transform.position, transform.rotation));

            allCars[i].InitCar(neuralNetworkShape, Random.Range(1f, 3f));
        }

        timer = maxWaitTime;
    }

    private void FixedUpdate()
    {
        bool allCrashed = true;

        for (int i = 0; i < totalCars; ++i)
        {
            if (allCars[i].Crashed)
                continue;

            allCrashed = false;
            allCars[i].PseudoFixedUpdateStep(this);
        }

        if (allCrashed)
        {
            ResetCars();
        }
        else
        {
            timer -= Time.fixedDeltaTime;

            if (timer <- 0)
            {
                ResetCars();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetCars();
        }
    }

    void ResetCars()
    {
        
        timer = maxWaitTime;
        ++iteration;

        for (int i = 0; i < allCars.Count; ++i)
        {
            allCars[i].Crashed = true;
        }

        bestCars.Clear();
        allCars.Sort();

        bestDistance = allCars[0].goalDistance;

        for (int i = 0; i < winners; ++i)
        {
            bestCars.Add(allCars[i]);
            bestCars[i].ResetCar();
        }

        for (int i = winners; i < totalCars; ++i)
        {
            allCars[i].markedForDeath = true;
            Destroy(allCars[i].gameObject);

            allCars[i] = Instantiate(prefab, transform.position, transform.rotation);
            allCars[i].InitCar(bestCars[i % winners]);

            //for (int j = 0; j < allCars[i].brain.weightMatrices.Length; ++j)
            //{
            //    allCars[i].brain.weightMatrices[j] *= 0.95f;
            //}

            allCars[i].brain.RandomizeWeights( 0.01f);
        }

        previousBest = bestDistance;
    }
}
