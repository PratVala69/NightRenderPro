using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;

public class LoginAPIHandler : MonoBehaviour
{
    public static LoginAPIHandler instance;

    public string baseURL = "https://staging-backend.gutterempire.app/";
    public string imageDownloadUrl = "https://d312a07oy7dfv8.cloudfront.net/";
    public string imageDownloadForARUrl = "https://d312a07oy7dfv8.cloudfront.net/";
    public string loginEndpoint = "auth/login";
    public string checkPhonenumberEndpoint = "auth/checkPhoneNumber";
    //  {
    //      "from": "login",
    //      "phone_number": "1234567890"
    //  }


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist across scenes
        }
        else if (instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
        }
    }

    private void CheckPhoneNumber()
    {
        StartCoroutine(CheckPhoneNumberResposeData("1234567890", (isValid, message) =>
        {
            if (isValid)
            {
                Debug.Log("Phone number valid: " + message);
            }
            else
            {
                Debug.LogWarning("Phone number invalid: " + message);
            }
        }));
    }

    UnityWebRequest request;
    public IEnumerator CheckPhoneNumberResposeData(string phoneNumber, System.Action<bool, string> onComplete)
    {
        yield return new WaitForSeconds(2f);
        //bool exists = System.IO.Directory.Exists(localURL);

        //if (!exists)
        //    System.IO.Directory.CreateDirectory(localURL);

        Debug.Log("IEnumerator CheckPhoneNumberResposeData");
        WWWForm form = new WWWForm();
        form.AddField("from", "login");
        form.AddField("phone_number", phoneNumber);
        form.AddField("device_type", "ios");

        using (request = UnityWebRequest.Post(baseURL + checkPhonenumberEndpoint, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error: " + request.result);
                onComplete?.Invoke(false, request.error);
            }
            //else if (request.result == UnityWebRequest.Result.ProtocolError)
            //{
            //    Debug.Log("Error: " + request.result);
            //    onComplete?.Invoke(false, request.error);
            //}
            else if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.Log("Error: " + request.result);
                onComplete?.Invoke(false, request.error);
            }
            else
            {
                Debug.Log("API Response: " +phoneNumber + request.result + request.downloadHandler.text);

                string json = request.downloadHandler.text;
                Debug.Log("CheckPhoneNumberResposeData JsonData : " + json);

                ResponseBodyWithSingleMessage responseBody = JsonUtility.FromJson<ResponseBodyWithSingleMessage>(json);

                if (responseBody.statusCode == 200)
                {
                    Debug.Log("Phone number exists : " + responseBody.message);
                    onComplete?.Invoke(true, responseBody.message);
                }
                else if (request.responseCode == 403)
                {
                    ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                    ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("Phone check failed : " + responseBody.message);
                    onComplete?.Invoke(false, responseBody.message);
                }
            }
        }
    }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
[Serializable]
public class CheckPhonenumberData
{
    public string from;
    public string phone_number;
}

[Serializable]
public class ResponseBody
{
    public int statusCode ;
    public List<string> message ;
    public DateTime timestamp ;
    public string endpoint ;
}

[Serializable]
public class ResponseBodyWithSingleMessage
{
    public int statusCode;
    public string message;
    public DateTime timestamp;
    public string endpoint;
}