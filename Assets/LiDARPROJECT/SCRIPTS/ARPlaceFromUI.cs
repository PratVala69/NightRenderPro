
using UnityEngine;
using UnityEngine.EventSystems; 


public class ARPlaceFromUI : MonoBehaviour, IPointerDownHandler
{

    public static bool CanPlace = false;
    private PlaceOnPlaneWithAnchor _placeOnPlaneWithAnchor;

    
    public void CanPlaceLight(bool canPlace)
    {
        CanPlace = canPlace;
        Debug.Log(canPlace);
        
    }

    private void Awake()
    {
        _placeOnPlaneWithAnchor = FindAnyObjectByType<PlaceOnPlaneWithAnchor>();
    }
  
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanPlace) return;
        
        _placeOnPlaneWithAnchor.PlaceLight(eventData.position);

        
    }

    
}
