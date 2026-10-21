using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A hidden SelectableObject that represents ALL spawned instances of one
/// prefab category at once.
///
/// LightOptionHandler talks only to this proxy — it never knows about
/// individual instances. Every setter the proxy receives is immediately
/// broadcast to every registered instance, so all lights change together.
///
/// ARMeasurementManager:
///   • Keeps one CategoryProxy per prefab index (created on demand).
///   • Calls Register(instance) for each spawned object.
///   • Calls Unregister(instance) on Undo / ClearAll.
///   • Passes the proxy to lightOptionHandler.SelectObject(proxy) when
///     the Info button is pressed — no AR tap-to-select needed.
///
/// LightOptionHandler requires ZERO changes — it only knows SelectableObject.
/// </summary>
public class CategoryProxy : SelectableObject
{
    private readonly List<SelectableObject> _instances = new();

    // ── Registration ──────────────────────────────────────────────────

    public void Register(SelectableObject inst)
    {
        if (inst != null && !_instances.Contains(inst))
            _instances.Add(inst);
    }

    public void Unregister(SelectableObject inst) => _instances.Remove(inst);

    public int InstanceCount => _instances.Count;

    // ── Broadcast setters ─────────────────────────────────────────────
    // Each setter saves the value on the proxy itself (so LightOptionHandler
    // reads it back correctly on panel open) AND pushes it to all instances.

    public new void SetSavedSpotAngle(float angle)
    {
        base.SetSavedSpotAngle(angle);
        foreach (var i in _instances) if (i) i.SetSavedSpotAngle(angle);
    }

    public new void SetSavedIntensity(float intensity)
    {
        base.SetSavedIntensity(intensity);
        foreach (var i in _instances) if (i) i.SetSavedIntensity(intensity);
    }

    public new void SetSavedColor(Color color)
    {
        base.SetSavedColor(color);
        foreach (var i in _instances) if (i) i.SetSavedColor(color);
    }

    public new void SetAccentRotation(float normalized)
    {
        base.SetAccentRotation(normalized);
        foreach (var i in _instances) if (i) i.SetAccentRotation(normalized);
    }

    /// <summary>
    /// Toggles all instances to the same on/off state.
    /// Proxy's own isON drives the target state so they stay in sync.
    /// </summary>
    public new void ToggleLight()
    {
        // Flip the proxy's own state first
        base.ToggleLight();
        bool targetState = isON;

        // Force every instance to match — no unnecessary double-toggle
        foreach (var inst in _instances)
        {
            if (inst == null) continue;
            if (inst.isON != targetState)
                inst.ToggleLight();
        }
    }
}
