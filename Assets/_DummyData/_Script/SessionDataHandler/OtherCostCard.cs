using UnityEngine;
using TMPro;
using UnityEngine.UI;


public class OtherCostCard : MonoBehaviour
{
    public TMP_InputField otherCostName;
    public TMP_InputField otherCostPrice;
    public Button deleteButton;

    public float otherCostPricefloat
    {
        get
        {
            if (float.TryParse(otherCostPrice.text, out float value))
                return value;
            return 0f; // fallback if input is invalid or empty
        }
    }

    //void Start()
    //{
    //    otherCostPrice.onValueChanged.AddListener(OnPriceChanged);
    //}

    //private void OnPriceChanged(string newValue)
    //{
    //    SessionDetailFetcher.instance.RecalculateTotal();
    //}
}
