using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VLB;
using TMPro;

public class LightOptionHandler : MonoBehaviour
{
    [Header("UI - Main Controls")]
    [SerializeField] private List<ColorButtonCard> colorButtons = new();
    [SerializeField] private ScrollRect spotAngleScrollRect;
    [SerializeField] private ScrollRect intensityScrollRect;
    [SerializeField] private CanvasGroup uiCanvasGroup;

    [SerializeField] private Button accentAngleButton;

    [Header("UI - Accent Light Specific")]
    [SerializeField] private ScrollRect accentLightAngleScrollRect;
    //[SerializeField] private CanvasGroup accentLightAngleGroup; // Drag the whole Accent slider parent here

    [Header("Header Text")]
    [SerializeField] private TextMeshProUGUI lightModelName;
    [SerializeField] private TextMeshProUGUI lightName;
    [SerializeField] private TextMeshProUGUI lightValue;

    private SelectableObject _current;
    private ColorButtonCard _currentColorButton;

    // Add near the top with other fields
    [Header("Group Mode (for AR Measurement)")]
    [SerializeField] private bool supportGroupMode = true;  // Set true only in AR scene
    [SerializeField] private Button singleInfoButton;
    [SerializeField] private ARMeasurementManager arManager;
    [SerializeField] private TweenAnimationHandler tweenAnimationHandler;
    private bool _globalLightState = false;

    private GameObject _currentGroupPrefab;
    private bool _isGroupMode = false;
    private List<SelectableObject> _allObjectsOfType = new List<SelectableObject>();
    private Dictionary<GameObject, LightSettings> prefabSettings = new();


    private void Awake()
    {
        foreach (var btn in colorButtons)
            if (btn) btn.Init(this);

        if (singleInfoButton)
            singleInfoButton.onClick.AddListener(OnSingleInfoButtonPressed);
    }

    private void OnEnable()
    {
        if (spotAngleScrollRect) spotAngleScrollRect.onValueChanged.AddListener(OnSpotAngleChanged);
        if (intensityScrollRect) intensityScrollRect.onValueChanged.AddListener(OnIntensityChanged);
        if (accentLightAngleScrollRect) accentLightAngleScrollRect.onValueChanged.AddListener(OnAccentLightAngleChanged);
    }

    private void OnDisable()
    {
        if (spotAngleScrollRect) spotAngleScrollRect.onValueChanged.RemoveListener(OnSpotAngleChanged);
        if (intensityScrollRect) intensityScrollRect.onValueChanged.RemoveListener(OnIntensityChanged);
        if (accentLightAngleScrollRect) accentLightAngleScrollRect.onValueChanged.RemoveListener(OnAccentLightAngleChanged);
    }

    public void OnSingleInfoButtonPressed()
    {
        if (arManager != null && arManager.HasSelection)
        {
            EnterGroupControlMode(arManager.SelectedPrefab);

            tweenAnimationHandler.ShowInfoPanel();
        }
        else
        {
            Debug.Log("Select a light prefab first");
            // optional: show UI warning
        }
    }

    public void SelectObject(SelectableObject obj)
    {
        if (_current && _current != obj)
            _current.SetSelected(false);

        _current = obj;

        if (obj == null)
        {
            SetUIActive(false);
            //HideAccentControls();
            return;
        }

        obj.SetSelected(true);
        UpdateHeader();
        RefreshUIFromObject();

        SetUIActive(true);
    }

    private void UpdateHeader()
    {
        if (lightModelName) lightModelName.text = _current.lightModelName;
        if (lightName) lightName.text = _current.lightName;
        if (lightValue) lightValue.text = _current.lightValue;
    }

    // Main Controls
    private void OnSpotAngleChanged(Vector2 v) => ApplySpotAngle(v.x);
    private void OnIntensityChanged(Vector2 v) => ApplyIntensity(v.x);
    private void OnAccentLightAngleChanged(Vector2 v) => ApplyAccentRotation(v.x);

    private void ApplySpotAngle(float norm)
    {
        if (!_current) return; // Block if accent light is active
        float angle = Mathf.Lerp(_current.minSpotAngle, _current.maxSpotAngle, norm);
        //_current.SetSavedSpotAngle(angle);
        if (_isGroupMode)
        {
            foreach (var obj in _allObjectsOfType)
                obj.SetSavedSpotAngle(angle);
        }
        else
            _current.SetSavedSpotAngle(angle);

        SetScrollValue(spotAngleScrollRect, norm);
        SaveCurrentSettings();
    }

    private void ApplyIntensity(float norm)
    {
        if (!_current) return;
        float intensity = Mathf.Lerp(_current.minIntensity, _current.maxIntensity, norm);
        //_current.SetSavedIntensity(intensity);
        if (_isGroupMode)
        {
            foreach (var obj in _allObjectsOfType)
                obj.SetSavedIntensity(intensity);
        }
        else
            _current.SetSavedIntensity(intensity);

        SetScrollValue(intensityScrollRect, norm);
        SaveCurrentSettings();
    }

    // Accent Light Specific
    private void ApplyAccentRotation(float normalized)
    {
        if (!_current || !_current.isAccentLight) return;

        //_current.SetAccentRotation(normalized); // rotates real object + saves
        if (_isGroupMode)
        {
            foreach (var obj in _allObjectsOfType)
                obj.SetAccentRotation(normalized);
        }
        else
            _current.SetAccentRotation(normalized);

        SetScrollValue(accentLightAngleScrollRect, normalized);
        SaveCurrentSettings();
    }

