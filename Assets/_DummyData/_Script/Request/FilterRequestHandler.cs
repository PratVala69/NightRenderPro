using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static ClientHandler;

public class FilterRequestHandler : MonoBehaviour
{
    public static FilterRequestHandler instance;
    public Transform clientParent;
    public Transform clientParentSalesRep;
    public GameObject selectedClientPrefab;

    private List<ClientInfo> selectedClients = new List<ClientInfo>();
    private List<ClientInfo> selectedClientsSalesRep = new List<ClientInfo>();

    public SalesRepAPIHandler salesRepAPIHandler;
    public ClientHandler clientHandler;

    public RequestFilter currentFilter;

    [Header("UI References")]
    public List<ToggleButton> productToggles;   // assign in Inspector
    public List<ToggleButton> statusToggles;    // assign in Inspector

    public List<ToggleButton> productTogglesSalesRep;   // assign in Inspector
    public List<ToggleButton> statusTogglesSalesRep;    // assign in Inspector

    [Header("Filter Response Screen")]
    public GameObject SalesRepResponseScreen;
    public Transform salesRepContainer;
    public GameObject noRequestYetSalesRep;

    public GameObject MyRequestResponseScreen;
    public Transform myRequestContainer;
    public GameObject noRequestYetMyResponse;

    public GameObject RequestResponseScreen;
    public Transform requestContainer;
    public GameObject noRequestYetAllRequest;

    [Header("Filter Screen")]
    public GameObject filterScreen;
    public GameObject filterScreenSalesRep;

    private List<bool> initialProductState;
    private List<bool> initialStatusState;
    private List<bool> initialProductStateSalesRep;
    private List<bool> initialStatusStateSalesRep;
    private List<ClientInfo> initialSelectedClients;
    private List<ClientInfo> initialSelectedClientsSalesRep;


    private void Awake()
    {
        instance = this;
    }

    #region requestFilter

    private void Start()
    {
        OpenFilterScreen(true);
        OpenFilterScreen(false);
    }

    public void ApplyFilters()
    {
        currentFilter = BuildFilter();
        StartCoroutine(FetchFilteredRequests(currentFilter));

        OpenFilterScreen(false);
    }

    public void ApplyFiltersSalesRep()
    {
        currentFilter = BuildFilterSalesRep();
        StartCoroutine(FetchFilteredRequests(currentFilter));

        OpenFilterScreen(true);
    }

    public void ClearFilters()
    {
        currentFilter = new RequestFilter();
        ClearClients();

        foreach (var t in productToggles)
        {
            t.isSelected = false;
            t.UpdateVisual();
        }
        foreach (var t in statusToggles)
        {
            t.isSelected = false;
            t.UpdateVisual();
        }

        ApplyFilters();
    }

    public void ClearFiltersSalesRep()
    {
        currentFilter = new RequestFilter();
        ClearClientsSalesRep();

        foreach (var t in productTogglesSalesRep)
        {
            t.isSelected = false;
            t.UpdateVisual();
        }
        foreach (var t in statusTogglesSalesRep)
        {
            t.isSelected = false;
            t.UpdateVisual();
        }

        ApplyFiltersSalesRep();
    }

    public RequestFilter BuildFilter()
    {

        RequestFilter filter = new RequestFilter
        {
            page = 1,
            limit = 30,
            search = ""
        };

        // Collect selected products
        foreach (var toggle in productToggles)
        {
            if (toggle.isSelected)
                filter.product_category.Add(toggle.label.text);
        }

        // Collect selected status
        foreach (var toggle in statusToggles)
        {
            if (toggle.isSelected)
                filter.status.Add(toggle.label.text.ToLower());
        }

        if (SalesRepResponseScreen.activeInHierarchy)
        {
            foreach (var c in selectedClientsSalesRep)
                filter.clients.Add(c.id);
        }
        else
        {
            foreach (var c in selectedClients)
                filter.clients.Add(c.id);
        }

        return filter;
    }

