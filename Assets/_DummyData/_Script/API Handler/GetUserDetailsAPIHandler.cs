using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class GetUserDetailsAPIHandler : MonoBehaviour
{
    private string getUserDetailsEndpoint = "auth/getUserDetails";

    [Header("My Profile")]
    public TMP_InputField firstName;
    public TMP_InputField lastName;
    public TMP_InputField email;
    public TMP_InputField phoneNumber;
    public Image targetImage;

    public TMP_InputField companyOwnerInputfield;
    public TMP_InputField companyNameInputfield;

    [Header("Setting screen")]
    public TextMeshProUGUI firstlastName;
    public TextMeshProUGUI emailForSetting;
    public TextMeshProUGUI phoneNumberForSetting;
    public Image targetImageForSetting;

    public TextMeshProUGUI companyOwnerForSetting;

    [Header("User Details")]
    public UserDetailsResponse userDetails;
    public LoginDetailAPIHandler loginDetailAPIHandler;

    public Sprite userSprite;


    //public void GetUserDetails()
    //{
    //    StartCoroutine(GetUserDetailsProcess());
    //}

    public void MyProfileSetup()
    {
        if (loginDetailAPIHandler.response.data.role == "company_owner")
        {
            companyOwnerInputfield.gameObject.SetActive(true);
            companyNameInputfield.gameObject.SetActive(true);

            firstName.gameObject.SetActive(false);
            lastName.gameObject.SetActive(false);

            companyOwnerForSetting.gameObject.SetActive(true);
            firstlastName.gameObject.SetActive(false);

            PlayerPrefs.SetInt("CompanyOwnerId", loginDetailAPIHandler.response.data.id); //"company_owner", "sales_representative"
            PlayerPrefs.Save();
        }
        else if (loginDetailAPIHandler.response.data.role == "sales_representative")
        {
            firstName.gameObject.SetActive(true);
            lastName.gameObject.SetActive(true);

            companyOwnerInputfield.gameObject.SetActive(false);
            companyNameInputfield.gameObject.SetActive(false);

            companyOwnerForSetting.gameObject.SetActive(false);
            firstlastName.gameObject.SetActive(true);
        }
    }

    public  IEnumerator GetUserDetailsProcess()
    {
        UnityWebRequest request = UnityWebRequest.Get(LoginAPIHandler.instance.baseURL + getUserDetailsEndpoint);
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));
        request.SetRequestHeader("accept", "*/*");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogError("Get User Details Error: " + request.error);

            ErrorPopupHandler.instance.errorDescText.text = "There is an issue with GetUserDetails API.";//"Get User Details Response : " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);

            userDetails = JsonUtility.FromJson<UserDetailsResponse>(request.downloadHandler.text);
            Debug.Log("ID : " + userDetails.data.id);
            firstName.text = userDetails.data.first_name;
            lastName.text = userDetails.data.last_name;

            companyNameInputfield.text = userDetails.data.company_name;
            companyOwnerInputfield.text = userDetails.data.owner_name;

            firstlastName.text = userDetails.data.first_name+" "+ userDetails.data.last_name;
            companyOwnerForSetting.text = userDetails.data.owner_name;

            email.text = userDetails.data.email;
            emailForSetting.text = userDetails.data.email;

            string rawNumber = userDetails.data.phone_number; // e.g., "1234567890"

            if (rawNumber.Length == 10)
            {
                string formatted = string.Format("{0} {1}-{2}",
                    rawNumber.Substring(0, 3),
                    rawNumber.Substring(3, 3),
                    rawNumber.Substring(6, 4));

                phoneNumber.text = formatted; // Output: "123 456-7890"
                phoneNumberForSetting.text = "+1 " + formatted;
            }
            else
            {
                phoneNumber.text = rawNumber; // fallback for unexpected lengths
                phoneNumberForSetting.text = "+1 " + rawNumber; // fallback for unexpected lengths
            }

            MyProfileSetup();
            yield return StartCoroutine(DownloadImage());
        }
    }

    public void ProfilePicImageDownload()
    {
        StartCoroutine(DownloadImage());
    }

    IEnumerator DownloadImage()
    {
        if (userDetails.data.profile_picture == null || userDetails.data.profile_picture.location_key == null)
        {
            Debug.Log("Profile is null");
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);

            targetImage.sprite = userSprite;
            targetImageForSetting.sprite = userSprite;
            yield break;
        }

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(LoginAPIHandler.instance.imageDownloadUrl + userDetails.data.profile_picture.location_key);
        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogError("Profile photo download failed: " + request.downloadHandler.text + LoginAPIHandler.instance.imageDownloadUrl + userDetails.data.profile_picture.location_key);
            ErrorPopupHandler.instance.errorDescText.text = "There is an issue with image download URL.";//"Profile photo download failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
        else
        {
            Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);


            int size = Mathf.Min(downloadedTexture.width, downloadedTexture.height);

            int x = (downloadedTexture.width - size) / 2;   // Center horizontally
            int y = (downloadedTexture.height - size) / 2;  // Center vertically

            Rect cropRect = new Rect(x, y, size, size);

            Sprite newSprite = Sprite.Create(
                downloadedTexture,
                cropRect,
                new Vector2(0.5f, 0.5f)
            );

            targetImage.sprite = newSprite;
            targetImageForSetting.sprite = newSprite;

            // Wait for one frame to ensure UI has updated
            yield return null;

            // Optionally, wait until the image component has a valid sprite
            while (targetImage.sprite == null || targetImageForSetting.sprite == null)
            {
                yield return null;
            }

            if (OTPVerifier.firebaseAuth == false)
                ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
    }
}

[System.Serializable]
public class UserDetailsResponse
{
    public UserDetails data;
    public int statusCode;
    public string message;
}

[System.Serializable]
public class UserDetails
{
    public int id;
    public string first_name;
    public string last_name;
    public string company_name;
    public string email;
    public string owner_name;
    public string phone_number;
    public ProfilePicture profile_picture;
    public string name;
}
