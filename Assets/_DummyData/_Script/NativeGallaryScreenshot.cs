using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NativeGallaryScreenshot : MonoBehaviour
{
    public GameObject saveScreenshotPopup;
    public Image previewScreenshotImage;
    public TextMeshProUGUI screenshotName;
    private string thumbnailPath;
    public string screenshotNameString;
    public string screenshotDateTimeString;
    public GameObject screenshotThumbnailScreen;

    public string imageUploadEndpoint = "image-management/uploadMultipleFile";
    public string moduleName = "request"; // or "logo"

    [Space(10)]
    public UpdateProfileAPIHandler updateProfileAPIHandler;

    [Header("For AR")]
    public RulerManager rulerManager;
    //public GoalManager goalManager;
    public GameObject loadingScreen;
    public ScreenshotThumbnailUIManager screenshotThumbnailUIManager;
    public GameObject pivotOriginPrefab;
    public GameObject screenshotWithPopup;
    public GameObject screenshotWithoutPopup;

    public void ScreenshotAndSave()
    {
        StartCoroutine(TakeScreenshotAndSave());
    }

    private async void RequestPermissionAsynchronously(NativeGallery.PermissionType permissionType, NativeGallery.MediaType mediaTypes)
    {
        NativeGallery.Permission permission = await NativeGallery.RequestPermissionAsync(permissionType, mediaTypes);
        Debug.Log("Permission result: " + permission);
    }

    private IEnumerator TakeScreenshotAndSave()
    {
        yield return new WaitForEndOfFrame();

        if (pivotOriginPrefab != null)
        {
            pivotOriginPrefab.SetActive(false);
        }

        // Get the main camera
        Camera camera = Camera.main;

        // Create a RenderTexture with camera's viewport size
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = rt;

        // Render the camera's view
        camera.Render();

        // Create a Texture2D to store the camera capture
        Texture2D cameraCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Set the RenderTexture as active and read pixels
        RenderTexture.active = rt;
        cameraCapture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        cameraCapture.Apply();

        // Reset camera settings
        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        if (pivotOriginPrefab != null)
        {
            pivotOriginPrefab.SetActive(true);
        }

        screenshotNameString = FetchProductCategoryDataForAR.instance.selectedProduct + " " + System.DateTime.Now.ToString("mmss") + ".png";
        thumbnailPath = Path.Combine(Application.persistentDataPath, screenshotNameString);

        DateTime now = DateTime.Now;
        screenshotDateTimeString = now.ToString("MM-dd-yyyy HH:mm");

        // Save the screenshot to Gallery/Photos
        NativeGallery.SaveImageToGallery(cameraCapture, "GutterAR", screenshotNameString, (success, path) => Debug.Log("Media save result: " + success + " " + path));

        // Save thumbnail to persistent data path
        byte[] thumbnailBytes = cameraCapture.EncodeToPNG();
        File.WriteAllBytes(thumbnailPath, thumbnailBytes);
        Debug.Log($"Thumbnail saved to: {thumbnailPath}");
        yield return StartCoroutine(UploadImageCoroutine(thumbnailPath));

        // Display thumbnail immediately
        LoadThumbnail();

        // To avoid memory leaks
        Destroy(cameraCapture);
    }

    private void LoadThumbnail()
    {
        if (File.Exists(thumbnailPath))
        {
            byte[] thumbnailBytes = File.ReadAllBytes(thumbnailPath);
            Texture2D loadedThumbnail = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!loadedThumbnail.LoadImage(thumbnailBytes))
            {
                Debug.LogError("Failed to load thumbnail image from bytes.");
                return;
            }

            Debug.Log($"Loaded thumbnail dimensions: {loadedThumbnail.width}x{loadedThumbnail.height}");

            Sprite thumbnailSprite = Sprite.Create(
                loadedThumbnail,
                new Rect(0, 0, loadedThumbnail.width, loadedThumbnail.height),
                new Vector2(0.5f, 0.5f)
            );

            if (previewScreenshotImage != null)
            {
                previewScreenshotImage.sprite = thumbnailSprite;
                previewScreenshotImage.preserveAspect = true; // Prevent stretching
                screenshotName.text = screenshotNameString;

                // Adjust AspectRatioFitter
                AspectRatioFitter fitter = previewScreenshotImage.GetComponent<AspectRatioFitter>();
                if (fitter == null) fitter = previewScreenshotImage.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = loadedThumbnail.width / (float)loadedThumbnail.height;

                screenshotWithPopup.SetActive(true);
                screenshotWithoutPopup.SetActive(false);
                Debug.Log("Thumbnail loaded and displayed.");
                screenshotThumbnailScreen.SetActive(true);
            }
            else
            {
                Debug.LogError("previewScreenshotImage is null.");
            }
        }
        else
        {
            Debug.Log("No thumbnail found at: " + thumbnailPath);
        }
    }

    public void UploadImage(string imagePath)
    {
        StartCoroutine(UploadImageCoroutine(imagePath));
    }

    private IEnumerator UploadImageCoroutine(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError("File not found: " + imagePath);
            yield break;
        }
        loadingScreen.SetActive(true);

        byte[] fileData = File.ReadAllBytes(imagePath);
        WWWForm form = new WWWForm();

        // Add image
        form.AddBinaryData("files", fileData, Path.GetFileName(imagePath), "image/jpeg");

        // Add other fields
        form.AddField("moduleName", moduleName);

        UnityWebRequest www = UnityWebRequest.Post(LoginAPIHandler.instance.baseURL + imageUploadEndpoint, form);
        www.SetRequestHeader("accept", "*/*");

        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (www.result != UnityWebRequest.Result.Success)
