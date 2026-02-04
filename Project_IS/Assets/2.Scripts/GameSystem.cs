using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    [SerializeField]
    private bool _fixedFrameRate = true;
    [SerializeField]
    private int _targetFrameRate = 60;
    [SerializeField]
    private bool _updateTimeScale = false;
    [SerializeField]
    private float _timeScale = 1f;

    [Header("CheckPoint")]
    [SerializeField]
    private Transform _trPlayerCharacter;
    [SerializeField]
    private Vector3 _checkPoint = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        if(_fixedFrameRate)
            Application.targetFrameRate = _targetFrameRate;

        Time.timeScale = _timeScale;
    }

    private void Update()
    {
        if (_updateTimeScale)
            Time.timeScale = _timeScale;

        if (Input.GetKeyDown(KeyCode.P))
        {
            Vector3 position = _trPlayerCharacter.position;
            position.x = _checkPoint.x;
            position.y = _checkPoint.y;
            _trPlayerCharacter.position = position;
        }
    }
}
