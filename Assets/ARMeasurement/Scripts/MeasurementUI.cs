using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Drives all on-screen HUD elements:
///  - Scanning hint panel
///  - Live preview distance label
///  - Measurement chips list
///  - Area display
///  - Unit toggle button text
///  - Point count
/// Wire up all references in the Inspector.
/// </summary>
public class MeasurementUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject areaPanel;
    public GameObject objectMenu;
    public Animation objectMenuAnimator;

    [Header("Labels")]
    public TextMeshProUGUI scanHintText;
    public TextMeshProUGUI liveDistanceText;
    public TextMeshProUGUI areaText;
    public TextMeshProUGUI unitButtonText;
    public TextMeshProUGUI pointCountText;

    [Header("Chips Container")]
    [Tooltip("Scroll rect content transform — chips are instantiated here")]
    public Transform chipsContainer;
    public GameObject measurementChipPrefab;

    [Header("Buttons")]
    public Button addButton;
    public Button undoButton;
    public Button clearButton;
    public Button unitButton;
    public Button createButton;
    public Button cancelButton;

   [Header("Reticle Status")]
    public Image reticleStatusDot;
    //public Color dotReady   = new Color(0.20f, 0.78f, 0.35f);  // #34C759
    //public Color dotScanning = new Color(1f, 0.84f, 0.04f);    // #FFD60A

    private bool m_ShowingMoveDevice = true;
    private bool m_ShowingPlacePoint = false;

    [SerializeField]
    ARCameraManager m_CameraManager;
    public ARCameraManager cameraManager
    {
        get { return m_CameraManager; }
        set
        {
            if (m_CameraManager == value)
                return;

            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= FrameChanged;

            m_CameraManager = value;

            if (m_CameraManager != null & enabled)
                m_CameraManager.frameReceived += FrameChanged;
        }
    }

    private void OnEnable()
    {
        if (m_CameraManager != null)
            m_CameraManager.frameReceived += FrameChanged;
        _manager.onPlacedObject += PlacePoint;
    }

    private void OnDisable()
    {
        if (m_CameraManager != null)
            m_CameraManager.frameReceived -= FrameChanged;
        _manager.onPlacedObject -= PlacePoint;
    }

    private void FrameChanged(ARCameraFrameEventArgs args)
    {
        if (PlanesFound() && m_ShowingMoveDevice)
        {
            if (moveDeviceCanvasGroup)
                moveDeviceCanvasGroup.alpha = 0f;

            if (placePointCanvasGroup)
                placePointCanvasGroup.alpha = 1f;

            m_ShowingMoveDevice = false;
            m_ShowingPlacePoint = true;
        }
    }

    private bool PlanesFound()
    {
        if (planeManager == null)
            return false;

        return planeManager.trackables.count > 0;
    }

    private void PlacePoint()
    {
        if (m_ShowingPlacePoint)
        {
            if (placePointCanvasGroup)
                placePointCanvasGroup.alpha = 0f;

            m_ShowingPlacePoint = false;
        }
    }

    [SerializeField]
    ARPlaneManager m_PlaneManager;
    public ARPlaneManager planeManager { get { return m_PlaneManager; } set { m_PlaneManager = value; } }

    [SerializeField]
    CanvasGroup m_MoveDeviceCanvasGroup;
    public CanvasGroup moveDeviceCanvasGroup { get { return m_MoveDeviceCanvasGroup; } set { m_MoveDeviceCanvasGroup = value; } }

    [SerializeField]
    CanvasGroup m_PlacePointCanvasGroup;
    public CanvasGroup placePointCanvasGroup { get { return m_PlacePointCanvasGroup; } set { m_PlacePointCanvasGroup = value; } }

    // ── References set from manager ──────────────────────────────────
    private ARMeasurementManager _manager;

    bool m_ShowObjectMenu;

    private void Awake()
    {
        _manager = FindFirstObjectByType<ARMeasurementManager>();

        moveDeviceCanvasGroup.alpha = 1f;
        placePointCanvasGroup.alpha = 0f;

        addButton?.onClick.AddListener(() => _manager?.TryPlacePoint());
        undoButton?.onClick.AddListener(() => _manager?.UndoLastPoint());
        clearButton?.onClick.AddListener(() => _manager?.ClearAll());
        unitButton?.onClick.AddListener(() => _manager?.CycleUnit());

        createButton.onClick.AddListener(ShowMenu);
        cancelButton.onClick.AddListener(HideMenu);
    }

    public void ShowMenu()
    {
        m_ShowObjectMenu = true;
        objectMenu.SetActive(true);
        //if (!m_ObjectMenuAnimator.GetBool("Show"))
        //{
        //    m_ObjectMenuAnimator.SetBool("Show", true);
        //}
        //AdjustARDebugMenuPosition();

        objectMenuAnimator.Play("NewMenuShow");
    }

    public void HideMenu()
    {
        //m_ObjectMenuAnimator.SetBool("Show", false);
        objectMenuAnimator.Play("NewMenuHide");
        m_ShowObjectMenu = false;
        //AdjustARDebugMenuPosition();
    }

    // ── Scanning State ────────────────────────────────────────────────

    //public void ShowScanningHint(bool show)
    //{
    //    if (scanHintPanel != null)
    //        scanHintPanel.SetActive(show);

    //    if (reticleStatusDot != null)
    //        reticleStatusDot.color = show ? dotScanning : dotReady;
    //}

    public void SetReticleActive(bool active)
    {
        if (addButton != null)
            addButton.interactable = active;
    }

    // ── Live Distance ─────────────────────────────────────────────────

    public void UpdateLiveDistance(float metres, MeasurementUnit unit)
    {
        //if (liveDistancePanel != null)
        //    liveDistancePanel.SetActive(true);

        if (liveDistanceText != null)
            liveDistanceText.text = MeasurementFormatter.Format(metres, unit);
    }

    public void HideLiveDistance()
    {
        //if (liveDistancePanel != null)
        //    liveDistancePanel.SetActive(false);
    }

    // ── Measurement List ──────────────────────────────────────────────

    public void RefreshMeasurementList(List<MeasurementSegment> segments, MeasurementUnit unit)
    {
        if (chipsContainer == null) return;

        // Clear old chips
        foreach (Transform child in chipsContainer)
            Destroy(child.gameObject);

        foreach (var seg in segments)
        {
            if (measurementChipPrefab == null) break;
            var chip = Instantiate(measurementChipPrefab, chipsContainer);
            var label = chip.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = MeasurementFormatter.Format(seg.Distance, unit);
        }
    }

    // ── Area ──────────────────────────────────────────────────────────

    public void ShowArea(float sqMetres, MeasurementUnit unit)
    {
        if (areaPanel != null) areaPanel.SetActive(true);
        if (areaText != null)
            areaText.text = "Area: " + MeasurementFormatter.FormatArea(sqMetres, unit);
    }

    public void HideArea()
    {
        if (areaPanel != null) areaPanel.SetActive(false);
    }

    // ── Unit Button ───────────────────────────────────────────────────

    public void SetUnit(MeasurementUnit unit)
    {
        if (unitButtonText != null)
            unitButtonText.text = MeasurementFormatter.UnitLabel(unit);
    }

    // ── Point Count ───────────────────────────────────────────────────

    public void UpdatePointCount(int count)
    {
        if (pointCountText == null) return;
        pointCountText.text = count > 0 ? $"{count} pt{(count > 1 ? "s" : "")}" : "";

        if (undoButton != null)
            undoButton.interactable = count > 0;

        if (clearButton != null)
            clearButton.interactable = count > 0;
    }
}
