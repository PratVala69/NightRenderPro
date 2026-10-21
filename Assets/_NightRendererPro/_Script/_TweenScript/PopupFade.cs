using UnityEngine;
using DG.Tweening;

public class PopupFade : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform panel;

    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float scaleDuration = 0.25f;

    private Vector3 startScale = Vector3.one * 0.85f;

    private void Awake()
    {
        panel.localScale = startScale;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void ShowPopup(System.Action onComplete = null)
    {
        gameObject.SetActive(true);
        panel.localScale = startScale;

        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(1, fadeDuration));
        seq.Join(panel.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));

        seq.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            onComplete?.Invoke();
        });
    }

    public void ShowPopup()
    {
        gameObject.SetActive(true);
        panel.localScale = startScale;

        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(1, fadeDuration));
        seq.Join(panel.DOScale(1f, scaleDuration).SetEase(Ease.OutBack));

        seq.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });
    }

    public void HidePopup(System.Action onComplete = null)
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(0, fadeDuration));
        seq.Join(panel.DOScale(startScale, scaleDuration).SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void HidePopup()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(canvasGroup.DOFade(0, fadeDuration));
        seq.Join(panel.DOScale(startScale, scaleDuration).SetEase(Ease.InBack));

        seq.OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        });
    }
}
