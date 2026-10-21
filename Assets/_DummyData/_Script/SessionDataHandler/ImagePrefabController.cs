using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.Globalization;

public class ImagePrefabController : MonoBehaviour
{
    public Image thumbnailImage; // Image component for the thumbnail
    public TextMeshProUGUI imageNameText; // Text component for name and time
    public TextMeshProUGUI imageDateTimeText; // Text component for name and time
    public Button deleteButton; // Button for deletion
    public Button screenshotButton;

    public ScreenshotDataForProduct screenshotData; // Data associated with this image
    private Action<ScreenshotDataForProduct> onDeleteCallback; // Callback for deletion
    private Action<ScreenshotDataForDraft> onDeleteCallbackDraft; // Callback for deletion
    private SessionDetailFetcher sessionManager; // Reference to SessionManager for UI updates
    private SalesRepRequestOverviewHandler sessionOverviewManager; // Reference to SessionManager for UI updates
    private RequestEditHandler RequestEditHandler;
    public ScreenshotDataForDraft screenshotDataForDraft;

    private void Start()
    {
        // Initialize delete button listener
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        screenshotButton.onClick.AddListener(OnThumbnailClick);
    }

    private void OnThumbnailClick()
    {
        if (ErrorPopupHandler.instance != null)
        {
            if(!String.IsNullOrEmpty (screenshotData.file_name))
                ErrorPopupHandler.instance.ShowFullImage(screenshotData.file_name, screenshotData.original_file_name);
            else if (!String.IsNullOrEmpty(screenshotDataForDraft.file_name))
                ErrorPopupHandler.instance.ShowFullImage(screenshotDataForDraft.file_name, screenshotDataForDraft.original_file_name);
        }
    }

    public void Initialize(ScreenshotDataForProduct data, Action<ScreenshotDataForProduct> deleteCallback, SessionDetailFetcher manager)
    {
        screenshotData = data;
        onDeleteCallback = deleteCallback;
        sessionManager = manager;
        imageNameText.text = data.original_file_name;

        // Load and set the thumbnail image
        //if (!string.IsNullOrEmpty(screenshotData.location_key))
        //{
        //    StartCoroutine(LoadImageFromURL(screenshotData.location_key, thumbnailImage));
        //}

        Debug.LogWarning("date format: " + screenshotData.created_at + " " + screenshotData.created_at.ToString());
        // Set the text label
        if (DateTime.TryParseExact(screenshotData.created_at.ToString(), "yyyy-MM-dd'T'HH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
        {
            DateTime localTime = parsedDate.ToLocalTime();
            imageDateTimeText.text = localTime.ToString("MM-dd-yyyy HH:mm");
            Debug.LogWarning("date format: " + screenshotData.created_at);
        }
        else
        {
            imageDateTimeText.text = "Invalid Date";
            Debug.LogWarning("Invalid date format: " + screenshotData.created_at);
        }
    }

    public void InitializeForDraft(ScreenshotDataForDraft data, Action<ScreenshotDataForDraft> deleteCallback, SessionDetailFetcher manager)
    {
        screenshotDataForDraft = data;
        onDeleteCallbackDraft = deleteCallback;
        sessionManager = manager;
        imageNameText.text = data.original_file_name;

        // Load and set the thumbnail image
        //if (!string.IsNullOrEmpty(screenshotData.location_key))
        //{
        //    StartCoroutine(LoadImageFromURL(screenshotData.location_key, thumbnailImage));
        //}

        Debug.LogWarning("date format: " + data.created_at + " " + data.created_at.ToString());
        // Set the text label
        if (DateTime.TryParseExact(data.created_at.ToString(), "yyyy-MM-dd'T'HH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
        {
            DateTime localTime = parsedDate.ToLocalTime();
            imageDateTimeText.text = localTime.ToString("MM-dd-yyyy HH:mm");
            Debug.LogWarning("date format: " + data.created_at);
        }
        else
        {
            imageDateTimeText.text = "Invalid Date";
            Debug.LogWarning("Invalid date format: " + data.created_at);
        }
    }

    public void InitializeForEditRequest(ScreenshotDataForProduct data, Action<ScreenshotDataForProduct> deleteCallback, RequestEditHandler manager)
    {
        screenshotData = data;
        onDeleteCallback = deleteCallback;
        RequestEditHandler = manager;
        imageNameText.text = data.original_file_name;

        Debug.LogWarning("date format: " + data.created_at + " " + data.created_at.ToString());
        // Set the text label
        if (DateTime.TryParseExact(data.created_at.ToString(), "yyyy-MM-dd'T'HH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
        {
            DateTime localTime = parsedDate.ToLocalTime();
            imageDateTimeText.text = localTime.ToString("MM-dd-yyyy HH:mm");
            Debug.LogWarning("date format: " + data.created_at);
        }
        else
        {
            imageDateTimeText.text = "Invalid Date";
            Debug.LogWarning("Invalid date format: " + data.created_at);
        }
    }

    IEnumerator LoadImageFromURL(string url, Image img)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(LoginAPIHandler.instance.imageDownloadUrl + url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);

                Rect safeRect = new Rect(0, 0, Mathf.Min(200, downloadedTexture.width), Mathf.Min(200, downloadedTexture.height));

                Sprite newSprite = Sprite.Create(downloadedTexture, safeRect, new Vector2(0.5f, 0.5f), 100.0f);

                //Sprite newSprite = Sprite.Create(
                //    downloadedTexture,
                //    new Rect(0, 0, 200, 200),
                //    new Vector2(0.5f, 0.5f)
                //);

                img.sprite = newSprite;

                // Wait for one frame to ensure UI has updated
                yield return null;
            }
            else
            {
                ErrorPopupHandler.instance.errorDescText.text = "There is an issue with image download URL.";//"Profile photo download failed: " + request.downloadHandler.text;
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }

    public void InitializeForSalesRepRequestOverview(ScreenshotDataForProduct data, Action<ScreenshotDataForProduct> deleteCallback, SalesRepRequestOverviewHandler manager)
    {
        screenshotData = data;
        onDeleteCallback = deleteCallback;
        sessionOverviewManager = manager;
        imageNameText.text = data.original_file_name;
        deleteButton.gameObject.SetActive(false);

        Debug.LogWarning("date format: " + data.created_at + " " + data.created_at.ToString());
        // Set the text label
        if (DateTime.TryParseExact(data.created_at.ToString(), "yyyy-MM-dd'T'HH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
        {
            DateTime localTime = parsedDate.ToLocalTime();
            imageDateTimeText.text = localTime.ToString("MM-dd-yyyy HH:mm");
            Debug.LogWarning("date format: " + data.created_at);
        }
        else
        {
            imageDateTimeText.text = "Invalid Date";
            Debug.LogWarning("Invalid date format: " + data.created_at);
        }
    }
    private void OnDeleteButtonClicked()
    {
        onDeleteCallback?.Invoke(screenshotData);
        if (sessionManager != null)
        {
            sessionManager.UpdatePhotos(); // Refresh Photos panel if needed
            sessionManager.OpenImagesPanel(); // Refresh Images panel if open
        }
    }

    // Optional: Method to clean up (e.g., when destroying the prefab)
    private void OnDestroy()
    {
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
        }
    }
}