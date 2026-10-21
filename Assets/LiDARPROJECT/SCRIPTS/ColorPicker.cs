using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ColorPicker : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform rectTransform; // Область самого зображення
    [SerializeField] private Image targetImage; 
    [SerializeField] private LightTemperatureSync _lightTemperatureSync;

    [SerializeField] private RectTransform _pointerTr;
    [SerializeField] private Image _pointerImage;

    private Color lastPickedColor;
    private bool _canPick;

    public void CanPickColor(bool canPick)
    {
        _canPick = canPick;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(_canPick)
            PickColor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(_canPick) 
            PickColor(eventData);
    }

    private void PickColor(PointerEventData eventData)
    {
        Vector2 localPoint;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            if (rectTransform.rect.Contains(localPoint))
            {

                _pointerTr.localPosition = localPoint;

                float width = rectTransform.rect.width;
                float height = rectTransform.rect.height;


                float x = (localPoint.x - rectTransform.rect.x) / width;
                float y = (localPoint.y - rectTransform.rect.y) / height;

                Texture2D tex = targetImage.sprite.texture;
                
                int texX = (int)(x * tex.width);
                int texY = (int)(y * tex.height);


                texX = Mathf.Clamp(texX, 0, tex.width - 1);
                texY = Mathf.Clamp(texY, 0, tex.height - 1);


                lastPickedColor = tex.GetPixel(texX, texY);


                _pointerImage.color = lastPickedColor;
                if (_lightTemperatureSync != null)
                {
                    _lightTemperatureSync.SetColorFilter(lastPickedColor);
                }
            }
        }
    }
}
