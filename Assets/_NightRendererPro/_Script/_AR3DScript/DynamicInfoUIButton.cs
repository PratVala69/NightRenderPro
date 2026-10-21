using UnityEngine;
using UnityEngine.UI;

public class DynamicInfoUIButton : MonoBehaviour
{
    private Camera mainCamera;
    public Button infoButton;
    public GameObject parentObject;

    [HideInInspector]
    public ARUIManager uiManager;

    private static DynamicInfoUIButton currentFocused;

    void Start()
    {
        mainCamera = Camera.main;
        uiManager = FindObjectOfType<ARUIManager>();

        if (infoButton != null)
            infoButton.onClick.AddListener(OnInfoClick);
        else
            Debug.LogError("❌ No Button component found on " + name);
    }

    //public void Focus()
    //{
    //    // Hide previous focused object's button
    //    if (currentFocused != null && currentFocused != this)
    //    {
    //        currentFocused.ShowInfo(false);
    //    }

    //    // Set this as the new focused object
    //    currentFocused = this;
    //    ShowInfo(true);
    //}

    //public void ShowInfo(bool show)
    //{
    //    if (infoButton != null)
    //        infoButton.gameObject.SetActive(show);
    //}

    //// Optional: If the object gets deselected externally
    //public void Unfocus()
    //{
    //    if (currentFocused == this)
    //        ShowInfo(false);
    //}


    void LateUpdate()
    {
        if (mainCamera == null)
            return;

        // Make the UI face the camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);
    }

    void OnInfoClick()
    {
        Debug.Log("ℹ️ Info clicked on: " + parentObject.name);

        // Access ARUIManager to show info or options
        
        if (uiManager != null)
        {
            uiManager.OnInfoClick(parentObject);
        }
    }
}
