using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkArea : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("myCar"))
            PlayerPrefs.SetInt("carParked", 1);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("myCar"))
            PlayerPrefs.SetInt("carParked", 0);
    }
}
