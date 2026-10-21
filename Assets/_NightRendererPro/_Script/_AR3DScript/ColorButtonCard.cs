using UnityEngine;
using UnityEngine.UI;

public class ColorButtonCard : MonoBehaviour
{
    [Header("References")]
    public Image outlineImage; // highlight border
    public Button button;       // button component
    public Color lightColor;    // color for light

    public bool isSelected = false; // Track selection state

    private LightOptionHandler manager;

    public void Init(LightOptionHandler selector)
    {
        manager = selector;
        button.onClick.AddListener(OnButtonClick);
        SetSelected(false); // Initialize as unselected
    }

    private void OnButtonClick()
    {
        // Notify manager that this card was clicked
        manager.OnColorSelected(this);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        outlineImage.enabled = selected; // Show/hide outline
    }

    public Color GetColor()
    {
        return lightColor;
    }
}
