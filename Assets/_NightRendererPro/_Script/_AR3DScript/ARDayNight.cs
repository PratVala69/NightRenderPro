using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.XR.ARFoundation;

public class ARDayNight : MonoBehaviour
{
    [Header("ScrollView")]
    public ScrollRect dayNightScroll;

    [Header("Light Reference")]
    public Light arLight;

    [Header("UI References")]
    public RectTransform handle;
    public Image moonIcon;
    public Image sunIcon;

    public Sprite moonSelected, moonDeselected;
    public Sprite sunSelected, sunDeselected;

    [Header("Settings")]
    public float moveDistance = 90f;
    public float moveDuration = 0.25f;
    public float fadeDuration = 0.2f;

    [Header("Light Settings")]
    public float dayIntensity = 1.2f;
    public float nightIntensity = 0.4f;

    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.4f, 0.5f, 1f); // soft moonlight

    [Header("Animation")]
    public float lightTransitionDuration = 1f;
    public float iconFadeDuration = 0.2f;

    public Image nightOverlay;
    public float overlayFadeDuration = 1f;
    public float imagefadeDuration = 1f;

    private bool isNight = false;


    private void Start()
    {
        if (dayNightScroll != null)
        {
            dayNightScroll.onValueChanged.AddListener(OnScrollChanged);
            dayNightScroll.horizontal = true;
            dayNightScroll.vertical = false;
        }

        // Start default in Day mode
        UpdateMode(0f);
    }

    private void OnScrollChanged(Vector2 pos)
    {
        float normalized = pos.x;

        // 0 = Night, 1 = Day
        UpdateMode(normalized);
    }

    private void UpdateMode(float normalized)
    {
        // Light intensity blend (0 = day, 1 = night)
        float targetIntensity = Mathf.Lerp(dayIntensity, nightIntensity, normalized);
        arLight.intensity = targetIntensity;

        // Light color blend
        Color targetColor = Color.Lerp(dayColor, nightColor, normalized);
        arLight.color = targetColor;

        // Disable AR light estimation
        ARCameraManager manager = FindObjectOfType<ARCameraManager>();
        if (manager != null)
            manager.requestedLightEstimation = LightEstimation.None;

        // Overlay (0 = day, 1 = night)
        float targetAlpha = Mathf.Lerp(0f, 0.7f, normalized);
        nightOverlay.DOFade(targetAlpha, overlayFadeDuration);

        // Detect night side correctly (1 = night)
        bool nowNight = normalized > 0.5f;

        if (nowNight != isNight)
        {
            isNight = nowNight;
            UpdateIcons();
        }
    }

    private void UpdateIcons()
    {
        if (isNight)
        {
            moonIcon.sprite = moonSelected;
            sunIcon.sprite = sunDeselected;

            moonIcon.color = new Color(1, 1, 1, 1);
            sunIcon.color = new Color(1, 1, 1, 0.4f);
        }
        else
        {
            moonIcon.sprite = moonDeselected;
            sunIcon.sprite = sunSelected;

            moonIcon.color = new Color(1, 1, 1, 0.4f);
            sunIcon.color = new Color(1, 1, 1, 1);
        }

        float targetX = isNight ? -moveDistance : moveDistance;
        // Slide handle
        handle.DOAnchorPosX(targetX, moveDuration).SetEase(Ease.OutQuad);
    }

    public void ToggleMode()
    {
        isNight = !isNight;

        float targetIntensity = isNight ? nightIntensity : dayIntensity;
        Color targetColor = isNight ? nightColor : dayColor;

        // Light transition using DOTween
        arLight.DOIntensity(targetIntensity, lightTransitionDuration);
        arLight.DOColor(targetColor, lightTransitionDuration);

        // Disable AR light estimation override
        ARCameraManager manager = FindObjectOfType<ARCameraManager>();
        manager.requestedLightEstimation = LightEstimation.None;

        // Skybox brightness (optional)
        RenderSettings.skybox.SetFloat("_Exposure", isNight ? 0.35f : 1f);

        float targetAlpha = isNight ? 0.7f : 0f;   // 0.6 looks like your screenshot
        nightOverlay.DOFade(targetAlpha, imagefadeDuration);

        // Update UI toggle
        UpdateToggleIcon(isNight, () =>
        {
            Debug.Log("Switch animation completed!");
        });
    }

    public void UpdateToggleIcon(bool nightState, System.Action onComplete = null)
    {
        float targetX = nightState ? -moveDistance : moveDistance;

        Sequence seq = DOTween.Sequence();

        // Slide handle
        seq.Append(handle.DOAnchorPosX(targetX, moveDuration).SetEase(Ease.OutQuad));

        // Fade icons + change sprite
        if (nightState)
        {
            seq.Join(moonIcon.DOFade(1f, fadeDuration));
            seq.Join(sunIcon.DOFade(1f, fadeDuration));

            moonIcon.sprite = moonSelected;
            sunIcon.sprite = sunDeselected;
        }
        else
        {
            seq.Join(moonIcon.DOFade(1f, fadeDuration));
            seq.Join(sunIcon.DOFade(1f, fadeDuration));

            moonIcon.sprite = moonDeselected;
            sunIcon.sprite = sunSelected;
        }

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }
}
