using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ScreenshotThumbnailItem : MonoBehaviour
{
    public Image imageUI;
    public TextMeshProUGUI imageName;
    public TextMeshProUGUI dateTime;
    public Button screenshotButton;
    public string fileName;

    private void Start()
    {
        screenshotButton.onClick.AddListener(OnThumbnailClick);
    }

    private void OnThumbnailClick()
    {
        if (ScreenshotThumbnailUIManager.instance != null)
        {
            ScreenshotThumbnailUIManager.instance.ShowFullImage(fileName, imageName.text);
        }
    }
}
