using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashCar : MonoBehaviour
{
    //This script is attached to 'out of bounds' colliders - usually meaning the edge of the track.
    //Touching this will automatically 'crash' the car, and halt its score.
    private void OnCollisionEnter(Collision collision)
    {
        CarController cc;

        if (collision.collider.TryGetComponent(out cc))
        {
            cc.Crashed = true;
        }
    }
}
