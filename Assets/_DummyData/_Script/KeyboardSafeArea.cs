using UnityEngine;
using UnityEngine.UI;

public class KeyboardSafeArea : MonoBehaviour
{
    public ScrollRect scrollRect; // Assign the ScrollRect component in the Inspector
    public RectTransform phoneInputField; // Assign the PhoneNumber InputField's RectTransform in the Inspector
    public CanvasScaler canvasScaler;
    private bool isKeyboardVisible = false;

    void Start()
    {
        //canvasScaler = GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            Debug.LogWarning("CanvasScaler not found. Ensure the panel is under a Canvas with CanvasScaler.");
        }
    }

    void Update()
    {
#if UNITY_IOS || UNITY_ANDROID
        if (TouchScreenKeyboard.visible && !isKeyboardVisible)
        {
            isKeyboardVisible = true;
            ScrollToInputField();
        }
        else if (!TouchScreenKeyboard.visible && isKeyboardVisible)
        {
            isKeyboardVisible = false;
        }
#endif
    }

    void ScrollToInputField()
    {
        if (scrollRect == null || phoneInputField == null || canvasScaler == null) {
            Debug.Log("Scroll Rect is null");
            return; }

        float keyboardHeight = TouchScreenKeyboard.area.height;
        if (keyboardHeight > 0)
        {
            // Convert keyboard height to canvas space
            float scaleFactor = canvasScaler.referenceResolution.y / Screen.height;
            float adjustedKeyboardHeight = keyboardHeight * scaleFactor;

            Canvas.ForceUpdateCanvases();
            Vector2 viewportLocalPosition = phoneInputField.transform.TransformPoint(phoneInputField.rect.center);
            Vector2 scrollViewLocalPosition = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollRect.content, viewportLocalPosition, null, out scrollViewLocalPosition);

            float contentHeight = scrollRect.content.sizeDelta.y;
            float scrollPosition = (scrollViewLocalPosition.y + adjustedKeyboardHeight) / contentHeight;
            scrollRect.verticalNormalizedPosition = 1 - Mathf.Clamp01(scrollPosition);
            Debug.Log("Scroll Rect new pos : " + scrollPosition);
        }
    }
}