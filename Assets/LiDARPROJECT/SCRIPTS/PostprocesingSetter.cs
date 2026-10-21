using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PostprocesingSetter : MonoBehaviour
{
    [SerializeField] private Volume _globalVolume;
    private ShadowsMidtonesHighlights _shadowsMidtonesHighlights;

    [SerializeField] private Slider _shadowsSlider, _midtonesSlider, _highlightsSlider, _alphaSleder;

    private float _shadowsV, _midtonesV, _highlightsV;
    [SerializeField] private float _alphaV;

    private void Awake()
    {
        if (_globalVolume.profile.TryGet(out _shadowsMidtonesHighlights))
        {
            _shadowsV = _shadowsMidtonesHighlights.shadows.value.w;
            _midtonesV = _shadowsMidtonesHighlights.midtones.value.w;
            _highlightsV = _shadowsMidtonesHighlights.highlights.value.w;
            
            
        }
    }
    private void Start()
    {
        
        _shadowsSlider.value = _shadowsV;
        _midtonesSlider.value = _midtonesV;
        _highlightsSlider.value = _highlightsV;
        _alphaSleder.value = _alphaV;
    }

    
    public void ResetVeluce()
    {
        Reseter(_shadowsV, _highlightsV, _highlightsV, _alphaV);
    }
    
    private void Reseter(float s, float m, float h, float a)
    {
        _shadowsSlider.value = s;
        _midtonesSlider.value = m;
        _highlightsSlider.value = h;
        _alphaSleder.value = a;
        
        _shadowsMidtonesHighlights.shadows = new Vector4Parameter(new Vector4(0f, 0f, 0f, s), true);
        _shadowsMidtonesHighlights.midtones = new Vector4Parameter(new Vector4(0f, 0f, 0f, m), true);
        _shadowsMidtonesHighlights.highlights = new Vector4Parameter(new Vector4(0f, 0f, 0f, h), true);
    }
    public void SetShadows(Slider slider)
    {
        _shadowsMidtonesHighlights.shadows = new Vector4Parameter(new Vector4(0f, 0f, 0f, slider.value), true);
    }
    public void SetMidtones(Slider slider)
    {
        _shadowsMidtonesHighlights.midtones = new Vector4Parameter(new Vector4(0f, 0f, 0f, slider.value), true);
    }
    public void SetHightLights(Slider slider)
    {
        _shadowsMidtonesHighlights.highlights = new Vector4Parameter(new Vector4(0f, 0f, 0f, slider.value), true);
    }
}
