using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InstallProductHandler : MonoBehaviour
{
    public Button measurementButton;
    public Button placementButton;
    public Button nextButton;
    private bool isMeasurementSelected = false;
    private bool isPlacementSelected = false;

    public void OnMeasurementSelected()
    {
        isMeasurementSelected = true;
        isPlacementSelected = false;
        UpdateNextButtonState();
    }

    public void OnPlacementSelected()
    {
        isPlacementSelected = true;
        isMeasurementSelected = false;
        UpdateNextButtonState();
    }

    public void OnMeasurementDeselected()
    {
        isMeasurementSelected = false;
        UpdateNextButtonState();
    }

    public void OnPlacementDeselected()
    {
        isPlacementSelected = false;
        UpdateNextButtonState();
    }

    private void UpdateNextButtonState()
    {
        nextButton.interactable = isMeasurementSelected || isPlacementSelected;
    }

    public void NextButtonClick()
    {
        StartCoroutine(WaitForARScene());
    }

    IEnumerator WaitForARScene()
    {
        yield return StartCoroutine(ProductFetcher.instance.FetchProductCategories());
        yield return new WaitForSeconds(2f);

        if (isPlacementSelected)
        {
            SceneTransition.instance.Load_ARSurfaceScene();
        }
        else
        {
            SceneTransition.instance.Load_ARMeasureScene();
        }
    }
}
