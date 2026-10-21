using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System.Collections.Generic;
using static ClientHandler;

public class ClientDetailInstance : MonoBehaviour
{
    public string sendPdfEndpoint = "request/sendPdf/";

    public TextMeshProUGUI FullNameText;
    public TextMeshProUGUI EmailText;
    public TextMeshProUGUI ContactNoText;
    public TextMeshProUGUI TimeDateText;
    public TextMeshProUGUI ProductCountText;
    public TextMeshProUGUI SessionCountText;
    public TextMeshProUGUI addressText;

    public List<GameObject> status;

    public Button pdfMailButton;
    public Button pdfDownloadButton;

    public string pdfUrl;
    public int requestId;

    public Button cardButton;

    public RequestItem requestItem;

    public void DownloadPDF()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        StartCoroutine(DownloadAndSavePDF());
    }

    public void Init()
    {
        cardButton.onClick.RemoveAllListeners(); // clear old listeners
        cardButton.onClick.AddListener(OnCardClick);
    }

    private void OnCardClick()
    {
        // Assuming you mark status with an index or active flag in `status` list
        bool isPending = false;

        // Example: check if "pending" is the first object in the list and active
        foreach (GameObject s in status)
        {
            if (s.name.ToLower().Contains("pending") && s.activeSelf)
            {
                isPending = true;
                break;
            }
        }

        if (isPending)
        {
            // Redirect to new session screen
            PlayerPrefs.SetInt("RequestId", requestId);
            ClientHandler.instance.SendRequestWithClientId(requestItem.client.id);

            // Optionally pass along requestId if needed
            // ClientHandler.instance.StartNewSession(requestId);
        }
        else
        {
            PlayerPrefs.SetInt("RequestId", requestId);
            GetSessionDetailsID(requestId);
        }
    }

    public void GetSessionDetailsID(int requestID)
    {
        StartCoroutine(SalesRepRequestOverviewHandler.instance.GetSessionDetails(requestID));
    }

    IEnumerator DownloadAndSavePDF()
    {
        UnityWebRequest www = UnityWebRequest.Get(pdfUrl);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            string safeName = FullNameText.text.Replace(" ", "_"); // Optional: remove/replace more invalid chars
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = safeName + "_" + timestamp + ".pdf";

            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllBytes(filePath, www.downloadHandler.data);
            Debug.Log("PDF saved to: " + filePath);
            OpenPDF(filePath);

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else if (www.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("PDF download failed: " + www.error);
            ErrorPopupHandler.instance.errorDescText.text = "PDF download failed.";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    void OpenPDF(string filePath)
    {
#if UNITY_IOS
        string url = "file://" + filePath;
        Application.OpenURL(url);
#endif
    }

    public void SendPDFToClient()
    {
        StartCoroutine(SendPDFRequest());
    }

    IEnumerator SendPDFRequest()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        string url = LoginAPIHandler.instance.baseURL + sendPdfEndpoint + requestId;

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("accept", "*/*");
        www.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (www.result == UnityWebRequest.Result.Success)
#else
        if (!www.isNetworkError && !www.isHttpError)
#endif
        {
            Debug.Log("✅ PDF sent successfully: " + www.downloadHandler.text);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else if (www.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ Failed to send PDF: " + www.error + " — " + www.downloadHandler.text);
            ErrorPopupHandler.instance.errorDescText.text = "Failed to send PDF";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }
}
