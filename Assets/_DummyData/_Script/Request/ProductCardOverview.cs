using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ProductCardOverview : MonoBehaviour
{
    public TextMeshProUGUI productNameText; // Text for product name and quantity in meters
    public TextMeshProUGUI sizeText; // Text for size (width)
    public TextMeshProUGUI materialText; // Text for material (color)
    public TextMeshProUGUI quantityText; // Text for quantity
    public TextMeshProUGUI costText; // Text for total cost and cost per unit

    public Image productImage;
    public string imagePath;
    public SessionProductForSessionDetail productData;
    public GameObject dividerLine; // assign in prefab inspector

    public void SetDividerVisible(bool visible)
    {
        if (dividerLine != null)
            dividerLine.SetActive(visible);
    }

    public void SetProductDetailsFromAR(SessionProductForSessionDetail product)
    {
        productData = product;

        if (product == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        productNameText.text = product.product_name;
        materialText.text = $"{product.product_type}";
        quantityText.text = $"Qty: {product.quantity}";
        costText.text = $"${product.cost}";

        sizeText.text = $"{product.size_name} | {product.color.color_name}";

        imagePath = product.product_image.location_key;

        //if (gameObject.activeInHierarchy)
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

            Debug.Log("Image Download issue : " + request.downloadHandler.text + "\n" + request.result + "\n" + request.responseCode);
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);

                Rect safeRect = new Rect(0, 0, Mathf.Min(200, downloadedTexture.width), Mathf.Min(200, downloadedTexture.height));

                Sprite newSprite = Sprite.Create(downloadedTexture, safeRect, new Vector2(0.5f, 0.5f), 100.0f);

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
}
