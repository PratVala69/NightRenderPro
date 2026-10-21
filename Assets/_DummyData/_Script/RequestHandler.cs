using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.UI;
using System.Collections;

public class RequestHandler : MonoBehaviour
{
    [Header("Client Detail Field")]
    public TMP_InputField FirstNameInput;
    public GameObject firstNameErrorText;
    public TMP_InputField LastNameInput;
    public GameObject lastNameErrorText;
    public TMP_InputField EmailInput;
    public GameObject emailErrorText;
    public TMP_InputField ContactNoInput;
    public GameObject contactNumberErrorText;
    //public TMP_InputField AddressInput;
    //public GameObject addressErrorText;

    [Header("Client Detail")]
    public GameObject clientDetailPrefab; 
    public Transform contentParent;
    public TMP_Dropdown clientDropdown;
    private List<string> clientNames = new List<string>();
    public GameObject noClientYet;

    private List<ClientData> clientList = new List<ClientData>();
    private string savePath;

    [Header("All Request Panel")]
    public Transform allRequestParent;
    private List<ClientData> allClientList = new List<ClientData>();
    public GameObject noRequestYet;

    [Header("Select Client Panel")]
    public Transform selectClientContainer;
    public GameObject clientNameTogglePrefab;
    public GameObject selectClientPanel;
    public TextMeshProUGUI selectedClient;
    public string selectedClientName;
    public TMP_InputField searchField; 

    [Header("UI Panel")]
    public GameObject newRequestScreen;
    public GameObject newSessionScreen;
    public GameObject allRequestScreen;
    public GameObject pricingScreen;
    public GameObject homeScreen;

    [Header("Install Panel")]
    public Button measurementButton;
    public Button placementButton;
    public Button nextButton;
    private bool isMeasurementSelected = false;
    private bool isPlacementSelected = false;

    public static string selectedClientNameStr;


    private void Start()
    {
        //savePath = Path.Combine(Application.persistentDataPath, "clients.json");
        //Debug.Log("Save path : " + savePath);
        //LoadClientData(); // Load saved client data at start
    }

    public void RequestCreate()
    {
        if (CheckSelectedToggle())
        {
            // Clear input fields
            FirstNameInput.text = "";
            LastNameInput.text = "";
            EmailInput.text = "";
            ContactNoInput.text = "";
            //AddressInput.text = "";

            // Hide all previous error messages first
            firstNameErrorText.SetActive(false);
            lastNameErrorText.SetActive(false);
            emailErrorText.SetActive(false);
            contactNumberErrorText.SetActive(false);
            //addressErrorText.SetActive(false);

            Debug.Log("Old Client!");

            Toggle[] toggles = selectClientContainer.GetComponentsInChildren<Toggle>();
            foreach (Toggle other in toggles)
            {
                other.isOn = false;
            }
            selectedClient.text = "Select Client";
            FirstNameInput.interactable = true;
            LastNameInput.interactable = true;
            EmailInput.interactable = true;
            ContactNoInput.interactable = true;

            newRequestScreen.SetActive(false);
            newSessionScreen.SetActive(true);
        }
        else
        {
            bool hasError = false;

            // Validate First Name
            if (string.IsNullOrWhiteSpace(FirstNameInput.text))
            {
                firstNameErrorText.SetActive(true); // Show error message for first name
                hasError = true;
            }

            // Validate Last Name
            if (string.IsNullOrWhiteSpace(LastNameInput.text))
            {
                lastNameErrorText.SetActive(true); // Show error message for last name
                hasError = true;
            }

            // Validate Email
            if (string.IsNullOrWhiteSpace(EmailInput.text))
            {
                emailErrorText.SetActive(true); // Show error message for email
                hasError = true;
            }

            // Validate Contact Number
            if (string.IsNullOrWhiteSpace(ContactNoInput.text))
            {
                contactNumberErrorText.SetActive(true); // Show error message for contact number
                hasError = true;
            }

            // Validate Address
            //if (string.IsNullOrWhiteSpace(AddressInput.text))
            //{
            //    addressErrorText.SetActive(true); // Show error message for address
            //    hasError = true;
            //}

            // If there is any error, stop the function here
            if (hasError)
            {
                Debug.Log("Error: All fields are required!");
                return;
            }

            string fullName = FirstNameInput.text + " " + LastNameInput.text;
            string email = EmailInput.text;
            string contactNo = ContactNoInput.text;
            string timeDate = DateTime.Now.ToString("dd MMM") + "\n" + DateTime.Now.ToString("hh:mm tt");

            ClientData newClient = new ClientData { fullName = fullName, email = email, contactNo = contactNo, timeDate = timeDate };
            clientList.Add(newClient);
            SaveClientData(); // Save the updated list

            selectedClientNameStr = fullName;

            GameObject clientDetailInstance = Instantiate(clientDetailPrefab, contentParent);
            clientDetailInstance.GetComponent<ClientDetailInstance>().FullNameText.text = fullName;
            clientDetailInstance.GetComponent<ClientDetailInstance>().EmailText.text = email;
            clientDetailInstance.GetComponent<ClientDetailInstance>().ContactNoText.text = contactNo;
            clientDetailInstance.GetComponent<ClientDetailInstance>().TimeDateText.text = timeDate;

            clientDropdown.options.Add(new TMP_Dropdown.OptionData(fullName));
            GameObject tempObj = Instantiate(clientNameTogglePrefab, selectClientContainer);
            tempObj.GetComponent<Toggle>().group = selectClientContainer.GetComponent<ToggleGroup>();
            tempObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = fullName;

            // Clear input fields
            FirstNameInput.text = "";
            LastNameInput.text = "";
            EmailInput.text = "";
            ContactNoInput.text = "";
            //AddressInput.text = "";

            // Hide all previous error messages first
            firstNameErrorText.SetActive(false);
            lastNameErrorText.SetActive(false);
            emailErrorText.SetActive(false);
            contactNumberErrorText.SetActive(false);
            //addressErrorText.SetActive(false);

            Debug.Log("Client Detail Created & Saved Successfully!");

            Toggle[] toggles = selectClientContainer.GetComponentsInChildren<Toggle>();
            foreach (Toggle other in toggles)
            {
                other.isOn = false;
            }
            selectedClient.text = "Select Client";
            FirstNameInput.interactable = true;
            LastNameInput.interactable = true;
            EmailInput.interactable = true;
            ContactNoInput.interactable = true;

            newRequestScreen.SetActive(false);
            newSessionScreen.SetActive(true);
        }
    }

