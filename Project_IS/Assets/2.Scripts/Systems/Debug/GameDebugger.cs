using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDebugger : MonoBehaviour
{
    public GameDebugLogSettings _settings_Log;
    public GameDebugGizmosSettings _settings_Gizmos;
    //public bool _enable = true;

    //[Header("Options")]
    //public bool _frameCount = true;
    //public GameDebug.LogCategory _logCategories;
    //public GameDebug.LogLevel _logLevels;

    private void Awake()
    {
        initialize();
        // updateOptions();
    }

    private void OnValidate()
    {
        // updateOptions();
    }

    private void initialize()
    {
        GameDebug.Initialize(_settings_Log, _settings_Gizmos);
    }

    //private void updateOptions()
    //{
    //    GameDebug.Enabled = _enable;
    //    GameDebug.frameCount = _frameCount;
    //    GameDebug.EnabledCategories = _logCategories;
    //    GameDebug.EnabledLevels = _logLevels;
    //}
}
