using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;

public class AddProductManager : MonoBehaviour
{
    public string sessionCountEndpoint = "request/session-count/";
    public string addProductEndpoint = "request/addProduct";

    [Header("UI References")]
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown productDropdown;
    public TMP_Dropdown variantDropdown;
    public TMP_Dropdown sessionDropdown;
    public TMP_Dropdown colorDropdown;
    public TMP_InputField lengthInput;
    public TMP_InputField qtyInput;
    public Button saveButton;
    public GameObject addProductScreen;

    public ProductCategoryResponse parsedData;

    private int selectedCategoryIndex = -1;
    private int selectedProductIndex = -1;
    private ProductVariant selectedVariant;
    private bool isMeasurementCategory = true;

    public SessionDetailFetcher sessionDetailFetcher;
    public ClientHandler clientHandler;
    public SalesRepAPIHandler salesRepAPIHandler;

    [Header("Screen")]
    public GameObject pricingScreen;
    public GameObject editRequestScreen;
    public GameObject editRequestScreenRequest;

    bool isrequest = false;

    public void AddManualProduct()
    {
        parsedData = ProductFetcher.instance.productCategoryResponse;

        categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        productDropdown.onValueChanged.AddListener(OnProductChanged);
        variantDropdown.onValueChanged.AddListener(OnVariantChanged);
        sessionDropdown.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });
        lengthInput.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });
        qtyInput.onValueChanged.AddListener(delegate { UpdateSaveButtonState(); });

        PopulateCategoryDropdown();
        StartCoroutine(FetchSessionsCoroutine());
        UpdateSaveButtonState();
    }

    public void AddProductManualRequest()
    {
        StartCoroutine(FetchProductCetagoryResponse());
    }

    IEnumerator FetchProductCetagoryResponse()
    {
        if (ProductFetcher.instance.productCategoryResponse != null)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(true);
            yield return StartCoroutine(ProductFetcher.instance.FetchProductCategories());

            ErrorPopupHandler.instance.loadingScreen.SetActive(false);

            AddManualProduct();
        }
        else
        {
            AddManualProduct();
        }
    }

    public void SetRequestTrue(bool value)
    {
        isrequest = value;
    }

    //void PopulateCategoryDropdown()
    //{
    //    categoryDropdown.ClearOptions();
    //    var options = new List<string> { "Select Product Category <color=red>*</color>" };
    //    var measurementCategories = parsedData.data.measurement_list.Select(m => new { Name = m.product_category, IsMeasurement = true }).ToList();
    //    var placementCategories = parsedData.data.placement_list.Select(p => new { Name = p.product_category, IsMeasurement = false }).ToList();

    //    var allCategories = measurementCategories.Concat(placementCategories).Select(c => c.Name).ToList();
    //    options.AddRange(allCategories);
    //    categoryDropdown.AddOptions(options);

    //    categoryDropdown.value = 0;
    //}

    void PopulateCategoryDropdown()
    {
        categoryDropdown.ClearOptions();
        var options = new List<string> { "Select Product Category <color=red>*</color>" };
        var measurementCategories = parsedData.data.measurement_list?.Select(m => new { Name = m.product_category, IsMeasurement = true }).ToList();
        var placementCategories = parsedData.data.placement_list?.Select(p => new { Name = p.product_category, IsMeasurement = false }).ToList();
        var allCategories = measurementCategories.Concat(placementCategories).Select(c => c.Name).ToList();
        options.AddRange(allCategories);
        categoryDropdown.AddOptions(options);
        categoryDropdown.value = 0;

        // Explicitly initialize productDropdown
        productDropdown.ClearOptions();
        productDropdown.AddOptions(new List<string> { "Select Product <color=red>*</color>" });
        productDropdown.value = 0;
        productDropdown.RefreshShownValue(); // Ensure UI updates

        // Explicitly initialize variantDropdown
        variantDropdown.ClearOptions();
        variantDropdown.AddOptions(new List<string> { "Select Variant <color=red>*</color>" });
        variantDropdown.value = 0;
        variantDropdown.RefreshShownValue(); // Ensure UI updates

        // Explicitly initialize colorDropdown
        colorDropdown.ClearOptions();
        colorDropdown.AddOptions(new List<string> { "Select Color <color=red>*</color>" });
        colorDropdown.value = 0;
        colorDropdown.RefreshShownValue(); // Ensure UI updates
    }

    public void ResetInputFields()
    {
        lengthInput.text = "";
        qtyInput.text = "";
        categoryDropdown.value = 0;
        productDropdown.value = 0;
        variantDropdown.value = 0;
        colorDropdown.value = 0;
        productDropdown.value = 0;
        sessionDropdown.value = 0;

        UpdateSaveButtonState();
    }

    void OnCategoryChanged(int index)
    {
        if (index == 0) // "Select Product Category" selected
        {
            selectedCategoryIndex = -1;
            productDropdown.ClearOptions();
            productDropdown.AddOptions(new List<string> { "Select Product <color=red>*</color>" });
            productDropdown.value = 0;
            variantDropdown.ClearOptions();
            variantDropdown.AddOptions(new List<string> { "Select Variant <color=red>*</color>" });
            variantDropdown.value = 0;
            colorDropdown.ClearOptions();
            colorDropdown.AddOptions(new List<string> { "Select Color <color=red>*</color>" });
            colorDropdown.value = 0;
            UpdateSaveButtonState();
            return;
        }

        selectedCategoryIndex = index - 1; // Adjust for placeholder
        var measurementCategories = parsedData.data.measurement_list;
        var placementCategories = parsedData.data.placement_list;
        int measurementCount = measurementCategories.Count;

        isMeasurementCategory = selectedCategoryIndex < measurementCount;

        List<Product> products;
        if (isMeasurementCategory)
        {
            products = measurementCategories[selectedCategoryIndex].products;
        }
        else
        {
            products = placementCategories[selectedCategoryIndex - measurementCount].products;
        }

        productDropdown.ClearOptions();
        productDropdown.AddOptions(new List<string> { "Select Product <color=red>*</color>" });
        productDropdown.AddOptions(products.Select(p => p.product_name).ToList());

        productDropdown.value = 0;
        OnProductChanged(0);
    }

    void OnProductChanged(int index)
    {
        if (index == 0) // "Select Product" selected
        {
            selectedProductIndex = -1;
            variantDropdown.ClearOptions();
            variantDropdown.AddOptions(new List<string> { "Select Variant <color=red>*</color>" });
            variantDropdown.value = 0;
            colorDropdown.ClearOptions();
            colorDropdown.AddOptions(new List<string> { "Select Color <color=red>*</color>" });
            colorDropdown.value = 0;
            UpdateSaveButtonState();
            return;
        }

        selectedProductIndex = index - 1; // Adjust for placeholder
        var measurementCategories = parsedData.data.measurement_list;
        var placementCategories = parsedData.data.placement_list;

        List<ProductVariant> variants;
        if (isMeasurementCategory)
        {
            variants = measurementCategories[selectedCategoryIndex].products[selectedProductIndex].product_variants;
        }
        else
        {
            variants = placementCategories[selectedCategoryIndex - measurementCategories.Count].products[selectedProductIndex].product_variants;
        }

        variantDropdown.ClearOptions();
        variantDropdown.AddOptions(new List<string> { "Select Variant <color=red>*</color>" });
        variantDropdown.AddOptions(variants.Select(v => v.size_name).Distinct().ToList());

        variantDropdown.value = 0;
        OnVariantChanged(0);
    }

    void OnVariantChanged(int index)
    {
        if (index == 0) // "Select Variant" selected
        {
            selectedVariant = null;
            colorDropdown.ClearOptions();
            colorDropdown.AddOptions(new List<string> { "Select Color <color=red>*</color>" });
            colorDropdown.value = 0;
            UpdateSaveButtonState();
            return;
        }

        string selectedSize = variantDropdown.options[index].text;
        var measurementCategories = parsedData.data.measurement_list;
        var placementCategories = parsedData.data.placement_list;

        List<ProductVariant> variants;
        if (isMeasurementCategory)
        {
            variants = measurementCategories[selectedCategoryIndex].products[selectedProductIndex].product_variants;
        }
        else
        {
            variants = placementCategories[selectedCategoryIndex - measurementCategories.Count].products[selectedProductIndex].product_variants;
        }

        selectedVariant = variants.FirstOrDefault(v => v.size_name == selectedSize);

        if (selectedVariant != null)
        {
            colorDropdown.ClearOptions();
            colorDropdown.AddOptions(new List<string> { "Select Color <color=red>*</color>" });
            colorDropdown.AddOptions(selectedVariant.color.Select(c => c.color_name).ToList());
            colorDropdown.value = 0;
        }

        if (!isMeasurementCategory)
        {
            qtyInput.gameObject.SetActive(true);
            lengthInput.gameObject.SetActive(false);
        }
        else
        {
            qtyInput.gameObject.SetActive(false);
            lengthInput.gameObject.SetActive(true);
        }

        UpdateSaveButtonState();
    }

    void UpdateSaveButtonState()
    {
        bool isValid =
            categoryDropdown.value > 0 &&
            productDropdown.value > 0 &&
            variantDropdown.value > 0 &&
            sessionDropdown.value > 0 &&
            colorDropdown.value > 0 &&
            (isMeasurementCategory ? !string.IsNullOrEmpty(lengthInput.text) : !string.IsNullOrEmpty(qtyInput.text));

        saveButton.interactable = isValid;
    }

    private List<int> sessionIdList = new List<int>();

    [ContextMenu("Fetch Session Count")]
    public void FetchSessionCount()
    {
        StartCoroutine(FetchSessionsCoroutine());
    }

    private IEnumerator FetchSessionsCoroutine()
    {
        string url = LoginAPIHandler.instance.baseURL + sessionCountEndpoint + PlayerPrefs.GetInt("RequestId");
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
        request.SetRequestHeader("accept", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            SessionResponseRoot response = JsonUtility.FromJson<SessionResponseRoot>(json);
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Select Session <color=red>*</color>") };

            sessionDropdown.ClearOptions();
            foreach (var session in response.data.sessions)
            {
                options.Add(new TMP_Dropdown.OptionData(session.session_name));
            }

            sessionDropdown.AddOptions(options);
            sessionIdList = response.data.sessions.Select(s => s.session_id).ToList();
            sessionIdList.Insert(0, -1); // Placeholder for "Select Session"
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Session fetch failed: " + request.downloadHandler.text);
            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with session count API";
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    public void AddProductButtonClick()
    {
        StartCoroutine(SendAddProductRequest());
    }

    private IEnumerator SendAddProductRequest()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        if (selectedVariant == null || colorDropdown.value == 0)
        {
            Debug.LogError("Selected variant or color is invalid.");
            ErrorPopupHandler.instance.errorDescText.text = "Please select a valid variant and color.";
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            yield break;
        }

        int requestId = PlayerPrefs.GetInt("RequestId");
        int sessionId = sessionIdList[sessionDropdown.value];
        var selectedColor = selectedVariant.color[colorDropdown.value - 1]; // Adjust for placeholder
        int companyProductId = selectedVariant.company_product_id;
        int colorId = selectedColor.id;

        AddProductRequest payload = new AddProductRequest
        {
            request_id = requestId,
            session_id = sessionId,
            company_product_id = companyProductId,
            color_id = colorId
        };

        if (isMeasurementCategory)
        {
            if (!float.TryParse(lengthInput.text, out float length))
            {
                Debug.LogError("Invalid length input.");
                ErrorPopupHandler.instance.errorDescText.text = "Please enter a valid length.";
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
                yield break;
            }
            payload.length = length;
            payload.quantity = 0;
        }
        else
        {
            if (!int.TryParse(qtyInput.text, out int qty))
            {
                Debug.LogError("Invalid qty input.");
                ErrorPopupHandler.instance.errorDescText.text = "Please enter a valid quantity.";
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
                yield break;
            }
            payload.quantity = qty;
            payload.length = 0;
        }

        string jsonPayload = JsonUtility.ToJson(payload);
        Debug.Log("Sending JSON: " + jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(LoginAPIHandler.instance.baseURL + addProductEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Product added successfully: " + request.downloadHandler.text);

                //yield return StartCoroutine(clientHandler.GetClientList());
                //yield return StartCoroutine(SalesRepRequestOverviewHandler.instance.GetSessionDetails(requestId));

                ////if (PlayerPrefs.GetString("Role", "") == "company_owner")
                ////{
                ////    Debug.Log("___is company_owner");
                ////    yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));
                ////}//yield return StartCoroutine(clientHandler.GetAllClientList());

                if (isrequest)
                {
                    Debug.Log("___is Request");
                    yield return StartCoroutine(RequestEditHandler.instance.GetSessionDetails(RequestEditHandler.instance.requestID));
                }
                else
                {
                    Debug.Log("!!___is Request");
                    //yield return StartCoroutine(sessionDetailFetcher.GetSessionDetails());
                    yield return StartCoroutine(sessionDetailFetcher.FetchDraftRequestDetails());
                }


                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                addProductScreen.SetActive(false);

                ResetInputFields();
            }
            else if (request.responseCode == 403)
            {
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
            }
            else
            {
                Debug.LogError("API Error: " + request.error + " | Response: " + request.downloadHandler.text);
                ErrorPopupHandler.instance.errorDescText.text = "There is some issue with add product API";
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }
}

[System.Serializable]
public class SessionDataForAddProduct
{
    public int session_id;
    public string session_name;
}

[System.Serializable]
public class SessionResponseData
{
    public int request_id;
    public int session_count;
    public List<SessionDataForAddProduct> sessions;
}

[System.Serializable]
public class SessionResponseRoot
{
    public SessionResponseData data;
    public int statusCode;
    public string message;
}