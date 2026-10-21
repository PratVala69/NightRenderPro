using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ClientHandler : MonoBehaviour
{
    public static ClientHandler instance;

    [Header("API Endpoints")]
    public string fetchClientListEndpoint = "request/getAllRequest";
    public string fetchClientDetailEndpoint = "client/";  // Append {id}
    public string addClientEndpoint = "request/client/add";
    public string fetchClientListForToggleEndpoint = "request/client/list";
    public string sendRequestEndpoint = "request/add";

    [Header("Client Detail Field")]
    public TMP_InputField FirstNameInput;
    public GameObject firstNameErrorText;
    public TMP_InputField LastNameInput;
    public GameObject lastNameErrorText;
    public TMP_InputField EmailInput;
    public GameObject emailErrorText;
    public TMP_InputField ContactNoInput;
    public GameObject contactNumberErrorText;
    public Button nextButton;

    [Header("Client Detail")]
    public GameObject clientDetailPrefab;
    public Transform contentParent;
    private List<string> clientNames = new List<string>();
    public GameObject noClientYet;

    private List<ClientData> clientList = new List<ClientData>();

    [Header("Select Client Panel")]
    public Transform selectClientContainer;
    public GameObject clientNameTogglePrefab;
    public GameObject selectClientPanel;
    public TextMeshProUGUI selectedClient;
    public Button removeSelectedClientButton;
    public string selectedClientName;
    public TMP_InputField searchField;

    [Header("All Request Panel")]
    public Transform allRequestParent;
    private List<ClientData> allClientList = new List<ClientData>();
    public GameObject noRequestYet;

    [Header("Request Panel for CompanyOwner")]
    public Transform allRequestCOParent;
    public GameObject noRequestCOYet;

    [Header("UI Panel")]
    public GameObject newRequestScreen;
    public GameObject newSessionScreen;
    public GameObject allRequestScreen;
    public GameObject allRequestCOScreen;
    public GameObject pricingScreen;
    public GameObject homeScreen;

    public static string selectedClientNameStr;
    public AddRequestResponse addRequestResponse;
    public SessionDetailFetcher sessionDetailFetcher;
    public SalesRepAPIHandler salesRepAPIHandler;


    private void Awake()
    {
        instance = this;
    }

    public void StartFetchingClientList()
    {
        StartCoroutine(GetClientList());
    }

    #region Fetch Client List

    public IEnumerator GetClientList()
    {
        string url = LoginAPIHandler.instance.baseURL + fetchClientListEndpoint;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        string jsonPayload = "{\"page\":1,\"limit\":5}";
        byte[] jsonBytes = new System.Text.UTF8Encoding().GetBytes(jsonPayload);

        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Client List : " + request.downloadHandler.text);

            isCompanyOwner = (PlayerPrefs.GetString("Role", "") == "company_owner");

            RequestResponse response = JsonUtility.FromJson<RequestResponse>(request.downloadHandler.text);
            PopulateClientUI(response.data.request_list);

            yield return StartCoroutine(GetAllClientList());

            yield return StartCoroutine(GetClientListForToggle());

            yield return StartCoroutine(salesRepAPIHandler.GetClientListForToggle());
            yield return StartCoroutine(salesRepAPIHandler.GetClientListForToggleSalesRep());

            FilterRequestHandler.instance.ClearFilters();
            FilterRequestHandler.instance.ClearFiltersSalesRep();

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("List Error: " + request.error);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with fetch client list API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    void PopulateClientUI(List<RequestItem> clients)
    {
        Debug.Log("__Load client data : " + JsonUtility.ToJson(clients));
        noClientYet.SetActive(false);
        clientList.Clear();

        foreach (Transform child in selectClientContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        if (clients.Count > 0)
        {
            foreach (RequestItem client in clients)
            {
                // Instantiate client detail view
                GameObject clientDetailInstance = Instantiate(clientDetailPrefab, contentParent);
                var detailComp = clientDetailInstance.GetComponent<ClientDetailInstance>();
                detailComp.requestItem = client;
                detailComp.FullNameText.text = client.client.first_name + " " + client.client.last_name;
                detailComp.EmailText.text = client.client.email;

                string rawNumber = client.client.phone_number; // e.g., "1234567890"

                if (rawNumber.Length == 10)
                {
                    string formatted = string.Format("{0} {1}-{2}",
                        rawNumber.Substring(0, 3),
                        rawNumber.Substring(3, 3),
                        rawNumber.Substring(6, 4));

                    detailComp.ContactNoText.text = "+1 " + formatted; // Output: "123 456-7890"
                }
                else
                {
                    detailComp.ContactNoText.text = rawNumber; // fallback for unexpected lengths
                }

                //detailComp.ContactNoText.text = client.client.phone_number;
                detailComp.TimeDateText.text = FormatDate(client.created_at);

                detailComp.ProductCountText.text = client.product_count.ToString();
                detailComp.SessionCountText.text = client.session_count.ToString();

                detailComp.requestId = client.id;
                detailComp.pdfUrl = client.pdf_url;

                detailComp.Init();

                if (!string.IsNullOrEmpty(client.pdf_url))
                {
                    detailComp.pdfDownloadButton.interactable = true;
                    detailComp.pdfMailButton.interactable = true;
                }
                else
                {
                    detailComp.pdfDownloadButton.interactable = false;
                    detailComp.pdfMailButton.interactable = false;
                }

                string currentStatus = client.status.ToLower();
                foreach (GameObject go in detailComp.status)
                {
                    go.SetActive(false);
                }

                foreach (GameObject go in detailComp.status)
                {
                    if (go.name.ToLower() == currentStatus)
                    {
                        go.SetActive(true);
                        break;
                    }
                }
            }
            Debug.Log("Client data loaded successfully.");
        }
        else
        {
            noClientYet.SetActive(true);
            Debug.Log("No previous client data found.");
        }
    }


    #endregion

    #region Get Single Client Details
    public IEnumerator GetClientDetails(int clientId)
    {
        string url = LoginAPIHandler.instance.baseURL + fetchClientDetailEndpoint + clientId;

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ClientDetailResponse response = JsonUtility.FromJson<ClientDetailResponse>(request.downloadHandler.text);
            Debug.Log("Client Details: " + response.data.first_name + " " + response.data.last_name);
            // You can show this info in a panel or popup
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Detail Error: " + request.error);
            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with get client detail API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }
    #endregion

    #region Add New Client

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

    public void RequestCreate()
    {
        if (CheckSelectedToggle())
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(true);
            StartCoroutine(SendRequest());
        }
        else
        {
            bool hasError = false;

            if (string.IsNullOrWhiteSpace(FirstNameInput.text))
            {
                firstNameErrorText.SetActive(true);
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(LastNameInput.text))
            {
                lastNameErrorText.SetActive(true);
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(EmailInput.text))
            {
                emailErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter Email";
                emailErrorText.SetActive(true);
                hasError = true;
            }
            else if (!IsValidEmail(EmailInput.text))
            {
                emailErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter valid Email";
                emailErrorText.SetActive(true);
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(ContactNoInput.text))
            {
                contactNumberErrorText.GetComponent<TextMeshProUGUI>().text = "Please enter Phone number";
                contactNumberErrorText.SetActive(true);
                hasError = true;
            }
            else if (!IsValidPhoneNumber(ContactNoInput.text))
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

            StartCoroutine(AddClientToServer(FirstNameInput.text, LastNameInput.text, EmailInput.text, ContactNoInput.text));
        }
    }

    public void ResetClientScreen()
    {
        Toggle[] toggles = selectClientContainer.GetComponentsInChildren<Toggle>();
        foreach (Toggle other in toggles)
        {
            other.isOn = false;
        }

        selectedClient.text = "Select Client";
        removeSelectedClientButton.gameObject.SetActive(false);
        FirstNameInput.interactable = true;
        LastNameInput.interactable = true;
        EmailInput.interactable = true;
        ContactNoInput.interactable = true;

        FirstNameInput.text = "";
        LastNameInput.text = "";
        EmailInput.text = "";
        ContactNoInput.text = "";
    }

    void Start()
    {
        FirstNameInput.onValueChanged.AddListener((value) => UpdateNextButtonState());
        LastNameInput.onValueChanged.AddListener((value) => UpdateNextButtonState());
        EmailInput.onValueChanged.AddListener((value) => UpdateNextButtonState());
        ContactNoInput.onValueChanged.AddListener(OnLoginInputChanged);
        nextButton.interactable = false; // Default state
        //StartFetchingClientList();
    }

    void OnDestroy()
    {
        if (FirstNameInput != null) FirstNameInput.onValueChanged.RemoveListener((value) => UpdateNextButtonState());
        if (LastNameInput != null) LastNameInput.onValueChanged.RemoveListener((value) => UpdateNextButtonState());
        if (EmailInput != null) EmailInput.onValueChanged.RemoveListener((value) => UpdateNextButtonState());
        if (ContactNoInput != null) ContactNoInput.onValueChanged.RemoveListener(OnLoginInputChanged);
    }

    public void OnLoginInputChanged(string input)
    {
        string snum = System.Text.RegularExpressions.Regex.Replace(input, @"[^0-9]", string.Empty) ?? string.Empty;
        if (snum.Length > 10) snum = snum.Substring(0, 10);

        if (snum.Length <= 3)
        {
            // Just first block
        }
        else if (snum.Length <= 6)
        {
            // Add space after 3 digits
            snum = $"{snum.Substring(0, 3)} {snum.Substring(3)}";
        }
        else
        {
            // Add space and dash: 123 456-7890
            snum = $"{snum.Substring(0, 3)} {snum.Substring(3, 3)}-{snum.Substring(6)}";
        }

        ContactNoInput.text = snum;

        if (snum.Length != input.Length)
        {
            ContactNoInput.ForceLabelUpdate();
            ContactNoInput.MoveTextEnd(false);
        }

        UpdateNextButtonState();
    }

    private void UpdateNextButtonState()
    {
        if (nextButton == null) return;

        bool isEmailValid = !string.IsNullOrWhiteSpace(EmailInput.text) &&
                           Regex.IsMatch(EmailInput.text, @"^[^@]+@[^@]+\.[^@]+$");
        bool isNewClientValid = !string.IsNullOrWhiteSpace(FirstNameInput.text) &&
                               !string.IsNullOrWhiteSpace(LastNameInput.text) &&
                               isEmailValid &&
                               ContactNoInput.text.Length == 12; // e.g., "123 456-7890"

        bool isClientSelected = CheckSelectedToggle();
        nextButton.interactable = isNewClientValid || isClientSelected;
    }

    public bool CheckSelectedToggle()
    {
        Toggle[] toggles = selectClientContainer.GetComponentsInChildren<Toggle>();
        foreach (Toggle toggle in toggles)
        {
            if (toggle.isOn)
            {
                selectedClientName = toggle.gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text;
                selectedClientNameStr = selectedClientName;
                return true;
            }
        }
        selectedClientName = "";
        return false;
    }

    public void OnSearchValueChanged()
    {
        string searchText = searchField.text.ToLower();

        foreach (Transform child in selectClientContainer)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                TextMeshProUGUI label = toggle.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    // Check if toggle text matches search
                    bool match = label.text.ToLower().Contains(searchText);
                    child.gameObject.SetActive(match);
                }
            }
        }
    }

    [System.Serializable]
    public class ClientDataResponse
    {
        public int id;
        public int client_id;
        public int user_id;
        public int company_id;
        public string first_name;
        public string last_name;
        public string email;
        public string phone_number;
        public bool is_active;
        public bool is_deleted;
        public string created_at;
        public string updated_at;
    }

    [System.Serializable]
    public class ClientResponse
    {
        public ClientDataResponse data;
        public int statusCode;
        public string message;
    }

    IEnumerator AddClientToServer(string firstName, string lastName, string email, string phoneNumber)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        string digitsOnly = "";
        if (phoneNumber.Length >= 10) // Check if the input has exactly 10 characters
        {
            foreach (char c in phoneNumber)
            {
                if (char.IsDigit(c))
                    digitsOnly += c;
            }
        }

        var requestData = new ClientData
        {
            first_name = firstName,
            last_name = lastName,
            email = email,
            phone_number = digitsOnly
        };

        string jsonData = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + "request/client/add", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Client added successfully!");
            ResetClientInputFields();
            yield return StartCoroutine(GetClientList());

            ClientResponse clientResponse = JsonUtility.FromJson<ClientResponse>(request.downloadHandler.text);
            selectedRequestId = clientResponse.data.id;
            Debug.Log("Request Id : " + selectedRequestId);
            PlayerPrefs.SetInt("RequestId", selectedRequestId);
            PlayerPrefs.Save();

            Debug.Log("Client Detail Created & Saved Successfully!");

            yield return StartCoroutine(sessionDetailFetcher.GetSessionDetails());

            ResetClientScreen();

            //newRequestScreen.SetActive(false);
            newSessionScreen.SetActive(true);

            FilterRequestHandler.instance.ClearFilters();
            FilterRequestHandler.instance.ClearFiltersSalesRep();

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);

            UpdateNextButtonState();
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            ErrorResponse error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
            Debug.LogError("Error adding client: " + request.downloadHandler.text);

            ErrorPopupHandler.instance.errorDescText.text = "Please enter valid Email or Contact number";//"Profile photo download failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

 
    void ResetClientInputFields()
    {
        FirstNameInput.text = "";
        LastNameInput.text = "";
        EmailInput.text = "";
        ContactNoInput.text = "";

        firstNameErrorText.SetActive(false);
        lastNameErrorText.SetActive(false);
        emailErrorText.SetActive(false);
        contactNumberErrorText.SetActive(false);
    }

    #endregion

    #region Fetch All Client

    public bool isCompanyOwner = false;

    public void CheckRequestPanelNeedsToEnable()
    {
        if (!isCompanyOwner)
            allRequestScreen.SetActive(true);
        else
            allRequestCOScreen.SetActive(true);
    }

    GameObject currentScreenObj;
    public void LoadAllClient_ScreenDisable(GameObject currentScreen)
    {
        currentScreenObj = currentScreen;
        StartCoroutine(GetAllClientList());
    }

    public IEnumerator GetAllClientList()
    {
        string url = LoginAPIHandler.instance.baseURL + fetchClientListEndpoint;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        string jsonPayload = "{\"page\":1,\"limit\":100}";
        byte[] jsonBytes = new System.Text.UTF8Encoding().GetBytes(jsonPayload);

        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Client all List : " + request.downloadHandler.text);

            RequestResponse response = JsonUtility.FromJson<RequestResponse>(request.downloadHandler.text);
            PopulateAllClientUI(response.data.request_list);
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("List Error: " + request.error);
            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with get all request API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    GameObject clientDetailInstance;
    void PopulateAllClientUI(List<RequestItem> clients)
    {
        //if (!isCompanyOwner)
        //    allRequestScreen.SetActive(false);
        //else
        //    allRequestCOScreen.SetActive(false);

        allClientList.Clear();

        if (!isCompanyOwner)
        {
            foreach (Transform child in allRequestParent)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            foreach (Transform child in allRequestCOParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Load from file
        if (clients.Count > 0)
        {
            foreach (RequestItem client in clients)
            {
                // Instantiate client detail view

                if(!isCompanyOwner)
                     clientDetailInstance = Instantiate(clientDetailPrefab, allRequestParent);
                else
                    clientDetailInstance = Instantiate(clientDetailPrefab, allRequestCOParent);

                var detailComp = clientDetailInstance.GetComponent<ClientDetailInstance>();
                detailComp.requestItem = client;
                detailComp.FullNameText.text = client.client.first_name + " " + client.client.last_name;
                detailComp.EmailText.text = client.client.email;

                string rawNumber = client.client.phone_number; // e.g., "1234567890"

                if (rawNumber.Length == 10)
                {
                    string formatted = string.Format("{0} {1}-{2}",
                        rawNumber.Substring(0, 3),
                        rawNumber.Substring(3, 3),
                        rawNumber.Substring(6, 4));

                    detailComp.ContactNoText.text = "+1 " + formatted; // Output: "123 456-7890"
                }
                else
                {
                    detailComp.ContactNoText.text = rawNumber; // fallback for unexpected lengths
                }

                //detailComp.ContactNoText.text = client.client.phone_number;
                detailComp.TimeDateText.text = FormatDate(client.created_at);

                detailComp.ProductCountText.text = client.product_count.ToString();
                detailComp.SessionCountText.text = client.session_count.ToString();

                detailComp.requestId = client.id;
                detailComp.pdfUrl = client.pdf_url;

                detailComp.Init();

                if (!string.IsNullOrEmpty(client.pdf_url))
                {
                    detailComp.pdfDownloadButton.interactable = true;
                    detailComp.pdfMailButton.interactable = true;
                }
                else
                {
                    detailComp.pdfDownloadButton.interactable = false;
                    detailComp.pdfMailButton.interactable = false;
                }

                string currentStatus = client.status.ToLower();
                foreach (GameObject go in detailComp.status)
                {
                    go.SetActive(false);
                }

                foreach (GameObject go in detailComp.status)
                {
                    if (go.name.ToLower() == currentStatus)
                    {
                        go.SetActive(true);
                        break;
                    }
                }
            }
            Debug.Log("Client data loaded successfully.");

            if (currentScreenObj != null)
            {
                if (!isCompanyOwner)
                    allRequestScreen.SetActive(true);
                else
                    allRequestCOScreen.SetActive(true);

                currentScreenObj.SetActive(false);

                currentScreenObj = null;
            }

        }
        else
        {
            if (!isCompanyOwner)
                noRequestYet.SetActive(true);
            else
                noRequestCOYet.SetActive(true);
            Debug.Log("No previous client data found.");
        }
    }

    string FormatDate(string isoDate)
    {
        if (System.DateTime.TryParse(isoDate, out System.DateTime dt))
            return dt.ToString("dd MMM\nhh:mm tt"); // Example: 16 Apr 12:23 PM
        return isoDate;
    }

    #endregion

    #region Client list for toggle group
    [System.Serializable]
    public class ClientInfo
    {
        public int id;
        public string first_name;
        public string last_name;
        public string email;
        public string phone_number;
    }

    [System.Serializable]
    public class ClientListWrapper
    {
        public List<ClientInfo> data;
        public int statusCode;
        public string message;
    }
    public int selectedClientId;
    public int selectedRequestId;

    public IEnumerator GetClientListForToggle()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        UnityWebRequest request = UnityWebRequest.Get(LoginAPIHandler.instance.baseURL + fetchClientListForToggleEndpoint);
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        foreach (Transform child in selectClientContainer)
        {
            Destroy(child.gameObject);
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Client list for toggle fetched!" + request.downloadHandler.text);

            ClientListWrapper wrapper = JsonUtility.FromJson<ClientListWrapper>(request.downloadHandler.text);

            Debug.Log(wrapper.data.Count);
            for (int i = 0; i < wrapper.data.Count; i++)
            {
                var client = wrapper.data[i]; // Capture local reference

                GameObject tempObj = Instantiate(clientNameTogglePrefab, selectClientContainer);
                Toggle toggle = tempObj.GetComponent<Toggle>();
                toggle.group = selectClientContainer.GetComponent<ToggleGroup>();

                tempObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = client.first_name + " " + client.last_name;

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        selectedClientName = client.first_name + " " + client.last_name;
                        selectedClientId = client.id;
                        ClientDetail_Close();

                        Debug.Log("Selected Client ID: " + selectedClientId);
                    }
                });
            }

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else
        {
            Debug.LogError("Failed to fetch client list: " + request.error);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.errorDescText.text = "Unable to fetch client list.";
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    public void ClientDetail_Close()
    {
        if (CheckSelectedToggle())
        {
            selectedClient.text = selectedClientName;
            removeSelectedClientButton.gameObject.SetActive(true);
            FirstNameInput.interactable = false;
            LastNameInput.interactable = false;
            EmailInput.interactable = false;
            ContactNoInput.interactable = false;
        }
        else
        {
            selectedClient.text = "Select Client";
            removeSelectedClientButton.gameObject.SetActive(false);
            FirstNameInput.interactable = true;
            LastNameInput.interactable = true;
            EmailInput.interactable = true;
            ContactNoInput.interactable = true;
        }
        UpdateNextButtonState();
        selectClientPanel.GetComponent<Animation>().Play("PanelCloseAnim");
    }

    public void RemoveSelectedClient_Close()
    {
        selectedClient.text = "Select Client";
        removeSelectedClientButton.gameObject.SetActive(false);
        FirstNameInput.interactable = true;
        LastNameInput.interactable = true;
        EmailInput.interactable = true;
        ContactNoInput.interactable = true;
        UpdateNextButtonState();

        foreach (Transform child in selectClientContainer)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = false;
            }
        }
    }


    #endregion

    #region Send Request

    [System.Serializable]
    public class AddRequestData
    {
        public int client_id;
        public int company_id;
        public string status;
        public int created_by;
        public string address;
        public string notes;
        public int id;
        public bool is_deleted;
        public string created_at;
        public string updated_at;
    }

    [System.Serializable]
    public class AddRequestResponse
    {
        public AddRequestData data;
        public int statusCode;
        public string message;
    }

    public int ExternalClientId;
    public bool isExternalClientId = false;
    public void SendRequestWithClientId(int clientId)
    {
        ExternalClientId = clientId;
        //isExternalClientId = true;
        //StartCoroutine(SendRequest());

        //PopulateEmptySessionList_PendingStatus();

        //StartCoroutine(GetClientList());
        StartCoroutine(sessionDetailFetcher.GetSessionDetails());
        newSessionScreen.SetActive(true);
    }

    string jsonData;
    IEnumerator SendRequest()
    {
        // JSON payload
        if(isExternalClientId)
            jsonData = JsonUtility.ToJson(new ClientRequest { client_id = ExternalClientId });
        else
            jsonData = JsonUtility.ToJson(new ClientRequest { client_id = selectedClientId });

        // Create the request
        UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + sendRequestEndpoint, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        // Set headers
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
        request.SetRequestHeader("accept", "application/json");

        // Send the request
        yield return request.SendWebRequest();

        // Handle response
#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.Success)
#else
        if (!request.isNetworkError && !request.isHttpError)
#endif
        {
            addRequestResponse = JsonUtility.FromJson<AddRequestResponse>(request.downloadHandler.text);
            Debug.Log("✅ Request Created: " + request.downloadHandler.text);

            selectedRequestId = addRequestResponse.data.id;
            Debug.Log("Request Id : " + selectedRequestId);
            PlayerPrefs.SetInt("RequestId", selectedRequestId);
            PlayerPrefs.Save();

            yield return StartCoroutine(GetClientList());
            yield return StartCoroutine(sessionDetailFetcher.GetSessionDetails());

            FilterRequestHandler.instance.ClearFilters();
            FilterRequestHandler.instance.ClearFiltersSalesRep();

            ResetClientInputFields();

            Toggle[] toggles = selectClientContainer.GetComponentsInChildren<Toggle>();
            foreach (Toggle other in toggles)
            {
                other.isOn = false;
            }

            selectedClient.text = "Select Client";
            removeSelectedClientButton.gameObject.SetActive(false);
            FirstNameInput.interactable = true;
            LastNameInput.interactable = true;
            EmailInput.interactable = true;
            ContactNoInput.interactable = true;

            //newRequestScreen.SetActive(false);
            newSessionScreen.SetActive(true);

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);

            Debug.Log("Old Client!");
        }
        else
        {
            Debug.LogError("❌ Request Failed: " + request.error + "\n" + request.downloadHandler.text);
        }
    }

    // JSON structure matching the request body
    [System.Serializable]
    public class ClientRequest
    {
        public int client_id;
    }

    #endregion

    #region searchFunctionality_Request

    public TMP_InputField searchInputfield_Request;
    public void OnSearchInputChanged_Request()
    {
        FilterAndToggleCards(searchInputfield_Request.text);
    }

    void FilterAndToggleCards(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            // Show all cards if no search term
            foreach (Transform child in allRequestParent)
            {
                child.gameObject.SetActive(true);
            }
            noRequestYet.SetActive(allRequestParent.transform.childCount == 0);
            return;
        }

        searchTerm = searchTerm.ToLower();
        bool anyMatch = false;

        foreach (Transform child in allRequestParent)
        {
            ClientDetailInstance client = child.GetComponent<ClientDetailInstance>();
            bool matches = (client.FullNameText.text).ToLower().Contains(searchTerm) ||
                          client.EmailText.text.ToLower().Contains(searchTerm) ||
                          client.ContactNoText.text.Contains(searchTerm) ||
                          client.requestId.ToString().Contains(searchTerm);

            child.gameObject.SetActive(matches);
            if (matches) anyMatch = true;
        }

        noRequestYet.SetActive(!anyMatch);
    }

    public void OnClearSearch_Request()
    {
        searchInputfield_Request.text = "";
        OnSearchInputChanged_Request(); // reset
    }

    #endregion

    #region searchFunctionality_MyRequest

    public TMP_InputField searchInputfield_MyRequest;
    public void OnSearchInputChanged_MyRequest()
    {
        FilterAndToggleCards_MyReq(searchInputfield_MyRequest.text);
    }

    void FilterAndToggleCards_MyReq(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            // Show all cards if no search term
            foreach (Transform child in allRequestCOParent)
            {
                child.gameObject.SetActive(true);
            }
            noRequestCOYet.SetActive(allRequestCOParent.transform.childCount == 0);
            return;
        }

        searchTerm = searchTerm.ToLower();
        bool anyMatch = false;

        foreach (Transform child in allRequestCOParent)
        {
            ClientDetailInstance client = child.GetComponent<ClientDetailInstance>();
            bool matches = (client.FullNameText.text).ToLower().Contains(searchTerm) ||
                          client.EmailText.text.ToLower().Contains(searchTerm) ||
                          client.ContactNoText.text.Contains(searchTerm) ||
                          client.requestId.ToString().Contains(searchTerm);

            child.gameObject.SetActive(matches);
            if (matches) anyMatch = true;
        }

        noRequestCOYet.SetActive(!anyMatch);
    }

    public void OnClearSearch_MyRequest()
    {
        searchInputfield_MyRequest.text = "";
        OnSearchInputChanged_MyRequest(); // reset
    }

    #endregion

    #region SearchFunctionality_SalesRepRequest

    public TMP_InputField searchInputfield_SalesRepRequest;
    public GameObject noRequestSalesRepYet;
    public Transform salesRepRequestParent;

    public void OnSearchInputChanged_SalesRepRequest()
    {
        FilterAndToggleCards_SalesRepRequest(searchInputfield_SalesRepRequest.text);
    }

    void FilterAndToggleCards_SalesRepRequest(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            // Show all cards if no search term
            foreach (Transform child in salesRepRequestParent)
            {
                child.gameObject.SetActive(true);
            }
            noRequestSalesRepYet.SetActive(salesRepRequestParent.transform.childCount == 0);
            return;
        }

        searchTerm = searchTerm.ToLower();
        bool anyMatch = false;

        foreach (Transform child in salesRepRequestParent)
        {
            ClientDetailInstance client = child.GetComponent<ClientDetailInstance>();
            bool matches = (client.FullNameText.text).ToLower().Contains(searchTerm) ||
                          client.EmailText.text.ToLower().Contains(searchTerm) ||
                          client.ContactNoText.text.Contains(searchTerm) ||
                          client.requestId.ToString().Contains(searchTerm);

            child.gameObject.SetActive(matches);
            if (matches) anyMatch = true;
        }

        noRequestSalesRepYet.SetActive(!anyMatch);
    }

    public void OnClearSearch_SalesRepRequest()
    {
        searchInputfield_SalesRepRequest.text = "";
        OnSearchInputChanged_SalesRepRequest(); // reset
    }

    #endregion

    #region Serializable Models

    [System.Serializable]
    public class ClientListRequest
    {
        public string search;
        public int page;
        public int limit;
        public string sort_order;
        public string sort_by;
        public int user_id;
    }

    [System.Serializable]
    public class ClientListResponse
    {
        public ClientListData data;
        public int statusCode;
        public string message;
    }

    [System.Serializable]
    public class ClientListData
    {
        public List<ClientData> client_list;
        public int total_count;
    }

    [System.Serializable]
    public class ClientData
    {
        public int id;
        public string first_name;
        public string last_name;
        public string email;
        public string phone_number;
        public string last_request_created;
        public string address;
        public int request_count;
    }

    [System.Serializable]
    public class ClientDetailResponse
    {
        public SingleClient data;
        public int statusCode;
        public string message;
    }

    [System.Serializable]
    public class SingleClient
    {
        public int id;
        public string first_name;
        public string last_name;
        public string email;
        public string phone_number;
        public bool is_active;
        public string created_at;
    }

    [System.Serializable]
    public class ClientInfoRequest
    {
        public int id;
        public int user_id;
        public int company_id;
        public string first_name;
        public string last_name;
        public string email;
        public string phone_number;
        public bool is_active;
        public bool is_deleted;
        public string created_at;
        public string updated_at;
    }

    [System.Serializable]
    public class RequestItem
    {
        public int id;
        public ClientInfoRequest client;
        public string created_at;
        public string status;
        public string pdf_url;
        public int session_count;
        public int product_count;
    }

    [System.Serializable]
    public class RequestData
    {
        public List<RequestItem> request_list;
        public int total_count;
    }

    [System.Serializable]
    public class RequestResponse
    {
        public RequestData data;
        public int statusCode;
        public string message;
    }
    [System.Serializable]
    public class ErrorResponse
    {
        public string[] message;
    }

    #endregion

}
