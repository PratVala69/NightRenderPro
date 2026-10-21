using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThreeDUIManager : MonoBehaviour
{
    public TBOrbit tbOrbit;

    [Header("UI")]
    public GameObject popupHint;
    public GameObject hintScreen;
    public GameObject discardPopup;

    [Header("Animator")]
    public GameObject objectMenu;
    public Animator objectMenuAnimator;

    [Header("3D Objects")]
    public GameObject[] threeDObjects;

    [Header("Light")]
    public List<SelectableObject> objects = new List<SelectableObject>();
    public Button powerButton;
    public Button accentAngleButton;

    private SelectableObject currentSelected;

    [Header("Script Reference")]
    public LightOptionHandler lightOptionHandler;

    private const string FirstTimeKey = "HasSeen3DHint";


    private void Start()
    {
        CheckAndShowHint();
        SelectObject(threeDObjects[0].GetComponent<SelectableObject>());
    }


    #region DefaultUI
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

    public void ShowMenu()
    {
        objectMenu.SetActive(true);
        if (!objectMenuAnimator.GetBool("Show"))
        {
            objectMenuAnimator.SetBool("Show", true);
        }
    }

    public void HideMenu()
    {
        objectMenuAnimator.SetBool("Show", false);
    }

    #endregion


    #region ObjectSpecificUIHandler
    public void ActiveObject(int index)
    {
        for (int i = 0; i < threeDObjects.Length; i++)
        {
            threeDObjects[i].SetActive(false);
        }

        threeDObjects[index].SetActive(true);
        HideMenu();

        SelectObject(threeDObjects[index].GetComponent<SelectableObject>());
    }

    public void SelectObject(SelectableObject obj)
    {
        // Deselect all
        foreach (var item in objects)
        {
            item.SetSelected(false);
        }

        // Select the clicked object
        currentSelected = obj;
        currentSelected.SetSelected(true);
        
        ResetFunction();

        if (obj.isAccentLight)
        {
            accentAngleButton.gameObject.SetActive(true);
        }
        else
        {
            accentAngleButton.gameObject.SetActive(false);
        }

        lightOptionHandler.SelectObject(obj);
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

    public void ResetFunction()
    {
        tbOrbit.ResetPanning();
        tbOrbit.IdealYaw = 0f;
        tbOrbit.IdealPitch = 0f;
        tbOrbit.IdealDistance = 10.0f;
    }
    #endregion
}