#else
        if (www.isNetworkError || www.isHttpError)
#endif
        {
            Debug.LogError("Image Upload failed: " + www.error + " " + www.downloadHandler.text);
            loadingScreen.SetActive(false);
        }
        else
        {
            ImageUploadResponse response = JsonUtility.FromJson<ImageUploadResponse>(www.downloadHandler.text);
            Debug.Log("Upload complete! Server response: " + www.downloadHandler.text + " Image name : " + response.data[0].name);
            loadingScreen.SetActive(false);

            if (rulerManager != null)
            {
                Screenshot newScreenshot = new Screenshot { file_name = response.data[0].name, type = "request" };
                ScreenshotDataAR newScreenshotData = new ScreenshotDataAR { imageName = screenshotNameString, dateTime = screenshotDateTimeString };

                screenshotThumbnailUIManager.screenshotList.Add(newScreenshot);
                screenshotThumbnailUIManager.dateTimeList.Add(newScreenshotData);
                screenshotThumbnailUIManager.cartcount.text = screenshotThumbnailUIManager.screenshotList.Count.ToString();

                if (rulerManager.productDataList != null && rulerManager.productDataList.Count > 0)
                {
                    SessionProductForCreateSession lastProduct = rulerManager.productDataList[rulerManager.productDataList.Count - 1];
                    if (lastProduct.screenshot == null)
                    {
                        lastProduct.screenshot = new List<Screenshot>();
                    }
                    lastProduct.screenshot.Add(newScreenshot);
                    Debug.Log($"Added screenshot {response.data[0].name} to productDataList at index {rulerManager.productDataList.Count - 1}");
                }
                else
                {
                    if (rulerManager.screenshotList == null)
                    {
                        rulerManager.screenshotList = new List<Screenshot>();
                    }
                    rulerManager.screenshotList.Add(newScreenshot);
                    Debug.Log($"Added screenshot {response.data[0].name} to screenshotList");
                }
            }

            //if (goalManager != null)
            //{
            //    Screenshot newScreenshot = new Screenshot { file_name = response.data[0].name, type = "request" };
            //    ScreenshotDataAR newScreenshotData = new ScreenshotDataAR { imageName = screenshotNameString, dateTime = screenshotDateTimeString };

            //    screenshotThumbnailUIManager.screenshotList.Add(newScreenshot);
            //    screenshotThumbnailUIManager.dateTimeList.Add(newScreenshotData);
            //    screenshotThumbnailUIManager.cartcount.text = screenshotThumbnailUIManager.screenshotList.Count.ToString();

            //    if (goalManager.productDataList != null && goalManager.productDataList.Count > 0)
            //    {
            //        SessionProductForCreateSession lastProduct = goalManager.productDataList[goalManager.productDataList.Count - 1];
            //        if (lastProduct.screenshot == null)
            //        {
            //            lastProduct.screenshot = new List<Screenshot>();
            //        }
            //        lastProduct.screenshot.Add(newScreenshot);
            //        Debug.Log($"Added screenshot {response.data[0].name} to productDataList at index {goalManager.productDataList.Count - 1}");
            //    }
            //    else
            //    {
            //        if (goalManager.screenshotList == null)
            //        {
            //            goalManager.screenshotList = new List<Screenshot>();
            //        }
            //        goalManager.screenshotList.Add(newScreenshot);
            //        Debug.Log($"Added screenshot {response.data[0].name} to screenshotList");
            //    }
            //}
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        float aspectRatio = source.width / (float)source.height;
        if (targetWidth / (float)targetHeight > aspectRatio)
        {
            targetWidth = Mathf.RoundToInt(targetHeight * aspectRatio);
        }
        else
        {
            targetHeight = Mathf.RoundToInt(targetWidth / aspectRatio);
        }

        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    public Image imageComponent; // Assign this in the Inspector
    string imagePath;

    public void PickImage()
    {
        int maxSize = 1024;

        NativeGallery.GetImageFromGallery((path) =>
        {
            imagePath = path;
            Debug.Log("Image path: " + path);
            if (path != null)
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize);
                if (texture == null)
                {
                    Debug.Log("Couldn't load texture from " + path);
                    return;
                }

                Rect cropRect = new Rect(0, 0, texture.width, texture.height);
                Sprite newSprite = Sprite.Create(
                    texture,
                    cropRect,
                    new Vector2(0.5f, 0.5f)
                );

                if (imageComponent != null)
                {
                    imageComponent.preserveAspect = true;
                    imageComponent.sprite = newSprite;

                    AspectRatioFitter fitter = imageComponent.GetComponent<AspectRatioFitter>();
                    if (fitter == null) fitter = imageComponent.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                    fitter.aspectRatio = texture.width / (float)texture.height;
                }
                else
                {
                    Debug.LogError("imageComponent is null.");
                }

                updateProfileAPIHandler.UploadImage(imagePath);
            }
        }, "Select an image", "image/*");
    }

    private void PickVideo()
    {
        NativeGallery.GetVideoFromGallery((path) =>
        {
            Debug.Log("Video path: " + path);
            if (path != null)
            {
                Handheld.PlayFullScreenMovie("file://" + path);
            }
        }, "Select a video");
    }

    private void PickImageOrVideo()
    {
        if (NativeGallery.CanSelectMultipleMediaTypesFromGallery())
        {
            NativeGallery.GetMixedMediaFromGallery((path) =>
            {
                Debug.Log("Media path: " + path);
                if (path != null)
                {
                    switch (NativeGallery.GetMediaTypeOfFile(path))
                    {
                        case NativeGallery.MediaType.Image: Debug.Log("Picked image"); break;
                        case NativeGallery.MediaType.Video: Debug.Log("Picked video"); break;
                        default: Debug.Log("Probably picked something else"); break;
                    }
                }
            }, NativeGallery.MediaType.Image | NativeGallery.MediaType.Video, "Select an image or video");
        }
    }
}