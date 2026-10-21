using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;


public class UpdateProfileAPIHandler : MonoBehaviour
{
    [Header("UI screen")]
    public GameObject settingScreen;
    public GameObject profileScreen;

    [Header("Profile")]
    public string updateProfileForSalesRepEndpoint = "sales-representative/";
    public string updateProfileForCompanyOwnerEndpoint = "company/company-profile/";

    public TMP_InputField firstNameInputfield;
    public TMP_InputField lastNameInputfield;
    public TMP_InputField emailInputfield;

    public TMP_InputField companyOwnerInputfield;
    public TMP_InputField companyNameInputfield;

    [Header("User Profilepic")]
    public string imageUploadEndpoint = "image-management/uploadMultipleFile";
    public string moduleName = "profilePic"; // or "logo"

    public string profilePicName = "";

    public GetUserDetailsAPIHandler getUserDetailsAPIHandler;
    public LoginDetailAPIHandler loginDetailAPIHandler;


    public void CheckForSalesRepOrOwner()
    {
        if (loginDetailAPIHandler.response.data.role == "company_owner")
        {
            StartCoroutine(UpdateProfileForCompanyOwner());
        }
        else if (loginDetailAPIHandler.response.data.role == "sales_representative")
        {
            StartCoroutine(UpdateProfileRequestForSalesRep());
        }
    }

    //public void UpdateSalesRepProfile()
    //{
    //    StartCoroutine(UpdateProfileRequestForSalesRep());
    //}

    private IEnumerator UpdateProfileRequestForSalesRep()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        yield return new WaitForSeconds(1f);

        string profilePicFileName = (profilePicName == "") ? getUserDetailsAPIHandler.userDetails.data.profile_picture.file_name : profilePicName;

        string jsonData;

        if (string.IsNullOrEmpty(profilePicFileName))
        {
            SalesRepProfileUpdateWithoutPic payload = new SalesRepProfileUpdateWithoutPic
            {
                first_name = firstNameInputfield.text,
                last_name = lastNameInputfield.text,
                email = emailInputfield.text
            };

            jsonData = JsonUtility.ToJson(payload);
        }
        else
        {
            SalesRepProfileUpdate payload = new SalesRepProfileUpdate
            {
                first_name = firstNameInputfield.text,
                last_name = lastNameInputfield.text,
                email = emailInputfield.text,
                profile_picture = new ProfilePictureForUpdate
                {
                    fileName = profilePicFileName,
                    type = moduleName
                }
            };

            jsonData = JsonUtility.ToJson(payload);
        }

        Debug.Log("Profile API data : " + jsonData);
        // Create UnityWebRequest with PUT
        UnityWebRequest request = UnityWebRequest.Put(LoginAPIHandler.instance.baseURL + updateProfileForSalesRepEndpoint + PlayerPrefs.GetInt("userID"), jsonData);
        request.method = UnityWebRequest.kHttpVerbPUT; // Important for Unity PUTs
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");

        // Optional: Add token
         request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();
        profilePicName = "";
        // Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {
            yield return StartCoroutine(getUserDetailsAPIHandler.GetUserDetailsProcess());
            Debug.Log("Profile update success: " + request.downloadHandler.text);

            settingScreen.SetActive(true);
            profileScreen.SetActive(false);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Profile update failed: " + request.error);
            Debug.LogError("Server response: " + request.downloadHandler.text);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with edit profile API";//"Profile update failed: " +  request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }
    //public void UpdateCompanyOwnerProfile()
    //{
    //    StartCoroutine(UpdateProfileForCompanyOwner());
    //}

    private IEnumerator UpdateProfileForCompanyOwner()
    {
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);
        yield return new WaitForSeconds(1f);

