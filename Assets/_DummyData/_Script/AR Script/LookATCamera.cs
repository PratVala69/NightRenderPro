using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookATCamera : MonoBehaviour
{
    //private Transform cameraTransform;
    public Transform cameraTransform;

    private void Start()
    {
        // Find the main camera in the scene
        // cameraTransform = Camera.main.transform;

        // Check if a camera is found
        if (cameraTransform == null)
        {
            Debug.LogError("Main camera not found in the scene!");
        }
    }

    private void Update()
    {
        // Make the object look at the camera
        if (cameraTransform != null)
        {
            transform.LookAt(cameraTransform);
        }
    }
}
