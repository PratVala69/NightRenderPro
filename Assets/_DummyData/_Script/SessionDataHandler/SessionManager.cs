using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using DG.Tweening;

public class SessionManager : MonoBehaviour
{
    //public static SessionManager instance;

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

    [Header("Button")]
    public Button newSessionButton; // Button to start a new session
    public Button nextButton; // Button to proceed
    public Button photosButton;

    [Header("For Button Selected")]
    // Add these fields for sprite and text color customization
    public Sprite selectedButtonSprite; // Sprite for selected button
    public Sprite defaultButtonSprite; // Default sprite for buttons
    public Color selectedTextColor = Color.white; // Text color for selected button
    public Color defaultTextColor = Color.black; // Default text color

    [Header("Session Empty Or List")]
    public GameObject emptySession;
    public GameObject[] listInSession;

    private List<SessionData> sessions;
    private SessionData selectedSession;
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

    [Header("All Session Panel")]
    public Transform allPhotoContainer;
    public Transform allSessionContainer;
    public Transform allPhotoListContainer;
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
    private List<GameObject> otherCostCards = new List<GameObject>();

    private System.Action onProductConfirm;
    private System.Action onImageConfirm;

    public static bool isFirstOpen = true;

    [Header("For Fade")]
    public CanvasGroup photoCanvasgroup;
    public CanvasGroup productCanvasgroup;

    public ClientHandler clientHandler;

    int sessionCount = 0;
    int productCount = 0;

    public List<Button> sessionButtons = new List<Button>();

    //private void Awake()
    //{
    //    if (instance == null)
    //    {
    //        instance = this;
    //    }
    //    else
    //    {
    //        Destroy(gameObject);
    //    }
    //}

    GameObject currentScreenObj;

    public void StartPopulateSession_ScreenDisable(GameObject currentScreen)
    {
        currentScreenObj = currentScreen;
        StartPopulateSession();
    }


    public void StartPopulateSession()
    {
        // Load sessions
        sessions = SessionDataSaver.instance.LoadSessions();
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
                SessionDetailFetcher.instance.RecalculateTotal();
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

        // Check if sessions are available
        if (sessions.Count == 0)
        {
            // Optionally, display a message or UI element indicating no sessions
            Debug.Log("No sessions available.");
            selectedSession = null; // Ensure no session is selected

            nextButton.interactable = false;
            emptySession.SetActive(true);
            for (int i = 0; i < listInSession.Length; i++)
            {
                listInSession[i].SetActive(false);
            }

            return;
        }

        emptySession.SetActive(false);
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
            buttonText.text = $"Session {session.sessionNumber}";
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
       // clientHandler.UpdateClientSessionAndProductCount(RequestHandler.selectedClientNameStr, sessionCount, productCount);
        // Select the last session by default if sessions exist

        if (sessions.Count > 0)
        {
            nextButton.interactable = true;

            int selectedIndex = isFirstOpen ? 0 : sessions.Count - 1;
            if (selectedIndex >= sessionButtonsContainer.childCount)
            {
                Debug.LogError($"Selected index {selectedIndex} out of bounds for sessionButtonsContainer with {sessionButtonsContainer.childCount} children");
                return;
            }
            SessionData selectedSessionData = sessions[selectedIndex];
            if (sessionButtonsContainer.GetChild(selectedIndex).transform.GetComponent<Button>() == null)
            {
                Debug.LogError($"Button component missing on sessionButtonsContainer child at index {selectedIndex}");
                return;
            }
            Debug.Log($"Selecting session {selectedSessionData.sessionNumber} at index {selectedIndex}, isFirstOpen: {isFirstOpen}");
            SelectSession(selectedSessionData, sessionButtons[selectedIndex]);
            //if (isFirstOpen)
            //{
            //    UpdateBool(); // Update isFirstOpen after first selection
            //}
        }
    }

