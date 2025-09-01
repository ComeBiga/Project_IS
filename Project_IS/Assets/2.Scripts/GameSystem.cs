using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    [SerializeField]
    private bool _fixedFrameRate = true;
    [SerializeField]
    private int _targetFrameRate = 60;

    // Start is called before the first frame update
    void Start()
    {
        if(_fixedFrameRate)
            Application.targetFrameRate = _targetFrameRate;
    }
}
