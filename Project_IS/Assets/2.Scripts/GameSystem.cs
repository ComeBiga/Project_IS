using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Player Character")]
    [SerializeField]
    private GameObject _goPlayerCharacter;
    [SerializeField]
    private Transform _trCameraFollowTarget;
    [SerializeField]
    private Vector3 _cameraFollowOffset = Vector3.zero;
    [SerializeField]
    private Transform _trCameraAimTarget;
    [SerializeField]
    private Vector3 _cameraAimOffset = Vector3.zero;

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

    private void FixedUpdate()
    {
        if (_trCameraAimTarget != null)
        {
            _trCameraAimTarget.position = _goPlayerCharacter.transform.position + _cameraAimOffset;
        }

        if (_trCameraFollowTarget != null)
        {
            _trCameraFollowTarget.position = _goPlayerCharacter.transform.position + _cameraFollowOffset;
        }
    }

    private void LateUpdate()
    {
        
    }

#if UNITY_EDITOR

    [MenuItem("Tools/Select Player Character #p")]
    private static void SelectPlayerCharacter()
    {
        GameObject target = GameObject.FindWithTag("Player");
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        // EditorUtility.FocusProjectWindow();
    }
#endif
}
