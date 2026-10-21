using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class CreateSessionSender : MonoBehaviour
{
    public GameObject loadingScreen;
    public ARSceneSwitchHandler ARSceneSwitch;
    public RulerManager rulerManager;
    //public GoalManager goalManager;

    public string createSessionEndpoint = "request/createSession";

    public void StartSendCreateRequest()
    {
        if(rulerManager != null)
            StartCoroutine(SendCreateRequest());

        //if (goalManager != null)
        //    StartCoroutine(SendCreateRequestForSurface());
    }

    IEnumerator SendCreateRequest()
    {
        loadingScreen.SetActive(true);

        // Build request body
        CreateRequestBody body = new CreateRequestBody
        {
            request_id = PlayerPrefs.GetInt("RequestId"),
             session_product = rulerManager.productDataList
        };

        if (rulerManager.productDataList == null || rulerManager.productDataList.Count == 0)
        {
            loadingScreen.SetActive(false);

            ARSceneSwitch.BackToMainScene();
            Debug.Log("Not Created Session");

            yield return null;
        }

        string jsonData = JsonUtility.ToJson(body);

        Debug.Log("Create Session : " + jsonData);

        // Fix JsonUtility not serializing nested lists well
        // jsonData = jsonData.Replace("\"session_product\":[{", "\"session_product\": [");

        string url = LoginAPIHandler.instance.baseURL + createSessionEndpoint;
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
        request.SetRequestHeader("accept", "application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.Success)
#else
        if (!request.isNetworkError && !request.isHttpError)
#endif
        {
            Debug.Log("✅ Request submitted: " + request.downloadHandler.text);
            loadingScreen.SetActive(false);

            ARSceneSwitch.BackToMainScene();
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ Error: " + request.error + "\n" + request.downloadHandler.text);
        }
    }

    IEnumerator SendCreateRequestForSurface()
    {
        loadingScreen.SetActive(true);

        // Build request body
        CreateRequestBody body = new CreateRequestBody
        {
            request_id = PlayerPrefs.GetInt("RequestId"),
            //session_product = goalManager.productDataList
        };

        //if (goalManager.productDataList == null || goalManager.productDataList.Count == 0)
        //{
        //    loadingScreen.SetActive(false);

        //    ARSceneSwitch.BackToMainScene();
        //    Debug.Log("Not Created Session");

        //    yield return null;
        //}

        string jsonData = JsonUtility.ToJson(body);

        Debug.Log("Create Session : " + jsonData);

        // Fix JsonUtility not serializing nested lists well
        // jsonData = jsonData.Replace("\"session_product\":[{", "\"session_product\": [");

        string url = LoginAPIHandler.instance.baseURL + createSessionEndpoint;
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
        request.SetRequestHeader("accept", "application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.Success)
#else
        if (!request.isNetworkError && !request.isHttpError)
#endif
        {
            Debug.Log("✅ Request submitted: " + request.downloadHandler.text);
            loadingScreen.SetActive(false);

            ARSceneSwitch.BackToMainScene();
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ Error: " + request.error + "\n" + request.downloadHandler.text);
        }
    }
}

[System.Serializable]
public class Screenshot
{
    public string file_name;
    public string type;
}

[System.Serializable]
public class SessionProduct
{
    public string product_name;
    public float length;
    public string length_unit;
    public float width;
    public string width_unit;
    public ColorData color;
    public int quantity;
    public float cost;
    public List<Screenshot> screenshot;
    public int company_product_id;
}

[System.Serializable]
public class CreateRequestBody
{
    public int request_id;
    public List<SessionProductForCreateSession> session_product;
}