    public RequestFilter BuildFilterSalesRep()
    {
        RequestFilter filter = new RequestFilter
        {
            page = 1,
            limit = 30,
            search = "",//searchField != null ? searchField.text : "",
            sales_representative_id = salesRepAPIHandler.selectedSalesRepId//PlayerPrefs.GetInt("sales_rep_id", 0),
        };

        // Collect selected products
        foreach (var toggle in productTogglesSalesRep)
        {
            if (toggle.isSelected)
                filter.product_category.Add(toggle.label.text);
        }

        // Collect selected status
        foreach (var toggle in statusTogglesSalesRep)
        {
            if (toggle.isSelected)
                filter.status.Add(toggle.label.text.ToLower());
        }

        if (SalesRepResponseScreen.activeInHierarchy)
        {
            foreach (var c in selectedClientsSalesRep)
                filter.clients.Add(c.id);
        }
        else
        {
            foreach (var c in selectedClients)
                filter.clients.Add(c.id);
        }

        return filter;
    }

    public IEnumerator FetchFilteredRequests(RequestFilter filter)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        string url = LoginAPIHandler.instance.baseURL + clientHandler.fetchClientListEndpoint;

        string jsonBody = JsonUtility.ToJson(filter);
        Debug.Log("Filter sent Json : " + jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        Debug.Log("Filter Response: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            RequestResponse response = JsonUtility.FromJson<RequestResponse>(FixJson(request.downloadHandler.text));

            if (SalesRepResponseScreen.activeInHierarchy)
                PopulateSalesRepRequestUI(response.data.request_list);
            else if (MyRequestResponseScreen.activeInHierarchy)
                PopulateMrRequestClientUI(response.data.request_list);
            else
                PopulateClientRequestUI(response.data.request_list);

        }
        else
        {
            Debug.LogError($"Failed: {request.error}");
        }
    }

