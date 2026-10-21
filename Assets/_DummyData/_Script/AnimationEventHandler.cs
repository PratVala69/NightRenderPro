using UnityEngine;

public class AnimationEventHandler: MonoBehaviour
{
    public GameObject selectClientPanel;
    public GameObject imageDetailPanel;

    public void ClientDetailPanel_Hide()
    {
        selectClientPanel.SetActive(false);
    }

    public void ImageDetailPanel_Hide()
    {
        imageDetailPanel.SetActive(false);
    }
}
