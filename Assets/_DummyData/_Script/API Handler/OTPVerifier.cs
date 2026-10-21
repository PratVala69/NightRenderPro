using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;
using TMPro;
using System.Collections;

public class OTPVerifier : MonoBehaviour
{
    public string strPhoneNumber; // UI InputField for phone number public InputField OTPInput; // UI InputField for OTP public Button SendOTPButton; // UI Button to send OTP public Button VerifyOTPButton; // UI Button to verify OTP public Text StatusText; // UI Text to display status

    //private FirebaseAuth auth;
    private string verificationId;
    private uint phoneAuthTimeoutMs = 120000; // 120 seconds timeout
    public TextMeshProUGUI StatusText;
    public TextMeshProUGUI resendOTPTimer;

    public string strOtp;

    public LoginHandler loginHandler;

    private bool isTimerRunning = false;
    private float timerDuration = 120f; // 2 minutes in seconds
    private float timerRemaining;

    public static bool firebaseAuth = false;

    void Start()
    {
        // Initialize Firebase Auth
        //auth = FirebaseAuth.DefaultInstance;

        //// Initialize FCM
        //FirebaseMessaging.TokenReceived += OnTokenReceived;
        //FirebaseMessaging.MessageReceived += OnMessageReceived;

        //// Request user permission (important for iOS)
        //FirebaseMessaging.RequestPermissionAsync().ContinueWith(task =>
        //{
        //    if (task.IsCompleted && !task.IsFaulted)
        //    {
        //        Debug.Log("FCM permission granted.");
        //    }
        //    else
        //    {
        //        Debug.LogWarning("FCM permission not granted: " + task.Exception?.Message);
        //    }
        //});

        //// Get FCM token immediately
        //FirebaseMessaging.GetTokenAsync().ContinueWith(task =>
        //{
        //    if (task.IsCanceled || task.IsFaulted)
        //    {
        //        Debug.LogError("Failed to get FCM token: " + task.Exception);
        //    }
        //    else
        //    {
        //        string token = task.Result;
        //        Debug.Log("FCM Token (from GetTokenAsync): " + token);
        //        PlayerPrefs.SetString("fcm_token", token);

        //        // You can also send this token to your server here
        //    }
        //});
    }

    //void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    //{
    //    Debug.Log("FCM Token: " + token.Token);

    //    // Optionally store or send to server
    //    PlayerPrefs.SetString("fcm_token", token.Token);
    //}

    //void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    //{
    //    Debug.Log("FCM Message Received from: " + e.Message.From);

    //    if (e.Message.Data.TryGetValue("otp", out string otp))
    //    {
    //        Debug.Log("OTP from FCM: " + otp);
    //        // Handle OTP (e.g., auto-fill)
    //    }
    //}

    public void PhoneAuthFirebase()
    {
        StartCoroutine(StartPhoneAuth());
        firebaseAuth = true;
    }

    public void VerifyOTPFirebase()
    {
        StartCoroutine(VerifyOTP());
    }

    IEnumerator StartPhoneAuth()
    {
        int index = loginHandler.prefixDropdown.value;
        string selectedText = loginHandler.prefixDropdown.options[index].text;
        
        string digitsOnly = "";
        foreach (char c in loginHandler.loginInputField.text)
        {
            if (char.IsDigit(c))
                digitsOnly += c;
        }

        strPhoneNumber = "";
        strPhoneNumber = selectedText + digitsOnly;

        string phoneNumber = strPhoneNumber;//PhoneNumberInput.text;
        if (string.IsNullOrEmpty(phoneNumber))
        {
            StatusText.text = "Please enter a valid phone number.";
            loginHandler.errorPopupScreen.SetActive(true);
            loginHandler.loadingScreen.SetActive(false);

            yield break;
        }

        // Ensure phone number includes country code, e.g., +12025550123
        if (!phoneNumber.StartsWith("+"))
        {
            StatusText.text = "Phone number must include country code (e.g., +1).";
            loginHandler.errorPopupScreen.SetActive(true);
            loginHandler.loadingScreen.SetActive(false);
            yield break;
        }

        StatusText.text = "Sending OTP...";

        //PhoneAuthProvider provider = PhoneAuthProvider.GetInstance(auth);
        //provider.VerifyPhoneNumber(new PhoneAuthOptions
        //{
        //    PhoneNumber = phoneNumber,
        //    TimeoutInMilliseconds = phoneAuthTimeoutMs,
        //    ForceResendingToken = null
        //}, (credential) =>
        //{
        //    // Auto-retrieval or instant validation
        //    StartCoroutine(SignInWithCredential(credential));
        //}, (error) =>
        //{
        //    StatusText.text = $"Verification failed: {error}";
        //    Debug.Log($"Verification failed: {error}");
        //    loginHandler.errorPopupScreen.SetActive(true);
        //    loginHandler.loadingScreen.SetActive(false);
        //}, (id, token) =>
        //{
        //    verificationId = id;
        //    StatusText.text = "OTP sent! Enter the code.";
        //    PlayerPrefs.SetString("VerificationId", id); // Persist verification ID

        //    loginHandler.loginScreen.SetActive(false); // Hide login screen
        //    loginHandler.otpScreen.SetActive(true); // Show OTP screen
        //    loginHandler.loadingScreen.SetActive(false);

        //    resendOTPTimer.text = "";
        //    StartCoroutine(StartTimer());
        //    resendOTPTimer.GetComponent<Button>().interactable = false;

        //}, (id) =>
        //{
        //    StatusText.text = "OTP retrieval timed out.";
        //});

        // Yield to allow Firebase to process the request
        yield return null;
    }