    public void OnColorSelected(ColorButtonCard btn)
    {
        if (_currentColorButton) _currentColorButton.SetSelected(false);
        _currentColorButton = btn;
        btn.SetSelected(true);

        if (!_current) return;

        Color c = btn.GetColor();

        if (_isGroupMode && _current)
        {
            foreach (var obj in _allObjectsOfType)
                obj.SetSavedColor(c);
        }
        else
            _current.SetSavedColor(c);

        foreach (var b in colorButtons)
            b.outlineImage.gameObject.SetActive(b == btn);

        SaveCurrentSettings();
    }

    private void RefreshUIFromObject()
    {
        if (!_current) return;

        // Existing main sliders...
        float angleNorm = Mathf.InverseLerp(_current.minSpotAngle, _current.maxSpotAngle, _current.GetCurrentSpotAngle());
        float intNorm = Mathf.InverseLerp(_current.minIntensity, _current.maxIntensity, _current.GetCurrentIntensity());
        SetScrollValue(spotAngleScrollRect, angleNorm);
        SetScrollValue(intensityScrollRect, intNorm);

        // Accent rotation
        if (_current.isAccentLight)
        {
            accentAngleButton.gameObject.SetActive(true);
            float rotNorm = _current.GetAccentRotationNormalized();
            SetScrollValue(accentLightAngleScrollRect, rotNorm);
        }
        else
        {
            accentAngleButton.gameObject.SetActive(false);
        }

        // Color matching...
        Color current = _current.GetCurrentColor();
        ColorButtonCard match = colorButtons.Find(b => ColorApproximately(b.GetColor(), current));
        if (match) OnColorSelected(match);
        else DeselectAllColorButtons();
    }

    public void ToggleLightsGroup()
    {
        // Flip global state
        _globalLightState = !_globalLightState;

        if (_isGroupMode)
        {
            foreach (var obj in _allObjectsOfType)
                obj.SetLightState(_globalLightState);
        }
        else if (_current != null)
        {
            _current.SetLightState(_globalLightState);
        }

        SaveCurrentSettings();
    }

    private void SetScrollValue(ScrollRect sr, float norm)
    {
        if (!sr) return;
        var pos = sr.normalizedPosition;
        pos.x = Mathf.Clamp01(norm);
        sr.normalizedPosition = pos;
    }

    private void SetUIActive(bool active)
    {
        if (uiCanvasGroup)
        {
            uiCanvasGroup.alpha = active ? 1f : 0.3f;
            uiCanvasGroup.interactable = active;
            uiCanvasGroup.blocksRaycasts = active;
        }
    }

    private void DeselectAllColorButtons()
    {
        if (_currentColorButton) _currentColorButton.SetSelected(false);
        _currentColorButton = null;
        foreach (var b in colorButtons)
            b.outlineImage.gameObject.SetActive(false);
    }

    public LightSettings GetSettingsForPrefab(GameObject prefab)
    {
        if (prefabSettings.TryGetValue(prefab, out var settings))
            return settings;

        return null;
    }

    private bool ColorApproximately(Color a, Color b)
        => Vector4.Distance(a, b) < 0.1f;

    /// <summary>
    /// Call this from your single Info button in the AR UI when a prefab is selected
    /// </summary>

    public void EnterGroupControlMode(GameObject prefab)
    {
        if (!supportGroupMode) return;

        _isGroupMode = true;
        _currentGroupPrefab = prefab;
        _allObjectsOfType.Clear();
        var segments = arManager.GetAllSegments();

        foreach (var seg in segments)
        {
            foreach (var obj in seg.LightObjects)
            {
                if (obj == null) continue;

                var sel = obj.GetComponent<SelectableObject>();

                if (sel != null && sel.prefabReference == prefab)
                {
                    _allObjectsOfType.Add(sel);
                }
            }
        }

        if (_allObjectsOfType.Count > 0)
        {
            // Use first object for UI display
            SelectObject(_allObjectsOfType[0]);  // reuses existing logic
            _current.isGroupInstance = true;     // we'll add this flag
        }
        else
        {
            Debug.Log("No instances of this prefab yet.");
        }
    }

    /// <summary>
    /// Exit group mode (call when user selects a different prefab or closes panel)
    /// </summary>
    public void ExitGroupControlMode()
    {
        _isGroupMode = false;
        _currentGroupPrefab = null;
        _allObjectsOfType.Clear();
        SelectObject(null); // hide UI
    }

    private void SaveCurrentSettings()
    {
        if (_current == null || _current.prefabReference == null)
            return;

        var prefab = _current.prefabReference;

        if (!prefabSettings.ContainsKey(prefab))
            prefabSettings[prefab] = new LightSettings();

        var settings = prefabSettings[prefab];

        settings.intensity = _current.GetCurrentIntensity();
        settings.spotAngle = _current.GetCurrentSpotAngle();
        settings.color = _current.GetCurrentColor();
        settings.accentRotation = _current.GetAccentRotationNormalized();
        settings.isOn = _current.isON;
    }
}


public class LightSettings
{
    public float intensity;
    public float spotAngle;
    public Color color;
    public float accentRotation;
    public bool isOn;
}