using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class CarController : MonoBehaviour, System.IEquatable<CarController>, System.IComparable<CarController>
{
    public float carAccelerationSpeed = 100f;
    public float carMaxVelocity = 5f;
    public float carTurnSpeed = 60f;
    public float carMidairAdjustSpeed = 5f;
    public float tireGroundedDistance = 0.1f;
    bool isGrounded = false;

    public float outOfBounds = 60f;
    public float failRadius = 6f;
    public float successthreshold = 5f;
    public float crashThreshold = 0.5f;

    public Transform[] tires;

    public float currentReward = 0;
    public float accumulatedReward = 0;
    public float jitterReduction = 1;

    public LayerMask raycastLayer;
    public float raycastLength = 50f;
    [SerializeField]
    private Transform[] raycastSensors;

    [HideInInspector]
    public NeuralNetwork brain;

    Rigidbody rb;
    MeshRenderer mr;

    RaycastHit hitInfo;

    Jatrix inputData;
    Jatrix outputData;

    float groundDistance = 0;
    //float relativeUp = 0;
    float relativeRight = 0;
    float squareDistance = 0;
    float goalDirection = 0;

    Vector3 goal;
    Vector3 start;
    Vector3 previousPosition;

    bool success = false;

    float proximity = 0;
    float distanceCovered = 0;
    public static float maxDistance = 0;

    public bool TagForDebug = false;

    public bool waitingForTermination = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();

        Material newMat = Instantiate(mr.material);
        mr.material = newMat;

        start = transform.position;
    }

    public void InitCar(List<int> neuralNetworkShape, Vector3 mainGoal)
    {
        inputData = new Jatrix(neuralNetworkShape[0], 1);

        outputData = new Jatrix(neuralNetworkShape[neuralNetworkShape.Count - 1], 1);

        brain = new NeuralNetwork(neuralNetworkShape);

        goal = mainGoal;

        currentReward = 0;
        accumulatedReward = 0;
        jitterReduction = 1;

        previousPosition = transform.position;
    }

    public void InitCar(CarController toCopy)
    {
        brain = new NeuralNetwork(toCopy.brain);

        inputData = new Jatrix(toCopy.inputData);
        outputData = new Jatrix(toCopy.outputData);

        goal = toCopy.goal;

        currentReward = 0;
        accumulatedReward = 0;
        jitterReduction = 1;

        previousPosition = transform.position;
    }

    public void ResetCar(float rewardDecay)
    {
        accumulatedReward += currentReward;
        accumulatedReward *= rewardDecay;
        currentReward = 0;
        jitterReduction = 1;

        success = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        distanceCovered = 0;
        maxDistance = 0;

        previousPosition = transform.position;
    }

    public int GetInputSize()
    {
        return raycastSensors.Length * 2 + 2;
    }

    public int GetOutputSize()
    {
        return 4;
    }

    public void PseudoFixedUpdateStep1(CarManager cm)
    {
        rb.AddForce(Vector3.down * 30f, ForceMode.Force);

        CreateInputs();

        if (TagForDebug)
        {
            string bigData = "";

            for (int i = 0; i < inputData.width; ++i)
            {
                bigData += ("\t " + inputData[i, 0]);
            }

            Debug.Log(bigData);
        }

        outputData = brain.GetOutputs(inputData);
    }

    public void PseudoFixedUpdateStep2(CarManager cm)
    {
        PerformOutputs();

        CalculateReward();
    }

    public void CreateInputs()
    {
        proximity = 1;

        if ((start - transform.position).sqrMagnitude >= failRadius * failRadius)
        {
            Vector3 deltaPos = (transform.position - previousPosition);
            distanceCovered += deltaPos.magnitude;
            
            previousPosition = transform.position;

            if (distanceCovered > maxDistance)
            {
                maxDistance = distanceCovered;
            }
        }

        int dataSlot = 0;

        for (int i = 0; i < raycastSensors.Length; ++i)
        {
            if (Physics.Raycast(raycastSensors[i].position, raycastSensors[i].forward, out hitInfo, raycastLength, raycastLayer))
            {
                groundDistance = hitInfo.distance / raycastLength;
                //relativeUp = Vector3.Dot(transform.up, hitInfo.normal);
                relativeRight = Vector3.Dot(transform.right, hitInfo.normal);
                proximity *= groundDistance;

                //if (groundDistance < crashThreshold)
                //{
                //    jitterReduction *= Mathf.Pow(0.8f, Time.fixedDeltaTime);
                //}
            }
            else
            {
                groundDistance = 1;
                //relativeUp = 1;
                relativeRight = 1;
            }

            PackData(groundDistance, ref dataSlot);
            //PackData(relativeUp, ref dataSlot);
            PackData(relativeRight, ref dataSlot);
        }

        squareDistance = (transform.position - goal).sqrMagnitude;
        goalDirection = Vector3.Dot(transform.right, (transform.position - goal).normalized);
        PackData(10f / (1f + squareDistance), ref dataSlot);
        PackData(goalDirection, ref dataSlot);
        //PackData(currentReward, ref dataSlot);
    }

    public void CalculateReward()
    {
        if (success || Mathf.Sqrt(squareDistance) < successthreshold)
        {
            success = true;
            currentReward = 1f;
        }
        else if (!isGrounded && Vector3.Dot(Vector3.up, transform.up) < 0)
            currentReward = 0;
        else if ((start - transform.position).sqrMagnitude < failRadius * failRadius)
            currentReward = 0;
        else
        {
            currentReward = Mathf.Clamp01((1f - Mathf.Sqrt(squareDistance) / outOfBounds) * distanceCovered / maxDistance * proximity);
        }

        mr.material.color = Color.green * currentReward;
    }

    public void PerformOutputs()
    {
        float accelerate = outputData[0, 0];
        float turn = outputData[1, 0];

        float accelerationThreshold = Mathf.Abs(outputData[2, 0]);
        float turnThreshold = Mathf.Abs(outputData[3, 0]);

        isGrounded = false;
        for (int i = 0; i < tires.Length; ++i)
        {
            if (Physics.Raycast(tires[i].position, -transform.up, tireGroundedDistance, raycastLayer))
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded)
        {
            if (accelerate >= accelerationThreshold)
            {
                rb.AddForce(transform.forward * carAccelerationSpeed, ForceMode.Force);
            }
            else if (accelerate <= -accelerationThreshold)
            {
                rb.AddForce(-transform.forward * carAccelerationSpeed, ForceMode.Force);
            }

            rb.velocity = Vector3.ClampMagnitude(rb.velocity, carMaxVelocity);

            float turnStrength = rb.velocity.magnitude / carMaxVelocity;
            if (!isGrounded)
                turnStrength = 0;

            if (turn >= turnThreshold)
            {
                transform.Rotate(Vector3.up, carTurnSpeed * turnStrength * Time.fixedDeltaTime, Space.Self);
            }
            else if (turn <= -turnThreshold)
            {
                transform.Rotate(Vector3.up, -carTurnSpeed * turnStrength * Time.fixedDeltaTime, Space.Self);
            }
        }
    }

    void PackData(float data, ref int dataSlot)
    {
        inputData[dataSlot, 0] = data;

        ++dataSlot;
    }

    public bool Equals(CarController other)
    {
        if (other == null)
            return false;
        return ((currentReward + accumulatedReward) * jitterReduction) == ((other.currentReward + other.accumulatedReward) * other.jitterReduction);
    }

    public int CompareTo(CarController other)
    {
        if (other == null)
            return -1;
        return -(((currentReward + accumulatedReward) * jitterReduction).CompareTo((other.currentReward + other.accumulatedReward) * other.jitterReduction));
    }
}
