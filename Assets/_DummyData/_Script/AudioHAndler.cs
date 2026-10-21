using UnityEngine;

public class AudioHAndler : MonoBehaviour
{
    public AudioSource buttonClickAS;
    public AudioSource captureAS;

    public void ButtonClick_AudioPlay()
    {
        if (buttonClickAS.isPlaying)
        {
            buttonClickAS.Stop();
        }
        buttonClickAS.Play();
    }

    public void CaptureClick_AudioPlay()
    {
        if (captureAS.isPlaying)
        {
            captureAS.Stop();
        }
        captureAS.Play();
    }
}
