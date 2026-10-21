using DG.Tweening;
using UnityEngine;

public class InfoPanelTweenHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform target; // The panel to move

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("Direction Settings")]
    [SerializeField] private bool slideFromLeft = false;
    [SerializeField] private bool slideFromBottom = false;

    private Vector2 originalPos;
    private Vector2 hiddenPos;

    private void Awake()
    {
        originalPos = target.anchoredPosition;

        if (slideFromLeft)
            hiddenPos = new Vector2(-Screen.width, originalPos.y); // off-screen left
        else if (slideFromBottom)
            hiddenPos = new Vector2(originalPos.x, -Screen.height); // off-screen bottom
        else
            hiddenPos = originalPos; // fallback

        // Start hidden
        target.anchoredPosition = hiddenPos;
        gameObject.SetActive(false);
    }

    public void ShowPanel()
    {
        gameObject.SetActive(true);
        target.DOKill();
        target.anchoredPosition = hiddenPos;
        target.DOAnchorPos(originalPos, duration).SetEase(easeType);
    }

    public void HidePanel()
    {
        target.DOKill();

        Vector2 exitPos = hiddenPos;

        // Reverse direction when closing
        if (slideFromLeft)
            exitPos = new Vector2(Screen.width, originalPos.y); // move out right
        else if (slideFromBottom)
            exitPos = new Vector2(originalPos.x, Screen.height); // move out top

        target.DOAnchorPos(exitPos, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
