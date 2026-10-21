using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;

public class CreateAccountManager : MonoBehaviour
{
    [Header("Create AccountInput Fields")]
    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField companyNameInput;
    public TMP_InputField mobileInput;
    public GameObject mobileError;
    public TMP_InputField emailInput;
    public GameObject emailError;
    public TMP_InputField passwordInput;

    [Header("Password Visibility")]
    public Image eyeIcon;
    public Sprite eyeOpenIcon;   // 👁️ Visible icon
    public Sprite eyeClosedIcon; // 🔒 Hidden icon
    private bool isPasswordVisible = false;

    [Header("Other UI Elements")]
    public Toggle termsToggle;
    public Button nextButton;
    public GameObject successPopup;
    public GameObject errorPopup;

    [Header("Forgot Password")]
    public TMP_InputField FP_emailInput;
    public GameObject FP_emailError;
    public Button FP_nextButton;
    public GameObject FP_successPopup;

    [Header("Screen")]
    public GameObject loginScreen;
    public GameObject loadingScreen;


    private void Start()
    {
        // Add listeners to check inputs on change
        firstNameInput.onValueChanged.AddListener(delegate { CheckFormComplete(); });
        lastNameInput.onValueChanged.AddListener(delegate { CheckFormComplete(); });
        companyNameInput.onValueChanged.AddListener(delegate { CheckFormComplete(); });
        mobileInput.onValueChanged.AddListener(delegate { CheckFormComplete(); });
        emailInput.onValueChanged.AddListener(delegate { CheckFormComplete(); });
        passwordInput.onValueChanged.AddListener(delegate { CheckFormComplete(); });
        termsToggle.onValueChanged.AddListener(delegate { CheckFormComplete(); });

        FP_emailInput.onValueChanged.AddListener(delegate { CheckEmailForFP(); });

        CheckFormComplete(); // Initial check
    }

    #region CreateAccount

    private void CheckFormComplete()
    {
        bool allFilled =
            !string.IsNullOrEmpty(firstNameInput.text.Trim()) &&
            !string.IsNullOrEmpty(lastNameInput.text.Trim()) &&
            !string.IsNullOrEmpty(companyNameInput.text.Trim()) &&
            !string.IsNullOrEmpty(mobileInput.text.Trim()) &&
            !string.IsNullOrEmpty(emailInput.text.Trim()) &&
            !string.IsNullOrEmpty(passwordInput.text.Trim()) &&
            termsToggle.isOn;

        nextButton.interactable = allFilled;
    }

    public void OnNextButtonClicked()
    {
        StartCoroutine(HandleCreateAccount());
    }

    public void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;
        SetPasswordVisibility(isPasswordVisible);
    }

    private void SetPasswordVisibility(bool visible)
    {
        passwordInput.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
        passwordInput.ForceLabelUpdate();

        if (eyeIcon != null)
            eyeIcon.sprite = visible ? eyeOpenIcon : eyeClosedIcon;
    }

    private IEnumerator HandleCreateAccount()
    {
        string email = emailInput.text.Trim();
        string mobile = mobileInput.text.Trim();

        // Reset all errors
        emailError.SetActive(false);
        mobileError.SetActive(false);

        bool hasError = false;

        if (!IsValidEmail(email))
        {
            emailError.SetActive(true);
            hasError = true;
        }

        if (!IsValidMobile(mobile))
        {
            mobileError.SetActive(true);
            hasError = true;
        }

        // Stop coroutine only if any validation failed
        if (hasError)
        {
            yield break;
        }
        // Show loading screen
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Simulate API delay
        yield return new WaitForSeconds(1f);

        List<UserData> users = UserDatabase.LoadUsers();

        // Check if email already exists
        if (users.Exists(u => u.email == email))
        {
            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            ShowError("User with this email already exists!");
            yield break;
        }

        // Add new user
        UserData newUser = new UserData
        {
            firstName = firstNameInput.text.Trim(),
            lastName = lastNameInput.text.Trim(),
            companyName = companyNameInput.text.Trim(),
            mobile = mobile,
            email = email,
            password = passwordInput.text.Trim()
        };

        users.Add(newUser);
        UserDatabase.SaveUsers(users);

        // Simulate slight delay for smooth UX
        yield return new WaitForSeconds(0.3f);

        // Hide loading and show success
        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        ShowSuccess("Account created successfully!");
    }

    private bool IsValidEmail(string email)
    {
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    private bool IsValidMobile(string mobile)
    {
        return Regex.IsMatch(mobile, @"^[0-9]{10}$");
    }

    private void ShowSuccess(string message)
    {
        if (successPopup != null)
        {
            successPopup.SetActive(true);
            successPopup.GetComponent<PopupFade>().ShowPopup();
        }
        Debug.Log(message);
        // Optionally set a Text component inside popup
    }

    public void Login_OnAccountCreated()
    {
        successPopup.GetComponent<PopupFade>().HidePopup(() =>
        {
            Debug.Log("Hide AccountCreate popup!");
            //successPopup.SetActive(false);

            NavigationManager.Instance.OpenScreen(loginScreen, () =>
            {
                Reset_CreateAccount();
            });
        });
    }

    private void ShowError(string message)
    {
        if (errorPopup != null)
        {
            errorPopup.SetActive(true);
            errorPopup.GetComponent<PopupFade>().ShowPopup();
        }
        Debug.Log(message);
        // Optionally set a Text component inside popup
    }

    public void Reset_CreateAccount()
    {
        firstNameInput.text = "";
        lastNameInput.text = "";
        companyNameInput.text = "";
        mobileInput.text = "";
        emailInput.text = "";
        passwordInput.text = "";
        termsToggle.isOn = false;
    }

    #endregion

    #region ForgotPassword

    private void CheckEmailForFP()
    {
        bool allFilled =
          !string.IsNullOrEmpty(FP_emailInput.text.Trim());

        FP_nextButton.interactable = allFilled;
    }

    public void OnResetButtonClicked()
    {
        StartCoroutine(HandleForgotPassword());
    }

    private IEnumerator HandleForgotPassword()
    {
        emailError.SetActive(false);
        FP_successPopup.SetActive(false);

        string email = FP_emailInput.text.Trim();

        if (!IsValidEmail(email))
        {
            FP_emailError.SetActive(true);
            yield break;
        }

        // Show loading
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        yield return new WaitForSeconds(1f); // Simulate API delay

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        FP_successPopup.SetActive(true);
        FP_successPopup.GetComponent<PopupFade>().ShowPopup();
        Debug.Log("Password reset successful for " + email);
    }

    public void Login_OnForgotPassword()
    {
        FP_successPopup.GetComponent<PopupFade>().HidePopup(() =>
        {
            NavigationManager.Instance.OpenScreen(loginScreen, () =>
            {
                FP_emailInput.text = "";
            });
        });
    }

    #endregion
}
