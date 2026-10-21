using UnityEngine;

public class ARMeasureHandler : MonoBehaviour
{
    public void BackToMainScene()
    {
        SceneTransition.instance.Load_MainScene();
    }

    //public void ARSurfaceScene()
    //{
    //    SceneTransition.instance.Load_ARSurfaceScene_RainTank();
    //}

    public void ARMeasureScene()
    {
        SceneTransition.instance.Load_ARMeasureScene();
    }
}
