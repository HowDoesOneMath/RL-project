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

    public float maxWaitTime = 30f;
    float timer = 0f;

    public float crashSeverity;

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

            allCars[i].InitCar(neuralNetworkShape, 2f);
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

        for (int i = 0; i < allCars.Count; ++i)
        {
            allCars[i].Crashed = true;
        }

        allCars.Sort();

        for (int i = 0; i < winners; ++i)
        {
            if (bestCars.Count <= i || bestCars[i].score < allCars[0].score)
            {
                bestCars.Insert(i, allCars[0]);
                allCars[0].gameObject.SetActive(false);
                allCars.RemoveAt(0);
                allCars.Add(null);

                if (bestCars.Count > winners)
                {
                    CarController cc = bestCars[bestCars.Count - 1];
                    bestCars.RemoveAt(bestCars.Count - 1);

                    Destroy(cc.gameObject);
                }
            }
        }

        for (int i = 0; i < totalCars; ++i)
        {
            if (allCars[i] != null)
            {
                allCars[i].markedForDeath = true;
                Destroy(allCars[i].gameObject);
            }

            allCars[i] = Instantiate(prefab, transform.position, transform.rotation);
            allCars[i].InitCar(bestCars[i % bestCars.Count]);

            float variance = ((1f * i) / totalCars);
            variance *= variance;
            allCars[i].brain.RandomizeWeights(2f * variance);
        }
    }
}
