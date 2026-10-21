using UnityEngine;
using UnityEngine.UI;
using TMPro; // if using TextMeshPro

public class ToggleButton : MonoBehaviour
{
    public bool isSelected = false;
    public Button button;
    public Image image;
    public TextMeshProUGUI label; // for button text

    [Header("Colors")]
    public Sprite selectedSprite;// = new Color(0.1f, 0.2f, 0.4f);  // dark blue bg
    public Sprite deselectedSprite;// = new Color(0.8f, 0.8f, 0.8f); // light gray bg

    [Header("Text Colors")]
    public Color selectedTextColor = Color.white;
    public Color deselectedTextColor = Color.black;

    private void Awake()
    {
        button.onClick.AddListener(Toggle);
    }

    public void Toggle()
    {
        isSelected = !isSelected;
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (image != null)
            image.sprite = isSelected ? selectedSprite : deselectedSprite;

        if (label != null)
            label.color = isSelected ? selectedTextColor : deselectedTextColor;
    }
}
