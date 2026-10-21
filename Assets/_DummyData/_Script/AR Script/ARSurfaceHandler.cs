using System.Collections;
using UnityEngine;

public class ARSurfaceHandler : MonoBehaviour
{
    public GameObject createButtonObject;
    public GameObject cancelButtonObject;
    public Animation objectMenuAnimation;
    public Animation gutterMenuAnimation;
    public GameObject createButtongutter;
    public GameObject cancelButtongutter;

    public void OnMenuHide()
    {
        //createButtonObject.SetActive(false);
        //cancelButtonObject.SetActive(true);
        objectMenuAnimation.Play("ProductMenuHide");
    }

    public void OnMenuHideAndDeactivate()
    {
        //createButtonObject.SetActive(false);
        //cancelButtonObject.SetActive(true);
        objectMenuAnimation.Play("ProductMenuHide");
        StartCoroutine(DeactivateAfterAnimation(objectMenuAnimation , gutterMenuAnimation));
    }

    public void OnMenuShow()
    {
        objectMenuAnimation.Play("ProductMenuShow");
    }

    public void GutterMenuOpen()
    {
        gutterMenuAnimation.Play("ProductMenuShow");
    }

    public void GutterMenuHide()
    {
        //createButtongutter.SetActive(true);
        //cancelButtongutter.SetActive(false);
        gutterMenuAnimation.Play("ProductMenuHide");
    }

    public void GutterMenuHideAndDeactivate()
    {
        gutterMenuAnimation.Play("ProductMenuHide");
        StartCoroutine(DeactivateAfterAnimation(gutterMenuAnimation , objectMenuAnimation));
    }

    IEnumerator DeactivateAfterAnimation(Animation DeactiveAnim, Animation activateAnim)
    {
        if (DeactiveAnim != null )//&& DeactiveAnim["ProductMenuHide"] != null)
        {
            //float length = DeactiveAnim["ProductMenuHide"].length;
            yield return new WaitForSeconds(0.45f);
            DeactiveAnim.gameObject.SetActive(false);
//            Debug.Log("Deactivate : " + DeactiveAnim.name);
            yield return StartCoroutine(ActivateAfterAnimation(activateAnim));

        }
    }

    IEnumerator ActivateAfterAnimation(Animation anim)
    {
        if (anim != null)// && anim["ProductMenuShow"] != null)
        {
            anim.gameObject.SetActive(true);
       //     Debug.Log("ActivateAfterAnimation : " + anim.name);

            //float length = anim["ProductMenuShow"].length;
            yield return new WaitForSeconds(0.45f);
            anim.Play("ProductMenuShow");
        }
    }
}
