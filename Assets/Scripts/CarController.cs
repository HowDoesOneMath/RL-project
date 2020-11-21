using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class CarController : MonoBehaviour, System.IEquatable<CarController>, System.IComparable<CarController>
{
    //public float carAccelerationSpeed = 100f;
    public float carSpeedFast = 5f;
    public float carSpeedMedium = 3f;
    public float carSpeedSlow = 2f;
    public float carTurnSpeed = 60f;
    public float tireGroundedDistance = 0.1f;
    bool isGrounded = false;

    public float score { get; set; } = 0;
    public float turnThreshold = 0.5f;
    public float deaccelerationThreshold = 0.5f;

    public Transform[] groundRaycast;

    public LayerMask raycastLayer;
    public float raycastLength = 50f;

    [SerializeField]
    private Transform forwardSensor;
    [SerializeField]
    private Transform backwardSensor;
    [SerializeField]
    private Transform leftSensor;
    [SerializeField]
    private Transform rightSensor;

    [HideInInspector]
    public NeuralNetwork brain;

    Rigidbody rb;
    MeshRenderer mr;

    RaycastHit hitInfo;

    Jatrix inputData;
    Jatrix outputData;

    VQ start;
    Vector3 previousPosition;
    Vector3 deltaPos;

    bool crashed = false;
    public bool Crashed
    {
        get { return crashed; }
        set
        {
            crashed = value;
            rb.isKinematic = crashed;

            if (crashed)
            {
                score = distanceCovered;
                brain.BackPropagate(0.1f, accumulatedReprecussion);
            }
        }
    }

    public bool markedForDeath = false;

    public float distanceCovered { get; private set; } = 0;

    public bool TagForDebug = false;

    public bool waitingForTermination = false;

    float singleReprecussion = 0;
    public float accumulatedReprecussion = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();

        Material newMat = Instantiate(mr.material);
        mr.material = newMat;

        start = new VQ(transform.position, transform.rotation);
    }

    public void InitCar(List<int> neuralNetworkShape, float randomizationStrength)
    {
        inputData = new Jatrix(neuralNetworkShape[0], 1);

        outputData = new Jatrix(neuralNetworkShape[neuralNetworkShape.Count - 1], 1);

        brain = new NeuralNetwork(neuralNetworkShape);
        brain.BeatPerceptronsWithHammer(randomizationStrength);

        previousPosition = transform.position;
        rb.velocity = transform.forward * carSpeedMedium;
    }

    public void InitCar(CarController toCopy)
    {
        brain = new NeuralNetwork(toCopy.brain);

        inputData = new Jatrix(toCopy.inputData);
        outputData = new Jatrix(toCopy.outputData);

        previousPosition = transform.position;
        rb.velocity = transform.forward * carSpeedMedium;
    }

    public void ResetCar()
    {
        markedForDeath = false;
        Crashed = false;

        rb.angularVelocity = Vector3.zero;

        distanceCovered = 0;
        accumulatedReprecussion = 0;

        transform.position = start.position;
        transform.rotation = start.rotation;
        previousPosition = transform.position;

        rb.velocity = transform.forward * carSpeedMedium;
    }

    public int GetInputSize()
    {
        return 2;
    }

    public int GetOutputSize()
    {
        return 2;
    }

    public void PseudoFixedUpdateStep(CarManager cm)
    {
        if (!CalculateReward())
            return;

        rb.AddForce(Vector3.down * 30f, ForceMode.Force);

        if (!CreateInputs())
            return;

        if (TagForDebug)
        {
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

        if (!PerformOutputs())
            return;
    }

    public bool CreateInputs()
    {
        int dataSlot = 0;

        deltaPos = (transform.position - previousPosition);

        previousPosition = transform.position;

        distanceCovered += deltaPos.magnitude;

        float zOrientation = 0, xOrientation = 0;

        CheckRaycast(forwardSensor, backwardSensor, ref zOrientation);

        CheckRaycast(leftSensor, rightSensor, ref xOrientation);

        PackData(zOrientation, ref dataSlot);
        PackData(xOrientation, ref dataSlot);

        return true;
    }

    void CheckRaycast(Transform sensorNegative, Transform sensorPositive, ref float orientation)
    {
        if (Physics.SphereCast(sensorNegative.position, 1f, sensorNegative.forward, out hitInfo, raycastLength, raycastLayer))
        {
            orientation -= hitInfo.distance / raycastLength;
        }
        else
        {
            orientation -= 1;
        }

        if (Physics.SphereCast(sensorPositive.position, 1f, sensorPositive.forward, out hitInfo, raycastLength, raycastLayer))
        {
            orientation += hitInfo.distance / raycastLength;
        }
        else
        {
            orientation += 1;
        }
    }

    public bool CalculateReward()
    {
        if (markedForDeath)
            return false;

        if (Crashed)
        {
            return false;
        }

        singleReprecussion = 0f;

        if (Mathf.Abs(inputData[0, 0]) > Mathf.Abs(inputData[1, 0]))
        {
            singleReprecussion -= inputData[0, 0];
        }
        else
        {
            singleReprecussion -= inputData[1, 0];
        }

        accumulatedReprecussion *= Mathf.Pow(0.1f, Time.fixedDeltaTime);
        accumulatedReprecussion += singleReprecussion;

        brain.BackPropagate(0.01f, accumulatedReprecussion);

        return true;
    }

    public bool PerformOutputs()
    {
        float accelerate = outputData[0, 0];
        float turn = outputData[1, 0];

        isGrounded = false;
        for (int i = 0; i < groundRaycast.Length; ++i)
        {
            if (Physics.Raycast(groundRaycast[i].position, -transform.up, tireGroundedDistance, raycastLayer))
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded)
        {
            if (accelerate >= deaccelerationThreshold)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * carSpeedSlow;
            }
            else if (accelerate <= -deaccelerationThreshold)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * carSpeedMedium;
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * carSpeedFast;
            }

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
        else if (transform.up.y < 0)
        {
            Crashed = true;
            return false;
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
        return (score) == (other.score);
    }

    public int CompareTo(CarController other)
    {
        if (other == null)
            return -1;
        return -(score).CompareTo(other.score);
    }
}

public struct VQ
{
    public VQ(Vector3 p, Quaternion q)
    {
        position = p;
        rotation = q;
    }

    public Vector3 position;
    public Quaternion rotation;
}