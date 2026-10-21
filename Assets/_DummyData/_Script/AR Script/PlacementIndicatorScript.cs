using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlacementIndicatorScript : MonoBehaviour
{
    public static PlacementIndicatorScript Instance;

    public ARPointCloudManager _ARPointCloudManager;
    private ARRaycastManager rayManager;
    public GameObject m_PlacedPrefab;
    public bool isPlaceOrMove = true;
    GameObject spawnedObject;
    //public PlaceOnPlane placeOnPlane;

    //protected override void OnPress(Vector3 position) => m_Pressed = true;

    //protected override void OnPressCancel() => m_Pressed = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        rayManager = FindObjectOfType<ARRaycastManager>();
    }

    void Update()
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        rayManager.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), hits, TrackableType.Planes);

        if (hits.Count > 0)
        {
            transform.position = hits[0].pose.position;
            transform.rotation = hits[0].pose.rotation;

            if ((Input.touchCount == 1 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                if (spawnedObject == null && isPlaceOrMove && m_PlacedPrefab != null && !IsPointerOverUIObject())
                {
                    //ARScreenUI.Instance.tapToPlacePanel.SetActive(false);
                    spawnedObject = Instantiate(m_PlacedPrefab, hits[0].pose.position, hits[0].pose.rotation);
                    spawnedObject = m_PlacedPrefab.gameObject;
                    spawnedObject.transform.position = hits[0].pose.position;
                    //spawnedObject.transform.rotation = Quaternion.Euler(hits[0].pose.rotation.x, -180f, hits[0].pose.rotation.z);
                    spawnedObject.SetActive(true);

                   // placeOnPlane.enabled = true;

                    foreach (var pointCloud in _ARPointCloudManager.trackables)
                    {
                        pointCloud.gameObject.SetActive(false);
                    }
                    Destroy(this.gameObject);
                }
            }
        }
    }
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}