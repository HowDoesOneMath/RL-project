using System.Collections.Generic;
using UnityEngine;

//Must have body and renderer
//Carries the IEquatable and IComparable interfaces to allow for List.sort to work
[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class CarController : MonoBehaviour, System.IEquatable<CarController>, System.IComparable<CarController>
{
    //Car speeds, 3 different states controllable by the neural network
    public float carSpeedFast = 5f;
    public float carSpeedMedium = 3f;
    public float carSpeedSlow = 2f;

    //Car turn speed for if the matrix says to turn
    public float carTurnSpeed = 60f;

    //Determines if the car is on the ground
    public float tireGroundedDistance = 0.1f;
    bool isGrounded = false;

    //Final score, calculated once the car crashes
    public float score { get; set; } = 0;

    //Thresholds for the neural network output
    public float turnThreshold = 0.5f;
    public float deaccelerationThreshold = 0.5f;

    //Array of transforms, each representing a position and direction to perform a raycast
    //Allows visualization of where the raycasts are pointed without having to create a Gizmos function
    public Transform[] groundRaycast;

    //Selectively ignore certain layers when casting.
    public LayerMask raycastLayer;
    
    //Max length for all raycasts.
    public float raycastLength = 50f;

    //Four sensors dictating the four directions we want to check for proximity to obstacles.
    [SerializeField]
    private Transform forwardSensor;
    [SerializeField]
    private Transform backwardSensor;
    [SerializeField]
    private Transform leftSensor;
    [SerializeField]
    private Transform rightSensor;

    //The brain, the neural network
    //One day, we'll know what it's thinking... one day...
    //(See the class for more details)
    [HideInInspector]
    public NeuralNetwork brain;

    //references to components for caching purposes
    Rigidbody rb;
    MeshRenderer mr;

    //Cached raycast hit info
    RaycastHit hitInfo;

    //Matrices representing input and output data, to feed to the neural network
    //Both have thickness of 1
    Jatrix inputData;
    Jatrix outputData;

    //Position and Rotation in one struct
    VQ start;

    //Used for checking a change in position
    Vector3 previousPosition;
    Vector3 deltaPos;

    //Determines if the car has failed
    //Score is instantly calculated upon this being set
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
                //score is simply the distance the car went for, attempts to make it more complex have led to poor results
                //brain backward propagates upon crashing, equal to its reprecussion
                score = distanceCovered;
                brain.BackPropagate(0.1f, accumulatedReprecussion);
            }
        }
    }

    //Used to avoid any potential issues where a script tries to access a destroyed object
    public bool markedForDeath = false;

    //Distance that the car has travelled
    public float distanceCovered { get; private set; } = 0;

    //Allows debugging, for our own purposes
    public bool TagForDebug = false;

    //Used to determine how angry we are that the car did what it did
    float singleReprecussion = 0;
    public float accumulatedReprecussion = 0;

    //Initialization. Material is duplicated due to previous implementation - currently serves no purpose.
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();

        Material newMat = Instantiate(mr.material);
        mr.material = newMat;

        start = new VQ(transform.position, transform.rotation);
    }

    //Non-Copy initialization of cars.
    //Forms input/output matrices as well as a neural network to compute them.
    //Also randomizes them slightly.
    public void InitCar(List<int> neuralNetworkShape, float randomizationStrength)
    {
        inputData = new Jatrix(neuralNetworkShape[0], 1);

        outputData = new Jatrix(neuralNetworkShape[neuralNetworkShape.Count - 1], 1);

        brain = new NeuralNetwork(neuralNetworkShape);

        //Alternate name for the weight randomization function. Same purpose.
        brain.BeatPerceptronsWithHammer(randomizationStrength);

        previousPosition = transform.position;
        rb.velocity = transform.forward * carSpeedMedium;
    }

    //Copy initialization.
    //Abuses the copy constructors of other classes to create a duplicate of the original matrix
    //No randomization here, for the sake of consistency.
    public void InitCar(CarController toCopy)
    {
        brain = new NeuralNetwork(toCopy.brain);

        inputData = new Jatrix(toCopy.inputData);
        outputData = new Jatrix(toCopy.outputData);

        previousPosition = transform.position;
        rb.velocity = transform.forward * carSpeedMedium;
    }

    //Originally used for testing, to make sure the AI was capable of modifying its behaviour
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

    //Width of the input matrix
    public int GetInputSize()
    {
        return 2;
    }

    //Width of the output matrix
    public int GetOutputSize()
    {
        return 2;
    }

    //Pseudo fixed update, used to offer more control of what executes and when
    //Originally was split up into several functions, to test them individually
    //Parts of it can return false to indicate something is wrong OR the car has crashed.
    public void PseudoFixedUpdateStep(CarManager cm)
    {
        if (!CalculateReward())
            return;

        //Gravity, to offer us more control should we want it
        rb.AddForce(Vector3.down * 30f, ForceMode.Force);

        if (!CreateInputs())
            return;

        //Prints the values of the perceptrons
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

        //Calculate output data from input data
        outputData = brain.GetOutputs(inputData);

        if (!PerformOutputs())
            return;
    }

    //Initializes data that's necessary to make a decision
    public bool CreateInputs()
    {
        int dataSlot = 0;

        deltaPos = (transform.position - previousPosition);

        previousPosition = transform.position;

        distanceCovered += deltaPos.magnitude;

        //Represents how close to either edge the car is
        float zOrientation = 0, xOrientation = 0;

        //Check opposing raycasts, to minimize data that has to be sent to the neural network
        CheckRaycast(forwardSensor, backwardSensor, ref zOrientation);
        CheckRaycast(leftSensor, rightSensor, ref xOrientation);

        //Put data into the inputData matrix
        PackData(zOrientation, ref dataSlot);
        PackData(xOrientation, ref dataSlot);

        return true;
    }

    //Performs opposing raycasts, creating a variable between 1 and -1 dictating how close to an edge of the track the car is
    void CheckRaycast(Transform sensorNegative, Transform sensorPositive, ref float orientation)
    {
        //Spherecast is used instead of raycast to 'cast a bigger net', and find obstacles more successfully
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

        //Assuming the car is centered, +1 and -1 will cancel out.
    }

    //Calculates the reward/punishment for the car
    public bool CalculateReward()
    {
        //Do not run this if it is in the process of getting destroyed
        if (markedForDeath)
            return false;

        //Also do not run this if crashed.
        if (Crashed)
        {
            return false;
        }

        //Used to determine how bad the car did this frame
        singleReprecussion = 0f;

        //The only two pieces of sensory data are the values between +1/-1 dictating how centered it is on the track.
        //The greater magnitude of the two is used to dictate how poor the car did.
        if (Mathf.Abs(inputData[0, 0]) > Mathf.Abs(inputData[1, 0]))
        {
            singleReprecussion -= inputData[0, 0];
        }
        else
        {
            singleReprecussion -= inputData[1, 0];
        }

        //Accumulated reprecussion is continuously reduced, to allow cars with smarter
        //  neural networks that have stayed away to not be mutated as much
        accumulatedReprecussion *= Mathf.Pow(0.1f, Time.fixedDeltaTime);
        accumulatedReprecussion += singleReprecussion;

        //Backwards propagation, to force the neural network to change itself slightly if it gave a poor output
        brain.BackPropagate(0.01f, accumulatedReprecussion);

        return true;
    }

    //Act on the outputs of the brain
    //Forces the car to speed up, slow down, or turn
    //Also calculates if the car is grounded to avoid it moving in the air.
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

            if (turn >= turnThreshold)
            {
                transform.Rotate(Vector3.up, carTurnSpeed * Time.fixedDeltaTime, Space.Self);
            }
            else if (turn <= -turnThreshold)
            {
                transform.Rotate(Vector3.up, -carTurnSpeed * Time.fixedDeltaTime, Space.Self);
            }
        }
        //If the car is upside down it is assumed crashed.
        else if (transform.up.y < 0)
        {
            Crashed = true;
            return false;
        }

        return true;
    }

    //The PackData function attributes every value to a separate slot in the input matrix
    void PackData(float data, ref int dataSlot)
    {
        inputData[dataSlot, 0] = data;

        ++dataSlot;
    }

    //The below two checks are mandated by IEquatable and IComparison
    //Used to sort the list by score
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

//Position and Rotation struct, just to group them
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