using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SalesRepDropdownHandler : MonoBehaviour
{
    [Header("API Endpoints")]
    public string changeStatusEndpoint = "request/change-status";
    public string timelineEndpoint = "request/getRequestTimeline";
    public string deleteRequestEndpoint = "request/";
    public string editClientEndpoint = "request/editClient";

    [Header("Dropdown")]
    public GameObject salesRepDropdown;
    bool isOpen = false;

    public GameObject reqDropdown;
    bool isOpenReq = false;

    [Header("Change Status")]
    public GameObject changeStatusPopup;
    public TMP_Dropdown statusDropdown;
    public TMP_InputField notesInput;
    public Button confirmButton;

    private Dictionary<string, List<string>> statusOptions = new Dictionary<string, List<string>>()
{
    { "created",  new List<string>{ "Sent", "Approved", "Rejected" } },
    { "sent",     new List<string>{ "Created", "Approved", "Rejected" } },
    { "approved", new List<string>{ "Sent", "Created", "Rejected" } },
    { "rejected", new List<string>{ "Sent", "Created", "Approved" } },
};

    [Header("Timeline")]
    public GameObject timelineScreen;
    public Transform timelineContainer; 
    public GameObject timelinePrefab;
    public GameObject noTimelineRequestYet;

    [Header("UI References")]
    public GameObject deleteConfirmationPopup;
    public Button yesButton;
    public Button noButton;
    public GameObject[] deactiveScreen;

    [Header("Edit client Fields")]
    public TextMeshProUGUI selectedClientName;
    public GameObject editClientPopup;
    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField phoneInput;

    public GameObject firstNameErrorText;
    public GameObject lastNameErrorText;
    public GameObject emailErrorText;
    public GameObject contactNumberErrorText;

    public Button addButton;
    private int currentClientId;

    [Header("Script Reference")]
    public ClientHandler clientHandler;
    public SalesRepAPIHandler salesRepAPIHandler;
    public GameObject salesRepRequestOverviewScreen;
    public RequestEditHandler requestEditHandler;

    #region UIHandler

    private void Start()
    {
        ValidateNoteInput(notesInput.text);
        notesInput.onValueChanged.AddListener(ValidateNoteInput);

        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(() => deleteConfirmationPopup.SetActive(false));

        addButton.interactable = false;

        // Add listeners for value change
        firstNameInput.onValueChanged.AddListener(delegate { ValidateForm(); });
        lastNameInput.onValueChanged.AddListener(delegate { ValidateForm(); });
        emailInput.onValueChanged.AddListener(delegate { ValidateForm(); });
        phoneInput.onValueChanged.AddListener(delegate { ValidateForm(); });

        phoneInput.onValueChanged.AddListener(OnPhoneInputChanged);
    }

    private void ValidateNoteInput(string text)
    {
        confirmButton.interactable = !string.IsNullOrWhiteSpace(text);
    }

    public void SalesRepToggleDropdown()
    {
        if (PlayerPrefs.GetString("Role", "") == "company_owner")
        {
            isOpen = !isOpen;
            salesRepDropdown.SetActive(isOpen);
        }
        else
        {
            isOpenReq = !isOpenReq;
            reqDropdown.SetActive(isOpenReq);
        }
    }

    public void EditRequest()
    {
        StartCoroutine(requestEditHandler.GetSessionDetails(SalesRepRequestOverviewHandler.instance.requestID));
    }
    
    #endregion

    #region ChangeRequestStatus

    public void ChangeRequestStatus()
    {
        StartCoroutine(ChangeStatusCoroutine(SalesRepRequestOverviewHandler.instance.requestID));
    }

    public void PopulateStatusDropdown(GameObject clientRequest)
    {
        string currentStatus = clientRequest.GetComponent<ClientDetailInstance>().requestItem.status.ToLower();
       
        statusDropdown.ClearOptions();

        if (statusOptions.ContainsKey(currentStatus))
        {
            statusDropdown.AddOptions(statusOptions[currentStatus]);
        }
        else
        {
            Debug.LogWarning("⚠️ Unknown status: " + currentStatus);
        }

        changeStatusPopup.SetActive(true);
    }

    private IEnumerator ChangeStatusCoroutine(int requestId)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        string url = LoginAPIHandler.instance.baseURL + changeStatusEndpoint;

        // Create request body
        ChangeStatusRequest requestBody = new ChangeStatusRequest
        {
            request_id = requestId,
            status = statusDropdown.options[statusDropdown.value].text.ToLower(),
            note = notesInput.text
        };

        string jsonData = JsonUtility.ToJson(requestBody);
        Debug.Log("Payload: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            ChangeStatusResponse response = JsonUtility.FromJson<ChangeStatusResponse>(request.downloadHandler.text);
            Debug.Log($"✅ Status updated: {response.data.status} (Request ID: {response.data.request_id})");

            string currentStatus = response.data.status.ToLower();

            SalesRepRequestOverviewHandler.instance.clientDetailInstance.requestItem.status = response.data.status;
            RequestEditHandler.instance.clientDetailInstanceReq.requestItem.status = response.data.status;

            foreach (GameObject go in SalesRepRequestOverviewHandler.instance.status)
            {
                go.SetActive(false);
            }

            foreach (GameObject go in SalesRepRequestOverviewHandler.instance.status)
            {
                if (go.name.ToLower() == currentStatus)
                {
                    go.SetActive(true);
                    break;
                }
            }

            foreach (GameObject go in RequestEditHandler.instance.statusReq)
            {
                go.SetActive(false);
            }

            foreach (GameObject go in RequestEditHandler.instance.statusReq)
            {
                if (go.name.ToLower() == currentStatus)
                {
                    go.SetActive(true);
                    break;
                }
            }

            //yield return StartCoroutine(clientHandler.GetAllClientList());
            yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));
            yield return StartCoroutine(clientHandler.GetClientList());
            //yield return StartCoroutine(requestEditHandler.GetSessionDetails(SalesRepRequestOverviewHandler.instance.requestID));

            //yield return new WaitForSeconds(1f);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            changeStatusPopup.SetActive(false);

            ResetChangeStatusPopup();

            FilterRequestHandler.instance.ClearFilters();
            FilterRequestHandler.instance.ClearFiltersSalesRep();
        }
        else
        {
            Debug.LogError("❌ Error: " + request.error + "\nResponse: " + request.downloadHandler.text);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.errorDescText.text = "There s an issue with change status API";
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    public void ResetChangeStatusPopup()
    {
        statusDropdown.value = 0;
        notesInput.text = "";
        ValidateNoteInput(notesInput.text);
    }

    #endregion

    #region Timeline

    public void FetchTimeline()
    {
        StartCoroutine(GetTimelineCoroutine(SalesRepRequestOverviewHandler.instance.requestID));
    }

    private IEnumerator GetTimelineCoroutine(int requestId)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        string url = LoginAPIHandler.instance.baseURL + timelineEndpoint;
        // JSON body
        TimelineRequestBody body = new TimelineRequestBody
        {
            page = 1,
            limit = 30,
            request_id = requestId
        };

        string bodyJson = JsonUtility.ToJson(body);
        Debug.Log("Request Body JSON: " + bodyJson);
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));


        yield return request.SendWebRequest();

         Debug.Log("Response: " + request.downloadHandler.text);
        if (request.result == UnityWebRequest.Result.Success)
        {

            TimelineResponse timelineResponse = JsonUtility.FromJson<TimelineResponse>(request.downloadHandler.text);

            // Clear old UI
            foreach (Transform child in timelineContainer)
                Destroy(child.gameObject);

            if (timelineResponse.data.timeline_list.Count == 0)
                noTimelineRequestYet.SetActive(true);
            else
            {
                noTimelineRequestYet.SetActive(false);
                // Instantiate new UI items
                foreach (var item in timelineResponse.data.timeline_list)
                {
                    GameObject obj = Instantiate(timelinePrefab, timelineContainer);

                    // Assuming prefab has two TMP texts: Title, Description, Date
                    TimelineCard timelineCard = obj.GetComponent<TimelineCard>();
                    timelineCard.headerText.text = item.title;
                    timelineCard.descriptionText.text = item.description;
                    timelineCard.dateTimeText.text = System.DateTime.Parse(item.created_at).ToString("dd MMM hh:mm tt");
                }
            }

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            timelineScreen.SetActive(true);
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.errorDescText.text = "There s an issue with timeline API";
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    #endregion

    #region DeleteRequest

    // Called when delete button is clicked from UI
    public void ShowDeleteConfirmation()
    {
        deleteConfirmationPopup.SetActive(true);
    }

    private void OnYesClicked()
    {
        deleteConfirmationPopup.SetActive(false);
        StartCoroutine(DeleteRequestCoroutine(SalesRepRequestOverviewHandler.instance.requestID));
    }

    private IEnumerator DeleteRequestCoroutine(int requestId)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        string url = LoginAPIHandler.instance.baseURL + deleteRequestEndpoint + requestId;

        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("accept", "*/*");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Delete success: " + request.ToString());
            // ✅ Redirect user back to listing screen

            //yield return StartCoroutine(clientHandler.GetAllClientList());
            //yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));

            //ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            //salesRepRequestOverviewScreen.SetActive(false);

            //yield return StartCoroutine(clientHandler.GetAllClientList());
            yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));
            yield return StartCoroutine(clientHandler.GetClientList());

            FilterRequestHandler.instance.ClearFilters();
            FilterRequestHandler.instance.ClearFiltersSalesRep();

            // if (isEdit)
            {
                //allRequestCompanyOwnerScreen.SetActive(true);
                for (int i = 0; i < deactiveScreen.Length; i++)
                    deactiveScreen[i].SetActive(false);
            }

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.errorDescText.text = "There s an issue with delete request API";
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    #endregion

    #region EditClient

    public void OnPhoneInputChanged(string input)
    {
        string digits = System.Text.RegularExpressions.Regex.Replace(input, @"[^0-9]", "");
        if (digits.Length > 10) digits = digits.Substring(0, 10);

        string formatted = digits;
        if (digits.Length > 6)
            formatted = $"{digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6)}";
        else if (digits.Length > 3)
            formatted = $"{digits.Substring(0, 3)} {digits.Substring(3)}";

        if (phoneInput.text != formatted)
        {
            phoneInput.text = formatted;
            phoneInput.MoveTextEnd(false);
        }
    }

    public void ResetForm()
    {
        // Clear all fields
        firstNameInput.text = "";
        lastNameInput.text = "";
        emailInput.text = "";
        phoneInput.text = "";

        // Disable Add button again
        addButton.interactable = false;
    }

    private void ValidateForm()
    {
        // Check if all fields are filled
        bool isValid = !string.IsNullOrWhiteSpace(firstNameInput.text) &&
                       !string.IsNullOrWhiteSpace(lastNameInput.text) &&
                       !string.IsNullOrWhiteSpace(emailInput.text) &&
                       !string.IsNullOrWhiteSpace(phoneInput.text);

        addButton.interactable = isValid;
    }

    public void SetClientData()
    {
        currentClientId = SalesRepRequestOverviewHandler.instance.clientID;

        string fullName = SalesRepRequestOverviewHandler.instance.selectedClient.client.name;
        string[] parts = fullName.Trim().Split(' ');

        // Default empty
        string firstName = "";
        string lastName = "";

        // Assign values
        if (parts.Length > 0) firstName = parts[0];
        if (parts.Length > 1) lastName = string.Join(" ", parts, 1, parts.Length - 1);

        firstNameInput.text = firstName;
        lastNameInput.text = lastName;

        emailInput.text = SalesRepRequestOverviewHandler.instance.selectedClient.client.email;

        string rawNumber = SalesRepRequestOverviewHandler.instance.selectedClient.client.phone_number; // e.g., "1234567890"

        if (rawNumber.Length == 10)
        {
            string formatted = string.Format("{0} {1}-{2}",
                rawNumber.Substring(0, 3),
                rawNumber.Substring(3, 3),
                rawNumber.Substring(6, 4));

            phoneInput.text = formatted; // Output: "123 456-7890"
        }
        else
        {
            phoneInput.text = rawNumber; // fallback for unexpected lengths
        }

        //phoneInput.text = SalesRepRequestOverviewHandler.instance.selectedClient.client.phone_number;

        editClientPopup.SetActive(true);
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidPhoneNumber(string phone)
    {
        // Remove spaces and dashes before validation
        string cleanedPhone = System.Text.RegularExpressions.Regex.Replace(phone, @"[^0-9]", string.Empty);
        return System.Text.RegularExpressions.Regex.IsMatch(cleanedPhone, @"^\d{10}$");
    }

    public void OnSaveClicked()
    {
        bool hasError = false;

        if (string.IsNullOrWhiteSpace(firstNameInput.text))
        {
            firstNameErrorText.SetActive(true);
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(lastNameInput.text))
        {
            lastNameErrorText.SetActive(true);
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(emailInput.text))
        {
            emailErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter Email";
            emailErrorText.SetActive(true);
            hasError = true;
        }
        else if (!IsValidEmail(emailInput.text))
        {
            emailErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter valid Email";
            emailErrorText.SetActive(true);
            hasError = true;
        }

        if (string.IsNullOrWhiteSpace(phoneInput.text))
        {
            contactNumberErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter Phone number";
            contactNumberErrorText.SetActive(true);
            hasError = true;
        }
        else if (!IsValidPhoneNumber(phoneInput.text))
        {
            contactNumberErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter valid Phone number";
            contactNumberErrorText.SetActive(true);
            hasError = true;
        }

        if (hasError)
        {
            Debug.Log("Error: All fields are required!");
            return;
        }

        StartCoroutine(EditClientCoroutine());
    }

    private IEnumerator EditClientCoroutine()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        string url = LoginAPIHandler.instance.baseURL + editClientEndpoint;

        var bodyData = new EditClientRequest()
        {
            client_id = currentClientId,
            first_name = firstNameInput.text,
            last_name = lastNameInput.text,
            email = emailInput.text,
            phone_number = System.Text.RegularExpressions.Regex.Replace(phoneInput.text, @"[^0-9]", "")

    };

        string bodyJson = JsonUtility.ToJson(bodyData);
        Debug.Log("Edit Client Request Body: " + bodyJson);

        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        
        yield return request.SendWebRequest();

            Debug.Log("Edit client: " + request.downloadHandler.text);
        if (request.result == UnityWebRequest.Result.Success)
        {
            // ✅ Show success popup and reload client list

            yield return StartCoroutine(clientHandler.GetClientList());
            yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));

            ClientDetailInstance updatedClinet = SalesRepRequestOverviewHandler.instance.clientDetailInstance;
            ClientDetailInstance updatedClinet2 = RequestEditHandler.instance.clientDetailInstance;
            ClientDataWrapper clientWrapper = JsonUtility.FromJson<ClientDataWrapper>(request.downloadHandler.text);

            updatedClinet.FullNameText.text = clientWrapper.data.first_name + " " + clientWrapper.data.last_name;
            updatedClinet.EmailText.text = clientWrapper.data.email;
            updatedClinet2.FullNameText.text = clientWrapper.data.first_name + " " + clientWrapper.data.last_name;
            updatedClinet2.EmailText.text = clientWrapper.data.email;

            SalesRepRequestOverviewHandler.instance.selectedClient.client.name = clientWrapper.data.first_name + " " + clientWrapper.data.last_name;
            SalesRepRequestOverviewHandler.instance.selectedClient.client.email = clientWrapper.data.email;
            SalesRepRequestOverviewHandler.instance.selectedClient.client.phone_number = clientWrapper.data.phone_number;

            string rawNumber = clientWrapper.data.phone_number; // e.g., "1234567890"

            if (rawNumber.Length == 10)
            {
                string formatted = string.Format("{0} {1}-{2}",
                    rawNumber.Substring(0, 3),
                    rawNumber.Substring(3, 3),
                    rawNumber.Substring(6, 4));

                updatedClinet.ContactNoText.text = "+1 " + formatted; // Output: "123 456-7890"
                updatedClinet2.ContactNoText.text = "+1 " + formatted; // Output: "123 456-7890"
            }
            else
            {
                updatedClinet.ContactNoText.text = rawNumber; // fallback for unexpected lengths
                updatedClinet2.ContactNoText.text = rawNumber; // fallback for unexpected lengths
            }

            //updatedClinet.ContactNoText.text = clientWrapper.data.phone_number;

            editClientPopup.SetActive(false);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ResetForm();

            FilterRequestHandler.instance.ClearFilters();
            FilterRequestHandler.instance.ClearFiltersSalesRep();
        }
        else
        {
            Debug.LogError("Edit failed: " + request.error);
        }
    }

    #endregion

}

#region serializableClass

[System.Serializable]
public class ChangeStatusRequest
{
    public int request_id;
    public string status;
    public string note;
}

[System.Serializable]
public class ChangeStatusResponse
{
    public ChangeStatusData data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class ChangeStatusData
{
    public int request_id;
    public string status;
}

[System.Serializable]
public class TimelineItem
{
    public int id;
    public string title;
    public string description;
    public int created_by;
    public bool is_deleted;
    public string created_at;
    public string updated_at;
}

[System.Serializable]
public class TimelineData
{
    public List<TimelineItem> timeline_list;
    public int totalCount;
}

[System.Serializable]
public class TimelineResponse
{
    public TimelineData data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class TimelineRequestBody
{
    public int page;
    public int limit;
    public int request_id;
}

[System.Serializable]
public class EditClientRequest
{
    public int client_id;
    public string first_name;
    public string last_name;
    public string email;
    public string phone_number;
}

[System.Serializable]
public class ClientDataWrapper
{
    public ClientDataResponse data;
}

[System.Serializable]
public class ClientDataResponse
{
    public string first_name;
    public string last_name;
    public string email;
    public string phone_number;
}

#endregion