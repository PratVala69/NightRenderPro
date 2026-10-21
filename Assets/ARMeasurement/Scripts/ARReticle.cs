using UnityEngine;

/// <summary>
/// Animates the placement reticle dot shown at the center of the screen.
/// Mimics the pulsing ring from iPhone Measure.
/// Attach to the reticlePrefab root.
/// </summary>
public class ARReticle : MonoBehaviour
{
    [Header("Rings")]
    [Tooltip("The outer animated ring transform")]
    public Transform outerRing;

    [Tooltip("The inner solid dot transform")]
    public Transform innerDot;

    [Header("Animation")]
    public float pulseSpeed = 2.5f;
    public float pulseAmplitude = 0.15f;   // ±15% scale pulse
    public float baseOuterScale = 0.06f;   // metres
    public float baseInnerScale = 0.015f;

    [Header("Colors")]
    public Color readyColor   = new Color(1f, 0.84f, 0.04f, 0.9f);
    public Color scanningColor = new Color(1f, 1f, 1f, 0.5f);

    private Renderer _outerRenderer;
    private Renderer _innerRenderer;
    private bool _isReady;
    private float _phase;

    private void Awake()
    {
        if (outerRing != null) _outerRenderer = outerRing.GetComponent<Renderer>();
        if (innerDot != null)  _innerRenderer  = innerDot.GetComponent<Renderer>();

        //SetReady(false);
    }

    private void Update()
    {
        _phase += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(_phase) * pulseAmplitude;

        if (outerRing != null)
            outerRing.localScale = Vector3.one * baseOuterScale * pulse;

        if (innerDot != null)
            innerDot.localScale = Vector3.one * baseInnerScale;

        // Lay flat on the detected plane surface
        //transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    /// <summary>Switch visual state when a plane is / isn't detected.</summary>
    public void SetReady(bool ready)
    {
        _isReady = ready;
        Color c = ready ? readyColor : scanningColor;

        if (_outerRenderer != null)
            SetColor(_outerRenderer, c);

        if (_innerRenderer != null)
            SetColor(_innerRenderer, ready ? readyColor : new Color(1, 1, 1, 0.8f));
    }

    private void SetColor(Renderer r, Color c)
    {
        // Works for both URP and Built-in RP
        if (r.material.HasProperty("_BaseColor"))
            r.material.SetColor("_BaseColor", c);
        else if (r.material.HasProperty("_Color"))
            r.material.color = c;
    }
}
