using System.Collections;
using UnityEngine;

public class ButtonForObjectRotation : MonoBehaviour
{
    public GameObject augmentedObject;

    public GameObject Buttons;
    public PanelHideAfterFiveSec RotateButtonHint;

    public void LeftClick()
    {
        if (augmentedObject != null)
        {
            Vector3 currentEuler = augmentedObject.transform.eulerAngles;
            augmentedObject.transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y + 90f, currentEuler.z);
        }
    }

    public void RightClick()
    {
        if (augmentedObject != null)
        {
            Vector3 currentEuler = augmentedObject.transform.eulerAngles;
            augmentedObject.transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y - 90f, currentEuler.z);
        }
    }

    bool isFirstTimeHint = true;

    public void RotateObjectHintEnable()
    {
        if (isFirstTimeHint)
        {
            RotateButtonHint.ShowPanel();
            isFirstTimeHint = false;
        }
    }

}