    GameObject clientDetailInstance;
    private void PopulateSalesRepRequestUI(List<RequestItem> requests)
    {
        noRequestYetSalesRep.SetActive(false);
        foreach (Transform child in salesRepContainer)
        {
            Destroy(child.gameObject);
        }

        if (requests.Count > 0)
        {
            foreach (RequestItem client in requests)
            {
                clientDetailInstance = Instantiate(salesRepAPIHandler.clientDetailPrefab, salesRepContainer);

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
            noRequestYetSalesRep.SetActive(true);
        }

        ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        filterScreenSalesRep.SetActive(false);
    }

    void PopulateClientRequestUI(List<RequestItem> clients)
    {
        noRequestYetAllRequest.SetActive(false);

        foreach (Transform child in requestContainer)
        {
            Destroy(child.gameObject);
        }

        if (clients.Count > 0)
        {
            foreach (RequestItem client in clients)
            {
                // Instantiate client detail view
                GameObject clientDetailInstance = Instantiate(salesRepAPIHandler.clientDetailPrefab, requestContainer);
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
            noRequestYetAllRequest.SetActive(true);
            Debug.Log("No previous client data found.");
        }

        ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        filterScreen.SetActive(false);
    }

    void PopulateMrRequestClientUI(List<RequestItem> clients)
    {
        noRequestYetMyResponse.SetActive(false);

        foreach (Transform child in myRequestContainer)
        {
            Destroy(child.gameObject);
        }

        // Load from file
        if (clients.Count > 0)
        {
            foreach (RequestItem client in clients)
            {
                clientDetailInstance = Instantiate(salesRepAPIHandler.clientDetailPrefab, myRequestContainer);

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
            noRequestYetMyResponse.SetActive(true);
            Debug.Log("No previous client data found.");
        }

        ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        filterScreen.SetActive(false);
    }

    private string FixJson(string value)
    {
        // sometimes API returns a single object instead of array -> Fix
        if (!value.StartsWith("{"))
            value = "{\"data\":" + value + "}";
        return value;
    }

    #endregion

    #region AddClient
    GameObject obj;
    // Called when a client is selected in modal
    public void AddClient(ClientInfo client)
    {
        if (selectedClients.Exists(c => c.id == client.id)) return;

        selectedClients.Add(client);

        obj = Instantiate(selectedClientPrefab, clientParent);

        // Use the new count as the index
        int index = selectedClients.Count;
        obj.GetComponentInChildren<TextMeshProUGUI>().text = index + ". " + client.first_name + " " + client.last_name;

        // Assign close button
        Button closeBtn = obj.transform.GetChild(1).GetComponent<Button>();
        closeBtn.onClick.AddListener(() => RemoveClient(client));
    }

    // Called when ❌ is pressed
    public void RemoveClient(ClientInfo client)
    {
        Debug.Log("Remove Client : " + client.id + " " + client.first_name);

        // 1. Remove from the list
        selectedClients.RemoveAll(c => c.id == client.id);

        // 2. Destroy the matching UI card in clientParent
        foreach (Transform child in clientParent)
        {
            var text = child.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (text != null && text.text.Contains(client.first_name + " " + client.last_name))
            {
                Destroy(child.gameObject);
                break;
            }
        }

        // 3. Uncheck correct toggle
        string fullName = client.first_name + " " + client.last_name;
        salesRepAPIHandler.UncheckClient(fullName);

        // ✅ renumber correctly
        RenumberClients();
    }

    private void RenumberClients()
    {
        if (clientParent.childCount == 0)
            return;

        // Clear all existing cards from parent
        foreach (Transform child in clientParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < selectedClients.Count; i++)
        {
            ClientInfo client = selectedClients[i];

            obj = Instantiate(selectedClientPrefab, clientParent);

            obj.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1) + ". " + client.first_name + " " + client.last_name;

            // Assign close button
            Button closeBtn = obj.transform.GetChild(1).GetComponent<Button>();
            closeBtn.onClick.AddListener(() => RemoveClient(client));
        }
    }

    public List<int> GetSelectedClientIds()
    {
        List<int> ids = new List<int>();
        foreach (var c in selectedClients) ids.Add(c.id);
        return ids;
    }

    public void ClearClients()
    {
        foreach (Transform child in clientParent)
            Destroy(child.gameObject);
        selectedClients.Clear();
        salesRepAPIHandler.RemoveSelectedClient_Close();
    }

    public void ClearClientsSalesRep()
    {
        foreach (Transform child in clientParentSalesRep)
            Destroy(child.gameObject);
        selectedClientsSalesRep.Clear();
        salesRepAPIHandler.RemoveSelectedClient_CloseSalesRep();
    }

    // Called when a client is selected in modal
    public void AddClientSalesRep(ClientInfo client)
    {
        if (selectedClientsSalesRep.Exists(c => c.id == client.id)) return;

        selectedClientsSalesRep.Add(client);

        GameObject obj = Instantiate(selectedClientPrefab, clientParentSalesRep);

        // Use the new count as the index
        int index = selectedClientsSalesRep.Count;
        obj.GetComponentInChildren<TextMeshProUGUI>().text = index + ". " + client.first_name + " " + client.last_name;

        // Assign close button
        Button closeBtn = obj.transform.GetChild(1).GetComponent<Button>();
        closeBtn.onClick.AddListener(() => RemoveClientSalesRep(client));
    }

    // Called when ❌ is pressed
    public void RemoveClientSalesRep(ClientInfo client)
    {
        Debug.Log("Remove Client : " + client.id + " " + client.first_name);

        // 1. Remove from the list
        selectedClientsSalesRep.RemoveAll(c => c.id == client.id);

        // 2. Destroy the matching UI card in clientParent
        foreach (Transform child in clientParentSalesRep)
        {
            var text = child.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (text != null && text.text.Contains(client.first_name + " " + client.last_name))
            {
                Destroy(child.gameObject);
                break;
            }
        }

        // 3. Uncheck correct toggle
        string fullName = client.first_name + " " + client.last_name;
        salesRepAPIHandler.UncheckClientSalesRep(fullName);

        // ✅ renumber correctly
        RenumberClientsSalesRep();
    }

    private void RenumberClientsSalesRep()
    {
        if (clientParentSalesRep.childCount == 0)
            return;

        foreach (Transform child in clientParentSalesRep)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < selectedClientsSalesRep.Count; i++)
        {
            ClientInfo client = selectedClientsSalesRep[i];

            obj = Instantiate(selectedClientPrefab, clientParentSalesRep);

            obj.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1) + ". " + client.first_name + " " + client.last_name;

            // Assign close button
            Button closeBtn = obj.transform.GetChild(1).GetComponent<Button>();
            closeBtn.onClick.AddListener(() => RemoveClientSalesRep(client));
        }
    }

