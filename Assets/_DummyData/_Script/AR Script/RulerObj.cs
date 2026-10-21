
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RulerObj : MonoBehaviour
{
    public List<Transform> objList = new List<Transform>();
    public LineRenderer lineObj;
    public Transform textObj;
    public TextMeshPro text;
    public Transform mainCam;

    public void SetInit(Vector3 pos)
    {
        if (objList.Count < 1 || objList[0] == null)
        {
            Debug.LogError("objList[0] is not assigned or null!");
            return;
        }
        objList[0].transform.position = pos;
        lineObj.SetPosition(0, pos);
        // Debug.Log($"SetInit: Position set to {pos}");
    }

    public void SetObj(Vector3 pos)
    {
        if (objList.Count < 2 || objList[1] == null)
        {
            Debug.LogError("objList[1] is not assigned or null!");
            return;
        }
        objList[1].transform.position = pos;
        lineObj.SetPosition(1, pos);
        // Debug.Log($"SetObj: Position set to {pos}");
    }

    //private void Update()
    //{
    //    if (objList.Count < 2 || objList[0] == null || objList[1] == null)
    //    {
    //        Debug.LogWarning("objList is not properly initialized. Cannot update ruler.");
    //        return;
    //    }

    //    Vector3 tVec = objList[1].position - objList[0].position;
    //    textObj.position = objList[0].position + tVec * 0.5f;

    //    float distanceMeters = tVec.magnitude; // Distance in meters
    //    float distanceInches = distanceMeters * 39.3701f; // Convert to inches (1 meter = 39.3701 inches)
    //    float distanceFeet = distanceInches / 12f; // Convert to feet for debugging

    //    // Format distance as inches with 2 decimal places
    //    string distanceText = string.Format("{0:F2} in", distanceInches);
    //    text.text = distanceText;

    //    // Debug log to trace measurements
    //    //Debug.Log($"RulerObj Update: \n" +
    //    //          $"Point A: {objList[0].position}\n" +
    //    //          $"Point B: {objList[1].position}\n" +
    //    //          $"Distance (meters): {distanceMeters:F4}\n" +
    //    //          $"Distance (inches): {distanceInches:F2}\n" +
    //    //          $"Distance (feet): {distanceFeet:F2}");

    //    textObj.LookAt(mainCam);
    //    // Flip the text to face the camera correctly
    //    //textObj.Rotate(0, 180, 0);
    //}

    private void Update()
    {
        if (objList.Count < 2 || objList[0] == null || objList[1] == null)
        {
            Debug.LogWarning("objList is not properly initialized. Cannot update ruler.");
            return;
        }

        Vector3 start = objList[0].position;
        Vector3 end = objList[1].position;
        Vector3 tVec = end - start;
        Vector3 midPoint = start + tVec * 0.5f;

        float distanceMeters = tVec.magnitude;
        float distanceInches = distanceMeters * 39.3701f;
        string distanceText = string.Format("{0:F2} in", distanceInches);
        text.text = distanceText;

        // Determine direction
        float dx = Mathf.Abs(tVec.x);
        float dy = Mathf.Abs(tVec.y);
        float dz = Mathf.Abs(tVec.z);
        float threshold = 0.01f;

        Vector3 offset = Vector3.zero;

        if (dy > dx && dy > dz && dy > threshold)
        {
            // Vertical line – show text to the left
            offset = -mainCam.right * 0.05f;
        }
        else if ((dx > dy && dx > dz && dx > threshold) || (dz > dy && dz > dx && dz > threshold))
        {
            // Horizontal line – show text slightly below
            offset = -mainCam.up * 0.05f;
        }
        else
        {
            // Diagonal or undefined – show above the midpoint
            offset = mainCam.up * 0.05f;
        }

       // Debug.Log("--> offset : " + offset);
        textObj.position = midPoint + offset;

        // Make text face the camera
        textObj.LookAt(mainCam);
        //textObj.Rotate(0, 180, 0); // Flip to face properly
    }

}