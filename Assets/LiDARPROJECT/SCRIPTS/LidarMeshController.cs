using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LidarMeshController : MonoBehaviour
{
    
    [SerializeField]  LayerMask _currentLayerMask;    
    [SerializeField] private LayerMask _whiteLayerMask;
    [SerializeField] private LayerMask _viewMask;

    private void Start()
    {
        _currentLayerMask = _whiteLayerMask;
    }
    
    public void SetLayer(Toggle toggle)
    {
        if (toggle.isOn)
        {
            _currentLayerMask = _viewMask;
            gameObject.layer = 0;
        }
        else
        {
            gameObject.layer = 6;
            _currentLayerMask = _whiteLayerMask;
        }
    }

    public void SetLayerONOff(bool isVisible)
    {
        if (isVisible)
        {
            _currentLayerMask = _viewMask;
            gameObject.layer = 0;
        }
        else
        {
            gameObject.layer = 6;
            _currentLayerMask = _whiteLayerMask;
        }

    }
}
