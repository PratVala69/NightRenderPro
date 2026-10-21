using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Global UI References")]
    public GameObject loadingScreen;
    public GameObject errorPopup;

    public TMP_Text errorText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (errorPopup != null) errorPopup.SetActive(false);
    }

    public void ShowLoading()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
    }


    public void HideLoading()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }


    public void ShowError(string message)
    {
        if (errorPopup != null)
        {
            if (errorText != null)
                errorText.text = message;

            errorPopup.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Error popup not assigned in UIManager!");
        }
    }

    public void HideError()
    {
        if (errorPopup != null)
            errorPopup.SetActive(false);
    }
}
