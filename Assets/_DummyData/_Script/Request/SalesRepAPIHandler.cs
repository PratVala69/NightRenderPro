using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using TMPro;
using UnityEngine.UI;
using static ClientHandler;

public class SalesRepAPIHandler : MonoBehaviour
{
    public string salesRepListEndpoint = "/sales-representative/list";
    public GameObject salesRepCardPrefab;
    public Transform salesRepContainer;
    private List<GameObject> allSalesRepCards = new List<GameObject>();
    public SalesRepRequestCard salesRepRequestCard;

    [Header("Request Handler")]
    public ClientHandler clientHandler;
    public Transform salesRepRequestContainer;
    public GameObject noRequestYet;
    public GameObject noRequestYetSalesRep;
    public GameObject clientDetailPrefab;

    [Header("UI Screen")]
    public GameObject salesRepListScreen;
    public GameObject selectedSalesRepRequestScreen;

    [Header("Client Selection")]
    public Transform selectClientContainer;
    public GameObject clientNameTogglePrefab;
    public GameObject selectClientPanel;
    public TMP_InputField searchField;

    [Header("Client Selection For SalesRep")]
    public Transform selectClientContainerSalesRep;
    public GameObject selectClientPanelSalesRep;
    public TMP_InputField searchFieldSalesRep;

    public SalesRepresentative currentSalesRep;
    public bool isShow = true;

    #region SalesRepList
    public void FetchSalesRep()
    {
        StartCoroutine(FetchSalesRepresentatives());
    }

