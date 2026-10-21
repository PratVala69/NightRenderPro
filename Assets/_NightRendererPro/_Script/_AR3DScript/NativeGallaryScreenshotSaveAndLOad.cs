using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class NativeGalleryScreenshotSaveAndLoad : MonoBehaviour
{
    [Header("UI References")]
    public Transform screenshotListParent;      // Content parent in ScrollView
    public GameObject screenshotItemPrefab;     // Prefab with Image + Text fields
    public GameObject screenshotListPanel;      // Panel that shows screenshots
    public GameObject loadingScreen;
    public GameObject screenshotPopup;

    private string dataFilePath;
    private string screenshotFolderPath;
    private List<ScreenshotSaveData> savedScreenshots = new List<ScreenshotSaveData>();

    private void Awake()
    {
        screenshotFolderPath = Application.persistentDataPath + "/Screenshots/";
        dataFilePath = Path.Combine(screenshotFolderPath, "screenshots.json");

        if (!Directory.Exists(screenshotFolderPath))
            Directory.CreateDirectory(screenshotFolderPath);

        LoadScreenshotData();
    }

    public void CameraViewScreenshot()
    {
        StartCoroutine(TakeCameraScreenshotAndSave());
    }

    private IEnumerator TakeCameraScreenshotAndSave()
    {
        loadingScreen.SetActive(true); // 🔹 Show loading
        yield return new WaitForEndOfFrame();

        Camera camera = Camera.main;
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = rt;
        camera.Render();

        Texture2D cameraCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        cameraCapture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        cameraCapture.Apply();

        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        string fileName = "LuminairesAR_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(screenshotFolderPath, fileName);

        byte[] bytes = cameraCapture.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
        Destroy(cameraCapture);

        NativeGallery.SaveImageToGallery(filePath, "LuminairesAR", fileName);

        ScreenshotSaveData data = new ScreenshotSaveData
        {
            fileName = fileName,
            dateTime = DateTime.Now.ToString("MM-dd-yyyy HH:mm"),
            filePath = filePath
        };

        savedScreenshots.Add(data);
        SaveScreenshotData();

        Debug.Log($"✅ Screenshot saved: {filePath}");
        yield return new WaitForSeconds(1f);

        loadingScreen.SetActive(false); // 🔹 Hide loading

        screenshotPopup.SetActive(true);
        screenshotPopup.GetComponent<PopupFade>().ShowPopup();
    }

    private void SaveScreenshotData()
    {
        string json = JsonUtility.ToJson(new ScreenshotListWrapper { list = savedScreenshots }, true);
        File.WriteAllText(dataFilePath, json);
    }

    private void LoadScreenshotData()
    {
        if (File.Exists(dataFilePath))
        {
            string json = File.ReadAllText(dataFilePath);
            ScreenshotListWrapper wrapper = JsonUtility.FromJson<ScreenshotListWrapper>(json);
            savedScreenshots = wrapper.list;
        }
        else
        {
            savedScreenshots = new List<ScreenshotSaveData>();
        }
    }

    [Serializable]
    private class ScreenshotListWrapper
    {
        public List<ScreenshotSaveData> list;
    }

    public void ShowScreenshots()
    {
        StartCoroutine(ShowScreenshotsWithLoading());
    }

    // Called when "Show Screenshots" button is pressed
    private IEnumerator ShowScreenshotsWithLoading()
    {
        loadingScreen.SetActive(true); // 🔹 Show loading

        foreach (Transform child in screenshotListParent)
            Destroy(child.gameObject);

        yield return null; // let UI update

        foreach (var data in savedScreenshots)
        {
            GameObject item = Instantiate(screenshotItemPrefab, screenshotListParent);
            Image img = item.transform.Find("Thumbnail").GetComponent<Image>();
            Text nameText = item.transform.Find("NameText").GetComponent<Text>();
            Text dateText = item.transform.Find("DateText").GetComponent<Text>();

            nameText.text = data.fileName;
            dateText.text = data.dateTime;

            yield return LoadThumbnail(data.filePath, img);
        }

        loadingScreen.SetActive(false); // 🔹 Hide loading
        screenshotListPanel.SetActive(true);
    }

    private IEnumerator LoadThumbnail(string path, Image image)
    {
        if (!File.Exists(path)) yield break;

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);

        Sprite sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));

        image.sprite = sprite;
        yield return null;
    }
}


[System.Serializable]
public class ScreenshotSaveData
{
    public string fileName;
    public string dateTime;
    public string filePath;
}