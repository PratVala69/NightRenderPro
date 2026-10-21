using UnityEngine;
using VLB;

[ExecuteInEditMode]
public class SelectableObject : MonoBehaviour
{
    [Header("Light References")]
    public Light[] lights;
    public VolumetricLightBeamSD[] volumetricLights;
    public Light[] pointLights;

    public Light[] lightsForIntensity;
    public VolumetricLightBeamSD[] volumetricLightsForIntensity;

    public Light[] lightsForSpotangle;
    public VolumetricLightBeamSD[] volumetricLightsForSpotangle;

    public GameObject[] gameObjects;
    public MeshRenderer[] meshRendMaterials;

    [Header("Limits")]
    public float minSpotAngle = 35f;
    public float maxSpotAngle = 135f;
    public float defaultSpotAngle = 60f;

    public float minIntensity = 1f;
    public float maxIntensity = 10f;
    public float defaultIntensity = 5f;

    // ────────────────────────────────
    // ACCENT LIGHT – PHYSICAL ROTATION
    // ────────────────────────────────
    [Header("Accent Light - Physical Rotation")]
    public bool isAccentLight = false;
    public Transform accentRotationPivot;           // Drag the moving lamp head here
    public float minRotation = -45f;
    public float maxRotation = 45f;
    public Vector3 rotationAxis = Vector3.right;    // X = tilt, Y = pan, Z = roll

    [SerializeField] private float savedAccentRotationNormalized = 0.5f; // 0–1
    private Quaternion initialLocalRotation;

    [Header("Persistent Saved Values")]
    [SerializeField] private float savedSpotAngle = -1f;
    [SerializeField] private float savedIntensity = -1f;
    private Color savedColor = new Color32(248, 254, 3, 255);

    [Header("Info")]
    public string lightModelName = "Spot Light";
    public string lightName = "Ceiling Lamp";
    public string lightValue = "3000K • 800lm";

    public bool isON = false;
    public bool isSelected = false;

    public GameObject prefabReference;   // Assign in prefab or when spawning
    public bool isGroupInstance;         // runtime flag

    private void Awake()
    {
        InitializeDefaults();

        // Store initial rotation of the moving part (so we can rotate relative to it)
        if (isAccentLight && accentRotationPivot != null)
            initialLocalRotation = accentRotationPivot.localRotation;
    }

    private void Start()
    {
        if (!isGroupInstance)
        {
            TurnOffAllLights();
            ApplySavedValues();
        }
        // Apply saved physical rotation for accent lights
        if (isAccentLight) ApplyAccentRotation();
    }

    private void InitializeDefaults()
    {
        if (savedSpotAngle < 0f) savedSpotAngle = defaultSpotAngle;
        if (savedIntensity < 0f) savedIntensity = defaultIntensity;
        if (savedColor == Color.black || savedColor.a == 0f) savedColor = Color.white;

        // Default accent rotation = middle
        if (savedAccentRotationNormalized < 0f)
            savedAccentRotationNormalized = 0.5f;
    }

    private void TurnOffAllLights()
    {
        SetLightsActive(false);
    }

    public void ToggleLight()
    {
        isON = !isON;
        SetLightsActive(isON);
    }

    public void SetLightState(bool state)
    {
        isON = state;
        SetLightsActive(state);
    }

    private void SetLightsActive(bool active)
    {
        foreach (var l in lights) if (l) l.gameObject.SetActive(active);
        foreach (var v in volumetricLights) if (v) v.gameObject.SetActive(active);
        foreach (var l in pointLights) if (l) l.gameObject.SetActive(active);
        foreach (var l in lightsForIntensity) if (l) l.gameObject.SetActive(active);
        foreach (var v in volumetricLightsForIntensity) if (v) v.gameObject.SetActive(active);
        foreach (var l in lightsForSpotangle) if (l) l.gameObject.SetActive(active);
        foreach (var v in volumetricLightsForSpotangle) if (v) v.gameObject.SetActive(active);
        foreach (var g in gameObjects) if (g) g.SetActive(active);
    }

    public void SetSelected(bool selected) => isSelected = selected;

    // ────────────────────────────────
    // PUBLIC SETTERS (called by UI)
    // ────────────────────────────────
    public void SetSavedSpotAngle(float angle)
    {
        savedSpotAngle = Mathf.Clamp(angle, minSpotAngle, maxSpotAngle);
        ApplySpotAngle();
    }