        string profilePicFileName = (profilePicName == "") ? getUserDetailsAPIHandler.userDetails.data.profile_picture.file_name : profilePicName;
        string jsonData;
        // Construct request body
        if (string.IsNullOrEmpty(profilePicFileName))
        {
            CompanyOwnerProfileUpdateWithoutPic payload = new CompanyOwnerProfileUpdateWithoutPic
            {
                name = companyNameInputfield.text,
                owner_name = companyOwnerInputfield.text,
                email = emailInputfield.text
            };

            jsonData = JsonUtility.ToJson(payload);
        }
        else
        {
            CompanyOwnerProfileUpdate payload = new CompanyOwnerProfileUpdate
            {
                name = companyNameInputfield.text,
                owner_name = companyOwnerInputfield.text,
                email = emailInputfield.text,
                profile_picture = new ProfilePictureForUpdate
                {
                    fileName = profilePicFileName,
                    type = moduleName
                }
            };

            jsonData = JsonUtility.ToJson(payload);
        }

        
        Debug.Log("Profile API data : " + jsonData);

        // Create UnityWebRequest with PUT
        UnityWebRequest request = UnityWebRequest.Put(LoginAPIHandler.instance.baseURL + updateProfileForCompanyOwnerEndpoint + PlayerPrefs.GetInt("userID"), jsonData);
        request.method = UnityWebRequest.kHttpVerbPUT; // Important for Unity PUTs
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("accept", "application/json");

        // Optional: Add token
         request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("accessToken"));

        yield return request.SendWebRequest();
        profilePicName = "";
        // Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {
            yield return StartCoroutine(getUserDetailsAPIHandler.GetUserDetailsProcess());
            Debug.Log("Profile update success: " + request.downloadHandler.text);

            settingScreen.SetActive(true);
            profileScreen.SetActive(false);
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
        else if (request.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("Profile update failed: " + request.error);
            Debug.LogError("Server response: " + request.downloadHandler.text);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with edit profile API";//"Profile update failed: " + request.downloadHandler.text;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
    }


    public void UploadImage(string imagePath)
    {
        StartCoroutine(UploadImageCoroutine(imagePath));
    }

    private IEnumerator UploadImageCoroutine(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Debug.LogError("File not found: " + imagePath);
            yield break;
        }
        ErrorPopupHandler.instance.loadingScreen.SetActive(true);

        byte[] fileData = File.ReadAllBytes(imagePath);
        WWWForm form = new WWWForm();

        // Add image
        form.AddBinaryData("files", fileData, Path.GetFileName(imagePath), "image/jpeg");

        // Add other fields
        form.AddField("moduleName", moduleName);

        UnityWebRequest www = UnityWebRequest.Post(LoginAPIHandler.instance.baseURL + imageUploadEndpoint, form);
        www.SetRequestHeader("accept", "*/*");

        yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (www.result != UnityWebRequest.Result.Success)
#else
        if (www.isNetworkError || www.isHttpError)
#endif
        {
            Debug.Log("Image Upload failed: " + www.error + www.downloadHandler.text);

            ErrorPopupHandler.instance.errorDescText.text = "There is some issue with image upload API";//"Image Upload failed: " + www.error;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.somethingWrongPopup.SetActive(true);
        }
        else if (www.responseCode == 403)
        {
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
            ErrorPopupHandler.instance.loginAgainPopup.SetActive(true);
        }
        else
        {
            ImageUploadResponse response = JsonUtility.FromJson<ImageUploadResponse>(www.downloadHandler.text);
            Debug.Log("Upload complete! Server response: " + www.downloadHandler.text + " Image name : " + response.data[0].name);
            profilePicName = response.data[0].name;
            ErrorPopupHandler.instance.loadingScreen.SetActive(false);
        }
    }
}

[System.Serializable]
public class ProfilePictureForUpdate
{
    public string fileName;
    public string type;
}

[System.Serializable]
public class SalesRepProfileUpdate
{
    public string first_name;
    public string last_name;
    public string email;
    public ProfilePictureForUpdate profile_picture;
}

[System.Serializable]
public class CompanyOwnerProfileUpdate
{
    public string name;
    public string owner_name;
    public string email;
    public ProfilePictureForUpdate profile_picture;
}

[System.Serializable]
public class CompanyOwnerProfileUpdateWithoutPic
{
    public string name;
    public string owner_name;
    public string email;
}

[System.Serializable]
public class ImageUploadItem
{
    public string name;
}

[System.Serializable]
public class ImageUploadResponse
{
    public int statusCode;
    public string message;
    public ImageUploadItem[] data;
}

[System.Serializable]
public class SalesRepProfileUpdateWithoutPic
{
    public string first_name;
    public string last_name;
    public string email;
}