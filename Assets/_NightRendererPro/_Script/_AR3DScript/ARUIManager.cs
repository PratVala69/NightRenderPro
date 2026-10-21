using UnityEngine;

public class ARUIManager : MonoBehaviour
{
    public GameObject popupHint;
    public GameObject hintScreen;
    public GameObject discardPopup;

    public string FirstTimeKey = "HasSeenARHint";

    [Header("Script Reference")]
    public GoalManager goalManager;
    public LightOptionHandler lightOptionHandler;
    public ARTemplateMenuManager templateMenuManager;
    public TweenAnimationHandler tweenAnimationHandler;

    [Header("SpawnObject Parent")]
    public Transform spawnObjectParent;

    private SelectableObject currentSelected;


    #region GenericUI
    private void OnEnable()
    {
        if(goalManager != null)
            goalManager.objectSpawnedInGoalManager += GetSpawnObject;
    }

    private void OnDisable()
    {
        if (goalManager != null)
            goalManager.objectSpawnedInGoalManager -= GetSpawnObject;
    }

    private void Start()
    {
        CheckAndShowHint();
    }

    private void CheckAndShowHint()
    {
        bool hasSeenHint = PlayerPrefs.GetInt(FirstTimeKey, 0) == 1;

        if (!hasSeenHint)
        {
            // First time user → show hint screen
            hintScreen.SetActive(true);
            popupHint.SetActive(false);

            // Save preference so next time it won't show
            PlayerPrefs.SetInt(FirstTimeKey, 1);
            PlayerPrefs.Save();
        }
        else
        {
            // Not first time → show popup hint instead
            hintScreen.SetActive(false);
            popupHint.SetActive(true);
        }
    }

    public void OnDiscardClose()
    {
        discardPopup.GetComponent<Animation>().Play("LogoutPopupHide");
    }

    public void GetSpawnObject(GameObject spawnObject)
    {
        Debug.Log("Get Spawn Object Name : " + spawnObject.name);
        var selectable = spawnObject.GetComponentInChildren<SelectableObject>(true);

        if (selectable != null)
        {
            Debug.Log("SelectableObject found on: " + selectable.name);
            Debug.Log("Number of Lights: " + selectable.lights.Length);

            // selectable.SetSelected(true);
            //lightOptionHandler.SelectObject(selectable);
            currentSelected = selectable;

            //var infoCanvas = spawnObject.GetComponentInChildren<DynamicInfoUIButton>(true);
            //infoCanvas.Focus();
            //templateMenuManager.SetFocusObject(tappedObject);
        }
        else
        {
            Debug.LogWarning("SelectableObject not found in spawned object!");
        }
    }
    #endregion

    #region ObjectSpecificUI

    GameObject infoSelectedObj;
    public void OnInfoClick(GameObject gameObject)
    {
        GameObject lastObj = gameObject; //templateMenuManager.returnFocusedObject();
        infoSelectedObj = gameObject;


        var selectable = lastObj.GetComponentInChildren<SelectableObject>();
        if (selectable != null)
        {
            Debug.Log("SelectableObject found: " + selectable.name + lastObj.name);

            lightOptionHandler.SelectObject(selectable);
            currentSelected = selectable;

            tweenAnimationHandler.ShowInfoPanel();
        }
        else
        {
            Debug.LogWarning("⚠️ No SelectableObject found on focused object!");
        }
    }

    public void OnDeleteSpecificObject()
    {
        var spawnedList = templateMenuManager.objectSpawner.objectInstantiated;
        int index = spawnedList.IndexOf(infoSelectedObj);

        if (index != -1)
        {
            currentSelected = null;
            // Remove from list and destroy the object
            spawnedList.RemoveAt(index);
            Destroy(infoSelectedObj);
            Debug.Log($"✅ Deleted focused object: {infoSelectedObj.name}");
            infoSelectedObj = null;

            tweenAnimationHandler.InfoPanel_Close();
        }
        else
        {
            // Fallback: object isn't in the list
            Debug.LogWarning($"⚠️ Focused object ({infoSelectedObj.name}) not found in objectInstantiated list!");
        }
    }

    public void OnPowerButtonClicked()
    {
        if (currentSelected != null)
        {
            currentSelected.ToggleLight();
        }
        else
        {
            Debug.Log("⚠️ No object selected!");
        }
    }

    #endregion
}