    public void SetSavedIntensity(float intensity)
    {
        savedIntensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);
        ApplyIntensity();
    }

    public void SetSavedColor(Color color)
    {
        savedColor = color;
        ApplyColor();
    }

    // NEW: Called by LightOptionHandler when slider moves
    public void SetAccentRotation(float normalized01)
    {
        if (!isAccentLight || accentRotationPivot == null) return;

        savedAccentRotationNormalized = Mathf.Clamp01(normalized01);

        float angle = Mathf.Lerp(minRotation, maxRotation, normalized01);

        // ----- NEW: Apply rotation ONLY on the selected axis -----
        Vector3 e = initialLocalRotation.eulerAngles;

        if (rotationAxis.x != 0) e.x = angle;
        if (rotationAxis.y != 0) e.y = angle;
        if (rotationAxis.z != 0) e.z = angle;

        accentRotationPivot.localRotation = Quaternion.Euler(e);
    }

    // NEW: For UI refresh
    public float GetAccentRotationNormalized() => savedAccentRotationNormalized;

    // ────────────────────────────────
    // GETTERS
    // ────────────────────────────────
    public float GetCurrentSpotAngle() => savedSpotAngle;
    public float GetCurrentIntensity() => savedIntensity;
    public Color GetCurrentColor() => savedColor;

    // ────────────────────────────────
    // APPLYERS
    // ────────────────────────────────
    private void ApplySavedValues()
    {
        ApplySpotAngle();
        ApplyIntensity();
        ApplyColor();
    }

    private void ApplySpotAngle()
    {
        foreach (var v in volumetricLights) if (v) { v.spotAngle = savedSpotAngle; v.UpdateAfterManualPropertyChange(); }
        foreach (var v in volumetricLightsForSpotangle) if (v) { v.spotAngle = savedSpotAngle; v.UpdateAfterManualPropertyChange(); }
        foreach (var l in lights)
        {
            if (l && l.type == LightType.Spot)
            {
                l.spotAngle = savedSpotAngle;
#if UNITY_2020_1_OR_NEWER
                l.innerSpotAngle = Mathf.Max(savedSpotAngle - 30f, 10f);
#endif
            }
        }
        foreach (var l in lightsForSpotangle)
        {
            if (l)
            {
                l.spotAngle = savedSpotAngle;
#if UNITY_2020_1_OR_NEWER
                l.innerSpotAngle = Mathf.Max(savedSpotAngle - 30f, 10f);
#endif
            }
        }
        foreach (var l in pointLights) if (l) l.range = savedSpotAngle;
    }

    private void ApplyIntensity()
    {
        foreach (var v in volumetricLights) if (v) { v.intensityInside = v.intensityOutside = savedIntensity; v.UpdateAfterManualPropertyChange(); }
        foreach (var v in volumetricLightsForIntensity) if (v) { v.intensityInside = v.intensityOutside = savedIntensity; v.UpdateAfterManualPropertyChange(); }
        foreach (var l in lights) if (l) l.intensity = savedIntensity;
        foreach (var l in pointLights) if (l) l.intensity = savedIntensity;
        foreach (var l in lightsForIntensity) if (l) l.intensity = savedIntensity;
    }

    private void ApplyColor()
    {
        foreach (var v in volumetricLights) if (v) { v.color = savedColor; v.UpdateAfterManualPropertyChange(); }
        foreach (var v in volumetricLightsForIntensity) if (v) { v.color = savedColor; v.UpdateAfterManualPropertyChange(); }
        foreach (var v in volumetricLightsForSpotangle) if (v) { v.color = savedColor; v.UpdateAfterManualPropertyChange(); }
        foreach (var l in lights) if (l) l.color = savedColor;
        foreach (var l in pointLights) if (l) l.color = savedColor;
        foreach (var l in lightsForIntensity) if (l) l.color = savedColor;
        foreach (var l in lightsForSpotangle) if (l) l.color = savedColor;

        foreach (var go in gameObjects)
            if (go && go.TryGetComponent<Light>(out var extra)) extra.color = savedColor;

        foreach (var rend in meshRendMaterials)
            if (rend)
            {
                if (!rend.material.name.Contains("(Instance)"))
                    rend.material = new Material(rend.material);
                rend.material.color = savedColor;
            }
    }

    // NEW: Apply saved rotation on Start / when re-selected
    private void ApplyAccentRotation()
    {
        if (!isAccentLight || accentRotationPivot == null) return;

        float angle = Mathf.Lerp(minRotation, maxRotation, savedAccentRotationNormalized);

        Vector3 e = initialLocalRotation.eulerAngles;

        if (rotationAxis.x != 0) e.x = angle;
        if (rotationAxis.y != 0) e.y = angle;
        if (rotationAxis.z != 0) e.z = angle;

        accentRotationPivot.localRotation = Quaternion.Euler(e);
    }
}