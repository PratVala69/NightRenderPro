using UnityEngine;

/// <summary>
/// Solid completed measurement segment with floating distance label.
///
/// Label rule (via LabelPositionHelper):
///   Horizontal line → label ABOVE
///   Vertical line   → label RIGHT
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MeasurementLine : MonoBehaviour
{
    [Header("Label")]
    [Tooltip("World-space TMP label on the child Billboard canvas — pre-wired in prefab")]
    public TMPro.TextMeshPro labelText;

    [Tooltip("Distance the label sits away from the line midpoint (metres)")]
    public float labelOffset = 0.025f;

    [Header("Line Appearance")]
    public Color lineColor   = new Color(1f, 0.84f, 0.04f, 1f);
    public Color activeColor = new Color(1f, 0.84f, 0.04f, 0.5f);
    public float lineWidth   = 0.003f;

    [Header("End Cap Ticks")]
    public bool  showEndTicks = true;
    public float tickLength   = 0.012f;

    // ── Private ──────────────────────────────────────────────────────
    private LineRenderer _lr;
    private LineRenderer _tickA;
    private LineRenderer _tickB;
    private Vector3      _from;
    private Vector3      _to;
    private bool         _initialized;

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();

        if (showEndTicks)
        {
            _tickA = CreateTickRenderer();
            _tickB = CreateTickRenderer();
        }
    }

    private void LateUpdate()
    {
        if (!_initialized || labelText == null || Camera.main == null) return;

        // Re-evaluate every frame so label stays correct as camera moves
        labelText.transform.position = LabelPositionHelper.Compute(_from, _to, labelOffset);

        // Billboard: always face camera
        labelText.transform.LookAt(
            labelText.transform.position + Camera.main.transform.rotation * Vector3.forward,
            Camera.main.transform.rotation * Vector3.up);
    }

    // ── Public API ───────────────────────────────────────────────────

    public void SetPoints(Vector3 from, Vector3 to)
    {
        _from        = from;
        _to          = to;
        _initialized = true;
        Refresh();
    }

    public void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }

    // ── Internal ─────────────────────────────────────────────────────

    private void Refresh()
    {
        _lr.SetPosition(0, _from);
        _lr.SetPosition(1, _to);

        if (labelText != null)
            labelText.transform.position = LabelPositionHelper.Compute(_from, _to, labelOffset);

        if (showEndTicks)
        {
            Vector3 dir  = (_to - _from).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.up).normalized;
            if (perp == Vector3.zero)
                perp = Vector3.Cross(dir, Vector3.right).normalized;

            SetTick(_tickA, _from, perp);
            SetTick(_tickB, _to,   perp);
        }
    }

    private void SetTick(LineRenderer lr, Vector3 center, Vector3 perp)
    {
        if (lr == null) return;
        lr.SetPosition(0, center - perp * (tickLength * 0.5f));
        lr.SetPosition(1, center + perp * (tickLength * 0.5f));
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void ConfigureLineRenderer(LineRenderer lr, Color color, float width)
    {
        lr.positionCount     = 2;
        lr.startWidth        = width;
        lr.endWidth          = width;
        lr.startColor        = color;
        lr.endColor          = color;
        lr.useWorldSpace     = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.numCapVertices    = 4;
        lr.numCornerVertices = 4;
        lr.material          = CreateLineMaterial(color);
    }

    private LineRenderer CreateTickRenderer()
    {
        var go = new GameObject("Tick");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lr, lineColor, lineWidth);
        return lr;
    }

    private Material CreateLineMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            mat = new Material(Shader.Find("Unlit/Color"));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else                               mat.color = color;
        return mat;
    }
}
