using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    //Amount of cars/winning cars that must exist in the scene at all times.
    public int winners = 0;
    public int totalCars = 0;

    //Original blank prefab to create the first batch of AI cars
    public CarController prefab;

    //Sizes of all the hidden perceptrons.
    public List<int> neuralNetworkShape;

    //Sizes of the input and output perceptrons, as defined by the car prefab
    int inputSize, outputSize;

    //List of all active cars, as well as the few best ones which templates are made from
    List<CarController> allCars;
    List<CarController> bestCars;

    public float maxWaitTime = 30f;
    float timer = 0f;

    //Initializes variables and also adds the input and output sizes to the neural network.
    private void Awake()
    {
        allCars = new List<CarController>();
        bestCars = new List<CarController>();

        inputSize = prefab.GetInputSize();
        outputSize = prefab.GetOutputSize();

        neuralNetworkShape.Insert(0, inputSize);
        neuralNetworkShape.Add(outputSize);

        timer = maxWaitTime;

        StartYourEngine();
    }

    //Creates the first batch of cars, separated from awake for clarity reasons.
    void StartYourEngine()
    {
        for (int i = 0; i < totalCars; ++i)
        {
            allCars.Add(Instantiate(prefab, transform.position, transform.rotation));

            allCars[i].InitCar(neuralNetworkShape, 2f);
        }
    }

    //Handles all physics of every car
    //This is done in one FixedUpdate call as opposed to placing one in each of the cars
    //  to provide as little overhead as possible.
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

            if (timer <= 0)
            {
                ResetCars();
            }
        }
    }

    //Allows user to stop round early. Usually good for if a car is going in circles repeatedly.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetCars();
        }
    }

    //Function accomplishes 4 things
    // 0. reset timer
    // 1. sort the cars based on score
    // 2. remake ordered list of winners taking into account new cars
    // 3. replace all current cars with duplicates of the winners
    void ResetCars()
    {
        //Reset
        timer = maxWaitTime;

        //Immediately crash all cars, forcing them to tally score.
        for (int i = 0; i < allCars.Count; ++i)
        {
            allCars[i].Crashed = true;
        }

        //Sort
        allCars.Sort();

        //Keep winning cars
        for (int i = 0; i < winners; ++i)
        {
            if (bestCars.Count <= i || bestCars[i].score < allCars[0].score)
            {
                //Winners are kept deactivated, only there so their score/template can be used to make more cars
                bestCars.Insert(i, allCars[0]);
                allCars[0].gameObject.SetActive(false);
                allCars.RemoveAt(0);
                allCars.Add(null);

                //Trim the winner list
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
            //Above we added a null to keep the list of cars strictly equal to totalCars
            //As such, this check becomes necessary for destruction of cars.
            if (allCars[i] != null)
            {
                allCars[i].markedForDeath = true;
                Destroy(allCars[i].gameObject);
            }

            //Create new cars, initialize them to be copies of the winners
            allCars[i] = Instantiate(prefab, transform.position, transform.rotation);
            allCars[i].InitCar(bestCars[i % bestCars.Count]);

            //Quadratic variance, such that we get more cars closer to the winner but can still seek out potential new solutions
            //Goes between 0 and 1 in the shape of a quadratic curve, represents how different its children are
            float variance = ((1f * i) / totalCars);
            variance *= variance;
            allCars[i].brain.RandomizeWeights(2f * variance);
        }
    }
}
