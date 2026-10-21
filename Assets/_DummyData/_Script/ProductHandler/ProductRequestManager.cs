using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class ProductRequestManager : MonoBehaviour
{
    public string productRequestEndpoint = "request/updateRequestStatus/";

    public Transform productCardContainer; // Parent where product cards are instantiated
    public Transform otherCostContainer;
    public TMP_InputField notesInput;
    public TMP_InputField addressInput;

    public ClientHandler clientHandler;
    public SalesRepAPIHandler salesRepAPIHandler;

    public GameObject allRequestScreen;
    public GameObject allRequestCOScreen;
    public GameObject pricingScreen;

    [Header("Edit Request")]
    public GameObject allRequestCompanyOwnerScreen;
    public GameObject[] deactiveScreen;
    public Transform productCardContainerReq; // Parent where product cards are instantiated
    public Transform otherCostContainerReq;
    public TMP_InputField notesInputReq;
    public TMP_InputField addressInputReq;
    public Transform productCardContainerReqCO; // Parent where product cards are instantiated
    public Transform otherCostContainerReqCO;
    public TMP_InputField notesInputReqCO;
    public TMP_InputField addressInputReqCO;

    bool isEdit = false;
    bool isCompanyOwner = false;

    string jsonPayload;

    public void BuildAndSendPayloadRequestEdit()
    {
        isCompanyOwner = (PlayerPrefs.GetString("Role") == "company_owner");
        isEdit = true;

        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        RequestEditHandler.instance.RecalculateTotal();

        List<SessionProductRequestWithSessionID> sessionProducts = new List<SessionProductRequestWithSessionID>();

        Transform container = (isCompanyOwner ? productCardContainerReqCO : productCardContainerReq);

        foreach (Transform child in container)
        {
            ProductCardDetailWithInputfield card = child.GetComponent<ProductCardDetailWithInputfield>();
            if (card != null)
            {
                sessionProducts.Add(card.GetProductDataWithSessionId());
            }
        }

        List<OtherCost> otherCosts = new List<OtherCost>();

        // Pick the correct container based on role
        Transform otherCardContainer = isCompanyOwner ? otherCostContainerReqCO : otherCostContainerReq;

        // Loop through all OtherCostCard children
        foreach (OtherCostCard costCard in otherCardContainer.GetComponentsInChildren<OtherCostCard>())
        {
            if (!string.IsNullOrWhiteSpace(costCard.otherCostName.text) &&
                float.TryParse(costCard.otherCostPrice.text, out float parsedPrice))
            {
                otherCosts.Add(new OtherCost
                {
                    name = costCard.otherCostName.text,
                    price = parsedPrice
                });
            }
        }

        UpdateRequestPayloadWithSessionID payload = new UpdateRequestPayloadWithSessionID
        {
            notes = isCompanyOwner? notesInputReqCO.text : notesInputReq.text,
            address = isCompanyOwner ? addressInputReqCO.text : addressInputReq.text,
            session_product = sessionProducts,
            other_cost = otherCosts,
            final_total = RequestEditHandler.instance.grandTotal
        };

        jsonPayload = JsonUtility.ToJson(payload);
        Debug.Log("Final JSON:\n" + jsonPayload);

        // Optionally send to API here
        StartCoroutine(SendPutRequest());
    }

    public void BuildAndSendPayload()
    {
        isEdit = false;
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        SessionDetailFetcher.instance.RecalculateTotal();

        List<SessionProductRequest> sessionProducts = new List<SessionProductRequest>();

        foreach (Transform child in productCardContainer)
        {
            ProductCardDetailWithInputfield card = child.GetComponent<ProductCardDetailWithInputfield>();
            if (card != null)
            {
                sessionProducts.Add(card.GetProductData());
            }
        }

        List<OtherCost> otherCosts = new List<OtherCost>();

        foreach (OtherCostCard costCard in otherCostContainer.GetComponentsInChildren<OtherCostCard>())
        {
            if (!string.IsNullOrWhiteSpace(costCard.otherCostName.text) &&
                float.TryParse(costCard.otherCostPrice.text, out float parsedPrice))
            {
                otherCosts.Add(new OtherCost
                {
                    name = costCard.otherCostName.text,
                    price = parsedPrice
                });
            }
        }

        UpdateRequestPayload payload = new UpdateRequestPayload
        {
            notes = notesInput.text,
            address = addressInput.text,
            session_product = sessionProducts,
            other_cost = otherCosts,
            final_total = SessionDetailFetcher.instance.finalTotal
        };

        jsonPayload = JsonUtility.ToJson(payload);
        Debug.Log("Final JSON:\n" + jsonPayload);

        // Optionally send to API here
        StartCoroutine(SendPutRequest());
    }

    //public void SendProductRequest()
    //{
    //    StartCoroutine(SendPutRequest());
    //}

    IEnumerator SendPutRequest()
    {
        isCompanyOwner = (PlayerPrefs.GetString("Role") == "company_owner");
        string url = LoginAPIHandler.instance.baseURL + productRequestEndpoint + PlayerPrefs.GetInt("RequestId");

        using (UnityWebRequest request = UnityWebRequest.Put(url, jsonPayload))
        {
            byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("accept", "*/*");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Success: " + request.downloadHandler.text);
                //yield return StartCoroutine(clientHandler.GetClientList());

                yield return StartCoroutine(clientHandler.GetClientList());
                yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));

                FilterRequestHandler.instance.ClearFilters();
                FilterRequestHandler.instance.ClearFiltersSalesRep();

                //if (PlayerPrefs.GetString("Role", "") == "company_owner")
                //{
                //    Debug.Log("___is company_owner");
                //    yield return StartCoroutine(salesRepAPIHandler.FetchRepeatRequet(false));
                //}

                if (isEdit)
                {
                    //allRequestCompanyOwnerScreen.SetActive(true);
                    if (!isCompanyOwner)
                        allRequestScreen.SetActive(true);
                    else
                        allRequestCOScreen.SetActive(true);

                    for (int i = 0; i < deactiveScreen.Length; i++)
                        deactiveScreen[i].SetActive(false);
                }
                else
                {
                    if (!isCompanyOwner)
                        allRequestScreen.SetActive(true);
                    else
                        allRequestCOScreen.SetActive(true);

                    pricingScreen.SetActive(false);
                }
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            }
            else if (request.responseCode == 403)
            {
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
                Debug.LogError("Response: " + request.downloadHandler.text);
                ErrorPopupHandler.instance.errorDescText.text = "There is some issue with update request status API";//"Profile update failed: " + request.downloadHandler.text;
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
                ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
            }
        }
    }

}

