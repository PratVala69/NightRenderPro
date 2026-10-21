using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class LoginHandler : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject loginScreen;
    public GameObject errorPopupScreen;
    public GameObject otpScreen;

    public GameObject homeScreen;
    public GameObject loadingScreen;
    public GameObject newRequestSessionListScreen;

    [Header("TMP field")]
    public TMP_InputField loginInputField;
    public TMP_Dropdown prefixDropdown;
    public TMP_InputField[] otpInputFields;
    public TextMeshProUGUI resendOTPTimer;

    [Header("Login Button")]
    public Button loginButton;
    public Button otpVerifyButton;

    [Header("LoginAPI")]
    public LoginAPIHandler loginAPIHandler;
    public OTPVerifier otpVerifier;
    public LoginDetailAPIHandler loginDetailAPIHandler;

    string digitsOnly;

    public static bool isFirstTime = true;

    private string[] previousTexts;

    private void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        loginAPIHandler = LoginAPIHandler.instance;

        Debug.Log("Login Handler start");

        if (isFirstTime)
        {
            if (!PlayerPrefs.HasKey("isLoggedIn"))
            {
                loginScreen.SetActive(true);
                homeScreen.SetActive(false);
            }
            else if (PlayerPrefs.GetInt("isLoggedIn") == 0)
            {
                loginScreen.SetActive(true);
                homeScreen.SetActive(false);
            }
            else
            {
                homeScreen.SetActive(true);
                loginScreen.SetActive(false);

                ErrorPopupHandler.instance.loadingScreen.SetActive(true);

                loginDetailAPIHandler.OnLoginButtonClick();
            }

            loginInputField.onValueChanged.AddListener(OnLoginInputChanged);
            isFirstTime = false;
        }
        else if (SceneTransition.isBackFromAR)
        {
            StartCoroutine(backState());
        }

        previousTexts = new string[otpInputFields.Length];
        for (int i = 0; i < otpInputFields.Length; i++)
        {
            int index = i;
            previousTexts[i] = "";
            otpInputFields[i].onValueChanged.AddListener(delegate { OnValueChanged(otpInputFields[index], index); });
        }

        // Select first input field on start
        if (otpInputFields.Length > 0)
        {
            otpInputFields[0].Select();
        }

        UpdateOTPVerifyButtonState();
    }

    IEnumerator backState()
    {
        loginScreen.SetActive(false);
        //loginDetailAPIHandler.OnLoginButtonClick();
        newRequestSessionListScreen.SetActive(true);
        yield return StartCoroutine(SessionDetailFetcher.instance.GetSessionDetails());
        yield return new WaitForSeconds(1f);
        loginDetailAPIHandler.OnLoginButtonClick();
    }

    public void CheckBackClick()
    {
        if (SceneTransition.isBackFromAR)
        {
            homeScreen.SetActive(true);
        }
    }

    public void LoginButtonClick()
    {
        digitsOnly = "";
        if (loginInputField.text.Length >= 10) // Check if the input has exactly 10 characters
        {
            // Extract only digits from the input field text
          
            foreach (char c in loginInputField.text)
            {
                if (char.IsDigit(c))
                    digitsOnly += c;
            }
           
            loadingScreen.SetActive(true);

            // Call the API with digits-only string
            StartCoroutine(loginAPIHandler.CheckPhoneNumberResposeData(digitsOnly, OnPhoneNumberCheckComplete));
            int index = prefixDropdown.value;
            string selectedText = prefixDropdown.options[index].text;
            otpVerifier.strPhoneNumber = selectedText + loginInputField.text;
        }
        else
        {
            errorPopupScreen.SetActive(true); // Show error popup
        }
    }

    public void OnLoginInputChanged(string input)
    {
        string snum = System.Text.RegularExpressions.Regex.Replace(input, @"[^0-9]", string.Empty) ?? string.Empty;
        if (snum.Length > 10) snum = snum.Substring(0, 10);

        if (snum.Length <= 3)
        {
            // Just first block
        }
        else if (snum.Length <= 6)
        {
            // Add space after 3 digits
            snum = $"{snum.Substring(0, 3)} {snum.Substring(3)}";
        }
        else
        {
            // Add space and dash: 123 456-7890
            snum = $"{snum.Substring(0, 3)} {snum.Substring(3, 3)}-{snum.Substring(6)}";
        }

        loginInputField.text = snum;

        if (snum.Length != input.Length)
        {
            loginInputField.ForceLabelUpdate();
            loginInputField.MoveTextEnd(false);
        }

        loginButton.interactable = snum.Length == 12;
    }


    private void OnPhoneNumberCheckComplete(bool success, string message)
    {
        if (success)
        {
            Debug.Log("Phone number verified: " + message);
            otpVerifier.PhoneAuthFirebase();

            Debug.Log("Digits only " + digitsOnly);
            PlayerPrefs.SetString("phoneNumber", digitsOnly);
            PlayerPrefs.Save();

            loginDetailAPIHandler.OnLoginButtonClick();
        }
        else
        {
            errorPopupScreen.SetActive(true); // Show error popup
            loadingScreen.SetActive(false);
            Debug.Log("Phone number check failed: " + message);
        }
    }

    public void OTPConfirmation()
    {
        Debug.Log("OTP Verified Successfully!");
        // Proceed to next screen or perform further actions
        OTPVerifier.firebaseAuth = false;

        homeScreen.SetActive(true);
        otpScreen.SetActive(false);
        ClearOTPInputs();

        PlayerPrefs.SetInt("isLoggedIn", 1);
        PlayerPrefs.Save();

        loadingScreen.SetActive(false);
        ClearLoginInput();
    }

    

    public string GetEnteredOTP()
    {
        string otp = "";
        foreach (TMP_InputField field in otpInputFields)
        {
            otp += field.text; // Concatenate values from each input field
        }
        return otp;
    }

    public void AssignOTP()
    {
        otpVerifier.strOtp = GetEnteredOTP();
        Debug.Log(GetEnteredOTP());
    }

    private void OnValueChanged(TMP_InputField currentField, int currentIndex)
    {
        // Check if backspace was pressed (text length decreased)
        if (currentField.text.Length < previousTexts[currentIndex].Length && currentIndex > 0)
        {
            // Select previous field if current field is empty
            // if (currentField.text.Length == 0)
            {
                TMP_InputField previousField = otpInputFields[currentIndex - 1];
                previousField.Select();
                previousField.ActivateInputField();
            }
        }
        // Move to next field if a character is entered
        else if (currentField.text.Length == 1 && currentIndex < otpInputFields.Length - 1)
        {
            TMP_InputField nextField = otpInputFields[currentIndex + 1];
            nextField.Select();
            nextField.ActivateInputField();
        }

        // Update previous text
        previousTexts[currentIndex] = currentField.text;
        UpdateOTPVerifyButtonState();
    }

    //private void Update()
    //{
    //    for (int i = 0; i < otpInputFields.Length; i++)
    //    {
    //        if (otpInputFields[i].isFocused && Input.GetKeyDown(KeyCode.Backspace))
    //        {
    //            Debug.Log("Backspace pressed in TMP_InputField");
    //            TMP_InputField previousField = otpInputFields[i - 1];
    //            previousField.Select();
    //            previousField.ActivateInputField();
    //        }
    //    }
    //}

    private void UpdateOTPVerifyButtonState()
    {
        bool allFieldsFilled = true;
        foreach (var field in otpInputFields)
        {
            if (field.text.Length != 1 || !char.IsDigit(field.text[0]))
            {
                allFieldsFilled = false;
                break;
            }
        }

        otpVerifyButton.interactable = allFieldsFilled;
    }

    public void ClearLoginInput()
    {
        loginInputField.text = ""; // Clear login input field
        prefixDropdown.value = 0;
    }

    public void ClearOTPInputs()
    {
        foreach (TMP_InputField field in otpInputFields)
        {
            field.text = ""; // Clear each OTP input field
        }
        otpInputFields[0].ActivateInputField();

        UpdateOTPVerifyButtonState();
    }

}
