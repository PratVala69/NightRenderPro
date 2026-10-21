using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ScreenshotThumbnailUIManager : MonoBehaviour
{
    public static ScreenshotThumbnailUIManager instance;

    public GameObject thumbnailPrefab;
    public Transform container;

    public List<Screenshot> screenshotList;
    public List<ScreenshotDataAR> dateTimeList;

    public GameObject loadingScreen;

    public TextMeshProUGUI cartcount;
    public TextMeshProUGUI screenshotPanelHeaderText;

    public RulerManager rulerManager;
    public GoalManager goalManager;

    public GameObject screenshotPanelScreen;

    public GameObject screenshotThumbnailScreen;
    public Image previewScreenshotImage;
    public TextMeshProUGUI screenshotName;
    public GameObject screenshotWithPopup;
    public GameObject screenshotWithoutPopup;

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

    public void DisplayScreenshots()
    {
        if (rulerManager != null)
            screenshotPanelHeaderText.text = screenshotList.Count + " Screenshots - " + rulerManager.productDataList.Count + " Products";

        //if (goalManager != null)
        //    screenshotPanelHeaderText.text = screenshotList.Count + " Screenshots - " + goalManager.productDataList.Count + " Products";

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        screenshotPanelScreen.SetActive(true);

        for (int i = 0; i < screenshotList.Count; i++)
        {
            loadingScreen.SetActive(true);
            GameObject obj = Instantiate(thumbnailPrefab, container);
            ScreenshotThumbnailItem item = obj.GetComponent<ScreenshotThumbnailItem>();
            item.dateTime.text = dateTimeList[i].dateTime;
            item.imageName.text = dateTimeList[i].imageName;
            item.fileName = screenshotList[i].file_name;

            StartCoroutine(LoadImageFromURL(screenshotList[i].file_name, item.imageUI));
        }
    }

    IEnumerator LoadImageFromURL(string url, Image img)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(LoginAPIHandler.instance.imageDownloadForARUrl + "request/" + url))
        {
            Debug.Log("URL : " + (LoginAPIHandler.instance.imageDownloadUrl + "request/" + url));
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                int size = Mathf.Min(downloadedTexture.width, downloadedTexture.height);

                int x = (downloadedTexture.width - size) / 2;   // Center horizontally
                int y = (downloadedTexture.height - size) / 2;  // Center vertically

                Rect cropRect = new Rect(x, y, size, size);

                Sprite newSprite = Sprite.Create(
                    downloadedTexture,
                    cropRect,
                    new Vector2(0.5f, 0.5f)
                );

                img.sprite = newSprite;

                // Wait for one frame to ensure UI has updated
                yield return null;
                Debug.Log("Image downalod success");
                loadingScreen.SetActive(false);
            }
            else if (request.responseCode == 403)
            {
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
            }
            else
            {
                Debug.Log("Image downalod failed" + request.downloadHandler.text);
                loadingScreen.SetActive(false);
            }
        }
    }

    public void ShowFullImage(string filepath, string imageName)
    {
        loadingScreen.SetActive(true);
        StartCoroutine(LoadFullImage(filepath, imageName));
    }

    private IEnumerator LoadFullImage(string filepath, string imageName)
    {
        string url = LoginAPIHandler.instance.imageDownloadForARUrl + "request/" + filepath;
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
                previewScreenshotImage.preserveAspect = true;

                AspectRatioFitter fitter = previewScreenshotImage.GetComponent<AspectRatioFitter>();
                if (fitter == null) fitter = previewScreenshotImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = downloadedTexture.width / (float)downloadedTexture.height;

                screenshotName.text = imageName;
                screenshotWithPopup.SetActive(false);
                screenshotWithoutPopup.SetActive(true);
                screenshotThumbnailScreen.SetActive(true); // Show the panel
                loadingScreen.SetActive(false);
            }
            else
            {
                Debug.Log("Failed to load full image: " + request.error);
            }
        }
    }

    public void HideFullImagePanel()
    {
        GameObject fullImagePanel = GameObject.Find("FullImagePanel");
        if (fullImagePanel != null)
        {
            fullImagePanel.SetActive(false);
        }
    }
}

[System.Serializable]
public class ScreenshotDataAR
{
    public string imageName;
    public string dateTime;
}