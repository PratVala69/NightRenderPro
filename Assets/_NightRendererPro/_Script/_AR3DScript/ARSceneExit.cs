using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ARSceneExit : MonoBehaviour
{
    public GameObject loadingScreen;
    public string mainSceneName = "MainUIScene"; // your main menu scene
    public string currentSceneName;

    public void OnExitButtonClicked()
    {
        StartCoroutine(ReturnToMain());
    }

    private IEnumerator ReturnToMain()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Set flag for main scene
        PlayerPrefs.SetInt("ReturnFromAR", 1);
        PlayerPrefs.Save();

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(mainSceneName);
    }

    public void Reset()
    {
        SceneManager.LoadScene(currentSceneName);
    }
}
