using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientManager : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField firstNameInput;
    public TMP_InputField mobileInput;
    public GameObject mobileError;
    public TMP_InputField companyInput;
    public TMP_InputField addressInput;
    public TMP_InputField notesInput;

    [Header("UI Elements")]
    public Button submitButton;
    public GameObject loadingScreen;

    [Header("Install Screen")]
    public GameObject installScreen;

    [Header("Client Data")]
    public Transform clientContainer;
    public GameObject clientCardPrefab;
    public GameObject noClientObject;
    public GameObject addClientButton;


    #region AddClient
    private void Start()
    {
        submitButton.interactable = false;

        firstNameInput.onValueChanged.AddListener(delegate { ValidateForm(); });
        mobileInput.onValueChanged.AddListener(delegate { ValidateForm(); });
        companyInput.onValueChanged.AddListener(delegate { ValidateForm(); });
        addressInput.onValueChanged.AddListener(delegate { ValidateForm(); });
    }

    private void ValidateForm()
    {
        bool allFilled = !string.IsNullOrWhiteSpace(firstNameInput.text) &&
                         !string.IsNullOrWhiteSpace(mobileInput.text) &&
                         !string.IsNullOrWhiteSpace(companyInput.text) &&
                         !string.IsNullOrWhiteSpace(addressInput.text);

        submitButton.interactable = allFilled;
    }

    public void OnAddClientButtonClicked()
    {
        StartCoroutine(HandleSubmit());
    }

    private IEnumerator HandleSubmit()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        yield return new WaitForSeconds(1f);

        if (!IsValidMobile(mobileInput.text))
        {
            Debug.Log("Invalid mobile number!");
            mobileError.SetActive(true);
            yield break;
        }

        List<AddClientData> clients = LocalClientDatabase.LoadClients();

        AddClientData newClient = new AddClientData
        {
            firstName = firstNameInput.text.Trim(),
            mobile = mobileInput.text.Trim(),
            companyName = companyInput.text.Trim(),
            address = addressInput.text.Trim(),
            notes = notesInput.text.Trim(),
            createdAt = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt") // Add timestamp
        };

        clients.Add(newClient);
        LocalClientDatabase.SaveClients(clients);

        // You can now navigate back to Home or show success
        Debug.Log("Client added successfully!");
        LoadClients();

        NavigationManager.Instance.OpenScreen(installScreen);

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    private bool IsValidMobile(string mobile)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(mobile, @"^[0-9]{10}$");
    }
    #endregion

    #region ClientCard

    public void LoadClients()
    {
        LoadClientCards();
    }

    private void LoadClientCards()
    {
        // Clear existing cards
        foreach (Transform child in clientContainer)
            Destroy(child.gameObject);

        // Load local clients
        List<AddClientData> clients = LocalClientDatabase.LoadClients();

        // Check if no clients exist
        if (clients == null || clients.Count == 0)
        {
            noClientObject.SetActive(true);
            addClientButton.SetActive(false);
            return;
        }
        else
        {
            addClientButton.SetActive(true);
            noClientObject.SetActive(false);
        }

        // Instantiate cards
        foreach (AddClientData client in clients)
        {
            GameObject card = Instantiate(clientCardPrefab, clientContainer);
            ClientCard cardComponent = card.GetComponent<ClientCard>();
            cardComponent.SetData(client);
        }
    }

    #endregion
}
