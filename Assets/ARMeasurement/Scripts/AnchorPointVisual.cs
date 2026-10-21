using UnityEngine;

/// <summary>
/// Animates a placed anchor point with a pulsing outer ring,
/// matching the iPhone Measure dot visual.
/// Attach to anchorPointPrefab.
/// </summary>
public class AnchorPointVisual : MonoBehaviour
{
    [Header("Visuals")]
    public Transform outerRing;
    public Transform innerDot;
    public Transform centerCore;

    [Header("Scale")]
    public float innerDotSize   = 0.008f;  // 8mm
    public float outerRingSize  = 0.018f;  // 18mm
    public float coreDotSize    = 0.003f;  // 3mm black center

    [Header("Pulse")]
    public float pulseSpeed     = 3f;
    public float pulseAmplitude = 0.2f;

    [Header("Colors")]
    public Color dotColor  = new Color(1f, 0.84f, 0.04f, 1f);    // gold
    public Color ringColor = new Color(1f, 0.84f, 0.04f, 0.35f); // semi-transparent

    private float _phase;

    private void Start()
    {
        _phase = Random.Range(0f, Mathf.PI * 2f); // stagger pulses across dots

        ApplyScale(innerDot,   innerDotSize);
        ApplyScale(outerRing,  outerRingSize);
        ApplyScale(centerCore, coreDotSize);

        SetRendererColor(innerDot,   dotColor);
        SetRendererColor(outerRing,  ringColor);
        SetRendererColor(centerCore, Color.black);
    }

    private void Update()
    {
        _phase += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(_phase) * pulseAmplitude;

        if (outerRing != null)
            outerRing.localScale = Vector3.one * outerRingSize * pulse;

        // Billboard: face camera
        if (Camera.main != null)
            transform.LookAt(Camera.main.transform);
    }

    private void ApplyScale(Transform t, float size)
    {
        if (t != null) t.localScale = Vector3.one * size;
    }

    private void SetRendererColor(Transform t, Color c)
    {
        if (t == null) return;
        var r = t.GetComponent<Renderer>();
        if (r == null) return;

        if (r.material.HasProperty("_BaseColor"))
            r.material.SetColor("_BaseColor", c);
        else if (r.material.HasProperty("_Color"))
            r.material.color = c;
    }
}
