using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LogoutAPIHandler : MonoBehaviour
{
    public GameObject loadingScreen;
    public GameObject loginScreen;
    public GameObject logoutScreen;
    public GameObject deleteAccScreen;

    public TMP_InputField deleteAccDesc;
    public Button deleteAccButton;

    public TextMeshProUGUI counterText;

    public string logoutEndpoint = "auth/logout";
    public string deleteAccEndPoint = "auth/deleteAccount";

    public TMP_InputField deleteAccDescInputfield;

    void Start()
    {
        // Add listener to input field
        deleteAccDesc.onValueChanged.AddListener(CheckInputField);
    }

    private void CheckInputField(string text)
    {
        counterText.text = $"{deleteAccDesc.text.Length} / {deleteAccDesc.characterLimit}";

        // Disable button if input is null or whitespace
        deleteAccButton.interactable = !string.IsNullOrWhiteSpace(deleteAccDesc.text);
    }

    public void OnLogoutButtonClick()
    {
        StartCoroutine(LogoutProcess());
    }

    private IEnumerator LogoutProcess()
    {
        loadingScreen.SetActive(true);
        yield return new WaitForSeconds(1f); // Simulate loading if needed

        using (UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + logoutEndpoint, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("accept", "*/*");
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                       request.result == UnityWebRequest.Result.ProtocolError ||
                       request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.Log("Logout failed: " + request.error);
                loadingScreen.SetActive(false);
            }
            else
            {
                Debug.Log("Logout success: " + request.downloadHandler.text);

                string json = request.downloadHandler.text;
                ResponseBodyWithSingleMessage responseBody = JsonUtility.FromJson<ResponseBodyWithSingleMessage>(json);

                if (responseBody.statusCode == 200)
                {
                    Debug.Log("Logout successful: " + responseBody.message);

                    //loginScreen.SetActive(true);
                    //logoutScreen.SetActive(false);
                    //loadingScreen.SetActive(false);


                    // Clear login state
                    PlayerPrefs.SetInt("isLoggedIn", 0);
                    PlayerPrefs.Save();
                    LoginHandler.isFirstTime = true;

                    PlayerPrefs.DeleteAll();

                    yield return new WaitForSeconds(1f);
                    SceneManager.LoadScene(0);
                }
                else if (responseBody.statusCode == 403)
                {
                    ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                    ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
                }
                else
                {
                    Debug.Log("Logout failed: " + responseBody.message);
                    loadingScreen.SetActive(false);

                    ErrorPopupHandler.instance.errorDescText.text = "There is some issue with Logout API.";//"Logout failed: " + responseBody.message;
                    ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                    ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
                }
            }
        }
    }



    public void DeleteAccount()
    {
        StartCoroutine(DeleteAccountRequest());
    }

    private IEnumerator DeleteAccountRequest()
    {
        loadingScreen.SetActive(true);
        yield return new WaitForSeconds(2f); // Simulate loading if needed
                                             // JSON payload
        string description = deleteAccDescInputfield.text.Trim();

        DeleteAccountPayload payload = new DeleteAccountPayload
        {
            description = description
        };

        string jsonBody = JsonUtility.ToJson(payload);

        // Create request
        UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + deleteAccEndPoint, "POST");
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "*/*");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));


        // Send the request
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
                       request.result == UnityWebRequest.Result.ProtocolError ||
                       request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.Log("Delete Account failed: " + request.error);
            deleteAccDesc.text = null;
            ErrorPopupHandler.instance.errorDescText.text = "There is an issue with Delete account API.";//"Get User Details Response : " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
        else
        {
            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Account deleted successfully: " + request.downloadHandler.text);

                loginScreen.SetActive(true);
                deleteAccScreen.SetActive(false);
                loadingScreen.SetActive(false);
                deleteAccDesc.text = null;

                // Clear login state
                PlayerPrefs.SetInt("isLoggedIn", 0);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("Failed to delete account: " + request.error + "\nResponse: " + request.downloadHandler.text);
                Debug.Log("delete account failed: " + request.error);
                deleteAccDesc.text = null;
                ErrorPopupHandler.instance.errorDescText.text = "There is an issue with Delete account API.";//"Get User Details Response : " + request.downloadHandler.text;
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }
}

[System.Serializable]
public class LogoutResponseBody
{
    public int statusCode;
    public string message;
}


[System.Serializable]
public class DeleteAccountPayload
{
    public string description;
}