    public bool CheckSelectedToggle()
    {
        Toggle[] toggles = selectClientContainer.GetComponentsInChildren<Toggle>(); // true = include inactive children if needed

        foreach (Toggle toggle in toggles)
        {
            if (toggle.isOn)
            {
                Debug.Log("Selected Toggle: " + toggle.name);
                selectedClientName = toggle.gameObject.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text;
                selectedClientNameStr = selectedClientName;
                return true;
            }
        }
        selectedClientName = "";
        return false;
    }

    public void ClientDetail_Close()
    {
        if (CheckSelectedToggle())
        {
            selectedClient.text = selectedClientName;
            FirstNameInput.interactable = false;
            LastNameInput.interactable = false;
            EmailInput.interactable = false;
            ContactNoInput.interactable = false;
            //AddressInput.interactable = false;
        }
        else
        {
            selectedClient.text = "Select Client";
            FirstNameInput.interactable = true;
            LastNameInput.interactable = true;
            EmailInput.interactable = true;
            ContactNoInput.interactable = true;
            //AddressInput.interactable = true;
        }

        selectClientPanel.GetComponent<Animation>().Play("PanelCloseAnim");
    }

    public void OnClientDropdownValueChanged()
    {
        if (clientDropdown.value == 0)
        {
            FirstNameInput.interactable = true;
            LastNameInput.interactable = true;
            EmailInput.interactable = true;
            ContactNoInput.interactable = true;
            //AddressInput.interactable = true;
        }
        else
        {
            FirstNameInput.interactable = false;
            LastNameInput.interactable = false;
            EmailInput.interactable = false;
            ContactNoInput.interactable = false;
            //AddressInput.interactable = false;
        }
    }

