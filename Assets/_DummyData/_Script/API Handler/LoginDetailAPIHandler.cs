using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LoginDetailAPIHandler : MonoBehaviour
{
    public string loginEndpoint = "auth/login";

    public GetUserDetailsAPIHandler getUserDetailsAPIHandler;
    public ClientHandler clientHandler;
    public LoginResponse response;

    public void OnLoginButtonClick()
    {
        StartCoroutine(LoginUser(
            PlayerPrefs.GetString("phoneNumber"),
            GetOrCreateDeviceId(),
            "ios",
            PlayerPrefs.GetString("fcm_token"),
            (success, message, data) =>
            {
                if (success)
                {
                    Debug.Log("Access token: " + data.access_token);

                    //PlayerPrefs.SetString("accessToken", data.access_token);
                    //PlayerPrefs.Save();

                    //PlayerPrefs.SetInt("userID", data.id);
                    //PlayerPrefs.Save();

                    Debug.Log("Company Owner : " + data.role);
                    PlayerPrefs.SetString("Role", data.role); //"company_owner", "sales_representative"
                    PlayerPrefs.Save();


                    clientHandler.StartFetchingClientList();
                }
                else
                {
                    Debug.LogWarning("Login failed: " + message);

                    // Show error popup
                }
            }));
    }

    public IEnumerator LoginUser(string phoneNumber, string deviceId, string deviceType, string fcmToken, System.Action<bool, string, LoginResponseData> onComplete)
    {
        LoginRequestBody requestBody = new LoginRequestBody
        {
            phone_number = phoneNumber,
            device_id = deviceId,
            device_type = deviceType,
            fcm_token = fcmToken
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        Debug.Log("Login json data : " + jsonData);
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + loginEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("accept", "*/*");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError ||
                request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogWarning("Login failed: " + request.error);
                onComplete?.Invoke(false, request.error, null);
            }
            else
            {
                Debug.Log("Login response: " + request.downloadHandler.text);
                response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                if (response.statusCode == 200)
                {
                    Debug.Log("User logged in successfully: " + response.message);

                    Debug.Log("Access token: " + response.data.access_token);
                    PlayerPrefs.SetString("accessToken", response.data.access_token);
                    PlayerPrefs.Save();

                    PlayerPrefs.SetInt("userID", response.data.id);
                    PlayerPrefs.Save();

                    yield return StartCoroutine(getUserDetailsAPIHandler.GetUserDetailsProcess());

                    onComplete?.Invoke(true, response.message, response.data);

                }
                else if (response.statusCode == 403)
                {
                    ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                    ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("Login error: " + response.message);

                    // Specific error handling
                    if (response.message.Contains( "Account does not exist" ))//for the entered details.")
                    {
                       // ErrorPopupHandler.instance.errorDescText.text = "Account does not exist for the entered details.";
                        ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                        ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
                    }
                    else
                    {
                        ErrorPopupHandler.instance.errorDescText.text = "There is some issue with Login API";
                        ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                        ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
                    }

                    onComplete?.Invoke(false, response.message, null);
                }
            }
        }
    }

    string GetOrCreateDeviceId()
    {
        if (!PlayerPrefs.HasKey("device_id"))
        {
            string newId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("device_id", newId);
            PlayerPrefs.Save();
        }
        return PlayerPrefs.GetString("device_id");
    }

}

[System.Serializable]
public class LoginRequestBody
{
    //public string email;
    //public string password;
    public string phone_number;
    //public string role;
    public string device_id;
    public string device_type;
    public string fcm_token;
}

[System.Serializable]
public class ProfilePicture
{
    public int id;
    public string mime_type;
    public string file_name;
    public string original_file_name;
    public string location_key;
    public string created_at;
    public string updated_at;
}

[System.Serializable]
public class LoginResponseData
{
    public int id;
    public string first_name;
    public string last_name;
    public string email;
    public string phone_number;
    public string role;
    public bool is_subscribed;
    public string access_token;
    public string device_id;
    public string device_type;
    public string fcm_token;
    public ProfilePicture profile_picture;
    public string stripe_id;
    public string created_at;
    public string updated_at;
}

[System.Serializable]
public class LoginResponse
{
    public LoginResponseData data;
    public int statusCode;
    public string message;
}
