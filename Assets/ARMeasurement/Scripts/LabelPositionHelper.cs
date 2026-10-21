using UnityEngine;

/// <summary>
/// Single source of truth for label placement on AR measurement lines.
///
/// Rule:
///   Horizontal line on screen  →  label appears ABOVE  (camera-up direction)
///   Vertical line on screen    →  label appears RIGHT   (camera-right direction)
///
/// "Horizontal" means the line's screen-projection is wider than it is tall.
/// The threshold is 45°: |projR| >= |projU| = horizontal, else vertical.
///
/// Both MeasurementLine and LivePreviewLine call this — guaranteeing
/// identical behaviour at all times.
/// </summary>
public static class LabelPositionHelper
{
    /// <param name="from">World-space start point of the line.</param>
    /// <param name="to">World-space end point of the line.</param>
    /// <param name="offset">World-space distance to push the label from the midpoint.</param>
    /// <returns>World-space position for the label.</returns>
    public static Vector3 Compute(Vector3 from, Vector3 to, float offset)
    {
        Vector3 mid = (from + to) * 0.5f;

        // Fallback when no camera exists (e.g. unit tests, editor without play mode)
        if (Camera.main == null)
            return mid + Vector3.up * offset;

        Vector3 lineDir = (to - from).normalized;
        if (lineDir == Vector3.zero)
            return mid + Vector3.up * offset;

        Transform cam    = Camera.main.transform;
        Vector3 camRight = cam.right;  // screen X+
        Vector3 camUp    = cam.up;     // screen Y+

        // Project line direction onto the camera's screen plane
        // Use Abs so direction of travel doesn't matter (A→B same as B→A)
        float projR = Mathf.Abs(Vector3.Dot(lineDir, camRight)); // horizontal-ness
        float projU = Mathf.Abs(Vector3.Dot(lineDir, camUp));    // vertical-ness

        // Choose offset axis based on dominant screen direction
        Vector3 offsetDir = (projR >= projU)
            ? camUp       // horizontal line → push ABOVE
            : camRight;   // vertical line   → push RIGHT

        return mid + offsetDir.normalized * offset;
    }
}
