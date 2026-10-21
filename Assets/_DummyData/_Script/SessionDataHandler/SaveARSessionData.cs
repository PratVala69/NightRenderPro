using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SaveARSessionData : MonoBehaviour
{
    private SessionData currentSession;
    private int sessionNumber = 1; // Increment for each new session
    private int screenshotCounter = 1; // Increment for each screenshot in the session
    private int productCounter = 1;

   public ARSceneSwitchHandler ARSceneSwitch;

    void Start()
    {
        // Initialize session data
        currentSession = new SessionData
        {
            sessionNumber = sessionNumber,
            products = new List<ProductData>()
        };

        // Load existing sessions to determine the next session number
        List<SessionData> existingSessions = SessionDataSaver.instance.LoadSessions();
        if (existingSessions.Count > 0)
        {
            sessionNumber = existingSessions[existingSessions.Count - 1].sessionNumber + 1;
            currentSession.sessionNumber = sessionNumber;
        }
    }

    [ContextMenu("OnSession Save")]
    public void OnARSessionSave()
    {
        List<SessionData> sessions = SessionDataSaver.instance.LoadSessions();
        sessions.Add(currentSession);
        SessionDataSaver.instance.SaveSessions(sessions);
        Debug.Log($"Session {currentSession.sessionNumber} saved with {currentSession.screenshots.Count} screenshots on scene unload.");

        ARSceneSwitch.BackToMainScene();
    }

    [ContextMenu("AddProductTest")]
    public void AddProductTest()
    {
        AddProduct("Gutter", "12inch", "White color");
    }

    // Add product data (called when placing AR objects)
    public void AddProduct(string name, string size, string color)
    {
        ProductData product = new ProductData
        {
            productName = name,
            productUniqueName = $"session_{currentSession.sessionNumber}_productname_{name}_productcount_{productCounter}",
            size = size,
            color = color
        };
        currentSession.products.Add(product);
        productCounter++;
        Debug.Log($"Added product: {name}, Size: {size}, Color: {color}");
    }

    public void AddProductData(ProductData product)
    {
        product.productUniqueName = $"session_{currentSession.sessionNumber}_productname_{name}_productcount_{productCounter}";
        currentSession.products.Add(product);
        productCounter++;
        Debug.Log($"Added product: {product.productName}, Size: {product.size}, Color: {product.color}");
    }

    public void RemoveProduct(ProductInstance productInstance)
    {
        if (productInstance != null)
        {
            //ProductData productData = productInstance.GetProductData();
            //if (currentSession.products.Contains(productData))
            //{
            //    currentSession.products.Remove(productData);
            //    Destroy(productInstance.gameObject);
            //    Debug.Log($"Removed product: {productData.productName}");
            //}
        }
    }

    // Capture and save screenshot as thumbnail
    //public void CaptureAndSaveSession()
    //{
    //    StartCoroutine(TakeAndSaveScreenshot());
    //}

    public IEnumerator TakeAndSaveScreenshot(Texture2D screenshot)
    {
        yield return new WaitForEndOfFrame();
        // Resize to thumbnail (512x512 as specified)
        Texture2D thumbnail = ResizeTexture(screenshot, 512, 512);

        // Create screenshot metadata
        ScreenshotData screenshotData = new ScreenshotData
        {
            screenshotName = $"session_{currentSession.sessionNumber}_shot_{screenshotCounter}.png",
            captureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            screenshotPath = SessionDataSaver.instance.SaveScreenshot(thumbnail, sessionNumber, currentSession.sessionNumber * 1000 + screenshotCounter) // Unique ID for filename
        };
        currentSession.screenshots.Add(screenshotData);
        screenshotCounter++; // Increment for next screenshot
        Destroy(screenshot); // Clean up

        Debug.Log($"Screenshot saved: {screenshotData.screenshotName}, Time: {screenshotData.captureTime}");
    }

    // Utility to resize texture for thumbnail
    public Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}