    public void AddClientToDropdown()
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(FirstNameInput.text) || string.IsNullOrWhiteSpace(LastNameInput.text))
        {
            Debug.Log("Error: First Name and Last Name are required!");
            return;
        }

        // Create full name
        string fullName = FirstNameInput.text + " " + LastNameInput.text;

        // Check for duplicate
        if (clientNames.Contains(fullName))
        {
            Debug.Log("Error: Client name already exists!");
            return;
        }

        // Add to list and dropdown
        clientNames.Add(fullName);
        clientDropdown.options.Add(new TMP_Dropdown.OptionData(fullName));

        GameObject tempObj = Instantiate(clientNameTogglePrefab, selectClientContainer);
        tempObj.GetComponent<Toggle>().group = selectClientContainer.GetComponent<ToggleGroup>();
        tempObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = fullName;
        //clientDropdown.value = clientDropdown.options.Count - 1; // Select newly added client

        Debug.Log($"Added Client: {fullName}");
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

    public void OnMeasurementSelected()
    {
        isMeasurementSelected = true;
        isPlacementSelected = false;
        UpdateNextButtonState();
    }

    public void OnPlacementSelected()
    {
        isPlacementSelected = true;
        isMeasurementSelected = false;
        UpdateNextButtonState();
    }

    public void OnMeasurementDeselected()
    {
        isMeasurementSelected = false;
        UpdateNextButtonState();
    }

    public void OnPlacementDeselected()
    {
        isPlacementSelected = false;
        UpdateNextButtonState();
    }

    private void UpdateNextButtonState()
    {
        nextButton.interactable = isMeasurementSelected || isPlacementSelected;
    }

    public void NextButtonClick()
    {
        StartCoroutine(WaitForARScene());
    }

    IEnumerator WaitForARScene()
    {
        yield return new WaitForSeconds(5f);

        if (isPlacementSelected)
        {
            SceneTransition.instance.Load_ARSurfaceScene();
        }
        else
        {
            SceneTransition.instance.Load_ARMeasureScene();
        }
    }

    private void SaveClientData()
    {
        string json = JsonUtility.ToJson(new ClientDataWrapper { clients = clientList }, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Client data saved to: " + savePath);
    }

    public void LoadClientData()
    {
        Debug.Log("__Load client data");
        noClientYet.SetActive(false);
        // Clear old list and UI
        clientList.Clear();
        clientDropdown.options.Clear();

        foreach (Transform child in selectClientContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Load from file
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            ClientDataWrapper loadedData = JsonUtility.FromJson<ClientDataWrapper>(json);
            clientList = loadedData.clients ?? new List<ClientData>();

            foreach (ClientData client in clientList)
            {
                // Add to dropdown
                clientDropdown.options.Add(new TMP_Dropdown.OptionData(client.fullName));

                // Instantiate toggle for selection
                GameObject tempObj = Instantiate(clientNameTogglePrefab, selectClientContainer);
                tempObj.GetComponent<Toggle>().group = selectClientContainer.GetComponent<ToggleGroup>();
                tempObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = client.fullName;

                // Instantiate client detail view
                GameObject clientDetailInstance = Instantiate(clientDetailPrefab, contentParent);
                var detailComp = clientDetailInstance.GetComponent<ClientDetailInstance>();
                detailComp.FullNameText.text = client.fullName;
                detailComp.EmailText.text = client.email;

                string rawNumber = client.contactNo; // e.g., "1234567890"

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

                //detailComp.ContactNoText.text = client.contactNo;
                detailComp.TimeDateText.text = client.timeDate;
                detailComp.ProductCountText.text = client.productCount.ToString();
                detailComp.SessionCountText.text = client.sessionCount.ToString();
            }

            clientDropdown.RefreshShownValue();
            Debug.Log("Client data loaded successfully.");
        }
        else
        {
            noClientYet.SetActive(true);
            Debug.Log("No previous client data found.");
        }

        LoadAllClientRequest();
    }

    GameObject currentScreenObj;

    public void LoadAllClient_ScreenDisable(GameObject currentScreen)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        currentScreenObj = currentScreen;
        LoadAllClientRequest();
    }

    public void LoadAllClientRequest()
    {
        noRequestYet.SetActive(false);
        allClientList.Clear();

        foreach (Transform child in allRequestParent)
        {
            Destroy(child.gameObject);
        }

        // Load from file
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            ClientDataWrapper loadedData = JsonUtility.FromJson<ClientDataWrapper>(json);
            clientList = loadedData.clients ?? new List<ClientData>();

            foreach (ClientData client in clientList)
            {
                // Instantiate client detail view
                GameObject clientDetailInstance = Instantiate(clientDetailPrefab, allRequestParent);
                var detailComp = clientDetailInstance.GetComponent<ClientDetailInstance>();
                detailComp.FullNameText.text = client.fullName;
                detailComp.EmailText.text = client.email;

                string rawNumber = client.contactNo; // e.g., "1234567890"

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

                //detailComp.ContactNoText.text = client.contactNo;
                detailComp.TimeDateText.text = client.timeDate;
                detailComp.ProductCountText.text = client.productCount.ToString();
                detailComp.SessionCountText.text = client.sessionCount.ToString();
            }
            Debug.Log("Client data loaded successfully.");

            if (currentScreenObj != null)
            {
                allRequestScreen.SetActive(true);
                currentScreenObj.SetActive(false);

                currentScreenObj = null;
            }


            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else
        {
            noRequestYet.SetActive(true);
            Debug.Log("No previous client data found.");
        }
    }

    public void UpdateClientSessionAndProductCount(string clientName, int sessionCount, int productCount)
    {
        ClientData existingClient = clientList.Find(c => c.fullName == clientName);

        if (existingClient != null)
        {
            existingClient.sessionCount = sessionCount;
            existingClient.productCount = productCount;

            SaveClientData();
            Debug.Log($"Updated {existingClient.fullName}'s sessions: {sessionCount}, products: {productCount}");

            LoadClientData();
        }
        else
        {
            Debug.LogWarning($"Client with email {clientName} not found.");
        }
    }

    

    [System.Serializable]
    private class ClientDataWrapper
    {
        public List<ClientData> clients;
    }
}


[System.Serializable]
public class ClientData
{
    public string fullName;
    public string email;
    public string contactNo;
    public string timeDate;
    public int sessionCount;
    public int productCount;
}

