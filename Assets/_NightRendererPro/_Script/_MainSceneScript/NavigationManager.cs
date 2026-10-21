using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance;

    [Header("Animation")]
    public float fadeDuration = 0.3f;
    public Ease fadeEase = Ease.Linear;   // ✅ Select ease from Inspector

    private Stack<GameObject> screenStack = new Stack<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenScreen(GameObject newScreen, System.Action onComplete = null)
    {
        CanvasGroup newCG = newScreen.GetComponent<CanvasGroup>();
        if (newCG == null) newCG = newScreen.AddComponent<CanvasGroup>();

        newScreen.SetActive(true);
        newCG.alpha = 0;
        newCG.interactable = false;
        newCG.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();

        // Fade-out old screen
        if (screenStack.Count > 0)
        {
            GameObject oldScreen = screenStack.Peek();
            CanvasGroup oldCG = oldScreen.GetComponent<CanvasGroup>();

            seq.Append(
                oldCG.DOFade(0f, fadeDuration).SetEase(fadeEase)   // ✅ use inspector ease
            )
            .AppendCallback(() =>
            {
                oldScreen.SetActive(false);
                oldCG.interactable = false;
                oldCG.blocksRaycasts = false;
            });
        }

        // Fade-in new screen
        seq.Append(
            newCG.DOFade(1f, fadeDuration).SetEase(fadeEase)       // ✅ use inspector ease
        )
        .OnComplete(() =>
        {
            newCG.interactable = true;
            newCG.blocksRaycasts = true;
            onComplete?.Invoke();
        });

        screenStack.Push(newScreen);
    }

    public void OpenScreen(GameObject newScreen)
    {
        CanvasGroup newCG = newScreen.GetComponent<CanvasGroup>();
        if (newCG == null) newCG = newScreen.AddComponent<CanvasGroup>();

        newScreen.SetActive(true);
        newCG.alpha = 0;
        newCG.interactable = false;
        newCG.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();

        // Fade-out old screen
        if (screenStack.Count > 0)
        {
            GameObject oldScreen = screenStack.Peek();
            CanvasGroup oldCG = oldScreen.GetComponent<CanvasGroup>();

            seq.Append(
                oldCG.DOFade(0f, fadeDuration).SetEase(fadeEase)   // ✅ use inspector ease
            )
            .AppendCallback(() =>
            {
                oldScreen.SetActive(false);
                oldCG.interactable = false;
                oldCG.blocksRaycasts = false;
            });
        }

        // Fade-in new screen
        seq.Append(
            newCG.DOFade(1f, fadeDuration).SetEase(fadeEase)       // ✅ use inspector ease
        )
        .OnComplete(() =>
        {
            newCG.interactable = true;
            newCG.blocksRaycasts = true;
        });

        screenStack.Push(newScreen);
    }

    public void GoBack()
    {
        if (screenStack.Count <= 1) return;

        GameObject current = screenStack.Pop();
        GameObject previous = screenStack.Peek();

        CanvasGroup currentCG = current.GetComponent<CanvasGroup>();
        CanvasGroup previousCG = previous.GetComponent<CanvasGroup>();

        previous.SetActive(true);
        previousCG.alpha = 0;
        previousCG.interactable = false;
        previousCG.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();

        // Fade-out current
        seq.Append(
            currentCG.DOFade(0f, fadeDuration).SetEase(fadeEase)   // ✅ use inspector ease
        )
        .AppendCallback(() =>
        {
            current.SetActive(false);
            currentCG.interactable = false;
            currentCG.blocksRaycasts = false;
        });

        // Fade-in previous
        seq.Append(
            previousCG.DOFade(1f, fadeDuration).SetEase(fadeEase)  // ✅ use inspector ease
        )
        .OnComplete(() =>
        {
            previousCG.interactable = true;
            previousCG.blocksRaycasts = true;
        });
    }
}
