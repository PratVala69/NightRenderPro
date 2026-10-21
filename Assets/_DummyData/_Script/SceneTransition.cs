using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    public string MainScene;
    public string ARMeasure;
    public string ARSurface;

    public static bool isBackFromAR = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Prevent destruction on scene load
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public void Load_ARSurfaceScene()
    {
        SceneManager.LoadScene(ARSurface);
        isBackFromAR = true;
    }

    public void Load_ARMeasureScene()
    {
        SceneManager.LoadScene(ARMeasure);
        isBackFromAR = true;
    }

    public void Load_MainScene()
    {
        SceneManager.LoadScene(MainScene);
    }
}
