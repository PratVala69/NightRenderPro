using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ProductFetcher : MonoBehaviour
{
    public string productFetchEndpoint = "request/productCategoryList";

    public ProductCategoryResponse productCategoryResponse;
    public string categoryJson;

    public static ProductFetcher instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void StartFetchingProductsForAR()
    {
        StartCoroutine(FetchProductCategories());
    }

    public IEnumerator FetchProductCategories()
    {
        string url = LoginAPIHandler.instance.baseURL + productFetchEndpoint;
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
            Debug.Log("✅ Product Categories fetched.");
            productCategoryResponse = JsonUtility.FromJson<ProductCategoryResponse>(FixJson(request.downloadHandler.text));
            categoryJson = request.downloadHandler.text;

            // Example: Log first product
            if (productCategoryResponse.data.measurement_list.Count > 0)
            {
                var product = productCategoryResponse.data.measurement_list[0].products[0];
                Debug.Log("Product Name: " + product.product_name);
                Debug.Log("Price: $" + product.product_price);
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
            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with get product category API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }

    // Fix nested array parsing (Unity requires the JSON to have a top-level object for nested lists)
    private string FixJson(string value)
    {
        // JsonUtility can't handle root-level arrays well. No fix needed here because our root is an object.
        return value;
    }
}

[System.Serializable]
public class ProductVariant
{
    public int id;
    public int company_product_id;
    public int product_id;
    public string image;
    public string fbx_file;
    public List<ColorData> color;
    public string price;
    public float variance;
    public string size_name;
    public string length;
    public string length_unit;
    public string width;
    public string width_unit;
}

[System.Serializable]
public class Product
{
    public string product_name;
    public string product_price;
    public string product_image;
    public List<ProductVariant> product_variants;
}

[System.Serializable]
public class AdditionalCost
{
    public int id;
    public string name;
    public string description;
    public string price;
}

[System.Serializable]
public class CategoryGroup
{
    public string product_category;
    public List<AdditionalCost> additional_cost;
    public List<Product> products;
}

[System.Serializable]
public class ProductCategoryData
{
    public List<CategoryGroup> measurement_list;
    public List<CategoryGroup> placement_list;
}

[System.Serializable]
public class ProductCategoryResponse
{
    public ProductCategoryData data;
    public int statusCode;
    public string message;
}
