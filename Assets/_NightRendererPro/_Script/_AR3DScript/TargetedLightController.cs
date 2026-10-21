using UnityEngine;

public class TargetedLightController : MonoBehaviour
{
    [Tooltip("Drag the specific Spot Light you want to reflect here.")]
    public Light targetSpotLight;

    // Reference to the Renderer, which holds the material instance
    private Renderer receiverRenderer;

    // Shader Property IDs (defined once for efficiency)
    // These names MUST match the CBUFFER names in the shader
    private static readonly int LightColorID = Shader.PropertyToID("_TargetLightColor");
    private static readonly int LightPositionID = Shader.PropertyToID("_TargetLightPosition");
    private static readonly int LightDirectionID = Shader.PropertyToID("_TargetLightDirection");
    private static readonly int LightAttenuationID = Shader.PropertyToID("_TargetLightAttenuation");

    void Awake()
    {
        // Get the Renderer component and ensure it exists
        receiverRenderer = GetComponent<Renderer>();
        if (receiverRenderer == null)
        {
            Debug.LogError("Renderer component not found on the Light Receiver Plane. Script disabled.");
            enabled = false;
        }
    }

    void Update()
    {
        // Access the unique material instance tied to this specific renderer.
        Material instanceMaterial = receiverRenderer.material;

        if (targetSpotLight == null || instanceMaterial == null)
            return;

        // --- 1. Calculate and Set Color/Direction/Position ---
        // Color * Intensity gives the true power of the light source
        Color lightEffectiveColor = targetSpotLight.color * targetSpotLight.intensity;

        instanceMaterial.SetColor(LightColorID, lightEffectiveColor);
        instanceMaterial.SetVector(LightPositionID, targetSpotLight.transform.position);
        instanceMaterial.SetVector(LightDirectionID, targetSpotLight.transform.forward);

        // --- 2. Calculate and Set Attenuation (Distance Falloff) ---
        float distance = Vector3.Distance(transform.position, targetSpotLight.transform.position);

        // This is a simplified inverse square falloff model.
        // It provides a realistic reduction in brightness over distance.
        float distanceSq = distance * distance;

        // Add a small constant to prevent division by zero near the light source.
        float attenuationFactor = 1.0f / (distanceSq + 0.0001f);

        // Optionally, multiply by a custom 'power boost' factor to make the light brighter
        // attenuationFactor *= 50.0f; 

        instanceMaterial.SetFloat(LightAttenuationID, attenuationFactor);
    }

    // --- Cleanup ---
    // Destroy the dynamically created material instance when the object is destroyed
    private void OnDestroy()
    {
        if (receiverRenderer != null && receiverRenderer.material != null)
        {
            // Destroy the material instance to prevent memory leaks
            Destroy(receiverRenderer.material);
        }
    }
}