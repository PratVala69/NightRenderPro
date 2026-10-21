using UnityEngine;

[ExecuteInEditMode]
public class VirtualLightController_Multi : MonoBehaviour
{
    [System.Serializable]
    public class VirtualLight
    {
        public Light lightSource;
        [Range(0, 10)] public float intensityMultiplier = 1f;
        public bool enabled = true;
    }

    [Header("Virtual Lights (drag Point/Spot Lights)")]
    public VirtualLight[] virtualLights = new VirtualLight[0];

    [Header("Target Materials (AR Mesh Materials)")]
    public Material[] targetMaterials;

    private const int MAX_LIGHTS = 32;
    private static readonly int ID_LightCount = Shader.PropertyToID("_VirtualLightCount");
    private static readonly int ID_LightPos = Shader.PropertyToID("_VirtualLightPos");
    private static readonly int ID_LightColor = Shader.PropertyToID("_VirtualLightColor");
    private static readonly int ID_LightIntensity = Shader.PropertyToID("_VirtualLightIntensity");

    void Update()
    {
        if (targetMaterials == null || targetMaterials.Length == 0) return;

        int count = 0;
        Vector4[] pos = new Vector4[MAX_LIGHTS];
        Color[] col = new Color[MAX_LIGHTS];
        float[] intensity = new float[MAX_LIGHTS];

        foreach (var vl in virtualLights)
        {
            if (count >= MAX_LIGHTS) break;
            if (vl == null || vl.lightSource == null || !vl.enabled) continue;

            pos[count] = vl.lightSource.transform.position;
            pos[count].w = 1; // mark as active

            col[count] = vl.lightSource.color;
            intensity[count] = vl.lightSource.intensity * vl.intensityMultiplier;

            count++;
        }

        foreach (var mat in targetMaterials)
        {
            mat.SetInt(ID_LightCount, count);
            mat.SetVectorArray(ID_LightPos, pos);
            mat.SetColorArray(ID_LightColor, col);
            mat.SetFloatArray(ID_LightIntensity, intensity);
        }
    }

    // Editor: Auto-populate materials from ARMeshVisualizer
#if UNITY_EDITOR
    [ContextMenu("Auto-Assign AR Mesh Materials")]
    void AssignARMaterials()
    {
        var visualizers = FindObjectsOfType<ARMeshVisualizer>();
        System.Collections.Generic.List<Material> mats = new System.Collections.Generic.List<Material>();
        foreach (var v in visualizers)
        {
            if (v.meshMaterial) mats.Add(v.meshMaterial);
            if (v.wireframeMaterial) mats.Add(v.wireframeMaterial);
        }
        targetMaterials = mats.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}