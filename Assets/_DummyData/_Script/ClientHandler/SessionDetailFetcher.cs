using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class SessionDetailFetcher : MonoBehaviour
{
    public static SessionDetailFetcher instance;

    public string sessionDetailFetchEndpoint = "request/"; // /request/{request_id}
    public string deleteScreenshotIdEndpoint = "request/screenshot/";
    public string deleteProductIdEndpoint = "request/session/";
    public string draftRequestDetailEndpoint = "request/getDraftRequestDetails/";

    [Header("Prefab & Container")]
    public Transform sessionButtonsContainer; // Parent for session buttons (e.g., Session 1, Session 2)
    public GameObject sessionButtonPrefab; // Prefab for session buttons
    public GameObject photoPrefab; // Prefab for photo thumbnails
    public Transform photosContainer; // Parent for photo thumbnails
    public GameObject noPhotoObject;
    public GameObject productDetailPanel; // Panel for product details
    public Transform productDetailContainer;
    public GameObject noProductObject;
    public GameObject photoListPrefab;
    public Transform photoListContainer;
    public GameObject photoListPanel;
    public GameObject noPhotoListObject;
    public GameObject photoParent;

    [Header("Button")]
    public Button newSessionButton; // Button to start a new session
    public Button nextButton; // Button to proceed
    public Button pricingNextButton; // Button to proceed
    public Button photosButton;
    private List<Button> sessionButtons = new List<Button>();

    [Header("For Button Selected")]
    // Add these fields for sprite and text color customization
    public Sprite selectedButtonSprite; // Sprite for selected button
    public Sprite defaultButtonSprite; // Default sprite for buttons
    public Color selectedTextColor = Color.white; // Text color for selected button
    public Color defaultTextColor = Color.black; // Default text color

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
    public GameObject allSessionScreen;
    public GameObject selecteSessionScreen;

    private System.Action onProductConfirm;
    private System.Action onImageConfirm;

    public static bool isFirstOpen = true;

    [Header("For Fade")]
    public CanvasGroup photoCanvasgroup;
    public CanvasGroup productCanvasgroup;

    public RequestDetailsResponse sessionDetailResponse;

    [Header("All Session Panel")]
    public Transform allPhotoContainer;
    public Transform allSessionContainer;
    private List<GameObject> allProductDetailCards = new List<GameObject>();
    public GameObject noPhotoListObjectInAllSession;
    public GameObject noProductObjectInAllSession;
    public GameObject noPhotoObjectInAllSession;
    public Button allPhotoButton;
    public GameObject allPhotoListPanel;
    public GameObject productCardInputfieldPrefab;

    [Header("Other Cost & Total Cost")]
    public Transform otherCostObj;
    public Transform totalCostObj;
    public GameObject otherCostPrefab;
    public Transform otherCostContainer;
    public TextMeshProUGUI totalCostText;
    private List<GameObject> otherCostCards = new List<GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Get Session Detail
    public void FetchSessionDetails()
    {
        StartCoroutine(GetSessionDetails());
    }

   public IEnumerator GetSessionDetails()
    {
        string url = LoginAPIHandler.instance.baseURL + sessionDetailFetchEndpoint + PlayerPrefs.GetInt("RequestId");
        Debug.Log("Get Session Details : " + url);

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

            sessionDetailResponse = JsonUtility.FromJson<RequestDetailsResponse>(FixJson(json));

            Debug.Log($"📦 Client Name: {sessionDetailResponse.data.client.name}");
            Debug.Log($"📸 Total Screenshots: {sessionDetailResponse.data.quotation_details.overview.screenshots.Count}");
            Debug.Log($"🛒 Products: {sessionDetailResponse.data.quotation_details.overview.products.Count}");
            StartPopulateSession();

            Debug.Log($"💰 Grand Total: {sessionDetailResponse.data.quotation_details.grand_total}");


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

        // Set up New Session button
        newSessionButton.onClick.AddListener(StartNewSession);

        // Set up Next button
        nextButton.onClick.AddListener(OnNextClicked);

        productConfirmButton.onClick.AddListener(() => { onProductConfirm?.Invoke(); productDeleteDialog.SetActive(false); });
        productCancelButton.onClick.AddListener(() => productDeleteDialog.SetActive(false));

        imageConfirmButton.onClick.AddListener(() => { onImageConfirm?.Invoke(); imageDeleteDialog.SetActive(false); });
        imageCancelButton.onClick.AddListener(() => imageDeleteDialog.SetActive(false));

        photosButton.onClick.AddListener(OpenImagesPanel);
        allPhotoButton.onClick.AddListener(AllPhotoListPopulate);
    }

    public void PopulateEmptySessionList_PendingStatus()
    {
        PopulateSessionList();
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

            nextButton.interactable = false;
            photoParent.SetActive(false);
            emptySession.SetActive(true);
            for (int i = 0; i < listInSession.Length; i++)
            {
                listInSession[i].SetActive(false);
            }

            return;
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
            button.image.sprite = defaultButtonSprite;
            buttonText.color = defaultTextColor;

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
            nextButton.interactable = true;

            int selectedIndex = isFirstOpen ? 0 : sessions.Count - 1;
            if (selectedIndex >= sessionButtonsContainer.childCount)
            {
                Debug.LogError($"Selected index {selectedIndex} out of bounds for sessionButtonsContainer with {sessionButtonsContainer.childCount} children");
                return;
            }
            SessionDataForQuotation selectedSessionData = sessions[selectedIndex];
            if (sessionButtonsContainer.GetChild(selectedIndex).transform.GetComponent<Button>() == null)
            {
                Debug.LogError($"Button component missing on sessionButtonsContainer child at index {selectedIndex}");
                return;
            }
            Debug.Log($"Selecting session {selectedSessionData.session_name} at index {selectedIndex}, isFirstOpen: {isFirstOpen}");
            SelectSession(selectedSessionData, sessionButtons[selectedIndex]);
        }
    }

    public void AddOtherCost()
    {
        GameObject newCostCard = Instantiate(otherCostPrefab, otherCostContainer);
        newCostCard.transform.SetAsLastSibling(); // Add to end

        OtherCostCard card = newCostCard.GetComponent<OtherCostCard>();
        if (card != null)
        {
            // Hook delete button
            card.deleteButton.onClick.AddListener(() => DeleteOtherCost(card));
            card.otherCostPrice.onValueChanged.AddListener((string newValue) =>
            {
                RecalculateTotal();
            });
            otherCostCards.Add(newCostCard);
        }

        StartCoroutine(UpdateLayout());
    }

    private void DeleteOtherCost(OtherCostCard card)
    {
        if (otherCostCards.Contains(card.gameObject))
        {
            otherCostCards.Remove(card.gameObject);
            Destroy(card.gameObject);

            StartCoroutine(UpdateLayout());

            RecalculateTotal();
        }
    }

    private IEnumerator UpdateLayout()
    {
        yield return null; // wait 1 frame

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(otherCostContainer as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(otherCostObj as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(otherCostObj.parent as RectTransform);
    }

    public void UpdateBool()
    {
        isFirstOpen = false;
    }

    private void StartNewSession()
    {
        // Logic to start a new AR session (e.g., load AR scene)
        //SceneManager.LoadScene("ARScene");
    }

    private void SelectSession(SessionDataForQuotation session, Button selectedButton)
    {
        Debug.Log("Selected session : " + session.session_name);
        // Reset the previously selected button's appearance
        if (lastSelectedButton != null)
        {
            lastSelectedButton.image.sprite = defaultButtonSprite;
            TextMeshProUGUI lastButtonText = lastSelectedButton.GetComponentInChildren<TextMeshProUGUI>();
            lastButtonText.color = defaultTextColor;
        }

        // Update the selected button's appearance
        selectedButton.image.sprite = selectedButtonSprite;
        TextMeshProUGUI buttonText = selectedButton.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.color = selectedTextColor;

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

        if (selectedSession.products.Count > 0)
        {
            noProductObject.SetActive(false);
            foreach (SessionProductForSessionDetail product in selectedSession.products)
            {
                Debug.Log("Product : " + product.product_name);
                // Instantiate the product detail card prefab
                GameObject productDetailCard = Instantiate(productDetailPanel, productDetailContainer);
                ProductCardDetail detailCard = productDetailCard.GetComponent<ProductCardDetail>();
                if (detailCard != null)
                {
                    //detailCard.SetProductDetailsFromAR(product, DeleteProduct);
                    //currentProductDetailCards.Add(productDetailCard); // Track the card

                    detailCard.SetProductDetailsFromAR(product, null); // just set details

                    // Clear ALL listeners (runtime + Inspector)
                    detailCard.deleteButton.onClick.RemoveAllListeners();

                    SessionProductForSessionDetail capturedProduct = product;  // Local copy to avoid loop capture issues
                    detailCard.deleteButton.onClick.AddListener(() => DeleteProduct(capturedProduct));

                    currentProductDetailCards.Add(productDetailCard);
                }
                else
                {
                    Debug.LogError("ProductDetailCard component not found on instantiated prefab.");
                    Destroy(productDetailCard); // Clean up if component is missing
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(productDetailContainer.GetComponent<RectTransform>());

            if (currentScreenObj != null)
            {
                currentScreenObj.SetActive(false);
                selecteSessionScreen.SetActive(true);

                currentScreenObj = null;

                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            }

        }
        else
        {
            noProductObject.SetActive(true);
            nextButton.interactable = false;
        }
    }

    private void OnNextClicked()
    {
        // Logic to proceed to the next step (e.g., pricing or submission)
        Debug.Log("Next clicked for Session " + selectedSession.session_name);
    }
    #endregion

    #region Draft Session
    public DraftRequestResponse draftRequestresponse;

    public void StartPopulateDraftAllSession_ScreenDisable(GameObject currentScreen)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        currentScreenObj = currentScreen;
        StartCoroutine(FetchDraftRequestDetails());
    }

    public IEnumerator FetchDraftRequestDetails()
    {
        string url = LoginAPIHandler.instance.baseURL + draftRequestDetailEndpoint + PlayerPrefs.GetInt("RequestId");
        Debug.Log("Get Session Details : " + url);

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            draftRequestresponse = JsonUtility.FromJson<DraftRequestResponse>(FixJson(request.downloadHandler.text));
            Debug.Log("Request Download Handler Text: " + request.downloadHandler.text);
            Debug.Log($"Grand Total: {draftRequestresponse.data.grand_total}");
            AllSessionPopulate();

            yield return StartCoroutine(AllPhotosPopulate());

            if (currentScreenObj != null)
            {
                yield return new WaitForSeconds(1f);

                currentScreenObj.SetActive(false);
                allSessionScreen.SetActive(true);

                currentScreenObj = null;

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
            Debug.LogError($"Failed to fetch draft request: {request.responseCode} - {request.error}");
        }
    }

    public void AllSessionPopulate()
    {
        // Clear existing cards
        foreach (GameObject card in allProductDetailCards)
        {
            Destroy(card);
        }
        allProductDetailCards.Clear();

        bool hasAnyProduct = false;
        Debug.Log("Populating All Sessions");

        foreach (ProductDetail product in draftRequestresponse.data.products)
        {
            hasAnyProduct = true;
            GameObject productDetailCard = Instantiate(productCardInputfieldPrefab, allSessionContainer);
            ProductCardDetailWithInputfield detailCard = productDetailCard.GetComponent<ProductCardDetailWithInputfield>();
            if (detailCard != null)
            {
                detailCard.SetProductDetailsCollapsed(product, null); // just set details

                // Clear ALL listeners (runtime + Inspector)
                detailCard.deleteButton.onClick.RemoveAllListeners();

                ProductDetail capturedProduct = product;  // Local copy to avoid loop capture issues
                detailCard.deleteButton.onClick.AddListener(() => DeleteProductForDraft(capturedProduct));

                allProductDetailCards.Add(productDetailCard);
            }
            else
            {
                Debug.LogError("ProductCardDetailWithInputfield not found on prefab.");
                Destroy(productDetailCard);
            }
        }

        int sessionCount = sessions.Count;

        // clientHandler.UpdateClientSessionAndProductCount(RequestHandler.selectedClientNameStr, sessionCount, productCount);

        // Update UI visibility
        noProductObjectInAllSession.SetActive(!hasAnyProduct);
        pricingNextButton.interactable = hasAnyProduct;
        if (float.TryParse(draftRequestresponse.data.grand_total, out var parsedTotal))
            totalCostText.text = "$" + parsedTotal.ToString("F2");
        else
            totalCostText.text = "$" + draftRequestresponse.data.grand_total;
        // Ensure other cost and total cost objects are positioned correctly
        otherCostObj.SetAsLastSibling();
        totalCostObj.SetAsLastSibling();

        RecalculateTotal();
    }

    public float finalTotal;
    public void RecalculateTotal()
    {
        Debug.Log("Recalculate : " + allProductDetailCards.Count + "--" + otherCostCards.Count);
        float productTotal = 0f;
        foreach (var card in allProductDetailCards)
            productTotal += card.GetComponent<ProductCardDetailWithInputfield>().totalCount;

        float otherCostTotal = 0f;
        foreach (var cost in otherCostCards)
            otherCostTotal += cost.GetComponent<OtherCostCard>().otherCostPricefloat;

        finalTotal = (float)Math.Round(productTotal + otherCostTotal, 2);
        //Debug.Log("Recalculate : " + productTotal + "--" + otherCostTotal + "--" + finalTotal);

        totalCostText.text = "$" + finalTotal.ToString("F2");
    }

    public IEnumerator AllPhotosPopulate()
    {
        foreach (Transform child in allPhotoContainer)
        {
            Destroy(child.gameObject);
        }

        bool hasAnyScreenshot = false;

        foreach (ScreenshotDataForDraft screenshot in draftRequestresponse.data.screenshots)
        {
            hasAnyScreenshot = true;

            GameObject photo = Instantiate(photoPrefab, allPhotoContainer);
            Image imageComponent = photo.transform.GetChild(0).GetComponent<Image>();

            if (imageComponent != null)
            {
                string fullImageUrl = screenshot.location_key;

               yield return StartCoroutine(LoadImageFromURL(fullImageUrl, imageComponent));
            }
        }

        noPhotoObjectInAllSession.SetActive(!hasAnyScreenshot);
    }

    #endregion

    private void ShowDeleteProductConfirmationDialog(System.Action confirmAction)
    {
        onProductConfirm = confirmAction;
        productDeleteDialog.SetActive(true);
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

    private void DeleteScreenshotForDraft(ScreenshotDataForDraft screenshot)
    {
        ShowDeleteImageConfirmationDialog(() =>
        {
            if (selectedSession != null && screenshot != null)
            {
                Debug.Log($"Deleting screenshot: {screenshot.original_file_name}");
                StartCoroutine(DeleteScreenshotCoroutineDraft(screenshot.id));
            }
        });
    }

    private IEnumerator DeleteScreenshotCoroutine(int screenshotId)
    {
        string url = LoginAPIHandler.instance.baseURL + deleteScreenshotIdEndpoint + screenshotId;

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
                            controller.Initialize(screenshot, DeleteScreenshot, this);
                        }
                        yield return StartCoroutine(LoadImageFromURL(screenshot.location_key, controller.thumbnailImage));
                    }
                }

                UpdatePhotos();
                StartCoroutine(FetchDraftRequestDetails());
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

    private IEnumerator DeleteScreenshotCoroutineDraft(int screenshotId)
    {
        string url = LoginAPIHandler.instance.baseURL + deleteScreenshotIdEndpoint + screenshotId;

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
                            controller.Initialize(screenshot, DeleteScreenshot, this);
                        }
                        yield return StartCoroutine(LoadImageFromURL(screenshot.location_key, controller.thumbnailImage));
                    }
                }
                else
                {
                    Debug.Log("No photo available.");

                    yield return null;
                }

                StartCoroutine(FetchDraftRequestDetails());
            }
            // Optionally refresh UI or remove screenshot from list
        }
        else
        {
            Debug.LogError($"Failed to delete screenshot: {request.responseCode} - {request.error}");
        }
    }

    int previousId;

    private void DeleteProduct(SessionProductForSessionDetail product)
    {
        ShowDeleteProductConfirmationDialog(() =>
        {
            if (selectedSession != null && product != null)
            {
                Debug.Log($"Initiating deletion of product: {product.product_name} (ID: {product.id})");
                if (previousId != product.id)
                {
                    previousId = product.id;
                    StartCoroutine(DeleteSessionCoroutine(product.id));
                }
            }
        });
    }

    int previousIdDraft;

    private void DeleteProductForDraft(ProductDetail product)
    {
        ShowDeleteProductConfirmationDialog(() =>
        {
            if (selectedSession != null && product != null)
            {
                Debug.Log($"Initiating deletion of product: {product.product_name} (ID: {product.id})");
                if (previousIdDraft != product.id)
                {
                    previousIdDraft = product.id;
                    StartCoroutine(DeleteSessionCoroutine(product.id));
                }
            }
        });
    }

    private IEnumerator DeleteSessionCoroutine(int sessionId)
    {
        string url = LoginAPIHandler.instance.baseURL + deleteProductIdEndpoint + sessionId;

        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("accept", "*/*");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Session deleted successfully.");

            if (selectedSession != null)
            {
                selectedSession.products.RemoveAll(p => p.id == sessionId);
                // Update the UI to reflect the change
               
                var toRemove = draftRequestresponse.data.products.Find(p => p.id == sessionId);
                if (toRemove != null)
                    draftRequestresponse.data.products.Remove(toRemove);

                //UpdateProductDetails();
                //AllSessionPopulate();

               yield return StartCoroutine(GetSessionDetails());

            }
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError($"Failed to delete session: {request.responseCode} - {request.error}");
        }
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
                controller.Initialize(screenshotData, DeleteScreenshot, this);
            }
            StartCoroutine(LoadImageFromURL(screenshot.location_key, controller.thumbnailImage));
        }
    }

    public void AllPhotoListPopulate()
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

        //foreach (var session in sessions)
        {
            foreach (var screenshot in draftRequestresponse.data.screenshots)
            {
                ScreenshotDataForDraft screenshotData = screenshot;
                GameObject imageObj = Instantiate(photoListPrefab, photoListContainer);
                ImagePrefabController controller = imageObj.GetComponent<ImagePrefabController>();
                if (controller != null)
                {
                    controller.InitializeForDraft(screenshotData, DeleteScreenshotForDraft, this);
                }
                StartCoroutine(LoadImageFromURL(screenshot.location_key, controller.thumbnailImage));
            }
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

#region Session Request Detail Class
[System.Serializable]
public class RequestDetailsResponse
{
    public RequestData data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class RequestData
{
    public int id;
    public string status;
    public string created_at;
    public int product_count;
    public int image_count;
    public CreatedBy created_by;
    public ClientDataForSessionfetcher client;
    public QuotationDetails quotation_details;
    public string pdf_key;
}

[System.Serializable]
public class CreatedBy
{
    public string company_name;
    public string owner_name;
    public string email;
    public string name;
    public string role;
    public string phone_number;
    public ProfileImage profile_image;
}

[System.Serializable]
public class ClientDataForSessionfetcher
{
    public int id;
    public string name;
    public string email;
    public string phone_number;
    public string address;
}

[System.Serializable]
public class QuotationDetails
{
    public List<SessionDataForQuotation> session;
    public Overview overview;
    public List<AdditionalCostP> other_cost;
    public float grand_total;
}

[System.Serializable]
public class SessionDataForQuotation
{
    public int session_id;
    public string session_name;
    public List<SessionProductForSessionDetail> products;
    public List<ScreenshotDataForProduct> screenshots;
}

[System.Serializable]
public class SessionProductForSessionDetail
{
    public int id;
    public int session_id ;
    public int company_product_id ;
    public int product_id ;
    public string product_category ;
    public string product_type ;
    public string product_name ;
    public ProductImageData product_image ;
    public string size_name ;
    public float length ;
    public string length_unit;
    public float width ;
    public string width_unit;
    public float length_in_inches;
    public string cost ;
    public float quantity ;
    public ProductColor color ;
    public string price;
    public float variance ;
    public List<AdditionalCost> additional_cost ;
    public string total ;
}

[System.Serializable]
public class Overview
{
    public List<SessionProductForSessionDetail> products;
    public List<ScreenshotDataForProduct> screenshots;
    public List<AdditionalCostP> other_cost;
    public string notes;
    public string grand_total;
    public List<AdditionalCostP> additional_cost;
}

[Serializable]
public class AdditionalCostP
{
    public string name;
    public string price;
}

[System.Serializable]
public class ColorData
{
    public int id;
    public string color_name;
    public string hex_code;
}

[System.Serializable]
public class ScreenshotDataForProduct
{
    public int id ;
    public string mime_type ;
    public string file_name ;
    public string location_key ;
    public string original_file_name ;
    public string created_at ;
}

[System.Serializable]
public class AdditionalCostData
{
    public string name;
    public string price;
}

[System.Serializable]
public class ProductImageData
{
    public string file_name;
    public string location_key;
}
#endregion

#region Draft Details class
[Serializable]
public class DraftRequestResponse
{
    public int statusCode;
    public string message;
    public DraftRequestData data;
}

[Serializable]
public class DraftRequestData
{
    public int request_id;
    public List<ProductDetail> products;
    public List<ScreenshotDataForDraft> screenshots;
    public string other_cost;
    public string grand_total;
}

[Serializable]
public class ProductDetail
{
    public int id;
    public int session_id;
    public int company_product_id;
    public int product_id;
    public string product_category;
    public string product_type;
    public string product_name;
    public ProductImage product_image;
    public string size_name;
    public float length;
    public string length_unit;
    public float width;
    public string width_unit;
    public float length_in_inches;
    public string cost;
    public float quantity;
    public string price;
    public float variance;
    public List<AdditionalCost> additional_cost;
    public string total;
    public ProductColor color;
}

[Serializable]
public class ProductImage
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

[Serializable]
public class ProductColor
{
    public int id;
    public string color_name;
    public string hex_code;
}

[Serializable]
public class ScreenshotDataForDraft
{
    public int id;
    public string mime_type;
    public string file_name;
    public string location_key;
    public string original_file_name;
    public string created_at;
}
#endregion