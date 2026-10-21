using System.Collections;
using UnityEngine;

public class PanelHideAfterFiveSec : MonoBehaviour
{
    public GameObject panel; // Assign your panel in the Inspector

    private Coroutine hideCoroutine;

    // Call this to show the panel and hide it after 5 seconds
    public void ShowPanel()
    {
        panel.SetActive(true);

        // Cancel any existing coroutine
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        // Start auto-hide timer
        hideCoroutine = StartCoroutine(HidePanelAfterDelay(5f));
    }

    // Call this to hide the panel immediately
    public void HidePanel()
    {
        // Cancel the auto-hide timer if it's running
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        panel.SetActive(false);
    }

    // Internal coroutine to hide after a delay
    private IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        panel.SetActive(false);
    }
}
