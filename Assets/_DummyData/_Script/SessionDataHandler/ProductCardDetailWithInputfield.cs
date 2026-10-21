using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductCardDetailWithInputfield : MonoBehaviour
{
    public TextMeshProUGUI productNameText; // Text for product name and quantity in meters
    public TextMeshProUGUI sizeText; // Text for size (width)
    public TextMeshProUGUI materialText; // Text for material (color)
    public TextMeshProUGUI quantityText; // Text for quantity
    public TMP_InputField costInputfield; // Text for total cost and cost per unit
    public TMP_InputField widthInputfield; // Text for total cost and cost per unit
    public TMP_InputField placementCostInputfield; // Text for total cost and cost per unit
    public TextMeshProUGUI variationText; // Text for variation percentage
    public TextMeshProUGUI productPricePerPieceText; // Text for additional costs
    public TextMeshProUGUI inchText;
    public Image productImage;
    public TextMeshProUGUI totalCostCount;

    public float totalCount;

    public string imagePath;

    public Button deleteButton; // Assign in the prefab

    public ProductDetail productData;
    private System.Action<ProductDetail> onDelete;

    public SessionProductForSessionDetail productDataSession;
    private System.Action<SessionProductForSessionDetail> onDeleteSession;

    public Animation expandCollapseAnimation;

    public GameObject[] expandableObjects;
    public GameObject[] collapsableObjects;

    public ContentSizeFitter expandFitter;

    public bool isCollapsed = false;

    public Transform additionalCostContainer;
    public GameObject additionalCostTextPrefab;
    public GameObject totalCost;

    public float expandHeight = 1000f;
    public float collapseHeight = 452f;
    public float duration = 0.3f;
    public Ease easing = Ease.OutCubic;

    private LayoutElement layoutElement;
    private Tween currentTween;

    bool isEditRequest = false;

    //public ProductDetail productDetail;

    public void ProductCardCollapsed()
    {
        if (isCollapsed)
        {
            OnCollapse();
        }
    }

    private float TryParseFloat(string value)
    {
        if (float.TryParse(value, out float result))
            return result;
        return 0f; // fallback
    }

    public void ResetData()
    {
        // Select source product depending on mode
        bool isSession = isEditRequest;
        string productName = isSession ? productDataSession.product_name : productData.product_name;
        string material = isSession ? productDataSession.product_type : productData.product_type;
        float quantity = isSession ? productDataSession.quantity : productData.quantity;
        string sizeName = isSession ? productDataSession.size_name : productData.size_name;
        string colorName = isSession ? productDataSession.color.color_name : productData.color.color_name;
        float price = isSession ? TryParseFloat(productDataSession.price) : TryParseFloat(productData.price);
        float cost = isSession  ? TryParseFloat(productDataSession.cost)  : TryParseFloat(productData.cost);
        float variance = isSession ? productDataSession.variance : productData.variance;
        float length = isSession ? productDataSession.length_in_inches : productData.length_in_inches;
        float total = isSession ? TryParseFloat(productDataSession.total) : TryParseFloat(productData.total);
        var additionalCosts = isSession ? productDataSession.additional_cost : productData.additional_cost;

        // --- Set UI fields ---
        productNameText.text = productName;
        materialText.text = material;
        quantityText.text = $"Qty: {quantity}";
        sizeText.text = $"{sizeName} | {colorName}";

        productPricePerPieceText.text = $"${price} per piece";
        costInputfield.text = cost.ToString("F2");
        variationText.text = $"{variance}% Variation is considered";
        widthInputfield.text = length.ToString();
        placementCostInputfield.text = cost.ToString("F2");

        totalCostCount.text = $"${total:F2}";
        totalCount = total;

        // --- Handle Additional Costs ---
        int existingChildren = additionalCostContainer.childCount;

        // Add missing children if needed
        for (int i = existingChildren; i < additionalCosts.Count; i++)
        {
            Instantiate(additionalCostTextPrefab, additionalCostContainer);
        }

        // Update or reset children
        for (int i = 0; i < additionalCosts.Count; i++)
        {
            var costItem = additionalCosts[i];
            Transform child = additionalCostContainer.GetChild(i);
            child.gameObject.SetActive(true);

            var costText = child.GetComponent<TextMeshProUGUI>();
            if (costText != null) costText.text = costItem.name;

            var inputField = child.GetChild(0).GetChild(1).GetComponent<TMP_InputField>();
            if (float.TryParse(costItem.price, out var parsedPrice))
                inputField.text = parsedPrice.ToString("F2");
            else
                inputField.text = costItem.price;

            // Clear old listeners to prevent stacking
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener((value) => UpdateTotal());
        }

        // Hide extra children if there are more than needed
        for (int i = additionalCosts.Count; i < existingChildren; i++)
        {
            additionalCostContainer.GetChild(i).gameObject.SetActive(false);
        }

        // --- Recalculate Total ---
        if (isSession)
            RequestEditHandler.instance.RecalculateTotal();
        else
            SessionDetailFetcher.instance.RecalculateTotal();

        Collapse();
    }

    public void ResetDataCheck()
    {
        // Select source product depending on mode
        bool isSession = isEditRequest;
        string productName = isSession ? productDataSession.product_name : productData.product_name;
        string material = isSession ? productDataSession.product_type : productData.product_type;
        float quantity = isSession ? productDataSession.quantity : productData.quantity;
        string sizeName = isSession ? productDataSession.size_name : productData.size_name;
        string colorName = isSession ? productDataSession.color.color_name : productData.color.color_name;
        float price = isSession ? TryParseFloat(productDataSession.price ): TryParseFloat(productData.price);
        float cost = isSession ? TryParseFloat(productDataSession.cost ): TryParseFloat(productData.cost);
        float variance = isSession ? productDataSession.variance : productData.variance;
        float length = isSession ? productDataSession.length_in_inches : productData.length_in_inches;
        float total = isSession ? TryParseFloat(productDataSession.total ): TryParseFloat(productData.total);
        var additionalCosts = isSession ? productDataSession.additional_cost : productData.additional_cost;

        // --- Set UI fields ---
        productNameText.text = productName;
        materialText.text = material;
        quantityText.text = $"Qty: {quantity}";
        sizeText.text = $"{sizeName} | {colorName}";

        productPricePerPieceText.text = $"${price} per piece";
        costInputfield.text = cost.ToString("F2");
        variationText.text = $"{variance}% Variation is considered";
        widthInputfield.text = length.ToString();
        placementCostInputfield.text = cost.ToString("F2");

        totalCostCount.text = $"${total:F2}";
        totalCount = total;

        // --- Handle Additional Costs ---
        int existingChildren = additionalCostContainer.childCount;

        // Add missing children if needed
        for (int i = existingChildren; i < additionalCosts.Count; i++)
        {
            Instantiate(additionalCostTextPrefab, additionalCostContainer);
        }

        // Update or reset children
        for (int i = 0; i < additionalCosts.Count; i++)
        {
            var costItem = additionalCosts[i];
            Transform child = additionalCostContainer.GetChild(i);
            child.gameObject.SetActive(true);

            var costText = child.GetComponent<TextMeshProUGUI>();
            if (costText != null) costText.text = costItem.name;

            var inputField = child.GetChild(0).GetChild(1).GetComponent<TMP_InputField>();
            if (float.TryParse(costItem.price, out var parsedPrice))
                inputField.text = parsedPrice.ToString("F2");
            else
                inputField.text = costItem.price;

            // Clear old listeners to prevent stacking
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener((value) => UpdateTotal());
        }

        // Hide extra children if there are more than needed
        for (int i = additionalCosts.Count; i < existingChildren; i++)
        {
            additionalCostContainer.GetChild(i).gameObject.SetActive(false);
        }

        // --- Recalculate Total ---
        if (isSession)
            RequestEditHandler.instance.RecalculateTotal();
        else
            SessionDetailFetcher.instance.RecalculateTotal();

    }


    public bool IsRainTankOrConcreteSplashBlocks()
    {
        if (isEditRequest)
        {
            if (productDataSession == null)
            {
                Debug.LogWarning("Product data is null.");
                return false;
            }

            string category = productDataSession.product_type; // or productData.product_category, depending on your data

            Debug.Log("IsRainTankOrConcreteSplashBlocks : " + category);
            return category == "Rain Tanks" || category == "Concrete Splash Blocks";
        }
        else
        {
            if (productData == null)
            {
                Debug.LogWarning("Product data is null.");
                return false;
            }

            string category = productData.product_type; // or productData.product_category, depending on your data

            Debug.Log("IsRainTankOrConcreteSplashBlocks : " + category);
            return category == "Rain Tanks" || category == "Concrete Splash Blocks";
        }
    }

    public void SetProductDetailsCollapsed(ProductDetail product, System.Action<ProductDetail> deleteCallback)
    {
        isEditRequest = false;

        totalCount = 0f;
        productData = product;
        onDelete = deleteCallback;

        if (product == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Basic Details
        productNameText.text = product.product_name;
        materialText.text = product.product_type; // or product.product_category
        quantityText.text = $"Qty: {product.quantity}";
        productPricePerPieceText.text = "$" + product.price + " per piece";
        costInputfield.text = product.cost.ToString();
        variationText.text = $"{product.variance}% Variation is considered";
        placementCostInputfield.text = product.cost.ToString();

        if (IsRainTankOrConcreteSplashBlocks())
        {
            //sizeText.text = $"{product.size_name} | {product.color.color_name}";
        }
        else
        {
            widthInputfield.text = product.length_in_inches.ToString();
            inchText.text = "In";
            //sizeText.text = $"{product.width} {product.width_unit} | {product.color.color_name}";
        }

        //string originalText = sizeText.text;
        //string capitalizedText = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(originalText.ToLower());
        //sizeText.text = capitalizedText;

        sizeText.text = $"{product.size_name} | {product.color.color_name}";

        // Total
        totalCostCount.text = $"${product.total:F2}";
        totalCount = TryParseFloat(product.total);

        // Load Image
        imagePath = product.product_image.location_key;
        if (gameObject.activeInHierarchy)
            StartCoroutine(LoadImageFromURL(imagePath, productImage));

        // Delete button
        //if (deleteButton != null)
        //{
        //    deleteButton.onClick.RemoveAllListeners();  // Already there—good
        //    if (onDelete != null)  // Or onDeleteSession
        //    {
        //        deleteButton.onClick.AddListener(() => onDelete?.Invoke(productData));
        //    }
        //}

        // Populate additional costs
        foreach (Transform child in additionalCostContainer)
            Destroy(child.gameObject);

        foreach (var cost in product.additional_cost)
        {
            GameObject item = Instantiate(additionalCostTextPrefab, additionalCostContainer);
            var costText = item.GetComponent<TextMeshProUGUI>();
            costText.text = cost.name;
            var inputField = item.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>();

            if (float.TryParse(cost.price, out var parsedPrice))
                inputField.text = parsedPrice.ToString("F2");
            else
                inputField.text = cost.price;
            inputField.onValueChanged.AddListener((value) => UpdateTotal()); // Add listener for dynamic update
        }

        // Add listeners to main InputFields
        costInputfield.onValueChanged.AddListener((value) => UpdateTotal());
        widthInputfield.onValueChanged.AddListener((value) => UpdateWidth());
        placementCostInputfield.onValueChanged.AddListener((value) => UpdateTotal());

        ResetDataCheck();
        ResetDataCheck();
    }

    public void SetProductDetailsCollapsedForEditRequest(SessionProductForSessionDetail product, System.Action<SessionProductForSessionDetail> deleteCallback)
    {
        isEditRequest = true;

        totalCount = 0f;
        onDeleteSession = null;

        productDataSession = product;
        onDeleteSession = deleteCallback;

        if (product == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Basic Details
        productNameText.text = product.product_name;
        materialText.text = product.product_type; // or product.product_category
        quantityText.text = $"Qty: {product.quantity}";
        productPricePerPieceText.text = "$" + product.price + " per piece";
        costInputfield.text = product.cost.ToString();
        variationText.text = $"{product.variance}% Variation is considered";
        placementCostInputfield.text = product.cost.ToString();

        if (IsRainTankOrConcreteSplashBlocks())
        {
            //sizeText.text = $"{product.size_name} | {product.color.color_name}";
        }
        else
        {
            widthInputfield.text = product.length_in_inches.ToString();
            inchText.text = "In";//product.length_unit;
            //sizeText.text = $"{product.width} {product.width_unit} | {product.color.color_name}";
        }

        //string originalText = sizeText.text;
        //string capitalizedText = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(originalText.ToLower());
        //sizeText.text = capitalizedText;

        sizeText.text = $"{product.size_name} | {product.color.color_name}";

        // Total
        totalCostCount.text = $"${product.total:F2}";
        totalCount = TryParseFloat(product.total);

        // Load Image
        imagePath = product.product_image.location_key;
        //if (gameObject.activeInHierarchy)
        StartCoroutine(LoadImageFromURL(imagePath, productImage));

        // Delete button
        //if (deleteButton != null)
        //{
        //    deleteButton.onClick.RemoveAllListeners();
        //    deleteButton.onClick.AddListener(() => onDeleteSession?.Invoke(productDataSession));
        //}

        // Populate additional costs
        foreach (Transform child in additionalCostContainer)
            Destroy(child.gameObject);

        foreach (var cost in product.additional_cost)
        {
            GameObject item = Instantiate(additionalCostTextPrefab, additionalCostContainer);
            var costText = item.GetComponent<TextMeshProUGUI>();
            costText.text = cost.name;
            var inputField = item.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>();
            if (float.TryParse(cost.price, out var parsedPrice))
                inputField.text = parsedPrice.ToString("F2");
            else
                inputField.text = cost.price;
            inputField.onValueChanged.AddListener((value) => UpdateTotal()); // Add listener for dynamic update
        }

        // Add listeners to main InputFields
        costInputfield.onValueChanged.AddListener((value) => UpdateTotal());
        widthInputfield.onValueChanged.AddListener((value) => UpdateWidth());
        placementCostInputfield.onValueChanged.AddListener((value) => UpdateTotal());

        ResetDataCheck();
        ResetDataCheck();
    }

    public void LoadImageOfSession(SessionProductForSessionDetail product)
    {
        imagePath = product.product_image.location_key;
        //if (gameObject.activeInHierarchy)
        StartCoroutine(LoadImageFromURL(imagePath, productImage));
    }

    IEnumerator LoadImageFromURL(string url, Image img)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(LoginAPIHandler.instance.imageDownloadUrl + url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);

                Rect safeRect = new Rect(0, 0, Mathf.Min(200, downloadedTexture.width), Mathf.Min(200, downloadedTexture.height));

                Sprite newSprite = Sprite.Create(downloadedTexture, safeRect, new Vector2(0.5f, 0.5f), 100.0f);

                img.sprite = newSprite;
                Debug.Log("Image Download");

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
                ErrorPopupHandler.instance.errorDescText.text = "There is an issue with image download URL.";
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }

    void Start()
    {
        if (productNameText == null || sizeText == null || materialText == null ||
             quantityText == null || costInputfield == null || variationText == null ||
             productPricePerPieceText == null)
        {
            Debug.LogWarning("One or more Text components are not assigned in ProductDetailCard.");
        }

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            Debug.LogError("LayoutElement component is missing on this GameObject!");
            return;
        }
        layoutElement.preferredHeight = collapseHeight;
        CollapseAnim();
    }

    public void ExpandAnim()
    {
        for (int i = 0; i < expandableObjects.Length; i++)
        {
            expandableObjects[i].SetActive(true);
        }

        for (int i = 0; i < collapsableObjects.Length; i++)
        {
            collapsableObjects[i].SetActive(false);
        }

        if (IsRainTankOrConcreteSplashBlocks())
        {
            widthInputfield.transform.parent.gameObject.SetActive(false);
            placementCostInputfield.transform.parent.gameObject.SetActive(true);
            costInputfield.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            widthInputfield.transform.parent.gameObject.SetActive(true);
            placementCostInputfield.transform.parent.gameObject.SetActive(false);
            costInputfield.transform.parent.gameObject.SetActive(true);
        }
    }

    public void CollapseAnim()
    {
        for (int i = 0; i < expandableObjects.Length; i++)
        {
            expandableObjects[i].SetActive(false);
        }

        for (int i = 0; i < collapsableObjects.Length; i++)
        {
            collapsableObjects[i].SetActive(true);
        }
    }

    public void ExpandFitter()
    {
        StartCoroutine(UpdateExpandFitterLayout());
    }

    public void OnExpand()
    {
        expandCollapseAnimation.Play("ExpandAnime");
        StartCoroutine(UpdateLayout());
    }

    public void OnCollapse()
    {
        expandCollapseAnimation.Play("CollapseAnime");
        StartCoroutine(UpdateLayout());
    }

    private IEnumerator UpdateLayout()
    {
        yield return null; // wait 1 frame

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
    }

    private IEnumerator UpdateExpandFitterLayout()
    {
        yield return null; // wait 1 frame

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
    }

    public void Expand()
    {
        //Debug.Log("Expand called");
        ExpandAnim();
        Canvas.ForceUpdateCanvases();
        float targetHeight = LayoutUtility.GetPreferredHeight(transform as RectTransform);
        //Debug.Log($"Target Height: {targetHeight}");

        if (targetHeight <= collapseHeight)
        {
            //Debug.LogWarning("Calculated height is less than or equal to collapse height. Check content visibility.");
            targetHeight = collapseHeight + 100f; // Fallback height
        }

        AnimateHeight(targetHeight);
        isCollapsed = false;
    }

    public void Collapse()
    {
        AnimateHeight(collapseHeight);
        CollapseAnim();
        isCollapsed = true;
    }

    private void AnimateHeight(float targetHeight)
    {
        if (layoutElement == null)
        {
            Debug.LogError("LayoutElement is null!");
            return;
        }

        currentTween?.Kill();
        //Debug.Log($"Animating from {layoutElement.preferredHeight} to {targetHeight}");
        currentTween = DOTween.To(
            () => layoutElement.preferredHeight,
            x =>
            {
                layoutElement.preferredHeight = x;
                Canvas.ForceUpdateCanvases();
            },
            targetHeight,
            duration
        )
        .SetEase(easing)
        .OnComplete(() =>
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
            //Debug.Log("Animation Completed");
        });
    }

    private bool HasSessionProduct => isEditRequest && productDataSession != null;

    private float GetVariance()
    {
        return HasSessionProduct ? productDataSession.variance : productData.variance;
    }

    private float GetPrice()
    {
        return HasSessionProduct ? TryParseFloat(productDataSession.price ): TryParseFloat(productData.price);
    }

    private float GetWidth()
    {
        return HasSessionProduct ? productDataSession.width : productData.width;
    }

    private string GetWidthUnit()
    {
        return HasSessionProduct ? productDataSession.width_unit : productData.width_unit;
    }

    public SessionProductRequest GetProductData()
    {
        int.TryParse(quantityText.text.Replace("Qty:", "").Trim(), out int quantityN);

        float cost = 0f;
        float width = 0f;

        if (IsRainTankOrConcreteSplashBlocks())
        {
            float.TryParse(placementCostInputfield.text, out cost);
        }
        else
        {
            float.TryParse(costInputfield.text, out cost);
            float.TryParse(widthInputfield.text, out width);
        }

        if (!string.IsNullOrEmpty(productData.width_unit) && productData.width_unit.ToLower() == "feet")
        {
            width = width / 12f;
        }

        List<AdditionalCostProductRequest> additionalCosts = new List<AdditionalCostProductRequest>();
        foreach (Transform child in additionalCostContainer)
        {
            var label = child.GetComponent<TextMeshProUGUI>().text;
            var input = child.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>().text;
            if (!string.IsNullOrWhiteSpace(input) && float.TryParse(input, out float parsedPrice))
            {
                additionalCosts.Add(new AdditionalCostProductRequest { name = label, price = parsedPrice });
            }
        }

        return new SessionProductRequest
        {
            id = HasSessionProduct ? productDataSession.id : productData.id,
            length = width,//HasSessionProduct ? productDataSession.length : productData.length,
            //width = HasSessionProduct ? productDataSession.width : productData.width,
            cost = cost,
            additional_cost = additionalCosts,
            quantity = quantityN,
            total = totalCount
        };
    }

    public SessionProductRequestWithSessionID GetProductDataWithSessionId()
    {
        int.TryParse(quantityText.text.Replace("Qty:", "").Trim(), out int quantityN);

        float cost = 0f;
        float width = 0f;

        if (IsRainTankOrConcreteSplashBlocks())
        {
            float.TryParse(placementCostInputfield.text, out cost);
        }
        else
        {
            float.TryParse(costInputfield.text, out cost);
            float.TryParse(widthInputfield.text, out width);
        }

        if (productDataSession.width_unit.ToLower() == "feet")
        {
            width = width / 12f;
        }

        List<AdditionalCostProductRequest> additionalCosts = new List<AdditionalCostProductRequest>();
        foreach (Transform child in additionalCostContainer)
        {
            var label = child.GetComponent<TextMeshProUGUI>().text;
            var input = child.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>().text;
            if (!string.IsNullOrWhiteSpace(input) && float.TryParse(input, out float parsedPrice))
            {
                additionalCosts.Add(new AdditionalCostProductRequest { name = label, price = parsedPrice });
            }
        }

        return new SessionProductRequestWithSessionID
        {
            session_id = HasSessionProduct ? productDataSession.session_id : productData.session_id,
            id = HasSessionProduct ? productDataSession.id : productData.id,
            length = width,//HasSessionProduct ? productDataSession.length : productData.length,
            //width = HasSessionProduct ? productDataSession.width : productData.width,
            cost = cost,
            additional_cost = additionalCosts,
            quantity = quantityN,
            total = totalCount
        };
    }

    private void UpdateWidth()
    {
        if (!HasSessionProduct && productData == null) return;

        float lengthInput = float.TryParse(widthInputfield.text, out float w) ? w : 0f;

        if (lengthInput == 0f)
        {
            Debug.LogWarning("Width is zero or invalid. Quantity calculation skipped.");
            quantityText.text = "Qty: 0";
            costInputfield.text = "0.00";
            UpdateTotal();
            return;
        }

        float width = GetWidth();

        string unit = GetWidthUnit().ToLower();
        if (unit == "feet" || unit == "ft")
        {
            width *= 12f; // convert feet → inches
        }
        else if (unit != "inch" && unit != "in")
        {
            Debug.LogWarning($"Unrecognized width unit: {unit}. Assuming inches.");
        }

        int qty = Mathf.CeilToInt(lengthInput / width);
        float cost = qty * GetPrice();

        Debug.Log("Update width : " + lengthInput + " " + width + " " + qty);

        quantityText.text = "Qty: " + qty;
        costInputfield.text = Math.Round(cost, 2).ToString();

        UpdateTotal();
    }

    private void UpdateTotal()
    {
        //if (!HasSessionProduct && productData == null) return;

        float baseCost = 0f;
        if (IsRainTankOrConcreteSplashBlocks())
        {
            baseCost = float.TryParse(placementCostInputfield.text, out float bc) ? bc : 0f;
        }
        else
        {
            baseCost = float.TryParse(costInputfield.text, out float bc) ? bc : 0f;
        }

        float additionalCosts = 0f;
        foreach (Transform child in additionalCostContainer)
        {
            var inputField = child.transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_InputField>();
            if (float.TryParse(inputField.text, out float cost))
            {
                additionalCosts += cost;
            }
        }

        float total = baseCost + additionalCosts;

        float variance = GetVariance();
        float totalWithVariance = variance > 0f ? total * (1f + variance / 100f) : total;

        totalCount = (float)Math.Round(totalWithVariance, 2);
        totalCostCount.text = $"${totalCount:F2}";

        Debug.Log($"${totalCount:F2}");


        if (isEditRequest)
        {
            RequestEditHandler.instance.RecalculateTotal();
        }
        else
        {
            SessionDetailFetcher.instance.RecalculateTotal();
        }
        //recalculate
    }


}