using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    public event Action onFootStep = null;
    public event Action onFootStepSmall = null;
    public event Action onFootStepMedium = null;
    public event Action onFootStepBig = null;
    public event Action onTouchHand = null;
    public event Action<int> onFrontFoot = null;
    public event Action onReleaseHand = null;

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

    private void FootPosition(int index)
    {
        switch (index)
        {
            case 0:
                // Left
                break;
            case 1:
                // Right
                break;
        }

        onFrontFoot?.Invoke(index);
    }

    private void ReleaseHand()
    {
        onReleaseHand?.Invoke();
    }
}
