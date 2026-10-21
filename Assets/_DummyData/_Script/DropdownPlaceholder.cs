using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class DropdownPlaceholder : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] public GameObject firstElement; // Public field to store the first option's GameObject

    private void Start()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        // Add listener to detect when dropdown is opened
       // dropdown.onValueChanged.AddListener(delegate { InitializeDropdownOption(); });

    }
    [ContextMenu("Initialize dropdown option")]
    public void InitializeDropdownOption()
    {
        //StartCoroutine(DisableFirstOption());

        firstElement = dropdown.transform.GetChild(3).transform.GetChild(0).transform.GetChild(0).transform.GetChild(1).gameObject;
        firstElement.SetActive(false);
        RectTransform rt = dropdown.transform.GetChild(3).GetComponent<RectTransform>();

        // Reduce height by 120
        Vector2 newSize = rt.sizeDelta;
        newSize.y -= 120;
        rt.sizeDelta = newSize;
    }
}