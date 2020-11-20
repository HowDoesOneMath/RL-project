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

    public float distanceImportance = 0.01f;
    public float successthreshold = 5f;
    public float crashThreshold = 0.5f;
    public float turnThreshold = 0.5f;
    public float accelerationThreshold = 0.5f;

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

    public float goalDistance { get; private set; } = 0;
    float steering = 0;
    float distanceCrawled = 0;

    VQ start;
    Vector3 previousPosition;
    Vector3 deltaPos;
    float dxHit = 0;
    float dzHit = 0;
    float previousZHit = 0;
    float previousXHit = 0;

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
                accumulatedReprecussion -= distanceCovered;
            }
        }
    }

    public bool markedForDeath = false;

    bool success = false;

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

        //goal = new VQ(mainGoal.position, mainGoal.rotation);

        previousPosition = transform.position;
        rb.velocity = transform.forward * carSpeedFast;
    }

    public void InitCar(CarController toCopy)
    {
        brain = new NeuralNetwork(toCopy.brain);

        inputData = new Jatrix(toCopy.inputData);
        outputData = new Jatrix(toCopy.outputData);

        //goal = toCopy.goal;

        previousPosition = transform.position;
        rb.velocity = transform.forward * carSpeedFast;
    }

    public void ResetCar()
    {
        success = false;
        markedForDeath = false;
        Crashed = false;

        rb.angularVelocity = Vector3.zero;

        distanceCovered = 0;
        accumulatedReprecussion = 0;

        transform.position = start.position;
        transform.rotation = start.rotation;
        previousPosition = transform.position;

        rb.velocity = transform.forward * carSpeedFast;
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

        if (!PerformOutputs())
            return;
    }

    public bool CreateInputs()
    {
        int dataSlot = 0;

        deltaPos = (transform.position - previousPosition);

        previousPosition = transform.position;

        distanceCovered += deltaPos.magnitude;

        distanceCrawled = deltaPos.magnitude / (Time.fixedDeltaTime * carSpeedFast);

        float zOrientation = 0, xOrientation = 0;

        if (Physics.Raycast(forwardSensor.position, forwardSensor.forward, out hitInfo, raycastLength, raycastLayer))
        {
            zOrientation -= hitInfo.distance / raycastLength;
        }
        else
        {
            zOrientation -= 1;
        }

        if (Physics.Raycast(backwardSensor.position, backwardSensor.forward, out hitInfo, raycastLength, raycastLayer))
        {
            zOrientation += hitInfo.distance / raycastLength;
        }
        else
        {
            zOrientation += 1;
        }

        dzHit = zOrientation - previousZHit;
        previousZHit = zOrientation;

        if (Physics.Raycast(leftSensor.position, leftSensor.forward, out hitInfo, raycastLength, raycastLayer))
        {
            xOrientation -= hitInfo.distance / raycastLength;
        }
        else
        {
            xOrientation -= 1;
        }

        if (Physics.Raycast(rightSensor.position, rightSensor.forward, out hitInfo, raycastLength, raycastLayer))
        {
            xOrientation += hitInfo.distance / raycastLength;
        }
        else
        {
            xOrientation += 1;
        }

        dxHit = xOrientation - previousXHit;
        previousXHit = xOrientation;

        PackData(zOrientation, ref dataSlot);
        PackData(xOrientation, ref dataSlot);

        return true;
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

        singleReprecussion += 1 - (1 - dzHit) * (1 - dxHit);

        accumulatedReprecussion += singleReprecussion * deltaPos.magnitude;

        brain.BackPropagate(0.1f, accumulatedReprecussion);

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
            if (accelerate >= accelerationThreshold)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * carSpeedFast;
            }
            else if (accelerate <= -accelerationThreshold)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * carSpeedMedium;
            }
            else
            {
                rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * carSpeedSlow;
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
        return (distanceCovered) == (other.distanceCovered);
    }

    public int CompareTo(CarController other)
    {
        if (other == null)
            return -1;
        return -(distanceCovered).CompareTo(other.distanceCovered);
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