    public void CheckProductCount()
    {
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
        //clientHandler.UpdateClientSessionAndProductCount(RequestHandler.selectedClientNameStr, sessionCount, productCount);
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

    private void SelectSession(SessionData session, Button selectedButton)
    {
        Debug.Log("Selected session : " + session.sessionNumber);
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
            if (!string.IsNullOrEmpty(screenshot.screenshotPath))
            {
                Texture2D thumbnail = SessionDataSaver.instance.LoadScreenshot(screenshot.screenshotPath);
                if (thumbnail != null)
                {
                    Sprite thumbnailSprite = Sprite.Create(
                        thumbnail,
                        new Rect(0, 0, thumbnail.width, thumbnail.height),
                        new Vector2(0.5f, 0.5f) // pivot
                    );

                    Image imageComponent = photo.transform.GetChild(0).GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = thumbnailSprite;
                    }
                }
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

        //if (selectedSession.products.Count > 0)
        //{
        //    noProductObject.SetActive(false);
        //    foreach (ProductData product in selectedSession.products)
        //    {
        //        Debug.Log("Product : " + product.productName);
        //        // Instantiate the product detail card prefab
        //        GameObject productDetailCard = Instantiate(productDetailPanel, productDetailContainer);
        //        ProductCardDetail detailCard = productDetailCard.GetComponent<ProductCardDetail>();
        //        if (detailCard != null)
        //        {
        //            detailCard.SetProductDetailsFromAR(product, DeleteProduct);
        //            currentProductDetailCards.Add(productDetailCard); // Track the card
        //        }
        //        else
        //        {
        //            Debug.LogError("ProductDetailCard component not found on instantiated prefab.");
        //            Destroy(productDetailCard); // Clean up if component is missing
        //        }
        //    }
        //    LayoutRebuilder.ForceRebuildLayoutImmediate(productDetailContainer.GetComponent<RectTransform>());

        //}
        //else
        //{
        //    noProductObject.SetActive(true);
        //}
    }

    private void OnNextClicked()
    {
        // Logic to proceed to the next step (e.g., pricing or submission)
        Debug.Log("Next clicked for Session " + selectedSession.sessionNumber);
    }

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

    private void DeleteScreenshot(ScreenshotData screenshot)
    {
        ShowDeleteImageConfirmationDialog(() =>
        {
            if (selectedSession != null && screenshot != null)
            {
                Debug.Log($"Deleting screenshot: {screenshot.screenshotName}");
                selectedSession.screenshots.Remove(screenshot);

                if (!string.IsNullOrEmpty(screenshot.screenshotPath) && System.IO.File.Exists(screenshot.screenshotPath))
                {
                    try
                    {
                        System.IO.File.Delete(screenshot.screenshotPath);
                        Debug.Log($"Deleted screenshot at path: {screenshot.screenshotPath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error deleting screenshot: {ex.Message}");
                    }
                }

                RemoveImagePrefabFromList(screenshot);

                SessionDataSaver.instance.SaveSessions(sessions);
                UpdatePhotos();
                AllPhotosPopulate();
            }
        });
    }

    private void DeleteProduct(ProductData product)
    {
        ShowDeleteProductConfirmationDialog(() =>
        {
            if (selectedSession != null && product != null)
            {
                Debug.Log($"Deleting product: {product.productName}");
                selectedSession.products.Remove(product);

                foreach (var session in sessions)
                {
                    var match = session.products.Find(p => p.productUniqueName == product.productUniqueName);
                    if (match != null)
                    {
                        session.products.Remove(match);
                        break; // Optional: Stop if only one match is expected
                    }
                }

                if (!string.IsNullOrEmpty(product.imagePath) && System.IO.File.Exists(product.imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(product.imagePath);
                        Debug.Log($"Deleted image at path: {product.imagePath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("Error deleting image: " + ex.Message);
                    }
                }

                SessionDataSaver.instance.SaveSessions(sessions);

                UpdateProductDetails();
                AllSessionPopulate();
                // Remove corresponding prefab from the image list UI
            }
        });
    }

    private void RemoveImagePrefabFromList(ScreenshotData screenshot)
    {
        // Assuming you have a parent container like a ScrollView content panel
        Transform imageListContent = photoListContainer; // Assign this in the inspector or your class

        foreach (Transform child in imageListContent)
        {
            ImagePrefabController controller = child.GetComponent<ImagePrefabController>();

            //Debug.Log("---> " + screenshot.screenshotPath + "--" + controller.screenshotData.screenshotPath);
            //if (controller != null && controller.screenshotData != null && controller.screenshotData.screenshotPath == screenshot.screenshotPath)
            //{
            //    Destroy(child.gameObject);
            //    break;
            //}
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
            GameObject imageObj = Instantiate(photoListPrefab, photoListContainer);
            ImagePrefabController controller = imageObj.GetComponent<ImagePrefabController>();
            //if (controller != null)
            //{
            //    controller.Initialize(screenshot, DeleteScreenshot, this);
            //}
        }
    }

    public void OnImageListClose(GameObject imageListAnimPanel)
    {
        imageListAnimPanel.GetComponent<Animation>().Play("ImagePanelCloseAnim");
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

        // Iterate through all sessions and their products
        foreach (var session in sessions)
        {
            foreach (ProductData product in session.products)
            {
                productCount++;
                hasAnyProduct = true;
                GameObject productDetailCard = Instantiate(productCardInputfieldPrefab, allSessionContainer);
                ProductCardDetailWithInputfield detailCard = productDetailCard.GetComponent<ProductCardDetailWithInputfield>();
                if (detailCard != null)
                {
                    Debug.Log("session number : " + session.sessionNumber);
                    //selectedSession = 
                   // detailCard.SetProductDetailsCollapsed(product, DeleteProduct);
                    allProductDetailCards.Add(productDetailCard);
                }
                else
                {
                    Debug.LogError("ProductDetailCardWithInputfield component not found on instantiated prefab.");
                    Destroy(productDetailCard);
                }
            }
        }

        int sessionCount = sessions.Count;
      
       // clientHandler.UpdateClientSessionAndProductCount(RequestHandler.selectedClientNameStr, sessionCount, productCount);

        // Update UI visibility
        noProductObjectInAllSession.SetActive(!hasAnyProduct);

        // Ensure other cost and total cost objects are positioned correctly
        otherCostObj.SetAsLastSibling();
        totalCostObj.SetAsLastSibling();
    }

    public void AllPhotosPopulate()
    {
        foreach (Transform child in allPhotoContainer)
        {
            Destroy(child.gameObject);
        }

        bool hasAnyScreenshot = false;

        foreach (var session in sessions)
        {
            foreach (var screenshot in session.screenshots)
            {
                hasAnyScreenshot = true;

                GameObject photo = Instantiate(photoPrefab, allPhotoContainer);
                if (!string.IsNullOrEmpty(screenshot.screenshotPath))
                {
                    Texture2D thumbnail = SessionDataSaver.instance.LoadScreenshot(screenshot.screenshotPath);
                    if (thumbnail != null)
                    {
                        Sprite thumbnailSprite = Sprite.Create(
                            thumbnail,
                            new Rect(0, 0, thumbnail.width, thumbnail.height),
                            new Vector2(0.5f, 0.5f)
                        );

                        Image imageComponent = photo.transform.GetChild(0).GetComponent<Image>();
                        if (imageComponent != null)
                        {
                            imageComponent.sprite = thumbnailSprite;
                        }
                    }
                }
            }
        }

        noPhotoObjectInAllSession.SetActive(!hasAnyScreenshot);
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

        foreach (var session in sessions)
        {
            foreach (var screenshot in session.screenshots)
            {
                GameObject imageObj = Instantiate(photoListPrefab, photoListContainer);
                ImagePrefabController controller = imageObj.GetComponent<ImagePrefabController>();
                //if (controller != null)
                //{
                //    controller.Initialize(screenshot, DeleteScreenshot, this);
                //}
            }
        }
    }

    public void OnAllSessionPanelOpen()
    {
        AllSessionPopulate();
        AllPhotosPopulate();
        // AllPhotoListPopulate(); // Optional
    }
}