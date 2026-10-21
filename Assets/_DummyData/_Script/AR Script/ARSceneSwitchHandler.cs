using UnityEngine;

public class ARSceneSwitchHandler : MonoBehaviour
{
    public void BackToMainScene()
    {
        SceneTransition.instance.Load_MainScene();
    }

    //public void ARSurfaceScene_RainTank()
    //{
    //    SceneTransition.instance.Load_ARSurfaceScene_RainTank();
    //}

    //public void ARSurfaceScene_SplashBlock()
    //{
    //    SceneTransition.instance.Load_ARSurfaceScene_SplashBlock();
    //}

    public void ARMeasureScene()
    {
        SceneTransition.instance.Load_ARMeasureScene();
    }
}
