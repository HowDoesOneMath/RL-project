using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public int iterations = 100;

    public CarController prefab;

    List<CarController> cars;
    List<CarController> bestCars;

    public Transform goal;

    public List<int> perceptronSizes;

    public float timeUntilReset;
    float modifiedTimeUntilReset = 0;
    float timer;

    public float randomizationStrength = 1f;

    public float rewardFalloff = 0.5f;
    public float rewardThreshold = 0.00001f;

    int iteration = 0;

    private void Awake()
    {
        bestCars = new List<CarController>();
        cars = new List<CarController>();

        StartCars();

        modifiedTimeUntilReset = timeUntilReset;
        timer = modifiedTimeUntilReset;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < cars.Count; ++i)
        {
            if (cars[i].waitingForTermination)
                continue;
            cars[i].PseudoFixedUpdateStep1(this);
        }

        for (int i = 0; i < cars.Count; ++i)
        {
            if (cars[i].waitingForTermination)
                continue;
            cars[i].PseudoFixedUpdateStep2(this);
        }

        timer -= Time.fixedDeltaTime;

        if (timer <= 0)
        {
            ResetAndCullCars();

            timer = modifiedTimeUntilReset;
        }
    }

    public void StartCars()
    {
        perceptronSizes.Insert(0, prefab.GetInputSize());
        perceptronSizes.Add(prefab.GetOutputSize());

        for (int i = 0; i < iterations; ++i)
        {
            cars.Add(Instantiate(prefab, transform.position, transform.rotation));
            cars[i].InitCar(perceptronSizes, goal.position);

            cars[i].brain.RandomizeWeights(Random.Range(0, randomizationStrength));
        }
    }

    public void ResetAndCullCars()
    {
        iteration++;

        bestCars.Clear();
        cars.Sort();

        modifiedTimeUntilReset = timeUntilReset + CarController.maxDistance / prefab.carMaxVelocity;

        for (int i = 0; i < iterations / 5; ++i)
        {
            if (cars[i].currentReward + cars[i].accumulatedReward > rewardThreshold)
            {
                bestCars.Add(cars[i]);
            }
        }

        Debug.Log("Iteration: " + iteration + ". Kept cars: " + bestCars.Count);

        int iter = 0;

        for (int i = 0; i < iterations; ++i)
        {
            if (i < bestCars.Count)
            {
                cars[i] = bestCars[i];

                cars[i].transform.position = transform.position;
                cars[i].transform.rotation = transform.rotation;

                cars[i].ResetCar(rewardFalloff);
            }
            else
            {
                cars[i].waitingForTermination = true;
                Destroy(cars[i].gameObject);

                cars[i] = Instantiate(prefab, transform.position, transform.rotation);

                if (bestCars.Count > 0)
                {
                    cars[i].InitCar(bestCars[iter]);
                }
                else
                {
                    cars[i].InitCar(perceptronSizes, goal.position);
                }

                cars[i].brain.RandomizeWeights(Random.Range(0, randomizationStrength));
            }

            ++iter;

            if (iter >= bestCars.Count)
            {
                iter -= bestCars.Count;
            }
        }
    }
}
