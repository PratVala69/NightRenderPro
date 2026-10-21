using TMPro;
using UnityEngine;

public class ClientCard : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI CompanyText;
    public TextMeshProUGUI AddressText;
    public TextMeshProUGUI SessionText;

    [Space(10)]
    public AddClientData clientData;

    // This function sets the card data from a ClientData object
    public void SetData(AddClientData data)
    {
        clientData = data;

        if (data == null)
        {
            Debug.LogWarning("ClientCard.SetData() called with null data");
            return;
        }

        NameText.text = data.firstName;
        CompanyText.text = data.companyName;
        AddressText.text = data.address;
        SessionText.text = $"Last Session: <b>{data.createdAt}</b>";
    }
}
