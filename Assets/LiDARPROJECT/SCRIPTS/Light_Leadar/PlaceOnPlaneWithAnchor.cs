using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceOnPlaneWithAnchor : MonoBehaviour
{
    
    public GameObject m_PlacedPrefab;

    private ARRaycastManager m_RaycastManager;
    private ARAnchorManager m_AnchorManager; // Змінна для Anchor Manager
    private List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();
    private GameObject m_SpawnedObject;
    private LightsController _lightsController;
    
    private ARPlaneManager _planeManager;

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        m_AnchorManager = GetComponent<ARAnchorManager>(); 
        _lightsController = FindObjectOfType<LightsController>();
        _planeManager = GetComponent<ARPlaneManager>();
    }
    
    private MyLight  _currentLight;
    

    public LayerMask raycastLayerMask;
    public void PlaceLight(Vector2 touchPosition)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (m_RaycastManager.Raycast(touchPosition, m_Hits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in  m_Hits )
            {
                
                ARPlane plane = _planeManager.GetPlane(hit.trackableId);
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    
                    //m_SpawnedObject = Instantiate(m_PlacedPrefab, hit.pose.position, hit.pose.rotation);
                    //_currentLight = m_SpawnedObject.GetComponent<MyLight>();
                    //_lightsController.AddedNewLight(_currentLight);
                    
                    
                    
                    if (m_AnchorManager != null)
                    {
                        
                        ARAnchor anchor = m_AnchorManager.AttachAnchor(
                            hit.trackable.GetComponent<ARPlane>(),
                            hit.pose
                        );

                        
                        //if (anchor != null)
                        //{
                            
                        //    m_SpawnedObject.transform.SetParent(anchor.transform);
                        //}

                        
                        ARPlaceFromUI.CanPlace = false;
                        break;
                    }
                    
                }
            }
        }
#else
        Ray ray = Camera.main.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            m_SpawnedObject = Instantiate(
                m_PlacedPrefab,
                hit.point,
                Quaternion.identity
            );

            _currentLight = m_SpawnedObject.GetComponent<MyLight>();
            _lightsController.AddedNewLight(_currentLight);

            ARPlaceFromUI.CanPlace = false;
        }
#endif       
    }

}
