using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR Measurement Manager with UI-driven object selection.
///
/// Object Selection flow:
///   • Each button in the Object Menu scroll view calls SelectLightPrefab(index).
///   • The selected button gets a gold highlight; others reset to white.
///   • If the user tries to finalize a segment (second tap) while nothing is
///     selected, a warning panel is shown: "Please select an object first."
///   • Tapping the same button again deselects it (toggle).
///
/// Placement flow:
///   Tap 1 → START point + live preview line (no selection required)
///   Tap 2 → gates on HasSelection → END point → segment → tile selected prefab
/// </summary>
public class ARMeasurementManager : MonoBehaviour
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // INSPECTOR
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Header("AR Foundation")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public ARAnchorManager anchorManager;

    [Header("Measurement Prefabs")]
    public GameObject anchorPointPrefab;
    public GameObject measurementLinePrefab;
    public GameObject livePreviewLinePrefab;
    public GameObject reticlePrefab;

    // ── Object Selection ─────────────────────────────────────────────

    [Header("Object Selection")]

    [Tooltip("All spawnable light prefabs.\n" +
             "Index 0 maps to objectMenuButtons[0], index 1 to objectMenuButtons[1], etc.\n" +
             "Wire each button's OnClick → ARMeasurementManager.SelectLightPrefab(N).")]
    public List<GameObject> lightPrefabs = new();

    [Tooltip("Buttons in the Object Menu scroll view — same order as lightPrefabs.\n" +
             "Selected button turns gold; others reset to white.")]
    public List<Button> objectMenuButtons = new();

    //[Tooltip("Color applied to the selected button's Image component.")]
    //public Color selectedButtonColor = new Color(1f, 0.84f, 0.04f, 1f);

    //[Tooltip("Color applied to all other (deselected) button Image components.")]
    //public Color deselectedButtonColor = Color.white;

    // ── No-Selection Warning ──────────────────────────────────────────

    [Header("No-Selection Warning")]

    [Tooltip("Panel shown when user tries to place a segment end-point without\n" +
             "having selected a light object. Add a TextMeshProUGUI child for the message.")]
    public GameObject noSelectionPanel;

    [Tooltip("TMP label inside the warning panel. Optional — leave null if the\n" +
             "panel has a fixed text baked in.")]
    public TextMeshProUGUI noSelectionText;

    [Tooltip("Seconds before the warning auto-hides. Set 0 to leave it open until dismissed.")]
    public float noSelectionDuration = 2.5f;

    // ── Dynamic Spawning ──────────────────────────────────────────────

    [Header("Object Spawning")]
    [Tooltip("World-space offset added to every spawned object position.")]
    public Vector3 spawnOffset = Vector3.zero;

    public enum SpacingAxis { Z, Y, X, Longest }

    [Tooltip("Axis of the prefab's renderer bounds used as the tiling step.")]
    public SpacingAxis spacingAxis = SpacingAxis.Longest;

    [Tooltip("Subtracted from the bounds size so pieces slightly overlap.")]
    public float overlapReduction = 0.01f;

    [Tooltip("Fallback spacing (metres) if no renderer is found on the prefab.")]
    public float fallbackSpacingMeters = 0.3f;

    [Tooltip("Flip 180° around Y when the line is drawn right-to-left.")]
    public bool flipForRightToLeft = true;

    [Tooltip("Optional parent transform for spawned objects.")]
    public Transform objectPool;

    // ── Light Option Handler ──────────────────────────────────────────

    [Header("Light Option Handler")]
    public LightOptionHandler lightOptionHandler;

    // ── UI & Settings ─────────────────────────────────────────────────

    [Header("UI")]
    public MeasurementUI measurementUI;

    [Header("Settings")]
    public int maxSegments = 20;
    public float snapThreshold = 0.03f;

    public event Action onPlacedObject;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PRIVATE STATE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private readonly List<MeasurementPoint> _points = new();
    private readonly List<MeasurementSegment> _segments = new();
    private readonly List<ARRaycastHit> _hits = new();

    private MeasurementPoint _pendingStart = null;
    private LivePreviewLine _previewLine = null;
    private GameObject _previewLineGO = null;
    private GameObject _reticleInstance;
    private bool _isPlacementValid;
    private Pose _currentHitPose;
    private MeasurementUnit _currentUnit = MeasurementUnit.Centimeters;

    // -1 = nothing selected
    private int _selectedPrefabIndex = 0;
    private Coroutine _hideWarningCoroutine;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void Awake()
    {
        raycastManager ??= FindFirstObjectByType<ARRaycastManager>();
        planeManager ??= FindFirstObjectByType<ARPlaneManager>();
        anchorManager ??= FindFirstObjectByType<ARAnchorManager>();
        lightOptionHandler ??= FindFirstObjectByType<LightOptionHandler>();

        if (lightOptionHandler == null)
            Debug.LogWarning("[ARMeasurement] LightOptionHandler not found — info buttons won't open a panel.");
    }

    private void Start()
    {
        SpawnReticle();
        measurementUI?.SetUnit(_currentUnit);

        if (planeManager != null)
            planeManager.trackablesChanged.AddListener(OnPlanesChanged);

        SetNoSelectionPanelActive(false);
        //RefreshButtonHighlights();
    }

    private void Update()
    {
        UpdateReticle();
        HandleInput();
    }

    private void OnDestroy()
    {
        if (planeManager != null)
            planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        ClearAll();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // OBJECT SELECTION  (called from Object Menu button OnClick events)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Select the prefab at position <paramref name="index"/> in lightPrefabs.
    /// Wire each Object Menu button's OnClick → SelectLightPrefab(N).
    /// Tapping the same button again toggles it off.
    /// </summary>
    public void SelectLightPrefab(int index)
    {
        if (index < -1 || index >= lightPrefabs.Count)
        {
            Debug.LogWarning($"[ARMeasurement] SelectLightPrefab({index}) is out of range " +
                             $"(lightPrefabs has {lightPrefabs.Count} entries).");
            return;
        }

        // Toggle off if already selected
        _selectedPrefabIndex = (index == _selectedPrefabIndex) ? -1 : index;

        string label = HasSelection ? lightPrefabs[_selectedPrefabIndex].name : "none";
        Debug.Log($"[ARMeasurement] Selected prefab: [{_selectedPrefabIndex}] {label}");

        //RefreshButtonHighlights();

        // If user just picked something, dismiss any lingering warning
        if (HasSelection)
            SetNoSelectionPanelActive(false);
    }

    /// <summary>True when a valid prefab is currently selected.</summary>
    public bool HasSelection =>
        _selectedPrefabIndex >= 0 &&
        _selectedPrefabIndex < lightPrefabs.Count &&
        lightPrefabs[_selectedPrefabIndex] != null;

    /// <summary>The currently selected prefab, or null.</summary>
    public GameObject SelectedPrefab =>
        HasSelection ? lightPrefabs[_selectedPrefabIndex] : null;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // BUTTON HIGHLIGHT
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    //private void RefreshButtonHighlights()
    //{
    //    for (int i = 0; i < objectMenuButtons.Count; i++)
    //    {
    //        if (objectMenuButtons[i] == null) continue;
    //        Image img = objectMenuButtons[i].GetComponent<Image>();
    //        if (img == null) continue;
    //        img.color = (i == _selectedPrefabIndex) ? selectedButtonColor : deselectedButtonColor;
    //    }
    //}

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // NO-SELECTION WARNING
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void ShowNoSelectionWarning()
    {
        if (noSelectionText != null)
            noSelectionText.text = "Please select an object from the menu first.";

        SetNoSelectionPanelActive(true);

        if (noSelectionDuration > 0f)
        {
            if (_hideWarningCoroutine != null)
                StopCoroutine(_hideWarningCoroutine);
            _hideWarningCoroutine = StartCoroutine(HideWarningAfterDelay(noSelectionDuration));
        }
    }

    private IEnumerator HideWarningAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetNoSelectionPanelActive(false);
        _hideWarningCoroutine = null;
    }

    private void SetNoSelectionPanelActive(bool active)
    {
        if (noSelectionPanel != null)
            noSelectionPanel.SetActive(active);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // RETICLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void SpawnReticle()
    {
        if (reticlePrefab == null) return;
        _reticleInstance = Instantiate(reticlePrefab);
        _reticleInstance.SetActive(false);
    }

    private void UpdateReticle()
    {
        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);

        _isPlacementValid = raycastManager.Raycast(
            screenCenter, _hits,
            TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds);

        if (_isPlacementValid && _hits.Count > 0)
        {
            _currentHitPose = _hits[0].pose;

            if (_reticleInstance != null)
            {
                _reticleInstance.SetActive(true);
                _reticleInstance.transform.SetPositionAndRotation(
                    _currentHitPose.position, _currentHitPose.rotation);
            }

            measurementUI?.SetReticleActive(true);

            if (_pendingStart != null)
            {
                float liveDist = Vector3.Distance(
                    _pendingStart.WorldPosition, _currentHitPose.position);
                string distText = MeasurementFormatter.Format(liveDist, _currentUnit);
                _previewLine?.UpdateEndPoint(_currentHitPose.position, distText);
                measurementUI?.UpdateLiveDistance(liveDist, _currentUnit);
            }
            else
            {
                measurementUI?.HideLiveDistance();
            }
        }
        else
        {
            if (_reticleInstance != null) _reticleInstance.SetActive(false);
            measurementUI?.SetReticleActive(false);
            measurementUI?.HideLiveDistance();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // INPUT
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void HandleInput()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryPlacePoint();
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            if (!EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                TryPlacePoint();
#endif
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PLACEMENT
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void TryPlacePoint()
    {
        if (!_isPlacementValid) return;

        if (_pendingStart == null)
        {
            // ── ODD TAP: place START — no selection required yet ───────
            if (_segments.Count >= maxSegments) return;

            MeasurementPoint start = SpawnPoint(_currentHitPose);
            _points.Add(start);
            _pendingStart = start;

            SpawnPreviewLine(start.WorldPosition);
            measurementUI?.UpdatePointCount(_points.Count);
        }
        else
        {
            // ── EVEN TAP: place END — selection required ───────────────
            if (!HasSelection)
            {
                ShowNoSelectionWarning();
                return;   // hold the pending start; user must pick an object first
            }

            if (Vector3.Distance(_pendingStart.WorldPosition, _currentHitPose.position)
                < snapThreshold) return;

            MeasurementPoint end = SpawnPoint(_currentHitPose);
            _points.Add(end);

            DestroyPreviewLine();

            MeasurementSegment seg = FinalizeSegment(_pendingStart, end);

            // Tile the currently selected prefab along the segment
            StartCoroutine(SpawnObjectsForSegment(seg, SelectedPrefab));

            _pendingStart = null;

            measurementUI?.RefreshMeasurementList(_segments, _currentUnit);
            measurementUI?.UpdatePointCount(_points.Count);
            measurementUI?.HideLiveDistance();
        }

        onPlacedObject?.Invoke();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // POINT SPAWNING
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private MeasurementPoint SpawnPoint(Pose pose)
    {
        ARAnchor anchor = null;
        if (anchorManager != null)
        {
            var go = new GameObject("MeasurementAnchor");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            anchor = go.AddComponent<ARAnchor>();
        }

        GameObject visual = null;
        if (anchorPointPrefab != null)
        {
            visual = Instantiate(anchorPointPrefab, pose.position, pose.rotation);
            if (anchor != null)
                visual.transform.SetParent(anchor.transform, true);
        }

        return new MeasurementPoint
        {
            WorldPosition = pose.position,
            Anchor = anchor,
            Visual = visual,
            Index = _points.Count
        };
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // PREVIEW LINE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void SpawnPreviewLine(Vector3 startPos)
    {
        if (livePreviewLinePrefab == null) return;
        _previewLineGO = Instantiate(livePreviewLinePrefab);
        _previewLine = _previewLineGO.GetComponent<LivePreviewLine>();
        _previewLine?.Init(startPos);
    }

    private void DestroyPreviewLine()
    {
        if (_previewLineGO != null)
        {
            Destroy(_previewLineGO);
            _previewLineGO = null;
            _previewLine = null;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // SEGMENT FINALIZATION
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private MeasurementSegment FinalizeSegment(MeasurementPoint from, MeasurementPoint to)
    {
        GameObject lineGO = null;
        MeasurementLine lineComp = null;

        if (measurementLinePrefab != null)
        {
            lineGO = Instantiate(measurementLinePrefab);
            lineComp = lineGO.GetComponent<MeasurementLine>();

            if (lineComp != null)
            {
                float dist = Vector3.Distance(from.WorldPosition, to.WorldPosition);
                string label = MeasurementFormatter.Format(dist, _currentUnit);
                lineComp.SetPoints(from.WorldPosition, to.WorldPosition);
                lineComp.SetLabel(label);
            }
        }

        var seg = new MeasurementSegment
        {
            From = from,
            To = to,
            Distance = Vector3.Distance(from.WorldPosition, to.WorldPosition),
            LineObject = lineGO,
            LineComponent = lineComp
        };

        _segments.Add(seg);
        return seg;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DYNAMIC SPACING
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IEnumerator CalculateDynamicSpacing(GameObject prefab, Action<float> onSpacingCalculated)
    {
        float spacing = fallbackSpacingMeters;
        GameObject temp = Instantiate(prefab, Vector3.down * 1000f, Quaternion.identity);
        temp.SetActive(true);

        yield return null;  // one frame — lets renderer bounds initialise

        Renderer rend = temp.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Vector3 size = rend.bounds.size;
            Debug.Log($"[ARMeasurement] Bounds of {prefab.name}: x={size.x:F3} y={size.y:F3} z={size.z:F3}");

            spacing = spacingAxis switch
            {
                SpacingAxis.Z => size.z,
                SpacingAxis.Y => size.y,
                SpacingAxis.X => size.x,
                SpacingAxis.Longest => Mathf.Max(size.x, size.y, size.z),
                _ => size.z
            };

            spacing = Mathf.Max(0.001f, spacing - overlapReduction);
            Debug.Log($"[ARMeasurement] Spacing = {spacing:F3} m");
        }
        else
        {
            Debug.LogWarning($"[ARMeasurement] No Renderer on {prefab.name} — fallback {fallbackSpacingMeters:F3} m");
        }

        Destroy(temp);
        onSpacingCalculated?.Invoke(spacing);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // OBJECT TILING
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IEnumerator SpawnObjectsForSegment(MeasurementSegment seg, GameObject prefab)
    {
        if (prefab == null) yield break;

        float spacingMeters = fallbackSpacingMeters;
        yield return StartCoroutine(CalculateDynamicSpacing(prefab, s => spacingMeters = s));

        Vector3 from = seg.From.WorldPosition;
        Vector3 to = seg.To.WorldPosition;
        Vector3 direction = (to - from).normalized;
        float distance = seg.Distance;

        float distanceInches = distance * 39.3701f;
        float spacingInches = spacingMeters * 39.3701f;

        if (distanceInches < spacingInches * 0.5f)
        {
            Debug.Log($"[ARMeasurement] Line too short ({distanceInches:F1} in) — no objects spawned.");
            yield break;
        }

        int objectCount = Mathf.FloorToInt(distance / spacingMeters);
        float remainder = distanceInches % spacingInches;
        if (remainder > spacingInches * 0.5f) objectCount++;
        objectCount = Mathf.Max(1, objectCount);

        Debug.Log($"[ARMeasurement] Spawning {objectCount}× {prefab.name}");

        bool isRightToLeft = flipForRightToLeft && Camera.main != null &&
                             Vector3.Dot(to - from, Camera.main.transform.right) < 0f;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        if (isRightToLeft) rotation *= Quaternion.Euler(0f, 180f, 0f);

        Vector3 spawnPos = from;

        for (int i = 0; i < objectCount; i++)
        {
            spawnPos = (i == 0)
                ? from + direction * (spacingMeters * 0.5f)
                : spawnPos + direction * spacingMeters;

            GameObject obj = Instantiate(prefab, spawnPos + spawnOffset, rotation, objectPool);
            seg.LightObjects.Add(obj);

            // Wire info button → LightOptionHandler (only on tap, not at spawn)
            SelectableObject sel = obj.GetComponent<SelectableObject>();
            if (sel != null)
            {
                sel.prefabReference = prefab;

                if (sel != null && lightOptionHandler != null)
                {
                    var settings = lightOptionHandler.GetSettingsForPrefab(prefab);

                    if (settings != null)
                    {
                        sel.SetSavedIntensity(settings.intensity);
                        sel.SetSavedSpotAngle(settings.spotAngle);
                        sel.SetSavedColor(settings.color);
                        sel.SetAccentRotation(settings.accentRotation);
                        sel.SetLightState(settings.isOn);
                    }
                }
                //Button infoBtn = obj.GetComponentInChildren<Button>(includeInactive: true);
                //if (infoBtn != null && lightOptionHandler != null)
                //{
                //    SelectableObject captured = sel;
                //    infoBtn.onClick.RemoveAllListeners();
                //    infoBtn.onClick.AddListener(() => lightOptionHandler.SelectObject(captured));
                //}
            }

            yield return null;
        }

        Debug.Log($"[ARMeasurement] Done — {seg.LightObjects.Count} objects.");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DESTROY HELPERS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void DestroySegmentObjects(MeasurementSegment seg)
    {
        foreach (var obj in seg.LightObjects)
        {
            if (obj == null) continue;
            var sel = obj.GetComponent<SelectableObject>();
            if (sel != null && lightOptionHandler != null)
                lightOptionHandler.SelectObject(null);
            Destroy(obj);
        }
        seg.LightObjects.Clear();
    }

    private void DestroyPoint(MeasurementPoint pt)
    {
        if (pt == null) return;
        if (pt.Visual != null) { Destroy(pt.Visual); pt.Visual = null; }
        if (pt.Anchor != null) { SafeDestroyAnchor(pt.Anchor); pt.Anchor = null; }
    }

    private void SafeDestroyAnchor(ARAnchor anchor)
    {
        if (anchor == null) return;
        if (anchorManager != null && anchorManager.isActiveAndEnabled)
        {
            try { anchorManager.TryRemoveAnchor(anchor); }
            catch { Destroy(anchor.gameObject); }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UNDO
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void UndoLastPoint()
    {
        if (_points.Count == 0) return;

        if (_pendingStart != null)
        {
            DestroyPreviewLine();
            DestroyPoint(_pendingStart);
            _points.Remove(_pendingStart);
            _pendingStart = null;
        }
        else if (_segments.Count > 0)
        {
            var lastSeg = _segments[^1];

            if (lastSeg.LineObject != null) Destroy(lastSeg.LineObject);
            DestroySegmentObjects(lastSeg);
            _segments.RemoveAt(_segments.Count - 1);

            DestroyPoint(lastSeg.To);
            _points.Remove(lastSeg.To);

            _pendingStart = lastSeg.From;
            SpawnPreviewLine(_pendingStart.WorldPosition);

            float liveDist = Vector3.Distance(
                _pendingStart.WorldPosition, _currentHitPose.position);
            measurementUI?.UpdateLiveDistance(liveDist, _currentUnit);
        }
        else if (_points.Count > 0)
        {
            DestroyPoint(_points[^1]);
            _points.RemoveAt(_points.Count - 1);
        }

        measurementUI?.RefreshMeasurementList(_segments, _currentUnit);
        measurementUI?.UpdatePointCount(_points.Count);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // CLEAR ALL
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void ClearAll()
    {
        DestroyPreviewLine();
        _pendingStart = null;

        foreach (var seg in _segments)
        {
            if (seg.LineObject != null) Destroy(seg.LineObject);
            DestroySegmentObjects(seg);
        }
        _segments.Clear();

        foreach (var pt in _points.ToArray())
            DestroyPoint(pt);
        _points.Clear();

        lightOptionHandler?.SelectObject(null);

        measurementUI?.RefreshMeasurementList(_segments, _currentUnit);
        measurementUI?.UpdatePointCount(0);
        measurementUI?.HideLiveDistance();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UNIT CYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void CycleUnit()
    {
        _currentUnit = _currentUnit switch
        {
            MeasurementUnit.Centimeters => MeasurementUnit.Meters,
            MeasurementUnit.Meters => MeasurementUnit.Inches,
            _ => MeasurementUnit.Centimeters
        };

        foreach (var seg in _segments)
            seg.LineComponent?.SetLabel(
                MeasurementFormatter.Format(seg.Distance, _currentUnit));

        measurementUI?.SetUnit(_currentUnit);
        measurementUI?.RefreshMeasurementList(_segments, _currentUnit);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // HELPERS
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args) { }

    public List<MeasurementSegment> GetAllSegments()
    {
        return _segments;
    }
}