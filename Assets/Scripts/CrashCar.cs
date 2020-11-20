using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashCar : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        CarController cc;

        if (collision.collider.TryGetComponent(out cc))
        {
            cc.Crashed = true;
        }
    }
}
