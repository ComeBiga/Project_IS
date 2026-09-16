using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateSetter : MonoBehaviour
{
    [SerializeField]
    private bool _fixedFrameRate = true;
    [SerializeField]
    private int _targetFrameRate = 60;

    private void Awake()
    {
        if (_fixedFrameRate)
            Application.targetFrameRate = _targetFrameRate;
    }
}
