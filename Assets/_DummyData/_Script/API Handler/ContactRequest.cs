using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class ContactRequest : MonoBehaviour
{
    public string postContactReqEndpoint = "contact-request/add";
    public string getContactReqEndpoint = "contact-request/";
    public string postContactListEndpoint = "contact-request/list";

    [Header("UI Screen")]
    public GameObject settingScreen;
    public GameObject createContactReqScreen;
    public GameObject contactReqListScreen;
    public GameObject contactReqDetailScreenById;

    [Header("Inputfield & Button")]
    public TMP_InputField titleInputfield;
    public TMP_InputField descriptionInputfield;
    public TextMeshProUGUI counterText;

    public Button createRequestButton;

    [Header("Contact Req List")]
    public GameObject contactReqPrefab;
    public Transform contactReqContainer;
    public GameObject noContactListObject;

    [Header("Request Response Panel")]
    public TMP_Text detailTitleText;
    public TMP_Text detailDateText;
    public TMP_Text detailDescriptionText;

    public GameObject pendingTag;
    public GameObject processingTag;
    public GameObject resolvedTag;

    public GameObject responseTextObject;
    public GameObject responseSection;
    public TMP_Text adminResponseText;
    public TMP_Text adminResponseDateText;


    void Start()
    {
        // Initial state check
        ValidateInputFields();

        // Add listeners to both input fields
        titleInputfield.onValueChanged.AddListener(delegate { ValidateInputFields(); });
        descriptionInputfield.onValueChanged.AddListener(delegate { ValidateInputFields(); });
    }

    void ValidateInputFields()
    {
        bool isTitleFilled = !string.IsNullOrWhiteSpace(titleInputfield.text);
        bool isDescriptionFilled = !string.IsNullOrWhiteSpace(descriptionInputfield.text);

        counterText.text = $"{descriptionInputfield.text.Length} / {descriptionInputfield.characterLimit}";

        // Enable button only if both are filled
        createRequestButton.interactable = isTitleFilled && isDescriptionFilled;
    }

    string jsonBody;
    public void FetchContactRequests()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        ContactRequestListRequest requestData = new ContactRequestListRequest
        {
            page = 1,
            limit = 30,
            search = "",
            sort_order = "",
            sort_by = "",
            company_id = PlayerPrefs.GetInt("userID")
        };

        jsonBody = JsonUtility.ToJson(requestData);
        StartCoroutine(PostContactList(jsonBody));
    }

    IEnumerator PostContactList(string json)
    {
        yield return new WaitForSeconds(1f);

        UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + postContactListEndpoint, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);

            ContactRequestListResponse response =
                JsonUtility.FromJson<ContactRequestListResponse>(request.downloadHandler.text);

            foreach (Transform child in contactReqContainer)
            {
                Destroy(child.gameObject); // Clear old list
            }

            if (response.data.contactRequests == null || response.data.contactRequests.Count == 0)
            {
                Debug.Log("No contact requests found.");

                // Optionally show "No Data" message
                noContactListObject.SetActive(true); // <-- Make sure you have this GameObject in the UI
                settingScreen.SetActive(false);
                contactReqListScreen.SetActive(true);
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            }
            else
            {
                noContactListObject.SetActive(false);
                foreach (var contact in response.data.contactRequests)
                {
                    GameObject item = Instantiate(contactReqPrefab, contactReqContainer);
                    ContactRequestCard card = item.GetComponent<ContactRequestCard>();
                    card.Init(contact, this); // Pass the data and reference
                    card.rootButton.onClick.AddListener(() => OnContactRequestSelected(card.requestId));
                }

                foreach (var contact in response.data.contactRequests)
                {
                    Debug.Log($"Title: {contact.title}, Status: {contact.status}, Created: {contact.created_at}");
                }

                settingScreen.SetActive(false);
                contactReqListScreen.SetActive(true);
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            }
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Failed: " + request.error);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with contact list API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    public void OnContactRequestSelected(string requestId)
    {
        GetContactRequestById(requestId);
    }

    public void SendContactRequest()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        ContactRequestInput requestData = new ContactRequestInput
        {
            title = titleInputfield.text,
            description = descriptionInputfield.text
        };

        string jsonData = JsonUtility.ToJson(requestData);
        Debug.Log("send contact req : "+jsonData);
        StartCoroutine(PostContactRequest(jsonData));
    }

    IEnumerator PostContactRequest(string json)
    {
        yield return new WaitForSeconds(1f);

        string url = LoginAPIHandler.instance.baseURL + postContactReqEndpoint;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Contact request created: " + request.downloadHandler.text);

            yield return StartCoroutine(PostContactList(jsonBody));

            createContactReqScreen.SetActive(false);
            contactReqListScreen.SetActive(true);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);

            // Optional: show confirmation, clear fields, refresh list
            titleInputfield.text = "";
            descriptionInputfield.text = "";
            // Refresh UI or show success message
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.Log("Failed to create contact request: " + request.error);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with post contact request API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    public void GetContactRequestById(string requestId)
    {
        //var selectedRequest = FindRequestById(requestId);

        //if (selectedRequest != null && selectedRequest.status.ToLower() == "resolved")
        //{
        //    Debug.Log("Resolved requests cannot be opened.");
        //    return; // Prevent opening detail screen
        //}

        StartCoroutine(GetContactRequestCoroutine(requestId));
    }

    private ContactRequestData FindRequestById(string requestId)
    {
        if (contactReqContainer == null) return null;

        foreach (Transform child in contactReqContainer)
        {
            var card = child.GetComponent<ContactRequestCard>();
            if (card != null && card.requestId == requestId)
            {
                return card.data; // You must store `data` (ContactRequestData) in the card
            }
        }

        return null;
    }

    IEnumerator GetContactRequestCoroutine(string requestId)
    {
        UnityWebRequest request = UnityWebRequest.Get(LoginAPIHandler.instance.baseURL + getContactReqEndpoint + requestId);
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
        request.SetRequestHeader("accept", "*/*");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Contact request fetched: " + request.downloadHandler.text);

            ContactRequestDetailResponse responseData = JsonUtility.FromJson<ContactRequestDetailResponse>(request.downloadHandler.text);    

            // Populate detail screen
            detailTitleText.text = responseData.data.title;
            detailDateText.text = FormatDate(responseData.data.created_at);
            detailDescriptionText.text = responseData.data.description;
            detailDescriptionText.transform.parent.GetComponent<ContentSizeFitter>().SetLayoutVertical();

            // Set status tags
            pendingTag.SetActive(false);
            resolvedTag.SetActive(false);
            processingTag.SetActive(false);

            switch (responseData.data.status.ToLower())
            {
                case "pending":
                    pendingTag.SetActive(true);
                    responseSection.SetActive(false);
                    responseTextObject.SetActive(false);
                    break;
                case "processing":
                    processingTag.SetActive(true);
                    responseSection.SetActive(false);
                    responseTextObject.SetActive(false);
                    break;
                case "resolved":
                    resolvedTag.SetActive(true);
                    if (!string.IsNullOrWhiteSpace(responseData.data.admin_response))
                    {
                        responseTextObject.SetActive(true);
                        responseSection.SetActive(true);
                        adminResponseText.text = responseData.data.admin_response;
                        adminResponseDateText.text = FormatDate(responseData.data.updated_at);
                        adminResponseText.transform.parent.GetComponent<ContentSizeFitter>().SetLayoutVertical();
                    }
                    else
                    {
                        responseSection.SetActive(false);
                        responseTextObject.SetActive(false);
                    }
                    break;
            }

            contactReqListScreen.SetActive(false);
            contactReqDetailScreenById.SetActive(true);
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Error: " + request.error);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with get contact with request id API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    string FormatDate(string isoDate)
    {
        if (System.DateTime.TryParse(isoDate, out System.DateTime dt))
            return dt.ToString("dd MMM hh:mm tt"); // 16 Apr 12:23 PM
        return isoDate;
    }
}

[System.Serializable]
public class ContactRequestInput
{
    public string title;
    public string description;
}

[System.Serializable]
public class ContactRequestResponse
{
    public ContactRequestListData data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class ContactRequestData
{
    public string id;
    public string title;
    public string description;
    public string status;
    public string admin_response;
    public bool is_deleted;
    public string created_at;
    public string updated_at;
    public User user;
}

[System.Serializable]
public class User
{
    public int id;
    public string first_name;
    public string last_name;
    public string company_name;
    public string email;
    public string owner_name;
    public string phone_number;
    public string role;
    public ProfilePictureForReq profile_picture;
}

[System.Serializable]
public class ProfilePictureForReq
{
    public int id;
    public string file_name;
    public string location_key;
}


[System.Serializable]
public class ContactRequestListResponse
{
    public ContactRequestListData data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class ContactRequestListData
{
    public List<ContactRequestData> contactRequests;
    public int totalCount;
}

[System.Serializable]
public class ContactRequestListRequest
{
    public int page = 1;
    public int limit = 30;
    public string search = "";
    public string sort_order = "";
    public string sort_by = "";
    public int company_id;
}

[System.Serializable]
public class ContactRequestDetailResponse
{
    public ContactRequestData data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class UserInfo
{
    public int id;
}