using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductCardDetail : MonoBehaviour
{
    public TextMeshProUGUI productNameText; // Text for product name and quantity in meters
    public TextMeshProUGUI sizeText; // Text for size (width)
    public TextMeshProUGUI materialText; // Text for material (color)
    public TextMeshProUGUI quantityText; // Text for quantity
    public TextMeshProUGUI costText; // Text for total cost and cost per unit
    public TextMeshProUGUI variationText; // Text for variation percentage
    public TextMeshProUGUI productPricePerPieceText; // Text for additional costs
    public TextMeshProUGUI productWidthText; 
    public Image productImage;
    public string imagePath;

    public Button deleteButton; // Assign in the prefab

    private SessionProductForSessionDetail productData;
    private System.Action<SessionProductForSessionDetail> onDelete;

    public Animation expandCollapseAnimation;

    public GameObject[] expandableObjects;
    public GameObject[] collapsableObjects;

    public ContentSizeFitter expandFitter;

    public bool isCollapsed = false;

    public Transform additionalCostContainer;
    public GameObject additionalCostTextPrefab;
    public GameObject totalCost;
    public TextMeshProUGUI totalCostCount;

    public void ProductCardCollapsed()
    {
        if (isCollapsed)
        {
            OnCollapse();
        }
    }

    public bool IsRainTankOrConcreteSplashBlocks()
    {
        if (productData == null)
        {
            Debug.LogWarning("Product data is null.");
            return false;
        }

        string category = productData.product_type; // or productData.product_category, depending on your data
        return category == "Rain Tanks" || category == "Concrete Splash Blocks";
    }

    public void SetProductDetailsFromAR(SessionProductForSessionDetail product, System.Action<SessionProductForSessionDetail> deleteCallback)
    {
        productData = product;
        onDelete = deleteCallback;

        if (product == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        productNameText.text = product.product_name;
        materialText.text = $"{product.product_type}";
        quantityText.text = $"Qty: {product.quantity}";
        //sizeText.text = $"{product.width} {product.length_unit} | {product.color.color_name}";
        productPricePerPieceText.text = "$" + product.price + " per piece";
        variationText.text = $"{product.variance}% Variation is considered";
        totalCostCount.text = "$" + product.total;
        //productWidthText.text = $"Width: {product.length} {product.width_unit} ";
        costText.text = $"Cost: ${product.cost}";

        if (IsRainTankOrConcreteSplashBlocks())
        {
            productWidthText.gameObject.SetActive(false);
            //sizeText.text = $"{product.size_name} | {product.color.color_name}";
        }
        else
        {
            productWidthText.text = $"Width: {product.length} {product.length_unit} ";
            //sizeText.text = $"{product.width} {product.length_unit} | {product.color.color_name}";
        }

        //string originalText = sizeText.text;
        //string capitalizedText = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(originalText.ToLower());
        //sizeText.text = capitalizedText;

        sizeText.text = $"{product.size_name} | {product.color.color_name}";


        imagePath = product.product_image.location_key;

        foreach (Transform child in additionalCostContainer)
        {
            Destroy(child.gameObject);
        }

        // ➕ ADD new additional cost entries
        if (product.additional_cost != null && product.additional_cost.Count > 0)
        {
            foreach (var cost in product.additional_cost)
            {
                GameObject costItem = Instantiate(additionalCostTextPrefab, additionalCostContainer);
                TextMeshProUGUI costName = costItem.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI costText = costItem.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (costName != null)
                {
                    costName.text = cost.name;
                }
                if (costText != null)
                {
                    costText.text = "$" + cost.price;
                }
            }
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(productData));
        }
        else
        {
            Debug.LogWarning("Delete button not assigned in ProductCardDetail.");
        }

        if (gameObject.activeInHierarchy)
            StartCoroutine(LoadImageFromURL(imagePath, productImage));
    }

    IEnumerator LoadImageFromURL(string url, Image img)
    {
        // Wait until the GameObject is active in the hierarchy
        while (!gameObject.activeInHierarchy)
        {
            yield return null;
        }

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(LoginAPIHandler.instance.imageDownloadUrl + url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);

                Rect safeRect = new Rect(0, 0, Mathf.Min(200, downloadedTexture.width), Mathf.Min(200, downloadedTexture.height));

                Sprite newSprite = Sprite.Create(downloadedTexture, safeRect, new Vector2(0.5f, 0.5f), 100.0f);

                // Sprite newSprite = Sprite.Create(
                //    downloadedTexture,
                //    new Rect(0, 0, 200, 200),
                //    new Vector2(0.5f, 0.5f)
                //);

                img.sprite = newSprite;

                Debug.Log("Load product image");
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
                Debug.Log("Not Load product image");
                ErrorPopupHandler.instance.errorDescText.text = "There is an issue with image download URL.";//"Profile photo download failed: " + request.downloadHandler.text;
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }

    // Optional: Initialize with default values or hide if no product is set
    void Start()
    {
        if (productNameText == null || sizeText == null || materialText == null ||
            quantityText == null || costText == null || variationText == null ||
            productPricePerPieceText == null)
        {
            Debug.LogWarning("One or more Text components are not assigned in ProductDetailCard.");
        }
        //gameObject.SetActive(false); // Hide by default until data is set
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
        expandFitter.SetLayoutVertical();
    }

    public void OnExpand()
    {
        expandCollapseAnimation.Play("ExpandAnime");
        transform.parent.GetComponent<ContentSizeFitter>().SetLayoutVertical();
    }

    public void OnCollapse()
    {
        expandCollapseAnimation.Play("CollapseAnime");
        transform.parent.GetComponent<ContentSizeFitter>().SetLayoutVertical();
    }
}
