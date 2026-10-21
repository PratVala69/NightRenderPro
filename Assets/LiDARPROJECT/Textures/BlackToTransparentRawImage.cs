using UnityEngine;
using UnityEngine.UI;

public class BlackToTransparentRawImage : MonoBehaviour
{
    [Header("Input")]
    public Material sourceMaterial;

    [Header("Output")]
    public RawImage targetRawImage;

    [Range(0f, 0.1f)]
    public float blackThreshold = 0.02f; // наскільки "чорний" має бути чорний

    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        if (sourceMaterial == null || targetRawImage == null)
        {
            Debug.LogError("Material або RawImage не задані");
            return;
        }

        Texture2D sourceTex = sourceMaterial.mainTexture as Texture2D;

        if (sourceTex == null)
        {
            Debug.LogError("Material не містить Texture2D");
            return;
        }

        Texture2D newTex = ConvertBlackToTransparent(sourceTex);

        targetRawImage.texture = newTex;
        targetRawImage.color = Color.white;
    }

    Texture2D ConvertBlackToTransparent(Texture2D source)
    {
        Texture2D result = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false
        );

        Color[] pixels = source.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];

            // якщо колір близький до чорного — робимо прозорим
            if (c.r < blackThreshold && c.g < blackThreshold && c.b < blackThreshold)
            {
                c.a = 0f;
            }
            else
            {
                c.a = 1f;
            }

            pixels[i] = c;
        }

        result.SetPixels(pixels);
        result.Apply();

        return result;
    }
}



