using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HintPanelFader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup currentPanel;   // Current active panel
    [SerializeField] private CanvasGroup nextPanel;      // Next hint panel (optional for last panel)

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float overlapDelay = 0.05f;
    [SerializeField] private Ease fadeEase = Ease.InOutQuad;

    private bool isTransitioning = false;

    private void Start()
    {
        // Ensure initial states
        if (currentPanel)
        {
            currentPanel.alpha = 1f;
            currentPanel.interactable = true;
            currentPanel.blocksRaycasts = true;
        }

        if (nextPanel)
        {
            nextPanel.alpha = 0f;
            nextPanel.interactable = false;
            nextPanel.blocksRaycasts = false;
            nextPanel.gameObject.SetActive(false);
        }
    }

    public void OnPanelTap()
    {
        if (isTransitioning || currentPanel == null)
            return;

        isTransitioning = true;

        // Prepare next panel before fading
        if (nextPanel != null)
        {
            nextPanel.gameObject.SetActive(true);
            nextPanel.alpha = 0f;
            nextPanel.interactable = false;
            nextPanel.blocksRaycasts = false;
        }

        // Fade out current panel
        currentPanel.DOKill();
        currentPanel.DOFade(0f, fadeDuration)
            .SetEase(fadeEase)
            .OnComplete(() =>
            {
                currentPanel.interactable = false;
                currentPanel.blocksRaycasts = false;
                currentPanel.gameObject.SetActive(false);
            });

        // Smoothly fade in next panel slightly before current finishes
        if (nextPanel != null)
        {
            nextPanel.DOKill();
            nextPanel.DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .SetDelay(overlapDelay) // 🟢 small overlap for smooth transition
                .OnComplete(() =>
                {
                    nextPanel.interactable = true;
                    nextPanel.blocksRaycasts = true;
                    isTransitioning = false;
                });
        }
        else
        {
            // No next panel
            DOVirtual.DelayedCall(fadeDuration, () => isTransitioning = false);
        }
    }
}
