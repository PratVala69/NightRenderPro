using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SessionDataSaver : MonoBehaviour
{
    public static SessionDataSaver instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    private string baseDataPath => Application.persistentDataPath;

    // Sanitize client folder name
    private string GetSanitizedClientFolder()
    {
        string rawName = RequestHandler.selectedClientNameStr;
        string cleanName = Regex.Replace(rawName, @"[^a-zA-Z0-9_\-]", "_"); // replace special characters
        return Path.Combine(baseDataPath, $"Client_{cleanName}");
    }

    private string DataFilePath => Path.Combine(GetSanitizedClientFolder(), "session_data.json");
    private string ScreenshotFolder => Path.Combine(GetSanitizedClientFolder(), "Screenshots");

    public void SaveSessions(List<SessionData> sessions)
    {
        string clientFolder = GetSanitizedClientFolder();
        if (!Directory.Exists(clientFolder))
            Directory.CreateDirectory(clientFolder);

        if (!Directory.Exists(ScreenshotFolder))
            Directory.CreateDirectory(ScreenshotFolder);

        string json = JsonUtility.ToJson(new SessionDataListWrapper { sessions = sessions }, true);
        File.WriteAllText(DataFilePath, json);
        Debug.Log("Saved sessions for client to: " + DataFilePath);
    }

    public List<SessionData> LoadSessions()
    {
        if (!File.Exists(DataFilePath))
        {
            Debug.Log("No session file found for client.");
            return new List<SessionData>();
        }

        string json = File.ReadAllText(DataFilePath);
        SessionDataListWrapper wrapper = JsonUtility.FromJson<SessionDataListWrapper>(json);
        return wrapper.sessions;
    }

    public string SaveScreenshot(Texture2D tex, int sessionNumber, int screenshotIndex)
    {
        if (!Directory.Exists(ScreenshotFolder))
            Directory.CreateDirectory(ScreenshotFolder);

        string filename = $"session_{sessionNumber}_shot_{screenshotIndex}.png";
        string filePath = Path.Combine(ScreenshotFolder, filename);
        File.WriteAllBytes(filePath, tex.EncodeToPNG());

        return filePath;
    }

    public Texture2D LoadScreenshot(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("Screenshot not found: " + path);
            return null;
        }

        byte[] imageData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);
        return tex;
    }
}


[System.Serializable]
public class SessionDataListWrapper
{
    public List<SessionData> sessions;
}

[System.Serializable]
public class ProductData
{
    public string productName;
    public string productUniqueName;
    public string size;
    public string color;
    public string imagePath;
    public string costPerUnit;
    public float variation; // e.g., 10% (stored as 10.0)
}

[System.Serializable]
public class SessionData
{
    public int sessionNumber;
    public List<ScreenshotData> screenshots = new List<ScreenshotData>(); // List of screenshots
    public List<ProductData> products = new List<ProductData>();
}

[System.Serializable]
public class ScreenshotData
{
    public string screenshotPath; // Relative or absolute path to image file
    public string screenshotName; // Filename of the screenshot (e.g., "session_1_shot_1.png")
    public string captureTime; // Timestamp of when the screenshot was taken
}

[System.Serializable]
public class ProductLoadData
{
    public string productName;
    public string size;
    public string color;
    public float quantity; // e.g., 17.5
    public float costPerUnit; // e.g., 2 USD per piece
    public float totalCost; // e.g., 35 USD
    public float variation; // e.g., 10% (stored as 10.0)
    public string additionalCosts; // e.g., "Nuts: 10 USD"
}
