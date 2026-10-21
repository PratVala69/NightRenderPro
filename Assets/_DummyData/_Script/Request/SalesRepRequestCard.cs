using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SalesRepRequestCard : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI salesRepNameText;
    public TextMeshProUGUI salesRepEmailText;
    public TextMeshProUGUI totalRequestCountText;
    public TextMeshProUGUI time;
    public Button callButton;
    public Button requestOverviewButton;
    public Image profileImage;
    public Image activeStatusIcon;

    [Header("Colors")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;

    [Header("Fallbacks")]
    public Sprite defaultProfileSprite;


    public SalesRepresentative repData;

    public void SetSalesRepData(SalesRepresentative rep)
    {
        repData = rep;

        //if (PlayerPrefs.GetString("Role", "") == "company_owner")
        //{
        //    callButton.gameObject.SetActive(true);
        //}
        //else
        //{
        //   callButton.gameObject.SetActive(false);
        //}

        salesRepNameText.text = $"{rep.first_name} {rep.last_name}";
        totalRequestCountText.text = rep.total_request_count.ToString();
        time.text = DateTime.Parse(rep.created_at).ToString("dd MMM hh:mm tt");

        salesRepEmailText.text = string.IsNullOrEmpty(rep.email) ? "Email Not Found" : rep.email;

        activeStatusIcon.color = rep.is_active ? activeColor : inactiveColor;

    }

    public void SetProfileImage(Texture2D texture)
    {
        int size = Mathf.Min(texture.width, texture.height);
        int x = (texture.width - size) / 2;
        int y = (texture.height - size) / 2;

        Rect cropRect = new Rect(x, y, size, size);
        Sprite newSprite = Sprite.Create(texture, cropRect, new Vector2(0.5f, 0.5f));

        profileImage.sprite = newSprite;
    }

    public void SetDefaultProfile()
    {
        profileImage.sprite = defaultProfileSprite;
    }


    public void OpenDialPad()
    {
#if UNITY_IOS && !UNITY_EDITOR
    Application.OpenURL("tel://" + repData.phone_number);
#endif
    }
}