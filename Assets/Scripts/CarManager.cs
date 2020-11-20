using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public List<int> winnerCopies;
    int totalCars = 0;

    public CarController prefab;

    List<CarController> cars;
    List<CarController> bestCars;

    public Transform goal;

    public List<int> perceptronSizes;

    public float initialTimeUntilReset;
    float timer;

    public float randomizationStrength = 1f;

    public float rewardFalloff = 0.5f;
    public float rewardThreshold = 0.00001f;

    int iteration = 0;

    private void Awake()
    {
        bestCars = new List<CarController>();
        cars = new List<CarController>();

        for (int i = 0; i < winnerCopies.Count; ++i)
        {
            ++totalCars;
            totalCars += winnerCopies[i];
        }

        StartCars();

        timer = initialTimeUntilReset;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < cars.Count; ++i)
        {
            if (cars[i].waitingForTermination)
            {
                continue;
            }
            cars[i].PseudoFixedUpdateStep1(this);
        }

        for (int i = 0; i < cars.Count; ++i)
        {
            if (cars[i].waitingForTermination)
            {
                continue;
            }
            cars[i].PseudoFixedUpdateStep2(this);
        }

        //timer -= Time.fixedDeltaTime;
        //
        //if (timer <= 0)
        //{
        //    ResetAndCullCars();
        //
        //    timer = Mathf.Min(maximumTimeUntilReset, modifiedTimeUntilReset);
        //}
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            ResetAndCullCars();
        }
    }

    public void StartCars()
    {
        perceptronSizes.Insert(0, prefab.GetInputSize());
        perceptronSizes.Add(prefab.GetOutputSize());

        for (int i = 0; i < totalCars; ++i)
        {
            cars.Add(Instantiate(prefab, transform.position, transform.rotation));
            cars[i].InitCar(perceptronSizes, goal.position);

            cars[i].brain.RandomizeWeights(randomizationStrength);
        }
    }

    public void ResetAndCullCars()
    {
        iteration++;

        for (int i = 0; i < cars.Count; ++i)
        {
            cars[i].transform.position = transform.position;
            cars[i].transform.rotation = transform.rotation;
            cars[i].ResetCar(1);
        }

        return;

        bestCars.Clear();
        cars.Sort();

        for (int i = 0; i < winnerCopies.Count; ++i)
        {
            bestCars.Add(cars[i]);

            cars[i].transform.position = transform.position;
            cars[i].transform.rotation = transform.rotation;

            cars[i].ResetCar(rewardFalloff);
        }

        Debug.Log("Iteration: " + iteration + ". Kept cars: " + bestCars.Count);

        int copyNumber = 0;
        int iter = 0;

        for (int i = winnerCopies.Count; i < totalCars; ++i)
        {
            //Debug.Log("Making copy: " + iter + ", " + copyNumber);

            cars[i].waitingForTermination = true;
            Destroy(cars[i].gameObject);

            cars[i] = Instantiate(prefab, transform.position, transform.rotation);

            if (bestCars.Count > 0)
            {
                cars[i].InitCar(bestCars[copyNumber]);
            }
            else
            {
                cars[i].InitCar(perceptronSizes, goal.position);
            }

            cars[i].brain.RandomizeWeights(randomizationStrength);

            ++iter;

            if (iter >= winnerCopies[copyNumber])
            {
                iter = 0;
                ++copyNumber;
            }
        }
    }
}