    IEnumerator VerifyOTP()
    {
        loginHandler.loadingScreen.SetActive(true);
        yield return new WaitForSeconds(2f);

        loginHandler.AssignOTP();


        if (string.IsNullOrEmpty(strOtp))
        {
            StatusText.text = "OTP Sent. Enter the OTP.";
            yield break;
        }

        if (string.IsNullOrEmpty(verificationId))
        {
            verificationId = PlayerPrefs.GetString("VerificationId", "");
            if (string.IsNullOrEmpty(verificationId))
            {
                StatusText.text = "No verification ID found. Request OTP again.";

                loginHandler.loadingScreen.SetActive(false);

                resendOTPTimer.text = "Resend OTP";
                resendOTPTimer.GetComponent<Button>().interactable = true;
                yield break;
            }
        }

        StatusText.text = "Verifying OTP...";

        //PhoneAuthProvider provider = PhoneAuthProvider.GetInstance(auth);
        //Credential credential = provider.GetCredential(verificationId, strOtp);
        //yield return StartCoroutine(SignInWithCredential(credential));
    }

    //IEnumerator SignInWithCredential(Credential credential)
    //{
    //    var signInTask = auth.SignInWithCredentialAsync(credential);
    //    while (!signInTask.IsCompleted)
    //        yield return null;

    //    if (signInTask.IsFaulted)
    //    {
    //        //StatusText.text = $"Sign-in error: {signInTask.Exception.Message}";
    //        StatusText.text = "OTP mismatch, please try again.";
    //        Debug.Log($"Sign-in error: {signInTask.Exception}");
    //        loginHandler.loadingScreen.SetActive(false);
    //        yield break;
    //    }

    //    loginHandler.loadingScreen.SetActive(false);

    //    FirebaseUser user = signInTask.Result;
    //   // StatusText.text = $"Signed in as: {user.PhoneNumber}";
    //    Debug.Log($"User signed in: {user.UserId}, Phone: {user.PhoneNumber}");
    //    StopCoroutine(StartTimer());

    //    loginHandler.OTPConfirmation();
    //}

    [ContextMenu("Check Timer")]
    public void checkTimer()
    {
        resendOTPTimer.text = "Resent OTP in";
        StartCoroutine(StartTimer()); // Stop timer on successful sign-in
        resendOTPTimer.GetComponent<Button>().interactable = false;
    }

    IEnumerator StartTimer()
    {
        isTimerRunning = true;
        timerRemaining = timerDuration;

        while (timerRemaining > 0)
        {
            timerRemaining -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(timerRemaining / 60);
            int seconds = Mathf.FloorToInt(timerRemaining % 60);
            resendOTPTimer.text = string.Format("<b><color=#0E1F3C>Resend OTP in</color></b> <color=#8C8C8C>{0}:{1:00}</color>", minutes, seconds);
            //Debug.Log("Time : " + string.Format("Resent otp in {0:00}:{1:00}", minutes, seconds));
            yield return null;
        }

        isTimerRunning = false;
        resendOTPTimer.text = "<b><color=#0E1F3C>Resend OTP</color>";
        resendOTPTimer.GetComponent<Button>().interactable = true; // Enable resend button after timer expires
        StatusText.text = "OTP expired. Please resend OTP.";
    }

    public void OnResendOTPClicked()
    {
        if (!isTimerRunning)
        {
            Debug.Log("Resending OTP...");
            StartCoroutine(StartPhoneAuth()); // Reuse your original OTP request method
        }
    }
}