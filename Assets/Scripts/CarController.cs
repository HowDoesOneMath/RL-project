using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class CarController : MonoBehaviour, System.IEquatable<CarController>, System.IComparable<CarController>
{
    public float carAccelerationSpeed = 100f;
    public float carMaxVelocity = 5f;
    public float carTurnSpeed = 60f;
    public float tireGroundedDistance = 0.1f;
    bool isGrounded = false;

    public float distanceImportance = 0.01f;
    public float successthreshold = 5f;
    public float crashThreshold = 0.5f;
    public float turnThreshold = 0.5f;
    public float accelerationThreshold = 0.5f;

    public Transform[] groundRaycast;

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
    float squareInverseDistance = 0;
    float goalDirection = 0;
    float distanceCrawled = 0;

    Vector3 goal;
    Vector3 start;
    Vector3 previousPosition;

    bool success = false;

    float distanceCovered = 0;

    public bool TagForDebug = false;

    public bool waitingForTermination = false;

    float problemAmount = 0;

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

        previousPosition = transform.position;
        rb.velocity = transform.forward * carMaxVelocity;
    }

    public void InitCar(CarController toCopy)
    {
        brain = new NeuralNetwork(toCopy.brain);

        inputData = new Jatrix(toCopy.inputData);
        outputData = new Jatrix(toCopy.outputData);

        goal = toCopy.goal;

        previousPosition = transform.position;
        rb.velocity = transform.forward * carMaxVelocity;
    }

    public void ResetCar(float rewardDecay)
    {
        success = false;

        rb.velocity = transform.forward * carMaxVelocity;
        rb.angularVelocity = Vector3.zero;

        distanceCovered = 0;

        previousPosition = transform.position;
    }

    public int GetInputSize()
    {
        return raycastSensors.Length * 2 + 3;
    }

    public int GetOutputSize()
    {
        return 2;
    }

    public void PseudoFixedUpdateStep1(CarManager cm)
    {
        if (!CalculateReward())
            return;

        rb.AddForce(Vector3.down * 30f, ForceMode.Force);

        if (!CreateInputs())
            return;

        if (TagForDebug)
        {

            brain.DebugMatrix(0);

            string bigData = "";

            for (int i = 0; i < brain.perceptrons.Length; ++i)
            {
                bigData += "\n";
                for (int j = 0; j < brain.perceptrons[i].width; ++j)
                {
                    bigData += ("\t " + brain.perceptrons[i][j, 0]);
                }
            }

            Debug.Log(bigData);
        }

        outputData = brain.GetOutputs(inputData);
    }

    public void PseudoFixedUpdateStep2(CarManager cm)
    {
        if (!PerformOutputs())
            return;
    }

    public bool CreateInputs()
    {
        Vector3 deltaPos = (transform.position - previousPosition);

        previousPosition = transform.position;

        distanceCrawled = deltaPos.magnitude / (Time.fixedDeltaTime * carMaxVelocity);

        int dataSlot = 0;

        for (int i = 0; i < raycastSensors.Length; ++i)
        {
            if (Physics.Raycast(raycastSensors[i].position, raycastSensors[i].forward, out hitInfo, raycastLength, raycastLayer))
            {
                groundDistance = hitInfo.distance / raycastLength;
                //relativeUp = Vector3.Dot(transform.up, hitInfo.normal);
                relativeRight = Vector3.Dot(transform.right, hitInfo.normal);

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

        squareInverseDistance = (transform.position - goal).sqrMagnitude;
        squareInverseDistance = 1f / (1f + squareInverseDistance);

        goalDirection = Vector3.Dot(transform.right, (transform.position - goal).normalized);

        PackData(squareInverseDistance, ref dataSlot);
        PackData(goalDirection, ref dataSlot);
        PackData(distanceCrawled, ref dataSlot);
        //PackData(currentReward, ref dataSlot);

        return true;
    }

    public bool CalculateReward()
    {
        problemAmount = 0f;

        for (int i = 0; i < inputData.width; ++i)
        {
            problemAmount += Mathf.Exp(-inputData[i, 0] * inputData[i, 0]);
        }

        problemAmount /= inputData.width;

        brain.BackPropagate(0.1f, problemAmount);

        return true;
    }

    public bool PerformOutputs()
    {
        float accelerate = outputData[0, 0];
        float turn = outputData[1, 0];

        //float accelerationThreshold = Mathf.Abs(outputData[2, 0]);
        //float turnThreshold = Mathf.Abs(outputData[3, 0]);

        isGrounded = false;
        for (int i = 0; i < groundRaycast.Length; ++i)
        {
            if (Physics.Raycast(groundRaycast[i].position, -transform.up, tireGroundedDistance, raycastLayer))
            {
                isGrounded = true;
                break;
            }
        }

        //Debug.Log(isGrounded);

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

            float turnStrength = 1f;
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

        return true;
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
        return (problemAmount) == (other.problemAmount);
    }

    public int CompareTo(CarController other)
    {
        if (other == null)
            return 1;
        return (problemAmount).CompareTo(problemAmount);
    }
}
