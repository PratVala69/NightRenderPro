using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightsController : MonoBehaviour
{
    [SerializeField] private Image _imageBackground;
    
    [SerializeField] private GameObject _pointLightPrefab;
    [SerializeField] private GameObject _spotLightPrefab;
    
    [SerializeField] private PlaceOnPlaneWithAnchor _placeOnPlaneWithAnchor;

    [SerializeField] private Camera _camera;
    private RaycastHit hit = new RaycastHit();
    [SerializeField] private  LayerMask _layerMaskLights;
    
    public void SetBackground(Slider slider)
    {
        Color color = new Color(_imageBackground.color.r, _imageBackground.color.g, _imageBackground.color.b,
            slider.value);
        _imageBackground.color = color;
    }

    public void DeleteLight(MyLight l)
    {
        _lights.Remove(l);
        Destroy(l.gameObject);
    }

    
    public void SetScannignMode(Toggle toggle)
    {
        if (FindAnyObjectByType<LidarMeshController>() != null)
        {
            LidarMeshController[] lidarMeshControllers = FindObjectsByType<LidarMeshController>(FindObjectsSortMode.None);
            for (int i = 0; i < lidarMeshControllers.Length; i++)
            {
                lidarMeshControllers[i].SetLayer(toggle);
            }
        }
        
    }
    

    [SerializeField] private GameObject _lightMode;

    public void OpenLightMode(bool open)
    {
        Debug.Log("OpenLightMode");
        _lightMode.SetActive(open);
        if (open)
        {
            StartModeIsLightMode();
        }
        
    }

    [SerializeField] private GameObject _mainPanel, _changeLightPanel, _addLightPanel;
    private void StartModeIsLightMode()
    {
        _mainPanel.SetActive(true);
        _changeLightPanel.SetActive(false);
        _addLightPanel.SetActive(false);
    }

    //[SerializeField] private GameObject _lightModePanel;
    //void Update()
    //{
    //   ActivateLightOnScreen();
    //}

    RaycastHit hit3 = new RaycastHit();
    private void ActivateLightOnScreen()
    {
#if UNITY_IOS && !UNITY_EDITOR      
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _layerMaskLights))
            {
                Debug.Log("Попав в об'єкт: " + hit.collider.gameObject.name);
                
                MyLight myLight = hit.collider.gameObject.GetComponent<MyLight>();
                if (myLight != null && _lights.Count > 0)
                {
                    UnAcriveAll();

                    myLight.SetLightIsActive(true);
                }
                    
                Debug.Log(hit.collider.gameObject.name);
            }
        }
#endif
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray2 = Camera.main.ScreenPointToRay(Input.mousePosition);
       
            if (Physics.Raycast(ray2, out RaycastHit hit3, Mathf.Infinity, _layerMaskLights))
            {
                MyLight myLight = hit3.collider.gameObject.GetComponent<MyLight>();
                if (myLight != null && _lights.Count > 0)
                {
                    Debug.Log(hit3.collider.gameObject.name);
                    
                    if (myLight.CanLightIsActive)
                    {
                        Debug.Log("Can Active Light");
                        UnAcriveAll();

                        myLight.SetLightIsActive(true);
                    }
                }
            }
        }
    }

    
    public void AddedNewLight(MyLight l)
    {
        _lights.Add(l);
        OpenLightMode(false);
    }


    public void OpenGloballSettings(bool open)
    {
        if (_lights.Count > 0)
        {
            foreach (MyLight lis in _lights)
            {
                lis.CanLightIsActive = !open;
            }
        }
    }
    public void UnAcriveAll()
    {
        if (_lights.Count > 0)
        {
            foreach (MyLight lis in _lights)
            {
                lis.SetLightIsActive(false);
            }
        }
    }
    private List<MyLight> _lights = new List<MyLight>();
    public void SpawnSpotLight()
    {
        UnAcriveAll();
        
        _placeOnPlaneWithAnchor.m_PlacedPrefab = _spotLightPrefab;
    }

    public void SpawnPointLight()
    {
        UnAcriveAll();
        
        _placeOnPlaneWithAnchor.m_PlacedPrefab = _pointLightPrefab;
    }
}
