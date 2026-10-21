using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dashed live preview line stretching from a fixed start to the moving reticle.
///
/// Label rule (via LabelPositionHelper — same as MeasurementLine):
///   Horizontal line → label ABOVE
///   Vertical line   → label RIGHT
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class LivePreviewLine : MonoBehaviour
{
    [Header("Line Appearance")]
    public Color previewColor = new Color(1f, 0.84f, 0.04f, 0.6f);
    public float lineWidth    = 0.002f;

    [Header("Dash Pattern")]
    public float dashLength = 0.015f;
    public float gapLength  = 0.010f;

    [Header("Distance Label")]
    [Tooltip("World-space TMP on the child LabelCanvas — pre-wired in prefab")]
    public TextMeshPro labelTMP;

    [Tooltip("Background Image on the pill — pre-wired in prefab")]
    public Image labelBackground;

    [Tooltip("Must match MeasurementLine.labelOffset so both labels sit at same distance")]
    public float labelOffset = 0.025f;

    [Tooltip("Pill background color")]
    public Color pillColor = new Color(0.05f, 0.05f, 0.05f, 0.80f);

    [Tooltip("Label text color")]
    public Color textColor = Color.white;

    // ── Private ────────────────────────────────────────────────────
    private LineRenderer _lr;
    private Vector3      _startPos;
    private Vector3      _endPos;
    private bool         _initialized;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        SetLabelVisible(false);
    }

    private void LateUpdate()
    {
        if (!_initialized) return;
        RebuildDash();
        UpdateLabelTransform();
    }

    // ── Public API ─────────────────────────────────────────────────

    public void Init(Vector3 startPos)
    {
        _startPos    = startPos;
        _endPos      = startPos;
        _initialized = true;
        SetLabelVisible(false);
    }

    public void UpdateEndPoint(Vector3 endPos, string distanceText)
    {
        _endPos = endPos;

        bool show = Vector3.Distance(_startPos, _endPos) > 0.005f;
        SetLabelVisible(show);

        if (show && labelTMP != null)
            labelTMP.text = distanceText;
    }

    // ── Dash builder ───────────────────────────────────────────────

    private void RebuildDash()
    {
        Vector3 dir   = _endPos - _startPos;
        float   total = dir.magnitude;

        if (total < 0.001f) { _lr.positionCount = 0; return; }

        Vector3 unit  = dir / total;
        float   step  = dashLength + gapLength;
        int     count = Mathf.Max(1, Mathf.CeilToInt(total / step));
        var     pts   = new System.Collections.Generic.List<Vector3>(count * 2);

        float cursor = 0f;
        while (cursor < total)
        {
            float end = Mathf.Min(cursor + dashLength, total);
            pts.Add(_startPos + unit * cursor);
            pts.Add(_startPos + unit * end);
            cursor += step;
        }

        _lr.positionCount = pts.Count;
        _lr.SetPositions(pts.ToArray());
    }

    // ── Label transform ────────────────────────────────────────────

    private void UpdateLabelTransform()
    {
        if (labelTMP == null || Camera.main == null) return;

        // Same helper as MeasurementLine — guaranteed identical behaviour
        Vector3 pos = LabelPositionHelper.Compute(_startPos, _endPos, labelOffset);

        Transform canvasRoot = labelTMP.transform.parent;
        canvasRoot.position = pos;

        // Billboard
        canvasRoot.LookAt(
            pos + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up);
    }

    private void SetLabelVisible(bool visible)
    {
        if (labelTMP        != null) labelTMP.gameObject.SetActive(visible);
        if (labelBackground != null) labelBackground.gameObject.SetActive(visible);
    }
}
