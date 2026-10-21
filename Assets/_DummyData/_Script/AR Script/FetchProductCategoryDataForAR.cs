using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FetchProductCategoryDataForAR : MonoBehaviour
{
    public string createSessionEndpoint = "request/createSession";
    public static FetchProductCategoryDataForAR instance;
    public Transform typeButtonContainer;
    public GameObject typeButtonPrefab;
    public TextMeshProUGUI typeNameText;
    public Transform productButtonContainer;
    public GameObject productButtonPrefab;
    public GameObject productMenuPanel;
    public TextMeshProUGUI productNameText;
    public TMP_Dropdown variantDropdown;
    public TMP_Dropdown colorDropdown;
    public GameObject sizeColorPanel;
    public Button confirmButton;
    public Sprite defaultSprite;
    public Sprite[] productCategoryImage;
    private List<CategoryGroup> arMeasurementResponse;
    public RulerManager rulerManager;
    public GoalManager goalManager;
    public ARSurfaceHandler surfaceHandler;
    public bool isMeasure;
    public GameObject loadingScreen;
    public TextMeshProUGUI placeHintText;
    public string selectedVariant;
    public string selectedColor;
    public string selectedProduct = "Gutter";
    private Button selectedTypeButton;
    private Button selectedProductButton;
    public Sprite defaultSpriteForButton;
    public Sprite selectedSpriteForButton;
    public Image selectedImage;
    private Dictionary<string, int> categoryToIndexMap = new Dictionary<string, int>
    {
        { "Rain Chains", 0 },
        { "Gutters", 1 },
        { "4-in PVC", 3 },
        { "Leaf Guards", 4 },
        { "Downspouts", 2 },
        { "Rain Tanks", 0 },
        { "Concrete Splash Blocks", 1 }
    };
    private Dictionary<string, int> categoryImageMap = new Dictionary<string, int>
    {
        { "Gutters", 0 },
        { "Rain Chains", 1 },
        { "Downspouts", 2 },
        { "4-in PVC", 3 },
        { "Leaf Guards", 4 },
        { "Rain Tanks", 5 },
        { "Concrete Splash Blocks", 6 }
    };
    private Dictionary<string, List<GameObject>> productButtonsByCategory = new Dictionary<string, List<GameObject>>();
    public GameObject testPrefab1;
    public GameObject testPrefab2;
    private ProductSelectionData pendingProductData; // Store product data until confirmed

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

    void Start()
    {
        loadingScreen.SetActive(true);
        confirmButton.onClick.AddListener(ConfirmSizeColor);
        if (ProductFetcher.instance?.productCategoryResponse?.data == null)
        {
            Debug.LogError("Product category data is missing.");
            return;
        }
        arMeasurementResponse = isMeasure
            ? ProductFetcher.instance.productCategoryResponse.data.measurement_list
            : ProductFetcher.instance.productCategoryResponse.data.placement_list;
        if (arMeasurementResponse == null)
        {
            Debug.LogError($"{(isMeasure ? "Measurement" : "Placement")} list is missing.");
            return;
        }
        typeNameText.text = "Product Categories";
        UpdateCategoryMappings();
        StartCoroutine(PopulateAllButtons());
    }

    private void UpdateCategoryMappings()
    {
        var categories = arMeasurementResponse.Select(item => item.product_category).ToList();
        for (int i = 0; i < categories.Count; i++)
        {
            if (!categoryToIndexMap.ContainsKey(categories[i]))
            {
                categoryToIndexMap[categories[i]] = i;
            }
        }
    }

    IEnumerator PopulateAllButtons()
    {
        foreach (Transform child in typeButtonContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in productButtonContainer)
        {
            Destroy(child.gameObject);
        }
        var categories = arMeasurementResponse.Select(item => item.product_category).Distinct().ToList();
        foreach (var category in categories)
        {
            GameObject buttonObj = Instantiate(typeButtonPrefab, typeButtonContainer);
            buttonObj.SetActive(true);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = category;
            Image categoryImage = buttonObj.transform.GetChild(1).transform.GetChild(0).GetComponent<Image>();
            if (categoryImage != null && productCategoryImage != null && categoryImageMap.ContainsKey(category))
            {
                int imageIndex = categoryImageMap[category];
                if (imageIndex < productCategoryImage.Length)
                {
                    categoryImage.sprite = productCategoryImage[imageIndex];
                }
                else
                {
                    categoryImage.sprite = defaultSprite;
                    Debug.LogWarning($"No sprite for category {category} at index {imageIndex}. Using default sprite.");
                }
            }
            else if (categoryImage != null)
            {
                categoryImage.sprite = defaultSprite;
                Debug.LogWarning($"No image mapping for category {category} or Image component missing.");
            }
            string selectedCategory = category;
            button.onClick.AddListener(() => ShowProductMenu(selectedCategory, button));
            productButtonsByCategory[selectedCategory] = new List<GameObject>();
            var products = arMeasurementResponse
                .Where(item => item.product_category == selectedCategory)
                .SelectMany(item => item.products)
                .ToList();
            if (products.Count == 0)
            {
                Debug.LogWarning($"No products found for category {category}.");
            }
            foreach (var product in products)
            {
                yield return StartCoroutine(CreateProductButton(product, selectedCategory));
            }
        }
        loadingScreen.SetActive(false);
        yield return null;
    }

    IEnumerator CreateProductButton(Product product, string category)
    {
        GameObject buttonObj = Instantiate(productButtonPrefab, productButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        buttonText.text = product.product_name;
        Image productImage = buttonObj.transform.GetChild(1).transform.GetChild(0).GetComponent<Image>();
        string imageUrl = LoginAPIHandler.instance.imageDownloadUrl + product.product_image;
        yield return StartCoroutine(LoadProductImage(imageUrl, productImage));
        int index = categoryToIndexMap.TryGetValue(category, out int mappedIndex) ? mappedIndex : 0;
        button.onClick.AddListener(() =>
        {
            Debug.Log("Product: " + product.product_name);
            if (isMeasure)
                placeHintText.text = "Tap + button to place points";
            pendingProductData = new ProductSelectionData
            {
                Index = index,
                Variants = product.product_variants ?? new List<ProductVariant>(),
                MeasurementItem = arMeasurementResponse.FirstOrDefault(item => item.product_category == category),
                name = product.product_name,
                fbxFileUrl = product.product_variants?.FirstOrDefault()?.fbx_file,
                localFbxPath = ""// ProductFetcher.instance?.GetLocalFbxPath(product.product_variants?.FirstOrDefault()?.fbx_file)
            };
            ShowSizeColorPopup(pendingProductData);
            SetSelectedProductButton(button);
        });
        productButtonsByCategory[category].Add(buttonObj);
        buttonObj.SetActive(false);
    }

    void ShowProductMenu(string category, Button clickedButton)
    {
        Debug.Log("category: " + category);
        int imageIndex = categoryImageMap[category];
        if (imageIndex < productCategoryImage.Length)
        {
            selectedImage.sprite = productCategoryImage[imageIndex];
        }
        else
        {
            selectedImage.sprite = defaultSprite;
        }
        SetSelectedTypeButton(clickedButton);
        productNameText.text = category;
        selectedProduct = category;
        foreach (var categoryButtons in productButtonsByCategory.Values)
        {
            foreach (var button in categoryButtons)
            {
                button.SetActive(false);
            }
        }
        if (productButtonsByCategory.ContainsKey(category))
        {
            foreach (var button in productButtonsByCategory[category])
            {
                button.SetActive(true);
            }
        }
        SetSelectedProductButton(null);
        surfaceHandler?.OnMenuHideAndDeactivate();
    }

    private void ShowSizeColorPopup(ProductSelectionData productData)
    {
        variantDropdown.ClearOptions();
        colorDropdown.ClearOptions();
        if (productData?.Variants == null || productData.Variants.Count == 0)
        {
            variantDropdown.options.Add(new TMP_Dropdown.OptionData("No Variant Available"));
            colorDropdown.options.Add(new TMP_Dropdown.OptionData("No Color Available"));
            variantDropdown.value = 0;
            colorDropdown.value = 0;
            variantDropdown.RefreshShownValue();
            colorDropdown.RefreshShownValue();
            sizeColorPanel.SetActive(true);
            return;
        }
        // Populate variant dropdown
        List<TMP_Dropdown.OptionData> variantOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var variant in productData.Variants)
        {
            if (!string.IsNullOrEmpty(variant.size_name))
            {
                variantOptions.Add(new TMP_Dropdown.OptionData(variant.size_name));
            }
        }
        variantDropdown.options = variantOptions.Count > 0 ? variantOptions : new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("No Variant Available") };
        variantDropdown.value = 0; // Default to first variant
        variantDropdown.RefreshShownValue();
        // Update color dropdown based on selected variant
        UpdateColorDropdown(productData.Variants[variantDropdown.value]);
        // Add listener to update colors when variant changes
        variantDropdown.onValueChanged.RemoveAllListeners();
        variantDropdown.onValueChanged.AddListener(index =>
        {
            UpdateColorDropdown(productData.Variants[index]);
        });
        sizeColorPanel.SetActive(true);
    }

    private void UpdateColorDropdown(ProductVariant variant)
    {
        colorDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> colorOptions = new List<TMP_Dropdown.OptionData>();
        if (variant.color != null && variant.color.Count > 0)
        {
            foreach (var color in variant.color)
            {
                if (!string.IsNullOrEmpty(color.color_name))
                {
                    colorOptions.Add(new TMP_Dropdown.OptionData(color.color_name));
                }
            }
        }
        colorDropdown.options = colorOptions.Count > 0 ? colorOptions : new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("No Color Available") };
        colorDropdown.value = 0;
        colorDropdown.RefreshShownValue();
    }

    private void SetSelectedTypeButton(Button clickedButton)
    {
        if (selectedTypeButton != null)
        {
            selectedTypeButton.image.sprite = defaultSpriteForButton;
        }
        selectedTypeButton = clickedButton;
        if (selectedTypeButton != null)
        {
            selectedTypeButton.image.sprite = selectedSpriteForButton;
        }
    }

    private void SetSelectedProductButton(Button clickedButton)
    {
        if (selectedProductButton != null)
        {
            selectedProductButton.image.sprite = defaultSpriteForButton;
        }
        selectedProductButton = clickedButton;
        if (selectedProductButton != null)
        {
            selectedProductButton.image.sprite = selectedSpriteForButton;
        }
    }

    public void ConfirmSizeColor()
    {
        selectedVariant = variantDropdown.options[variantDropdown.value].text;
        selectedColor = colorDropdown.options[colorDropdown.value].text;
        if (pendingProductData?.Variants != null && pendingProductData.Variants.Count > 0)
        {
            var selectedVariantData = pendingProductData.Variants
                .FirstOrDefault(v => v.size_name == selectedVariant);
            if (selectedVariantData != null)
            {
                var selectedColorData = selectedVariantData.color
                    .FirstOrDefault(c => c.color_name == selectedColor);
                if (selectedColorData != null)
                {
                    pendingProductData.Variant = selectedVariantData;
                    pendingProductData.selectedColorHex = selectedColorData.hex_code;
                    pendingProductData.fbxFileUrl = selectedVariantData.fbx_file;
                   // pendingProductData.localFbxPath = ProductFetcher.instance?.GetLocalFbxPath(selectedVariantData.fbx_file);
                    Debug.Log($"Confirmed Variant: {selectedVariant}, Color: {selectedColor}, FBX: {pendingProductData.fbxFileUrl}");
                }
                else
                {
                    Debug.LogWarning($"No color data found for selected color: {selectedColor}");
                }
            }
            else
            {
                Debug.LogWarning($"No variant data found for selected variant: {selectedVariant}");
            }
        }
        else
        {
            Debug.LogWarning("No variants available for the selected product.");
        }
        // Pass the updated product data to RulerManager
        if (rulerManager != null)
        {
            rulerManager.SetProductData(pendingProductData);
        }
        else if (goalManager != null && goalManager.objectSpawner != null)
        {
            //goalManager.CurrentProductData = pendingProductData;
            //goalManager.SelectedColorHex = pendingProductData.selectedColorHex;
            //goalManager.SetProductData(pendingProductData);
        }
        sizeColorPanel.SetActive(false);
    }

    private IEnumerator LoadProductImage(string url, Image targetImage)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogError("Failed to load product image: " + url + " | " + request.error);
            targetImage.sprite = defaultSprite;
            yield break;
        }
        Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
        Sprite thumbnailSprite = Sprite.Create(downloadedTexture,
            new Rect(0, 0, downloadedTexture.width, downloadedTexture.height), new Vector2(0.5f, 0.5f));
        if (targetImage != null)
        {
            targetImage.sprite = thumbnailSprite;
            targetImage.preserveAspect = true;
            AspectRatioFitter fitter = targetImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = targetImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = downloadedTexture.width / (float)downloadedTexture.height;
        }
    }
}

[System.Serializable]
public class ProductSelectionData
{
    public int Index;
    public ProductVariant Variant;
    public List<ProductVariant> Variants;
    public CategoryGroup MeasurementItem;
    public string name;
    public string fbxFileUrl;
    public string localFbxPath;
    public string selectedColorHex;
}

[System.Serializable]
public class CreateSessionRequest
{
    public int request_id;
    public List<SessionProductForCreateSession> session_product;
}

[System.Serializable]
public class SessionProductForCreateSession
{
    public string product_name;
    public int length;
    public string length_unit;
    public float width;
    public string width_unit;
    public int color_id;
    public int quantity;
    public int cost;
    public List<Screenshot> screenshot;
    public int company_product_id;
}