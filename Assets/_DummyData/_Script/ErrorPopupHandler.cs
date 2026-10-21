using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ErrorPopupHandler : MonoBehaviour
{
    public static ErrorPopupHandler instance;

    public GameObject loadingScreen;
    public GameObject somethingWrongPopup;
    public GameObject loginAgainPopup;
    public TextMeshProUGUI errorDescText;

    [Header("Screenshot view")]
    public GameObject screenshotThumbnailScreen;
    public Image previewScreenshotImage;
    public TextMeshProUGUI screenshotName;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    public void RestartApp()
    {
        SceneManager.LoadScene(0);
    }

    public void RestartAppForLogin()
    {
        PlayerPrefs.DeleteAll();
        LoginHandler.isFirstTime = true;
        SessionDetailFetcher.isFirstOpen = true;
        SessionManager.isFirstOpen = true;
        SceneTransition.isBackFromAR = false;
        SceneManager.LoadScene(0);
    }

    public void ShowFullImage(string filepath, string imageName)
    {
        loadingScreen.SetActive(true);
        StartCoroutine(LoadFullImage(filepath, imageName));
    }

    private IEnumerator LoadFullImage(string filepath, string imageName)
    {
        //string url = LoginAPIHandler.instance.imageDownloadForARUrl + "request/" + filepath;
        string url = LoginAPIHandler.instance.imageDownloadForARUrl + "request/" + filepath;
        Debug.Log("Image URL : " + url);
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                // Sprite newSprite = Sprite.Create(downloadedTexture, new Rect(0, 0, downloadedTexture.width, downloadedTexture.height), new Vector2(0.5f, 0.5f));

                Sprite thumbnailSprite = Sprite.Create(
               downloadedTexture,
               new Rect(0, 0, downloadedTexture.width, downloadedTexture.height),
               new Vector2(0.5f, 0.5f)
           );

                previewScreenshotImage.sprite = thumbnailSprite;
                previewScreenshotImage.preserveAspect = true; // Prevent stretching

                AspectRatioFitter fitter = previewScreenshotImage.GetComponent<AspectRatioFitter>();
                if (fitter == null) fitter = previewScreenshotImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = downloadedTexture.width / (float)downloadedTexture.height;

                screenshotName.text = imageName;
                screenshotThumbnailScreen.SetActive(true); // Show the panel
                loadingScreen.SetActive(false);
            }
            else
            {
                Debug.Log("Failed to load full image: " + request.error);
            }
        }
    }
}