using UnityEngine;
using TMPro; // Додайте, якщо використовуєте TextMeshPro для відображення тексту

public class FPSTracker : MonoBehaviour
{

  public TextMeshProUGUI fpsText;


  public float updateInterval = 0.5f;


  private float accum = 0; 
  private int frames = 0; 
  private float timeleft; 

  public int targetFPS = -1; 

  void Start()
  {
    Application.targetFrameRate = targetFPS;
    timeleft = updateInterval;
  }

  void Update()
  {
    timeleft -= Time.deltaTime;
    accum += Time.timeScale / Time.deltaTime; 
    ++frames;

    if (timeleft <= 0.0f)
    {

      float fps = accum / frames;
      string format = System.String.Format("{0:F2} FPS", fps); 
      fpsText.text = format;


      if (fps < 30)
        fpsText.color = Color.yellow;
      else if (fps < 10)
        fpsText.color = Color.red;
      else
        fpsText.color = Color.green;


      timeleft = updateInterval;
      accum = 0.0f;
      frames = 0;
    }
  }
}
