using UnityEngine;
using UnityEngine.UI;

public class SafeAreaHandler : MonoBehaviour
{
    [SerializeField]
    private RectTransform uiPanel; // Assign your UI panel or canvas in the Inspector

    void Start()
    {
        ApplySafeArea();
    }

    void Update()
    {
        // Reapply safe area if the screen orientation or device changes (e.g., notch visibility)
        if (Screen.safeArea != GetComponent<RectTransform>().rect)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        if (uiPanel == null) return;

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        // Convert screen space to canvas local space
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        uiPanel.anchorMin = anchorMin;
        uiPanel.anchorMax = anchorMax;
        uiPanel.offsetMin = Vector2.zero;
        uiPanel.offsetMax = Vector2.zero;
    }
}