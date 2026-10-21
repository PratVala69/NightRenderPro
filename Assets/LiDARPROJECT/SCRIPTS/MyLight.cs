using System;
using System.Collections;
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using UnityEngine.UI;

public class MyLight : MonoBehaviour
{
    [SerializeField] private Light _light;

    [SerializeField] private GameObject _lightCanvas;

    private LeanTwistRotateAxis _rotate;
    //private LeanDragTranslate _dragTrail;
    //private LeanPinchScale _scale;
    
    private LightsController _lightsController;
    

    public float moveSpeed = 0.001f; 

    private Transform camTransform;

    private void Start()
    {
        if (Camera.main != null)
            camTransform = Camera.main.transform;
    }

    private void Update()
    {
       MovingLight();
    }

    public void OpenLightLocalSettings(bool open)
    {
        _canMoving = !open;
        _rotate.enabled = !open;

        Debug.Log("OpenLightSettingsLocal" +  open);
        
        CanLightIsActive = !open;
    }

    public bool SETTINGS_IS_OPENED;
    private void MovingLight()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (Input.touchCount == 1 && _canMoving)
        {
            Touch touch = Input.GetTouch(0);

            Debug.Log(_canMoving + " Can move: " );
            if (touch.phase == TouchPhase.Moved)
            {
                Vector3 forward = camTransform.forward;
                Vector3 right = camTransform.right;

                forward.y = 0;
                right.y = 0;

                forward.Normalize();
                right.Normalize();

                Vector3 moveDirection = (forward * touch.deltaPosition.y) + (right * touch.deltaPosition.x);

                gameObject.transform.position += moveDirection * moveSpeed;
            }
        }
#else
        if (Input.GetMouseButton(0) && _canMoving)
        {

            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            if (mouseX != 0 || mouseY != 0)
            {
                Vector3 forward = camTransform.forward;
                Vector3 right = camTransform.right;
                
                forward.y = 0;
                right.y = 0;
                
                forward.Normalize();
                right.Normalize();
                
                Vector3 moveDirection = (forward * mouseY) + (right * mouseX);
                
                gameObject.transform.position += moveDirection * moveSpeed;
        
                Debug.Log(_canMoving + " Moving with mouse. Delta: " + mouseX + ", " + mouseY);
            }
        }
#endif
    }

    private void Awake()
    {
        _lightsController = FindAnyObjectByType<LightsController>();
        _rotate = GetComponent<LeanTwistRotateAxis>();
        CanLightIsActive = true;
    }
    
    public void DeleteLight()
    {
        _lightsController.DeleteLight(this);
        _lightsController.OpenLightMode(true);
    }
    
    public void ActivateLight(Toggle toggle)
    {
        _light.enabled = toggle.isOn;
    }
    
    private bool _canMoving;
    [SerializeField] private GameObject[] _toActivateByStart, _toUnactivateByStart;

    

    public bool CanLightIsActive = true;
    public void SetLightIsActive(bool isActive)
    {

        _canMoving = isActive;
        _rotate.enabled = isActive;
        Debug.Log("SetLightIsActive=====================  " + isActive + "     cAn - " + CanLightIsActive);
        if (CanLightIsActive)
        {
            Debug.Log("SetLightIsActive 2= ====================  " + CanLightIsActive );
            _lightsController.OpenLightMode(!isActive);
            _lightCanvas.SetActive(isActive);
        
            LocalCanvasToStart();
            
        }
       
      
    }

    private void LocalCanvasToStart()
    {
        for (int i = 0; i < _toActivateByStart.Length; i++)
        {
            _toActivateByStart[i].SetActive(true);
        }

        for (int i = 0; i < _toUnactivateByStart.Length; i++)
        {
            _toUnactivateByStart[i].SetActive(false);
        }
    }
    
    
    
    public void SetLightIntensity(Slider slider)
    {
        _light.intensity = slider.value;
    }
    public void SetLightRange(Slider slider)
    {
        _light.range = slider.value;
        
    }

    public void SetLightShape(Slider slider)
    {
        _light.innerSpotAngle = slider.value;
    }
  
}
