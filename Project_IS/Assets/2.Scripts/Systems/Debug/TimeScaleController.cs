using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeScaleController : MonoBehaviour
{
    //[SerializeField]
    //private bool _updateTimeScale = false;
    [SerializeField]
    private float _startTimeScale = 1f;
    //[SerializeField]
    //private bool _useTimeScalePreset = false;
    [SerializeField]
    private List<float> _timeScalePreset;

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = _startTimeScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = _timeScalePreset[0];
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = _timeScalePreset[1];
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = _timeScalePreset[2];
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Time.timeScale = _timeScalePreset[3];
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Time.timeScale = _timeScalePreset[4];
        }
    }
}
