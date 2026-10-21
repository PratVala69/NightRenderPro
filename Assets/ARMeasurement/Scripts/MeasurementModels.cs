using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// ── Enums ──────────────────────────────────────────────────────────────

public enum MeasurementUnit
{
    Centimeters,
    Meters,
    Inches
}

// ── Data Classes ───────────────────────────────────────────────────────

/// <summary>A single placed anchor point in world space.</summary>
public class MeasurementPoint
{
    public int        Index;
    public Vector3    WorldPosition;
    public ARAnchor   Anchor;   // AR world-locked anchor
    public GameObject Visual;   // Dot prefab instance
}

/// <summary>
/// A completed line segment between two MeasurementPoints.
/// Also owns the list of light prefab instances spawned along the segment
/// so they are destroyed correctly on Undo / Clear.
/// </summary>
public class MeasurementSegment
{
    public MeasurementPoint From;
    public MeasurementPoint To;
    public float            Distance;        // metres
    public GameObject       LineObject;      // LineRenderer wrapper GO
    public MeasurementLine  LineComponent;

    /// <summary>
    /// All light prefab GameObjects instantiated for this segment.
    /// Populated by ARMeasurementManager.SpawnLightsForSegment().
    /// Destroyed by DestroySegmentLights() on Undo / ClearAll.
    /// </summary>
    public List<GameObject> LightObjects = new List<GameObject>();
}

// ── Static Utilities ───────────────────────────────────────────────────

public static class MeasurementFormatter
{
    public static string Format(float metres, MeasurementUnit unit)
    {
        return unit switch
        {
            MeasurementUnit.Centimeters => $"{metres * 100f:F1} cm",
            MeasurementUnit.Meters      => $"{metres:F2} m",
            MeasurementUnit.Inches      => $"{metres * 39.3701f:F1} in",
            _                           => $"{metres:F2} m"
        };
    }

    public static string FormatArea(float sqMetres, MeasurementUnit unit)
    {
        return unit switch
        {
            MeasurementUnit.Centimeters => $"{sqMetres * 10000f:F0} cm²",
            MeasurementUnit.Meters      => $"{sqMetres:F3} m²",
            MeasurementUnit.Inches      => $"{sqMetres * 1550.0031f:F1} in²",
            _                           => $"{sqMetres:F3} m²"
        };
    }

    public static string UnitLabel(MeasurementUnit unit)
    {
        return unit switch
        {
            MeasurementUnit.Centimeters => "cm",
            MeasurementUnit.Meters      => "m",
            MeasurementUnit.Inches      => "in",
            _                           => "m"
        };
    }
}
