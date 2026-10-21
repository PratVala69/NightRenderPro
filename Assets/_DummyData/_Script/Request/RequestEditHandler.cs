using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RequestEditHandler : MonoBehaviour
{
    public static RequestEditHandler instance;

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
    public GameObject noProductObject;
    public GameObject photoListPrefab;
    public Transform photoListContainer;
    public GameObject photoListPanel;
    public GameObject noPhotoListObject;
    public GameObject photoParent;

    public Transform sessionButtonsContainerReq; // Parent for session buttons (e.g., Session 1, Session 2)
    public Transform photosContainerReq; // Parent for photo thumbnails
    public GameObject noPhotoObjectReq;
    public Transform productDetailContainerReq;
    public GameObject noProductObjectReq;
    public GameObject photoParentReq;

    [Header("Button")]
    public Button photosButton;
    public Button pricingNextButton; // Button to proceed
    private List<Button> sessionButtons = new List<Button>();

    public Button photosButtonReq;
    public Button pricingNextButtonReq;

    [Header("Session Empty Or List")]
    public GameObject emptySession;
    public GameObject emptySessionReq;
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

    public CanvasGroup photoCanvasgroupReq;
    public CanvasGroup productCanvasgroupReq;

    [Header("Request Data")]
    public ClientDetailInstance clientDetailInstance;
    public SalesRepRequestCard salesRepRequestCard;

    public ClientDetailInstance clientDetailInstanceReq;
    public SalesRepRequestCard salesRepRequestCardReq;

    public List<GameObject> status;
    public List<GameObject> statusReq;

    public string pdfUrl;

    [Header("Other Cost & Total Cost")]
    public GameObject otherCostPrefab;
    private List<GameObject> otherCostCards = new List<GameObject>();

    public Transform totalCostObj;
    public TextMeshProUGUI totalCostText;
    public Transform otherCostContainer;
    public Transform otherCostMain;
    public TMP_InputField notesText;

    public Transform totalCostObjReq;
    public TextMeshProUGUI totalCostTextReq;
    public Transform otherCostContainerReq;
    public Transform otherCostMainReq;
    public TMP_InputField notesTextReq;


    [Header("Screen")]
    public GameObject editRequestScreen;
    public GameObject editRequestScreenReq;

    public int requestID;
    public int clientID;
    public RequestData selectedClient;


    public List<List<ProductCardDetailWithInputfield>> sessionProductCards = new List<List<ProductCardDetailWithInputfield>>();
    public List<List<SessionProductForSessionDetail>> sessionProducts = new List<List<SessionProductForSessionDetail>>();
    public List<float> sessionTotals = new List<float>();
    //public TMP_Text grandTotalText;
    public List<TMP_Text> sessionTotalTexts;

    private float totalPrice;

    private void Awake()
    {
        instance = this;
    }

    public void SetPredefineData()
    {
        Debug.Log($"📦 Client Name: {sessionDetailResponse.data.client.name}");
        Debug.Log($"📸 Total Screenshots: {sessionDetailResponse.data.quotation_details.overview.screenshots.Count}");

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

        if (clientDetailInstance.addressText != null)
            clientDetailInstance.addressText.text = string.IsNullOrEmpty(sessionDetailResponse.data.client.address)? "Address Not Found" : sessionDetailResponse.data.client.address;

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

    public void SetPredefineDataReq()
    {
        Debug.Log($"📦 Client Name: {sessionDetailResponse.data.client.name}");
        Debug.Log($"📸 Total Screenshots: {sessionDetailResponse.data.quotation_details.overview.screenshots.Count}");

        clientDetailInstanceReq.requestItem.status = sessionDetailResponse.data.status;

        clientDetailInstanceReq.FullNameText.text = sessionDetailResponse.data.client.name;
        clientDetailInstanceReq.EmailText.text = sessionDetailResponse.data.client.email;

        string rawNumber = sessionDetailResponse.data.client.phone_number; // e.g., "1234567890"

        if (rawNumber.Length == 10)
        {
            string formatted = string.Format("{0} {1}-{2}",
                rawNumber.Substring(0, 3),
                rawNumber.Substring(3, 3),
                rawNumber.Substring(6, 4));

            clientDetailInstanceReq.ContactNoText.text = "+1 " + formatted; // Output: "123 456-7890"
        }
        else
        {
            clientDetailInstanceReq.ContactNoText.text = rawNumber; // fallback for unexpected lengths
        }

        //clientDetailInstanceReq.ContactNoText.text = sessionDetailResponse.data.client.phone_number;
        clientDetailInstanceReq.TimeDateText.text = FormatDate(sessionDetailResponse.data.created_at);
        clientDetailInstanceReq.ProductCountText.text = sessionDetailResponse.data.quotation_details.overview.products.Count.ToString();
        clientDetailInstanceReq.SessionCountText.text = sessionDetailResponse.data.quotation_details.session.Count.ToString();

        clientDetailInstanceReq.pdfUrl = sessionDetailResponse.data.pdf_key;
        clientDetailInstanceReq.requestId = sessionDetailResponse.data.id;

        if (clientDetailInstanceReq.addressText != null)
            clientDetailInstanceReq.addressText.text = string.IsNullOrEmpty(sessionDetailResponse.data.client.address) ? "Address Not Found" : sessionDetailResponse.data.client.address;

        if (!string.IsNullOrEmpty(pdfUrl))
        {
            clientDetailInstanceReq.pdfDownloadButton.interactable = true;
            clientDetailInstanceReq.pdfMailButton.interactable = true;
        }
        else
        {
            clientDetailInstanceReq.pdfDownloadButton.interactable = false;
            clientDetailInstanceReq.pdfMailButton.interactable = false;
        }

        string currentStatus = sessionDetailResponse.data.status.ToLower();
        foreach (GameObject go in statusReq)
        {
            go.SetActive(false);
        }

        foreach (GameObject go in statusReq)
        {
            if (go.name.ToLower() == currentStatus)
            {
                go.SetActive(true);
                break;
            }
        }
    }

    public IEnumerator GetSessionDetails(int requestId)
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        string url = LoginAPIHandler.instance.baseURL + sessionDetailFetcher.sessionDetailFetchEndpoint + requestId;
        Debug.Log("Get Session Details : " + url);
        requestID = requestId;
        PlayerPrefs.SetInt("RequestId", requestID);

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

            Debug.Log("Current SalesRep : " + salesRepAPIHandler.currentSalesRep);

            if(!string.IsNullOrEmpty( salesRepAPIHandler.currentSalesRep.first_name))
                yield return StartCoroutine(SetSelectedSalesRepresentative(salesRepAPIHandler.currentSalesRep));
            else
                yield return StartCoroutine(SetSelectedSalesRepresentativeCreatedBy());


           // if (PlayerPrefs.GetString("Role", "") == "company_owner")
                SetPredefineData();
            //else
                SetPredefineDataReq();

            StartPopulateSession();

            clientID = sessionDetailResponse.data.client.id;
            selectedClient = sessionDetailResponse.data;

            Debug.Log("Request ID : " + requestID + " Client ID : " + clientID);
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

    IEnumerator SetSelectedSalesRepresentativeCreatedBy()
    {
        var createdBy = sessionDetailResponse.data.created_by;

        // Decide which card to use
        var targetCard = (createdBy.role == "company_owner")
            ? salesRepRequestCard
            : salesRepRequestCardReq;

        Debug.Log($"Set Selected SalesRep ({createdBy.role}) -> Using Card: {targetCard.name}");

        if (createdBy.role == "company_owner")
            targetCard.salesRepNameText.text = createdBy.owner_name ?? "Unknown";
        else
            targetCard.salesRepNameText.text = createdBy.name ?? "Unknown";

        if (PlayerPrefs.GetString("Role", "") == "company_owner")
        {
            targetCard.callButton.gameObject.SetActive(true);
        }
        else
        {
            targetCard.callButton.gameObject.SetActive(false);
        }

        if (PlayerPrefs.GetString("phoneNumber") == createdBy.phone_number)
        {
            targetCard.callButton.gameObject.SetActive(false);
        }

        targetCard.repData.phone_number = createdBy.phone_number;

        // Set Email
        targetCard.salesRepEmailText.text = createdBy.email ?? "Email Not Found";

        // Hide active status (you can re-enable if API supports is_active)
        targetCard.activeStatusIcon.gameObject.SetActive(false);

        // Handle Profile Image
        if (createdBy.profile_image != null && !string.IsNullOrEmpty(createdBy.profile_image.location_key))
        {
            string urlForImg = LoginAPIHandler.instance.imageDownloadUrl + createdBy.profile_image.location_key;
            using (UnityWebRequest requestForImg = UnityWebRequestTexture.GetTexture(urlForImg))
            {
                yield return requestForImg.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (requestForImg.result != UnityWebRequest.Result.Success)
#else
            if (requestForImg.isNetworkError || requestForImg.isHttpError)
#endif
                {
                    Debug.LogWarning("Image download failed: " + urlForImg + " -> " + requestForImg.error);
                    targetCard.profileImage.sprite = targetCard.defaultProfileSprite;
                }
                else
                {
                    Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(requestForImg);

                    // Crop to square
                    int size = Mathf.Min(downloadedTexture.width, downloadedTexture.height);
                    int x = (downloadedTexture.width - size) / 2;
                    int y = (downloadedTexture.height - size) / 2;

                    Rect cropRect = new Rect(x, y, size, size);
                    Sprite newSprite = Sprite.Create(downloadedTexture, cropRect, new Vector2(0.5f, 0.5f));

                    targetCard.profileImage.sprite = newSprite;
                }
            }
        }
        else
        {
            // No image available
            targetCard.profileImage.sprite = targetCard.defaultProfileSprite;
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

    IEnumerator SetSelectedSalesRepresentative(SalesRepresentative rep)
    {
        salesRepRequestCard.repData = rep;
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

    GameObject currentScreenObj;
    public void StartPopulateSession_ScreenDisable(GameObject currentScreen)
    {
        currentScreenObj = currentScreen;
        StartPopulateSession();
    }
    public void StartPopulateSession()
    {
        sessions = sessionDetailResponse.data.quotation_details.session;
        PopulateSessionList();

        productConfirmButton.onClick.AddListener(() => { onProductConfirm?.Invoke(); productDeleteDialog.SetActive(false); });
        productCancelButton.onClick.AddListener(() => productDeleteDialog.SetActive(false));

        imageConfirmButton.onClick.AddListener(() => { onImageConfirm?.Invoke(); imageDeleteDialog.SetActive(false); });
        imageCancelButton.onClick.AddListener(() => imageDeleteDialog.SetActive(false));

        photosButton.onClick.AddListener(OpenImagesPanel);
        photosButtonReq.onClick.AddListener(OpenImagesPanel);
    }

    private void PopulateSessionList()
    {
        bool isCompanyOwner = PlayerPrefs.GetString("Role", "") == "company_owner";
        Transform sessionContainer = isCompanyOwner ? sessionButtonsContainer : sessionButtonsContainerReq;

        sessionContainer.gameObject.SetActive(false);
        foreach (Transform child in sessionContainer)
        {
            Destroy(child.gameObject);
        }
        sessionContainer.gameObject.SetActive(true);

        // Clear the sessionButtons list to avoid stale references
        sessionButtons.Clear();

        foreach (GameObject card in currentProductDetailCards)
        {
            Destroy(card);
        }
        currentProductDetailCards.Clear();
        sessionProductCards.Clear();
        sessionProducts.Clear();

        if (sessions.Count == 0)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            Debug.Log("No sessions available.");
            selectedSession = null;
            if (isCompanyOwner)
            {
                photoParent.SetActive(false);
                emptySession.SetActive(true);
                for (int i = 0; i < listInSession.Length; i++)
                {
                    listInSession[i].SetActive(false);
                }
                editRequestScreen.SetActive(true);
                pricingNextButton.interactable = false;
                return;
            }
            else
            {
                photoParentReq.SetActive(false);
                emptySessionReq.SetActive(true);
                for (int i = 0; i < listInSession.Length; i++)
                {
                    listInSession[i].SetActive(false);
                }
                editRequestScreenReq.SetActive(true);
                pricingNextButtonReq.interactable = false;
                return;
            }
        }
        else
        {
            pricingNextButtonReq.interactable = true;
            pricingNextButton.interactable = true;
        }

        if (isCompanyOwner)
        {
            editRequestScreen.SetActive(true);
            emptySession.SetActive(false);
            photoParent.SetActive(true);
        }
        else
        {
            editRequestScreenReq.SetActive(true);
            emptySessionReq.SetActive(false);
            photoParentReq.SetActive(true);
        }

        for (int i = 0; i < listInSession.Length; i++)
        {
            listInSession[i].SetActive(true);
        }

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            GameObject buttonObj = Instantiate(sessionButtonPrefab, sessionContainer);
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"Button component missing on instantiated sessionButtonPrefab for session {session.session_name} at index {i}. Check the sessionButtonPrefab setup in the Inspector.");
                continue; // Skip this iteration to avoid adding null to sessionButtons
            }
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText == null)
            {
                Debug.LogError($"TextMeshProUGUI component missing on sessionButtonPrefab for session {session.session_name} at index {i}.");
                Destroy(buttonObj); // Clean up the invalid GameObject
                continue;
            }
            buttonText.text = session.session_name;
            int sessionIndex = i;
            button.onClick.AddListener(() => SelectSession(session, button, sessionIndex));

            button.image.sprite = sessionDetailFetcher.defaultButtonSprite;
            buttonText.color = sessionDetailFetcher.defaultTextColor;
            sessionButtons.Add(button);

            List<ProductCardDetailWithInputfield> productCards = new List<ProductCardDetailWithInputfield>();
            List<SessionProductForSessionDetail> productDataList = new List<SessionProductForSessionDetail>();
            Transform container = isCompanyOwner ? productDetailContainer : productDetailContainerReq;

            if (session.products != null && session.products.Count > 0)
            {
                foreach (var product in session.products)
                {
                    GameObject productDetailCard = Instantiate(productDetailPanel, container);
                    ProductCardDetailWithInputfield detailCard = productDetailCard.GetComponent<ProductCardDetailWithInputfield>();
                    if (detailCard != null)
                    {
                        //detailCard.SetProductDetailsCollapsedForEditRequest(product, DeleteProductForDraft);

                        //detailCard.deleteButton.onClick.RemoveAllListeners();
                        //detailCard.deleteButton.onClick.AddListener(() => DeleteProductForDraft(product));


                        detailCard.SetProductDetailsCollapsedForEditRequest(product, null); // just set details

                        // Clear ALL listeners (runtime + Inspector)
                        detailCard.deleteButton.onClick.RemoveAllListeners();

                        SessionProductForSessionDetail capturedProduct = product;  // Local copy to avoid loop capture issues
                        detailCard.deleteButton.onClick.AddListener(() => DeleteProductForDraft(capturedProduct));

                        productCards.Add(detailCard);
                        productDataList.Add(product);
                        currentProductDetailCards.Add(productDetailCard);
                    }
                    else
                    {
                        Debug.LogError("ProductDetailCard component not found on instantiated prefab.");
                        Destroy(productDetailCard);
                    }
                    productDetailCard.SetActive(false);
                }
            }

            sessionProductCards.Add(productCards);
            sessionProducts.Add(productDataList);
        }

        if (sessions.Count > 0)
        {
            int selectedIndex = 0;
            if (sessionButtons.Count == 0)
            {
                Debug.LogError("No session buttons available. Check sessionButtonPrefab and instantiation logic.");
                return;
            }
            if (sessionButtons[selectedIndex] == null)
            {
                Debug.LogError($"Button component missing at index {selectedIndex}. Skipping session selection.");
                return;
            }
            SessionDataForQuotation selectedSessionData = sessions[selectedIndex];
            Debug.Log($"Selecting session {selectedSessionData.session_name} at index {selectedIndex}");
            SelectSession(selectedSessionData, sessionButtons[selectedIndex], selectedIndex);
        }
    }

    private void SelectSession(SessionDataForQuotation session, Button selectedButton, int sessionIndex)
    {
        Debug.Log($"Selected session: {session.session_name} at index {sessionIndex}");

        if (lastSelectedButton != null)
        {
            lastSelectedButton.image.sprite = sessionDetailFetcher.defaultButtonSprite;
            TextMeshProUGUI lastButtonText = lastSelectedButton.GetComponentInChildren<TextMeshProUGUI>();
            lastButtonText.color = sessionDetailFetcher.defaultTextColor;
        }

        selectedButton.image.sprite = sessionDetailFetcher.selectedButtonSprite;
        TextMeshProUGUI buttonText = selectedButton.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.color = sessionDetailFetcher.selectedTextColor;

        selectedSession = session;
        lastSelectedButton = selectedButton;

        foreach (var cardList in sessionProductCards)
        {
            foreach (var card in cardList)
            {
                if (card != null && card.gameObject != null)
                {
                    card.gameObject.SetActive(false);
                }
            }
        }

        if (sessionIndex < sessionProductCards.Count && sessionIndex < sessionProducts.Count)
        {
            for (int i = 0; i < sessionProductCards[sessionIndex].Count; i++)
            {
                var card = sessionProductCards[sessionIndex][i];
                var product = sessionProducts[sessionIndex][i];
                if (card != null && card.gameObject != null && product != null)
                {
                    card.gameObject.SetActive(true);
                    //card.SetProductDetailsCollapsedForEditRequest(product, DeleteProductForDraft);
                    card.LoadImageOfSession(product);
                }
            }
        }

        DOTween.Kill(photoCanvasgroup);
        DOTween.Kill(productCanvasgroup);
        photoCanvasgroup.DOFade(0, 0.1f);
        productCanvasgroup.DOFade(0, 0.1f);

        PlayerPrefs.SetInt("SessionId", session.session_id);
        Debug.Log("Session ID: " + session.session_id);

        StartCoroutine(UpdateUIAfterFade(photoCanvasgroup, productCanvasgroup));
    }


    private void UpdateProductDetails()
    {
        if (selectedSession == null)
        {
            Debug.LogError("❌ UpdateProductDetails called but selectedSession is null!");
            return;
        }

        if (selectedSession.products == null)
        {
            Debug.LogWarning("⚠️ selectedSession.products is null, initializing to empty list.");
            selectedSession.products = new List<SessionProductForSessionDetail>();
        }

        bool isCompanyOwner = PlayerPrefs.GetString("Role", "") == "company_owner";
        Transform container = isCompanyOwner ? productDetailContainer : productDetailContainerReq;
        GameObject noProductObj = isCompanyOwner ? noProductObject : noProductObjectReq;
        TextMeshProUGUI totalCostTxt = isCompanyOwner ? totalCostText : totalCostTextReq;
        Transform totalCostObjTransform = isCompanyOwner ? totalCostObj : totalCostObjReq;
        GameObject editScreen = isCompanyOwner ? editRequestScreen : editRequestScreenReq;
        Transform otherCostContainerTransform = isCompanyOwner ? otherCostContainer : otherCostContainerReq;
        Transform otherCostMainTransform = isCompanyOwner ? otherCostMain : otherCostMainReq;
        TMP_InputField notesInput = isCompanyOwner ? notesText : notesTextReq;


        foreach (GameObject costCard in otherCostCards)
            Destroy(costCard.gameObject);

        otherCostCards.Clear();

        var overview = sessionDetailResponse.data.quotation_details.overview;

        if (overview.other_cost != null && overview.other_cost.Count > 0)
        {
            foreach (var cost in overview.other_cost)
            {
                GameObject costItem = Instantiate(otherCostPrefab, otherCostContainerTransform);
                OtherCostCard card = costItem.GetComponent<OtherCostCard>();

                // Set name
                card.otherCostName.text = cost.name;

                // Ensure price is numeric text (for calculation)
                if (float.TryParse(cost.price, out var parsedPrice))
                    card.otherCostPrice.text = parsedPrice.ToString("F2");
                else
                    card.otherCostPrice.text = cost.price; // fallback

                // Setup delete button
                card.deleteButton.onClick.AddListener(() => DeleteOtherCost(card));

                otherCostCards.Add(card.gameObject);
            }
            StartCoroutine(UpdateLayout());
        }

        if (!string.IsNullOrEmpty(overview.notes))
            notesInput.text = overview.notes;
        else
            notesInput.text = "";


        noProductObj.SetActive(false);
        totalPrice = 0f;
        editScreen.SetActive(true);

        if (selectedSession.products.Count > 0)
        {
            foreach (SessionProductForSessionDetail product in selectedSession.products)
            {
                totalPrice += float.Parse( product.total);
            }

            float otherCostsTotal = 0f;
            foreach (GameObject costCard in otherCostCards)
            {
                OtherCostCard card = costCard.GetComponent<OtherCostCard>();
                if (card != null)
                {
                    TMP_InputField costInput = card.otherCostPrice;
                    if (costInput != null && float.TryParse(costInput.text, out float costValue))
                    {
                        otherCostsTotal += costValue;
                    }
                }
            }

            grandTotal = 0;
            grandTotal = totalPrice + otherCostsTotal;
            totalCostTxt.text = $"${grandTotal:F2}";

            LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
            otherCostMainTransform.SetAsLastSibling();
            totalCostObjTransform.SetAsLastSibling();

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else
        {
            float otherCostsTotal = 0f;
            foreach (GameObject costCard in otherCostCards)
            {
                if (costCard == null) continue; // ⬅️ skip destroyed cards

                OtherCostCard card = costCard.GetComponent<OtherCostCard>();
                if (card != null && card.otherCostPrice != null)
                {
                    if (float.TryParse(card.otherCostPrice.text, out float costValue))
                    {
                        otherCostsTotal += costValue;
                    }
                }
            }

            totalCostTxt.text = $"${otherCostsTotal:F2}";
            noProductObj.SetActive(true);
        }

        RecalculateTotal();
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
        //PopulateAdditionalCosts();
        photosCanvas.DOFade(1, 0.1f);
        scrollCanvas.DOFade(1, 0.1f);
    }

    public void UpdatePhotos()
    {
        if (PlayerPrefs.GetString("Role", "") == "company_owner")
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
        else
        {
            foreach (Transform child in photosContainerReq)
            {
                Destroy(child.gameObject);
            }

            if (selectedSession.screenshots.Count == 0)
            {
                noPhotoObjectReq.SetActive(true);
                return;
            }

            noPhotoObjectReq.SetActive(false);
            foreach (var screenshot in selectedSession.screenshots)
            {
                GameObject photo = Instantiate(photoPrefab, photosContainerReq);
                Image imageComponent = photo.transform.GetChild(0).GetComponent<Image>();
                if (!string.IsNullOrEmpty(screenshot.location_key))
                {
                    StartCoroutine(LoadImageFromURL(screenshot.location_key, imageComponent));
                }
            }
        }
    }

    int previousId;

    private void DeleteProductForDraft(SessionProductForSessionDetail product)
    {
        ShowDeleteProductConfirmationDialog(() =>
        {
            if (selectedSession != null && product != null)
            {
                Debug.Log($"Initiating deletion of product: {product.product_name} (ID: {product.id})");
                if (previousId != product.id)
                {
                    previousId = product.id;
                    StartCoroutine(DeleteProductCoroutine(product));
                }
            }
        });
    }

    private void ShowDeleteProductConfirmationDialog(System.Action confirmAction)
    {
        onProductConfirm = confirmAction;
        productDeleteDialog.SetActive(true);
    }

    private IEnumerator DeleteProductCoroutine(SessionProductForSessionDetail product)
    {
        bool isCompanyOwner = PlayerPrefs.GetString("Role", "") == "company_owner";
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        string url = LoginAPIHandler.instance.baseURL + sessionDetailFetcher.deleteProductIdEndpoint + product.id;
        Debug.Log($"Sending DELETE request to: {url}");

        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("accept", "*/*");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();

        //Debug.Log("Response : " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Product {product.product_name} (ID: {product.id}) deleted successfully.");

            selectedSession.products.RemoveAll(p => p.id == product.id);

            int sessionIndex = sessions.IndexOf(selectedSession);
            if (sessionIndex >= 0 && sessionIndex < sessionProductCards.Count && sessionIndex < sessionProducts.Count)
            {
                int productIndex = sessionProducts[sessionIndex].FindIndex(p => p.id == product.id);
                if (productIndex >= 0)
                {
                    ProductCardDetailWithInputfield cardToRemove = sessionProductCards[sessionIndex][productIndex];
                    if (cardToRemove != null)
                    {
                        sessionProductCards[sessionIndex].RemoveAt(productIndex);
                        sessionProducts[sessionIndex].RemoveAt(productIndex);
                        currentProductDetailCards.Remove(cardToRemove.gameObject);
                        Destroy(cardToRemove.gameObject);
                    }
                }
            }

            if ( selectedSession == null || selectedSession.products == null || selectedSession.products.Count == 0)
            {
                
                GameObject noProductObj = isCompanyOwner ? noProductObject : noProductObjectReq;
                noProductObj.SetActive(true);
            }

            yield return StartCoroutine(GetSessionDetails(requestID));
            yield return StartCoroutine(SalesRepRequestOverviewHandler.instance.GetSessionDetails(requestID));
            //UpdateProductDetails();
            //StartCoroutine(UpdateLayout());
            //RecalculateTotal();

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else if (request.responseCode == 403)
        {
            Debug.LogError("Unauthorized access (403). Prompting login.");
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError($"Failed to delete product: {request.responseCode} - {request.error}");
            ErrorPopupHandler.instance.errorDescText.text = $"Failed to delete product: {request.error}";
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            //ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
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
                controller.InitializeForEditRequest(screenshotData, DeleteScreenshot, this);
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
                            controller.InitializeForEditRequest(screenshot, DeleteScreenshot, this);
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

    public void AddOtherCost()
    {
        Transform container = PlayerPrefs.GetString("Role", "") == "company_owner" ? otherCostContainer : otherCostContainerReq;
        GameObject newCostCard = Instantiate(otherCostPrefab, container);
        newCostCard.transform.SetAsLastSibling();

        OtherCostCard card = newCostCard.GetComponent<OtherCostCard>();
        if (card != null)
        {
            card.deleteButton.onClick.AddListener(() => DeleteOtherCost(card));

            card.otherCostPrice.onValueChanged.AddListener((string newValue) =>
            {
                RecalculateTotal();
            });

            otherCostCards.Add(newCostCard);
        }

        StartCoroutine(UpdateLayout());
        RecalculateTotal();
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
        yield return null;
        Canvas.ForceUpdateCanvases();
        Transform container = PlayerPrefs.GetString("Role", "") == "company_owner" ? otherCostContainer : otherCostContainerReq;
        Transform otherCostMainTransform = PlayerPrefs.GetString("Role", "") == "company_owner" ? otherCostMain : otherCostMainReq;

        LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(otherCostMainTransform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(otherCostMainTransform.parent as RectTransform);
    }

    public float grandTotal;
    public void RecalculateTotal()
    {
        sessionTotals.Clear();
        grandTotal = 0f;

        for (int i = 0; i < sessionProductCards.Count; i++)
        {
            float sessionTotal = 0f;
            foreach (var card in sessionProductCards[i])
            {
                if (card != null)
                {
                    sessionTotal += card.totalCount;
                }
            }
            sessionTotals.Add(sessionTotal);
            grandTotal += sessionTotal;

            if (i < sessionTotalTexts.Count)
            {
                sessionTotalTexts[i].text = $"Session {i + 1}: ${sessionTotal:F2}";
            }
        }

        float otherCostsTotal = 0f;
        foreach (GameObject costCard in otherCostCards)
        {
            OtherCostCard card = costCard.GetComponent<OtherCostCard>();
            if (card != null)
            {
                TMP_InputField costInput = card.otherCostPrice;
                if (costInput != null && float.TryParse(costInput.text, out float costValue))
                {
                    otherCostsTotal += costValue;
                }
            }
        }

        grandTotal += otherCostsTotal;

        //if (grandTotalText != null)
        //{
        //    grandTotalText.text = $"${grandTotal:F2}";
        //}
        Debug.Log($"Grand Total: ${grandTotal:F2}");
        grandTotal = float.Parse(grandTotal.ToString("F2"));

        bool isCompanyOwner = PlayerPrefs.GetString("Role", "") == "company_owner";
        TextMeshProUGUI totalCostTxt = isCompanyOwner ? totalCostText : totalCostTextReq;
        totalCostTxt.text = $"${grandTotal:F2}"; //$"${(totalPrice + otherCostsTotal):F2}";

        Debug.Log($"${(totalPrice + otherCostsTotal):F2}");
    }
}
