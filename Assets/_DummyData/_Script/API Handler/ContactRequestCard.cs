using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ContactRequestCard : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text dateText;

    public GameObject pendingStatus;
    public GameObject processingStatus;
    public GameObject resolvedStatus;

    public Button rootButton;

    public string requestId;
    public string status;

    public ContactRequestData data;

    public void Init(ContactRequestData data, ContactRequest parent)
    {
        titleText.text = data.title;
        descriptionText.text = TruncateText(data.description, 80);
        dateText.text = FormatDate(data.created_at);
        status = data.status;

        requestId = data.id;
        this.data = data;

        // Reset all status indicators
        pendingStatus.SetActive(false);
        processingStatus.SetActive(false);
        resolvedStatus.SetActive(false);

        // Enable the correct status GameObject
        switch (status)
        {
            case "pending":
                pendingStatus.SetActive(true);
                break;
            case "processing":
                processingStatus.SetActive(true);
                break;
            case "resolved":
                resolvedStatus.SetActive(true);
                break;
        }
    }

    string TruncateText(string text, int maxChars)
    {
        return text.Length > maxChars ? text.Substring(0, maxChars) + "..." : text;
    }

    string FormatDate(string isoDate)
    {
        if (System.DateTime.TryParse(isoDate, out System.DateTime dt))
            return dt.ToString("dd MMM hh:mm tt"); // Example: 16 Apr 12:23 PM
        return isoDate;
    }

}
