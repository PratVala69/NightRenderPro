using UnityEngine;

public class EnabledDisabled : MonoBehaviour
{
    private void OnEnable()
    {
        Debug.Log("Login Again Enabled");
    }

    private void OnDisable()
    {
        Debug.Log("Login Again Disabled");
    }
}
