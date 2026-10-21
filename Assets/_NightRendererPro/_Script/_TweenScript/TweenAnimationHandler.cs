using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class TweenAnimationHandler : MonoBehaviour
{
    [Header("Instruction Popup References")]
    public RectTransform popupBGImage;
    public CanvasGroup popupCanvasGroup;

    [Header("Animation Settings")]
    public float moveDistance = 300f;
    public float moveDurationforInfo = 0.4f;
    public float fadeDuration = 0.3f;
    public Ease moveEase = Ease.InOutQuad; // Ease for smooth motion
    public Ease fadeEase = Ease.OutQuad;   // Ease for fading

    private Vector2 originalPos;

    [Header("Main Info Panel")]
    [SerializeField] private RectTransform infoPanel;
    [SerializeField] private CanvasGroup defaultViewPanel;

    [Header("Sub Panels (4 different hint panels)")]
    [SerializeField] private RectTransform[] subPanels;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutQuad;

    private Vector2 infoShowPos;
    private Vector2 infoHidePos;
    private Vector2[] subShowPos;
    private Vector2[] subHidePos;

    [Header("Color Scrollbar")]
    public ScrollRect colorScrollRect;
    public RectTransform content; // colorScrollRect.content

    private int currentSubIndex = -1;

    private void Awake()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (infoPanel != null)
        {        // 🟩 Info panel setup (Left ↔ Right)
            infoShowPos = infoPanel.anchoredPosition;
            infoHidePos = new Vector2(-screenWidth, infoShowPos.y); // off-screen left


            infoPanel.anchoredPosition = infoHidePos;
            infoPanel.gameObject.SetActive(false);
        }

        // Subpanels setup (slide bottom ↔ top)
        subShowPos = new Vector2[subPanels.Length];
        subHidePos = new Vector2[subPanels.Length];

        for (int i = 0; i < subPanels.Length; i++)
        {
            subShowPos[i] = subPanels[i].anchoredPosition;
            subHidePos[i] = new Vector2(subShowPos[i].x, -screenHeight);

            subPanels[i].anchoredPosition = subHidePos[i];
            subPanels[i].gameObject.SetActive(false);
        }
    }

    #region InfoAndSubPanel
    public void ShowInfoPanel()
    {
        if (currentSubIndex != -1)
        {
            // Close open subpanel before showing info
            HideSubPanel(() => AnimateInfoShow());
        }
        else
        {
            AnimateInfoShow();
        }
    }

    private void AnimateInfoShow()
    {
        // Fade out default view
        if (defaultViewPanel != null)
        {
            defaultViewPanel.DOKill();
            defaultViewPanel.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                defaultViewPanel.interactable = false;
                defaultViewPanel.blocksRaycasts = false;
            });
        }

        if (infoPanel == null)
            return;

        infoPanel.gameObject.SetActive(true);
        infoPanel.DOKill();

        float screenWidth = Screen.width;
        Vector2 startPos = new Vector2(-screenWidth, infoShowPos.y); // start from left
        infoPanel.anchoredPosition = startPos;

        infoPanel.DOAnchorPos(infoShowPos, moveDuration).SetEase(easeType);
    }

    public void InfoPanel_Close ()
    {
        infoPanel.DOKill();

        float screenWidth = Screen.width;
        Vector2 hidePos = new Vector2(-screenWidth, infoShowPos.y); // move off-screen left

        infoPanel.DOAnchorPos(hidePos, moveDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                infoPanel.gameObject.SetActive(false);

                // Fade back in default view
                if (defaultViewPanel != null)
                {
                    defaultViewPanel.DOKill();
                    defaultViewPanel.DOFade(1f, fadeDuration).OnComplete(() =>
                    {
                        defaultViewPanel.interactable = true;
                        defaultViewPanel.blocksRaycasts = true;
                    });
                }
            });
    }

    // 🟥 Hide Info Panel (Right → Left)
    public void HideInfoPanel(System.Action onComplete = null)
    {
        infoPanel.DOKill();

        float screenWidth = Screen.width;
        Vector2 hidePos = new Vector2(-screenWidth, infoShowPos.y); // move off-screen left

        infoPanel.DOAnchorPos(hidePos, moveDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                infoPanel.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    // 🟦 Show Sub Panel (Bottom → Top)
    //public void ShowSubPanel(int index)
    //{
    //    if (index < 0 || index >= subPanels.Length) return;

    //    // If another subpanel is open, close it first before showing new one
    //    if (currentSubIndex != -1 && currentSubIndex != index)
    //    {
    //        HideSubPanel(() => ShowSubPanel(index));
    //        return;
    //    }

    //    currentSubIndex = index;

    //    HideInfoPanel(() =>
    //    {
    //        RectTransform panel = subPanels[index];
    //        panel.gameObject.SetActive(true);
    //        panel.DOKill();
    //        panel.anchoredPosition = subHidePos[index];
    //        panel.DOAnchorPos(subShowPos[index], moveDuration).SetEase(easeType);
    //    });
    //}

    public void ShowSubPanel(int index)
    {
        if (index < 0 || index >= subPanels.Length) return;

        // If already open another panel
        if (currentSubIndex != -1 && currentSubIndex != index)
        {
            HideSubPanel(() => ShowSubPanel(index));
            return;
        }

        currentSubIndex = index;

        // Start sliding info panel out
        infoPanel.DOKill();
        float screenWidth = Screen.width;
        Vector2 hidePos = new Vector2(-screenWidth, infoShowPos.y);

        // Slide info out
        infoPanel.DOAnchorPos(hidePos, moveDuration)
            .SetEase(easeType)
            .OnComplete(() => infoPanel.gameObject.SetActive(false));

        // Overlap: Start subpanel after a slight delay
        StartCoroutine(ShowSubDelayed(index, 0.2f));
    }

    private IEnumerator ShowSubDelayed(int index, float delay)
    {
        yield return new WaitForSeconds(delay);

        RectTransform panel = subPanels[index];
        panel.gameObject.SetActive(true);
        panel.DOKill();
        panel.anchoredPosition = subHidePos[index];

        panel.DOAnchorPos(subShowPos[index], moveDuration)
            .SetEase(easeType);
    }

    // 🟨 Hide Sub Panel (Top → Bottom)
    public void HideSubPanel(System.Action onComplete = null)
    {
        if (currentSubIndex == -1)
        {
            onComplete?.Invoke();
            return;
        }

        RectTransform panel = subPanels[currentSubIndex];
        int closedIndex = currentSubIndex;
        currentSubIndex = -1;

        panel.DOKill();
        Vector2 offScreenBottom = new Vector2(panel.anchoredPosition.x, -Screen.height);

        panel.DOAnchorPos(offScreenBottom, moveDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                panel.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    // Called by SubPanel Close Button
    public void OnSubPanelClose()
    {
        HideSubPanel(() =>
        {
            ShowInfoPanel();
        });
    }

    void HideDefaultView(System.Action onComplete = null)
    {
        if (defaultViewPanel == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Stop previous tweens on this CanvasGroup
        defaultViewPanel.DOKill();

        // Disable interaction immediately
        defaultViewPanel.interactable = false;
        defaultViewPanel.blocksRaycasts = false;

        // Fade out
        defaultViewPanel
            .DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                onComplete?.Invoke();
            });
    }

    public void DayNightScrollOpen()
    {
        HideDefaultView(() =>
        {
            Debug.Log("Default panel is now hidden");
            ShowSubPanel(3);
        });
    }

    public void DayNightScrollOpenForMeasure()
    {
        HideDefaultView(() =>
        {
            Debug.Log("Default panel is now hidden");
            ShowSubPanel(0);
        });
    }

    public void OnDayNightScrollClose()
    {
        HideSubPanel(() =>
        {
            if (defaultViewPanel != null)
            {
                defaultViewPanel.DOKill();
                defaultViewPanel
                    .DOFade(1f, fadeDuration)
                    .OnComplete(() =>
                    {
                        defaultViewPanel.interactable = true;
                        defaultViewPanel.blocksRaycasts = true;
                    });
            }
        });
    }

    [ContextMenu("Reset COlor")]
    public void ResetColorScroll()
    {
        // If user is dragging, wait one frame so we don't fight the drag
        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        // Stop inertia & motion
        colorScrollRect.StopMovement();
        colorScrollRect.velocity = Vector2.zero;

        // Make sure layout is up-to-date
        Canvas.ForceUpdateCanvases();
        if (content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // Wait one frame so sizes/scrollable range are valid on device
        yield return null;

        // Now set normalized position
        var p = colorScrollRect.normalizedPosition;
        p.x = 0f;
        colorScrollRect.normalizedPosition = p;

        // Safety: apply again next frame to beat elastic bounce/inertia on some devices
        yield return null;
        colorScrollRect.StopMovement();
        colorScrollRect.velocity = Vector2.zero;
        p = colorScrollRect.normalizedPosition;
        p.x = 0f;
        colorScrollRect.normalizedPosition = p;
    }

    #endregion

    #region InstructionPopup
    public void OnInstructionPopup_Close()
    {
        if (popupCanvasGroup == null || popupBGImage == null)
        {
            Debug.LogWarning("TweenAnimationHandler: Missing references.");
            return;
        }

        // Disable interaction while animating
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;

        // Step 1: Move down
        popupBGImage.DOAnchorPosY(originalPos.y - moveDistance, moveDurationforInfo)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                // Step 2: Fade out after move-down complete
                popupCanvasGroup.DOFade(0f, fadeDuration)
                    .SetEase(fadeEase)
                    .OnComplete(() =>
                    {
                        // Step 3: Deactivate panel and reset
                        popupCanvasGroup.gameObject.SetActive(false);
                        popupBGImage.anchoredPosition = originalPos;
                        popupCanvasGroup.alpha = 1f;
                        popupCanvasGroup.interactable = true;
                        popupCanvasGroup.blocksRaycasts = true;
                    });
            });
    }
    #endregion
}
