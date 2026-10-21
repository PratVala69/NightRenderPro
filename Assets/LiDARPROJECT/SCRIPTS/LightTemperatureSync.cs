using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(Light))]
public class LightTemperatureSync : MonoBehaviour
{

    [Range(1800f, 7000f)]
    public float temperature = 6500f;
    
    public Color filter = Color.white;
    
    public float filterPriority = 0.5f;

    private Light _light;

    private void Awake()
    {
        _light = GetComponent<Light>();
        UpdateLight();
    }

    private void OnValidate()
    {
        if (_light == null)
            _light = GetComponent<Light>();

        UpdateLight();
    }

    void UpdateLight()
    {
        Color temperatureColor = TemperatureToRGB(temperature);

        Color finalColor = Color.Lerp(
            temperatureColor,
            filter,
            filterPriority
        );

        _light.color = finalColor;
    }
    public void SetColorFilter(Color color)
    {
        filter = color;
        UpdateLight();
    }
    public void SetTemperature(Slider slider)
    {
        temperature = slider.value;
        UpdateLight();
    }

    public void SetFilterPriority(Slider slider)
    {
        filterPriority = slider.value;
        UpdateLight();
    }

    private Color TemperatureToRGB(float kelvin)
    {
        float temp = kelvin / 100f;
        float r, g, b;

        // Red
        if (temp <= 66) r = 255;
        else
        {
            r = temp - 60;
            r = 329.698727446f * Mathf.Pow(r, -0.1332047592f);
        }

        // Green
        if (temp <= 66)
        {
            g = 99.4708025861f * Mathf.Log(temp) - 161.1195681661f;
        }
        else
        {
            g = temp - 60;
            g = 288.1221695283f * Mathf.Pow(g, -0.0755148492f);
        }

        // Blue
        if (temp >= 66) b = 255;
        else if (temp <= 19) b = 0;
        else
        {
            b = temp - 10;
            b = 138.5177312231f * Mathf.Log(b) - 305.0447927307f;
        }

        return new Color(
            Mathf.Clamp01(r / 255f),
            Mathf.Clamp01(g / 255f),
            Mathf.Clamp01(b / 255f)
        );
    }
}

