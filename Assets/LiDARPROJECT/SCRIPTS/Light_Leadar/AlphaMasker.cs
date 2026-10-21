using UnityEngine;
using UnityEngine.UI;

public class AlphaMasker : MonoBehaviour
{
    
    public RawImage targetRawImage;

   
    public Texture2D sourceMaskTexture; 

    
    private Texture2D finalTexture;

    
    public Texture2D baseColorTexture; 
    
 
    private int width;
    private int height;

    void Start()
    {
        if (targetRawImage == null || sourceMaskTexture == null)
        {
            
            enabled = false;
            return;
        }

        width = sourceMaskTexture.width;
        height = sourceMaskTexture.height;

        
        finalTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        finalTexture.filterMode = FilterMode.Bilinear;
        finalTexture.wrapMode = TextureWrapMode.Clamp;

        targetRawImage.texture = finalTexture;
    }

    private void Update()
    {
        UpdateTransparencyMask();
    }
    // Цю функцію викликатимемо щокадру (або коли оновлюється sourceMaskTexture)
    public void UpdateTransparencyMask()
    {
        // Перевірка, чи можна читати дані
        // ПРИМІТКА: Початкова текстура (sourceMaskTexture) повинна мати увімкнену опцію "Read/Write Enabled" в налаштуваннях імпорту!
        if (!sourceMaskTexture.isReadable)
        {
            Debug.LogError("Source Mask Texture має бути Read/Write Enabled.");
            return;
        }

        Color[] maskPixels = sourceMaskTexture.GetPixels();
        Color[] finalPixels;
        
        
        if (baseColorTexture != null && baseColorTexture.isReadable)
        {
            finalPixels = baseColorTexture.GetPixels();
        }
        else
        {
            
            finalPixels = new Color[width * height];
            for (int i = 0; i < finalPixels.Length; i++)
            {
                finalPixels[i] = Color.white; 
            }
        }


       
        for (int i = 0; i < maskPixels.Length; i++)
        {
            
            float alphaValue = (maskPixels[i].r + maskPixels[i].g + maskPixels[i].b) / 3f;

            
            finalPixels[i].a = alphaValue;
        }


        finalTexture.SetPixels(finalPixels);
        finalTexture.Apply(); 


        targetRawImage.texture = finalTexture;
    }
}
