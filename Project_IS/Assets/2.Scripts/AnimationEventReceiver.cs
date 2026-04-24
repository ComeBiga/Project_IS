using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    public event Action onFootStep = null;
    public event Action onFootStepSmall = null;
    public event Action onFootStepMedium = null;
    public event Action onFootStepBig = null;
    public event Action onTouchHand = null;

    private void FootStep()
    {
        onFootStep?.Invoke();
    }

    private void FootStep_Small()
    {
        onFootStepSmall?.Invoke();
        // Debug.Log("Foot Step Small");
    }

    private void FootStep_Medium()
    {
        onFootStepMedium?.Invoke();
        // Debug.Log("Foot Step Medium");
    }

    private void FootStep_Big()
    {
        onFootStepBig?.Invoke();
        // Debug.Log("Foot Step Big");
    }

    private void HandTouch()
    {
        onTouchHand?.Invoke();
        // Debug.Log("Hand Touch");
    }

}