    public List<int> GetSelectedClientIdsSalesRep()
    {
        List<int> ids = new List<int>();
        foreach (var c in selectedClientsSalesRep) ids.Add(c.id);
        return ids;
    }


    #endregion

    #region FilerOpenClose

    public void OpenFilterScreen(bool isSalesRep)
    {
        if (isSalesRep)
        {
            // snapshot toggles
            initialProductStateSalesRep = productTogglesSalesRep.ConvertAll(t => t.isSelected);
            initialStatusStateSalesRep = statusTogglesSalesRep.ConvertAll(t => t.isSelected);

            // snapshot clients
            initialSelectedClientsSalesRep = new List<ClientInfo>(selectedClientsSalesRep);

            //filterScreenSalesRep.SetActive(true);
        }
        else
        {
            initialProductState = productToggles.ConvertAll(t => t.isSelected);
            initialStatusState = statusToggles.ConvertAll(t => t.isSelected);

            // snapshot clients
            initialSelectedClients = new List<ClientInfo>(selectedClients);

            //filterScreen.SetActive(true);
        }
    }

    public void CloseFilterScreen(bool isSalesRep)
    {
        if (isSalesRep)
        {
            // restore toggles
            for (int i = 0; i < productTogglesSalesRep.Count; i++)
            {
                productTogglesSalesRep[i].isSelected = initialProductStateSalesRep[i];
                productTogglesSalesRep[i].UpdateVisual();
            }
            for (int i = 0; i < statusTogglesSalesRep.Count; i++)
            {
                statusTogglesSalesRep[i].isSelected = initialStatusStateSalesRep[i];
                statusTogglesSalesRep[i].UpdateVisual();
            }

            // restore clients
            ClearClientsSalesRep();
            foreach (var client in initialSelectedClientsSalesRep)
                AddClientSalesRep(client);

            filterScreenSalesRep.SetActive(false);
        }
        else
        {
            for (int i = 0; i < productToggles.Count; i++)
            {
                productToggles[i].isSelected = initialProductState[i];
                productToggles[i].UpdateVisual();
            }
            for (int i = 0; i < statusToggles.Count; i++)
            {
                statusToggles[i].isSelected = initialStatusState[i];
                statusToggles[i].UpdateVisual();
            }

            // restore clients
            ClearClients();
            foreach (var client in initialSelectedClients)
                AddClient(client);

            filterScreen.SetActive(false);
        }
    }

    string FormatDate(string isoDate)
    {
        if (System.DateTime.TryParse(isoDate, out System.DateTime dt))
            return dt.ToString("dd MMM\nhh:mm tt"); // Example: 16 Apr 12:23 PM
        return isoDate;
    }

    #endregion
}

[System.Serializable]
public class RequestFilter
{
    public int page = 1;
    public int limit = 30;
    public string search = "";
    public List<string> product_category = new List<string>();
    public List<string> status = new List<string>();
    public int sales_representative_id;
    public List<int> clients = new List<int>();
}