    public IEnumerator FetchSalesRepresentatives()
    {
        string url = LoginAPIHandler.instance.baseURL + salesRepListEndpoint;
        Debug.Log("Fetching Sales Representatives: " + url);

        // Prepare request body
        string jsonBody = JsonUtility.ToJson(new SalesRepListRequest()
        {
            page = 1,
            limit = 30,
            search = "",
            company_id = PlayerPrefs.GetInt("CompanyOwnerId")
        }); ;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("SalesRep Response: " + request.downloadHandler.text);

            SalesRepresentativeResponse response = JsonUtility.FromJson<SalesRepresentativeResponse>(FixJson(request.downloadHandler.text));

            if (response != null && response.data != null)
            {
                if (response.data.sales_representatives_list.Count == 0)
                {
                    noRequestYetSalesRep.SetActive(true);
                }
                else
                {
                    noRequestYetSalesRep.SetActive(false);
                    Debug.Log("Total Sales Reps: " + response.data.total_count);

                    foreach (var rep in response.data.sales_representatives_list)
                    {
                        Debug.Log($"Rep: {rep.first_name} {rep.last_name}, Email: {rep.email}, Active: {rep.is_active}");
                    }

                    StartCoroutine(PopulateSalesRepUI(response.data.sales_representatives_list));
                }
            }
        }
        else if (request.responseCode == 403)
        {
            Debug.LogError("Unauthorized (403). Showing login again popup.");
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError($"Failed to fetch Sales Reps: {request.responseCode} - {request.error}");
        }
    }

    private IEnumerator PopulateSalesRepUI(List<SalesRepresentative> reps)
    {
        // Clear old cards
        foreach (GameObject card in allSalesRepCards)
        {
            Destroy(card);
        }
        allSalesRepCards.Clear();

        List<IEnumerator> downloadCoroutines = new List<IEnumerator>();

        foreach (var rep in reps)
        {
            GameObject cardObj = Instantiate(salesRepCardPrefab, salesRepContainer);
            SalesRepRequestCard cardUI = cardObj.GetComponent<SalesRepRequestCard>();

            if (cardUI != null)
            {
                cardUI.SetSalesRepData(rep); // only sets text + status, not image
                allSalesRepCards.Add(cardObj);

                cardUI.requestOverviewButton.onClick.AddListener(() =>
                {
                    StartCoroutine(FetchRequests(rep));
                });

                if (rep.profile_picture != null && !string.IsNullOrEmpty(rep.profile_picture.location_key))
                {
                    downloadCoroutines.Add(DownloadAndSetImage(rep.profile_picture.location_key, cardUI));
                }
                else
                {
                    cardUI.SetDefaultProfile();
                }
            }
        }

        // Wait until all image downloads are done
        foreach (var coroutine in downloadCoroutines)
        {
            yield return StartCoroutine(coroutine);
        }

        Debug.Log("✅ All profile images downloaded!");
        ErrorPopupHandler.instance.loadingScreen.SetActive(false);
    }

    private IEnumerator DownloadAndSetImage(string locationKey, SalesRepRequestCard cardUI)
    {
        string url = LoginAPIHandler.instance.imageDownloadUrl + locationKey;
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogWarning("Image download failed: " + url + " -> " + request.error);
                cardUI.SetDefaultProfile();
            }
            else
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                cardUI.SetProfileImage(downloadedTexture);
            }
        }
    }

    private string FixJson(string value)
    {
        // sometimes API returns a single object instead of array -> Fix
        if (!value.StartsWith("{"))
            value = "{\"data\":" + value + "}";
        return value;
    }
    #endregion

    #region SalesRepRequest
    public int selectedSalesRepId;

    [Serializable]
    public class GetAllRequestPayload
    {
        public int page;
        public int limit;
        public int sales_representative_id;
    }

    public IEnumerator FetchRepeatRequet(bool isUIShow)
    {
        isShow = isUIShow;
        yield return FetchRequests(currentSalesRep);
    }

    public IEnumerator FetchRequests(SalesRepresentative rep)
    {
        currentSalesRep = rep;
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        yield return SetSelectedSalesRepresentative(rep);
        currentSalesRep = rep;

        string url = LoginAPIHandler.instance.baseURL + clientHandler.fetchClientListEndpoint;
        Debug.Log("Fetching Requests: " + url);
        selectedSalesRepId = rep.id;

        // Request body
        string jsonBody = JsonUtility.ToJson(new GetAllRequestPayload()
        {
            page = 1,
            limit = 30,
            sales_representative_id = rep.id
        });

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Requests Response: " + request.downloadHandler.text);

            RequestResponse response = JsonUtility.FromJson<RequestResponse>(FixJson(request.downloadHandler.text));

            if (response != null && response.data != null)
            {
                Debug.Log("Total Requests: " + response.data.total_count);

                PopulateRequestUI(response.data);
            }
        }
        else if (request.responseCode == 403)
        {
            Debug.LogError("Unauthorized (403). Forcing login again.");
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Failed to fetch request list: " + request.error);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.errorDescText.text = "Unable to fetch sales representative request list.";
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    IEnumerator SetSelectedSalesRepresentative(SalesRepresentative rep)
    {
        currentSalesRep = rep;

        if (PlayerPrefs.GetString("Role", "") == "company_owner")
        {
            salesRepRequestCard.callButton.gameObject.SetActive(true);
        }
        else
        {
            salesRepRequestCard.callButton.gameObject.SetActive(false);
        }

        if (PlayerPrefs.GetString("phoneNumber") == rep.phone_number)
        {
            salesRepRequestCard.callButton.gameObject.SetActive(false);
        }


        salesRepRequestCard.salesRepNameText.text = $"{rep.first_name} {rep.last_name}";
        salesRepRequestCard.salesRepEmailText.text = string.IsNullOrEmpty(rep.email) ? "Email Not Found" : rep.email;
        salesRepRequestCard.activeStatusIcon.color = rep.is_active ? salesRepRequestCard.activeColor : salesRepRequestCard.inactiveColor;
        salesRepRequestCard.repData.phone_number = rep.phone_number;

        string urlForImg = LoginAPIHandler.instance.imageDownloadUrl + rep.profile_picture.location_key;
        using (UnityWebRequest requestForImg = UnityWebRequestTexture.GetTexture(urlForImg))
        {
            yield return requestForImg.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (requestForImg.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogWarning("Image download failed: " + urlForImg + " -> " + requestForImg.error);
                salesRepRequestCard.profileImage.sprite = salesRepRequestCard.defaultProfileSprite;
            }
            else
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(requestForImg);
                int size = Mathf.Min(downloadedTexture.width, downloadedTexture.height);
                int x = (downloadedTexture.width - size) / 2;
                int y = (downloadedTexture.height - size) / 2;

                Rect cropRect = new Rect(x, y, size, size);
                Sprite newSprite = Sprite.Create(downloadedTexture, cropRect, new Vector2(0.5f, 0.5f));

                salesRepRequestCard.profileImage.sprite = newSprite;
            }
        }
    }

    GameObject clientDetailInstance;
    private void PopulateRequestUI(ClientHandler.RequestData requestResponse)
    {
        noRequestYet.SetActive(false);
        foreach (Transform child in salesRepRequestContainer)
        {
            Destroy(child.gameObject);
        }

        List<RequestItem> requests = requestResponse.request_list;
        if (requests.Count > 0)
        {
            foreach (RequestItem client in requests)
            {
                // Debug.Log("Client : " + client.id + "--" + client.client.id);
                clientDetailInstance = Instantiate(clientDetailPrefab, salesRepRequestContainer);

                var detailComp = clientDetailInstance.GetComponent<ClientDetailInstance>();
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
                detailComp.TimeDateText.text = DateTime.Parse(client.client.created_at).ToString("dd MMM\nhh:mm tt");

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
            noRequestYet.SetActive(true);
        }

        ErrorPopupHandler.instance.loadingScreen.SetActive(false);

        if (isShow)
        {
            selectedSalesRepRequestScreen.SetActive(true);
            salesRepListScreen.SetActive(false);
        }
        else
        {
            isShow = true;
        }
    }

    #endregion

    #region selectClientList

    public IEnumerator GetClientListForToggle()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        UnityWebRequest request = UnityWebRequest.Get(LoginAPIHandler.instance.baseURL + clientHandler.fetchClientListForToggleEndpoint);
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
                ClientInfo client = wrapper.data[i]; // Capture local reference

                GameObject tempObj = Instantiate(clientNameTogglePrefab, selectClientContainer);
                Toggle toggle = tempObj.GetComponent<Toggle>();
                //toggle.group = selectClientContainer.GetComponent<ToggleGroup>();

                tempObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = client.first_name + " " + client.last_name;

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        FilterRequestHandler.instance.AddClient(client);
                    }
                    else
                    {
                        FilterRequestHandler.instance.RemoveClient(client);
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
        selectClientPanel.GetComponent<Animation>().Play("PanelCloseAnim");
    }

    public void RemoveSelectedClient_Close()
    {
        foreach (Transform child in selectClientContainer)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = false;
            }
        }
    }

    public void UncheckClient(string fullName)
    {
        foreach (Transform child in selectClientContainer)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            string toggleName = child.GetChild(2).GetComponent<TextMeshProUGUI>().text;
            if (toggleName == fullName)
            {
                toggle.isOn = false;
                break;
            }
        }
    }

    public void OnSearchValueChanged()
    {
        string searchText = searchField.text.ToLower();

        foreach (Transform child in selectClientContainer)
        {
            if (child != null)
            {
                TextMeshProUGUI label = child.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    // Check if toggle text matches search
                    bool match = label.text.ToLower().Contains(searchText);
                    child.gameObject.SetActive(match);
                }
            }
        }
    }

    #endregion

    #region selectClientListSalesRep

    public IEnumerator GetClientListForToggleSalesRep()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        UnityWebRequest request = UnityWebRequest.Get(LoginAPIHandler.instance.baseURL + clientHandler.fetchClientListForToggleEndpoint);
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        foreach (Transform child in selectClientContainerSalesRep)
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
                ClientInfo client = wrapper.data[i]; // Capture local reference

                GameObject tempObj = Instantiate(clientNameTogglePrefab, selectClientContainerSalesRep);
                Toggle toggle = tempObj.GetComponent<Toggle>();
                //toggle.group = selectClientContainerSalesRep.GetComponent<ToggleGroup>();

                tempObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = client.first_name + " " + client.last_name;

                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        FilterRequestHandler.instance.AddClientSalesRep(client);
                        //ClientDetail_CloseSalesRep();

                        Debug.Log("Selected Client ID: " + client.first_name + " " + client.last_name);
                    }
                    else
                    {
                        FilterRequestHandler.instance.RemoveClientSalesRep(client);
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

    public void ClientDetail_CloseSalesRep()
    {
        selectClientPanelSalesRep.GetComponent<Animation>().Play("PanelCloseAnim");
    }

    public void RemoveSelectedClient_CloseSalesRep()
    {
        foreach (Transform child in selectClientContainerSalesRep)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = false;
            }
        }
    }

    public void UncheckClientSalesRep(string fullName)
    {
        foreach (Transform child in selectClientContainerSalesRep)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            string toggleName = child.GetChild(2).GetComponent<TextMeshProUGUI>().text;
            if (toggleName == fullName)
            {
                toggle.isOn = false;
                break;
            }
        }
    }

    public void OnSearchValueChangedSalesRep()
    {
        string searchText = searchFieldSalesRep.text.ToLower();

        foreach (Transform child in selectClientContainerSalesRep)
        {
            //Toggle toggle = child.GetComponent<Toggle>();
            if (child != null)
            {
                TextMeshProUGUI label = child.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    // Check if toggle text matches search
                    bool match = label.text.ToLower().Contains(searchText);
                    child.gameObject.SetActive(match);
                }
            }
        }
    }

    #endregion

    #region searchFunctionality_SalesRep

    public TMP_InputField searchInputfield_SalesRep;
    public void OnSearchInputChanged_SalesRep()
    {
        FilterAndToggleCards_SalesRep(searchInputfield_SalesRep.text);
    }

    void FilterAndToggleCards_SalesRep(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            // Show all cards if no search term
            foreach (Transform child in salesRepContainer)
            {
                child.gameObject.SetActive(true);
            }
            noRequestYetSalesRep.SetActive(salesRepContainer.transform.childCount == 0);
            return;
        }

        searchTerm = searchTerm.ToLower();
        bool anyMatch = false;

        foreach (Transform child in salesRepContainer)
        {
            SalesRepRequestCard client = child.GetComponent<SalesRepRequestCard>();
            bool matches = (client.salesRepNameText.text).ToLower().Contains(searchTerm) ||
                          client.salesRepEmailText.text.ToLower().Contains(searchTerm);

            child.gameObject.SetActive(matches);
            if (matches) anyMatch = true;
        }

        noRequestYetSalesRep.SetActive(!anyMatch);
    }

    public void OnClearSearch_SalesRep()
    {
        searchInputfield_SalesRep.text = "";
        OnSearchInputChanged_SalesRep(); // reset
    }

    #endregion

    #region searchFunctionality_Request

    public TMP_InputField searchInputfield_Request;
    public void OnSearchInputChanged_Request()
    {
        FilterAndToggleCards_Req(searchInputfield_Request.text);
    }

    void FilterAndToggleCards_Req(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            // Show all cards if no search term
            foreach (Transform child in salesRepRequestContainer)
            {
                child.gameObject.SetActive(true);
            }
            noRequestYet.SetActive(salesRepRequestContainer.transform.childCount == 0);
            return;
        }

        searchTerm = searchTerm.ToLower();
        bool anyMatch = false;

        foreach (Transform child in salesRepRequestContainer)
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

}

[Serializable]
public class SalesRepListRequest
{
    public int page;
    public int limit;
    public string search;
    public int company_id;
}

[Serializable]
public class SalesRepresentativeResponse
{
    public SalesRepresentativeData data;
    public int statusCode;
    public string message;
}

[Serializable]
public class SalesRepresentativeData
{
    public List<SalesRepresentative> sales_representatives_list;
    public int total_count;
}

[Serializable]
public class SalesRepresentative
{
    public int id;
    public string first_name;
    public string last_name;
    public string email;
    public string phone_number;
    public bool is_active;
    public string created_at;
    public string updated_at;
    public string last_logged_in;
    public Company company;
    public ProfilePictureForReq profile_picture;
    public int total_request_count;
}

[Serializable]
public class Company
{
    public int id;
    public string company_name;
}
