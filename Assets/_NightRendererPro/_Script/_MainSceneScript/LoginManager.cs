using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("Login UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public GameObject emailError;

    [Header("Password Visibility")]
    public Image eyeIcon;
    public Sprite eyeOpenIcon;   // 👁️ Visible icon
    public Sprite eyeClosedIcon; // 🔒 Hidden icon
    private bool isPasswordVisible = false;

    [Header("Popups")]
    public GameObject errorPopup;

    [Header("Loading Screen")]
    public GameObject loadingScreen;  // A panel with spinner or “Please wait” text

    [Header("Home Screen")]
    public GameObject HomeScreen;
    public GameObject LoginScreen;

    [Header("Logout Screen")]
    public GameObject logoutPopup;

    [Header("User Name and Email")]
    public TextMeshProUGUI[] userName;
    public TextMeshProUGUI[] userEmail;

    [Header("Script Reference")]
    public ClientManager clientManager;

    #region Login

    private void Start()
    {
        loginButton.interactable = false;

        // Add text change listeners for validation
        emailInput.onValueChanged.AddListener(delegate { ValidateInput(); });
        passwordInput.onValueChanged.AddListener(delegate { ValidateInput(); });

        SetPasswordVisibility(false);

        // ✅ Check if user is already logged in
        CheckAutoLogin();
    }

    private void ValidateInput()
    {
        bool hasEmail = !string.IsNullOrEmpty(emailInput.text.Trim());
        bool hasPassword = !string.IsNullOrEmpty(passwordInput.text.Trim());

        loginButton.interactable = hasEmail && hasPassword;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        // Basic RFC 5322 email pattern (simple and efficient)
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    public void OnLoginButtonClicked()
    {
        StartCoroutine(HandleLoginProcess());
    }

    UserData user;
    private IEnumerator HandleLoginProcess()
    {
        if (!IsValidEmail(emailInput.text))
        {
            emailError.SetActive(true);
            yield break; // Stop coroutine early
        }

        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Simulate API delay
        yield return new WaitForSeconds(1f);

        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        List<UserData> users = UserDatabase.LoadUsers();

        user = users.Find(u => u.email == email && u.password == password);

        if (user != null)
        {
            // ✅ Save login for auto-login next time
            PlayerPrefs.SetString("loggedEmail", email);
            PlayerPrefs.SetString("loggedPassword", password);
            PlayerPrefs.Save();

            OnLoginSuccess();
        }
        else
        {
            ShowError("Invalid email or password. Please try again!");

            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }
    }

    private void OnLoginSuccess()
    {
        Debug.Log("✅ Login Successful!");
        Debug.Log("Welcome, " + user.firstName + "!");

        for (int i = 0; i < userName.Length; i++)
            userName[i].text = "Hello! " + user.firstName;

        for (int i = 0; i < userEmail.Length; i++)
            userEmail[i].text = PlayerPrefs.GetString("loggedEmail", "");

        ShowHomeAndLoadClients();
        ResetLogin();
    }

    private void ShowError(string message)
    {
        Debug.Log("❌ " + message);
        if (errorPopup != null)
        {
            errorPopup.SetActive(true);
            errorPopup.GetComponent<PopupFade>().ShowPopup();
        }
    }

    public void ResetLogin()
    {
        emailInput.text = "";
        passwordInput.text = "";

        loginButton.interactable = false;
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

    #endregion

    #region AutoLogin

    private void CheckAutoLogin()
    {
        // Try loading saved user
        string savedEmail = PlayerPrefs.GetString("loggedEmail", "");
        string savedPassword = PlayerPrefs.GetString("loggedPassword", "");

        if (!string.IsNullOrEmpty(savedEmail) && !string.IsNullOrEmpty(savedPassword))
        {
            List<UserData> users = UserDatabase.LoadUsers();
            user = users.Find(u => u.email == savedEmail && u.password == savedPassword);

            if (user != null)
            {
                Debug.Log("✅ Auto Login Successful!");
                //ShowHomeAndLoadClients();

                OnLoginSuccess();
            }
            else
            {
                Debug.Log("⚠️ Saved login invalid, clearing data");
                PlayerPrefs.DeleteKey("loggedEmail");
                PlayerPrefs.DeleteKey("loggedPassword");

                NavigationManager.Instance.OpenScreen(LoginScreen);
            }
        }
        else
        {
            Debug.Log("Started with New Login");
            NavigationManager.Instance.OpenScreen(LoginScreen);
        }
    }

    private void ShowHomeAndLoadClients()
    {
        StartCoroutine(ShowHomeAndLoadClientsCoroutine());
    }

    private IEnumerator ShowHomeAndLoadClientsCoroutine()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Optional: Small wait for smoother transition
        yield return new WaitForSeconds(1f);

        // ✅ Open Home Screen
        NavigationManager.Instance.OpenScreen(HomeScreen, () =>
        {
            // Clear login fields
            ResetLogin();
        });

        // ✅ Load Clients
        if (clientManager != null)
        {
            clientManager.LoadClients();
        }
        else
        {
            Debug.LogWarning("⚠️ ClientManager reference missing!");
        }

        // Optional: Short delay before hiding (gives user a brief loading feel)
        yield return new WaitForSeconds(0.5f);

        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }

    #endregion

    #region Logout

    public void Logout()
    {
        StartCoroutine(HandleLogoutProcess());
    }

    private IEnumerator HandleLogoutProcess()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        yield return new WaitForSeconds(1f); // brief loading effect

        // Clear saved login credentials
        PlayerPrefs.DeleteKey("loggedEmail");
        PlayerPrefs.DeleteKey("loggedPassword");
        PlayerPrefs.Save();

        PlayerPrefs.DeleteAll();

        Debug.Log("👋 User logged out successfully.");


        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        logoutPopup.SetActive(false);

        NavigationManager.Instance.OpenScreen(LoginScreen);
    }

    public void LogoutPopupClose()
    {
        logoutPopup.GetComponent<Animation>().Play("LogoutPopupHide");
    }

    #endregion
}
