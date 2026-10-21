using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SalesRepRequestOverviewHandler : MonoBehaviour
{
    public static SalesRepRequestOverviewHandler instance;

    public SessionDetailFetcher sessionDetailFetcher;
    public RequestDetailsResponse sessionDetailResponse;
    public SalesRepAPIHandler salesRepAPIHandler;

    [Header("Prefab & Container")]
    public Transform sessionButtonsContainer; // Parent for session buttons (e.g., Session 1, Session 2)
    public GameObject sessionButtonPrefab; // Prefab for session buttons
    public GameObject photoPrefab; // Prefab for photo thumbnails
    public Transform photosContainer; // Parent for photo thumbnails
    public GameObject noPhotoObject;
    public GameObject productDetailPanel; // Panel for product details
    public Transform productDetailContainer;
    public Transform lastProductBG;
    public GameObject noProductObject;
    public GameObject photoListPrefab;
    public Transform photoListContainer;
    public GameObject photoListPanel;
    public GameObject noPhotoListObject;
    public GameObject photoParent;
    public GameObject sessionScrollView;

    [Header("Button")]
    public Button photosButton;
    private List<Button> sessionButtons = new List<Button>();

    [Header("Session Empty Or List")]
    public GameObject emptySession;
    public GameObject[] listInSession;

    public List<SessionDataForQuotation> sessions;
    public SessionDataForQuotation selectedSession;
    private List<GameObject> currentProductDetailCards = new List<GameObject>();
    private Button lastSelectedButton; // Track the last selected button

    [Header("Delete Popup")]
    public GameObject productDeleteDialog; // Assign in Inspector
    public Button productConfirmButton;
    public Button productCancelButton;
    public GameObject imageDeleteDialog; // Assign in Inspector
    public Button imageConfirmButton;
    public Button imageCancelButton;

    [Header("Panel")]
    public Animation imageListAnimPanel;

    private System.Action onProductConfirm;
    private System.Action onImageConfirm;

    [Header("For Fade")]
    public CanvasGroup photoCanvasgroup;
    public CanvasGroup productCanvasgroup;

    [Header("Request Data")]

    public ClientDetailInstance clientDetailInstance;
    public SalesRepRequestCard salesRepRequestCard;

    public List<GameObject> status;

    public string pdfUrl;

    [Header("Additional Cost UI")]
    public Transform additionalCostContainer;   // parent transform for additional costs
    public GameObject additionalCostPrefab;

    [Header("Other Cost UI")]
    public Transform otherCostContainer;   // parent transform for additional costs
    public GameObject otherCostPrefab;
    public TextMeshProUGUI notesText;
    public TextMeshProUGUI grandTotalText;

    [Header("Screen")]
    public GameObject salesRepRequestOvervierScreen;

    public int requestID;
    public int clientID;
    public RequestData selectedClient;

    private void Awake()
    {
        instance = this;
    }

    public void SetPredefineData()
    {
        Debug.Log($"📦 Client Name: {sessionDetailResponse.data.client.name}");
        Debug.Log($"📸 Total Screenshots: {sessionDetailResponse.data.quotation_details.overview.screenshots.Count}");
        Debug.Log($"🛒 Products: {sessionDetailResponse.data.quotation_details.overview.products.Count}");

        clientDetailInstance.requestItem.status = sessionDetailResponse.data.status;

        clientDetailInstance.FullNameText.text = sessionDetailResponse.data.client.name;
        clientDetailInstance.EmailText.text = sessionDetailResponse.data.client.email;

        string rawNumber = sessionDetailResponse.data.client.phone_number; // e.g., "1234567890"

        if (rawNumber.Length == 10)
        {
            string formatted = string.Format("{0} {1}-{2}",
                rawNumber.Substring(0, 3),
                rawNumber.Substring(3, 3),
                rawNumber.Substring(6, 4));

            clientDetailInstance.ContactNoText.text = "+1 " + formatted; // Output: "123 456-7890"
        }
        else
        {
            clientDetailInstance.ContactNoText.text = rawNumber; // fallback for unexpected lengths
        }

        //clientDetailInstance.ContactNoText.text = sessionDetailResponse.data.client.phone_number;
        clientDetailInstance.TimeDateText.text = FormatDate(sessionDetailResponse.data.created_at);
        clientDetailInstance.ProductCountText.text = sessionDetailResponse.data.quotation_details.overview.products.Count.ToString();
        clientDetailInstance.SessionCountText.text = sessionDetailResponse.data.quotation_details.session.Count.ToString();

        clientDetailInstance.pdfUrl = sessionDetailResponse.data.pdf_key;
        clientDetailInstance.requestId = sessionDetailResponse.data.id;

        pdfUrl = sessionDetailResponse.data.pdf_key;

        if (!string.IsNullOrEmpty(pdfUrl))
        {
            clientDetailInstance.pdfDownloadButton.interactable = true;
            clientDetailInstance.pdfMailButton.interactable = true;
        }
        else
        {
            clientDetailInstance.pdfDownloadButton.interactable = false;
            clientDetailInstance.pdfMailButton.interactable = false;
        }

        string currentStatus = sessionDetailResponse.data.status.ToLower();
        foreach (GameObject go in status)
        {
            go.SetActive(false);
        }

        foreach (GameObject go in status)
        {
            if (go.name.ToLower() == currentStatus)
            {
                go.SetActive(true);
                break; 
            }
        }
    }

    public CreatedBy currentCreatedBy;

    IEnumerator SetSelectedSalesRepresentative()
    {
        Debug.Log("Set Selected SalesRep : " + sessionDetailResponse.data.created_by);

        currentCreatedBy = sessionDetailResponse.data.created_by;

        if (sessionDetailResponse.data.created_by.role == "company_owner")
        {
            salesRepRequestCard.salesRepNameText.text = sessionDetailResponse.data.created_by.owner_name;
        }
        else
        {
            salesRepRequestCard.salesRepNameText.text = sessionDetailResponse.data.created_by.name;
        }

        if (PlayerPrefs.GetString("Role", "") == "company_owner")
        {
            salesRepRequestCard.callButton.gameObject.SetActive(true);
        }
        else
        {
            salesRepRequestCard.callButton.gameObject.SetActive(false);
        }

        if (PlayerPrefs.GetString("phoneNumber") == sessionDetailResponse.data.created_by.phone_number)
        {
            salesRepRequestCard.callButton.gameObject.SetActive(false);
        }


        salesRepRequestCard.salesRepEmailText.text = sessionDetailResponse.data.created_by.email;
        salesRepRequestCard.activeStatusIcon.gameObject.SetActive(false);

        //activeStatusIcon.color = rep.is_active
        //    ? salesRepAPIHandler.activeColor
        //    : salesRepAPIHandler.inactiveColor;

        salesRepRequestCard.repData.phone_number = sessionDetailResponse.data.created_by.phone_number;

        string urlForImg = LoginAPIHandler.instance.imageDownloadUrl + sessionDetailResponse.data.created_by.profile_image.location_key;
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

    public IEnumerator GetSessionDetails(int requestId)
    {
        string url = LoginAPIHandler.instance.baseURL + sessionDetailFetcher.sessionDetailFetchEndpoint + requestId;
        Debug.Log("Get Session Details : " + url);
        requestID = requestId;

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.Success)
#else
        if (!request.isHttpError && !request.isNetworkError)
#endif
        {
            string json = request.downloadHandler.text;
            Debug.Log("✅ Response: " + json);

            sessionDetailResponse = JsonUtility.FromJson<RequestDetailsResponse>(json);

            Debug.Log("✅ Response: " + sessionDetailResponse.data + "\n" + sessionDetailResponse.data.created_by);

            StartCoroutine(SetSelectedSalesRepresentative());

            SetPredefineData();

            StartPopulateSession();

            Debug.Log($"💰 Grand Total: {sessionDetailResponse.data.quotation_details.grand_total}");

            clientID = sessionDetailResponse.data.client.id;
            selectedClient = sessionDetailResponse.data;

            Debug.Log("Request ID : " + requestID + " Client ID : " + clientID);

            foreach (var product in sessionDetailResponse.data.quotation_details.overview.products)
            {
                Debug.Log($"➡ Product: {product.product_name}, Qty: {product.quantity}, Total: {product.total}");
            }
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ Error: " + request.error);
            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with request API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    string FormatDate(string isoDate)
    {
        if (System.DateTime.TryParse(isoDate, out System.DateTime dt))
            return dt.ToString("dd MMM\nhh:mm tt"); // Example: 16 Apr 12:23 PM
        return isoDate;
    }

    // Workaround for Unity's JSON parsing limits
    string FixJson(string json)
    {
        return json.Replace("\"additional_cost\": []", "\"additional_cost\": [{}]");
    }

    GameObject currentScreenObj;
    public void StartPopulateSession_ScreenDisable(GameObject currentScreen)
    {
        currentScreenObj = currentScreen;
        StartPopulateSession();
    }
    public void StartPopulateSession()
    {
        // Load sessions
        sessions = sessionDetailResponse.data.quotation_details.session;
        PopulateSessionList();

        productConfirmButton.onClick.AddListener(() => { onProductConfirm?.Invoke(); productDeleteDialog.SetActive(false); });
        productCancelButton.onClick.AddListener(() => productDeleteDialog.SetActive(false));

        imageConfirmButton.onClick.AddListener(() => { onImageConfirm?.Invoke(); imageDeleteDialog.SetActive(false); });
        imageCancelButton.onClick.AddListener(() => imageDeleteDialog.SetActive(false));

        photosButton.onClick.AddListener(OpenImagesPanel);
    }

    private void PopulateSessionList()
    {
        sessionButtonsContainer.gameObject.SetActive(false);
        sessionButtons.Clear();
        // Clear existing buttons
        foreach (Transform child in sessionButtonsContainer)
        {
            Destroy(child.gameObject);
        }
        sessionButtonsContainer.gameObject.SetActive(true);

        foreach (GameObject card in currentProductDetailCards)
        {
            Destroy(card);
        }
        currentProductDetailCards.Clear();

        // Check if sessions are available
        if (sessions.Count == 0)
        {
            // Optionally, display a message or UI element indicating no sessions
            Debug.Log("No sessions available.");
            selectedSession = null; // Ensure no session is selected

            photoParent.SetActive(false);
            emptySession.SetActive(true);
            for (int i = 0; i < listInSession.Length; i++)
            {
                listInSession[i].SetActive(false);
            }

            Transform content = sessionScrollView.transform.GetChild(0).GetChild(0);

            if (content.childCount > 0)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    content.GetChild(i).gameObject.SetActive(i == 0);
                    // ✅ Only enable the first, disable all others
                }
            }
            salesRepRequestOvervierScreen.SetActive(true);
            return;
        }

        Transform content2 = sessionScrollView.transform.GetChild(0).GetChild(0);

        if (content2.childCount > 0)
        {
            for (int i = 0; i < content2.childCount; i++)
            {
                content2.GetChild(i).gameObject.SetActive(true);
                // ✅ Only enable the first, disable all others
            }
        }

        emptySession.SetActive(false);
        photoParent.SetActive(true);
        for (int i = 0; i < listInSession.Length; i++)
        {
            listInSession[i].SetActive(true);
        }

        // Create buttons for each session
        foreach (var session in sessions)
        {
            GameObject buttonObj = Instantiate(sessionButtonPrefab, sessionButtonsContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = session.session_name;
            button.onClick.AddListener(() => SelectSession(session, button));

            // Set default appearance
            button.image.sprite = sessionDetailFetcher.defaultButtonSprite;
            buttonText.color = sessionDetailFetcher.defaultTextColor;

            sessionButtons.Add(button);
        }

        int sessionCount = sessions.Count;
        int productCount = 0;
        foreach (var session in sessions)
        {
            if (session.products != null)
            {
                productCount += session.products.Count;
                Debug.Log("Product : " + productCount);
            }
        }

        if (sessions.Count > 0)
        {
            int selectedIndex = 0;

            SessionDataForQuotation selectedSessionData = sessions[selectedIndex];
            if (sessionButtonsContainer.GetChild(selectedIndex).transform.GetComponent<Button>() == null)
            {
                Debug.LogError($"Button component missing on sessionButtonsContainer child at index {selectedIndex}");
                return;
            }
            Debug.Log($"Selecting session {selectedSessionData.session_name} at index {selectedIndex}");//, isFirstOpen: {isFirstOpen}");
            SelectSession(selectedSessionData, sessionButtons[selectedIndex]);
        }
    }

    public void PopulateAdditionalCosts()
    {
        foreach (Transform child in additionalCostContainer)
        {
            Destroy(child.gameObject);
        }

        if (selectedSession.products.Count > 0)
        {
            foreach (SessionProductForSessionDetail product in selectedSession.products)
            {
                Debug.Log("Product : " + product.additional_cost.Count);
 
                foreach (var cost in product.additional_cost)
                {
                    GameObject costItem = Instantiate(additionalCostPrefab, additionalCostContainer);
                    TextMeshProUGUI[] texts = costItem.GetComponentsInChildren<TextMeshProUGUI>();

                    if (texts.Length >= 2)
                    {
                        texts[0].text = cost.name;
                        if (float.TryParse(cost.price, out var parsedPrice))
                            texts[1].text = "$" + parsedPrice.ToString("F2");
                        else
                            texts[1].text = "$" + cost.price; // fallback
                    }
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(productDetailContainer.GetComponent<RectTransform>());
            PopulateOtherCosts();
        }
    }

    public void PopulateOtherCosts()
    {
        var overview = sessionDetailResponse.data.quotation_details.overview;

        foreach (Transform child in otherCostContainer)
            Destroy(child.gameObject);

        if (overview.other_cost != null && overview.other_cost.Count > 0)
        {
            foreach (var cost in overview.other_cost)
            {
                GameObject costItem = Instantiate(otherCostPrefab, otherCostContainer);
                TextMeshProUGUI[] texts = costItem.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 2)
                {
                    texts[0].text = cost.name;
                    texts[1].text = "$" +  (float.TryParse(cost.price.ToString(), out var parsedPrice) ? parsedPrice : 0f).ToString("F2");
                }
            }
        }

        if (!string.IsNullOrEmpty(overview.notes))
            notesText.text = overview.notes;
        else
            notesText.text = "";

        grandTotalText.text = "$" + (float.TryParse(overview.grand_total, out var grandTotal) ? grandTotal : 0f).ToString("F2");

        LayoutRebuilder.ForceRebuildLayoutImmediate(productDetailContainer.GetComponent<RectTransform>());
    }

    private void SelectSession(SessionDataForQuotation session, Button selectedButton)
    {
        Debug.Log("Selected session : " + session.session_name);
        // Reset the previously selected button's appearance
        if (lastSelectedButton != null)
        {
            lastSelectedButton.image.sprite = sessionDetailFetcher.defaultButtonSprite;
            TextMeshProUGUI lastButtonText = lastSelectedButton.GetComponentInChildren<TextMeshProUGUI>();
            lastButtonText.color = sessionDetailFetcher.defaultTextColor;
        }

        // Update the selected button's appearance
        selectedButton.image.sprite = sessionDetailFetcher.selectedButtonSprite;
        TextMeshProUGUI buttonText = selectedButton.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.color = sessionDetailFetcher.selectedTextColor;

        // Update the selected session and track the button
        selectedSession = session;
        lastSelectedButton = selectedButton;

        DOTween.Kill(photoCanvasgroup);
        DOTween.Kill(productCanvasgroup);
        photoCanvasgroup.DOFade(0, 0.1f);
        productCanvasgroup.DOFade(0, 0.1f);

        PlayerPrefs.SetInt("SessionId", session.session_id);
        Debug.Log("Session ID : " + session.session_id);

        StartCoroutine(UpdateUIAfterFade(photoCanvasgroup, productCanvasgroup));
    }

    public void HideCanvas()
    {
        photoCanvasgroup.DOFade(0, 0.1f);
        productCanvasgroup.DOFade(0, 0.1f);
    }

    private IEnumerator UpdateUIAfterFade(CanvasGroup photosCanvas, CanvasGroup scrollCanvas)
    {
        yield return new WaitForSeconds(0.1f); // Wait for fade-out to complete
        UpdatePhotos();
        UpdateProductDetails();
        PopulateAdditionalCosts();
        photosCanvas.DOFade(1, 0.1f);
        scrollCanvas.DOFade(1, 0.1f);
    }

    public void UpdatePhotos()
    {
        foreach (Transform child in photosContainer)
        {
            Destroy(child.gameObject);
        }

        if (selectedSession.screenshots.Count == 0)
        {
            noPhotoObject.SetActive(true);
            return;
        }

        noPhotoObject.SetActive(false);
        foreach (var screenshot in selectedSession.screenshots)
        {
            GameObject photo = Instantiate(photoPrefab, photosContainer);
            Image imageComponent = photo.transform.GetChild(0).GetComponent<Image>();
            if (!string.IsNullOrEmpty(screenshot.location_key))
            {
                StartCoroutine(LoadImageFromURL(screenshot.location_key, imageComponent));
            }
        }
    }

    private void UpdateProductDetails()
    {
        // Clear any existing product detail cards
        foreach (GameObject card in currentProductDetailCards)
        {
            Destroy(card);
        }
        currentProductDetailCards.Clear();
        noProductObject.SetActive(false);
        sessionScrollView.SetActive(true);

        salesRepRequestOvervierScreen.SetActive(true);

        if (selectedSession.products.Count > 0)
        {
            foreach (SessionProductForSessionDetail product in selectedSession.products)
            {
                Debug.Log("Product : " + product.product_name);
                // Instantiate the product detail card prefab
                GameObject productDetailCard = Instantiate(productDetailPanel, productDetailContainer);
                ProductCardOverview detailCard = productDetailCard.GetComponent<ProductCardOverview>();
                if (detailCard != null)
                {
                    detailCard.SetProductDetailsFromAR(product);
                   
                    currentProductDetailCards.Add(productDetailCard); // Track the card

                    bool isLast = (product.id == selectedSession.products[selectedSession.products.Count - 1].id);

                    Debug.Log("isLAst : " + isLast + product.id + selectedSession.products[selectedSession.products.Count - 1].id);

                    detailCard.SetDividerVisible(!isLast);
                }
                else
                {
                    Debug.LogError("ProductDetailCard component not found on instantiated prefab.");
                    Destroy(productDetailCard); // Clean up if component is missing
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(productDetailContainer.GetComponent<RectTransform>());

            
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);           
        }
        else
        {
            noProductObject.SetActive(true);
            sessionScrollView.SetActive(false);
        }
        lastProductBG.SetAsLastSibling();
    }

   
    public void OpenImagesPanel()
    {
        if (selectedSession == null || selectedSession.screenshots == null || photoListPanel == null)
        {
            return;
        }

        photoListPanel.SetActive(true);

        foreach (Transform child in photoListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var screenshot in selectedSession.screenshots)
        {
            ScreenshotDataForProduct screenshotData = screenshot;
            GameObject imageObj = Instantiate(photoListPrefab, photoListContainer);
            ImagePrefabController controller = imageObj.GetComponent<ImagePrefabController>();
            if (controller != null)
            {
                controller.InitializeForSalesRepRequestOverview(screenshotData, DeleteScreenshot, this);
            }
            StartCoroutine(LoadImageFromURL(screenshot.location_key, controller.thumbnailImage));
        }
    }

    private void ShowDeleteImageConfirmationDialog(System.Action confirmAction)
    {
        onImageConfirm = confirmAction;
        imageDeleteDialog.SetActive(true);
    }

    private void DeleteScreenshot(ScreenshotDataForProduct screenshot)
    {
        ShowDeleteImageConfirmationDialog(() =>
        {
            if (selectedSession != null && screenshot != null)
            {
                Debug.Log($"Deleting screenshot: {screenshot.original_file_name}");
                StartCoroutine(DeleteScreenshotCoroutine(screenshot.id));
            }
        });
    }

    private IEnumerator DeleteScreenshotCoroutine(int screenshotId)
    {
        string url = LoginAPIHandler.instance.baseURL + sessionDetailFetcher.deleteScreenshotIdEndpoint + screenshotId;

        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Screenshot deleted successfully.");
            if (selectedSession != null)
            {
                selectedSession.screenshots.RemoveAll(s => s.id == screenshotId);
                // Update the UI to reflect the change

                if (photoListPanel != null && photoListPanel.activeSelf)
                {
                    foreach (Transform child in photoListContainer)
                    {
                        Destroy(child.gameObject);
                    }
                    foreach (var screenshot in selectedSession.screenshots)
                    {
                        GameObject imageObj = Instantiate(photoListPrefab, photoListContainer);
                        ImagePrefabController controller = imageObj.GetComponent<ImagePrefabController>();
                        if (controller != null)
                        {
                            controller.InitializeForSalesRepRequestOverview(screenshot, DeleteScreenshot, this);
                        }
                        yield return StartCoroutine(LoadImageFromURL(screenshot.location_key, controller.thumbnailImage));
                    }
                }

                UpdatePhotos();
            }
            // Optionally refresh UI or remove screenshot from list
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError($"Failed to delete screenshot: {request.responseCode} - {request.error}");
        }
    }

    public void OnImageListClose(GameObject imageListAnimPanel)
    {
        imageListAnimPanel.GetComponent<Animation>().Play("ImagePanelCloseAnim");
    }

    IEnumerator LoadImageFromURL(string url, Image img)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(LoginAPIHandler.instance.imageDownloadUrl + url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                int size = Mathf.Min(downloadedTexture.width, downloadedTexture.height);

                int x = (downloadedTexture.width - size) / 2;   // Center horizontally
                int y = (downloadedTexture.height - size) / 2;  // Center vertically

                Rect cropRect = new Rect(x, y, size, size);

                Sprite newSprite = Sprite.Create(
                    downloadedTexture,
                    cropRect,
                    new Vector2(0.5f, 0.5f)
                );

                img.sprite = newSprite;

                // Wait for one frame to ensure UI has updated
                yield return null;
            }
            else if (request.responseCode == 403)
            {
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
            }
            else
            {
                ErrorPopupHandler.instance.errorDescText.text = "There is an issue with image download URL.";//"Profile photo download failed: " + request.downloadHandler.text;
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }
}


[System.Serializable]
public class ProfileImage
{
    public int id;
    public string mime_type;
    public string file_name;
    public string original_file_name;
    public string location_key;
    public string thumbnail_image;
    public string thumbnail_image_key;
    public string created_at;
    public string updated_at;
}

[System.Serializable]
public class Client
{
    public int id;
    public string name;
    public string email;
    public string phone_number;
    public string created_at;
    public string address;
}

[System.Serializable]
public class Stats
{
    public int product_count;
    public int image_count;
    public float grand_total;
}

[System.Serializable]
public class OverviewForSalesRep
{
    public List<string> products;
    public List<string> screenshots;
    public string other_cost;
    public string notes;
    public List<string> additional_cost;
}

[System.Serializable]
public class QuotationDetailsForSalesRep
{
    public OverviewForSalesRep overview;
}

[System.Serializable]
public class Data
{
    public int id;
    public string status;
    public string created_at;
    public string notes;
    public CreatedBy created_by;
    public Client client;
    public Stats stats;
    public string other_cost;
    public string pdf_key;
    public QuotationDetailsForSalesRep quotation_details;
}

[System.Serializable]
public class RootResponse
{
    public Data data;
    public int statusCode;
    public string message;
}
