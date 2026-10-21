using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
//using Gpm.WebView;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CMSDataAPIHandler : MonoBehaviour
{
    public string CMSEndpoint = "admin/getCmsDetails";

    public TextMeshProUGUI privacyPolicyText; // Assign in inspector
    public string cmsTypePrivacyPolicy = "privacy_policy";

    public TextMeshProUGUI termsAndConditionText;
    public string cmsTypeTermsAndCondition = "terms_and_condition";


    public string privacyPolicyWebURL;
    public string termsNConditionWebURL;

    public RectTransform webViewFrame;

    //public SampleWebView sampleWebView;

    private void Start()
    {
        //GetPrivacyPolicyData();
        //GetTermsAndConditionData();
    }

    public void GetPrivacyPolicyData()
    {
        //ShowUrlPopupPositionSize(privacyPolicyWebURL);
    }

    // Popup custom position and size
//    public void ShowUrlPopupPositionSize(string url)
//    {
//        //ApplySafeAreaTopLeftRightOnly(webViewFrame);

//        Vector3[] worldCorners = new Vector3[4];
//        webViewFrame.GetWorldCorners(worldCorners);

//        // Bottom-left and top-right corners
//        Vector3 bottomLeft = worldCorners[0];
//        Vector3 topRight = worldCorners[2];

//        // Convert to screen space (if Canvas is in World Space, adjust accordingly)
//        Vector2 screenBottomLeft = RectTransformUtility.WorldToScreenPoint(null, bottomLeft);
//        Vector2 screenTopRight = RectTransformUtility.WorldToScreenPoint(null, topRight);

//        int x = (int)screenBottomLeft.x;
//        int y = (int)(Screen.height - screenTopRight.y); // Invert Y for GPM (origin top-left)

//        int width = (int)(screenTopRight.x - screenBottomLeft.x);
//        int height = (int)(screenTopRight.y - screenBottomLeft.y);

//        GpmWebView.ShowUrl(
//         url,
//         new GpmWebViewRequest.Configuration()
//         {
//             style = GpmWebViewStyle.POPUP,
//             orientation = GpmOrientation.PORTRAIT,
//             isClearCookie = true,
//             isClearCache = true,
//             isNavigationBarVisible = false,
//             isCloseButtonVisible = false,
//             position = new GpmWebViewRequest.Position
//             {
//                 hasValue = true,
//                 x = x,
//                 y = y
//             },
//             size = new GpmWebViewRequest.Size
//             {
//                 hasValue = true,
//                 width = width,
//                 height = height
//             },
//             supportMultipleWindows = true,
//#if UNITY_IOS
//            contentMode = GpmWebViewContentMode.MOBILE,
//            isMaskViewVisible = false,
//#endif
//        }, null, null);
//    }


//    private void OnCallback(
//    GpmWebViewCallback.CallbackType callbackType,
//    string data,
//    GpmWebViewError error)
//    {
//        Debug.Log("OnCallback: " + callbackType);
//        switch (callbackType)
//        {
//            case GpmWebViewCallback.CallbackType.Open:
//                if (error != null)
//                {
//                    Debug.LogFormat("Fail to open WebView. Error:{0}", error);
//                }
//                break;
//            case GpmWebViewCallback.CallbackType.Close:
//                if (error != null)
//                {
//                    Debug.LogFormat("Fail to close WebView. Error:{0}", error);
//                }
//                break;
//            case GpmWebViewCallback.CallbackType.PageStarted:
//                if (string.IsNullOrEmpty(data) == false)
//                {
//                    Debug.LogFormat("PageStarted Url : {0}", data);
//                }
//                break;
//            case GpmWebViewCallback.CallbackType.PageLoad:
//                if (string.IsNullOrEmpty(data) == false)
//                {
//                    Debug.LogFormat("Loaded Page:{0}", data);
//                }
//                break;
//            case GpmWebViewCallback.CallbackType.MultiWindowOpen:
//                Debug.Log("MultiWindowOpen");
//                break;
//            case GpmWebViewCallback.CallbackType.MultiWindowClose:
//                Debug.Log("MultiWindowClose");
//                break;
//            case GpmWebViewCallback.CallbackType.Scheme:
//                if (error == null)
//                {
//                    if (data.Equals("USER_ CUSTOM_SCHEME") == true || data.Contains("CUSTOM_SCHEME") == true)
//                    {
//                        Debug.Log(string.Format("scheme:{0}", data));
//                    }
//                }
//                else
//                {
//                    Debug.Log(string.Format("Fail to custom scheme. Error:{0}", error));
//                }
//                break;
//            case GpmWebViewCallback.CallbackType.GoBack:
//                Debug.Log("GoBack");
//                break;
//            case GpmWebViewCallback.CallbackType.GoForward:
//                Debug.Log("GoForward");
//                break;
//            case GpmWebViewCallback.CallbackType.ExecuteJavascript:
//                Debug.LogFormat("ExecuteJavascript data : {0}, error : {1}", data, error);
//                break;
//#if UNITY_ANDROID
//        case GpmWebViewCallback.CallbackType.BackButtonClose:
//            Debug.Log("BackButtonClose");
//            break;
//#endif
//        }
//    }

    public void Close()
    {
        //GpmWebView.Close();
    }

    public void ApplySafeAreaTopLeftRightOnly(RectTransform target)
    {
        Rect safe = Screen.safeArea;

        // Calculate screen insets
        float bottomInset = Screen.height - (safe.y + safe.height);
        float topInset = safe.y;
        float leftInset = safe.x;
        float rightInset = Screen.width - (safe.x + safe.width);

        // Apply safe area padding (ignore bottom)
        target.anchorMin = new Vector2(0, 0);
        target.anchorMax = new Vector2(1, 1);
        target.offsetMin = new Vector2(leftInset, 0); // bottom = 0 → ignore bottom inset
        target.offsetMax = new Vector2(-rightInset, -topInset);
    }

    private IEnumerator GetCMSContent_PrivacyPolicy()
    {
        string fullUrl = LoginAPIHandler.instance.baseURL + CMSEndpoint + "?type=" + cmsTypePrivacyPolicy; ;
        UnityWebRequest request = UnityWebRequest.Get(fullUrl);
        request.SetRequestHeader("accept", "*/*");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("CMS fetched: " + request.downloadHandler.text);

            CMSResponse response = JsonUtility.FromJson<CMSResponse>(request.downloadHandler.text);

            // Optional: Convert HTML to plain text (basic)
            string richText = ConvertHtmlToPlainText(response.data.content);
            privacyPolicyText.text = richText;
        }
        else
        {
            Debug.LogError("Failed to fetch CMS: " + request.error);
        }
    }

    public void GetTermsAndConditionData()
    {
        //StartCoroutine(GetCMSContent_TermsAndCondition());

        //ShowUrlPopupPositionSize(termsNConditionWebURL);
    }

    private IEnumerator GetCMSContent_TermsAndCondition()
    {
        string fullUrl = LoginAPIHandler.instance.baseURL + CMSEndpoint + "?type=" + cmsTypeTermsAndCondition; ;
        UnityWebRequest request = UnityWebRequest.Get(fullUrl);
        request.SetRequestHeader("accept", "*/*");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("CMS fetched: " + request.downloadHandler.text);

            CMSResponse response = JsonUtility.FromJson<CMSResponse>(request.downloadHandler.text);

            // Optional: Convert HTML to plain text (basic)
            string richText = ConvertHtmlToPlainText(response.data.content);
            termsAndConditionText.text = richText;
        }
        else
        {
            Debug.LogError("Failed to fetch CMS: " + request.error);
        }
    }

    string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        // Replace common HTML tags with newlines or spacing
        string result = html;
        result = result.Replace("<br>", "\n").Replace("<br/>", "\n");
        result = result.Replace("</p>", "\n\n");

        // Remove all remaining tags
        result = Regex.Replace(result, "<.*?>", string.Empty);

        // Replace common HTML entities
        result = result.Replace("&nbsp;", " ").Replace("&amp;", "&");

        return result.Trim();
    }

}

[System.Serializable]
public class CMSData
{
    public int id;
    public string content;
    public string heading;
    public string created_at;
    public string updated_at;
}

[System.Serializable]
public class CMSResponse
{
    public CMSData data;
    public int statusCode;
    public string message;
}