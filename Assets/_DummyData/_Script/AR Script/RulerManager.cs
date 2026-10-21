using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Networking;

public class RulerManager : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    private static List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    public Vector2 _centerVe;
    public Transform _camPivot;
    public Transform _pivot;
    public Transform _rulerPool;
    public GameObject _rulerObj;
    private RulerObj _nowRulerObj;
    private List<RulerObj> _rulerObjList = new List<RulerObj>();
    private bool _rulerEnable;
    private Vector3 _rulerPosSave;
    [SerializeField]
    [Tooltip("Minimum distance (inches) to trigger augmentation")]
    public GameObject defaultObjectPrefab; // Fallback prefab if FBX fails to load
    public GameObject[] _augmentObjectPrefab;
    public GameObject[] gutterObjectPrefab;
    private List<List<GameObject>> _augmentedObjects = new List<List<GameObject>>();
    private List<GameObject> instatiatedObjects = new List<GameObject>();
    public event Action onPlacedObject;
    public int objectIndex = -1;
    public int gutterObjectIndex = -1;
    public float snapThreshold = 0.02f;
    [Header("Move Closer function")]
    public Button addButton; // Reference to the Add button in the UI
    public Text moveCloserText; // Reference to the UI Text for "Move Closer" warning
    public GameObject moveCloserObject; // Reference to the UI Text for "Move Closer" warning
    public float MAX_DISTANCE_METERS = 3f; // Max allowed distance (3 meters)
    public float MIN_DISTANCE_METERS = 0.1f; // Min allowed distance (10 cm, to avoid too close)
    [Header("line placement warning function")]
    public Text lineWarningText;
    public PanelHideAfterFiveSec lineWarningObject;
    public ButtonForObjectRotation forObjectRotation;
    [Header("Line too short")]
    public PanelHideAfterFiveSec lineTooShortPanel;
    public FetchProductCategoryDataForAR arController; // Reference to AR controller
    public ProductSelectionData CurrentProductData; // Store the selected product data
    public List<Screenshot> screenshotList;
    public List<SessionProductForCreateSession> productDataList;
    [Header("Material Settings")]
    public Material baseMaterial; // Assign in Inspector
    private ARPlane hitPlane;

    public void SetProductData(ProductSelectionData productData)
    {
        CurrentProductData = productData;
        gutterObjectIndex = productData.Index; // Set the index for instantiation
        Debug.Log($"Product Data set: Index={gutterObjectIndex}, Product={productData.name}, Variant={productData.Variant?.size_name}");
        SetObjectIndex(gutterObjectIndex);
    }

    void Start()
    {
        _centerVe = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (moveCloserObject != null)
            moveCloserObject.gameObject.SetActive(false); // Hide "Move Closer" text initially
        UpdateAddButtonState(false); // Disable Add button initially
        productDataList = new List<SessionProductForCreateSession>(); // Initialize list
    }

    void OnAddProduct(GameObject spawnedObject, int size)
    {
        string selectedColor = arController.selectedColor;
        int colorId = 0; // Default value if no color is found
        if (!string.IsNullOrEmpty(selectedColor) && CurrentProductData.Variant?.color != null && CurrentProductData.Variant.color.Count > 0)
        {
            var selectedColorData = CurrentProductData.Variant.color.FirstOrDefault(c => c.color_name == selectedColor);
            if (selectedColorData != null)
            {
                colorId = selectedColorData.id; // Use color ID from selected variant
                Debug.Log($"Selected Color: {selectedColor}, Color ID: {colorId}");
            }
            else
            {
                Debug.LogWarning($"No color data found for selected color: {selectedColor}. Using default color_id: {colorId}");
            }
        }
        else
        {
            Debug.LogWarning("No valid color data or selected color is empty. Using default color_id: 0");
        }

        float parsedWidth = 0f;
        float.TryParse(CurrentProductData.Variant?.width, out parsedWidth);

        int parsedCost = 0;
        if (!string.IsNullOrEmpty(CurrentProductData.Variant?.price))
        {
            string cleanPrice = Regex.Replace(CurrentProductData.Variant.price, "[^0-9.]", "");
            if (float.TryParse(cleanPrice, out float costFloat))
            {
                parsedCost = Mathf.RoundToInt(costFloat);
            }
            else
            {
                Debug.LogWarning($"Failed to parse price: {CurrentProductData.Variant.price}, using default value 0");
            }
        }
        else
        {
            Debug.LogWarning("Price is null or empty, using default value 0");
        }

        var temp = new SessionProductForCreateSession
        {
            product_name = CurrentProductData.name,
            length = size,
            length_unit = CurrentProductData.Variant?.length_unit ?? "inch",
            width = parsedWidth,
            width_unit = CurrentProductData.Variant?.width_unit ?? "inch",
            color_id = colorId,
            quantity = 1,
            cost = parsedCost,
            screenshot = new List<Screenshot>(screenshotList),
            company_product_id = CurrentProductData.Variant?.company_product_id ?? 0
        };
        productDataList.Add(temp);
        screenshotList.Clear();
    }

    void Update()
    {
        bool isValidDistance = false;
        _rulerEnable = false;
        bool showMoveCloser = false;
        if (raycastManager != null && raycastManager.Raycast(_centerVe, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            if (raycastHits.Count > 0)
            {
                var hitPose = raycastHits[0].pose;
                float distanceToHit = Vector3.Distance(_camPivot.position, hitPose.position);
                hitPlane = raycastHits[0].trackable as ARPlane;
                if (distanceToHit >= MIN_DISTANCE_METERS && distanceToHit <= MAX_DISTANCE_METERS)
                {
                    _rulerEnable = true;
                    _rulerPosSave = hitPose.position;
                    _pivot.rotation = Quaternion.Lerp(_pivot.rotation, hitPose.rotation, 0.2f);
                    isValidDistance = true;
                    if (_nowRulerObj != null)
                    {
                        _nowRulerObj.SetObj(hitPose.position);
                    }
                    else
                    {
                        _rulerEnable = false;
                    }
                }
                else
                {
                    if (moveCloserObject != null)
                    {
                        moveCloserText.text = distanceToHit > MAX_DISTANCE_METERS ? "Move Closer" : "Move Back";
                        showMoveCloser = true;
                    }
                }
            }
        }
        if (moveCloserObject != null)
        {
            moveCloserObject.gameObject.SetActive(showMoveCloser);
        }
        UpdateAddButtonState(isValidDistance && objectIndex != -1 && gutterObjectIndex != -1);
    }

    private void UpdateAddButtonState(bool enable)
    {
        if (addButton != null)
        {
            addButton.interactable = enable;
        }
    }

    public void SetObjectIndex(int index)
    {
        objectIndex = index;
    }

    public void SetGutterObjectIndex(int index)
    {
        gutterObjectIndex = index;
    }

    public void MakeRulerObj()
    {
        if (_nowRulerObj == null) // First point
        {
            forObjectRotation.Buttons.SetActive(false);
            Debug.Log("Instantiating ruler object: " + _rulerObj.name);
            GameObject tobj = Instantiate(_rulerObj, Vector3.zero, Quaternion.identity, _rulerPool);
            tobj.transform.localScale = Vector3.one;
            RulerObj tobjST = tobj.GetComponent<RulerObj>();
            if (tobjST != null)
            {
                tobjST.mainCam = _camPivot;
                tobjST.SetInit(_rulerPosSave);
                _rulerObjList.Add(tobjST);
                _nowRulerObj = tobjST;
            }
            else
            {
                Debug.LogError("RulerObj component not found on instantiated ruler object!");
                Destroy(tobj);
            }
            if (onPlacedObject != null)
            {
                onPlacedObject();
            }
        }
        else // Second point: Draw line and instantiate 3D object
        {
            RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
            Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
            Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
            Vector3 centerPoint = (to + from) / 2f;
            Vector3 direction = to - from;
            float distanceMeters = Vector3.Magnitude(direction);
            float distanceInches = distanceMeters * 39.3701f;
            float distanceFeet = distanceInches / 12f;
            Debug.Log(
                "From A: " + from + " to B: " + to + "\n" +
                "Center of Line: " + centerPoint + "\n" +
                "Direction: " + direction + "\n" +
                "Distance (meters): " + distanceMeters + "\n" +
                "Distance (inches): " + distanceInches + "\n" +
                "Distance (feet): " + distanceFeet);
            LineType orientation = LineOrientation(from, to);
            if (objectIndex == 0)
            {
                if (orientation == LineType.Horizontal)
                {
                    lineWarningText.text = "Rain Chain should be placed vertically!";
                    lineWarningObject.ShowPanel();
                    Debug.LogWarning("Rain Chain should be placed vertically!");
                    return;
                }
                lineWarningObject.HidePanel();
                StartCoroutine(AugmentObjectBasedOnDistance_ForRainChain(distanceInches, centerPoint));
            }
            else if (objectIndex == 1)
            {
                if (orientation == LineType.Vertical)
                {
                    lineWarningText.text = "Gutter should be placed horizontally!";
                    lineWarningObject.ShowPanel();
                    Debug.LogWarning("Gutter should be placed horizontally!");
                    return;
                }
                lineWarningObject.HidePanel();
                StartCoroutine(AugmentObject_ForGutter(distanceInches, centerPoint));
            }
            else if (objectIndex == 2)
            {
                if (orientation == LineType.Horizontal)
                {
                    lineWarningText.text = "Downspout should be placed vertically!";
                    lineWarningObject.ShowPanel();
                    Debug.LogWarning("Downspout should be placed vertically!");
                    return;
                }
                lineWarningObject.HidePanel();
                StartCoroutine(AugmentObject_ForDownspout(distanceInches, centerPoint));
            }
            else if (objectIndex == 3)
            {
                if (orientation == LineType.Horizontal)
                {
                    lineWarningText.text = "PVC should be placed vertically!";
                    lineWarningObject.ShowPanel();
                    Debug.LogWarning("PVC should be placed vertically!");
                    return;
                }
                lineWarningObject.HidePanel();
                StartCoroutine(AugmentObjectBasedOnDistance_ForPVC(distanceInches, centerPoint));
            }
            else if (objectIndex == 4)
            {
                if (orientation == LineType.Vertical)
                {
                    lineWarningText.text = "Leaf Guard should be placed horizontally!";
                    lineWarningObject.ShowPanel();
                    Debug.LogWarning("Leaf Guard should be placed horizontally!");
                    return;
                }
                lineWarningObject.HidePanel();
                StartCoroutine(AugmentObject_ForLeafGuard(distanceInches, centerPoint));
            }
        }
    }

    private IEnumerator CalculateDynamicSpacing(string localFbxPath, string remoteFbxUrl, int objectIndex, int gutterObjectIndex, Action<float> onSpacingCalculated)
    {
        float spacingMeters = 0f;
        GameObject tempObject = null;
        yield return StartCoroutine(LoadFBXModel(localFbxPath, remoteFbxUrl, Vector3.zero, Quaternion.identity, null, null, (loadedObject) =>
        {
            tempObject = loadedObject;
        }));
        if (tempObject != null)
        {
            Vector3 bounds = CalculateModelBounds(tempObject);
            if (objectIndex == 1 || objectIndex == 3 || objectIndex == 4)
            {
                spacingMeters = bounds.z; // Horizontal objects (gutters, leaf guards, PVC)
            }
            else
            {
                spacingMeters = bounds.y; // Vertical objects (rain chains, downspouts)
            }
            Debug.Log($"Calculated spacing from model bounds: {spacingMeters:F3} meters (Axis: {(objectIndex == 1 || objectIndex == 3 || objectIndex == 4 ? "Z" : "Y")})");
            Destroy(tempObject);
        }
        else
        {
            // Fallback to hardcoded values
            if (objectIndex == 0)
                spacingMeters = 0.136f; // Rain chain
            else if (objectIndex == 1)
                spacingMeters = 0.27f; // Gutter
            else if (objectIndex == 3 || objectIndex == 4)
                spacingMeters = 0.29f; // PVC, Leaf Guard
            else if (objectIndex == 2)
                spacingMeters = (gutterObjectIndex == 1) ? 0.551f : 0.406f; // Downspout
            Debug.LogWarning($"Using fallback spacing: {spacingMeters:F3} meters");
        }
        onSpacingCalculated?.Invoke(spacingMeters);
    }

    private Vector3 CalculateModelBounds(GameObject model)
    {
        Vector3 boundsSize;
        Renderer renderer = model.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            boundsSize = renderer.bounds.size;
            Debug.Log($"Model Bounds Size (World Units, Object): {boundsSize}");
        }
        else
        {
            boundsSize = new Vector3(0, 0, 0);
            Debug.LogWarning($"No Renderer found on FBX model or its children (Object).");
        }
        return boundsSize;
    }

    private IEnumerator LoadFBXModel(string localFbxPath, string remoteFbxUrl, Vector3 position, Quaternion rotation, Transform parent,
        string hexColor, Action<GameObject> onModelLoaded)
    {
        if (!ObjectExistsLocally(localFbxPath))
        {
            Debug.Log($"Local FBX file not found at {localFbxPath}. Attempting to download from {remoteFbxUrl}.");
            yield return StartCoroutine(DownloadAndLoadObject(remoteFbxUrl, localFbxPath, () => { }));
        }
        if (!ObjectExistsLocally(localFbxPath))
        {
            Debug.LogError($"FBX file not found at: {localFbxPath}, using default prefab.");
            if (defaultObjectPrefab != null)
            {
                GameObject fallbackObject = Instantiate(defaultObjectPrefab, position, rotation, parent);
                ApplyMaterialToModel(fallbackObject, Color.white);
                onModelLoaded?.Invoke(fallbackObject);
            }
            else
            {
                onModelLoaded?.Invoke(null);
            }
            yield break;
        }
        //var assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
        //assetLoaderOptions.GenerateColliders = false;
        //assetLoaderOptions.AutomaticallyPlayLegacyAnimations = false;
        //AssetLoaderContext assetLoaderContext = null;
        //bool loadError = false;
        //AssetLoader.LoadModelFromFile(localFbxPath, (context) =>
        //{
        //    assetLoaderContext = context;
        //    Debug.Log("Model loaded. Loading materials.");
        //    if (context.RootGameObject != null)
        //    {
        //        context.RootGameObject.SetActive(false);
        //    }
        //}, (context) =>
        //{
        //    Debug.Log("Materials loaded. Model fully loaded.");
        //    if (!string.IsNullOrEmpty(hexColor) && ColorUtility.TryParseHtmlString(hexColor, out Color color))
        //    {
        //        ApplyMaterialToModel(context.RootGameObject, color);
        //    }
        //    else
        //    {
        //        Debug.LogWarning($"Invalid or missing hex color ({hexColor}) for model, applying default material.");
        //        ApplyMaterialToModel(context.RootGameObject, Color.white);
        //    }
        //    context.RootGameObject.SetActive(true);
        //}, (context, progress) =>
        //{
        //}, (errorContext) =>
        //{
        //    Debug.LogError($"An error occurred while loading your Model: {errorContext.GetInnerException()}");
        //    loadError = true;
        //}, null, assetLoaderOptions);
        //while (assetLoaderContext == null && !loadError)
        //{
        //    yield return null;
        //}
        //if (loadError || assetLoaderContext == null || assetLoaderContext.RootGameObject == null)
        //{
        //    Debug.LogError($"Failed to load FBX model from: {localFbxPath}");
        //    if (defaultObjectPrefab != null)
        //    {
        //        GameObject fallbackObject = Instantiate(defaultObjectPrefab, position, rotation, parent);
        //        onModelLoaded?.Invoke(fallbackObject);
        //    }
        //    else
        //    {
        //        onModelLoaded?.Invoke(null);
        //    }
        //    yield break;
        //}
        //GameObject loadedObject = assetLoaderContext.RootGameObject;
        //loadedObject.transform.position = position;
        //loadedObject.transform.rotation = rotation;
        //loadedObject.transform.SetParent(parent, false);
        //onModelLoaded?.Invoke(loadedObject);
    }

    private bool ObjectExistsLocally(string localPath)
    {
        return !string.IsNullOrEmpty(localPath) && System.IO.File.Exists(localPath);
    }

    private IEnumerator DownloadAndLoadObject(string remoteUrl, string localPath, Action onComplete)
    {
        if (string.IsNullOrEmpty(remoteUrl) || string.IsNullOrEmpty(localPath))
        {
            Debug.LogError("Remote URL or local path is empty.");
            onComplete?.Invoke();
            yield break;
        }
        using (UnityWebRequest www = UnityWebRequest.Get(remoteUrl))
        {
            www.timeout = 10;
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                byte[] data = www.downloadHandler.data;
                try
                {
                    System.IO.File.WriteAllBytes(localPath, data);
                    Debug.Log($"Object saved to local storage: {localPath}");
                    onComplete?.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to save object to {localPath}: {e.Message}");
                    onComplete?.Invoke();
                }
            }
            else
            {
                Debug.LogError($"Error downloading object from {remoteUrl}: {www.error}");
                onComplete?.Invoke();
            }
        }
    }

    private void ApplyMaterialToModel(GameObject model, Color color)
    {
        if (model == null)
        {
            Debug.LogError("Cannot apply material: Model is null.");
            return;
        }
        if (baseMaterial == null)
        {
            Debug.LogWarning("Base material not assigned, skipping material application.");
            return;
        }
        Material tintedMaterial = new Material(baseMaterial) { color = color };
        Renderer[] allRenderers = model.GetComponentsInChildren<Renderer>(true);
        if (allRenderers == null || allRenderers.Length == 0)
        {
            Debug.LogWarning($"No renderers found on model {model.name}. Material application skipped.");
            return;
        }
        Transform mainObjectTransform = model.transform
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name.Equals("Main_Object", System.StringComparison.OrdinalIgnoreCase));
        Renderer[] targetRenderers = mainObjectTransform != null
            ? mainObjectTransform.GetComponentsInChildren<Renderer>(true)
            : allRenderers;
        Debug.Log($"Applying material to {targetRenderers.Length} renderers under {(mainObjectTransform != null ? "'Main_Object'" : "all renderers")}.");
        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null || renderer.materials == null || renderer.materials.Length == 0)
            {
                Debug.LogWarning($"No valid materials found on renderer of {renderer?.gameObject.name ?? "null"}");
                continue;
            }
            Material[] currentMaterials = renderer.materials;
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                currentMaterials[i] = tintedMaterial;
            }
            renderer.materials = currentMaterials;
            Debug.Log($"Applied material to {renderer.gameObject.name} ({currentMaterials.Length} slots)");
        }
    }

    private IEnumerator AugmentObjectBasedOnDistance_ForRainChain(float distanceInches, Vector3 centerPoint)
    {
        if (string.IsNullOrEmpty(CurrentProductData?.Variant?.fbx_file) || string.IsNullOrEmpty(CurrentProductData?.localFbxPath))
        {
            Debug.LogWarning("No FBX file or URL specified for Rain Chain!");
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
        Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
        Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
        Vector3 direction = (to - from).normalized;
        float distanceMeters = distanceInches / 39.3701f;
        float spacingMeters = 0f;
        yield return StartCoroutine(CalculateDynamicSpacing(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, objectIndex, gutterObjectIndex, (spacing) =>
        {
            spacingMeters = spacing;
        }));
        spacingMeters -= 0.01f;
        float tempDistanceInches = spacingMeters * 39.3701f;
        if (distanceInches < (tempDistanceInches / 2f))
        {
            Debug.Log($"Line too short (<{tempDistanceInches / 2f:F2} inches), no objects instantiated.");
            lineTooShortPanel.ShowPanel();
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        int objectCount = Mathf.FloorToInt(distanceMeters / spacingMeters);
        float remainder = distanceInches % tempDistanceInches;
        //float tenInchesInMeters = 1.5f / 39.3701f;
        if (remainder > (tempDistanceInches / 2f))
        {
            objectCount += 1;
        }
        objectCount = Mathf.Max(1, objectCount);
        Debug.Log($"Spawning {objectCount} objects along line of length {distanceInches:F2} inches with spacing {spacingMeters * 39.3701f:F2} inches");
        List<GameObject> currentLineObjects = new List<GameObject>();
        Vector3 spawnPos = from;
        for (int i = 0; i < objectCount; i++)
        {
            if (i == 0)
            {
                spawnPos = from + direction * (spacingMeters * 0.5f);
            }
            else
            {
                spawnPos = spawnPos + direction * spacingMeters;
            }
            yield return StartCoroutine(LoadFBXModel(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, spawnPos, Quaternion.identity, _rulerPool,
                CurrentProductData.selectedColorHex, (loadedObject) =>
                {
                    if (loadedObject != null)
                    {
                        currentLineObjects.Add(loadedObject);
                        instatiatedObjects.Add(loadedObject);
                        if (i == objectCount - 1)
                        {
                            OnAddProduct(loadedObject, Mathf.RoundToInt(distanceInches));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load FBX model at index {i}.");
                    }
                }));
        }
        _augmentedObjects.Add(currentLineObjects);
        _nowRulerObj = null;
    }

    private IEnumerator AugmentObjectBasedOnDistance_ForPVC(float distanceInches, Vector3 centerPoint)
    {
        if (string.IsNullOrEmpty(CurrentProductData?.Variant?.fbx_file) || string.IsNullOrEmpty(CurrentProductData?.localFbxPath))
        {
            Debug.LogWarning("No FBX file or URL specified for PVC!");
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
        Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
        Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
        Vector3 direction = (to - from).normalized;
        float distanceMeters = distanceInches / 39.3701f;
        float spacingMeters = 0f;
        yield return StartCoroutine(CalculateDynamicSpacing(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, objectIndex, gutterObjectIndex, (spacing) =>
        {
            spacingMeters = spacing;
        }));
        float tempDistanceInches = spacingMeters * 39.3701f;
        if (distanceInches < (tempDistanceInches / 2f))
        {
            Debug.Log($"Line too short (<{tempDistanceInches / 2f:F2} inches), no objects instantiated.");
            lineTooShortPanel.ShowPanel();
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        int objectCount = Mathf.FloorToInt(distanceMeters / spacingMeters);

        //float remainder = distanceMeters % spacingMeters;
        //float tenInchesInMeters = 1.5f / 39.3701f;
        //if (remainder > (tempDistanceInches / 2f))
        //{
        //    objectCount += 1;
        //}
        float remainder = distanceInches % tempDistanceInches;
        if (remainder > (tempDistanceInches / 2f))
        {
            objectCount += 1;
        }

        objectCount = Mathf.Max(1, objectCount);
        Debug.Log($"Spawning {objectCount} objects along line of length {distanceInches:F2} inches with spacing {spacingMeters * 39.3701f:F2} inches");
        List<GameObject> currentLineObjects = new List<GameObject>();
        Vector3 spawnPos = from;
        for (int i = 0; i < objectCount; i++)
        {
            if (i == 0)
            {
                spawnPos = from + direction * (spacingMeters * 0.5f);
            }
            else
            {
                spawnPos = spawnPos + direction * spacingMeters;
            }
            yield return StartCoroutine(LoadFBXModel(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, spawnPos, Quaternion.identity, _rulerPool,
                CurrentProductData.selectedColorHex, (loadedObject) =>
                {
                    if (loadedObject != null)
                    {
                        currentLineObjects.Add(loadedObject);
                        instatiatedObjects.Add(loadedObject);
                        if (i == objectCount - 1)
                        {
                            OnAddProduct(loadedObject, Mathf.RoundToInt(distanceInches));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load FBX model at index {i}.");
                    }
                }));
        }
        _augmentedObjects.Add(currentLineObjects);
        _nowRulerObj = null;
    }

    private IEnumerator AugmentObject_ForGutter(float distanceInches, Vector3 centerPoint)
    {
        if (string.IsNullOrEmpty(CurrentProductData?.Variant?.fbx_file) || string.IsNullOrEmpty(CurrentProductData?.localFbxPath))
        {
            Debug.LogWarning("No FBX file or URL specified for Gutter!");
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
        Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
        Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
        Vector3 direction = (to - from).normalized;
        float distanceMeters = distanceInches / 39.3701f;
        float spacingMeters = 0f;
        yield return StartCoroutine(CalculateDynamicSpacing(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, objectIndex, gutterObjectIndex, (spacing) =>
        {
            spacingMeters = spacing;
        }));
        float tempDistance = spacingMeters * 39.3701f;
        if (distanceInches < (tempDistance / 2f))
        {
            Debug.Log($"Line too short (<{tempDistance / 2f:F2} inches), no objects instantiated.");
            lineTooShortPanel.ShowPanel();
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        int objectCount = Mathf.FloorToInt(distanceMeters / spacingMeters);
        float remainder = distanceInches % tempDistance;
        //float tenInchesInMeters = 7f / 39.3701f;
        if (remainder > (tempDistance/2f))
        {
            objectCount += 1;
        }
        objectCount = Mathf.Max(1, objectCount);
        Debug.Log($"Spawning {objectCount} objects along line of length {distanceInches:F2} inches with spacing {spacingMeters * 39.3701f:F2} inches");
        List<GameObject> currentLineObjects = new List<GameObject>();
        Vector3 spawnPos = from;

        // Determine if the line is drawn right-to-left relative to the camera
        ObjectPosition drawDirection = GetObjectPositionRelativeToCube(to - from);
        bool isRightToLeft = drawDirection == ObjectPosition.Left;
        Debug.Log($"Drawing direction: {(isRightToLeft ? "Right-to-Left" : "Left-to-Right")}");

        for (int i = 0; i < objectCount; i++)
        {
            if (i == 0)
            {
                spawnPos = from + direction * (spacingMeters * 0.5f);
            }
            else
            {
                spawnPos = spawnPos + direction * spacingMeters;
            }
            // Adjust rotation for right-to-left drawing
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (isRightToLeft)
            {
                rotation *= Quaternion.Euler(0, 180, 0); // Rotate 180 degrees around Y-axis
            }
            yield return StartCoroutine(LoadFBXModel(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, spawnPos, rotation, _rulerPool,
                CurrentProductData.selectedColorHex, (loadedObject) =>
                {
                    if (loadedObject != null)
                    {
                        currentLineObjects.Add(loadedObject);
                        instatiatedObjects.Add(loadedObject);
                        if (i == objectCount - 1)
                        {
                            OnAddProduct(loadedObject, Mathf.RoundToInt(distanceInches));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load FBX model at index {i}.");
                    }
                }));
        }
        _augmentedObjects.Add(currentLineObjects);
        _nowRulerObj = null;
    }

        private IEnumerator AugmentObject_ForLeafGuard(float distanceInches, Vector3 centerPoint)
    {
        if (string.IsNullOrEmpty(CurrentProductData?.Variant?.fbx_file) || string.IsNullOrEmpty(CurrentProductData?.localFbxPath))
        {
            Debug.LogWarning("No FBX file or URL specified for Leaf Guard!");
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
        Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
        Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
        Vector3 direction = (to - from).normalized;
        float distanceMeters = distanceInches / 39.3701f;
        float spacingMeters = 0f;
        yield return StartCoroutine(CalculateDynamicSpacing(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, objectIndex, gutterObjectIndex, (spacing) =>
        {
            spacingMeters = spacing;
        }));
        float tempDistance = spacingMeters * 39.3701f;
        if (distanceInches < (tempDistance / 2f))
        {
            Debug.Log($"Line too short (<{tempDistance / 2f:F2} inches), no objects instantiated.");
            lineTooShortPanel.ShowPanel();
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        int objectCount = Mathf.FloorToInt(distanceMeters / spacingMeters);
        float remainder = distanceInches % tempDistance;
        //float tenInchesInMeters = 7f / 39.3701f;
        if (remainder > (tempDistance / 2f))
        {
            objectCount += 1;
        }
        objectCount = Mathf.Max(1, objectCount);
        Debug.Log($"Spawning {objectCount} leaf guards along line of length {distanceInches:F2} inches with spacing {spacingMeters * 39.3701f:F2} inches");
        List<GameObject> currentLineObjects = new List<GameObject>();
        Vector3 spawnPos = from;
        ObjectPosition drawDirection = GetObjectPositionRelativeToCube(to - from);
        bool isRightToLeft = drawDirection == ObjectPosition.Left;
        Debug.Log($"Leaf Guard drawing direction: {(isRightToLeft ? "Right-to-Left" : "Left-to-Right")}");
        Vector3 modelRotationOffset = new Vector3(0, 0, 0); // Adjust if leaf guard model needs rotation
        for (int i = 0; i < objectCount; i++)
        {
            if (i == 0)
            {
                spawnPos = from + direction * (spacingMeters * 0.5f);
            }
            else
            {
                spawnPos = spawnPos + direction * spacingMeters;
            }
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (isRightToLeft)
            {
                rotation *= Quaternion.Euler(0, 180, 0); // Flip for right-to-left
            }
            StartCoroutine(LoadFBXModel(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, spawnPos, rotation, _rulerPool,
                CurrentProductData.selectedColorHex, (loadedObject) =>
                {
                    if (loadedObject != null)
                    {
                        currentLineObjects.Add(loadedObject);
                        instatiatedObjects.Add(loadedObject);
                        if (i == objectCount - 1)
                        {
                            OnAddProduct(loadedObject, Mathf.RoundToInt(distanceInches));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load leaf guard FBX model at index {i}.");
                    }
                }));
        }
        _augmentedObjects.Add(currentLineObjects);
        _nowRulerObj = null;
    }

    private IEnumerator AugmentObject_ForDownspout(float distanceInches, Vector3 centerPoint)
    {
        if (string.IsNullOrEmpty(CurrentProductData?.Variant?.fbx_file) || string.IsNullOrEmpty(CurrentProductData?.localFbxPath))
        {
            Debug.LogWarning("No FBX file or URL specified for Downspout!");
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
        Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
        Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
        Vector3 direction = (to - from).normalized;
        float distanceMeters = distanceInches / 39.3701f;
        float spacingMeters = 0f;
        yield return StartCoroutine(CalculateDynamicSpacing(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, objectIndex, gutterObjectIndex, (spacing) =>
        {
            spacingMeters = spacing;
        }));
        float tempDistanceInch = spacingMeters * 39.3701f;
        if (distanceInches < (tempDistanceInch / 2f))
        {
            Debug.Log($"Line too short (<{tempDistanceInch / 2f:F2} inches), no objects instantiated.");
            lineTooShortPanel.ShowPanel();
            _augmentedObjects.Add(null);
            _nowRulerObj = null;
            yield break;
        }
        int objectCount = Mathf.FloorToInt(distanceMeters / spacingMeters);
        float remainder = distanceInches % tempDistanceInch;
        //float tenInchesInMeters = 10f / 39.3701f;
        if (remainder > (tempDistanceInch / 2f))
        {
            objectCount += 1;
        }
        objectCount = Mathf.Max(1, objectCount);
        Debug.Log($"Spawning {objectCount} objects along line of length {distanceInches:F2} inches with spacing {spacingMeters * 39.3701f:F2} inches");
        List<GameObject> currentLineObjects = new List<GameObject>();
        if (gutterObjectIndex == 1)
        {
            Vector3 spawnPos = from;
            for (int i = 0; i < objectCount; i++)
            {
                if (i == 0)
                {
                    spawnPos = from + direction * (spacingMeters * 0.5f);
                }
                else
                {
                    spawnPos = spawnPos + direction * spacingMeters;
                }
                yield return StartCoroutine(LoadFBXModel(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, spawnPos, Quaternion.identity, _rulerPool,
                    CurrentProductData.selectedColorHex, (loadedObject) =>
                    {
                        if (loadedObject != null)
                        {
                            currentLineObjects.Add(loadedObject);
                            instatiatedObjects.Add(loadedObject);
                            if (i != (objectCount - 1))
                            {
                                Transform snapEnd = loadedObject.transform.Find("SnapEnd");
                                if (snapEnd != null)
                                {
                                    snapEnd.gameObject.SetActive(false);
                                }
                            }
                            if (i == objectCount - 1)
                            {
                                OnAddProduct(loadedObject, Mathf.RoundToInt(distanceInches));
                            }
                        }
                    }));
            }
            if (currentLineObjects.Count > 0)
            {
                for (int i = 1; i < currentLineObjects.Count; i++)
                {
                    TrySnap(currentLineObjects[i - 1], currentLineObjects[i]);
                }
                GameObject lastObject = currentLineObjects[currentLineObjects.Count - 1];
                float closestDistance = Mathf.Infinity;
                GameObject closestObject = null;
                foreach (GameObject obj in instatiatedObjects)
                {
                    if (obj == lastObject) continue;
                    Transform snapEnd = obj.transform.Find("SnapEnd");
                    if (snapEnd == null || !snapEnd.gameObject.activeInHierarchy) continue;
                    Transform newSnapStart = lastObject.transform.Find("SnapStart");
                    float distance = Vector3.Distance(snapEnd.position, newSnapStart.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestObject = obj;
                    }
                }
                if (closestObject != null && closestDistance <= snapThreshold)
                {
                    TrySnap(closestObject, lastObject);
                }
            }
            _augmentedObjects.Add(currentLineObjects);
        }
        else
        {
            Vector3 spawnPos = from;
            Vector3 verticalDirection = (to - from).normalized;
            Vector3 wallNormal = hitPlane.transform.forward;
            if (Mathf.Abs(wallNormal.y) > 0.3f)
            {
                Debug.LogWarning("Plane may not be vertical.");
            }
            Quaternion rawRotation = Quaternion.LookRotation(wallNormal, verticalDirection);
            Vector3 euler = rawRotation.eulerAngles;
            euler.z = 0f;
            euler.x = 0f;
            euler.y = euler.y - 90f;
            Quaternion finalRotation = Quaternion.Euler(euler);
            yield return StartCoroutine(LoadFBXModel(CurrentProductData.localFbxPath, CurrentProductData.Variant.fbx_file, spawnPos, finalRotation, _rulerPool,
                CurrentProductData.selectedColorHex, (loadedObject) =>
                {
                    if (loadedObject != null)
                    {
                        forObjectRotation.augmentedObject = loadedObject;
                        forObjectRotation.Buttons.SetActive(true);
                        forObjectRotation.RotateObjectHintEnable();
                        currentLineObjects.Add(loadedObject);
                        instatiatedObjects.Add(loadedObject);
                        OnAddProduct(loadedObject, Mathf.RoundToInt(distanceInches));
                        Transform newSnapStart = loadedObject.transform.Find("SnapStart");
                        if (newSnapStart != null)
                        {
                            float closestDistance = Mathf.Infinity;
                            GameObject closestObject = null;
                            foreach (GameObject obj in instatiatedObjects)
                            {
                                if (obj == loadedObject) continue;
                                if (obj == null || !obj) continue;
                                Transform snapEnd = obj.transform.Find("SnapEnd");
                                if (snapEnd == null || !snapEnd.gameObject.activeInHierarchy) continue;
                                float distance = Vector3.Distance(snapEnd.position, newSnapStart.position);
                                if (distance < closestDistance)
                                {
                                    closestDistance = distance;
                                    closestObject = obj;
                                }
                            }
                            if (closestObject != null && closestDistance <= snapThreshold)
                            {
                                TrySnap(closestObject, loadedObject);
                            }
                        }
                    }
                }));
            _augmentedObjects.Add(currentLineObjects);
        }
        _nowRulerObj = null;
    }

    void TrySnap(GameObject currentPiece, GameObject newPiece)
    {
        Transform currentEnd = currentPiece.transform.Find("SnapEnd");
        Transform newStart = newPiece.transform.Find("SnapStart");
        if (currentEnd != null && newStart != null && Vector3.Distance(currentEnd.position, newStart.position) < snapThreshold)
        {
            Vector3 offset = currentEnd.position - newStart.position;
            newPiece.transform.position += offset;
            Quaternion targetRotation = Quaternion.LookRotation(currentEnd.forward);
            newPiece.transform.rotation = targetRotation;
            newPiece.transform.SetParent(currentPiece.transform);
            currentEnd.gameObject.SetActive(false);
            newStart.gameObject.SetActive(false);
        }
    }

    public void ClickUndo()
    {
        if (_rulerObjList.Count == 0) return;

        int lastIndex = _rulerObjList.Count - 1;

        // Remove product data only if it exists
        if (productDataList.Count > lastIndex)
        {
            productDataList.RemoveAt(lastIndex);
        }

        // Destroy ruler
        if (_rulerObjList[lastIndex] != null)
        {
            Destroy(_rulerObjList[lastIndex].gameObject);
        }
        _rulerObjList.RemoveAt(lastIndex);

        // Remove augmented objects
        if (_augmentedObjects.Count > lastIndex)
        {
            var objects = _augmentedObjects[lastIndex];
            if (objects != null)
            {
                foreach (var obj in objects)
                {
                    if (obj != null)
                    {
                        instatiatedObjects.Remove(obj);
                        Destroy(obj);
                    }
                }
            }
            _augmentedObjects.RemoveAt(lastIndex);
        }

        instatiatedObjects.RemoveAll(obj => obj == null);
        _nowRulerObj = null;
    }

    public GameObject GetLastAugmentedObject()
    {
        if (_augmentedObjects.Count > 0 && _augmentedObjects[_augmentedObjects.Count - 1] != null && _augmentedObjects[_augmentedObjects.Count - 1].Count > 0)
        {
            return _augmentedObjects[_augmentedObjects.Count - 1][0];
        }
        return null;
    }

    public enum LineType { Horizontal, Vertical, Diagonal }

    public LineType LineOrientation(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        float verticalThreshold = 0.9f;
        float horizontalThreshold = 0.9f;
        float yComponent = Mathf.Abs(direction.y);
        float xzComponent = new Vector2(direction.x, direction.z).magnitude;
        if (yComponent >= verticalThreshold && xzComponent < (1f - verticalThreshold))
        {
            Debug.Log("Line is Vertical");
            return LineType.Vertical;
        }
        else if (xzComponent >= horizontalThreshold && yComponent < (1f - horizontalThreshold))
        {
            Debug.Log("Line is Horizontal");
            return LineType.Horizontal;
        }
        else
        {
            Debug.Log("Line is Diagonal");
            return LineType.Diagonal;
        }
    }

    [Header("Cube Reference")]
    public GameObject cube;

    public enum ObjectPosition
    {
        Front,
        Back,
        Left,
        Right,
        Centered
    }

    public ObjectPosition GetObjectPositionRelativeToCube(Vector3 objectPosition)
    {
        Vector3 cameraRight = Camera.main.transform.right;
        float dot = Vector3.Dot(objectPosition, cameraRight);
        if (dot > 0)
        {
            Debug.Log("Object's front is facing to the **right** side of the camera.");
            return ObjectPosition.Right;
        }
        else if (dot < 0)
        {
            Debug.Log("Object's front is facing to the **left** side of the camera.");
            return ObjectPosition.Left;
        }
        else
        {
            Debug.Log("Object's front is directly perpendicular to the camera's right vector.");
            return ObjectPosition.Centered;
        }
    }

    public void ReplaceProduct()
    {
        if (_rulerObjList.Count > 0 && _augmentedObjects.Count > 0)
        {
            RulerObj lastRuler = _rulerObjList[_rulerObjList.Count - 1];
            Vector3 from = lastRuler.objList[lastRuler.objList.Count - 2].transform.position;
            Vector3 to = lastRuler.objList[lastRuler.objList.Count - 1].transform.position;
            Vector3 centerPoint = (to + from) / 2f;
            float distanceInches = Vector3.Magnitude(to - from) * 39.3701f;
            List<GameObject> lastLineObjects = _augmentedObjects[_augmentedObjects.Count - 1];
            if (lastLineObjects != null)
            {
                foreach (GameObject obj in lastLineObjects)
                {
                    if (obj != null)
                    {
                        Destroy(obj);
                    }
                }
                _augmentedObjects.RemoveAt(_augmentedObjects.Count - 1);
            }
            if (objectIndex == 0)
            {
                StartCoroutine(AugmentObjectBasedOnDistance_ForRainChain(distanceInches, centerPoint));
            }
            else if (objectIndex == 1)
            {
                StartCoroutine(AugmentObject_ForGutter(distanceInches, centerPoint));
            }
            else if (objectIndex == 2)
            {
                StartCoroutine(AugmentObject_ForDownspout(distanceInches, centerPoint));
            }
            else if (objectIndex == 3)
            {
                StartCoroutine(AugmentObjectBasedOnDistance_ForPVC(distanceInches, centerPoint));
            }
            else if (objectIndex == 4)
            {
                StartCoroutine(AugmentObject_ForLeafGuard(distanceInches, centerPoint));
            }
        }
    }
}

[System.Serializable]
public class ProductDataForAR
{
    public string name;
    public string size;
    public string color;
}