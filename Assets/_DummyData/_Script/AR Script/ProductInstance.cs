using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ProductInstance : MonoBehaviour
{
    public ProductSelectionData productData;
    public int size;

    public List<Screenshot> screenshotList;
    public SessionProductForCreateSession sessionProductData;

    // Set the ProductData for this instance
    public void SetProductData(ProductSelectionData data)
    {
        productData = data;
    }

    // Get the ProductData for this instance
    public ProductSelectionData GetProductData()
    {
        return productData;
    }

    public IEnumerator SendProductDataForCreateSession()
    {
        if (productData == null || productData.Variant == null)
        {
            Debug.LogError("ProductSelectionData or Variant is null in ProductInstance.");
            yield break;
        }

        // Use the first color as a default; this should be updated with UI selection if available
        ColorData selectedColor = productData.Variant.color != null && productData.Variant.color.Count > 0
            ? productData.Variant.color[0]
            : null;

        float parsedWidth = 0f;
        float.TryParse(productData.Variant.width, out parsedWidth);

        // Clean and parse the price string
        int parsedCost = 0;
        if (!string.IsNullOrEmpty(productData.Variant.price))
        {
            string cleanPrice = Regex.Replace(productData.Variant.price, "[^0-9.]", "");
            if (float.TryParse(cleanPrice, out float costFloat))
            {
                parsedCost = Mathf.RoundToInt(costFloat);
            }
            else
            {
                Debug.LogWarning($"Failed to parse price: {productData.Variant.price}, using default value 0");
            }
        }
        else
        {
            Debug.LogWarning("Price is null or empty, using default value 0");
        }

        // Initialize sessionProductData
        sessionProductData = new SessionProductForCreateSession
        {
            product_name = productData.name,
            length = size,
            length_unit = productData.Variant.length_unit ?? "inch",
            width = parsedWidth,
            width_unit = productData.Variant.width_unit,
            //color = selectedColor,
            color_id = selectedColor.id,
            quantity = 1,
            cost = parsedCost,
            screenshot = screenshotList,
            company_product_id = productData.Variant.company_product_id
        };

        // Optional: Simulate or handle asynchronous work (e.g., image upload)
        // yield return new WaitForSeconds(1); // Example delay
        yield break; // End the coroutine
    }
}