[System.Serializable]
public class UpdateRequestPayloadWithSessionID
{
    public List<SessionProductRequestWithSessionID> session_product;
    public List<OtherCost> other_cost;
    public string notes;
    public string address;
    public float final_total;
}


[System.Serializable]
public class UpdateRequestPayload
{
    public List<SessionProductRequest> session_product;
    public List<OtherCost> other_cost;
    public string notes;
    public string address;
    public float final_total;
}

[System.Serializable]
public class AddProductRequest
{
    public int request_id;
    public int company_product_id;
    public int color_id;
    public float length;
    public int quantity;
    public int session_id;
}

[System.Serializable]
public class SessionProductRequest
{
    //public int request_id;
    //public int company_product_id;
    //public int color_id;
    //public float length;
    //public int qty;
    //public int session_id;
    public int id;
    public float length;
    public float width;
    public float cost;
    public int quantity;
    public List<AdditionalCostProductRequest> additional_cost;
    public float total;
}

[System.Serializable]
public class SessionProductRequestWithSessionID
{
    //public int request_id;
    //public int company_product_id;
    //public int color_id;
    //public float length;
    //public int qty;
    public int session_id;
    public int id;
    public float length;
    public float width;
    public float cost;
    public int quantity;
    public List<AdditionalCostProductRequest> additional_cost;
    public float total;
}

[System.Serializable]
public class AdditionalCostProductRequest
{
    public string name;
    public float price;
}

[System.Serializable]
public class OtherCost
{
    public string name;
    public float price;
}
