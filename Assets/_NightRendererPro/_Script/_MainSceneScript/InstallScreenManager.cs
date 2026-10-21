using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InstallScreenManager : MonoBehaviour
{
    [Header("Loading Screen")]
    public GameObject loadingScreen;  // assign a panel with spinner or text

    [Header("Scene Names")]
    public string measurementScene = "MeasurementScene";
    public string placementMeshScene = "PlacementMeshScene";
    public string placementScene = "PlacementScene";
    public string viewScene = "3DViewScene";

    // Called from Button (→)
    public void OnMeasurementClicked()
    {
        StartCoroutine(LoadSceneWithLoading(measurementScene));
    }

    public void OnPlacementMeshClicked()
    {
        StartCoroutine(LoadSceneWithLoading(placementMeshScene));
    }

    public void OnPlacementClicked()
    {
        StartCoroutine(LoadSceneWithLoading(placementScene));
    }

    public void On3DViewClicked()
    {
        StartCoroutine(LoadSceneWithLoading(viewScene));
    }

    private IEnumerator LoadSceneWithLoading(string sceneName)
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        yield return new WaitForSeconds(1f); // simulate loading

        SceneManager.LoadScene(sceneName);
    }
}
