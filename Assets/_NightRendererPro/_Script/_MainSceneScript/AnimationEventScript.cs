using UnityEngine;

public class AnimationEventScript : MonoBehaviour
{
    public GameObject logoutPopup;

    public void LogoutPopupShow()
    {
        logoutPopup.SetActive(true);
    }

    public void LogoutPopupHide()
    {
        logoutPopup.SetActive(false);
    }
}
