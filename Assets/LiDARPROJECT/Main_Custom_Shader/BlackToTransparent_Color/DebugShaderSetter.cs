using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugShaderSetter : MonoBehaviour
{
    [SerializeField] private RawImage image;
    [SerializeField] private Material material;

    public float aM, soft, aP;
    
    private void Awake()
    {
        material = image.material;
    }

    public void SetAlpha(Slider slider)
    {
        material.SetFloat("_AlphaMultiplier", slider.value);
    }

    public void Standard()
    {
        material.SetFloat("_AlphaMultiplier", aM);
        material.SetFloat("_Softness", soft);
        material.SetFloat("_AlphaPower", aP);
    }

    public void SetAlphaMultiplire(Slider alpha)
    {
        material.SetFloat("_AlphaMultiplier", alpha.value);
        
        Debug.LogError(" Alpha M =========== " + alpha.value);

    }

    public void Softness(Slider softness)
    {
        material.SetFloat("_Softness", softness.value);

        Debug.LogError(" Soft =========== " + softness.value);
    }

    public void SetAlphaPower(Slider power)
    {
        material.SetFloat("_AlphaPower", power.value);
        
        Debug.LogError(" Alpha P =========== " + power.value);

    }
}
